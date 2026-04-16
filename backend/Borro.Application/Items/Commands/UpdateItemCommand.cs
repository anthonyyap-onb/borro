using Borro.Application.Items.DTOs;
using Borro.Domain.Enums;
using MediatR;

namespace Borro.Application.Items.Commands;

public sealed record UpdateItemCommand(
    Guid ItemId,
    Guid RequestingUserId,
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
) : IRequest<ItemDto?>;
