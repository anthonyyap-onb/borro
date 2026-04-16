using MediatR;

namespace Borro.Application.Items.Commands;

/// <summary>
/// Adds an item to a user's wishlist.
/// Returns true if added, false if already wishlisted, null if item not found.
/// </summary>
public sealed record AddToWishlistCommand(Guid UserId, Guid ItemId) : IRequest<bool?>;
