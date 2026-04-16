using Borro.Application.Common.Interfaces;
using Borro.Application.Items.DTOs;
using Borro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands;

public sealed class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand, ItemDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ItemDto?> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .Include(i => i.Owner)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);

        if (item is null)
            return null;

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
