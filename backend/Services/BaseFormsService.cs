using Microsoft.Extensions.Caching.Distributed;
using SupportBot.Models;
using System.Text.Json;
using SupportBot.Hubs;
using Microsoft.AspNetCore.SignalR;
using SupportBot.Dtos;

namespace SupportBot.Services;

public abstract class BaseFormsService : IFormsService
{
    protected readonly IDistributedCache _cache;
    protected readonly TimeSpan _sessionTimeout;
    protected readonly IHubContext<FormsHub> _formsHub;
    protected readonly IHubContext<AdminHub> _adminHub;

    protected BaseFormsService(IDistributedCache cache, IConfiguration configuration, IHubContext<FormsHub> formsHub, IHubContext<AdminHub> adminHub)
    {
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

    public async Task SendForm(Form form, string sessionId)
    {
        var formDto = await FormToDto(form);
        Console.WriteLine("Sending form via hub: " + form.Description + " to session: " + sessionId);
        await _formsHub.Clients.Group(sessionId).SendAsync("ReceiveUserForm", formDto);
        await _adminHub.Clients.All.SendAsync("AdminReceiveForm", formDto);
    }

    public async Task<FormDto> FormToDto(Form form)
    {
        if (form == null) throw new ArgumentNullException(nameof(form));

        var username = await GetUsernameByIdAsync(form.UserId);
        
        return new FormDto
        {
            Id = form.Id,
            Description = form.Description,
            Category = form.Category.ToString(),
            Urgency = form.Urgency.ToString(),
            State = form.State.ToString(),
            CreatedAt = form.CreatedAt,
            Username = username
        };
    }

    public async Task<FormDto[]> FormsToDtos(IEnumerable<Form> forms)
    {
        if (forms == null) throw new ArgumentNullException(nameof(forms));

        var dtos = new List<FormDto>();
        foreach (var form in forms)
        {
            dtos.Add(await FormToDto(form));
        }
        
        return dtos.ToArray();
    }

    public async Task<FormDto[]> GetFormDtosAsync(IEnumerable<int> formIds)
    {
        var forms = await GetFormsByIdsAsync(formIds.ToArray());
        return await FormsToDtos(forms);
    }

    public abstract Task SaveFormAsync(Form form, string? sessionId);
    public abstract Task<Form?> GetFormFromIdAsync(int formId);
    public abstract Task<List<Form>> GetFormsByIdsAsync(int[] formIds);
    public abstract Task<FormDto[]> GetFormsByCriteriaAsync(FormUrgency? urgency, FormState? state, FormCategory? category, string? keyword, int page, int pageSize);
    public abstract Task UpdateFormStateAsync(int formId, FormState newState);
    protected abstract Task<string?> GetUsernameByIdAsync(int? userId);
}
