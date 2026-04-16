using Borro.Application.Common.Interfaces;
using Borro.Application.Items.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Queries;

public sealed class SearchItemsQueryHandler : IRequestHandler<SearchItemsQuery, SearchItemsResult>
{
    private readonly IApplicationDbContext _context;

    public SearchItemsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<SearchItemsResult> Handle(SearchItemsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Items.Include(i => i.Owner).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var term = request.SearchText.Trim().ToLower();
            query = query.Where(i =>
                i.Title.ToLower().Contains(term) ||
                i.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var loc = request.Location.Trim().ToLower();
            query = query.Where(i => i.Location.ToLower().Contains(loc));
        }

        if (request.Category.HasValue)
            query = query.Where(i => i.Category == request.Category.Value);

        if (request.MinPrice.HasValue)
            query = query.Where(i => i.DailyPrice >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(i => i.DailyPrice <= request.MaxPrice.Value);

        if (request.AvailableFrom.HasValue && request.AvailableTo.HasValue)
        {
            var from = DateTime.SpecifyKind(request.AvailableFrom.Value.Date, DateTimeKind.Utc);
            var to = DateTime.SpecifyKind(request.AvailableTo.Value.Date, DateTimeKind.Utc);
            query = query.Where(i => !i.BlockedDates.Any(d => d.DateUtc >= from && d.DateUtc <= to));
        }

        if (request.HandoverOptions is { Count: > 0 })
        {
            var optionStrings = request.HandoverOptions.Select(o => o.ToString()).ToList();
            query = query.Where(i => optionStrings.Any(opt => i.HandoverOptionsRaw.Contains(opt)));
        }

        if (request.MaxMileage.HasValue)
        {
            var maxMileage = request.MaxMileage.Value;
            query = query.Where(i => i.Attributes.Mileage == null || i.Attributes.Mileage <= maxMileage);
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
            query = query.Where(i => i.Attributes.Bedrooms != null && i.Attributes.Bedrooms >= minBedrooms);
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
