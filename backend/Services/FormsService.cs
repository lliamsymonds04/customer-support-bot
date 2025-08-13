using Microsoft.Extensions.Caching.Distributed;
using SupportBot.Data;
using SupportBot.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SupportBot.Services;

public interface IFormsService
{
    Task<int[]> GetSessionFormIdsAsync(string sessionId);
    Task CacheFormForSessionAsync(Form form, string sessionId);
    Task SaveFormAsync(Form form, string? sessionId);
    Task<Form?> GetFormFromIdAsync(int formId);
    Task<List<Form>> GetFormsByIdsAsync(int[] formIds);
}

public class FormsService : IFormsService
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _sessionTimeout;
    private readonly AppDbContext _dbContext;

    public FormsService(IDistributedCache cache, IConfiguration configuration, AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache;
        _sessionTimeout = TimeSpan.FromMinutes(
            configuration.GetValue<int>("SessionTimeoutMinutes", 120));
    }

    public Task<int[]> GetSessionFormIdsAsync(string sessionId)
    {
        var sessionFormIds = _cache.GetString($"sessionForms:{sessionId}");
        if (sessionFormIds != null)
        {
            var sessionFormIdsArray = JsonSerializer.Deserialize<int[]>(sessionFormIds);
            return Task.FromResult(sessionFormIdsArray ?? Array.Empty<int>());
        }
        return Task.FromResult(Array.Empty<int>());
    }

    public async Task CacheFormForSessionAsync(Form form, string sessionId)
    {
        var sessionFormIds = await GetSessionFormIdsAsync(sessionId);
        var updatedFormIds = sessionFormIds.ToList();
        updatedFormIds.Add(form.Id);

        var sessionFormsJson = JsonSerializer.Serialize(updatedFormIds);
        await _cache.SetStringAsync($"sessionForms:{sessionId}", sessionFormsJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _sessionTimeout
        });
    }

    public async Task SaveFormAsync(Form form, string? sessionId)
    {
        if (form == null) throw new ArgumentNullException(nameof(form));

        // Cache the form for the session
        if (!string.IsNullOrEmpty(sessionId))
        {
            await CacheFormForSessionAsync(form, sessionId);
        }

        // Save the form to the database
        _dbContext.Forms.Add(form);
        await _dbContext.SaveChangesAsync();
    }
    

    public async Task<Form?> GetFormFromIdAsync(int formId)
    {
        return await _dbContext.Forms.FindAsync(formId);
    }

    public async Task<List<Form>> GetFormsByIdsAsync(int[] formIds)
    {
        if (formIds == null || formIds.Length == 0)
        {
            return new List<Form>();
        }

        return await _dbContext.Forms
            .Where(f => formIds.Contains(f.Id))
            .ToListAsync();
    }
}