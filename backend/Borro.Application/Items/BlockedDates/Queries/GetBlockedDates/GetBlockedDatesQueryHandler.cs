using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.BlockedDates.Queries.GetBlockedDates;

public class GetBlockedDatesQueryHandler : IRequestHandler<GetBlockedDatesQuery, BlockedDatesDto>
{
    private readonly IApplicationDbContext _context;

    public GetBlockedDatesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BlockedDatesDto> Handle(GetBlockedDatesQuery request, CancellationToken cancellationToken)
    {
        var itemExists = await _context.Items
            .AnyAsync(i => i.Id == request.ItemId, cancellationToken);

        if (!itemExists)
            throw new KeyNotFoundException($"Item '{request.ItemId}' not found.");

        var dates = await _context.BlockedDates
            .Where(b => b.ItemId == request.ItemId)
            .OrderBy(b => b.Date)
            .Select(b => b.Date)
            .ToListAsync(cancellationToken);

        return new BlockedDatesDto(request.ItemId, dates);
    }
}
