# Phase 4: Trust, Security & Payments — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Stripe escrow (payment hold on approval, capture 24h after `StartDateUtc`), automated late-fee detection via background service, photo-based condition reports for pickup/dropoff, and two-way reviews after completion.

**Architecture:** Stripe integration lives behind `IPaymentService` in Application. Background service (`IHostedService`) polls for late/uncaptured bookings. `ConditionReport` gates the `PaymentHeld → Active` and `Active → Completed` transitions (frontend uploads photos; backend checks report exists before allowing transition). Reviews are created by both parties after `Completed`.

**Tech Stack:** .NET 9, MediatR 12.5, EF Core 9 + Npgsql, Stripe.net, AWSSDK.S3 (for condition report photos, reuses `IStorageService`), ASP.NET Core `IHostedService`, xUnit + Moq, React 18 + TypeScript + Tailwind

**Prerequisite:** Phase 3 plan fully implemented.

---

## File Map

**Create:**
| File | Responsibility |
|------|----------------|
| `backend/Borro.Domain/Entities/Review.cs` | Two-way rating after completed booking |
| `backend/Borro.Domain/Entities/ConditionReport.cs` | Pre/post photo checklist |
| `backend/Borro.Domain/Enums/ConditionReportType.cs` | Pickup / Dropoff |
| `backend/Borro.Application/Common/Interfaces/IPaymentService.cs` | Stripe abstraction |
| `backend/Borro.Application/Payments/Commands/HoldPayment/HoldPaymentCommand.cs` | Create payment hold |
| `backend/Borro.Application/Payments/Commands/HoldPayment/HoldPaymentCommandHandler.cs` | Handler |
| `backend/Borro.Application/Payments/Commands/CapturePayment/CapturePaymentCommand.cs` | Capture held funds |
| `backend/Borro.Application/Payments/Commands/CapturePayment/CapturePaymentCommandHandler.cs` | Handler |
| `backend/Borro.Application/ConditionReports/Commands/CreateReport/CreateConditionReportCommand.cs` | Upload checklist |
| `backend/Borro.Application/ConditionReports/Commands/CreateReport/CreateConditionReportCommandHandler.cs` | Handler |
| `backend/Borro.Application/Reviews/Commands/CreateReview/CreateReviewCommand.cs` | Submit review |
| `backend/Borro.Application/Reviews/Commands/CreateReview/CreateReviewCommandHandler.cs` | Handler |
| `backend/Borro.Application/Reviews/Queries/GetReviews/GetReviewsQuery.cs` | List reviews for a user |
| `backend/Borro.Application/Reviews/Queries/GetReviews/GetReviewsQueryHandler.cs` | Handler |
| `backend/Borro.Application/Reviews/DTOs/ReviewDto.cs` | Read-side DTO |
| `backend/Borro.Infrastructure/Services/StripePaymentService.cs` | Stripe implementation |
| `backend/Borro.Infrastructure/BackgroundServices/BookingEscrowWorker.cs` | Capture + late fee worker |
| `backend/Borro.Api/Endpoints/ConditionReportEndpoints.cs` | Condition report API |
| `backend/Borro.Api/Endpoints/ReviewEndpoints.cs` | Review API |
| `backend/Borro.Tests/Payments/HoldPaymentCommandHandlerTests.cs` | Payment handler tests |
| `backend/Borro.Tests/Reviews/CreateReviewCommandHandlerTests.cs` | Review handler tests |
| `frontend/src/features/bookings/HandoverChecklist.tsx` | Upload photos before status transition |
| `frontend/src/features/bookings/ReviewPrompt.tsx` | Rating form after completion |
| `frontend/src/features/bookings/conditionReportApi.ts` | Axios wrappers |
| `frontend/src/features/bookings/reviewApi.ts` | Axios wrappers |

**Modify:**
| File | Change |
|------|--------|
| `backend/Borro.Domain/Entities/Booking.cs` | Add `StripePaymentIntentId` field |
| `backend/Borro.Application/Common/Interfaces/IApplicationDbContext.cs` | Add `DbSet<Review>`, `DbSet<ConditionReport>` |
| `backend/Borro.Application/Bookings/Commands/TransitionBooking/TransitionBookingCommandHandler.cs` | Gate `PaymentHeld` → trigger hold; `Active` → check pickup report; `Completed` → check dropoff report |
| `backend/Borro.Infrastructure/Persistence/BorroDbContext.cs` | Add Review + ConditionReport config |
| `backend/Borro.Infrastructure/DependencyInjection.cs` | Register `IPaymentService`, `BookingEscrowWorker` |
| `backend/Borro.Infrastructure/Borro.Infrastructure.csproj` | Add `Stripe.net` |
| `backend/Borro.Api/appsettings.json` | Add `Stripe` config section |
| `backend/Borro.Api/Program.cs` | `MapConditionReportEndpoints()`, `MapReviewEndpoints()` |
| `frontend/src/features/bookings/BookingDetailPage.tsx` | Replace transition buttons with HandoverChecklist gating |

---

## Task 1: Domain Entities

