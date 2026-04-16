namespace Borro.Domain.Entities;

public class BlockedDate
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public DateOnly Date { get; set; }
}
