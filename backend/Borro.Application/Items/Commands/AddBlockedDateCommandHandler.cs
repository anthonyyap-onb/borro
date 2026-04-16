using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands;

public sealed class AddBlockedDateCommandHandler : IRequestHandler<AddBlockedDateCommand, bool?>
{
    private readonly IApplicationDbContext _context;

    public AddBlockedDateCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool?> Handle(AddBlockedDateCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .Include(i => i.BlockedDates)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);

        if (item is null)
            return null;

        if (item.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException($"User {request.RequestingUserId} does not own item {request.ItemId}.");

        var dateUtc = DateTime.SpecifyKind(request.DateUtc.Date, DateTimeKind.Utc);
        item.AddBlockedDate(dateUtc);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
