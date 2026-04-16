using Borro.Application.Items.DTOs;
using Borro.Domain.Entities;

namespace Borro.Application.Items;

internal static class ItemMappingExtensions
{
    internal static ItemDto ToDto(this Item item, User lender) => new(
        item.Id,
        item.Title,
        item.Description,
        item.DailyPrice,
        item.Category,
        item.LenderId,
        lender.FirstName,
        lender.LastName,
        item.InstantBookEnabled,
        item.DeliveryAvailable,
        item.ImageUrls,
        item.Attributes.Values,
        item.CreatedAtUtc,
        item.UpdatedAtUtc);
}
