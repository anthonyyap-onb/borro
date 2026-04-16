using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands;

public sealed class RemoveBlockedDateCommandHandler : IRequestHandler<RemoveBlockedDateCommand, bool?>
{
    private readonly IApplicationDbContext _context;

    public RemoveBlockedDateCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool?> Handle(RemoveBlockedDateCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .Include(i => i.BlockedDates)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);

        if (item is null)
            return null;

        if (item.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException($"User {request.RequestingUserId} does not own item {request.ItemId}.");

        var dateUtc = DateTime.SpecifyKind(request.DateUtc.Date, DateTimeKind.Utc);
        var removed = item.RemoveBlockedDate(dateUtc);

        if (removed)
            await _context.SaveChangesAsync(cancellationToken);

        return removed;
    }
}
