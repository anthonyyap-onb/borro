# Phase 3: Booking State Machine & Real-Time Chat — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a strict booking lifecycle (PendingApproval → Approved → PaymentHeld → Active → Completed / Disputed) and real-time SignalR messaging between renters and lenders within each booking.

**Architecture:** Booking transitions enforced in `Application` command handlers — the API layer never transitions state directly. SignalR `ChatHub` broadcasts messages scoped to a `BookingId` group. Availability endpoint (stubbed in Phase 2) is filled in to return actually-booked dates.

**Tech Stack:** .NET 9, MediatR 12.5, EF Core 9 + Npgsql, SignalR (built-in ASP.NET Core), xUnit + Moq, React 18 + `@microsoft/signalr` npm package, TypeScript, Tailwind

**Prerequisite:** Phase 2 plan fully implemented.

---

## File Map

**Create:**
| File | Responsibility |
|------|----------------|
| `backend/Borro.Domain/Enums/BookingStatus.cs` | Booking state machine enum |
| `backend/Borro.Domain/Entities/Booking.cs` | Booking aggregate |
| `backend/Borro.Domain/Entities/Message.cs` | Chat message entity |
| `backend/Borro.Application/Bookings/DTOs/BookingDto.cs` | Read-side DTO |
| `backend/Borro.Application/Bookings/Commands/CreateBooking/CreateBookingCommand.cs` | Initiate booking |
| `backend/Borro.Application/Bookings/Commands/CreateBooking/CreateBookingCommandHandler.cs` | Handler |
| `backend/Borro.Application/Bookings/Commands/TransitionBooking/TransitionBookingCommand.cs` | State transition |
| `backend/Borro.Application/Bookings/Commands/TransitionBooking/TransitionBookingCommandHandler.cs` | Handler |
| `backend/Borro.Application/Bookings/Queries/GetBooking/GetBookingQuery.cs` | Get single booking |
| `backend/Borro.Application/Bookings/Queries/GetBooking/GetBookingQueryHandler.cs` | Handler |
| `backend/Borro.Application/Bookings/Queries/GetUserBookings/GetUserBookingsQuery.cs` | User's booking list |
| `backend/Borro.Application/Bookings/Queries/GetUserBookings/GetUserBookingsQueryHandler.cs` | Handler |
| `backend/Borro.Application/Chat/Commands/SendMessage/SendMessageCommand.cs` | Post chat message |
| `backend/Borro.Application/Chat/Commands/SendMessage/SendMessageCommandHandler.cs` | Handler |
| `backend/Borro.Application/Chat/Queries/GetMessages/GetMessagesQuery.cs` | Message history |
| `backend/Borro.Application/Chat/Queries/GetMessages/GetMessagesQueryHandler.cs` | Handler |
| `backend/Borro.Application/Chat/DTOs/MessageDto.cs` | Message DTO |
| `backend/Borro.Infrastructure/Hubs/ChatHub.cs` | SignalR hub |
| `backend/Borro.Api/Endpoints/BookingEndpoints.cs` | Booking API route group |
| `backend/Borro.Tests/Bookings/CreateBookingCommandHandlerTests.cs` | Tests |
| `backend/Borro.Tests/Bookings/TransitionBookingCommandHandlerTests.cs` | State machine tests |
| `frontend/src/features/bookings/bookingApi.ts` | Axios wrappers |
| `frontend/src/features/bookings/useChat.ts` | SignalR hook |
| `frontend/src/features/bookings/BookingDetailPage.tsx` | Status timeline + chat |

**Modify:**
| File | Change |
|------|--------|
| `backend/Borro.Application/Common/Interfaces/IApplicationDbContext.cs` | Add `DbSet<Booking>`, `DbSet<Message>` |
| `backend/Borro.Infrastructure/Persistence/BorroDbContext.cs` | Add Booking + Message config |
| `backend/Borro.Api/Endpoints/ItemEndpoints.cs` | Fill in `/availability` stub |
| `backend/Borro.Api/Program.cs` | `AddSignalR()`, `MapHub<ChatHub>`, `MapBookingEndpoints()` |
| `frontend/src/App.tsx` | Add `/bookings/:id` route |
| `frontend/src/features/items/ItemDetailPage.tsx` | Wire "Book" button to booking creation |

---

## Task 1: Domain — Booking & Message Entities

**Files:**
- Create: `backend/Borro.Domain/Enums/BookingStatus.cs`
- Create: `backend/Borro.Domain/Entities/Booking.cs`
- Create: `backend/Borro.Domain/Entities/Message.cs`

- [ ] **Step 1: Create BookingStatus enum**

```csharp
// backend/Borro.Domain/Enums/BookingStatus.cs
namespace Borro.Domain.Enums;

public enum BookingStatus
{
    PendingApproval,
    Approved,
    PaymentHeld,
    Active,
    Completed,
    Disputed,
    Cancelled
}
```

- [ ] **Step 2: Create Booking entity**

```csharp
// backend/Borro.Domain/Entities/Booking.cs
using Borro.Domain.Enums;

namespace Borro.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public Guid RenterId { get; set; }
    public User Renter { get; set; } = null!;

    /// <summary>All dates in UTC.</summary>
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
```

- [ ] **Step 3: Create Message entity**

```csharp
// backend/Borro.Domain/Entities/Message.cs
namespace Borro.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
```

- [ ] **Step 4: Build Domain**

```bash
cd backend
dotnet build Borro.Domain/Borro.Domain.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Domain/
git commit -m "feat: add Booking and Message entities with BookingStatus enum"
```

---

## Task 2: Extend IApplicationDbContext & Add DTOs

**Files:**
- Modify: `backend/Borro.Application/Common/Interfaces/IApplicationDbContext.cs`
- Create: `backend/Borro.Application/Bookings/DTOs/BookingDto.cs`
- Create: `backend/Borro.Application/Chat/DTOs/MessageDto.cs`

- [ ] **Step 1: Extend interface**

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
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Create BookingDto**

