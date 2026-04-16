using Borro.Application.Items.DTOs;
using Borro.Domain.Entities;
using Borro.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands;

public sealed class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand, ItemDto?>
{
    private readonly BorroDbContext _context;

    public UpdateItemCommandHandler(BorroDbContext context)
    {
        _context = context;
    }

    public async Task<ItemDto?> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);

        if (item is null)
            return null;

        // Only the owner may update the item.
        if (item.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException($"User {request.RequestingUserId} does not own item {request.ItemId}.");

        item.Update(
            request.Title,
            request.Description,
            request.DailyPrice,
            request.Location,
            request.Category,
            request.InstantBookEnabled,
            request.HandoverOptions,
            new ItemAttributes
            {
                Mileage = request.Mileage,
                Transmission = request.Transmission,
                Bedrooms = request.Bedrooms,
                Megapixels = request.Megapixels,
                Brand = request.Brand,
                Condition = request.Condition
            });

        await _context.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }
}
