# Swagger UI via NSwag — Design Spec

**Date:** 2026-04-17
**Status:** Approved

## Overview

Add an interactive Swagger UI to the Borro API using NSwag, running alongside the existing Microsoft.AspNetCore.OpenApi pipeline. The UI is available in development only and includes a JWT bearer Authorize button for testing protected endpoints.

## Package

Add `NSwag.AspNetCore` to `backend/Borro.Api/Borro.Api.csproj`. The existing `Microsoft.AspNetCore.OpenApi` package remains untouched.

## Program.cs Changes

### Services (before `var app = builder.Build()`)

Register NSwag's document generation with:
- API title: `"Borro API"`
- API version: `"v1"`
- A JWT bearer security scheme named `"Bearer"`, so the Authorize button appears in Swagger UI

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

### Middleware (inside `if (app.Environment.IsDevelopment())`)

Add after the existing `app.MapOpenApi()` call:

```csharp
app.UseOpenApi();    // serves /swagger/v1/swagger.json
app.UseSwaggerUi(); // serves /swagger
```

## Endpoints Produced

| URL | Source | Purpose |
|-----|--------|---------|
| `/openapi/v1.json` | Microsoft.AspNetCore.OpenApi | Existing spec (unchanged) |
| `/swagger/v1/swagger.json` | NSwag | NSwag-generated spec |
| `/swagger` | NSwag SwaggerUi | Interactive API browser |

## Scope

- No changes to any endpoint files (`AuthEndpoints`, `ItemEndpoints`, `BlockedDatesEndpoints`, `HealthEndpoints`)
- No changes to the existing Microsoft OpenAPI configuration
- Dev-only — consistent with the existing `if (app.Environment.IsDevelopment())` gate
- No TypeScript client generation

## Out of Scope

- NSwag TypeScript client generation
- Production Swagger UI exposure
- XML doc comments on endpoints
