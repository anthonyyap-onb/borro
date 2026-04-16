using Borro.Domain.Enums;

namespace Borro.Domain.Entities;

public class Item
{
    // Required by EF Core (parameterless constructor for materialisation).
    private Item() { }

    /// <summary>
    /// Factory constructor enforcing domain invariants.
    /// </summary>
    public static Item Create(
        Guid ownerId,
        string title,
        string description,
        decimal dailyPrice,
        string location,
        Category category)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title must not be empty.", nameof(title));

        if (dailyPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(dailyPrice), "DailyPrice must be greater than zero.");

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location must not be empty.", nameof(location));

        var now = DateTime.UtcNow;

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
            HandoverOptions = new List<HandoverOption>(),
            ImageUrls = new List<string>(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal DailyPrice { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public Category Category { get; private set; }
    public ItemAttributes Attributes { get; private set; } = new();
    public bool InstantBookEnabled { get; private set; }
    public List<HandoverOption> HandoverOptions { get; private set; } = new();
    public List<string> ImageUrls { get; private set; } = new();
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // Navigation property for blocked dates.
    private readonly List<ItemBlockedDate> _blockedDates = new();
    public IReadOnlyCollection<ItemBlockedDate> BlockedDates => _blockedDates.AsReadOnly();

    /// <summary>
    /// Updates mutable listing fields. OwnerId, Id, and timestamps are immutable via this method.
    /// </summary>
    public void Update(
        string title,
        string description,
        decimal dailyPrice,
        string location,
        Category category,
        bool instantBookEnabled,
        List<HandoverOption> handoverOptions,
        ItemAttributes attributes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title must not be empty.", nameof(title));

        if (dailyPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(dailyPrice), "DailyPrice must be greater than zero.");

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location must not be empty.", nameof(location));

        Title = title.Trim();
        Description = description;
        DailyPrice = dailyPrice;
        Location = location.Trim();
        Category = category;
        InstantBookEnabled = instantBookEnabled;
        HandoverOptions = handoverOptions;
        Attributes = attributes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Adds an image URL to the item's image collection.</summary>
    public void AddImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Image URL must not be empty.", nameof(url));

        ImageUrls.Add(url);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a blocked date. Silently ignores duplicates (same calendar day).
    /// </summary>
    public void AddBlockedDate(DateTime dateUtc)
    {
        if (dateUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("dateUtc must have DateTimeKind.Utc.", nameof(dateUtc));

        var day = dateUtc.Date;
        var alreadyBlocked = _blockedDates.Any(d => d.DateUtc.Date == day);
        if (alreadyBlocked)
            return;

        _blockedDates.Add(ItemBlockedDate.Create(Id, dateUtc));
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a blocked date. Returns true if a record was removed, false if it did not exist.
    /// </summary>
    public bool RemoveBlockedDate(DateTime dateUtc)
    {
        var day = dateUtc.Date;
        var record = _blockedDates.FirstOrDefault(d => d.DateUtc.Date == day);
        if (record is null)
            return false;

        _blockedDates.Remove(record);
        UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }
}
