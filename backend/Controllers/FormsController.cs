using Microsoft.AspNetCore.Authorization;
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
        var formDtos = await _formService.GetFormDtosAsync(formIds);

        return Ok(formDtos);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetFormsForAdmin(FormUrgency? urgency, FormState? state, FormCategory? category, string? keyword, int page = 1, int pageSize = 10)
    {
        var forms = await _formService.GetFormsByCriteriaAsync(urgency, state, category, keyword, page, pageSize);
        return Ok(forms);
    }

    [HttpPut("{formId}/state")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFormState(int formId, [FromBody] FormState newState)
    {
        Console.WriteLine($"Updating form {formId} to state {newState}");
        try
        {
            Console.WriteLine("Testing");
            await _formService.UpdateFormStateAsync(formId, newState);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}