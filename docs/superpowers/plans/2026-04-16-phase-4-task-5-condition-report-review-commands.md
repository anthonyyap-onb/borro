# Phase 4 Task 5: ConditionReport & Review Commands + Tests

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement `CreateConditionReportCommand`, `CreateReviewCommand`, `GetReviewsQuery` with handlers and tests (TDD).

**Context:** Part of Phase 4 — Trust, Security & Payments. Depends on Tasks 1 & 2.

**Tech Stack:** .NET 9, MediatR 12.5, EF Core 9, xUnit, AWSSDK.S3 (`IStorageService`)

---

## File Map

| Action | File |
|--------|------|
| Create | `backend/Borro.Application/ConditionReports/Commands/CreateReport/CreateConditionReportCommand.cs` |
| Create | `backend/Borro.Application/ConditionReports/Commands/CreateReport/CreateConditionReportCommandHandler.cs` |
| Create | `backend/Borro.Application/Reviews/Commands/CreateReview/CreateReviewCommand.cs` |
| Create | `backend/Borro.Application/Reviews/Commands/CreateReview/CreateReviewCommandHandler.cs` |
| Create | `backend/Borro.Application/Reviews/Queries/GetReviews/GetReviewsQuery.cs` |
| Create | `backend/Borro.Application/Reviews/Queries/GetReviews/GetReviewsQueryHandler.cs` |
| Create | `backend/Borro.Tests/Reviews/CreateReviewCommandHandlerTests.cs` |

---

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
