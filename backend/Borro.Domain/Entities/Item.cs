using Borro.Domain.Enums;

namespace Borro.Domain.Entities;

public class ItemAttributes
{
    public Dictionary<string, object> Values { get; set; } = new();
}

public class Item
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    /// <summary>Dynamic category-specific attributes stored as JSONB via EF Core .ToJson().</summary>
    public ItemAttributes Attributes { get; set; } = new();

    public bool InstantBookEnabled { get; set; }

    /// <summary>Stored as comma-delimited text, e.g. "OwnerDelivers,RenterPicksUp".</summary>
    public string HandoverOptionsRaw { get; set; } = string.Empty;

    /// <summary>Not mapped — computed from HandoverOptionsRaw.</summary>
    public List<HandoverOption> HandoverOptions
    {
        get => string.IsNullOrEmpty(HandoverOptionsRaw)
            ? new List<HandoverOption>()
            : HandoverOptionsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(Enum.Parse<HandoverOption>)
                .ToList();
        set => HandoverOptionsRaw = string.Join(',', value.Select(h => h.ToString()));
    }

    public List<string> ImageUrls { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
