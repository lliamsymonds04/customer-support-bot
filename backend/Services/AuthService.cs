using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SupportBot.Data;
using Microsoft.EntityFrameworkCore;
using SupportBot.Models;
using System.Text.Json;
using System.Net.Http.Headers;

public interface IAuthService
{
    string GenerateJwtToken(User user, bool isRefreshToken = false);
    ClaimsPrincipal? ValidateJwtToken(string token);
    Task<bool> UserExistsAsync(string username);
    int GetUserIdByJwt(string token);
    string GetUserJwtToken();
    Task<User?> ExchangeGitHubCode(string code);
    Task<User?> ExchangeGoogleCode(string code);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfiguration configuration, AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string GenerateJwtToken(User user, bool isRefreshToken = false)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT Secret Key is not configured.");
        }

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            SecurityAlgorithms.HmacSha256
        );

        Claim[] claims;
        int expiryTime;

        if (!isRefreshToken)
        {
            // Access token claims
            claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            ];
            expiryTime = _configuration.GetValue<int>("JwtSettings:ExpiryTime");
        }
        else
        {
            claims = [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ];
            expiryTime = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryTime");
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryTime),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateJwtToken(string token)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT Secret Key is not configured.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            // Token validation failed
            return null;
        }
    }

    public int GetUserIdByJwt(string token)
    {
        var principal = ValidateJwtToken(token);
        if (principal == null)
        {
            throw new UnauthorizedAccessException("Invalid JWT token.");
        }

        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub) ??
                          principal.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in JWT token.");
        }

        return userId;
    }

    public string GetUserJwtToken()
    {
        var cookieName = _configuration["JwtSettings:AuthTokenName"] ?? "AuthToken";
        var token = _httpContextAccessor.HttpContext?.Request.Cookies[cookieName];

        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException("JWT token is missing.");
        }

        return token;
    }

    public async Task<bool> UserExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<User?> ExchangeGitHubCode(string code)
    {
        var clientId = _configuration["GitHub:ClientId"];
        var clientSecret = _configuration["GitHub:ClientSecret"];
        var redirectUri = _configuration["Auth:GitHub:RedirectUri"];

        if (string.IsNullOrEmpty(redirectUri))
        {
            throw new InvalidOperationException("GitHub Redirect URI is not configured.");
        }

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("GitHub Client ID and Client Secret are not configured.");
        }

        using var httpClient = new HttpClient();
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        _logger.LogInformation("Exchanging GitHub code for access token...");
        var requestUri = $"https://github.com/login/oauth/access_token";
        var tokenResponse = await httpClient.PostAsync(requestUri, tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();
        _logger.LogInformation("GitHub token exchange successful.");
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        _logger.LogInformation("Token response: {TokenContent}", tokenContent);
        var tokenData = System.Web.HttpUtility.ParseQueryString(tokenContent);
        var accessToken = tokenData["access_token"];
        _logger.LogInformation("Access token received: {AccessToken}", accessToken);


        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("support-bot");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", accessToken);

        var userResponse = await httpClient.GetAsync("https://api.github.com/user");
        userResponse.EnsureSuccessStatusCode();
        var userJson = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
        var githubId = userJson.RootElement.GetProperty("id").GetInt64();
        var username = userJson.RootElement.GetProperty("login").GetString();

        if (string.IsNullOrEmpty(username))
        {
            return null;
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.GithubId == githubId.ToString());
        if (user == null)
        {
            user = new User
            {
                GithubId = githubId.ToString(),
                Username = username
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User?> ExchangeGoogleCode(string code)
    {
        var clientId = _configuration["Google:ClientId"];
        var clientSecret = _configuration["Google:ClientSecret"];
        var redirectUri = _configuration["Auth:Google:RedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Google Client ID and Client Secret are not configured.");
        }

        if (string.IsNullOrEmpty(redirectUri))
        {
            throw new InvalidOperationException("Google Redirect URI is not configured.");
        }

        using var httpClient = new HttpClient();
        
        // Exchange code for access token
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to exchange Google code: {tokenContent}");
        }

        var tokenJson = JsonDocument.Parse(tokenContent);
        var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();

        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }

        // Get user info from Google
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var userResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
        var userContent = await userResponse.Content.ReadAsStringAsync();

        if (!userResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to get Google user info: {userContent}");
        }

        var userJson = JsonDocument.Parse(userContent);
        var googleId = userJson.RootElement.GetProperty("id").GetString();
        var email = userJson.RootElement.GetProperty("email").GetString();
        var name = userJson.RootElement.GetProperty("name").GetString();

        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
        {
            return null;
        }

        // Check if user already exists by Google ID
        var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        if (user == null)
        {
            // Create new user with email as username (or name if email is not suitable)
            var username = email.Split('@')[0]; // Use part before @ as username
            
            // Ensure username is unique
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                username = $"{username}_{googleId.Substring(0, 6)}"; // Make it unique
            }

            user = new User
            {
                GoogleId = googleId,
                Username = username,
                Email = email
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return user;
    }
}