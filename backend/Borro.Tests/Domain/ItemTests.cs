using Borro.Domain.Entities;
using Borro.Domain.Enums;
using FluentAssertions;

namespace Borro.Tests.Domain;

public class ItemTests
{
    private static readonly Guid SomeOwnerId = Guid.NewGuid();

    // ── Factory guard: Title ────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_Throws(string? title)
    {
        var act = () => Item.Create(SomeOwnerId, title!, "desc", 10m, "Sydney", Category.Other);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Title*");
    }

    // ── Factory guard: DailyPrice ───────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Create_WithNonPositiveDailyPrice_Throws(decimal price)
    {
        var act = () => Item.Create(SomeOwnerId, "Camera", "desc", price, "Sydney", Category.Electronics);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithMessage("*DailyPrice*");
    }

    // ── Factory guard: Location ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyLocation_Throws(string? location)
    {
        var act = () => Item.Create(SomeOwnerId, "Camera", "desc", 10m, location!, Category.Electronics);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Location*");
    }

    // ── Defaults ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidArgs_HasCorrectDefaults()
    {
        var item = Item.Create(SomeOwnerId, "Toyota Camry", "Nice car", 75m, "Sydney", Category.Vehicle);

        item.InstantBookEnabled.Should().BeFalse();
        item.HandoverOptions.Should().BeEmpty();
        item.ImageUrls.Should().BeEmpty();
        item.BlockedDates.Should().BeEmpty();
    }

    [Fact]
    public void Create_ValidArgs_AttributesInstanceExists()
    {
        var item = Item.Create(SomeOwnerId, "Toyota Camry", "Nice car", 75m, "Sydney", Category.Vehicle);

        item.Attributes.Should().NotBeNull();
        item.Attributes.Mileage.Should().BeNull();
        item.Attributes.Transmission.Should().BeNull();
    }

    [Fact]
    public void Create_ValidArgs_TimestampsAreUtc()
    {
        var item = Item.Create(SomeOwnerId, "Toyota Camry", "Nice car", 75m, "Sydney", Category.Vehicle);

        item.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        item.UpdatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_ValidArgs_OwnerIdIsSet()
    {
        var item = Item.Create(SomeOwnerId, "Toyota Camry", "Nice car", 75m, "Sydney", Category.Vehicle);

        item.OwnerId.Should().Be(SomeOwnerId);
    }

    [Fact]
    public void Create_ValidArgs_IdIsNonEmpty()
    {
        var item = Item.Create(SomeOwnerId, "Toyota Camry", "Nice car", 75m, "Sydney", Category.Vehicle);

        item.Id.Should().NotBeEmpty();
    }
}
