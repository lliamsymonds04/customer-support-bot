using SupportBot.Models;

namespace SupportBot.Services;

public class InMemoryAuthService : BaseAuthService
{
    private readonly InMemoryDataStore _dataStore;

    public InMemoryAuthService(IConfiguration configuration, InMemoryDataStore dataStore, IHttpContextAccessor httpContextAccessor, ILogger<InMemoryAuthService> logger)
        : base(configuration, httpContextAccessor, logger)
    {
        _dataStore = dataStore;
    }

    public override Task<bool> UserExistsAsync(string username)
    {
        var user = _dataStore.GetUserByUsername(username);
        return Task.FromResult(user != null);
    }

    protected override Task<User?> GetOrCreateGitHubUserAsync(string githubId, string username)
    {
        var user = _dataStore.GetUserByGithubId(githubId);
        if (user == null)
        {
            user = new User
            {
                Id = _dataStore.GetNextUserId(),
                GithubId = githubId,
                Username = username
            };
            _dataStore.AddUser(user);
        }

        return Task.FromResult<User?>(user);
    }

    protected override Task<User?> GetOrCreateGoogleUserAsync(string googleId, string email)
    {
        var user = _dataStore.GetUserByGoogleId(googleId);
        if (user == null)
        {
            var username = email.Split('@')[0];
            
            var existingUser = _dataStore.GetUserByUsername(username);
            if (existingUser != null)
            {
                username = $"{username}_{googleId.Substring(0, 6)}";
            }

            user = new User
            {
                Id = _dataStore.GetNextUserId(),
                GoogleId = googleId,
                Username = username,
                Email = email
            };
            _dataStore.AddUser(user);
        }

        return Task.FromResult<User?>(user);
    }
}
