using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SupportBot.Skills;
using SupportBot.Data;
using SupportBot.Services;
using SupportBot.Hubs;
using System.Text.Json.Serialization;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var isLocal = builder.Environment.IsDevelopment();

// Helper method to check database availability
static bool IsDatabaseAvailable(string? connectionString, ILogger logger)
{
    if (string.IsNullOrEmpty(connectionString))
    {
        logger.LogWarning("Database connection string is null or empty");
        return false;
    }

    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        command.ExecuteScalar();
        logger.LogInformation("✅ Database connection successful - using PostgreSQL");
        return true;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "❌ Database connection failed - falling back to in-memory mode");
        return false;
    }
}

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers()
  .AddJsonOptions(opts =>
  {
    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  });

// add Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var redisPassword = builder.Configuration["Redis:Password"];

var redisOptions = new StackExchange.Redis.ConfigurationOptions
{
    EndPoints = {redisConnectionString},
    Ssl = false,
    AbortOnConnectFail = false,
    ConnectRetry = 3
};

if (!string.IsNullOrEmpty(redisPassword))
{
    redisOptions.Password = redisPassword;
    redisOptions.User = "default";
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions = redisOptions;
});


// Build connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.Contains("{DatabasePassword}"))
{
    var databasePassword = builder.Configuration["DatabasePassword"];
    if (!string.IsNullOrEmpty(databasePassword))
    {
        connectionString = connectionString.Replace("{DatabasePassword}", databasePassword);
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    }
}

// Check if database is available
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
var useDatabaseMode = IsDatabaseAvailable(connectionString, logger);

// Always register in-memory data store as a singleton for fallback purposes
builder.Services.AddSingleton<InMemoryDataStore>();

if (useDatabaseMode)
{
    // Add Db context
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
    
    // Register database-backed services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IFormsService, FormsService>();
    
    logger.LogInformation("🗄️  Running in DATABASE mode with in-memory fallback");
    
    // Set the mode in HealthController
    HealthController.SetDatabaseMode(false, true, connectionString?.Split(';').FirstOrDefault(s => s.Contains("Database"))?.Split('=').LastOrDefault());
}
else
{
    // Register in-memory services
    builder.Services.AddSingleton<IAuthService, InMemoryAuthService>();
    builder.Services.AddSingleton<IFormsService, InMemoryFormsService>();
    
    logger.LogWarning("💾 Running in IN-MEMORY mode - data will not be persisted!");
    
    // Set the mode in HealthController
    HealthController.SetDatabaseMode(true, false, null);
}

builder.Services.AddHttpContextAccessor();

// Register skills
builder.Services.AddScoped<LogFormSkill>();

// Add session manager
builder.Services.AddScoped<ISessionManager, RedisSessionManager>();

// Skills
builder.Services.AddScoped<LogFormSkill>();

// Configure Groq API
builder.Services.AddSingleton<GroqConfig>(sp =>
{
    return new GroqConfig
    {
        ApiKey = builder.Configuration["Groq:ApiKey"] ?? throw new ArgumentNullException("Groq:ApiKey"),
        ModelName = builder.Configuration["Groq:ModelName"] ?? throw new ArgumentNullException("Groq:ModelName"),
        Endpoint = builder.Configuration["Groq:Endpoint"] ?? throw new ArgumentNullException("Groq:Endpoint")
    };
});

// Add Semantic Kernel service
builder.Services.AddScoped<ISemanticKernelService, SemanticKernelService>();

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecretKey = jwtSettings["SecretKey"];
if (string.IsNullOrEmpty(jwtSecretKey))
{
    throw new InvalidOperationException("JWT Secret Key is not configured.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

// Add Cors Policy
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEnd",
        builder => builder.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());
});


// Add SignalR
builder.Services.AddSignalR();

// Configure Kestrel to listen on port 5000 in development
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://localhost:5000");
}


var app = builder.Build();

// Cors
app.UseCors("AllowFrontEnd");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Jwt authentication
app.UseMiddleware<JwtFromCookieMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<FormsHub>("/hubs/forms");
app.MapHub<AdminHub>("/hubs/admin");

var endpointDataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
Console.WriteLine("📌 Registered endpoints:");
foreach (var endpoint in endpointDataSource.Endpoints)
{
    Console.WriteLine(endpoint.DisplayName);
}
app.MapGet("/test", () => "working");

app.Run();
