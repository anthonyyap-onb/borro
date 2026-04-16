using Borro.Domain.Entities;
using FluentAssertions;

namespace Borro.Tests.Domain;

public class WishlistTests
{
    [Fact]
    public void Create_ExposesUserIdAndItemId()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var wishlist = Wishlist.Create(userId, itemId);

        wishlist.UserId.Should().Be(userId);
        wishlist.ItemId.Should().Be(itemId);
        wishlist.Id.Should().NotBeEmpty();
        wishlist.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }
}
