using Borro.Application.Common.Interfaces;
using Borro.Application.Items.Commands.CreateItem;
using Borro.Application.Items.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries.SearchItems;

public class SearchItemsQueryHandler : IRequestHandler<SearchItemsQuery, List<ItemDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchItemsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ItemDto>> Handle(SearchItemsQuery q, CancellationToken ct)
    {
        var query = _db.Items.Include(i => i.Owner).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Category))
            query = query.Where(i => i.Category == q.Category);

        if (!string.IsNullOrWhiteSpace(q.Location))
            query = query.Where(i => i.Location.Contains(q.Location));

        if (q.MaxDailyPrice.HasValue)
            query = query.Where(i => i.DailyPrice <= q.MaxDailyPrice.Value);

        var items = await query.OrderByDescending(i => i.CreatedAtUtc).ToListAsync(ct);
        return items.Select(i => CreateItemCommandHandler.ToDto(i, i.Owner)).ToList();
    }
}
