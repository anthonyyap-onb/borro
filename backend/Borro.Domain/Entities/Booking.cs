using Borro.Domain.Enums;

namespace Borro.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public Guid RenterId { get; set; }
    public User Renter { get; set; } = null!;

    /// <summary>All dates in UTC.</summary>
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
