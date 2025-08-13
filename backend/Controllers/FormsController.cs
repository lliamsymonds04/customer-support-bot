using Microsoft.AspNetCore.Mvc;
using SupportBot.Models;
using SupportBot.Services;

namespace SupportBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormsController : ControllerBase
{
    private readonly IFormsService _formService;

    public FormsController(IFormsService formsService)
    {
        _formService = formsService;
    }

    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetFormsForSession(string sessionId)
    {
        var formIds = await _formService.GetSessionFormIdsAsync(sessionId);
        Console.WriteLine($"Form IDs for session {sessionId}: {string.Join(", ", formIds)}");
        var forms = await _formService.GetFormsByIdsAsync(formIds);

        return Ok(forms);
    }
}