using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Commands.CreateItem;

public record CreateItemCommand(
    Guid LenderId,
    string Title,
    string Description,
    decimal DailyPrice,
    string Category,
    bool InstantBookEnabled,
    bool DeliveryAvailable,
    string[] ImageUrls,
    Dictionary<string, object> Attributes) : IRequest<ItemDto>;
