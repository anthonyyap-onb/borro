using Borro.Domain.Enums;

namespace Borro.Application.Items.DTOs;

/// <summary>Read model returned from all item queries.</summary>
public sealed record ItemDto(
    Guid Id,
    Guid OwnerId,
    string Title,
    string Description,
    decimal DailyPrice,
    string Location,
    Category Category,
    ItemAttributesDto Attributes,
    bool InstantBookEnabled,
    List<HandoverOption> HandoverOptions,
    List<string> ImageUrls,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public sealed record ItemAttributesDto(
    int? Mileage,
    string? Transmission,
    int? Bedrooms,
    int? Megapixels,
    string? Brand,
    string? Condition
);
