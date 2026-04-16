using Borro.Application.Items.Commands;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Borro.Tests.Items;

public class CreateItemCommandHandlerTests
{
    private static BorroDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BorroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BorroDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesItemAndReturnsDto()
    {
        await using var ctx = CreateContext();
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@test.com",
            FirstName = "Alice",
            LastName = "Smith",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        ctx.Users.Add(owner);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateItemCommandHandler(ctx);
        var cmd = new CreateItemCommand(
            OwnerId: owner.Id,
            Title: "DeWalt Drill",
            Description: "Heavy-duty cordless drill",
            DailyPrice: 15m,
            Location: "Portland, OR",
            Category: Category.Tools,
            InstantBookEnabled: true,
            HandoverOptions: new List<HandoverOption> { HandoverOption.LocalPickup },
            Mileage: null,
            Transmission: null,
            Bedrooms: null,
            Megapixels: null,
            Brand: "DeWalt",
            Condition: null
        );

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.Equal("DeWalt Drill", result.Title);
        Assert.Equal(15m, result.DailyPrice);
        Assert.Equal(owner.Id, result.OwnerId);
        Assert.True(result.InstantBookEnabled);
        Assert.Single(ctx.Items);
    }

    [Fact]
    public async Task Handle_UnknownOwner_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var handler = new CreateItemCommandHandler(ctx);
        var cmd = new CreateItemCommand(
            OwnerId: Guid.NewGuid(),
            Title: "Camera",
            Description: "DSLR",
            DailyPrice: 50m,
            Location: "NYC",
            Category: Category.Electronics,
            InstantBookEnabled: false,
            HandoverOptions: new List<HandoverOption>(),
            Mileage: null,
            Transmission: null,
            Bedrooms: null,
            Megapixels: null,
            Brand: null,
            Condition: null
        );

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }
}
