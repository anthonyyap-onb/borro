# ─── Build Stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first for layer-cached restore
COPY Borro.sln ./
COPY Borro.Domain/Borro.Domain.csproj Borro.Domain/
COPY Borro.Application/Borro.Application.csproj Borro.Application/
COPY Borro.Infrastructure/Borro.Infrastructure.csproj Borro.Infrastructure/
COPY Borro.Api/Borro.Api.csproj Borro.Api/
COPY Borro.Tests/Borro.Tests.csproj Borro.Tests/

RUN dotnet restore

# Copy the rest of the source
COPY . .

# Publish in Release mode
RUN dotnet publish Borro.Api/Borro.Api.csproj -c Release -o /app/publish --no-restore

# Build a self-contained migrations bundle so the runtime image needs no SDK.
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 9.* && \
    dotnet ef migrations bundle \
        --project Borro.Infrastructure/Borro.Infrastructure.csproj \
        --startup-project Borro.Api/Borro.Api.csproj \
        --configuration Release \
        --self-contained -r linux-x64 \
        -o /app/efbundle

# ─── Migrations Stage ──────────────────────────────────────────────────────────
# Tiny one-shot image that runs the pre-built EF migration bundle.
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0 AS migrations
WORKDIR /app
COPY --from=build /app/efbundle ./efbundle
COPY --from=build /src/Borro.Api/appsettings*.json ./
ENTRYPOINT ["./efbundle"]

# ─── Runtime Stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system --gid 1001 borro && \
    adduser --system --uid 1001 --ingroup borro borro

COPY --from=build --chown=borro:borro /app/publish .

USER borro

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Borro.Api.dll"]
