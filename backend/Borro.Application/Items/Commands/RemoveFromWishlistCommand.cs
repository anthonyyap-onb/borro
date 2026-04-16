using MediatR;

namespace Borro.Application.Items.Commands;

/// <summary>Returns true if removed, false if it was not in the wishlist, null if item not found.</summary>
public sealed record RemoveFromWishlistCommand(Guid UserId, Guid ItemId) : IRequest<bool?>;
