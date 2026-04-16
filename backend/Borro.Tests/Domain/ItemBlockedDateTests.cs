using Borro.Domain.Entities;
using FluentAssertions;

namespace Borro.Tests.Domain;

public class ItemBlockedDateTests
{
    [Fact]
    public void Create_WithUtcDate_Succeeds()
    {
        var itemId = Guid.NewGuid();
        var utcDate = DateTime.SpecifyKind(new DateTime(2026, 7, 1), DateTimeKind.Utc);

        var blocked = ItemBlockedDate.Create(itemId, utcDate);

        blocked.ItemId.Should().Be(itemId);
        blocked.DateUtc.Should().Be(utcDate);
        blocked.DateUtc.Kind.Should().Be(DateTimeKind.Utc);
        blocked.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        blocked.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithLocalDate_Throws()
    {
        var itemId = Guid.NewGuid();
        var localDate = DateTime.SpecifyKind(new DateTime(2026, 7, 1), DateTimeKind.Local);

        var act = () => ItemBlockedDate.Create(itemId, localDate);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*DateUtc*");
    }

    [Fact]
    public void Create_WithUnspecifiedKind_Throws()
    {
        var itemId = Guid.NewGuid();
        var unspecified = new DateTime(2026, 7, 1); // DateTimeKind.Unspecified

        var act = () => ItemBlockedDate.Create(itemId, unspecified);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*DateUtc*");
    }
}
