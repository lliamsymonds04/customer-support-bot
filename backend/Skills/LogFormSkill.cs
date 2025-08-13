using SupportBot.Models;
using SupportBot.Data;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using SupportBot.Services;

namespace SupportBot.Skills;
public class LogFormSkill
{

    private readonly IFormsService _formsService;

    public LogFormSkill(IFormsService formsService)
    {
        _formsService = formsService;
    }

    [KernelFunction("LogForm")]
    [Description("Log a form submission with sessionId, category, urgency, and description.")]
    public async Task<string> LogForm (
        [Description("The sessionId for the user")] string sessionId,
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

            Console.WriteLine($"Logging form for session {sessionId}: {description}, Category: {category}, Urgency: {urgency}");
            await _formsService.SaveFormAsync(form, sessionId);

            return "Form submitted successfully.";
        }
        catch (Exception ex)
        {
            return $"Error submitting form: {ex.Message}";
        }
    }
}