using SupportBot.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using SupportBot.Services;
using SupportBot.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace SupportBot.Skills;
public class LogFormSkill
{

    private readonly IFormsService _formsService;
    private readonly IHubContext<FormsHub> _formsHub;

    public LogFormSkill(IFormsService formsService, IHubContext<FormsHub> formsHub)
    {
        _formsService = formsService;
        _formsHub = formsHub;
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

            // await _formsHub.Clients.Group(sessionId).SendAsync("ReceiveUserForm", form);
            await _formsHub.Clients.All.SendAsync("ReceiveUserForm", form);

            return "Form submitted successfully.";
        }
        catch (Exception ex)
        {
            return $"Error submitting form: {ex.Message}";
        }
    }
}