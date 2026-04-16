using Borro.Application.Items.DTOs;
using Borro.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries;

public sealed class SearchItemsQueryHandler : IRequestHandler<SearchItemsQuery, SearchItemsResult>
{
    private readonly BorroDbContext _context;

    public SearchItemsQueryHandler(BorroDbContext context)
    {
        _context = context;
    }

    public async Task<SearchItemsResult> Handle(SearchItemsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Items.AsNoTracking().AsQueryable();

        // ── Text search ────────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var term = request.SearchText.Trim().ToLower();
            query = query.Where(i =>
                i.Title.ToLower().Contains(term) ||
                i.Description.ToLower().Contains(term));
        }

        // ── Location ───────────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var loc = request.Location.Trim().ToLower();
            query = query.Where(i => i.Location.ToLower().Contains(loc));
        }

        // ── Category ───────────────────────────────────────────────────────────────
        if (request.Category.HasValue)
            query = query.Where(i => i.Category == request.Category.Value);

        // ── Price range ────────────────────────────────────────────────────────────
        if (request.MinPrice.HasValue)
            query = query.Where(i => i.DailyPrice >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(i => i.DailyPrice <= request.MaxPrice.Value);

        // ── Availability: exclude items blocked within the requested date range ───
        if (request.AvailableFrom.HasValue && request.AvailableTo.HasValue)
        {
            var from = DateTime.SpecifyKind(request.AvailableFrom.Value.Date, DateTimeKind.Utc);
            var to = DateTime.SpecifyKind(request.AvailableTo.Value.Date, DateTimeKind.Utc);

            query = query.Where(i => !i.BlockedDates.Any(d => d.DateUtc >= from && d.DateUtc <= to));
        }

        // ── Handover options: item must support at least one requested option ─────
        if (request.HandoverOptions is { Count: > 0 })
        {
            var options = request.HandoverOptions;
            query = query.Where(i => i.HandoverOptions.Any(h => options.Contains(h)));
        }

        // ── JSONB attribute filters (EF Core JSON query translation) ──────────────
        if (request.MaxMileage.HasValue)
        {
            var maxMileage = request.MaxMileage.Value;
            query = query.Where(i =>
                i.Attributes.Mileage == null ||
                i.Attributes.Mileage <= maxMileage);
        }

        if (!string.IsNullOrWhiteSpace(request.Transmission))
        {
            var trans = request.Transmission.Trim().ToLower();
            query = query.Where(i =>
                i.Attributes.Transmission != null &&
                i.Attributes.Transmission.ToLower() == trans);
        }

        if (request.MinBedrooms.HasValue)
        {
            var minBedrooms = request.MinBedrooms.Value;
            query = query.Where(i =>
                i.Attributes.Bedrooms != null &&
                i.Attributes.Bedrooms >= minBedrooms);
        }

        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            var brand = request.Brand.Trim().ToLower();
            query = query.Where(i =>
                i.Attributes.Brand != null &&
                i.Attributes.Brand.ToLower() == brand);
        }

        if (!string.IsNullOrWhiteSpace(request.Condition))
        {
            var condition = request.Condition.Trim().ToLower();
            query = query.Where(i =>
                i.Attributes.Condition != null &&
                i.Attributes.Condition.ToLower() == condition);
        }

        // ── Pagination ─────────────────────────────────────────────────────────────
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new SearchItemsResult(
            items.Select(i => i.ToDto()).ToList(),
            totalCount,
            page,
            pageSize);
    }
}
