using Borro.Application.Common.Interfaces;
using Borro.Application.Items.DTOs;
using Borro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands.CreateItem;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, ItemDto>
{
    private readonly IApplicationDbContext _db;

    public CreateItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ItemDto> Handle(CreateItemCommand cmd, CancellationToken ct)
    {
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == cmd.OwnerId, ct)
            ?? throw new InvalidOperationException($"User {cmd.OwnerId} not found.");

        var item = Item.Create(cmd.OwnerId, cmd.Title, cmd.Description, cmd.DailyPrice, cmd.Location, cmd.Category);

        item.Update(
            cmd.Title,
            cmd.Description,
            cmd.DailyPrice,
            cmd.Location,
            cmd.Category,
            cmd.InstantBookEnabled,
            cmd.HandoverOptions,
            new ItemAttributes
            {
                Mileage = cmd.Mileage,
                Transmission = cmd.Transmission,
                Bedrooms = cmd.Bedrooms,
                Megapixels = cmd.Megapixels,
                Brand = cmd.Brand,
                Condition = cmd.Condition
            });

        item.Owner = owner;

        _db.Items.Add(item);
        await _db.SaveChangesAsync(ct);

        return item.ToDto();
    }
}
