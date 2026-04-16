using MediatR;

namespace Borro.Application.Items.Commands;

/// <summary>
/// Removes a blocked date from an item.
/// Returns true if removed, false if the date was not blocked, null if item not found.
/// </summary>
public sealed record RemoveBlockedDateCommand(
    Guid ItemId,
    Guid RequestingUserId,
    DateTime DateUtc
) : IRequest<bool?>;
