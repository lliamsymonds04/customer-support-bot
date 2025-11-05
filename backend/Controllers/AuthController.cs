using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportBot.Data;
using SupportBot.Models;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using SupportBot.Services;

namespace InventoryAPI.Controllers;

enum TokenType
{
    AuthToken,
    RefreshToken,
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext? _context;
    private static readonly PasswordHasher<string> _passwordHasher = new PasswordHasher<string>();
    private readonly IAuthService _authService;
    private readonly InMemoryAuthService? _fallbackAuthService;
    private readonly InMemoryDataStore? _fallbackDataStore;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, IAuthService authService, ILogger<AuthController> logger, AppDbContext? context = null, InMemoryDataStore? fallbackDataStore = null)
    {
        _configuration = configuration;
        _context = context;
        _authService = authService;
        _logger = logger;
        _fallbackDataStore = fallbackDataStore;
        
        if (_fallbackDataStore != null && context == null)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var inMemoryLogger = loggerFactory.CreateLogger<InMemoryAuthService>();
            
            _fallbackAuthService = new InMemoryAuthService(
                configuration, 
                _fallbackDataStore, 
                new HttpContextAccessor { HttpContext = HttpContext },
                inMemoryLogger
            );
        }
    }

    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public bool RememberMe { get; set; } = false;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        User? user = null;
        
        try
        {
            if (_context != null)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database login failed, falling back to in-memory auth service");
            if (_fallbackDataStore != null)
            {
                user = _fallbackDataStore.GetUserByUsername(request.Username);
            }
        }
        
        if (user == null)
        {
            return Unauthorized("Invalid username or password.");
        }

        // hash the incoming password and compare it with the stored hash
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return Unauthorized("Invalid username or password.");
        }
        var result = _passwordHasher.VerifyHashedPassword(request.Username, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid username or password.");
        }

        try
        {
            if (_context != null)
            {
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save context after login");
        }

        HandleToken(user, TokenType.AuthToken);
        if (request.RememberMe)
        {
            HandleToken(user, TokenType.RefreshToken);
        }

        return Ok(new
        {
            role = user.Role.ToString(),
        });
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] LoginRequest request)
    {
        User? existingUser = null;
        
        try
        {
            if (_context != null)
            {
                existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database signup check failed, falling back to in-memory auth service");
            if (_fallbackDataStore != null)
            {
                existingUser = _fallbackDataStore.GetUserByUsername(request.Username);
            }
        }

        if (existingUser != null)
        {
            return BadRequest("Username already exists.");
        }

        // hash the password before saving
        var passwordHash = _passwordHasher.HashPassword(request.Username, request.Password);

        var newUser = new User
        {
            Username = request.Username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            Role = Roles.User
        };

        try
        {
            if (_context != null)
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
            else if (_fallbackDataStore != null)
            {
                newUser.Id = _fallbackDataStore.GetNextUserId();
                _fallbackDataStore.AddUser(newUser);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database signup failed, falling back to in-memory auth service");
            if (_fallbackDataStore != null)
            {
                newUser.Id = _fallbackDataStore.GetNextUserId();
                _fallbackDataStore.AddUser(newUser);
            }
            else
            {
                return StatusCode(500, "Failed to create user.");
            }
        }

        var token = HandleToken(newUser, TokenType.AuthToken);

        if (request.RememberMe)
        {
            HandleToken(newUser, TokenType.RefreshToken);
        }

        return Ok(new
        {
            role = newUser.Role.ToString(),
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Clear the JWT cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1) // Set to a past date to clear the cookie
        };

        var cookieName = _configuration["JwtSettings:AuthTokenName"] ?? "auth_token";
        Response.Cookies.Append(cookieName, "", cookieOptions);

        //remove the refresh token cookie
        var refreshCookieName = _configuration["JwtSettings:RefreshTokenName"] ?? "refresh_token";
        Response.Cookies.Append(refreshCookieName, "", cookieOptions);

        return Ok("Logged out successfully.");
    }

    [HttpGet("check")]
    public IActionResult Check()
    {
        var cookieName = _configuration["JwtSettings:AuthTokenName"] ?? "auth_token";
        if (Request.Cookies.TryGetValue(cookieName, out var token))
        {
            try
            {
                var principal = _authService.ValidateJwtToken(token);
                if (principal != null)
                {
                    return Ok("User is authenticated.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Main auth service validation failed, trying fallback");
                if (_fallbackAuthService != null)
                {
                    var principal = _fallbackAuthService.ValidateJwtToken(token);
                    if (principal != null)
                    {
                        return Ok("User is authenticated.");
                    }
                }
            }
        }
        return Unauthorized("User is not authenticated.");
    }

    [HttpGet("refresh")]
    public IActionResult Refresh()
    {
        var cookieName = _configuration["JwtSettings:RefreshTokenName"] ?? "refresh_token";
        if (Request.Cookies.TryGetValue(cookieName, out var token))
        {
            Console.WriteLine("Validating refresh token...");
            IAuthService activeAuthService = _authService;
            
            try
            {
                var principal = _authService.ValidateJwtToken(token);
                if (principal == null)
                {
                    return Unauthorized("User is not authenticated.");
                }
                
                Console.WriteLine("Refresh token is valid, generating new auth token...");
                var userId = _authService.GetUserIdByJwt(token);
                Console.WriteLine("User ID found:", userId);
                
                User? user = null;
                if (_context != null)
                {
                    user = _context.Users.Find(userId);
                }
                
                if (user != null)
                {
                    HandleToken(user, TokenType.AuthToken);
                    return Ok(new
                    {
                        role = user.Role,
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Main auth service refresh failed, trying fallback");
                if (_fallbackAuthService != null && _fallbackDataStore != null)
                {
                    try
                    {
                        var principal = _fallbackAuthService.ValidateJwtToken(token);
                        if (principal != null)
                        {
                            var userId = _fallbackAuthService.GetUserIdByJwt(token);
                            var user = _fallbackDataStore.GetUserById(userId);
                            if (user != null)
                            {
                                HandleToken(user, TokenType.AuthToken);
                                return Ok(new
                                {
                                    role = user.Role,
                                });
                            }
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback auth service also failed");
                    }
                }
            }
        }

        return Unauthorized("User is not authenticated.");
    }

    [HttpGet("role")]
    public IActionResult GetUserRole()
    {
        try
        {
            var token = _authService.GetUserJwtToken();
            var userId = _authService.GetUserIdByJwt(token);
            
            User? user = null;
            if (_context != null)
            {
                user = _context.Users.Find(userId);
            }
            
            if (user != null)
            {
                return Ok(new
                {
                    role = user.Role,
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Main auth service get role failed, trying fallback");
            if (_fallbackAuthService != null && _fallbackDataStore != null)
            {
                try
                {
                    var token = _fallbackAuthService.GetUserJwtToken();
                    var userId = _fallbackAuthService.GetUserIdByJwt(token);
                    var user = _fallbackDataStore.GetUserById(userId);
                    if (user != null)
                    {
                        return Ok(new
                        {
                            role = user.Role,
                        });
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback auth service also failed");
                }
            }
        }

        return Unauthorized("User is not authenticated.");
    }

    private string HandleToken(User user, TokenType tokenType)
    {
        var token = _authService.GenerateJwtToken(user, tokenType == TokenType.RefreshToken);

        var expiryTime = tokenType == TokenType.AuthToken
            ? _configuration.GetValue<int>("JwtSettings:ExpiryTime")
            : _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryTime");

        var cookieOptions = new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(expiryTime)
        };

        var cookieName = tokenType == TokenType.AuthToken
            ? _configuration["JwtSettings:AuthTokenName"] ?? "auth_token"
            : _configuration["JwtSettings:RefreshTokenName"] ?? "refresh_token";

        Response.Cookies.Append(cookieName, token, cookieOptions);

        return token;
    }

    [HttpGet("github/login")]
    public IActionResult GitHubLogin()
    {

        var redirectUri = _configuration["Auth:GitHub:RedirectUri"];
        var authorizationUrl = $"https://github.com/login/oauth/authorize?client_id={_configuration["GitHub:ClientId"]}&redirect_uri={redirectUri}";
        return Redirect(authorizationUrl);
    }

    private string GetOauthRedirectUri()
    {
        var baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var redirectUri = _configuration["Frontend:OAuthRedirectUri"] ?? "/";
        // Join the base URL and the redirect URI
        return new Uri(new Uri(baseUrl), redirectUri).ToString();
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback(string code)
    {
        User? user = null;
        
        try
        {
            user = await _authService.ExchangeGitHubCode(code);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Main auth service GitHub exchange failed, trying fallback");
            if (_fallbackAuthService != null)
            {
                try
                {
                    user = await _fallbackAuthService.ExchangeGitHubCode(code);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback auth service GitHub exchange also failed");
                }
            }
        }
        
        if (user == null)
        {
            return Unauthorized();
        }

        HandleToken(user, TokenType.AuthToken);
        HandleToken(user, TokenType.RefreshToken);

        Console.WriteLine($"User {user.Username} authenticated via GitHub.");
        var redirectUri = GetOauthRedirectUri();
        if (!string.IsNullOrEmpty(redirectUri))
        {
            //add role to query params
            var queryParams = new Dictionary<string, string>
            {
                { "role", user.Role.ToString() },
                { "username", user.Username }
            };
            var uriBuilder = new UriBuilder(redirectUri)
            {
                Query = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync()
            };
            return Redirect(uriBuilder.ToString());
        }

        return Redirect("/");
    }

    [HttpGet("google/login")]
    public IActionResult GoogleLogin()
    {
        var redirectUri = _configuration["Auth:Google:RedirectUri"];
        var clientId = _configuration["Google:ClientId"];
        var scope = "openid email profile";
        var authorizationUrl = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&scope={scope}&response_type=code";
        return Redirect(authorizationUrl);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(string code)
    {
        User? user = null;
        
        try
        {
            user = await _authService.ExchangeGoogleCode(code);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Main auth service Google exchange failed, trying fallback");
            if (_fallbackAuthService != null)
            {
                try
                {
                    user = await _fallbackAuthService.ExchangeGoogleCode(code);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback auth service Google exchange also failed");
                }
            }
        }
        
        if (user == null)
        {
            return Unauthorized();
        }

        HandleToken(user, TokenType.AuthToken);
        HandleToken(user, TokenType.RefreshToken);

        Console.WriteLine($"User {user.Username} authenticated via Google.");
        var redirectUri = GetOauthRedirectUri();
        if (!string.IsNullOrEmpty(redirectUri))
        {
            //add role to query params
            var queryParams = new Dictionary<string, string>
            {
                { "role", user.Role.ToString() },
                { "username", user.Username }
            };
            var uriBuilder = new UriBuilder(redirectUri)
            {
                Query = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync()
            };
            return Redirect(uriBuilder.ToString());
        }

        return Redirect("/");
    }
}