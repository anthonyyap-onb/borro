using Borro.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands;

public sealed class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand, bool>
{
    private readonly BorroDbContext _context;

    public DeleteItemCommandHandler(BorroDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);

        if (item is null)
            return false;

        if (item.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException($"User {request.RequestingUserId} does not own item {request.ItemId}.");

        _context.Items.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
