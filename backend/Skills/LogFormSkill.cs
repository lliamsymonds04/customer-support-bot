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
    private readonly ISessionManager _sessionManager;
    private readonly IAuthService _authService;

    public LogFormSkill(IFormsService formsService, IHubContext<FormsHub> formsHub, ISessionManager sessionManager, IAuthService authService)
    {
        _formsService = formsService;
        _formsHub = formsHub;
        _sessionManager = sessionManager;
        _authService = authService;
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
            var session = await _sessionManager.GetOrCreateSessionAsync(sessionId);
            int? userId = null;
            try
            {
                var token = _authService.GetUserJwtToken();
                userId = _authService.GetUserIdByJwt(token);
                Console.WriteLine($"User ID from JWT: {userId}");
            }
            catch (Exception ex)
            {
                // Handle token parsing errors
                Console.WriteLine($"Error parsing JWT: {ex.Message}");
            }

            var form = new Form
            {
                Description = description,
                category = category,
                urgency = urgency,
                CreatedAt = DateTimeOffset.UtcNow,
                UserId = userId
            };

            await _formsService.SaveFormAsync(form, sessionId);

            await _formsHub.Clients.Group(sessionId).SendAsync("ReceiveUserForm", form);

            return "Form submitted successfully.";
        }
        catch (Exception ex)
        {
            return $"Error submitting form: {ex.Message}";
        }
    }
}