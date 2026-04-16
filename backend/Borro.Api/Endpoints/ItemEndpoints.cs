using Borro.Api.Extensions;
using Borro.Application.Items.Commands;
using Borro.Application.Items.DTOs;
using Borro.Application.Items.Queries;
using Borro.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Borro.Api.Endpoints;

/// <summary>
/// Item management endpoints.
///
/// Auth note: JWT authentication is not yet wired up (Phase 1 auth integration is pending).
/// Endpoints that mutate owner-scoped resources accept a requestingUserId query parameter as
/// a temporary stand-in. When Phase 1 auth middleware is added, replace this with
/// HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) and call .RequireAuthorization().
/// </summary>
public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/items").WithTags("Items");

        // ── CRUD ──────────────────────────────────────────────────────────────────
        group.MapPost("", CreateItem)
            .WithName("CreateItem")
            .Produces<ItemDto>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("{id:guid}", GetItemById)
            .WithName("GetItemById")
            .Produces<ItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("{id:guid}", UpdateItem)
            .WithName("UpdateItem")
            .Produces<ItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("{id:guid}", DeleteItem)
            .WithName("DeleteItem")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        // ── Search ────────────────────────────────────────────────────────────────
        group.MapGet("search", SearchItems)
            .WithName("SearchItems")
            .Produces<SearchItemsResult>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        // ── Availability ──────────────────────────────────────────────────────────
        group.MapGet("{id:guid}/availability", GetAvailability)
            .WithName("GetItemAvailability")
            .Produces<List<DateTime>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // ── Blocked dates ─────────────────────────────────────────────────────────
        group.MapPost("{id:guid}/blocked-dates", AddBlockedDate)
            .WithName("AddBlockedDate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("{id:guid}/blocked-dates", RemoveBlockedDate)
            .WithName("RemoveBlockedDate")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        // ── Image upload ──────────────────────────────────────────────────────────
        group.MapPost("images", UploadImage)
            .WithName("UploadItemImage")
            .DisableAntiforgery()
            .Produces<UploadImageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        // ── Wishlist ──────────────────────────────────────────────────────────────
        group.MapPost("{id:guid}/wishlist", AddToWishlist)
            .WithName("AddToWishlist")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{id:guid}/wishlist", RemoveFromWishlist)
            .WithName("RemoveFromWishlist")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // ── Handlers ─────────────────────────────────────────────────────────────────

    private static async Task<IResult> CreateItem(
        [FromBody] CreateItemRequest body,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreateItemCommand(
            body.OwnerId,
            body.Title,
            body.Description ?? string.Empty,
            body.DailyPrice,
            body.Location,
            body.Category,
            body.InstantBookEnabled,
            body.HandoverOptions ?? new List<HandoverOption>(),
            body.Mileage,
            body.Transmission,
            body.Bedrooms,
            body.Megapixels,
            body.Brand,
            body.Condition);

        try
        {
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/items/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.ToErrorDictionary());
        }
    }

    private static async Task<IResult> GetItemById(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetItemByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateItem(
        Guid id,
        [FromBody] UpdateItemRequest body,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new UpdateItemCommand(
            id,
            body.RequestingUserId,
            body.Title,
            body.Description ?? string.Empty,
            body.DailyPrice,
            body.Location,
            body.Category,
            body.InstantBookEnabled,
            body.HandoverOptions ?? new List<HandoverOption>(),
            body.Mileage,
            body.Transmission,
            body.Bedrooms,
            body.Megapixels,
            body.Brand,
            body.Condition);

        try
        {
            var result = await mediator.Send(command, ct);
            if (result is null) return Results.NotFound();
            return Results.Ok(result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.ToErrorDictionary());
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> DeleteItem(
        Guid id,
        [FromQuery] Guid requestingUserId,
        IMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var deleted = await mediator.Send(new DeleteItemCommand(id, requestingUserId), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> SearchItems(
        IMediator mediator,
        CancellationToken ct,
        [FromQuery] string? searchText = null,
        [FromQuery] string? location = null,
        [FromQuery] Category? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] DateTime? availableFrom = null,
        [FromQuery] DateTime? availableTo = null,
        [FromQuery] string? handoverOptions = null,
        [FromQuery] int? maxMileage = null,
        [FromQuery] string? transmission = null,
        [FromQuery] int? minBedrooms = null,
        [FromQuery] string? brand = null,
        [FromQuery] string? condition = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Parse comma-separated handover options (e.g. "0,1")
        List<HandoverOption>? parsedHandoverOptions = null;
        if (!string.IsNullOrWhiteSpace(handoverOptions))
        {
            parsedHandoverOptions = handoverOptions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var v) ? (HandoverOption?)v : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();
        }

        var query = new SearchItemsQuery(
            searchText,
            location,
            category,
            minPrice,
            maxPrice,
            availableFrom.HasValue ? DateTime.SpecifyKind(availableFrom.Value, DateTimeKind.Utc) : null,
            availableTo.HasValue ? DateTime.SpecifyKind(availableTo.Value, DateTimeKind.Utc) : null,
            parsedHandoverOptions,
            maxMileage,
            transmission,
            minBedrooms,
            brand,
            condition,
            page,
            pageSize);

        try
        {
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(ex.ToErrorDictionary());
        }
    }

    private static async Task<IResult> GetAvailability(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetItemAvailabilityQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> AddBlockedDate(
        Guid id,
        [FromBody] BlockedDateRequest body,
        IMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new AddBlockedDateCommand(id, body.RequestingUserId, body.DateUtc), ct);

            if (result is null) return Results.NotFound();
            return Results.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> RemoveBlockedDate(
        Guid id,
        [FromBody] BlockedDateRequest body,
        IMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new RemoveBlockedDateCommand(id, body.RequestingUserId, body.DateUtc), ct);

            if (result is null) return Results.NotFound();
            if (result == false) return Results.NotFound();
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> UploadImage(
        [FromForm] IFormFile file,
        [FromForm] Guid itemId,
        [FromForm] Guid requestingUserId,
        IMediator mediator,
        CancellationToken ct)
    {
        if (file.Length == 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["File must not be empty."]
            });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = [$"Unsupported content type '{file.ContentType}'. Allowed: jpeg, png, webp, gif."]
            });

        using var stream = file.OpenReadStream();
        try
        {
            var url = await mediator.Send(
                new UploadItemImageCommand(itemId, requestingUserId, stream, file.FileName, file.ContentType), ct);

            if (url is null) return Results.NotFound();
            return Results.Ok(new UploadImageResponse(url));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> AddToWishlist(
        Guid id,
        [FromBody] WishlistRequest body,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new AddToWishlistCommand(body.UserId, id), ct);

        return result switch
        {
            null => Results.NotFound(),
            false => Results.Conflict("Item is already in wishlist."),
            true => Results.Ok()
        };
    }

    private static async Task<IResult> RemoveFromWishlist(
        Guid id,
        [FromBody] WishlistRequest body,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveFromWishlistCommand(body.UserId, id), ct);

        return result switch
        {
            null => Results.NotFound(),
            false => Results.NotFound(),
            true => Results.NoContent()
        };
    }
}

// ── Request models ────────────────────────────────────────────────────────────

/// <summary>Request body for POST /api/items.</summary>
public sealed record CreateItemRequest(
    Guid OwnerId,
    string Title,
    string? Description,
    decimal DailyPrice,
    string Location,
    Category Category,
    bool InstantBookEnabled,
    List<HandoverOption>? HandoverOptions,
    int? Mileage,
    string? Transmission,
    int? Bedrooms,
    int? Megapixels,
    string? Brand,
    string? Condition
);

/// <summary>Request body for PUT /api/items/{id}.</summary>
public sealed record UpdateItemRequest(
    Guid RequestingUserId,
    string Title,
    string? Description,
    decimal DailyPrice,
    string Location,
    Category Category,
    bool InstantBookEnabled,
    List<HandoverOption>? HandoverOptions,
    int? Mileage,
    string? Transmission,
    int? Bedrooms,
    int? Megapixels,
    string? Brand,
    string? Condition
);

public sealed record BlockedDateRequest(Guid RequestingUserId, DateTime DateUtc);

public sealed record WishlistRequest(Guid UserId);

public sealed record UploadImageResponse(string Url);
