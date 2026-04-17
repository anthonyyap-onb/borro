# Phase 4 Task 3: HoldPayment Command + Tests

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement `HoldPaymentCommand` + handler (TDD) that creates a Stripe payment hold and transitions the booking to `PaymentHeld`.

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Tasks 1 & 2.

**Tech Stack:** .NET 9, MediatR 12.5, EF Core 9, xUnit, Moq

---

## File Map

| Action | File |
|--------|------|
| Create | `backend/Borro.Application/Payments/Commands/HoldPayment/HoldPaymentCommand.cs` |
| Create | `backend/Borro.Application/Payments/Commands/HoldPayment/HoldPaymentCommandHandler.cs` |
| Create | `backend/Borro.Tests/Payments/HoldPaymentCommandHandlerTests.cs` |

---

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
