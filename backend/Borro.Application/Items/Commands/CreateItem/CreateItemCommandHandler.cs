using Borro.Application.Common.Interfaces;
using Borro.Application.Items.DTOs;
using Borro.Domain.Entities;
using Borro.Domain.Enums;
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

        var item = new Item
        {
            Id = Guid.NewGuid(),
            OwnerId = cmd.OwnerId,
            Title = cmd.Title,
            Description = cmd.Description,
            DailyPrice = cmd.DailyPrice,
            Location = cmd.Location,
            Category = cmd.Category,
            Attributes = new ItemAttributes { Values = cmd.Attributes },
            InstantBookEnabled = cmd.InstantBookEnabled,
            HandoverOptions = cmd.HandoverOptions
                .Select(s => Enum.TryParse<HandoverOption>(s, out var v)
                    ? v
                    : throw new InvalidOperationException($"Invalid handover option: '{s}'."))
                .ToList(),
            ImageUrls = new List<string>(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync(ct);

        return ToDto(item, owner);
    }

    internal static ItemDto ToDto(Item item, User owner) => new(
        Id: item.Id,
        OwnerId: item.OwnerId,
        OwnerName: $"{owner.FirstName} {owner.LastName}",
        Title: item.Title,
        Description: item.Description,
        DailyPrice: item.DailyPrice,
        Location: item.Location,
        Category: item.Category,
        Attributes: item.Attributes.Values,
        InstantBookEnabled: item.InstantBookEnabled,
        HandoverOptions: item.HandoverOptions.Select(h => h.ToString()).ToList(),
        ImageUrls: item.ImageUrls,
        CreatedAtUtc: item.CreatedAtUtc
    );
}
