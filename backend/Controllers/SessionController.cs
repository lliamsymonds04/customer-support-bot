using Microsoft.AspNetCore.Mvc;
using SupportBot.Services;

namespace SupportBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    // Controller actions go here
    private readonly ISessionManager _sessionManager;
    private readonly IAuthService _authService;

    public SessionController(ISessionManager sessionManager, IAuthService authService)
    {
        _authService = authService;
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
        return Ok(session.Id);
    }

    [HttpGet("{sessionId}")]
    public async Task<IActionResult> GetSessionById(string sessionId)
    {
        var exists = await _sessionManager.SessionExistsAsync(sessionId);

        if (!exists)
        {
            return NotFound();
        }
        return Ok();
    }

    public class UserAddClass
    {
        public required string SessionId { get; set; }
        public required string Username { get; set; }
    }

    [HttpPost("/add-user")]
    public async Task<IActionResult> AddUserToSession([FromBody] UserAddClass userAdd)
    {
        var session = await _sessionManager.GetOrCreateSessionAsync(userAdd.SessionId);
        var token = _authService.GetUserJwtToken();
        var userId = _authService.GetUserIdByJwt(token);
        await _sessionManager.AddUserToSession(session, userId);
        return Ok();
    }
}