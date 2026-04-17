using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

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
            Id = Guid.NewGuid(), LenderId = lender.Id, Lender = lender,
            Title = "Drill", Description = "desc", DailyPrice = 20m,
            Location = "Portland", Category = "Tools",
            Attributes = new ItemAttributes { Values = new() },
            InstantBookEnabled = instantBook,
            ImageUrls = [], CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
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
