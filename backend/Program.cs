using Microsoft.EntityFrameworkCore;
using SupportBot.Models;
using SupportBot.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ISemanticKernelService>(sp =>
{
    var config = new GroqConfig
    {
        ApiKey = builder.Configuration["Groq:ApiKey"] ?? throw new ArgumentNullException("Groq:ApiKey"),
        ModelName = builder.Configuration["Groq:ModelName"] ?? throw new ArgumentNullException("Groq:ModelName"),
        Endpoint = builder.Configuration["Groq:Endpoint"] ?? throw new ArgumentNullException("Groq:Endpoint")
    };
    return new SemanticKernelService(config);
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var sk = app.Services.GetRequiredService<ISemanticKernelService>();
var result = await sk.RunPromptAsync("Tell me a nerdy programming joke.");
Console.WriteLine(result);

await app.RunAsync();