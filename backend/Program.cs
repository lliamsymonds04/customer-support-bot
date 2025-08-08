using Microsoft.EntityFrameworkCore;
using SupportBot.Skills;
using SupportBot.Data;
using SupportBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// add Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
});



// Build connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.Contains("{DatabasePassword}"))
{
    var databasePassword = builder.Configuration["DatabasePassword"] ?? throw new ArgumentNullException("DatabasePassword");
    connectionString = connectionString.Replace("{DatabasePassword}", databasePassword);
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}
else if (connectionString == null)
{
    throw new ArgumentNullException("DefaultConnection");
}

// Add Db context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register skills
builder.Services.AddScoped<LogFormSkill>();

// Add session manager
builder.Services.AddSingleton<ISessionManager, RedisSessionManager>();

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
// builder.Services.AddSingleton<ISemanticKernelService>(sp =>
// {
//     var config = new GroqConfig
//     {
//         ApiKey = builder.Configuration["Groq:ApiKey"] ?? throw new ArgumentNullException("Groq:ApiKey"),
//         ModelName = builder.Configuration["Groq:ModelName"] ?? throw new ArgumentNullException("Groq:ModelName"),
//         Endpoint = builder.Configuration["Groq:Endpoint"] ?? throw new ArgumentNullException("Groq:Endpoint")
//     };

//     var sessionManager = sp.GetRequiredService<ISessionManager>();
//     var logFormSkill = sp.GetRequiredService<LogFormSkill>();
//     return new SemanticKernelService(config, logFormSkill, sessionManager);
// });

// Add Cors Policy
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEnd",
        builder => builder.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapControllers();
app.Run();