using Borro.Application.Common.Interfaces;
using Borro.Application.Items.Commands.CreateItem;
using Borro.Application.Items.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries.GetItem;

public class GetItemQueryHandler : IRequestHandler<GetItemQuery, ItemDto>
{
    private readonly IApplicationDbContext _db;

    public GetItemQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ItemDto> Handle(GetItemQuery request, CancellationToken ct)
    {
        var item = await _db.Items
            .Include(i => i.Owner)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, ct)
            ?? throw new InvalidOperationException($"Item {request.ItemId} not found.");

        return CreateItemCommandHandler.ToDto(item, item.Owner);
    }
}
