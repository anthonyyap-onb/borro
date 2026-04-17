using MediatR;

namespace Borro.Application.Items.BlockedDates.Queries.GetBlockedDates;

public record GetBlockedDatesQuery(Guid ItemId) : IRequest<BlockedDatesDto>;

public record BlockedDatesDto(Guid ItemId, List<DateOnly> Dates);