```csharp
// backend/Borro.Application/Bookings/DTOs/BookingDto.cs
using Borro.Domain.Enums;

namespace Borro.Application.Bookings.DTOs;

public record BookingDto(
    Guid Id,
    Guid ItemId,
    string ItemTitle,
    Guid RenterId,
    string RenterName,
    Guid LenderId,
    string LenderName,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    decimal TotalPrice,
    BookingStatus Status,
    DateTime CreatedAtUtc
);
```

- [ ] **Step 3: Create MessageDto**

```csharp
// backend/Borro.Application/Chat/DTOs/MessageDto.cs
namespace Borro.Application.Chat.DTOs;

public record MessageDto(
    Guid Id,
    Guid BookingId,
    Guid SenderId,
    string SenderName,
    string Content,
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
git commit -m "feat: extend IApplicationDbContext with Booking/Message, add BookingDto and MessageDto"
```

---

## Task 3: CreateBooking Command + Tests

**Files:**
- Create: `backend/Borro.Application/Bookings/Commands/CreateBooking/CreateBookingCommand.cs`
- Create: `backend/Borro.Application/Bookings/Commands/CreateBooking/CreateBookingCommandHandler.cs`
- Create: `backend/Borro.Tests/Bookings/CreateBookingCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// backend/Borro.Tests/Bookings/CreateBookingCommandHandlerTests.cs
using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Borro.Tests.Bookings;

public class CreateBookingCommandHandlerTests
{
    private static BorroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<BorroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (User lender, User renter, Item item) Seed(BorroDbContext ctx, bool instantBook)
    {
        var lender = new User { Id = Guid.NewGuid(), Email = "lender@t.com", FirstName = "Len", LastName = "Der", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var renter = new User { Id = Guid.NewGuid(), Email = "renter@t.com", FirstName = "Ren", LastName = "Ter", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var item = new Item
        {
            Id = Guid.NewGuid(), LenderId = lender.Id, Owner = lender,
            Title = "Drill", Description = "desc", DailyPrice = 20m,
            Location = "Portland", Category = "Tools",
            Attributes = new ItemAttributes { Values = new() },
            InstantBookEnabled = instantBook,
            ImageUrls = new(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        ctx.Users.AddRange(lender, renter);
        ctx.Items.Add(item);
        ctx.SaveChanges();
        return (lender, renter, item);
    }

    [Fact]
    public async Task Handle_NormalItem_CreatesPendingApprovalBooking()
    {
        await using var ctx = CreateContext();
        var (_, renter, item) = Seed(ctx, instantBook: false);
        var handler = new CreateBookingCommandHandler(ctx);
        var start = DateTime.UtcNow.Date.AddDays(2);
        var end = start.AddDays(3);

        var result = await handler.Handle(
            new CreateBookingCommand(item.Id, renter.Id, start, end), CancellationToken.None);

        Assert.Equal(BookingStatus.PendingApproval, result.Status);
        Assert.Equal(item.DailyPrice * 3, result.TotalPrice);
    }

    [Fact]
    public async Task Handle_InstantBookItem_CreatesApprovedBooking()
    {
        await using var ctx = CreateContext();
        var (_, renter, item) = Seed(ctx, instantBook: true);
        var handler = new CreateBookingCommandHandler(ctx);
        var start = DateTime.UtcNow.Date.AddDays(1);

        var result = await handler.Handle(
            new CreateBookingCommand(item.Id, renter.Id, start, start.AddDays(1)), CancellationToken.None);

        Assert.Equal(BookingStatus.Approved, result.Status);
    }

    [Fact]
    public async Task Handle_RenterIsOwner_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var (lender, _, item) = Seed(ctx, instantBook: false);
        var handler = new CreateBookingCommandHandler(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new CreateBookingCommand(item.Id, lender.Id, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2)),
                CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test — confirm it fails**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "CreateBookingCommandHandlerTests" -v minimal 2>&1 | head -10
```

Expected: Build error — `CreateBookingCommand` not found.

- [ ] **Step 3: Create CreateBookingCommand**

```csharp
// backend/Borro.Application/Bookings/Commands/CreateBooking/CreateBookingCommand.cs
using Borro.Application.Bookings.DTOs;
using MediatR;

namespace Borro.Application.Bookings.Commands.CreateBooking;

public record CreateBookingCommand(
    Guid ItemId,
    Guid RenterId,
    DateTime StartDateUtc,
    DateTime EndDateUtc
) : IRequest<BookingDto>;
```

- [ ] **Step 4: Create CreateBookingCommandHandler**

```csharp
// backend/Borro.Application/Bookings/Commands/CreateBooking/CreateBookingCommandHandler.cs
using Borro.Application.Bookings.DTOs;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDto>
{
    private readonly IApplicationDbContext _db;

    public CreateBookingCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<BookingDto> Handle(CreateBookingCommand cmd, CancellationToken ct)
    {
        var item = await _db.Items
            .Include(i => i.Owner)
            .FirstOrDefaultAsync(i => i.Id == cmd.ItemId, ct)
            ?? throw new InvalidOperationException($"Item {cmd.ItemId} not found.");

        if (item.LenderId == cmd.RenterId)
            throw new InvalidOperationException("You cannot book your own item.");

        var renter = await _db.Users.FirstOrDefaultAsync(u => u.Id == cmd.RenterId, ct)
            ?? throw new InvalidOperationException($"User {cmd.RenterId} not found.");

        var days = Math.Max(1, (int)(cmd.EndDateUtc.Date - cmd.StartDateUtc.Date).TotalDays);
        var status = item.InstantBookEnabled ? BookingStatus.Approved : BookingStatus.PendingApproval;

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ItemId = cmd.ItemId,
            RenterId = cmd.RenterId,
            StartDateUtc = cmd.StartDateUtc,
            EndDateUtc = cmd.EndDateUtc,
            TotalPrice = item.DailyPrice * days,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);

        return ToDto(booking, item, renter);
    }

    internal static BookingDto ToDto(Booking b, Item item, User renter) => new(
        Id: b.Id,
        ItemId: b.ItemId,
        ItemTitle: item.Title,
        RenterId: b.RenterId,
        RenterName: $"{renter.FirstName} {renter.LastName}",
        LenderId: item.LenderId,
        LenderName: $"{item.Owner.FirstName} {item.Owner.LastName}",
        StartDateUtc: b.StartDateUtc,
        EndDateUtc: b.EndDateUtc,
        TotalPrice: b.TotalPrice,
        Status: b.Status,
        CreatedAtUtc: b.CreatedAtUtc
    );
}
```

