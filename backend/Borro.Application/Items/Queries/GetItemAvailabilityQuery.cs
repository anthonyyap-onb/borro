using MediatR;

namespace Borro.Application.Items.Queries;

/// <summary>Returns blocked dates for an item as UTC DateTime list, or null if item not found.</summary>
public sealed record GetItemAvailabilityQuery(Guid ItemId) : IRequest<List<DateTime>?>;
