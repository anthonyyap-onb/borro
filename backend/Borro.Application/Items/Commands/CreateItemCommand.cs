using Borro.Application.Items.DTOs;
using Borro.Domain.Enums;
using MediatR;

namespace Borro.Application.Items.Commands;

public sealed record CreateItemCommand(
    Guid OwnerId,
    string Title,
    string Description,
    decimal DailyPrice,
    string Location,
    Category Category,
    bool InstantBookEnabled,
    List<HandoverOption> HandoverOptions,
    // Attributes — all nullable, only relevant fields populated per category
    int? Mileage,
    string? Transmission,
    int? Bedrooms,
    int? Megapixels,
    string? Brand,
    string? Condition
) : IRequest<ItemDto>;
