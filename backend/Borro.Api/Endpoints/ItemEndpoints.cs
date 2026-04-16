using Borro.Application.Items.Commands;
using Borro.Application.Items.Queries;
using Borro.Domain.Enums;
using MediatR;
using System.Security.Claims;

namespace Borro.Api.Endpoints;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/items").WithTags("Items");

        // GET /api/items/search
        group.MapGet("/search", async (
            string? searchText, string? location, string? category, decimal? minPrice, decimal? maxPrice,
            DateTime? availableFrom, DateTime? availableTo,
            int page = 1, int pageSize = 20,
            IMediator mediator = null!, CancellationToken ct = default) =>
        {
            Category? categoryEnum = null;
            if (!string.IsNullOrWhiteSpace(category))
            {
                if (!Enum.TryParse<Category>(category, true, out var parsed))
                    return Results.BadRequest(new { error = $"Invalid category '{category}'." });
                categoryEnum = parsed;
            }

            var results = await mediator.Send(
                new SearchItemsQuery(searchText, location, categoryEnum, minPrice, maxPrice,
                    availableFrom, availableTo, null, null, null, null, null, null, page, pageSize), ct);
            return Results.Ok(results);
        });

        // GET /api/items/{id}
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var item = await mediator.Send(new GetItemByIdQuery(id), ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        // GET /api/items/{id}/availability
        group.MapGet("/{id:guid}/availability", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dates = await mediator.Send(new GetItemAvailabilityQuery(id), ct);
            return dates is null ? Results.NotFound() : Results.Ok(dates);
        });

        // POST /api/items  (requires auth)
        group.MapPost("/", async (CreateItemRequest req, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var ownerIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");
            if (ownerIdClaim is null || !Guid.TryParse(ownerIdClaim, out var ownerId))
                return Results.Unauthorized();

            if (!Enum.TryParse<Category>(req.Category, true, out var category))
                return Results.BadRequest(new { error = $"Invalid category '{req.Category}'." });

            var handoverOptions = req.HandoverOptions
                .Select(s => Enum.TryParse<HandoverOption>(s, true, out var v) ? (HandoverOption?)v : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            try
            {
                var result = await mediator.Send(new CreateItemCommand(
                    ownerId, req.Title, req.Description, req.DailyPrice,
                    req.Location, category, req.InstantBookEnabled, handoverOptions,
                    req.Mileage, req.Transmission, req.Bedrooms, req.Megapixels, req.Brand, req.Condition), ct);
                return Results.Created($"/api/items/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // POST /api/items/images  (requires auth, multipart/form-data)
        group.MapPost("/images", async (
            HttpRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            if (!request.Form.TryGetValue("itemId", out var itemIdStr)
                || !Guid.TryParse(itemIdStr, out var itemId))
                return Results.BadRequest(new { error = "itemId is required." });

            var file = request.Form.Files.FirstOrDefault();
            if (file is null)
                return Results.BadRequest(new { error = "No file provided." });

            string[] allowedTypes = ["image/jpeg", "image/png", "image/webp"];
            if (!allowedTypes.Contains(file.ContentType))
                return Results.BadRequest(new { error = "Only JPEG, PNG, and WebP images are allowed." });

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(new { error = "File size must not exceed 10 MB." });

            try
            {
                await using var stream = file.OpenReadStream();
                var url = await mediator.Send(
                    new UploadItemImageCommand(itemId, userId, stream, file.FileName, file.ContentType), ct);
                return Results.Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }).RequireAuthorization();

        return app;
    }

    private record CreateItemRequest(
        string Title,
        string Description,
        decimal DailyPrice,
        string Location,
        string Category,
        bool InstantBookEnabled,
        List<string> HandoverOptions,
        int? Mileage = null,
        string? Transmission = null,
        int? Bedrooms = null,
        int? Megapixels = null,
        string? Brand = null,
        string? Condition = null
    );
}
