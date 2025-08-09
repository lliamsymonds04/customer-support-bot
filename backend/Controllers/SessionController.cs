using Microsoft.AspNetCore.Mvc;
using SupportBot.Services;

namespace SupportBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    // Controller actions go here
    private readonly ISessionManager _sessionManager;

    public SessionController(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetSession()
    {
        var sessionId = _sessionManager.GenerateSessionId();
        var session = await _sessionManager.GetOrCreateSessionAsync(sessionId);

        if (session == null)
        {
            return NotFound();
        }
        return Ok(session);
    }
}