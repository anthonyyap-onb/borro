using System.Security.Claims;
using Borro.Application.Items.Commands.CreateItem;
using Borro.Application.Items.Commands.UploadItemImage;
using Borro.Application.Items.Queries.GetItemById;
using Borro.Application.Items.Queries.GetMyItems;
using Borro.Application.Items.Queries.SearchItems;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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

        // GET /api/items/my — lender's own listings (auth required; must come before {id} route)
        group.MapGet("/my", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var lenderId = ParseUserId(user);
            if (lenderId is null)
                return Results.Unauthorized();

            var items = await mediator.Send(new GetMyItemsQuery(lenderId.Value), ct);
            return Results.Ok(items);
        }).RequireAuthorization();

        // GET /api/items/{id}
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var item = await mediator.Send(new GetItemByIdQuery(id), ct);
            return item is null ? Results.NotFound(new { error = "Item not found." }) : Results.Ok(item);
        });

        // GET /api/items/{id}/availability  — returns blocked dates (Phase 3 will populate this)
        group.MapGet("/{id:guid}/availability", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await Task.CompletedTask;
            return Results.Ok(Array.Empty<object>());
        });

        // POST /api/items — create listing (auth required)
        group.MapPost("/", async (
            [FromBody] CreateItemRequest req,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var lenderId = ParseUserId(user);
            if (lenderId is null)
                return Results.Unauthorized();

            try
            {
                var item = await mediator.Send(
                    new CreateItemCommand(
                        lenderId.Value,
                        req.Title,
                        req.Description,
                        req.DailyPrice,
                        req.Location,
                        req.Category,
                        req.Attributes,
                        req.InstantBookEnabled,
                        req.DeliveryAvailable,
                        req.HandoverOptions), ct);
                return Results.Created($"/api/items/{item.Id}", item);
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
            var userId = ParseUserId(user);
            if (userId is null)
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
                    new UploadItemImageCommand(itemId, userId.Value, stream, file.FileName, file.ContentType), ct);
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

    private static Guid? ParseUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private record CreateItemRequest(
        string Title,
        string Description,
        decimal DailyPrice,
        string Location,
        string Category,
        Dictionary<string, object> Attributes,
        bool InstantBookEnabled,
        bool DeliveryAvailable,
        List<string> HandoverOptions
    );
}
