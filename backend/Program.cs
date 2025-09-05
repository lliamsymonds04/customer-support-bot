using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SupportBot.Skills;
using SupportBot.Data;
using SupportBot.Services;
using SupportBot.Hubs;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var isLocal = builder.Environment.IsDevelopment();

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

// var redisOptions = new StackExchange.Redis.ConfigurationOptions
// {
//     EndPoints = { redisConnectionString },
//     AbortOnConnectFail = false,
// };
var redisOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
redisOptions.AbortOnConnectFail = false;

if (!string.IsNullOrEmpty(redisPassword))
{
    redisOptions.Password = redisPassword;
    // redisOptions.Ssl = true;
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

builder.Services.AddHttpContextAccessor();

// Register skills
builder.Services.AddScoped<LogFormSkill>();

// Add session manager
builder.Services.AddScoped<ISessionManager, RedisSessionManager>();

// Add form service
builder.Services.AddScoped<IFormsService, FormsService>();

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

// Add Auth Service
builder.Services.AddScoped<IAuthService, AuthService>();

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

app.Run();