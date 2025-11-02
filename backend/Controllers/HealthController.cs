using Microsoft.AspNetCore.Mvc;
using SupportBot.Data;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public static bool IsInMemoryMode { get; private set; }
    public static bool IsDatabaseConnected { get; private set; }
    public static string? DatabaseInfo { get; private set; }

    public static void SetDatabaseMode(bool isInMemoryMode, bool isDatabaseConnected, string? databaseInfo)
    {
        IsInMemoryMode = isInMemoryMode;
        IsDatabaseConnected = isDatabaseConnected;
        DatabaseInfo = databaseInfo;
    }

    public HealthController(ILogger<HealthController> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            machine = Environment.MachineName,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("mode")]
    public IActionResult GetMode()
    {
        return Ok(new
        {
            mode = IsInMemoryMode ? "in-memory" : "database",
            databaseConnected = IsDatabaseConnected,
            databaseInfo = DatabaseInfo,
            warning = IsInMemoryMode ? "Data will not be persisted across restarts" : null,
            timestamp = DateTime.UtcNow
        });
    }
}