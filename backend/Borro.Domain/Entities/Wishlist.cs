namespace Borro.Domain.Entities;

public class Wishlist
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }

    public static Wishlist Create(Guid userId, Guid itemId) =>
        new() { Id = Guid.NewGuid(), UserId = userId, ItemId = itemId, CreatedAtUtc = DateTime.UtcNow };
}
