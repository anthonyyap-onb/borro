using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Commands.CreateItem;

public record CreateItemCommand(
    Guid LenderId,
    string Title,
    string Description,
    decimal DailyPrice,
    string Location,
    string Category,
    Dictionary<string, object> Attributes,
    bool InstantBookEnabled,
    bool DeliveryAvailable,
    List<string> HandoverOptions
) : IRequest<ItemDto>;