**Files:**
- Create: `backend/Borro.Domain/Enums/ConditionReportType.cs`
- Create: `backend/Borro.Domain/Entities/ConditionReport.cs`
- Create: `backend/Borro.Domain/Entities/Review.cs`
- Modify: `backend/Borro.Domain/Entities/Booking.cs`

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

In `backend/Borro.Domain/Entities/Booking.cs`, add these lines after `TotalPrice`:

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

---

## Task 2: Application Interfaces & DTOs

**Files:**
- Modify: `backend/Borro.Application/Common/Interfaces/IApplicationDbContext.cs`
- Create: `backend/Borro.Application/Common/Interfaces/IPaymentService.cs`
- Create: `backend/Borro.Application/Reviews/DTOs/ReviewDto.cs`

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

---

## Task 3: HoldPayment Command + Tests

**Files:**
- Create: `backend/Borro.Application/Payments/Commands/HoldPayment/HoldPaymentCommand.cs`
- Create: `backend/Borro.Application/Payments/Commands/HoldPayment/HoldPaymentCommandHandler.cs`
- Create: `backend/Borro.Tests/Payments/HoldPaymentCommandHandlerTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
// backend/Borro.Tests/Payments/HoldPaymentCommandHandlerTests.cs
using Borro.Application.Payments.Commands.HoldPayment;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Borro.Tests.Payments;

public class HoldPaymentCommandHandlerTests
{
    private static BorroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<BorroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_ApprovedBooking_StoresPaymentIntentIdAndTransitionsToPaymentHeld()
    {
        await using var ctx = CreateContext();

        var lender = new User { Id = Guid.NewGuid(), Email = "l@t.com", FirstName = "L", LastName = "L", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var renter = new User { Id = Guid.NewGuid(), Email = "r@t.com", FirstName = "R", LastName = "R", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var item = new Item { Id = Guid.NewGuid(), OwnerId = lender.Id, Owner = lender, Title = "T", Description = "D", DailyPrice = 50m, Location = "L", Category = "C", Attributes = new ItemAttributes { Values = new() }, ImageUrls = new(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var booking = new Booking
        {
            Id = Guid.NewGuid(), ItemId = item.Id, Item = item,
            RenterId = renter.Id, Renter = renter,
            StartDateUtc = DateTime.UtcNow.AddDays(2),
            EndDateUtc = DateTime.UtcNow.AddDays(4),
            TotalPrice = 100m, Status = BookingStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        ctx.Users.AddRange(lender, renter);
        ctx.Items.Add(item);
        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var mockPayment = new Mock<IPaymentService>();
        mockPayment
            .Setup(p => p.CreatePaymentHoldAsync(100m, "aud", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("pi_test_123");

        var handler = new HoldPaymentCommandHandler(ctx, mockPayment.Object);
        var result = await handler.Handle(new HoldPaymentCommand(booking.Id, renter.Id), CancellationToken.None);

        Assert.Equal(BookingStatus.PaymentHeld, result.Status);
        var persisted = await ctx.Bookings.FindAsync(booking.Id);
        Assert.Equal("pi_test_123", persisted!.StripePaymentIntentId);
    }

    [Fact]
    public async Task Handle_BookingNotInApprovedState_Throws()
    {
        await using var ctx = CreateContext();
        var booking = new Booking { Id = Guid.NewGuid(), Status = BookingStatus.PendingApproval, ItemId = Guid.NewGuid(), RenterId = Guid.NewGuid(), TotalPrice = 50m, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var mockPayment = new Mock<IPaymentService>();
        var handler = new HoldPaymentCommandHandler(ctx, mockPayment.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new HoldPaymentCommand(booking.Id, booking.RenterId), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test — confirm build fails**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "HoldPaymentCommandHandlerTests" -v minimal 2>&1 | head -10
```

Expected: Build error.

- [ ] **Step 3: Create HoldPaymentCommand**

```csharp
// backend/Borro.Application/Payments/Commands/HoldPayment/HoldPaymentCommand.cs
using Borro.Application.Bookings.DTOs;
using MediatR;

namespace Borro.Application.Payments.Commands.HoldPayment;

public record HoldPaymentCommand(Guid BookingId, Guid RenterId) : IRequest<BookingDto>;
```

- [ ] **Step 4: Create HoldPaymentCommandHandler**

```csharp
// backend/Borro.Application/Payments/Commands/HoldPayment/HoldPaymentCommandHandler.cs
using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Application.Bookings.DTOs;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Payments.Commands.HoldPayment;

public class HoldPaymentCommandHandler : IRequestHandler<HoldPaymentCommand, BookingDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPaymentService _payment;

    public HoldPaymentCommandHandler(IApplicationDbContext db, IPaymentService payment)
    {
        _db = db;
        _payment = payment;
    }

    public async Task<BookingDto> Handle(HoldPaymentCommand cmd, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item).ThenInclude(i => i.Owner)
            .Include(b => b.Renter)
            .FirstOrDefaultAsync(b => b.Id == cmd.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {cmd.BookingId} not found.");

        if (booking.Status != BookingStatus.Approved)
            throw new InvalidOperationException("Only approved bookings can have payment held.");

        if (booking.RenterId != cmd.RenterId)
            throw new UnauthorizedAccessException("Only the renter can confirm payment.");

        var description = $"Borro rental: {booking.Item.Title} ({booking.Id})";
        var intentId = await _payment.CreatePaymentHoldAsync(booking.TotalPrice, "aud", description, ct);

        booking.StripePaymentIntentId = intentId;
        booking.Status = BookingStatus.PaymentHeld;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return CreateBookingCommandHandler.ToDto(booking, booking.Item, booking.Renter);
    }
}
```

