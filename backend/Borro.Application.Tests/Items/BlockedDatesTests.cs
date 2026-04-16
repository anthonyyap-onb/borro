using Borro.Application.Items.BlockedDates.Commands.BlockDates;
using Borro.Application.Items.BlockedDates.Commands.UnblockDates;
using Borro.Application.Items.BlockedDates.Queries.GetBlockedDates;
using Borro.Application.Tests.Infrastructure;
using Borro.Domain.Entities;

namespace Borro.Application.Tests.Items;

public class BlockedDatesTests
{
    private static User CreateUser(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = "lender@test.com",
        FirstName = "Test",
        LastName = "Lender",
        PasswordHash = "hash",
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
    };

    private static Item CreateItem(Guid? id = null, Guid? lenderId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Title = "Test Item",
        Description = "A test item",
        DailyPrice = 10m,
        Category = "Other",
        LenderId = lenderId ?? Guid.NewGuid(),
        InstantBookEnabled = false,
        DeliveryAvailable = false,
        ImageUrls = [],
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
    };

    // ──────────────────────────────────────────────────────────────
    // BlockDatesCommand
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task BlockDates_ForValidItem_SavesBlockedDates()
    {
        var ctx = TestDbContextFactory.Create();
        var user = CreateUser();
        var item = CreateItem(lenderId: user.Id);
        item.LenderId = user.Id;
        ctx.Users.Add(user);
        ctx.Items.Add(item);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var dates = new[] { new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 2) };
        var handler = new BlockDatesCommandHandler(ctx);
        var command = new BlockDatesCommand(item.Id, item.LenderId, dates);

        await handler.Handle(command, CancellationToken.None);

