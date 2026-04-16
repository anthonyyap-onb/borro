using Borro.Api.Endpoints;
using Borro.Application;
using Borro.Infrastructure;
using Borro.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Core Services ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();

// ── Clean Architecture Layers ──────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicyName);

// ── Endpoints ──────────────────────────────────────────────────────────────────
app.MapHealthEndpoints();
app.MapItemEndpoints();

app.Run();
