using Borro.Application.Items.DTOs;
using Borro.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries;

public sealed class GetItemByIdQueryHandler : IRequestHandler<GetItemByIdQuery, ItemDto?>
{
    private readonly BorroDbContext _context;

    public GetItemByIdQueryHandler(BorroDbContext context)
    {
        _context = context;
    }

    public async Task<ItemDto?> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);

        return item?.ToDto();
    }
}
