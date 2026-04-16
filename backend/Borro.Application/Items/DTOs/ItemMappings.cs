using Borro.Domain.Entities;

namespace Borro.Application.Items.DTOs;

internal static class ItemMappings
{
    internal static ItemDto ToDto(this Item item) =>
        new(
            item.Id,
            item.OwnerId,
            item.Owner is null ? "" : $"{item.Owner.FirstName} {item.Owner.LastName}",
            item.Title,
            item.Description,
            item.DailyPrice,
            item.Location,
            item.Category,
            item.Attributes.ToDto(),
            item.InstantBookEnabled,
            item.HandoverOptions.ToList(),
            item.ImageUrls.ToList(),
            item.CreatedAtUtc,
            item.UpdatedAtUtc
        );

    internal static ItemAttributesDto ToDto(this ItemAttributes a) =>
        new(a.Mileage, a.Transmission, a.Bedrooms, a.Megapixels, a.Brand, a.Condition);
}
