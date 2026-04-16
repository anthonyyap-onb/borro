using MediatR;

namespace Borro.Application.Items.Commands;

/// <summary>
/// Adds a blocked date to an item.
/// Returns true on success, null if the item was not found.
/// </summary>
public sealed record AddBlockedDateCommand(
    Guid ItemId,
    Guid RequestingUserId,
    DateTime DateUtc
) : IRequest<bool?>;
