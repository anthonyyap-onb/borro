# Phase 4 Task 6: Stripe Service + Background Escrow Worker

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `Stripe.net` package, implement `StripePaymentService`, `BookingEscrowWorker` (IHostedService), register them in DI, and add Stripe config section to appsettings.

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Tasks 1, 2, 3, 4.

**Tech Stack:** .NET 9, Stripe.net 47.3.0, ASP.NET Core `IHostedService`, MediatR 12.5

---

## File Map

| Action | File |
|--------|------|
| Modify | `backend/Borro.Infrastructure/Borro.Infrastructure.csproj` |
| Create | `backend/Borro.Infrastructure/Services/StripePaymentService.cs` |
| Create | `backend/Borro.Infrastructure/BackgroundServices/BookingEscrowWorker.cs` |
| Modify | `backend/Borro.Infrastructure/DependencyInjection.cs` |
| Modify | `backend/Borro.Api/appsettings.json` |

---

- [ ] **Step 1: Add Stripe.net to Infrastructure**

In `backend/Borro.Infrastructure/Borro.Infrastructure.csproj`, add inside the existing PackageReferences `<ItemGroup>`:

```xml
<PackageReference Include="Stripe.net" Version="47.3.0" />
```

- [ ] **Step 2: dotnet restore**

```bash
cd backend
dotnet restore
```

Expected: `Restore succeeded.`

- [ ] **Step 3: Create StripePaymentService**

```csharp
// backend/Borro.Infrastructure/Services/StripePaymentService.cs
using Borro.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Borro.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    public StripePaymentService(IConfiguration config)
    {
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");
    }

    public async Task<string> CreatePaymentHoldAsync(
        decimal amount, string currency, string description, CancellationToken ct)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100), // Stripe uses cents
            Currency = currency,
            Description = description,
            CaptureMethod = "manual", // Hold without capturing
        };
        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options, cancellationToken: ct);
        return intent.Id;
    }

    public async Task CapturePaymentAsync(string paymentIntentId, CancellationToken ct)
    {
        var service = new PaymentIntentService();
        await service.CaptureAsync(paymentIntentId, cancellationToken: ct);
    }

    public async Task CancelPaymentAsync(string paymentIntentId, CancellationToken ct)
    {
        var service = new PaymentIntentService();
        await service.CancelAsync(paymentIntentId, cancellationToken: ct);
    }
}
```

- [ ] **Step 4: Create BookingEscrowWorker**

```csharp
// backend/Borro.Infrastructure/BackgroundServices/BookingEscrowWorker.cs
using Borro.Application.Payments.Commands.CapturePayment;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Borro.Infrastructure.BackgroundServices;

/// <summary>
/// Runs every 15 minutes.
/// 1. Captures payment for Active bookings where StartDateUtc + 24h has passed.
/// 2. Flags Active bookings where EndDateUtc has passed (late return) — logs for now.
/// </summary>
public class BookingEscrowWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingEscrowWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    public BookingEscrowWorker(IServiceScopeFactory scopeFactory, ILogger<BookingEscrowWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var now = DateTime.UtcNow;

        // 1. Capture payment: Active bookings 24h+ after StartDateUtc
        var toCapture = await db.Bookings
            .Where(b => b.Status == BookingStatus.Active
                     && b.StripePaymentIntentId != null
                     && b.StartDateUtc.AddHours(24) <= now)
            .ToListAsync(ct);

        foreach (var booking in toCapture)
        {
            try
            {
                await mediator.Send(new CapturePaymentCommand(booking.Id), ct);
                _logger.LogInformation("Captured payment for booking {BookingId}", booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture payment for booking {BookingId}", booking.Id);
            }
        }

        // 2. Flag late returns: Active bookings past EndDateUtc
        var lateBookings = await db.Bookings
            .Where(b => b.Status == BookingStatus.Active && b.EndDateUtc < now)
            .ToListAsync(ct);

        foreach (var booking in lateBookings)
        {
            _logger.LogWarning(
                "Booking {BookingId} is overdue — EndDateUtc {EndDate} has passed. Manual late-fee review required.",
                booking.Id, booking.EndDateUtc);
        }
    }
}
```

- [ ] **Step 5: Register services in DependencyInjection.cs**

Add these lines inside `AddInfrastructure` after existing service registrations:

```csharp
services.AddScoped<IPaymentService, StripePaymentService>();
services.AddHostedService<BookingEscrowWorker>();
```

Add using at top:
```csharp
using Borro.Infrastructure.BackgroundServices;
```

- [ ] **Step 6: Add Stripe config to appsettings.json**

In `backend/Borro.Api/appsettings.json`, add before closing `}`:

```json
"Stripe": {
  "SecretKey": "sk_test_REPLACE_WITH_YOUR_STRIPE_TEST_KEY"
}
```

- [ ] **Step 7: Build Infrastructure**

```bash
cd backend
dotnet build Borro.Infrastructure/Borro.Infrastructure.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add backend/Borro.Infrastructure/ backend/Borro.Api/appsettings.json
git commit -m "feat: add Stripe payment service and escrow background worker"
```
