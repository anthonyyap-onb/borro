using Borro.Application.Items.DTOs;
using Borro.Domain.Enums;
using MediatR;

namespace Borro.Application.Items.Queries.SearchItems;

public record SearchItemsQuery(
    Category? Category,
    string? Location,
    decimal? MaxDailyPrice
) : IRequest<List<ItemDto>>;
