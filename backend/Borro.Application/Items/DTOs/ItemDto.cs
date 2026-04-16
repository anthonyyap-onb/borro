namespace Borro.Application.Items.DTOs;

public record ItemDto(
    Guid Id,
    string Title,
    string Description,
    decimal DailyPrice,
    string Category,
    Guid LenderId,
    string LenderFirstName,
    string LenderLastName,
    bool InstantBookEnabled,
    bool DeliveryAvailable,
    string[] ImageUrls,
    Dictionary<string, object> Attributes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
