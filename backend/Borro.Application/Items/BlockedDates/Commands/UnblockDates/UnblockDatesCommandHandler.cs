using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.BlockedDates.Commands.UnblockDates;

public class UnblockDatesCommandHandler : IRequestHandler<UnblockDatesCommand>
{
    private readonly IApplicationDbContext _context;

    public UnblockDatesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UnblockDatesCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Item '{request.ItemId}' not found.");

        if (item.LenderId != request.RequesterId)
            throw new UnauthorizedAccessException("Only the item owner can manage blocked dates.");

        var datesToRemove = request.Dates.ToHashSet();

        var blocked = await _context.BlockedDates
            .Where(b => b.ItemId == request.ItemId && datesToRemove.Contains(b.Date))
            .ToListAsync(cancellationToken);

        if (blocked.Count > 0)
        {
            _context.BlockedDates.RemoveRange(blocked);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
