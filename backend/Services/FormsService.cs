using Microsoft.Extensions.Caching.Distributed;
using SupportBot.Data;
using SupportBot.Models;
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

public class FormsService : BaseFormsService
{
    private readonly AppDbContext _dbContext;

    public FormsService(IDistributedCache cache, IConfiguration configuration, AppDbContext dbContext, IHubContext<FormsHub> formsHub, IHubContext<AdminHub> adminHub)
        : base(cache, configuration, formsHub, adminHub)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    protected override async Task<string?> GetUsernameByIdAsync(int? userId)
    {
        if (!userId.HasValue) return null;
        
        var user = await _dbContext.Users.FindAsync(userId.Value);
        return user?.Username;
    }

    public override async Task SaveFormAsync(Form form, string? sessionId)
    {
        if (form == null) throw new ArgumentNullException(nameof(form));

        _dbContext.Forms.Add(form);
        await _dbContext.SaveChangesAsync();

        if (!string.IsNullOrEmpty(sessionId))
        {
            await CacheFormForSessionAsync(form, sessionId);
        }
    }

    public override async Task<Form?> GetFormFromIdAsync(int formId)
    {
        return await _dbContext.Forms.FindAsync(formId);
    }

    public override async Task<List<Form>> GetFormsByIdsAsync(int[] formIds)
    {
        if (formIds == null || formIds.Length == 0)
        {
            return new List<Form>();
        }

        return await _dbContext.Forms
            .Where(f => formIds.Contains(f.Id))
            .ToListAsync();
    }

    public override async Task<FormDto[]> GetFormsByCriteriaAsync(FormUrgency? urgency, FormState? state, FormCategory? category, string? keyword, int page, int pageSize)
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

    public override async Task UpdateFormStateAsync(int formId, FormState newState)
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