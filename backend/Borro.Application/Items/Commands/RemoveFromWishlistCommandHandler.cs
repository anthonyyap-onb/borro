using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands;

public sealed class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, bool?>
{
    private readonly IApplicationDbContext _context;

    public RemoveFromWishlistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool?> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        var itemExists = await _context.Items
            .AnyAsync(i => i.Id == request.ItemId, cancellationToken);

        if (!itemExists)
            return null;

        var entry = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == request.UserId && w.ItemId == request.ItemId, cancellationToken);

        if (entry is null)
            return false;

        _context.Wishlists.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
