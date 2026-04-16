namespace Borro.Domain.Entities;

public class Wishlist
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
