using Borro.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries;

public sealed class GetItemAvailabilityQueryHandler : IRequestHandler<GetItemAvailabilityQuery, List<DateTime>?>
{
    private readonly BorroDbContext _context;

    public GetItemAvailabilityQueryHandler(BorroDbContext context)
    {
        _context = context;
    }

    public async Task<List<DateTime>?> Handle(GetItemAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var itemExists = await _context.Items
            .AsNoTracking()
            .AnyAsync(i => i.Id == request.ItemId, cancellationToken);

        if (!itemExists)
            return null;

        var blockedDates = await _context.ItemBlockedDates
            .AsNoTracking()
            .Where(d => d.ItemId == request.ItemId)
            .Select(d => d.DateUtc)
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);

        return blockedDates;
    }
}