- [ ] **Step 5: Run tests — all 3 should pass**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "CreateBookingCommandHandlerTests" -v minimal
```

Expected: `Passed: 3`

- [ ] **Step 6: Commit**

```bash
git add backend/Borro.Application/Bookings/Commands/CreateBooking/ backend/Borro.Tests/Bookings/CreateBookingCommandHandlerTests.cs
git commit -m "feat: add CreateBookingCommand with instant-book support and tests"
```

---

## Task 4: TransitionBooking Command + State Machine Tests

**Files:**
- Create: `backend/Borro.Application/Bookings/Commands/TransitionBooking/TransitionBookingCommand.cs`
- Create: `backend/Borro.Application/Bookings/Commands/TransitionBooking/TransitionBookingCommandHandler.cs`
- Create: `backend/Borro.Tests/Bookings/TransitionBookingCommandHandlerTests.cs`

- [ ] **Step 1: Write failing state machine tests**

```csharp
// backend/Borro.Tests/Bookings/TransitionBookingCommandHandlerTests.cs
using Borro.Application.Bookings.Commands.TransitionBooking;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Borro.Tests.Bookings;

public class TransitionBookingCommandHandlerTests
{
    private static BorroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<BorroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(BorroDbContext ctx, Booking booking, User lender, User renter)> SeedWithBooking(
        BookingStatus status)
    {
        var ctx = CreateContext();
        var lender = new User { Id = Guid.NewGuid(), Email = "l@t.com", FirstName = "L", LastName = "L", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var renter = new User { Id = Guid.NewGuid(), Email = "r@t.com", FirstName = "R", LastName = "R", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var item = new Item { Id = Guid.NewGuid(), LenderId = lender.Id, Owner = lender, Title = "T", Description = "D", DailyPrice = 10m, Location = "L", Category = "C", Attributes = new ItemAttributes { Values = new() }, ImageUrls = new(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var booking = new Booking { Id = Guid.NewGuid(), ItemId = item.Id, Item = item, RenterId = renter.Id, Renter = renter, StartDateUtc = DateTime.UtcNow.AddDays(1), EndDateUtc = DateTime.UtcNow.AddDays(3), TotalPrice = 20m, Status = status, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        ctx.Users.AddRange(lender, renter);
        ctx.Items.Add(item);
        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync(CancellationToken.None);
        return (ctx, booking, lender, renter);
    }

    [Fact]
    public async Task LenderCanApprovePendingBooking()
    {
        var (ctx, booking, lender, _) = await SeedWithBooking(BookingStatus.PendingApproval);
        var handler = new TransitionBookingCommandHandler(ctx);

        var result = await handler.Handle(
            new TransitionBookingCommand(booking.Id, lender.Id, BookingStatus.Approved), CancellationToken.None);

        Assert.Equal(BookingStatus.Approved, result.Status);
    }

    [Fact]
    public async Task RenterCannotApproveBooking()
    {
        var (ctx, booking, _, renter) = await SeedWithBooking(BookingStatus.PendingApproval);
        var handler = new TransitionBookingCommandHandler(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new TransitionBookingCommand(booking.Id, renter.Id, BookingStatus.Approved),
                CancellationToken.None));
    }

    [Fact]
    public async Task CannotSkipStateMachineSteps()
    {
        // PendingApproval → Active is an illegal jump (must go through Approved → PaymentHeld)
        var (ctx, booking, lender, _) = await SeedWithBooking(BookingStatus.PendingApproval);
        var handler = new TransitionBookingCommandHandler(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new TransitionBookingCommand(booking.Id, lender.Id, BookingStatus.Active),
                CancellationToken.None));
    }

    [Fact]
    public async Task LenderCanCancelPendingBooking()
    {
        var (ctx, booking, lender, _) = await SeedWithBooking(BookingStatus.PendingApproval);
        var handler = new TransitionBookingCommandHandler(ctx);

        var result = await handler.Handle(
            new TransitionBookingCommand(booking.Id, lender.Id, BookingStatus.Cancelled), CancellationToken.None);

        Assert.Equal(BookingStatus.Cancelled, result.Status);
    }
}
```

- [ ] **Step 2: Run test — confirm build fails**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "TransitionBookingCommandHandlerTests" -v minimal 2>&1 | head -10
```

Expected: Build error.

- [ ] **Step 3: Create TransitionBookingCommand**

```csharp
// backend/Borro.Application/Bookings/Commands/TransitionBooking/TransitionBookingCommand.cs
using Borro.Application.Bookings.DTOs;
using Borro.Domain.Enums;
using MediatR;

namespace Borro.Application.Bookings.Commands.TransitionBooking;

public record TransitionBookingCommand(
    Guid BookingId,
    Guid RequestingUserId,
    BookingStatus TargetStatus
) : IRequest<BookingDto>;
```

- [ ] **Step 4: Create TransitionBookingCommandHandler with strict state machine**

```csharp
// backend/Borro.Application/Bookings/Commands/TransitionBooking/TransitionBookingCommandHandler.cs
using Borro.Application.Bookings.DTOs;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Bookings.Commands.TransitionBooking;

public class TransitionBookingCommandHandler : IRequestHandler<TransitionBookingCommand, BookingDto>
{
    // Maps (currentStatus, targetStatus) → which role is allowed: "lender" | "renter" | "system"
    private static readonly Dictionary<(BookingStatus from, BookingStatus to), string> AllowedTransitions = new()
    {
        { (BookingStatus.PendingApproval, BookingStatus.Approved),    "lender" },
        { (BookingStatus.PendingApproval, BookingStatus.Cancelled),   "lender" },
        { (BookingStatus.Approved,        BookingStatus.PaymentHeld), "renter" },
        { (BookingStatus.Approved,        BookingStatus.Cancelled),   "lender" },
        { (BookingStatus.PaymentHeld,     BookingStatus.Active),      "renter" },  // after pickup checklist
        { (BookingStatus.Active,          BookingStatus.Completed),   "renter" },  // after dropoff checklist
        { (BookingStatus.Active,          BookingStatus.Disputed),    "renter" },
        { (BookingStatus.Active,          BookingStatus.Disputed),    "lender" },
    };

    private readonly IApplicationDbContext _db;

    public TransitionBookingCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<BookingDto> Handle(TransitionBookingCommand cmd, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item).ThenInclude(i => i.Owner)
            .Include(b => b.Renter)
            .FirstOrDefaultAsync(b => b.Id == cmd.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {cmd.BookingId} not found.");

        var key = (booking.Status, cmd.TargetStatus);
        if (!AllowedTransitions.TryGetValue(key, out var requiredRole))
            throw new InvalidOperationException(
                $"Transition from {booking.Status} to {cmd.TargetStatus} is not allowed.");

        var isLender = booking.Item.LenderId == cmd.RequestingUserId;
        var isRenter = booking.RenterId == cmd.RequestingUserId;

        var authorized = requiredRole switch
        {
            "lender" => isLender,
            "renter" => isRenter,
            _ => false
        };

        if (!authorized)
            throw new UnauthorizedAccessException(
                $"Only the {requiredRole} can perform this transition.");

        booking.Status = cmd.TargetStatus;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Bookings.Commands.CreateBooking.CreateBookingCommandHandler
            .ToDto(booking, booking.Item, booking.Renter);
    }
}
```

- [ ] **Step 5: Run all booking tests**

```bash
cd backend
dotnet test Borro.Tests/Borro.Tests.csproj --filter "Bookings" -v minimal
```

Expected: `Passed: 7` (3 CreateBooking + 4 TransitionBooking)

- [ ] **Step 6: Commit**

```bash
git add backend/Borro.Application/Bookings/Commands/TransitionBooking/ backend/Borro.Tests/Bookings/TransitionBookingCommandHandlerTests.cs
git commit -m "feat: add TransitionBookingCommand with strict state machine and tests"
```

---

## Task 5: Booking Queries & Chat Commands

**Files:**
- Create: `backend/Borro.Application/Bookings/Queries/GetBooking/GetBookingQuery.cs`
- Create: `backend/Borro.Application/Bookings/Queries/GetBooking/GetBookingQueryHandler.cs`
- Create: `backend/Borro.Application/Bookings/Queries/GetUserBookings/GetUserBookingsQuery.cs`
- Create: `backend/Borro.Application/Bookings/Queries/GetUserBookings/GetUserBookingsQueryHandler.cs`
- Create: `backend/Borro.Application/Chat/Commands/SendMessage/SendMessageCommand.cs`
- Create: `backend/Borro.Application/Chat/Commands/SendMessage/SendMessageCommandHandler.cs`
- Create: `backend/Borro.Application/Chat/Queries/GetMessages/GetMessagesQuery.cs`
- Create: `backend/Borro.Application/Chat/Queries/GetMessages/GetMessagesQueryHandler.cs`

- [ ] **Step 1: GetBookingQuery**

```csharp
// backend/Borro.Application/Bookings/Queries/GetBooking/GetBookingQuery.cs
using Borro.Application.Bookings.DTOs;
using MediatR;

namespace Borro.Application.Bookings.Queries.GetBooking;

public record GetBookingQuery(Guid BookingId, Guid RequestingUserId) : IRequest<BookingDto>;
```

- [ ] **Step 2: GetBookingQueryHandler**

```csharp
// backend/Borro.Application/Bookings/Queries/GetBooking/GetBookingQueryHandler.cs
using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Application.Bookings.DTOs;
using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Bookings.Queries.GetBooking;

public class GetBookingQueryHandler : IRequestHandler<GetBookingQuery, BookingDto>
{
    private readonly IApplicationDbContext _db;
    public GetBookingQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<BookingDto> Handle(GetBookingQuery q, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item).ThenInclude(i => i.Owner)
            .Include(b => b.Renter)
            .FirstOrDefaultAsync(b => b.Id == q.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {q.BookingId} not found.");

        // Only lender or renter can view
        var isParticipant = booking.RenterId == q.RequestingUserId
                         || booking.Item.LenderId == q.RequestingUserId;
        if (!isParticipant)
            throw new UnauthorizedAccessException("Access denied.");

        return CreateBookingCommandHandler.ToDto(booking, booking.Item, booking.Renter);
    }
}
```

- [ ] **Step 3: GetUserBookingsQuery**

```csharp
// backend/Borro.Application/Bookings/Queries/GetUserBookings/GetUserBookingsQuery.cs
using Borro.Application.Bookings.DTOs;
using MediatR;

namespace Borro.Application.Bookings.Queries.GetUserBookings;

public record GetUserBookingsQuery(Guid UserId) : IRequest<List<BookingDto>>;
```

- [ ] **Step 4: GetUserBookingsQueryHandler**

```csharp
// backend/Borro.Application/Bookings/Queries/GetUserBookings/GetUserBookingsQueryHandler.cs
using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Application.Bookings.DTOs;
using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Bookings.Queries.GetUserBookings;

public class GetUserBookingsQueryHandler : IRequestHandler<GetUserBookingsQuery, List<BookingDto>>
{
    private readonly IApplicationDbContext _db;
    public GetUserBookingsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<BookingDto>> Handle(GetUserBookingsQuery q, CancellationToken ct)
    {
        var bookings = await _db.Bookings
            .Include(b => b.Item).ThenInclude(i => i.Owner)
            .Include(b => b.Renter)
            .Where(b => b.RenterId == q.UserId || b.Item.LenderId == q.UserId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct);

        return bookings.Select(b => CreateBookingCommandHandler.ToDto(b, b.Item, b.Renter)).ToList();
    }
}
```

- [ ] **Step 5: SendMessageCommand**

```csharp
// backend/Borro.Application/Chat/Commands/SendMessage/SendMessageCommand.cs
using Borro.Application.Chat.DTOs;
using MediatR;

namespace Borro.Application.Chat.Commands.SendMessage;

public record SendMessageCommand(
    Guid BookingId,
    Guid SenderId,
    string Content
) : IRequest<MessageDto>;
```

- [ ] **Step 6: SendMessageCommandHandler**

```csharp
// backend/Borro.Application/Chat/Commands/SendMessage/SendMessageCommandHandler.cs
using Borro.Application.Chat.DTOs;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Chat.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IApplicationDbContext _db;
    public SendMessageCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<MessageDto> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == cmd.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {cmd.BookingId} not found.");

        var isParticipant = booking.RenterId == cmd.SenderId || booking.Item.LenderId == cmd.SenderId;
        if (!isParticipant)
            throw new UnauthorizedAccessException("Only booking participants can send messages.");

        var sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == cmd.SenderId, ct)
            ?? throw new InvalidOperationException($"User {cmd.SenderId} not found.");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            BookingId = cmd.BookingId,
            SenderId = cmd.SenderId,
            Content = cmd.Content,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);

        return new MessageDto(message.Id, message.BookingId, message.SenderId,
            $"{sender.FirstName} {sender.LastName}", message.Content, message.CreatedAtUtc);
    }
}
```

- [ ] **Step 7: GetMessagesQuery + Handler**

```csharp
// backend/Borro.Application/Chat/Queries/GetMessages/GetMessagesQuery.cs
using Borro.Application.Chat.DTOs;
using MediatR;

namespace Borro.Application.Chat.Queries.GetMessages;

public record GetMessagesQuery(Guid BookingId, Guid RequestingUserId) : IRequest<List<MessageDto>>;
```

```csharp
// backend/Borro.Application/Chat/Queries/GetMessages/GetMessagesQueryHandler.cs
using Borro.Application.Chat.DTOs;
using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Chat.Queries.GetMessages;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, List<MessageDto>>
{
    private readonly IApplicationDbContext _db;
    public GetMessagesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<MessageDto>> Handle(GetMessagesQuery q, CancellationToken ct)
    {
        var booking = await _db.Bookings.Include(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == q.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking {q.BookingId} not found.");

        var isParticipant = booking.RenterId == q.RequestingUserId || booking.Item.LenderId == q.RequestingUserId;
        if (!isParticipant) throw new UnauthorizedAccessException("Access denied.");

        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.BookingId == q.BookingId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(ct);

        return messages.Select(m => new MessageDto(m.Id, m.BookingId, m.SenderId,
            $"{m.Sender.FirstName} {m.Sender.LastName}", m.Content, m.CreatedAtUtc)).ToList();
    }
}
```

- [ ] **Step 8: Build Application**

```bash
cd backend
dotnet build Borro.Application/Borro.Application.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add backend/Borro.Application/Bookings/ backend/Borro.Application/Chat/
git commit -m "feat: add booking queries and chat send/list commands"
```

---

## Task 6: Update BorroDbContext + EF Migration

**Files:**
- Modify: `backend/Borro.Infrastructure/Persistence/BorroDbContext.cs`

- [ ] **Step 1: Add Bookings and Messages to BorroDbContext**

In `BorroDbContext.cs`, add these two DbSets (after the Wishlists line):

```csharp
public DbSet<Booking> Bookings => Set<Booking>();
public DbSet<Message> Messages => Set<Message>();
```

Add Booking and Message configuration inside `OnModelCreating` (after the Wishlist config):

```csharp
modelBuilder.Entity<Booking>(entity =>
{
    entity.HasKey(b => b.Id);
    entity.Property(b => b.TotalPrice).HasColumnType("numeric(18,2)");
    entity.Property(b => b.Status).HasConversion<string>();

    entity.HasOne(b => b.Item)
        .WithMany()
        .HasForeignKey(b => b.ItemId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(b => b.Renter)
        .WithMany()
        .HasForeignKey(b => b.RenterId)
        .OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<Message>(entity =>
{
    entity.HasKey(m => m.Id);
    entity.Property(m => m.Content).IsRequired().HasMaxLength(2000);

    entity.HasOne(m => m.Booking)
        .WithMany(b => b.Messages)
        .HasForeignKey(m => m.BookingId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(m => m.Sender)
        .WithMany()
        .HasForeignKey(m => m.SenderId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

- [ ] **Step 2: Build solution**

```bash
cd backend
dotnet build Borro.sln
```

Expected: `Build succeeded.`

- [ ] **Step 3: Add EF migration**

```bash
cd backend
dotnet ef migrations add Phase3_BookingsAndMessages \
  --project Borro.Infrastructure \
  --startup-project Borro.Api
```

- [ ] **Step 4: Apply migration**

```bash
cd backend
dotnet ef database update \
  --project Borro.Infrastructure \
  --startup-project Borro.Api
```

Expected: `Done.`

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Infrastructure/Persistence/
git commit -m "feat: add Booking/Message to BorroDbContext with Phase3 migration"
```

---

## Task 7: SignalR ChatHub

**Files:**
- Create: `backend/Borro.Infrastructure/Hubs/ChatHub.cs`
- Modify: `backend/Borro.Api/Program.cs`

- [ ] **Step 1: Create ChatHub**

```csharp
// backend/Borro.Infrastructure/Hubs/ChatHub.cs
using Borro.Application.Chat.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Borro.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;

    public ChatHub(IMediator mediator) => _mediator = mediator;

    /// <summary>Called by client when they open a booking's chat.</summary>
    public async Task JoinBookingGroup(string bookingId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
    }

    /// <summary>Client calls this to send a message. Server persists it and broadcasts to the group.</summary>
    public async Task SendMessage(string bookingId, string content)
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var senderId))
        {
            throw new HubException("Unauthorized.");
        }

        if (!Guid.TryParse(bookingId, out var bookingGuid))
            throw new HubException("Invalid bookingId.");

        try
        {
            var dto = await _mediator.Send(new SendMessageCommand(bookingGuid, senderId, content));
            await Clients.Group($"booking-{bookingId}").SendAsync("ReceiveMessage", dto);
        }
        catch (Exception ex)
        {
            throw new HubException(ex.Message);
        }
    }
}
```

- [ ] **Step 2: Register SignalR + map hub in Program.cs**

Add `builder.Services.AddSignalR();` after `builder.Services.AddAuthorization();`

Add `app.MapHub<ChatHub>("/hubs/chat");` after `app.MapItemEndpoints();`

Add the using:
```csharp
using Borro.Infrastructure.Hubs;
```

- [ ] **Step 3: Update CORS to allow SignalR credentials**

The existing CORS policy already has `.AllowCredentials()`, so SignalR will work. No change needed.

- [ ] **Step 4: Build API**

```bash
cd backend
dotnet build Borro.Api/Borro.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add backend/Borro.Infrastructure/Hubs/ backend/Borro.Api/Program.cs
git commit -m "feat: add SignalR ChatHub with MediatR integration"
```

---

## Task 8: BookingEndpoints + Update Availability Stub

**Files:**
- Create: `backend/Borro.Api/Endpoints/BookingEndpoints.cs`
- Modify: `backend/Borro.Api/Endpoints/ItemEndpoints.cs` (fill in availability)
- Modify: `backend/Borro.Api/Program.cs`

- [ ] **Step 1: Create BookingEndpoints**

```csharp
// backend/Borro.Api/Endpoints/BookingEndpoints.cs
using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Application.Bookings.Commands.TransitionBooking;
using Borro.Application.Bookings.Queries.GetBooking;
using Borro.Application.Bookings.Queries.GetUserBookings;
using Borro.Application.Chat.Queries.GetMessages;
using Borro.Domain.Enums;
using MediatR;
using System.Security.Claims;

namespace Borro.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings").RequireAuthorization();

        // GET /api/bookings  — returns bookings where user is renter OR lender
        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            var results = await mediator.Send(new GetUserBookingsQuery(userId.Value), ct);
            return Results.Ok(results);
        });

        // GET /api/bookings/{id}
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            try
            {
                var booking = await mediator.Send(new GetBookingQuery(id, userId.Value), ct);
                return Results.Ok(booking);
            }
            catch (InvalidOperationException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // POST /api/bookings
        group.MapPost("/", async (CreateBookingRequest req, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            try
            {
                var booking = await mediator.Send(
                    new CreateBookingCommand(req.ItemId, userId.Value, req.StartDateUtc, req.EndDateUtc), ct);
                return Results.Created($"/api/bookings/{booking.Id}", booking);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        // PATCH /api/bookings/{id}/status
        group.MapPatch("/{id:guid}/status", async (Guid id, TransitionRequest req, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            if (!Enum.TryParse<BookingStatus>(req.Status, out var targetStatus))
                return Results.BadRequest(new { error = "Invalid status value." });
            try
            {
                var booking = await mediator.Send(new TransitionBookingCommand(id, userId.Value, targetStatus), ct);
                return Results.Ok(booking);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // GET /api/bookings/{id}/messages
        group.MapGet("/{id:guid}/messages", async (Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            try
            {
                var messages = await mediator.Send(new GetMessagesQuery(id, userId.Value), ct);
                return Results.Ok(messages);
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private record CreateBookingRequest(Guid ItemId, DateTime StartDateUtc, DateTime EndDateUtc);
    private record TransitionRequest(string Status);
}
```

- [ ] **Step 2: Fill in the availability stub in ItemEndpoints.cs**

Replace the stub availability handler:
```csharp
// OLD (stub):
group.MapGet("/{id:guid}/availability", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    await Task.CompletedTask;
    return Results.Ok(Array.Empty<object>());
});
```

```csharp
// NEW (real implementation):
group.MapGet("/{id:guid}/availability", async (Guid id, IApplicationDbContext db, CancellationToken ct) =>
{
    var bookedRanges = await db.Bookings
        .Where(b => b.ItemId == id &&
               b.Status != BookingStatus.Cancelled &&
               b.Status != BookingStatus.Disputed)
        .Select(b => new { b.StartDateUtc, b.EndDateUtc })
        .ToListAsync(ct);
    return Results.Ok(bookedRanges);
});
```

Add the required usings at the top of `ItemEndpoints.cs`:
```csharp
using Borro.Application.Common.Interfaces;
using Borro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
```

- [ ] **Step 3: Add MapBookingEndpoints() to Program.cs**

After `app.MapItemEndpoints();`, add:
```csharp
app.MapBookingEndpoints();
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
git commit -m "feat: add BookingEndpoints, fill availability endpoint, wire in Program.cs"
```

---

## Task 9: Frontend — bookingApi.ts + useChat.ts

**Files:**
- Create: `frontend/src/features/bookings/bookingApi.ts`
- Create: `frontend/src/features/bookings/useChat.ts`

- [ ] **Step 1: Install @microsoft/signalr**

```bash
cd frontend
npm install @microsoft/signalr
```

- [ ] **Step 2: Create bookingApi.ts**

```typescript
// frontend/src/features/bookings/bookingApi.ts
import apiClient from '../../lib/apiClient';

export type BookingStatus =
  | 'PendingApproval' | 'Approved' | 'PaymentHeld'
  | 'Active' | 'Completed' | 'Disputed' | 'Cancelled';

export interface BookingDto {
  id: string;
  itemId: string;
  itemTitle: string;
  renterId: string;
  renterName: string;
  lenderId: string;
  lenderName: string;
  startDateUtc: string;
  endDateUtc: string;
  totalPrice: number;
  status: BookingStatus;
  createdAtUtc: string;
}

export interface MessageDto {
  id: string;
  bookingId: string;
  senderId: string;
  senderName: string;
  content: string;
  createdAtUtc: string;
}

export const bookingApi = {
  create: (itemId: string, startDateUtc: string, endDateUtc: string) =>
    apiClient.post<BookingDto>('/api/bookings', { itemId, startDateUtc, endDateUtc }),

  list: () => apiClient.get<BookingDto[]>('/api/bookings'),

  getById: (id: string) => apiClient.get<BookingDto>(`/api/bookings/${id}`),

  transition: (id: string, status: BookingStatus) =>
    apiClient.patch<BookingDto>(`/api/bookings/${id}/status`, { status }),

  getMessages: (bookingId: string) =>
    apiClient.get<MessageDto[]>(`/api/bookings/${bookingId}/messages`),
};
```

- [ ] **Step 3: Create useChat.ts SignalR hook**

```typescript
// frontend/src/features/bookings/useChat.ts
import * as signalR from '@microsoft/signalr';
import { useCallback, useEffect, useRef, useState } from 'react';
import type { MessageDto } from './bookingApi';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:8180';

export function useChat(bookingId: string) {
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [connected, setConnected] = useState(false);
  const hubRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const token = localStorage.getItem('borro_token') ?? '';

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/chat`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMessage', (msg: MessageDto) => {
      setMessages(prev => [...prev, msg]);
    });

