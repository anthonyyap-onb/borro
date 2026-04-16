using Borro.Application.Items.DTOs;
using Borro.Domain.Enums;
using MediatR;

namespace Borro.Application.Items.Queries;

/// <summary>
/// Search/filter query for items. All parameters are optional.
/// Date filtering: excludes items with ANY blocked date in [StartDate, EndDate] (inclusive).
/// JSONB attribute filters are applied only when their respective field is non-null.
/// </summary>
public sealed record SearchItemsQuery(
    // Text search
    string? SearchText,
    // Location filter (partial match)
    string? Location,
    // Category filter
    Category? Category,
    // Price range
    decimal? MinPrice,
    decimal? MaxPrice,
    // Availability: exclude items blocked on any date in this range (UTC)
    DateTime? AvailableFrom,
    DateTime? AvailableTo,
    // Handover options: item must support at least one of these
    List<HandoverOption>? HandoverOptions,
    // JSONB attribute filters
    int? MaxMileage,
    string? Transmission,
    int? MinBedrooms,
    string? Brand,
    string? Condition,
    // Pagination
    int Page = 1,
    int PageSize = 20
) : IRequest<SearchItemsResult>;

public sealed record SearchItemsResult(
    List<ItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
