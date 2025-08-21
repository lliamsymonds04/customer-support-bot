using SupportBot.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using SupportBot.Services;

namespace SupportBot.Skills;
public class LogFormSkill
{

    private readonly IFormsService _formsService;
    private readonly ISessionManager _sessionManager;
    private readonly IAuthService _authService;

    public LogFormSkill(IFormsService formsService, ISessionManager sessionManager, IAuthService authService)
    {
        _formsService = formsService;
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
            }
            catch (Exception ex)
            {
                // Handle token parsing errors
                Console.WriteLine($"Error parsing JWT: {ex.Message}");
            }
            if (userId != null)
            {
                Console.WriteLine($"User {userId} submitted a form.");
            }

            var form = new Form
            {
                Description = description,
                Category = category,
                Urgency = urgency,
                CreatedAt = DateTimeOffset.UtcNow,
                UserId = userId
            };

            await _formsService.SaveFormAsync(form, sessionId);

            await _formsService.SendForm(form, sessionId);

            return "Form submitted successfully.";
        }
        catch (Exception ex)
        {
            return $"Error submitting form: {ex.Message}";
        }
    }
}