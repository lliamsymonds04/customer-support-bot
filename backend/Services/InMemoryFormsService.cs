using Microsoft.Extensions.Caching.Distributed;
using SupportBot.Models;
using SupportBot.Hubs;
using Microsoft.AspNetCore.SignalR;
using SupportBot.Dtos;

namespace SupportBot.Services;

public class InMemoryFormsService : BaseFormsService
{
    private readonly InMemoryDataStore _dataStore;

    public InMemoryFormsService(IDistributedCache cache, IConfiguration configuration, InMemoryDataStore dataStore, IHubContext<FormsHub> formsHub, IHubContext<AdminHub> adminHub)
        : base(cache, configuration, formsHub, adminHub)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
    }

    protected override Task<string?> GetUsernameByIdAsync(int? userId)
    {
        if (!userId.HasValue) return Task.FromResult<string?>(null);
        
        var user = _dataStore.GetUserById(userId.Value);
        return Task.FromResult(user?.Username);
    }

    public override async Task SaveFormAsync(Form form, string? sessionId)
    {
        if (form == null) throw new ArgumentNullException(nameof(form));

        form.Id = _dataStore.GetNextFormId();
        _dataStore.AddForm(form);

        if (!string.IsNullOrEmpty(sessionId))
        {
            await CacheFormForSessionAsync(form, sessionId);
        }
    }

    public override Task<Form?> GetFormFromIdAsync(int formId)
    {
        var form = _dataStore.GetFormById(formId);
        return Task.FromResult(form);
    }

    public override Task<List<Form>> GetFormsByIdsAsync(int[] formIds)
    {
        if (formIds == null || formIds.Length == 0)
        {
            return Task.FromResult(new List<Form>());
        }

        var forms = formIds
            .Select(id => _dataStore.GetFormById(id))
            .Where(f => f != null)
            .Cast<Form>()
            .ToList();

        return Task.FromResult(forms);
    }

    public override async Task<FormDto[]> GetFormsByCriteriaAsync(FormUrgency? urgency, FormState? state, FormCategory? category, string? keyword, int page, int pageSize)
    {
        var query = _dataStore.GetAllForms().AsEnumerable();

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
            query = query.Where(f => f.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        query = query.OrderByDescending(f => f.CreatedAt);

        var forms = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var results = new List<FormDto>();
        foreach (var form in forms)
        {
            results.Add(await FormToDto(form));
        }

        return results.ToArray();
    }

    public override Task UpdateFormStateAsync(int formId, FormState newState)
    {
        var form = _dataStore.GetFormById(formId);
        if (form == null)
        {
            throw new KeyNotFoundException($"Form with ID {formId} not found.");
        }

        form.State = newState;
        _dataStore.UpdateForm(form);
        
        return Task.CompletedTask;
    }
}
