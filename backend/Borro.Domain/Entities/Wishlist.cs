namespace Borro.Domain.Entities;

public class Wishlist
{
    private Wishlist() { }

    public static Wishlist Create(Guid userId, Guid itemId)
    {
        return new Wishlist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ItemId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    // Navigations
    public User User { get; private set; } = null!;
    public Item Item { get; private set; } = null!;
}
