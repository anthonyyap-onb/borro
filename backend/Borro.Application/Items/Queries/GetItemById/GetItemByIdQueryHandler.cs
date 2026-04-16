using Borro.Application.Common.Interfaces;
using Borro.Application.Items;
using Borro.Application.Items.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries.GetItemById;

public class GetItemByIdQueryHandler : IRequestHandler<GetItemByIdQuery, ItemDto?>
{
    private readonly IApplicationDbContext _context;

    public GetItemByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ItemDto?> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .Include(i => i.Lender)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);

        return item?.ToDto(item.Lender);
    }
}
