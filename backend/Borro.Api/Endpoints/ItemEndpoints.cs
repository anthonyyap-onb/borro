// backend/Borro.Api/Endpoints/ItemEndpoints.cs
using Borro.Application.Items.Commands.CreateItem;
using Borro.Application.Items.Commands.UploadItemImage;
using Borro.Application.Items.Queries.GetItem;
using Borro.Application.Items.Queries.SearchItems;
using MediatR;
using System.Security.Claims;

namespace Borro.Api.Endpoints;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/items").WithTags("Items");

        // GET /api/items/search?category=Tools&location=Portland&maxPrice=50
        group.MapGet("/search", async (
            string? category, string? location, decimal? maxPrice,
            IMediator mediator, CancellationToken ct) =>
        {
            var results = await mediator.Send(new SearchItemsQuery(category, location, maxPrice), ct);
            return Results.Ok(results);
        });

        // GET /api/items/{id}
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var item = await mediator.Send(new GetItemQuery(id), ct);
                return Results.Ok(item);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        });

        // GET /api/items/{id}/availability  — returns blocked dates (Phase 3 will populate this)
        group.MapGet("/{id:guid}/availability", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            // Stub: Phase 3 replaces this with real booked-date logic
            await Task.CompletedTask;
            return Results.Ok(Array.Empty<object>());
        });

        // POST /api/items  (requires auth)
        group.MapPost("/", async (CreateItemRequest req, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var ownerIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");
            if (ownerIdClaim is null || !Guid.TryParse(ownerIdClaim, out var ownerId))
                return Results.Unauthorized();

            try
            {
                var result = await mediator.Send(
                    new CreateItemCommand(
                        ownerId, req.Title, req.Description, req.DailyPrice,
                        req.Location, req.Category, req.Attributes,
                        req.InstantBookEnabled, req.HandoverOptions), ct);
                return Results.Created($"/api/items/{result.Id}", result);
            }
            catch (InvalidOperationException)
            {
                return Results.BadRequest(new { error = "Item could not be created." });
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
            catch (InvalidOperationException)
            {
                return Results.BadRequest(new { error = "Image could not be uploaded." });
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
        Dictionary<string, object> Attributes,
        bool InstantBookEnabled,
        List<string> HandoverOptions
    );
}
