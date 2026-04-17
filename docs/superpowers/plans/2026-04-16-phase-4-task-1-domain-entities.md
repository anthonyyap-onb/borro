# Phase 4 Task 1: Domain Entities

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `ConditionReport`, `Review` domain entities, `ConditionReportType` enum, and `StripePaymentIntentId` field on `Booking`.

**Context:** Part of Phase 4 — Trust, Security & Payments. Prerequisite: Phase 3 fully implemented.

**Tech Stack:** .NET 9, EF Core 9 + Npgsql

---

## File Map

| Action | File |
|--------|------|
| Create | `backend/Borro.Domain/Enums/ConditionReportType.cs` |
| Create | `backend/Borro.Domain/Entities/ConditionReport.cs` |
| Create | `backend/Borro.Domain/Entities/Review.cs` |
| Modify | `backend/Borro.Domain/Entities/Booking.cs` |

---

- [ ] **Step 1: Create ConditionReportType enum**

```csharp
// backend/Borro.Domain/Enums/ConditionReportType.cs
namespace Borro.Domain.Enums;

public enum ConditionReportType
{
    Pickup,
    Dropoff
}
```

- [ ] **Step 2: Create ConditionReport entity**

```csharp
// backend/Borro.Domain/Entities/ConditionReport.cs
using Borro.Domain.Enums;

namespace Borro.Domain.Entities;

public class ConditionReport
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid SubmittedByUserId { get; set; }
    public User SubmittedBy { get; set; } = null!;
    public ConditionReportType Type { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
```

- [ ] **Step 3: Create Review entity**

```csharp
// backend/Borro.Domain/Entities/Review.cs
namespace Borro.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid ReviewerId { get; set; }
    public User Reviewer { get; set; } = null!;
    public Guid TargetUserId { get; set; }
    public User Target { get; set; } = null!;

    /// <summary>1 to 5.</summary>
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
```

- [ ] **Step 4: Add StripePaymentIntentId to Booking entity**

In `backend/Borro.Domain/Entities/Booking.cs`, add after `TotalPrice`:

```csharp
/// <summary>Set when renter confirms payment (PaymentHeld state).</summary>
public string? StripePaymentIntentId { get; set; }
```

- [ ] **Step 5: Build Domain**

```bash
cd backend
dotnet build Borro.Domain/Borro.Domain.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add backend/Borro.Domain/
git commit -m "feat: add ConditionReport, Review entities, StripePaymentIntentId on Booking"
```
