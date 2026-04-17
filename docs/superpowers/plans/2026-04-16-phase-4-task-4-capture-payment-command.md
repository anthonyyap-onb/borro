# Phase 4 Task 4: CapturePayment Command

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement `CapturePaymentCommand` + handler for the background escrow worker to call 24h after `StartDateUtc`.

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Tasks 1 & 2.

**Tech Stack:** .NET 9, MediatR 12.5, EF Core 9

---

## File Map

| Action | File |
|--------|------|
| Create | `backend/Borro.Application/Payments/Commands/CapturePayment/CapturePaymentCommand.cs` |
| Create | `backend/Borro.Application/Payments/Commands/CapturePayment/CapturePaymentCommandHandler.cs` |

---

- [ ] **Step 1: Create CapturePaymentCommand**

```csharp
// backend/Borro.Application/Payments/Commands/CapturePayment/CapturePaymentCommand.cs
using MediatR;

namespace Borro.Application.Payments.Commands.CapturePayment;

/// <summary>Called by background worker 24h after StartDateUtc.</summary>
public record CapturePaymentCommand(Guid BookingId) : IRequest;
```

- [ ] **Step 2: Create CapturePaymentCommandHandler**

```csharp
// backend/Borro.Application/Payments/Commands/CapturePayment/CapturePaymentCommandHandler.cs
using Borro.Application.Common.Interfaces;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Payments.Commands.CapturePayment;

public class CapturePaymentCommandHandler : IRequestHandler<CapturePaymentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPaymentService _payment;

    public CapturePaymentCommandHandler(IApplicationDbContext db, IPaymentService payment)
    {
        _db = db;
        _payment = payment;
    }

    public async Task Handle(CapturePaymentCommand cmd, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == cmd.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {cmd.BookingId} not found.");

        if (booking.Status != BookingStatus.Active)
            return; // Already completed/cancelled — no-op

        if (booking.StripePaymentIntentId is null)
            throw new InvalidOperationException($"Booking {cmd.BookingId} has no PaymentIntent to capture.");

        await _payment.CapturePaymentAsync(booking.StripePaymentIntentId, ct);
    }
}
```

- [ ] **Step 3: Build Application**

```bash
cd backend
dotnet build Borro.Application/Borro.Application.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/Borro.Application/Payments/Commands/CapturePayment/
git commit -m "feat: add CapturePaymentCommand for background escrow capture"
```