    connection
      .start()
      .then(() => {
        setConnected(true);
        return connection.invoke('JoinBookingGroup', bookingId);
      })
      .catch(console.error);

    hubRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [bookingId]);

  const sendMessage = useCallback(async (content: string) => {
    if (!hubRef.current || hubRef.current.state !== signalR.HubConnectionState.Connected) return;
    await hubRef.current.invoke('SendMessage', bookingId, content);
  }, [bookingId]);

  return { messages, connected, sendMessage, setMessages };
}
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/bookings/bookingApi.ts frontend/src/features/bookings/useChat.ts
git commit -m "feat: add bookingApi and useChat SignalR hook"
```

---

## Task 10: Frontend — BookingDetailPage

**Files:**
- Create: `frontend/src/features/bookings/BookingDetailPage.tsx`
- Modify: `frontend/src/features/items/ItemDetailPage.tsx` (wire Book button)
- Modify: `frontend/src/App.tsx` (add /bookings/:id route)

- [ ] **Step 1: Create BookingDetailPage**

```tsx
// frontend/src/features/bookings/BookingDetailPage.tsx
import { useEffect, useRef, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { bookingApi, type BookingDto, type BookingStatus } from './bookingApi';
import { useChat } from './useChat';
import { useAuth } from '../auth/AuthContext';

const STATUS_STEPS: BookingStatus[] = [
  'PendingApproval', 'Approved', 'PaymentHeld', 'Active', 'Completed'
];

const STATUS_LABELS: Record<BookingStatus, string> = {
  PendingApproval: 'Pending Approval',
  Approved: 'Approved',
  PaymentHeld: 'Payment Held',
  Active: 'Active',
  Completed: 'Completed',
  Disputed: 'Disputed',
  Cancelled: 'Cancelled',
};

export function BookingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [booking, setBooking] = useState<BookingDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [messageInput, setMessageInput] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const { messages, connected, sendMessage, setMessages } = useChat(id ?? '');

  useEffect(() => {
    if (!id) return;
    bookingApi.getById(id)
      .then(res => setBooking(res.data))
      .catch(() => navigate('/'))
      .finally(() => setLoading(false));

    bookingApi.getMessages(id)
      .then(res => setMessages(res.data))
      .catch(() => {});
  }, [id, navigate, setMessages]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!messageInput.trim()) return;
    await sendMessage(messageInput.trim());
    setMessageInput('');
  };

  const handleTransition = async (status: BookingStatus) => {
    if (!id) return;
    const res = await bookingApi.transition(id, status);
    setBooking(res.data);
  };

  if (loading) return <div className="min-h-screen flex items-center justify-center">Loading...</div>;
  if (!booking) return null;

  const isLender = user?.id === booking.lenderId;
  const isRenter = user?.id === booking.renterId;
  const currentStepIndex = STATUS_STEPS.indexOf(booking.status as BookingStatus);

  return (
    <div className="min-h-screen bg-surface font-body">
      <div className="max-w-screen-lg mx-auto px-6 py-10">
        <button onClick={() => navigate(-1)} className="flex items-center gap-2 text-on-surface-variant mb-6 bg-transparent border-none p-0 cursor-pointer hover:text-primary transition-colors">
          <span className="material-symbols-outlined">arrow_back</span> Back
        </button>

        <div className="grid lg:grid-cols-2 gap-10">
          {/* Left: Booking details + status timeline */}
          <div>
            <h1 className="font-headline text-2xl font-bold mb-2">{booking.itemTitle}</h1>
            <p className="text-on-surface-variant mb-6">
              {new Date(booking.startDateUtc).toLocaleDateString()} –{' '}
              {new Date(booking.endDateUtc).toLocaleDateString()}
              {' · '}${booking.totalPrice} total
            </p>

            {/* Status timeline */}
            <div className="bg-surface-container-low rounded-2xl p-6 mb-6">
              <p className="font-bold mb-4">Booking Status</p>
              <div className="flex flex-col gap-3">
                {STATUS_STEPS.map((step, i) => (
                  <div key={step} className="flex items-center gap-3">
                    <div className={`w-6 h-6 rounded-full flex items-center justify-center shrink-0 ${
                      i < currentStepIndex ? 'bg-primary text-on-primary' :
                      i === currentStepIndex ? 'bg-primary text-on-primary' :
                      'bg-surface-container border border-outline-variant'
                    }`}>
                      {i < currentStepIndex && <span className="material-symbols-outlined text-sm">check</span>}
                    </div>
                    <span className={`text-sm font-semibold ${
                      i === currentStepIndex ? 'text-primary' : 'text-on-surface-variant'
                    }`}>{STATUS_LABELS[step]}</span>
                  </div>
                ))}
              </div>
              {(booking.status === 'Disputed' || booking.status === 'Cancelled') && (
                <p className="mt-4 font-bold text-red-600">{STATUS_LABELS[booking.status]}</p>
              )}
            </div>

            {/* Action buttons based on role + current status */}
            <div className="flex flex-wrap gap-3">
              {isLender && booking.status === 'PendingApproval' && (
                <button onClick={() => handleTransition('Approved')}
                  className="bg-primary text-on-primary rounded-full px-6 py-3 font-bold border-none hover:opacity-90 active:scale-95">
                  Approve Booking
                </button>
              )}
              {isLender && (booking.status === 'PendingApproval' || booking.status === 'Approved') && (
                <button onClick={() => handleTransition('Cancelled')}
                  className="bg-surface-container border border-outline-variant rounded-full px-6 py-3 font-bold hover:bg-surface-container-high active:scale-95 border-none">
                  Cancel
                </button>
              )}
              {isRenter && booking.status === 'Approved' && (
                <button onClick={() => handleTransition('PaymentHeld')}
                  className="bg-primary text-on-primary rounded-full px-6 py-3 font-bold border-none hover:opacity-90 active:scale-95">
                  Confirm & Pay
                </button>
              )}
              {isRenter && booking.status === 'PaymentHeld' && (
                <button onClick={() => handleTransition('Active')}
                  className="bg-primary text-on-primary rounded-full px-6 py-3 font-bold border-none hover:opacity-90 active:scale-95">
                  Confirm Pickup
                </button>
              )}
              {isRenter && booking.status === 'Active' && (
                <button onClick={() => handleTransition('Completed')}
                  className="bg-primary text-on-primary rounded-full px-6 py-3 font-bold border-none hover:opacity-90 active:scale-95">
                  Confirm Return
                </button>
              )}
            </div>
          </div>

          {/* Right: Chat */}
          <div className="flex flex-col h-[500px] bg-surface-container-low rounded-2xl overflow-hidden">
            <div className="px-4 py-3 border-b border-outline-variant/20 flex items-center gap-2">
              <span className={`w-2 h-2 rounded-full ${connected ? 'bg-green-500' : 'bg-gray-400'}`} />
              <p className="font-bold text-sm">Chat with {isRenter ? booking.lenderName : booking.renterName}</p>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-3">
              {messages.map(msg => (
                <div key={msg.id} className={`flex ${msg.senderId === user?.id ? 'justify-end' : 'justify-start'}`}>
                  <div className={`max-w-xs px-4 py-2 rounded-2xl text-sm ${
                    msg.senderId === user?.id
                      ? 'bg-primary text-on-primary'
                      : 'bg-surface-container text-on-surface'
                  }`}>
                    {msg.content}
                  </div>
                </div>
              ))}
              <div ref={messagesEndRef} />
            </div>

            <form onSubmit={handleSend} className="p-3 border-t border-outline-variant/20 flex gap-2">
              <input
                className="flex-1 bg-surface border border-outline-variant rounded-full px-4 py-2 text-sm outline-none"
                placeholder="Type a message..."
                value={messageInput}
                onChange={e => setMessageInput(e.target.value)}
              />
              <button type="submit" className="bg-primary text-on-primary w-10 h-10 rounded-full flex items-center justify-center border-none">
                <span className="material-symbols-outlined text-sm">send</span>
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Wire "Book" button in ItemDetailPage.tsx**

In `ItemDetailPage.tsx`, add state and handler for booking:

```tsx
// Add these imports:
import { useState } from 'react';
import { bookingApi } from '../bookings/bookingApi';

// Add state inside the component (after existing state):
const [bookingLoading, setBookingLoading] = useState(false);

// Replace the Book button with:
<button
  onClick={async () => {
    if (!item) return;
    setBookingLoading(true);
    try {
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const dayAfter = new Date(tomorrow);
      dayAfter.setDate(dayAfter.getDate() + 1);
      const res = await bookingApi.create(
        item.id,
        tomorrow.toISOString(),
        dayAfter.toISOString()
      );
      navigate(`/bookings/${res.data.id}`);
    } catch {
      alert('Failed to create booking.');
    } finally {
      setBookingLoading(false);
    }
  }}
  disabled={bookingLoading}
  className="w-full bg-primary text-on-primary rounded-full py-4 font-bold text-lg hover:opacity-90 transition-opacity active:scale-95 border-none disabled:opacity-50"
>
  {bookingLoading ? 'Booking...' : (item.instantBookEnabled ? 'Book Instantly' : 'Request to Book')}
</button>
```

Note: This books with hardcoded dates (tomorrow + 1 day) as a placeholder. A date picker for selecting specific rental dates is a UI enhancement beyond the MVP scope.

- [ ] **Step 3: Add /bookings/:id route to App.tsx**

```tsx
// Add import:
import { BookingDetailPage } from './features/bookings/BookingDetailPage';

// Add route inside <Routes>:
<Route path="/bookings/:id" element={<ProtectedRoute><BookingDetailPage /></ProtectedRoute>} />
```

- [ ] **Step 4: Build frontend**

```bash
cd frontend
npm run build 2>&1 | tail -20
```

Expected: `built in X.XXs` with no TypeScript errors.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/
git commit -m "feat: add BookingDetailPage with status timeline, transition actions, and SignalR chat"
```

---

## Phase 3 Complete

At this point:
- Backend: Full booking state machine (7 states, role-enforced transitions), SignalR chat, message history
- Frontend: BookingDetailPage with progress timeline, action buttons for lender/renter, real-time chat
- Availability endpoint fills in booked date ranges for Phase 4's checklist gating

Proceed to **Phase 4** plan: `2026-04-16-phase-4-trust-payments-reviews.md`
