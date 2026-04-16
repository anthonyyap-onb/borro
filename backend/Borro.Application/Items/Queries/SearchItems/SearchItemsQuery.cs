using Borro.Application.Items.DTOs;
using MediatR;

namespace Borro.Application.Items.Queries.SearchItems;

public record SearchItemsQuery(
    string? Category,
    string? Location,
    decimal? MaxDailyPrice
) : IRequest<List<ItemDto>>;
