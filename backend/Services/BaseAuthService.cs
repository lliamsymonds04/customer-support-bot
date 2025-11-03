using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SupportBot.Models;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SupportBot.Services;

public abstract class BaseAuthService : IAuthService
{
    protected readonly IConfiguration _configuration;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly ILogger _logger;

    protected BaseAuthService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger logger)
    {
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

        return await GetOrCreateGitHubUserAsync(githubId.ToString(), username);
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

        return await GetOrCreateGoogleUserAsync(googleId, email);
    }

    public abstract Task<bool> UserExistsAsync(string username);
    protected abstract Task<User?> GetOrCreateGitHubUserAsync(string githubId, string username);
    protected abstract Task<User?> GetOrCreateGoogleUserAsync(string googleId, string email);
}
