using Borro.Application.Common.Interfaces;
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
            .Include(i => i.Lender)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, ct)
            ?? throw new InvalidOperationException($"Item {request.ItemId} not found.");

        return item.ToDto(item.Lender);
    }
}
