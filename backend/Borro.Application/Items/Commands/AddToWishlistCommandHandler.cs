using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands;

public sealed class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, bool?>
{
    private readonly IApplicationDbContext _context;

    public AddToWishlistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool?> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var itemExists = await _context.Items
            .AnyAsync(i => i.Id == request.ItemId, cancellationToken);

        if (!itemExists)
            return null;

        var alreadyWishlisted = await _context.Wishlists
            .AnyAsync(w => w.UserId == request.UserId && w.ItemId == request.ItemId, cancellationToken);

        if (alreadyWishlisted)
            return false;

        _context.Wishlists.Add(Wishlist.Create(request.UserId, request.ItemId));
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
