namespace Borro.Domain.Entities;

public class ItemAttributes
{
    // Flexible dictionary-style storage for JSONB mapping via EF Core .ToJson()
    public Dictionary<string, object> Values { get; set; } = new();
}

public class Item
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public string Category { get; set; } = string.Empty;
    public Guid LenderId { get; set; }
    public User Lender { get; set; } = null!;
    public bool InstantBookEnabled { get; set; }
    public bool DeliveryAvailable { get; set; }
    public string[] ImageUrls { get; set; } = [];

    /// <summary>
    /// Dynamic category-specific attributes stored as PostgreSQL JSONB via EF Core .ToJson().
    /// </summary>
    public ItemAttributes Attributes { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
