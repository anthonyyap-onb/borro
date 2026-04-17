using System.Text;
using Borro.Api.Endpoints;
using Borro.Application;
using Borro.Infrastructure;
using Borro.Infrastructure.Hubs;
using Borro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Core Services ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "Borro API";
    config.Version = "v1";
    config.AddSecurity("Bearer", new NSwag.OpenApiSecurityScheme
    {
        Type = NSwag.OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token."
    });
    config.OperationProcessors.Add(
        new NSwag.Generation.Processors.Security.OperationSecurityScopeProcessor("Bearer"));
});

// ── Clean Architecture Layers ──────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Authentication & Authorization ────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key 'Jwt:Key' is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "borro-api",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "borro-frontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
        // SignalR WebSocket connections cannot send Authorization headers,
        // so the JWT is passed as a query param ?access_token=...
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

// ── CORS ───────────────────────────────────────────────────────────────────────
const string CorsPolicyName = "BorroFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",   // Vite dev server (local)
                "http://frontend:5173",    // Docker service name
                "http://localhost:3000"    // fallback
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ── Run DB migrations & seed on startup ───────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// ── Middleware Pipeline ────────────────────────────────────────────────────────
app.UseCors(CorsPolicyName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseOpenApi();    // serves /swagger/v1/swagger.json
    app.UseSwaggerUi(); // serves /swagger
}

app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ──────────────────────────────────────────────────────────────────
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapItemEndpoints();
app.MapBlockedDatesEndpoints();
app.MapBookingEndpoints();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
