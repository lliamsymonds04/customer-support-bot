var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddSingleton(sp =>
{
    var config = new GroqConfig
    {
        ApiKey = builder.Configuration["Groq:ApiKey"] ?? throw new ArgumentNullException("Groq:ApiKey"),
        ModelName = builder.Configuration["Groq:ModelName"] ?? throw new ArgumentNullException("Groq:ModelName"),
        Endpoint = builder.Configuration["Groq:Endpoint"] ?? throw new ArgumentNullException("Groq:Endpoint")
    };
    return new SemanticKernelService(config);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();