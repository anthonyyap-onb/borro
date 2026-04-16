using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Commands.CreateItem;

public record CreateItemCommand(
    Guid OwnerId,
    string Title,
    string Description,
    decimal DailyPrice,
    string Location,
    string Category,
    Dictionary<string, object> Attributes,
    bool InstantBookEnabled,
    List<string> HandoverOptions
) : IRequest<ItemDto>;
