using Borro.Application.Common.Interfaces;
using Borro.Application.Items;
using Borro.Application.Items.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries.GetMyItems;

public class GetMyItemsQueryHandler : IRequestHandler<GetMyItemsQuery, List<ItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMyItemsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ItemDto>> Handle(GetMyItemsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Items
            .Include(i => i.Lender)
            .Where(i => i.LenderId == request.LenderId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => i.ToDto(i.Lender))
            .ToListAsync(cancellationToken);
    }
}