- [ ] **Step 5: Run tests — both should pass**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "HoldPaymentCommandHandlerTests" -v minimal
```

Expected: `Passed: 2`

- [ ] **Step 6: Commit**

```bash
git add backend/Borro.Application/Payments/Commands/HoldPayment/ backend/Borro.Tests/Payments/
git commit -m "feat: add HoldPaymentCommand that creates Stripe hold and transitions to PaymentHeld"
```

---

## Task 4: CapturePayment Command

**Files:**
- Create: `backend/Borro.Application/Payments/Commands/CapturePayment/CapturePaymentCommand.cs`
- Create: `backend/Borro.Application/Payments/Commands/CapturePayment/CapturePaymentCommandHandler.cs`

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

---

## Task 5: ConditionReport & Review Commands + Tests

**Files:**
- Create all condition report and review command/query files
- Create: `backend/Borro.Tests/Reviews/CreateReviewCommandHandlerTests.cs`

- [ ] **Step 1: Write failing review test**

```csharp
// backend/Borro.Tests/Reviews/CreateReviewCommandHandlerTests.cs
using Borro.Application.Reviews.Commands.CreateReview;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Borro.Tests.Reviews;

public class CreateReviewCommandHandlerTests
{
    private static BorroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<BorroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_CompletedBooking_CreatesReview()
    {
        await using var ctx = CreateContext();
        var lender = new User { Id = Guid.NewGuid(), Email = "l@t.com", FirstName = "L", LastName = "L", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var renter = new User { Id = Guid.NewGuid(), Email = "r@t.com", FirstName = "R", LastName = "R", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var item = new Item { Id = Guid.NewGuid(), OwnerId = lender.Id, Owner = lender, Title = "T", Description = "D", DailyPrice = 10m, Location = "L", Category = "C", Attributes = new ItemAttributes { Values = new() }, ImageUrls = new(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var booking = new Booking { Id = Guid.NewGuid(), ItemId = item.Id, Item = item, RenterId = renter.Id, Renter = renter, StartDateUtc = DateTime.UtcNow.AddDays(-3), EndDateUtc = DateTime.UtcNow.AddDays(-1), TotalPrice = 30m, Status = BookingStatus.Completed, CreatedAtUtc = DateTime.UtcNow.AddDays(-5), UpdatedAtUtc = DateTime.UtcNow };
        ctx.Users.AddRange(lender, renter);
        ctx.Items.Add(item);
        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateReviewCommandHandler(ctx);
        var result = await handler.Handle(
            new CreateReviewCommand(booking.Id, renter.Id, lender.Id, 5, "Great lender!"),
            CancellationToken.None);

        Assert.Equal(5, result.Rating);
        Assert.Equal(lender.Id, result.TargetUserId);
        Assert.Single(ctx.Reviews);
    }

    [Fact]
    public async Task Handle_NonCompletedBooking_Throws()
    {
        await using var ctx = CreateContext();
        var booking = new Booking { Id = Guid.NewGuid(), Status = BookingStatus.Active, ItemId = Guid.NewGuid(), RenterId = Guid.NewGuid(), TotalPrice = 10m, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateReviewCommandHandler(ctx);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new CreateReviewCommand(booking.Id, booking.RenterId, Guid.NewGuid(), 4, "ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateReview_Throws()
    {
        await using var ctx = CreateContext();
        var lender = new User { Id = Guid.NewGuid(), Email = "l@t.com", FirstName = "L", LastName = "L", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var renter = new User { Id = Guid.NewGuid(), Email = "r@t.com", FirstName = "R", LastName = "R", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var item = new Item { Id = Guid.NewGuid(), OwnerId = lender.Id, Owner = lender, Title = "T", Description = "D", DailyPrice = 10m, Location = "L", Category = "C", Attributes = new ItemAttributes { Values = new() }, ImageUrls = new(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var booking = new Booking { Id = Guid.NewGuid(), ItemId = item.Id, Item = item, RenterId = renter.Id, Renter = renter, StartDateUtc = DateTime.UtcNow.AddDays(-3), EndDateUtc = DateTime.UtcNow.AddDays(-1), TotalPrice = 30m, Status = BookingStatus.Completed, CreatedAtUtc = DateTime.UtcNow.AddDays(-5), UpdatedAtUtc = DateTime.UtcNow };
        var existingReview = new Review { Id = Guid.NewGuid(), BookingId = booking.Id, Booking = booking, ReviewerId = renter.Id, Reviewer = renter, TargetUserId = lender.Id, Target = lender, Rating = 4, Comment = "Good", CreatedAtUtc = DateTime.UtcNow };
        ctx.Users.AddRange(lender, renter);
        ctx.Items.Add(item);
        ctx.Bookings.Add(booking);
        ctx.Reviews.Add(existingReview);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateReviewCommandHandler(ctx);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new CreateReviewCommand(booking.Id, renter.Id, lender.Id, 5, "Updated"), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test — confirm build fails**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "CreateReviewCommandHandlerTests" -v minimal 2>&1 | head -10
```

Expected: Build error.

- [ ] **Step 3: CreateConditionReportCommand**

```csharp
// backend/Borro.Application/ConditionReports/Commands/CreateReport/CreateConditionReportCommand.cs
using Borro.Domain.Enums;
using MediatR;

namespace Borro.Application.ConditionReports.Commands.CreateReport;

public record CreateConditionReportCommand(
    Guid BookingId,
    Guid SubmittedByUserId,
    ConditionReportType Type,
    List<Stream> PhotoStreams,
    List<string> FileNames,
    List<string> ContentTypes,
    string Notes
) : IRequest<Guid>;
```

- [ ] **Step 4: CreateConditionReportCommandHandler**

```csharp
// backend/Borro.Application/ConditionReports/Commands/CreateReport/CreateConditionReportCommandHandler.cs
using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.ConditionReports.Commands.CreateReport;

public class CreateConditionReportCommandHandler : IRequestHandler<CreateConditionReportCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IStorageService _storage;

    public CreateConditionReportCommandHandler(IApplicationDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Guid> Handle(CreateConditionReportCommand cmd, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == cmd.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {cmd.BookingId} not found.");

        var isParticipant = booking.RenterId == cmd.SubmittedByUserId
                         || booking.Item.OwnerId == cmd.SubmittedByUserId;
        if (!isParticipant)
            throw new UnauthorizedAccessException("Only booking participants can submit condition reports.");

        var urls = new List<string>();
        for (var i = 0; i < cmd.PhotoStreams.Count; i++)
        {
            var uniqueName = $"reports/{cmd.BookingId}/{cmd.Type}/{Guid.NewGuid()}_{cmd.FileNames[i]}";
            var url = await _storage.UploadFileAsync(cmd.PhotoStreams[i], uniqueName, cmd.ContentTypes[i], ct);
            urls.Add(url);
        }

        var report = new ConditionReport
        {
            Id = Guid.NewGuid(),
            BookingId = cmd.BookingId,
            SubmittedByUserId = cmd.SubmittedByUserId,
            Type = cmd.Type,
            PhotoUrls = urls,
            Notes = cmd.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ConditionReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return report.Id;
    }
}
```

- [ ] **Step 5: CreateReviewCommand**

```csharp
// backend/Borro.Application/Reviews/Commands/CreateReview/CreateReviewCommand.cs
using Borro.Application.Reviews.DTOs;
using MediatR;

namespace Borro.Application.Reviews.Commands.CreateReview;

public record CreateReviewCommand(
    Guid BookingId,
    Guid ReviewerId,
    Guid TargetUserId,
    int Rating,
    string Comment
) : IRequest<ReviewDto>;
```

- [ ] **Step 6: CreateReviewCommandHandler**

```csharp
// backend/Borro.Application/Reviews/Commands/CreateReview/CreateReviewCommandHandler.cs
using Borro.Application.Common.Interfaces;
using Borro.Application.Reviews.DTOs;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    private readonly IApplicationDbContext _db;

    public CreateReviewCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ReviewDto> Handle(CreateReviewCommand cmd, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == cmd.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {cmd.BookingId} not found.");

        if (booking.Status != BookingStatus.Completed)
            throw new InvalidOperationException("Reviews can only be submitted for completed bookings.");

        var isParticipant = booking.RenterId == cmd.ReviewerId || booking.Item.OwnerId == cmd.ReviewerId;
        if (!isParticipant)
            throw new UnauthorizedAccessException("Only booking participants can leave reviews.");

        var alreadyReviewed = await _db.Reviews
            .AnyAsync(r => r.BookingId == cmd.BookingId && r.ReviewerId == cmd.ReviewerId, ct);
        if (alreadyReviewed)
            throw new InvalidOperationException("You have already reviewed this booking.");

        if (cmd.Rating is < 1 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");

        var reviewer = await _db.Users.FirstAsync(u => u.Id == cmd.ReviewerId, ct);
        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == cmd.TargetUserId, ct)
            ?? throw new InvalidOperationException($"Target user {cmd.TargetUserId} not found.");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            BookingId = cmd.BookingId,
            ReviewerId = cmd.ReviewerId,
            TargetUserId = cmd.TargetUserId,
            Rating = cmd.Rating,
            Comment = cmd.Comment,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);

        return new ReviewDto(review.Id, review.BookingId, review.ReviewerId,
            $"{reviewer.FirstName} {reviewer.LastName}", review.TargetUserId,
            $"{target.FirstName} {target.LastName}", review.Rating, review.Comment, review.CreatedAtUtc);
    }
}
```

- [ ] **Step 7: GetReviewsQuery + Handler**

```csharp
// backend/Borro.Application/Reviews/Queries/GetReviews/GetReviewsQuery.cs
using Borro.Application.Reviews.DTOs;
using MediatR;

namespace Borro.Application.Reviews.Queries.GetReviews;

public record GetReviewsQuery(Guid TargetUserId) : IRequest<List<ReviewDto>>;
```

```csharp
// backend/Borro.Application/Reviews/Queries/GetReviews/GetReviewsQueryHandler.cs
using Borro.Application.Common.Interfaces;
using Borro.Application.Reviews.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Reviews.Queries.GetReviews;

public class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, List<ReviewDto>>
{
    private readonly IApplicationDbContext _db;
    public GetReviewsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ReviewDto>> Handle(GetReviewsQuery q, CancellationToken ct)
    {
        var reviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Target)
            .Where(r => r.TargetUserId == q.TargetUserId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        return reviews.Select(r => new ReviewDto(
            r.Id, r.BookingId, r.ReviewerId,
            $"{r.Reviewer.FirstName} {r.Reviewer.LastName}",
            r.TargetUserId, $"{r.Target.FirstName} {r.Target.LastName}",
            r.Rating, r.Comment, r.CreatedAtUtc
        )).ToList();
    }
}
```

- [ ] **Step 8: Run review tests — all 3 should pass**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "CreateReviewCommandHandlerTests" -v minimal
```

Expected: `Passed: 3`

- [ ] **Step 9: Commit**

```bash
git add backend/Borro.Application/ConditionReports/ backend/Borro.Application/Reviews/ backend/Borro.Tests/Reviews/
git commit -m "feat: add ConditionReport and Review commands/queries with tests"
```

---

## Task 6: Stripe Service + Background Escrow Worker

**Files:**
- Modify: `backend/Borro.Infrastructure/Borro.Infrastructure.csproj`
- Create: `backend/Borro.Infrastructure/Services/StripePaymentService.cs`
- Create: `backend/Borro.Infrastructure/BackgroundServices/BookingEscrowWorker.cs`
- Modify: `backend/Borro.Infrastructure/DependencyInjection.cs`
- Modify: `backend/Borro.Api/appsettings.json`

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
/// 2. Flags Active bookings where EndDateUtc has passed (late return) — logs for now;
///    real late-fee charge can be added in a future iteration.
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

Add these lines inside `AddInfrastructure` (after existing service registrations):

```csharp
services.AddScoped<IPaymentService, StripePaymentService>();
services.AddHostedService<BookingEscrowWorker>();
```

Add using at top:
```csharp
using Borro.Infrastructure.BackgroundServices;
```

- [ ] **Step 6: Add Stripe config to appsettings.json**

In `backend/Borro.Api/appsettings.json`, add (before closing `}`):

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

---

## Task 7: Update BorroDbContext + EF Migration

**Files:**
- Modify: `backend/Borro.Infrastructure/Persistence/BorroDbContext.cs`

- [ ] **Step 1: Add ConditionReport + Review + StripePaymentIntentId to BorroDbContext**

Add DbSets (after Messages):
```csharp
public DbSet<ConditionReport> ConditionReports => Set<ConditionReport>();
public DbSet<Review> Reviews => Set<Review>();
```

Add entity configurations inside `OnModelCreating` (after Message config):

```csharp
modelBuilder.Entity<ConditionReport>(entity =>
{
    entity.HasKey(r => r.Id);
    entity.Property(r => r.Notes).HasMaxLength(2000);
    entity.Property(r => r.Type).HasConversion<string>();
    entity.Property(r => r.PhotoUrls).HasColumnType("text[]");

    entity.HasOne(r => r.Booking).WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(r => r.SubmittedBy).WithMany().HasForeignKey(r => r.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<Review>(entity =>
{
    entity.HasKey(r => r.Id);
    entity.HasIndex(r => new { r.BookingId, r.ReviewerId }).IsUnique();
    entity.Property(r => r.Comment).HasMaxLength(1000);

    entity.HasOne(r => r.Booking).WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(r => r.Reviewer).WithMany().HasForeignKey(r => r.ReviewerId).OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(r => r.Target).WithMany().HasForeignKey(r => r.TargetUserId).OnDelete(DeleteBehavior.Restrict);
});
```

- [ ] **Step 2: Add migration**

```bash
cd backend
dotnet ef migrations add Phase4_ReviewsAndConditionReports \
  --project Borro.Infrastructure \
  --startup-project Borro.Api
```

- [ ] **Step 3: Apply migration**

```bash
cd backend
dotnet ef database update \
  --project Borro.Infrastructure \
  --startup-project Borro.Api
```

Expected: `Done.`

- [ ] **Step 4: Run all tests**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj -v minimal
```

Expected: All passing (at minimum the 12+ tests from Phases 2/3/4).

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Infrastructure/Persistence/
git commit -m "feat: add ConditionReport/Review to BorroDbContext with Phase4 migration"
```

---

## Task 8: ConditionReport & Review Endpoints

**Files:**
- Create: `backend/Borro.Api/Endpoints/ConditionReportEndpoints.cs`
- Create: `backend/Borro.Api/Endpoints/ReviewEndpoints.cs`
- Modify: `backend/Borro.Api/Program.cs`

- [ ] **Step 1: Create ConditionReportEndpoints**

```csharp
// backend/Borro.Api/Endpoints/ConditionReportEndpoints.cs
using Borro.Application.ConditionReports.Commands.CreateReport;
using Borro.Domain.Enums;
using MediatR;
using System.Security.Claims;

namespace Borro.Api.Endpoints;

public static class ConditionReportEndpoints
{
    public static IEndpointRouteBuilder MapConditionReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/condition-reports").WithTags("ConditionReports").RequireAuthorization();

        // POST /api/condition-reports  (multipart/form-data with photos)
        group.MapPost("/", async (HttpRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            if (!request.Form.TryGetValue("bookingId", out var bookingIdStr)
                || !Guid.TryParse(bookingIdStr, out var bookingId))
                return Results.BadRequest(new { error = "bookingId is required." });

            if (!request.Form.TryGetValue("type", out var typeStr)
                || !Enum.TryParse<ConditionReportType>(typeStr, out var reportType))
                return Results.BadRequest(new { error = "type must be 'Pickup' or 'Dropoff'." });

            var notes = request.Form.TryGetValue("notes", out var n) ? n.ToString() : string.Empty;
            var files = request.Form.Files;

            if (files.Count == 0)
                return Results.BadRequest(new { error = "At least one photo is required." });

            var streams = files.Select(f => f.OpenReadStream()).ToList();
            var fileNames = files.Select(f => f.FileName).ToList();
            var contentTypes = files.Select(f => f.ContentType).ToList();

            try
            {
                var reportId = await mediator.Send(new CreateConditionReportCommand(
                    bookingId, userId, reportType, streams, fileNames, contentTypes, notes), ct);
                return Results.Created($"/api/condition-reports/{reportId}", new { id = reportId });
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            finally
            {
                foreach (var s in streams) await s.DisposeAsync();
            }
        });

        return app;
    }
}
```

- [ ] **Step 2: Create ReviewEndpoints**

```csharp
// backend/Borro.Api/Endpoints/ReviewEndpoints.cs
using Borro.Application.Reviews.Commands.CreateReview;
using Borro.Application.Reviews.Queries.GetReviews;
using MediatR;
using System.Security.Claims;

namespace Borro.Api.Endpoints;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reviews").WithTags("Reviews");

        // GET /api/reviews?userId=<guid>  — public
        group.MapGet("/", async (Guid userId, IMediator mediator, CancellationToken ct) =>
        {
            var reviews = await mediator.Send(new GetReviewsQuery(userId), ct);
            return Results.Ok(reviews);
        });

        // POST /api/reviews  (requires auth)
        group.MapPost("/", async (CreateReviewRequest req, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var reviewerId))
                return Results.Unauthorized();

            try
            {
                var review = await mediator.Send(
                    new CreateReviewCommand(req.BookingId, reviewerId, req.TargetUserId, req.Rating, req.Comment), ct);
                return Results.Created($"/api/reviews/{review.Id}", review);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        }).RequireAuthorization();

        return app;
    }

    private record CreateReviewRequest(Guid BookingId, Guid TargetUserId, int Rating, string Comment);
}
```

- [ ] **Step 3: Wire endpoints in Program.cs**

After `app.MapBookingEndpoints();`, add:
```csharp
app.MapConditionReportEndpoints();
app.MapReviewEndpoints();
```

Also add the `HoldPayment` endpoint to `BookingEndpoints.cs`. In `BookingEndpoints.cs`, add this route inside the group (after the PATCH status route):

```csharp
// POST /api/bookings/{id}/hold-payment  — renter confirms payment, Stripe hold created
group.MapPost("/{id:guid}/hold-payment", async (Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    try
    {
        var booking = await mediator.Send(new HoldPaymentCommand(id, userId.Value), ct);
        return Results.Ok(booking);
    }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (UnauthorizedAccessException) { return Results.Forbid(); }
});
```

Add using at the top of `BookingEndpoints.cs`:
```csharp
using Borro.Application.Payments.Commands.HoldPayment;
```

- [ ] **Step 4: Build full solution**

```bash
cd backend
dotnet build Borro.sln
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Api/
git commit -m "feat: add ConditionReport and Review endpoints, wire HoldPayment into BookingEndpoints"
```

---

## Task 9: Frontend — conditionReportApi.ts + reviewApi.ts

**Files:**
- Create: `frontend/src/features/bookings/conditionReportApi.ts`
- Create: `frontend/src/features/bookings/reviewApi.ts`

- [ ] **Step 1: Create conditionReportApi.ts**

```typescript
// frontend/src/features/bookings/conditionReportApi.ts
import apiClient from '../../lib/apiClient';

export const conditionReportApi = {
  create: (bookingId: string, type: 'Pickup' | 'Dropoff', photos: File[], notes: string) => {
    const form = new FormData();
    form.append('bookingId', bookingId);
    form.append('type', type);
    form.append('notes', notes);
    photos.forEach(f => form.append('files', f));
    return apiClient.post<{ id: string }>('/api/condition-reports', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};
```

- [ ] **Step 2: Create reviewApi.ts**

```typescript
// frontend/src/features/bookings/reviewApi.ts
import apiClient from '../../lib/apiClient';

export interface ReviewDto {
  id: string;
  bookingId: string;
  reviewerId: string;
  reviewerName: string;
  targetUserId: string;
  targetUserName: string;
  rating: number;
  comment: string;
  createdAtUtc: string;
}

export const reviewApi = {
  getForUser: (userId: string) =>
    apiClient.get<ReviewDto[]>('/api/reviews', { params: { userId } }),

  create: (bookingId: string, targetUserId: string, rating: number, comment: string) =>
    apiClient.post<ReviewDto>('/api/reviews', { bookingId, targetUserId, rating, comment }),
};
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/bookings/conditionReportApi.ts frontend/src/features/bookings/reviewApi.ts
git commit -m "feat: add conditionReportApi and reviewApi Axios wrappers"
```

---

## Task 10: Frontend — HandoverChecklist Component

**Files:**
- Create: `frontend/src/features/bookings/HandoverChecklist.tsx`

- [ ] **Step 1: Create HandoverChecklist**

```tsx
// frontend/src/features/bookings/HandoverChecklist.tsx
import { useState } from 'react';
import { conditionReportApi } from './conditionReportApi';

interface Props {
  bookingId: string;
  type: 'Pickup' | 'Dropoff';
  onComplete: () => void;
}

export function HandoverChecklist({ bookingId, type, onComplete }: Props) {
  const [photos, setPhotos] = useState<File[]>([]);
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (photos.length === 0) {
      setError('Please upload at least one photo.');
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await conditionReportApi.create(bookingId, type, photos, notes);
      onComplete();
    } catch {
      setError('Failed to submit checklist. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const label = type === 'Pickup' ? 'Pickup Checklist' : 'Drop-off Checklist';
  const description = type === 'Pickup'
    ? 'Upload photos of the item\'s condition before you take it. This protects you from false damage claims.'
    : 'Upload photos proving the item was returned in the agreed condition.';

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-surface rounded-2xl p-6 max-w-md w-full shadow-2xl">
        <h2 className="font-headline text-xl font-bold mb-2">{label}</h2>
        <p className="text-on-surface-variant text-sm mb-6">{description}</p>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-2 rounded-lg mb-4 text-sm">{error}</div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-bold mb-1">Photos (required)</label>
            <input
              type="file"
              accept="image/*"
              multiple
              required
              className="w-full"
              onChange={e => setPhotos(Array.from(e.target.files ?? []))}
            />
            {photos.length > 0 && (
              <p className="text-sm text-on-surface-variant mt-1">{photos.length} photo(s) selected</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-bold mb-1">Notes (optional)</label>
            <textarea
              rows={3}
              className="w-full border border-outline-variant rounded-lg px-4 py-2 text-sm"
              placeholder="Any notes about the item's condition..."
              value={notes}
              onChange={e => setNotes(e.target.value)}
            />
          </div>

          <button
            type="submit"
            disabled={submitting}
            className="w-full bg-primary text-on-primary rounded-full py-3 font-bold hover:opacity-90 transition-opacity disabled:opacity-50 border-none"
          >
            {submitting ? 'Submitting...' : `Submit ${label}`}
          </button>
        </form>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/features/bookings/HandoverChecklist.tsx
git commit -m "feat: add HandoverChecklist component for mandatory photo upload"
```

---

## Task 11: Frontend — ReviewPrompt Component

**Files:**
- Create: `frontend/src/features/bookings/ReviewPrompt.tsx`

- [ ] **Step 1: Create ReviewPrompt**

```tsx
// frontend/src/features/bookings/ReviewPrompt.tsx
import { useState } from 'react';
import { reviewApi } from './reviewApi';

interface Props {
  bookingId: string;
  targetUserId: string;
  targetName: string;
  onComplete: () => void;
  onSkip: () => void;
}

export function ReviewPrompt({ bookingId, targetUserId, targetName, onComplete, onSkip }: Props) {
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (rating === 0) { setError('Please select a rating.'); return; }
    setSubmitting(true);
    setError(null);
    try {
      await reviewApi.create(bookingId, targetUserId, rating, comment);
      onComplete();
    } catch {
      setError('Failed to submit review. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-surface rounded-2xl p-6 max-w-md w-full shadow-2xl">
        <h2 className="font-headline text-xl font-bold mb-2">Rate your experience</h2>
        <p className="text-on-surface-variant text-sm mb-6">How was your experience with {targetName}?</p>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-2 rounded-lg mb-4 text-sm">{error}</div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="flex gap-2">
            {[1, 2, 3, 4, 5].map(star => (
              <button
                key={star}
                type="button"
                onClick={() => setRating(star)}
                className={`text-3xl bg-transparent border-none p-0 cursor-pointer transition-transform active:scale-110 ${star <= rating ? 'text-yellow-400' : 'text-gray-300'}`}
              >
                ★
              </button>
            ))}
          </div>

          <div>
            <label className="block text-sm font-bold mb-1">Comment (optional)</label>
            <textarea
              rows={3}
              className="w-full border border-outline-variant rounded-lg px-4 py-2 text-sm"
              placeholder="Share your experience..."
              value={comment}
              onChange={e => setComment(e.target.value)}
            />
          </div>

          <div className="flex gap-3">
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 bg-primary text-on-primary rounded-full py-3 font-bold hover:opacity-90 transition-opacity disabled:opacity-50 border-none"
            >
              {submitting ? 'Submitting...' : 'Submit Review'}
            </button>
            <button
              type="button"
              onClick={onSkip}
              className="px-6 py-3 bg-surface-container border border-outline-variant rounded-full font-bold text-sm hover:bg-surface-container-high transition-colors border-none"
            >
              Skip
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/features/bookings/ReviewPrompt.tsx
git commit -m "feat: add ReviewPrompt star-rating component"
```

---

## Task 12: Update BookingDetailPage with Checklist Gating + Review Prompt

**Files:**
- Modify: `frontend/src/features/bookings/BookingDetailPage.tsx`

- [ ] **Step 1: Add checklist + review prompt to BookingDetailPage**

Add these imports at the top of `BookingDetailPage.tsx`:
```tsx
import { HandoverChecklist } from './HandoverChecklist';
import { ReviewPrompt } from './ReviewPrompt';
import { bookingApi } from './bookingApi';
```

Add state for checklist and review modals (inside the component, after existing state):
```tsx
const [showPickupChecklist, setShowPickupChecklist] = useState(false);
const [showDropoffChecklist, setShowDropoffChecklist] = useState(false);
const [showReviewPrompt, setShowReviewPrompt] = useState(false);
```

Replace the `Confirm Pickup` button (in the action buttons section) with:
```tsx
{isRenter && booking.status === 'PaymentHeld' && (
  <button
    onClick={() => setShowPickupChecklist(true)}
    className="bg-primary text-on-primary rounded-full px-6 py-3 font-bold border-none hover:opacity-90 active:scale-95"
  >
    Confirm Pickup
  </button>
)}
```

Replace the `Confirm Return` button with:
```tsx
{isRenter && booking.status === 'Active' && (
  <button
    onClick={() => setShowDropoffChecklist(true)}
    className="bg-primary text-on-primary rounded-full px-6 py-3 font-bold border-none hover:opacity-90 active:scale-95"
  >
    Confirm Return
  </button>
)}
```

Add the modal components and review prompt at the end of the return JSX (before the closing `</div>`):
```tsx
{showPickupChecklist && (
  <HandoverChecklist
    bookingId={booking.id}
    type="Pickup"
    onComplete={async () => {
      setShowPickupChecklist(false);
      await handleTransition('Active');
    }}
  />
)}

{showDropoffChecklist && (
  <HandoverChecklist
    bookingId={booking.id}
    type="Dropoff"
    onComplete={async () => {
      setShowDropoffChecklist(false);
      await handleTransition('Completed');
      setShowReviewPrompt(true);
    }}
  />
)}

{showReviewPrompt && booking && (
  <ReviewPrompt
    bookingId={booking.id}
    targetUserId={isRenter ? booking.lenderId : booking.renterId}
    targetName={isRenter ? booking.lenderName : booking.renterName}
    onComplete={() => setShowReviewPrompt(false)}
    onSkip={() => setShowReviewPrompt(false)}
  />
)}
```

- [ ] **Step 2: Build frontend**

```bash
cd frontend
npm run build 2>&1 | tail -20
```

Expected: `built in X.XXs` with no TypeScript errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/bookings/BookingDetailPage.tsx
git commit -m "feat: gate PaymentHeld→Active and Active→Completed transitions behind photo checklists; show review prompt on completion"
```

---

## Phase 4 Complete — MVP Done

At this point the full Borro MVP is implemented:

| Phase | Feature | Status |
|-------|---------|--------|
| 1 | Auth (JWT, Google OAuth), User model, Docker infra | ✅ Done before this plan |
| 2 | Dynamic listings (JSONB attributes), image upload, search | ✅ Plan done |
| 3 | Booking state machine, SignalR chat | ✅ Plan done |
| 4 | Stripe escrow, background capture, condition reports, two-way reviews | ✅ Plan done |

**Next steps before production:**
- Replace placeholder dates in `ItemDetailPage` Book button with a real date picker
- Add Stripe webhook endpoint to handle payment failures
- Configure proper Stripe secret key (production vs test)
- Add user profile page showing their reviews and listings
- Add `GET /api/auth/me` endpoint (noted in spec but not yet implemented)
