using Microsoft.Extensions.Caching.Distributed;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace SupportBot.Services;

public interface ISessionManager
{
    Task<UserSession> GetOrCreateSessionAsync(string sessionId);
    Task UpdateSessionAsync(UserSession session);
    Task RemoveSessionAsync(string sessionId);
    Task<bool> SessionExistsAsync(string sessionId);
}

public class RedisSessionManager : ISessionManager
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _sessionTimeout;

    public RedisSessionManager(IDistributedCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _sessionTimeout = TimeSpan.FromMinutes(
            configuration.GetValue<int>("SessionTimeoutMinutes", 120));
    }

    public async Task<UserSession> GetOrCreateSessionAsync(string sessionId)
    {
        var sessionJson = await _cache.GetStringAsync($"session:{sessionId}");
        
        if (sessionJson != null)
        {
            var existingSession = JsonSerializer.Deserialize<UserSessionData>(sessionJson);
            if (existingSession != null)
            {
                return new UserSession
                {
                    Id = sessionId,
                    ChatHistory = DeserializeChatHistory(existingSession.ChatMessages),
                    CreatedAt = existingSession.CreatedAt,
                    LastActivity = DateTime.UtcNow
                };
            }
        }

        // Create new session
        var newSession = new UserSession
        {
            Id = sessionId,
            ChatHistory = new ChatHistory(),
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };

        await UpdateSessionAsync(newSession);
        return newSession;
    }

    public async Task UpdateSessionAsync(UserSession session)
    {
        session.LastActivity = DateTime.UtcNow;
        
        var sessionData = new UserSessionData
        {
            CreatedAt = session.CreatedAt,
            LastActivity = session.LastActivity,
            ChatMessages = SerializeChatHistory(session.ChatHistory)
        };

        var sessionJson = JsonSerializer.Serialize(sessionData);
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = _sessionTimeout
        };

        await _cache.SetStringAsync($"session:{session.Id}", sessionJson, options);
    }

    public async Task AddChatHistoryToChatMessagesAsync(string sessionId, ChatHistory newMessages)
    {
        var session = await GetOrCreateSessionAsync(sessionId);

        // Append each new message into the session's ChatHistory
        foreach (var message in newMessages)
        {
            if (message.Role == AuthorRole.User)
                session.ChatHistory.AddUserMessage(message.Content ?? string.Empty);
            else if (message.Role == AuthorRole.Assistant)
                session.ChatHistory.AddAssistantMessage(message.Content ?? string.Empty);
        }

        // Save updated session to Redis
        await UpdateSessionAsync(session);
    }

    public async Task RemoveSessionAsync(string sessionId)
    {
        await _cache.RemoveAsync($"session:{sessionId}");
    }

    public async Task<bool> SessionExistsAsync(string sessionId)
    {
        var sessionJson = await _cache.GetStringAsync($"session:{sessionId}");
        return sessionJson != null;
    }

    private List<ChatMessageData> SerializeChatHistory(ChatHistory chatHistory)
    {
        return chatHistory.Select(message => new ChatMessageData
        {
            Role = message.Role.ToString(),
            Content = message.Content ?? string.Empty
        }).ToList();
    }

    private ChatHistory DeserializeChatHistory(List<ChatMessageData> messages)
    {
        var chatHistory = new ChatHistory();
        foreach (var message in messages)
        {
            if (message.Role == "User")
                chatHistory.AddUserMessage(message.Content);
            else if (message.Role == "Assistant")
                chatHistory.AddAssistantMessage(message.Content);
        }
        return chatHistory;
    }
}

public class UserSessionData
{
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public List<ChatMessageData> ChatMessages { get; set; } = new();
}

public class ChatMessageData
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class UserSession
{
    public required string Id { get; set; }
    public required ChatHistory ChatHistory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
}