namespace Borro.Domain.Entities;

public class ItemBlockedDate
{
    private ItemBlockedDate() { }

    public static ItemBlockedDate Create(Guid itemId, DateTime dateUtc)
    {
        if (dateUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateUtc must have DateTimeKind.Utc.", nameof(dateUtc));

        return new ItemBlockedDate
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            DateUtc = dateUtc,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }

    /// <summary>Always UTC. Represents a calendar day that is blocked for booking.</summary>
    public DateTime DateUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    // Navigation
    public Item Item { get; private set; } = null!;
}
