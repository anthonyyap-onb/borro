using Borro.Application.Items.Commands.CreateItem;
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

        var handler = new CreateItemCommandHandler(ctx, TimeProvider.System);
        var cmd = new CreateItemCommand(
            LenderId: owner.Id,
            Title: "DeWalt Drill",
            Description: "Heavy-duty cordless drill",
            DailyPrice: 15m,
            Location: "Portland, OR",
            Category: "Tools",
            Attributes: new Dictionary<string, object> { ["Voltage"] = "20V" },
            InstantBookEnabled: true,
            DeliveryAvailable: false,
            HandoverOptions: new List<string> { "RenterPicksUp" },
            ImageUrls: new List<string>()
        );

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.Equal("DeWalt Drill", result.Title);
        Assert.Equal(15m, result.DailyPrice);
        Assert.Equal(owner.Id, result.LenderId);
        Assert.True(result.InstantBookEnabled);
        Assert.Single(ctx.Items);
    }

    [Fact]
    public async Task Handle_UnknownOwner_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var handler = new CreateItemCommandHandler(ctx, TimeProvider.System);
        var cmd = new CreateItemCommand(
            LenderId: Guid.NewGuid(),
            Title: "Camera",
            Description: "DSLR",
            DailyPrice: 50m,
            Location: "NYC",
            Category: "Electronics",
            Attributes: new Dictionary<string, object>(),
            InstantBookEnabled: false,
            DeliveryAvailable: false,
            HandoverOptions: new List<string>(),
            ImageUrls: new List<string>()
        );

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }
}
