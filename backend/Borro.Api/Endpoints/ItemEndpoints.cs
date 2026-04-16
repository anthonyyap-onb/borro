using System.Security.Claims;
using Borro.Application.Items.Commands.CreateItem;
using Borro.Application.Items.Queries.GetItemById;
using Borro.Application.Items.Queries.GetMyItems;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Borro.Api.Endpoints;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/items").WithTags("Items");

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
                var command = new CreateItemCommand(
                    lenderId.Value,
                    req.Title,
                    req.Description,
                    req.DailyPrice,
                    req.Category,
                    req.InstantBookEnabled,
                    req.DeliveryAvailable,
                    req.ImageUrls,
                    req.Attributes);

                var item = await mediator.Send(command, ct);
                return Results.Created($"/api/items/{item.Id}", item);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
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
        string Category,
        bool InstantBookEnabled,
        bool DeliveryAvailable,
        string[] ImageUrls,
        Dictionary<string, object> Attributes);
}
