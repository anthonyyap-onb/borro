using Borro.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Borro.Domain.Entities;

public class ItemAttributes
{
    public Dictionary<string, object> Values { get; set; } = new();
}

public class Item
{
    public Guid Id { get; set; }
    public Guid LenderId { get; set; }
    public User Lender { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool InstantBookEnabled { get; set; }
    public bool DeliveryAvailable { get; set; }

    public string HandoverOptionsRaw { get; set; } = string.Empty;

    [NotMapped]
    public List<HandoverOption> HandoverOptions
    {
        get => string.IsNullOrEmpty(HandoverOptionsRaw)
            ? new List<HandoverOption>()
            : HandoverOptionsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => (Valid: Enum.TryParse<HandoverOption>(s, out var v), Value: v))
                .Where(x => x.Valid)
                .Select(x => x.Value)
                .ToList();
        set => HandoverOptionsRaw = value is null ? string.Empty : string.Join(',', value.Select(h => h.ToString()));
    }

    public string[] ImageUrls { get; set; } = [];
    public ItemAttributes Attributes { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
