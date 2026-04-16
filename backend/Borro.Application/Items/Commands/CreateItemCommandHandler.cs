using Borro.Application.Items.DTOs;
using Borro.Domain.Entities;
using Borro.Infrastructure.Persistence;
using MediatR;

namespace Borro.Application.Items.Commands;

public sealed class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, ItemDto>
{
    private readonly BorroDbContext _context;

    public CreateItemCommandHandler(BorroDbContext context)
    {
        _context = context;
    }

    public async Task<ItemDto> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
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

        _context.Items.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }
}
