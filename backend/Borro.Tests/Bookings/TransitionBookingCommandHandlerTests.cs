using Borro.Application.Bookings.Commands.TransitionBooking;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

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
        var item = new Item
        {
            Id = Guid.NewGuid(), LenderId = lender.Id, Lender = lender,
            Title = "T", Description = "D", DailyPrice = 10m, Location = "L", Category = "C",
            Attributes = new ItemAttributes { Values = new() }, ImageUrls = [],
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        var booking = new Booking
        {
            Id = Guid.NewGuid(), ItemId = item.Id, Item = item,
            RenterId = renter.Id, Renter = renter,
            StartDateUtc = DateTime.UtcNow.AddDays(1), EndDateUtc = DateTime.UtcNow.AddDays(3),
            TotalPrice = 20m, Status = status,
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
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
