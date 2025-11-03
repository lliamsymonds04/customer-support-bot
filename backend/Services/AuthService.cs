using SupportBot.Data;
using Microsoft.EntityFrameworkCore;
using SupportBot.Models;
using SupportBot.Services;
using System.Security.Claims;

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

public class AuthService : BaseAuthService
{
    private readonly AppDbContext _context;

    public AuthService(IConfiguration configuration, AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        : base(configuration, httpContextAccessor, logger)
    {
        _context = context;
    }

    public override async Task<bool> UserExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    protected override async Task<User?> GetOrCreateGitHubUserAsync(string githubId, string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.GithubId == githubId);
        if (user == null)
        {
            user = new User
            {
                GithubId = githubId,
                Username = username
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return user;
    }

    protected override async Task<User?> GetOrCreateGoogleUserAsync(string googleId, string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        if (user == null)
        {
            var username = email.Split('@')[0];
            
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                username = $"{username}_{googleId.Substring(0, 6)}";
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