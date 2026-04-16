using Borro.Application.Items.DTOs;
using Borro.Domain.Enums;
using MediatR;

namespace Borro.Application.Items.Commands.CreateItem;

public record CreateItemCommand(
    Guid OwnerId,
    string Title,
    string Description,
    decimal DailyPrice,
    string Location,
    Category Category,
    bool InstantBookEnabled,
    List<HandoverOption> HandoverOptions,
    int? Mileage,
    string? Transmission,
    int? Bedrooms,
    int? Megapixels,
    string? Brand,
    string? Condition
) : IRequest<ItemDto>;
