# NSwag Swagger UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an interactive Swagger UI at `/swagger` (dev-only) via NSwag alongside the existing Microsoft OpenAPI pipeline, with JWT bearer Authorize support.

**Architecture:** NSwag.AspNetCore is added as a second OpenAPI pipeline. It registers its own document generator via `AddOpenApiDocument()` and serves the UI via `UseOpenApi()` / `UseSwaggerUi()`. The existing `Microsoft.AspNetCore.OpenApi` setup (`AddOpenApi` / `MapOpenApi`) is left completely untouched.

**Tech Stack:** NSwag.AspNetCore (latest stable), .NET 9, ASP.NET Core Minimal APIs

---

## File Map

| Action | File |
|--------|------|
| Modify | `backend/Borro.Api/Borro.Api.csproj` |
| Modify | `backend/Borro.Api/Program.cs` |

---

### Task 1: Add NSwag.AspNetCore package

**Files:**
- Modify: `backend/Borro.Api/Borro.Api.csproj`

- [ ] **Step 1: Add the NSwag.AspNetCore package reference**

Open `backend/Borro.Api/Borro.Api.csproj`. Add the following inside the existing `<ItemGroup>` that contains `PackageReference` entries:

```xml
<PackageReference Include="NSwag.AspNetCore" Version="14.*" />
```

The file should look like this after the edit:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.10" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.10" />
    <PackageReference Include="NSwag.AspNetCore" Version="14.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Borro.Application\Borro.Application.csproj" />
    <ProjectReference Include="..\Borro.Infrastructure\Borro.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Restore packages**

Run from `backend/`:

```bash
dotnet restore Borro.sln
```

Expected: output ends with `Restore complete.` and no errors.

- [ ] **Step 3: Commit**

```bash
git add backend/Borro.Api/Borro.Api.csproj
git commit -m "chore: add NSwag.AspNetCore package"
```

---

### Task 2: Register NSwag document generation and Swagger UI

**Files:**
- Modify: `backend/Borro.Api/Program.cs`

- [ ] **Step 1: Add NSwag service registration**

In `backend/Borro.Api/Program.cs`, add the following block immediately after the `builder.Services.AddOpenApi();` line (line 13):

```csharp
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
```

- [ ] **Step 2: Add NSwag middleware**

In `backend/Borro.Api/Program.cs`, inside the `if (app.Environment.IsDevelopment())` block, add two lines immediately after `app.MapOpenApi();`:

```csharp
app.UseOpenApi();    // serves /swagger/v1/swagger.json
app.UseSwaggerUi(); // serves /swagger
```

The complete `if` block should look like this:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseOpenApi();    // serves /swagger/v1/swagger.json
    app.UseSwaggerUi(); // serves /swagger
}
```

- [ ] **Step 3: Build to verify no compile errors**

Run from `backend/`:

```bash
dotnet build Borro.sln
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 4: Run the API and smoke-test the Swagger UI**

Run from `backend/`:

```bash
dotnet run --project Borro.Api
```

Then open a browser and navigate to:

- `http://localhost:<port>/swagger` — should show the NSwag Swagger UI with "Borro API" title and an "Authorize" button
- `http://localhost:<port>/swagger/v1/swagger.json` — should return a JSON OpenAPI document
- `http://localhost:<port>/openapi/v1.json` — should still return Microsoft's OpenAPI document (unchanged)

Click the **Authorize** button — it should prompt for a Bearer token.

- [ ] **Step 5: Stop the server and commit**

```bash
git add backend/Borro.Api/Program.cs
git commit -m "feat: add NSwag Swagger UI with JWT bearer support"
```

---

## Done

After Task 2, the Swagger UI is live at `/swagger` in development. Both OpenAPI specs coexist independently.
