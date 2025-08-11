using Microsoft.Extensions.Caching.Distributed;
using SupportBot.Models;
using System.Text.Json;

namespace SupportBot.Services;

public interface IFormService
{
    Task<Form[]> GetSessionFormsAsync(string sessionId);
    Task CacheFormForSessionAsync(Form form, string sessionId);
}

public class FormService : IFormService
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _sessionTimeout;

    public FormService(IDistributedCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _sessionTimeout = TimeSpan.FromMinutes(
            configuration.GetValue<int>("SessionTimeoutMinutes", 120));
    }

    public Task<Form[]> GetSessionFormsAsync(string sessionId)
    {
        var sessionFormsJson = _cache.GetString($"sessionForms:{sessionId}");
        if (sessionFormsJson != null)
        {
            var forms = JsonSerializer.Deserialize<Form[]>(sessionFormsJson);
            return Task.FromResult(forms ?? Array.Empty<Form>());
        }
        return Task.FromResult(Array.Empty<Form>());
    }

    public async Task CacheFormForSessionAsync(Form form, string sessionId)
    {
        var sessionForms = await GetSessionFormsAsync(sessionId);
        var updatedForms = sessionForms.Append(form).ToArray();
        
        var sessionFormsJson = JsonSerializer.Serialize(updatedForms);
        await _cache.SetStringAsync($"sessionForms:{sessionId}", sessionFormsJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _sessionTimeout
        });
    }
}