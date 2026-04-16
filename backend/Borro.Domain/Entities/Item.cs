using Borro.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Borro.Domain.Entities;

public class Item
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public string Location { get; set; } = string.Empty;
    public Category Category { get; set; }

    public ItemAttributes Attributes { get; set; } = new();

    public bool InstantBookEnabled { get; set; }

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

    public List<string> ImageUrls { get; set; } = new();
    public List<ItemBlockedDate> BlockedDates { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public static Item Create(Guid ownerId, string title, string description, decimal dailyPrice, string location, Category category)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (dailyPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(dailyPrice), "DailyPrice must be positive.");
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        return new Item
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = title.Trim(),
            Description = description,
            DailyPrice = dailyPrice,
            Location = location.Trim(),
            Category = category,
            Attributes = new ItemAttributes(),
            InstantBookEnabled = false,
            HandoverOptionsRaw = string.Empty,
            ImageUrls = new List<string>(),
            BlockedDates = new List<ItemBlockedDate>(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(string title, string description, decimal dailyPrice, string location,
        Category category, bool instantBookEnabled, List<HandoverOption> handoverOptions, ItemAttributes attributes)
    {
        Title = title;
        Description = description;
        DailyPrice = dailyPrice;
        Location = location;
        Category = category;
        InstantBookEnabled = instantBookEnabled;
        HandoverOptions = handoverOptions;
        Attributes = attributes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddBlockedDate(DateTime dateUtc)
    {
        if (dateUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateUtc must be UTC.", nameof(dateUtc));
        var date = dateUtc.Date;
        var utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        if (!BlockedDates.Any(d => d.DateUtc == utcDate))
            BlockedDates.Add(ItemBlockedDate.Create(Id, utcDate));
    }

    public bool RemoveBlockedDate(DateTime dateUtc)
    {
        var utcDate = DateTime.SpecifyKind(dateUtc.Date, DateTimeKind.Utc);
        var entry = BlockedDates.FirstOrDefault(d => d.DateUtc == utcDate);
        if (entry is null) return false;
        BlockedDates.Remove(entry);
        return true;
    }

    public void AddImageUrl(string url)
    {
        ImageUrls.Add(url);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
