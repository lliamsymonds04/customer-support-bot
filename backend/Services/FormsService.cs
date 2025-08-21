using Microsoft.Extensions.Caching.Distributed;
using SupportBot.Data;
using SupportBot.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SupportBot.Hubs;
using Microsoft.AspNetCore.SignalR;
using SupportBot.Dtos;

namespace SupportBot.Services;

public interface IFormsService
{
    Task<int[]> GetSessionFormIdsAsync(string sessionId);
    Task CacheFormForSessionAsync(Form form, string sessionId);
    Task SaveFormAsync(Form form, string? sessionId);
    Task<Form?> GetFormFromIdAsync(int formId);
    Task<List<Form>> GetFormsByIdsAsync(int[] formIds);
    Task SendForm(Form form, string sessionId);
    Task<FormDto[]> GetFormsByCriteriaAsync(FormUrgency? urgency, FormState? state, FormCategory? category, string? keyword, int page, int pageSize);
    Task<FormDto> FormToDto(Form form);
    Task<FormDto[]> FormsToDtos(IEnumerable<Form> forms);
    Task<FormDto[]> GetFormDtosAsync(IEnumerable<int> formIds);
    Task UpdateFormStateAsync(int formId, FormState newState);
}

public class FormsService : IFormsService
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _sessionTimeout;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<FormsHub> _formsHub;
    private readonly IHubContext<AdminHub> _adminHub;

    public FormsService(IDistributedCache cache, IConfiguration configuration, AppDbContext dbContext, IHubContext<FormsHub> formsHub, IHubContext<AdminHub> adminHub)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache;
        _sessionTimeout = TimeSpan.FromMinutes(
            configuration.GetValue<int>("SessionTimeoutMinutes", 120));
        _formsHub = formsHub;
        _adminHub = adminHub;
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

        // Save the form to the database
        _dbContext.Forms.Add(form);
        await _dbContext.SaveChangesAsync();

        // Cache the form for the session
        if (!string.IsNullOrEmpty(sessionId))
        {
            await CacheFormForSessionAsync(form, sessionId);
        }
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

    public async Task SendForm(Form form, string sessionId)
    {
        var formDto = FormToDto(form);
        await _formsHub.Clients.Group(sessionId).SendAsync("ReceiveUserForm", formDto);
        await _adminHub.Clients.All.SendAsync("AdminReceiveForm", formDto);
    }

    public async Task<FormDto[]> GetFormsByCriteriaAsync(FormUrgency? urgency, FormState? state, FormCategory? category, string? keyword, int page, int pageSize)
    {
        var query = _dbContext.Forms.AsQueryable();

        if (urgency.HasValue)
        {
            query = query.Where(f => f.Urgency == urgency.Value);
        }

        if (state.HasValue)
        {
            query = query.Where(f => f.State == state.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(f => f.Category == category.Value);
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(f => f.Description.Contains(keyword));
        }

        // sort by most recent
        query = query.OrderByDescending(f => f.CreatedAt);

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FormDto
            {
                Id = f.Id,
                Description = f.Description,
                Category = f.Category.ToString(),
                Urgency = f.Urgency.ToString(),
                State = f.State.ToString(),
                CreatedAt = f.CreatedAt,
                Username = f.User != null ? f.User.Username : null
            })
            .ToArrayAsync();
    }

    public async Task<FormDto> FormToDto(Form form)
    {
        if (form == null) throw new ArgumentNullException(nameof(form));

        var dto = await _dbContext.Forms
            .Where(f => f.Id == form.Id)
            .Select(f => new FormDto
            {
                Id = f.Id,
                Description = f.Description,
                Category = f.Category.ToString(),
                Urgency = f.Urgency.ToString(),
                State = f.State.ToString(),
                CreatedAt = f.CreatedAt,
                Username = f.User != null ? f.User.Username : null
            })
            .FirstOrDefaultAsync();
        return dto!;
    }

    public async Task<FormDto[]> FormsToDtos(IEnumerable<Form> forms)
    {
        if (forms == null) throw new ArgumentNullException(nameof(forms));

        var formIds = forms.Select(f => f.Id).ToArray();
        
        var dtos = await _dbContext.Forms
            .Where(f => formIds.Contains(f.Id))
            .Select(f => new FormDto
            {
                Id = f.Id,
                Description = f.Description,
                Category = f.Category.ToString(),
                Urgency = f.Urgency.ToString(),
                State = f.State.ToString(),
                CreatedAt = f.CreatedAt,
                Username = f.User != null ? f.User.Username : null
            })
            .ToArrayAsync();
            
        return dtos;
    }

    public async Task<FormDto[]> GetFormDtosAsync(IEnumerable<int> formIds)
    {
        return await _dbContext.Forms
            .Where(f => formIds.Contains(f.Id))
            .Select(f => new FormDto
            {
                Id = f.Id,
                Description = f.Description,
                Category = f.Category.ToString(),
                Urgency = f.Urgency.ToString(),
                State = f.State.ToString(),
                CreatedAt = f.CreatedAt,
                Username = f.User != null ? f.User.Username : null
            })
            .ToArrayAsync();
    }

    public async Task UpdateFormStateAsync(int formId, FormState newState)
    {
        var form = await _dbContext.Forms.FindAsync(formId);
        if (form == null)
        {
            throw new KeyNotFoundException($"Form with ID {formId} not found.");
        }

        form.State = newState;
        _dbContext.Forms.Update(form);
        await _dbContext.SaveChangesAsync();
    }
}