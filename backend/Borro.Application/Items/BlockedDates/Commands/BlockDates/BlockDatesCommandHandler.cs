using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.BlockedDates.Commands.BlockDates;

public class BlockDatesCommandHandler : IRequestHandler<BlockDatesCommand>
{
    private readonly IApplicationDbContext _context;

    public BlockDatesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(BlockDatesCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Item '{request.ItemId}' not found.");

        if (item.LenderId != request.RequesterId)
            throw new UnauthorizedAccessException("Only the item owner can manage blocked dates.");

        var existingDates = await _context.BlockedDates
            .Where(b => b.ItemId == request.ItemId)
            .Select(b => b.Date)
            .ToHashSetAsync(cancellationToken);

        var newDates = request.Dates
            .Where(d => !existingDates.Contains(d))
            .Select(d => new BlockedDate
            {
                Id = Guid.NewGuid(),
                ItemId = request.ItemId,
                Date = d,
            });

        _context.BlockedDates.AddRange(newDates);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
