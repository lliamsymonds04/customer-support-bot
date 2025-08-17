using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportBot.Data;
using SupportBot.Models;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

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
    private readonly AppDbContext _context;
    private static readonly PasswordHasher<string> _passwordHasher = new PasswordHasher<string>();
    private readonly IAuthService _authService;

    public AuthController(AppDbContext context, IConfiguration configuration, IAuthService authService)
    {
        _configuration = configuration;
        _context = context;
        _authService = authService;
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
        {
            return Unauthorized("Invalid username or password.");
        }

        // hash the incoming password and compare it with the stored hash
        var result = _passwordHasher.VerifyHashedPassword(request.Username, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid username or password.");
        }

        await _context.SaveChangesAsync();

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
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

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

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

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
            var principal = _authService.ValidateJwtToken(token);
            if (principal != null)
            {
                return Ok("User is authenticated.");
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
            var principal = _authService.ValidateJwtToken(token);
            if (principal != null)
            {
                Console.WriteLine("Refresh token is valid, generating new auth token...");
                var userId = _authService.GetUserIdByJwt(token);
                Console.WriteLine("User ID found:", userId);
                var user = _context.Users.Find(userId);
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
}