using Borro.Application.Common.Interfaces;
using Borro.Application.Items.DTOs;
using Borro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands;

public sealed class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, ItemDto>
{
    private readonly IApplicationDbContext _context;

    public CreateItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<ItemDto> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.OwnerId, cancellationToken)
            ?? throw new InvalidOperationException($"User {request.OwnerId} not found.");

        var item = Item.Create(
            request.OwnerId,
            request.Title,
            request.Description,
            request.DailyPrice,
            request.Location,
            request.Category);

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

        item.Owner = owner;

        _context.Items.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }
}
