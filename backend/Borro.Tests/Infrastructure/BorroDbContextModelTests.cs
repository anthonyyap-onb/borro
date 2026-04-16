using Borro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Borro.Tests.Infrastructure;

/// <summary>
/// Lightweight model-validation tests that verify the EF Core model configuration
/// compiles and all expected entity sets are registered.
///
/// Uses the Npgsql provider with a dummy connection string but never opens a
/// real database connection — model building is purely in-memory.
/// </summary>
public class BorroDbContextModelTests
{
    /// <summary>
    /// Build a context with the Npgsql provider but no real connection.
    /// Model building succeeds without a live database.
    /// </summary>
    private static BorroDbContext BuildModelOnlyContext()
    {
        var options = new DbContextOptionsBuilder<BorroDbContext>()
            .UseNpgsql("Host=localhost;Database=borro_model_test;Username=test;Password=test")
            .Options;

        return new BorroDbContext(options);
    }

    [Fact]
    public void DbContext_Model_HasItemsEntitySet()
    {
        using var ctx = BuildModelOnlyContext();

        ctx.Items.Should().NotBeNull();
        ctx.Model.FindEntityType(typeof(Borro.Domain.Entities.Item)).Should().NotBeNull();
    }

    [Fact]
    public void DbContext_Model_HasUsersEntitySet()
    {
        using var ctx = BuildModelOnlyContext();

        ctx.Users.Should().NotBeNull();
        ctx.Model.FindEntityType(typeof(Borro.Domain.Entities.User)).Should().NotBeNull();
    }

    [Fact]
    public void DbContext_Model_HasItemBlockedDatesEntitySet()
    {
        using var ctx = BuildModelOnlyContext();

        ctx.ItemBlockedDates.Should().NotBeNull();
        ctx.Model.FindEntityType(typeof(Borro.Domain.Entities.ItemBlockedDate)).Should().NotBeNull();
    }

    [Fact]
    public void DbContext_Model_HasWishlistsEntitySet()
    {
        using var ctx = BuildModelOnlyContext();

        ctx.Wishlists.Should().NotBeNull();
        ctx.Model.FindEntityType(typeof(Borro.Domain.Entities.Wishlist)).Should().NotBeNull();
    }

    [Fact]
    public void DbContext_Model_ItemHasOwnedAttributesType()
    {
        using var ctx = BuildModelOnlyContext();

        var itemType = ctx.Model.FindEntityType(typeof(Borro.Domain.Entities.Item));
        itemType.Should().NotBeNull();

        var attributesNav = itemType!.FindNavigation(nameof(Borro.Domain.Entities.Item.Attributes));
        attributesNav.Should().NotBeNull("ItemAttributes should be configured as an owned navigation");
    }

    [Fact]
    public void DbContext_Model_WishlistHasUniqueIndex_UserIdItemId()
    {
        using var ctx = BuildModelOnlyContext();

        var wishlistType = ctx.Model.FindEntityType(typeof(Borro.Domain.Entities.Wishlist));
        wishlistType.Should().NotBeNull();

        var indexes = wishlistType!.GetIndexes();
        indexes.Should().Contain(ix =>
            ix.IsUnique &&
            ix.Properties.Count == 2 &&
            ix.Properties.Any(p => p.Name == "UserId") &&
            ix.Properties.Any(p => p.Name == "ItemId"),
            "Wishlist must have a unique composite index on (UserId, ItemId)");
    }

    [Fact]
    public void DbContext_Model_ItemBlockedDateHasCompositeIndex_ItemIdDateUtc()
    {
        using var ctx = BuildModelOnlyContext();

        var blockedType = ctx.Model.FindEntityType(typeof(Borro.Domain.Entities.ItemBlockedDate));
        blockedType.Should().NotBeNull();

        var indexes = blockedType!.GetIndexes();
        indexes.Should().Contain(ix =>
            ix.Properties.Count == 2 &&
            ix.Properties.Any(p => p.Name == "ItemId") &&
            ix.Properties.Any(p => p.Name == "DateUtc"),
            "ItemBlockedDate must have a composite index on (ItemId, DateUtc)");
    }
}
