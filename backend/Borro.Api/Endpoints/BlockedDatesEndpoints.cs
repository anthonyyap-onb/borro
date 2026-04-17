using System.Security.Claims;
using Borro.Application.Items.BlockedDates.Commands.BlockDates;
using Borro.Application.Items.BlockedDates.Commands.UnblockDates;
using Borro.Application.Items.BlockedDates.Queries.GetBlockedDates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Borro.Api.Endpoints;

public static class BlockedDatesEndpoints
{
    public static IEndpointRouteBuilder MapBlockedDatesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/items/{id:guid}/blocked-dates").WithTags("BlockedDates");

        // GET /api/items/{id}/blocked-dates — public; used by renters to check availability
        group.MapGet("/", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetBlockedDatesQuery(id), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        // POST /api/items/{id}/blocked-dates — lender only
        group.MapPost("/", async (
            Guid id,
            [FromBody] BlockedDatesRequest req,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var requesterId = ParseUserId(user);
            if (requesterId is null)
                return Results.Unauthorized();

            try
            {
                await mediator.Send(new BlockDatesCommand(id, requesterId.Value, req.Dates), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }).RequireAuthorization();

        // DELETE /api/items/{id}/blocked-dates — lender only
        group.MapDelete("/", async (
            Guid id,
            [FromBody] BlockedDatesRequest req,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var requesterId = ParseUserId(user);
            if (requesterId is null)
                return Results.Unauthorized();

            try
            {
                await mediator.Send(new UnblockDatesCommand(id, requesterId.Value, req.Dates), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
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

    private record BlockedDatesRequest(DateOnly[] Dates);
}
