using Microsoft.AspNetCore.Mvc;
using SupportBot.Services;

namespace SupportBot.Controllers;

public class ChatRequest
{
    public string SessionId { get; set; } = string.Empty;
    public required string Prompt { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly ISessionManager _sessionManager;

    public ChatController(ISemanticKernelService semanticKernelService, ISessionManager sessionManager)
    {
        _semanticKernelService = semanticKernelService;
        _sessionManager = sessionManager;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        var response = await _semanticKernelService.ChatWithAgentAsync(request.Prompt, request.SessionId);
        return Ok(response);
    }
}