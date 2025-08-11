using SupportBot.Models;
using SupportBot.Data;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SupportBot.Skills;
public class LogFormSkill
{

    private readonly AppDbContext _dbContext;

    public LogFormSkill(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [KernelFunction("LogForm")]
    [Description("Log a form submission with title, category, urgency, and description.")]
    public async Task<string> LogForm (
        [Description("The title of the form submission.")] string title,
        [Description("The description of the form submission.")] string description,
        [Description("The category of the form submission.")] FormCategory category = FormCategory.General,
        [Description("The urgency of the form submission.")] FormUrgency urgency = FormUrgency.Low
    )
    {
        try
        {
            var form = new Form
            {
                Description = description,
                category = category,
                urgency = urgency,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Forms.Add(form);
            await _dbContext.SaveChangesAsync();

            return "Form submitted successfully.";
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log the error)
            return $"Error submitting form: {ex.Message}";
        }
    }
}