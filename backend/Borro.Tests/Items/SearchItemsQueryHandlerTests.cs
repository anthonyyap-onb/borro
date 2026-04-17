using Borro.Application.Items.Queries.SearchItems;
using Borro.Domain.Entities;
using Borro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Borro.Tests.Items;

public class SearchItemsQueryHandlerTests
{
    private static BorroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<BorroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (User owner, Item item) Seed(BorroDbContext ctx, string category, decimal price, string location)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(), Email = $"{Guid.NewGuid()}@t.com",
            FirstName = "A", LastName = "B",
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        var item = new Item
        {
            Id = Guid.NewGuid(), LenderId = owner.Id, Lender = owner,
            Title = "Test Item", Description = "desc", DailyPrice = price,
            Location = location, Category = category,
            Attributes = new ItemAttributes { Values = new() },
            InstantBookEnabled = false, ImageUrls = [],
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        ctx.Users.Add(owner);
        ctx.Items.Add(item);
        ctx.SaveChanges();
        return (owner, item);
    }

    [Fact]
    public async Task Handle_NullFilters_ReturnsAllItems()
    {
        await using var ctx = CreateContext();
        Seed(ctx, "Tools", 20m, "Portland");
        Seed(ctx, "Vehicles", 80m, "Seattle");

        var handler = new SearchItemsQueryHandler(ctx);
        var results = await handler.Handle(new SearchItemsQuery(null, null, null), CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Handle_CategoryFilter_ReturnsOnlyMatchingItems()
    {
        await using var ctx = CreateContext();
        Seed(ctx, "Tools", 20m, "Portland");
        Seed(ctx, "Vehicles", 80m, "Seattle");

        var handler = new SearchItemsQueryHandler(ctx);
        var results = await handler.Handle(new SearchItemsQuery("Tools", null, null), CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("Tools", results[0].Category);
    }

    [Fact]
    public async Task Handle_MaxPriceFilter_ExcludesExpensiveItems()
    {
        await using var ctx = CreateContext();
        Seed(ctx, "Tools", 20m, "Portland");
        Seed(ctx, "Vehicles", 80m, "Seattle");

        var handler = new SearchItemsQueryHandler(ctx);
        var results = await handler.Handle(new SearchItemsQuery(null, null, 50m), CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(20m, results[0].DailyPrice);
    }
}