        var saved = ctx.BlockedDates.Where(b => b.ItemId == item.Id).ToList();
        Assert.Equal(2, saved.Count);
        Assert.Contains(saved, b => b.Date == new DateOnly(2025, 6, 1));
        Assert.Contains(saved, b => b.Date == new DateOnly(2025, 6, 2));
    }

    [Fact]
    public async Task BlockDates_ForNonExistentItem_ThrowsKeyNotFoundException()
    {
        var ctx = TestDbContextFactory.Create();
        var handler = new BlockDatesCommandHandler(ctx);
        var command = new BlockDatesCommand(Guid.NewGuid(), Guid.NewGuid(), [new DateOnly(2025, 6, 1)]);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task BlockDates_WhenNotItemOwner_ThrowsUnauthorizedAccessException()
    {
        var ctx = TestDbContextFactory.Create();
        var owner = CreateUser();
        var item = CreateItem(lenderId: owner.Id);
        ctx.Users.Add(owner);
        ctx.Items.Add(item);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var differentUser = Guid.NewGuid();
        var handler = new BlockDatesCommandHandler(ctx);
        var command = new BlockDatesCommand(item.Id, differentUser, [new DateOnly(2025, 6, 1)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task BlockDates_WithAlreadyBlockedDates_DoesNotDuplicate()
    {
        var ctx = TestDbContextFactory.Create();
        var user = CreateUser();
        var item = CreateItem(lenderId: user.Id);
        ctx.Users.Add(user);
        ctx.Items.Add(item);
        ctx.BlockedDates.Add(new BlockedDate { Id = Guid.NewGuid(), ItemId = item.Id, Date = new DateOnly(2025, 6, 1) });
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new BlockDatesCommandHandler(ctx);
        // Attempt to block same date again + one new date
        var command = new BlockDatesCommand(item.Id, item.LenderId, [new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 2)]);

        await handler.Handle(command, CancellationToken.None);

        var saved = ctx.BlockedDates.Where(b => b.ItemId == item.Id).ToList();
        Assert.Equal(2, saved.Count); // no duplicate for June 1
    }

    // ──────────────────────────────────────────────────────────────
    // UnblockDatesCommand
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UnblockDates_ForValidItem_RemovesBlockedDates()
    {
        var ctx = TestDbContextFactory.Create();
        var user = CreateUser();
        var item = CreateItem(lenderId: user.Id);
        ctx.Users.Add(user);
        ctx.Items.Add(item);
        ctx.BlockedDates.AddRange(
            new BlockedDate { Id = Guid.NewGuid(), ItemId = item.Id, Date = new DateOnly(2025, 6, 1) },
            new BlockedDate { Id = Guid.NewGuid(), ItemId = item.Id, Date = new DateOnly(2025, 6, 2) }
        );
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new UnblockDatesCommandHandler(ctx);
        var command = new UnblockDatesCommand(item.Id, item.LenderId, [new DateOnly(2025, 6, 1)]);

        await handler.Handle(command, CancellationToken.None);

        var remaining = ctx.BlockedDates.Where(b => b.ItemId == item.Id).ToList();
        Assert.Single(remaining);
        Assert.Equal(new DateOnly(2025, 6, 2), remaining[0].Date);
    }

    [Fact]
    public async Task UnblockDates_ForNonExistentItem_ThrowsKeyNotFoundException()
    {
        var ctx = TestDbContextFactory.Create();
        var handler = new UnblockDatesCommandHandler(ctx);
        var command = new UnblockDatesCommand(Guid.NewGuid(), Guid.NewGuid(), [new DateOnly(2025, 6, 1)]);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UnblockDates_WhenNotItemOwner_ThrowsUnauthorizedAccessException()
    {
        var ctx = TestDbContextFactory.Create();
        var owner = CreateUser();
        var item = CreateItem(lenderId: owner.Id);
        ctx.Users.Add(owner);
        ctx.Items.Add(item);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new UnblockDatesCommandHandler(ctx);
        var command = new UnblockDatesCommand(item.Id, Guid.NewGuid(), [new DateOnly(2025, 6, 1)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UnblockDates_ForNonBlockedDates_DoesNothing()
    {
        var ctx = TestDbContextFactory.Create();
        var user = CreateUser();
        var item = CreateItem(lenderId: user.Id);
        ctx.Users.Add(user);
        ctx.Items.Add(item);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new UnblockDatesCommandHandler(ctx);
        var command = new UnblockDatesCommand(item.Id, item.LenderId, [new DateOnly(2025, 6, 1)]);

        // Should not throw — just a no-op
        await handler.Handle(command, CancellationToken.None);

        Assert.Empty(ctx.BlockedDates.Where(b => b.ItemId == item.Id));
    }

    // ──────────────────────────────────────────────────────────────
    // GetBlockedDatesQuery
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBlockedDates_ForItemWithBlockedDates_ReturnsSortedDates()
    {
        var ctx = TestDbContextFactory.Create();
        var user = CreateUser();
        var item = CreateItem(lenderId: user.Id);
        ctx.Users.Add(user);
        ctx.Items.Add(item);
        ctx.BlockedDates.AddRange(
            new BlockedDate { Id = Guid.NewGuid(), ItemId = item.Id, Date = new DateOnly(2025, 6, 3) },
            new BlockedDate { Id = Guid.NewGuid(), ItemId = item.Id, Date = new DateOnly(2025, 6, 1) },
            new BlockedDate { Id = Guid.NewGuid(), ItemId = item.Id, Date = new DateOnly(2025, 6, 2) }
        );
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new GetBlockedDatesQueryHandler(ctx);
        var result = await handler.Handle(new GetBlockedDatesQuery(item.Id), CancellationToken.None);

        Assert.Equal(3, result.Dates.Count);
        Assert.Equal(new DateOnly(2025, 6, 1), result.Dates[0]);
        Assert.Equal(new DateOnly(2025, 6, 2), result.Dates[1]);
        Assert.Equal(new DateOnly(2025, 6, 3), result.Dates[2]);
    }

    [Fact]
    public async Task GetBlockedDates_ForItemWithNoBlockedDates_ReturnsEmpty()
    {
        var ctx = TestDbContextFactory.Create();
        var user = CreateUser();
        var item = CreateItem(lenderId: user.Id);
        ctx.Users.Add(user);
        ctx.Items.Add(item);
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new GetBlockedDatesQueryHandler(ctx);
        var result = await handler.Handle(new GetBlockedDatesQuery(item.Id), CancellationToken.None);

        Assert.Empty(result.Dates);
    }

    [Fact]
    public async Task GetBlockedDates_ForNonExistentItem_ThrowsKeyNotFoundException()
    {
        var ctx = TestDbContextFactory.Create();
        var handler = new GetBlockedDatesQueryHandler(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetBlockedDatesQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
