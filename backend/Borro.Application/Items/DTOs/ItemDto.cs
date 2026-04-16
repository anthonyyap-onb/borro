namespace Borro.Application.Items.DTOs;

public record ItemDto(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    string Title,
    string Description,
    decimal DailyPrice,
    string Location,
    string Category,
    Dictionary<string, object> Attributes,
    bool InstantBookEnabled,
    List<string> HandoverOptions,
    List<string> ImageUrls,
    DateTime CreatedAtUtc
);
