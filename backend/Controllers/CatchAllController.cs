using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("{*path}")]  // Catch-all
public class CatchAllController : ControllerBase
{
    [HttpGet]
    public IActionResult Get(string path)
    {
        return Ok(new { message = $"You hit {path}" });
    }
}