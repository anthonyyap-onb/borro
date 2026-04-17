# Phase 4 Task 2: Application Interfaces & DTOs

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend `IApplicationDbContext` with `ConditionReport`/`Review` DbSets, create `IPaymentService` abstraction, and add `ReviewDto`.

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Task 1 (domain entities).

**Tech Stack:** .NET 9, MediatR 12.5, EF Core 9

---

## File Map

| Action | File |
|--------|------|
| Modify | `backend/Borro.Application/Common/Interfaces/IApplicationDbContext.cs` |
| Create | `backend/Borro.Application/Common/Interfaces/IPaymentService.cs` |
| Create | `backend/Borro.Application/Reviews/DTOs/ReviewDto.cs` |

---

- [ ] **Step 1: Extend IApplicationDbContext**

```csharp
// backend/Borro.Application/Common/Interfaces/IApplicationDbContext.cs
using Borro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Item> Items { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Message> Messages { get; }
    DbSet<ConditionReport> ConditionReports { get; }
    DbSet<Review> Reviews { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Create IPaymentService**

```csharp
// backend/Borro.Application/Common/Interfaces/IPaymentService.cs
namespace Borro.Application.Common.Interfaces;

public interface IPaymentService
{
    /// <summary>Creates a Stripe PaymentIntent in "manual" capture mode. Returns the PaymentIntent ID.</summary>
    Task<string> CreatePaymentHoldAsync(decimal amount, string currency, string description, CancellationToken ct);

    /// <summary>Captures a previously held PaymentIntent.</summary>
    Task CapturePaymentAsync(string paymentIntentId, CancellationToken ct);

    /// <summary>Cancels (releases) a PaymentIntent without charging.</summary>
    Task CancelPaymentAsync(string paymentIntentId, CancellationToken ct);
}
```

- [ ] **Step 3: Create ReviewDto**

```csharp
// backend/Borro.Application/Reviews/DTOs/ReviewDto.cs
namespace Borro.Application.Reviews.DTOs;

public record ReviewDto(
    Guid Id,
    Guid BookingId,
    Guid ReviewerId,
    string ReviewerName,
    Guid TargetUserId,
    string TargetUserName,
    int Rating,
    string Comment,
    DateTime CreatedAtUtc
);
```

- [ ] **Step 4: Build Application**

```bash
cd backend
dotnet build Borro.Application/Borro.Application.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Application/
git commit -m "feat: extend interfaces with ConditionReport/Review DbSets and IPaymentService"
```
