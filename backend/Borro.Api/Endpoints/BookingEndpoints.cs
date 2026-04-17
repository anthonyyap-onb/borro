using Borro.Application.Bookings.Commands.CreateBooking;
using Borro.Application.Bookings.Commands.TransitionBooking;
using Borro.Application.Bookings.Queries.GetBooking;
using Borro.Application.Bookings.Queries.GetUserBookings;
using Borro.Application.Chat.Queries.GetMessages;
using Borro.Domain.Enums;
using MediatR;
using System.Security.Claims;

namespace Borro.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings").RequireAuthorization();

        // GET /api/bookings — returns bookings where user is renter OR lender
        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            var results = await mediator.Send(new GetUserBookingsQuery(userId.Value), ct);
            return Results.Ok(results);
        });

        // GET /api/bookings/{id}
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            try
            {
                var booking = await mediator.Send(new GetBookingQuery(id, userId.Value), ct);
                return Results.Ok(booking);
            }
            catch (InvalidOperationException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // POST /api/bookings
        group.MapPost("/", async (CreateBookingRequest req, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            try
            {
                var booking = await mediator.Send(
                    new CreateBookingCommand(req.ItemId, userId.Value, req.StartDateUtc, req.EndDateUtc), ct);
                return Results.Created($"/api/bookings/{booking.Id}", booking);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        // PATCH /api/bookings/{id}/status
        group.MapPatch("/{id:guid}/status", async (Guid id, TransitionRequest req, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            if (!Enum.TryParse<BookingStatus>(req.Status, out var targetStatus))
                return Results.BadRequest(new { error = "Invalid status value." });
            try
            {
                var booking = await mediator.Send(new TransitionBookingCommand(id, userId.Value, targetStatus), ct);
                return Results.Ok(booking);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // GET /api/bookings/{id}/messages
        group.MapGet("/{id:guid}/messages", async (Guid id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            try
            {
                var messages = await mediator.Send(new GetMessagesQuery(id, userId.Value), ct);
                return Results.Ok(messages);
            }
            catch (InvalidOperationException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private record CreateBookingRequest(Guid ItemId, DateTime StartDateUtc, DateTime EndDateUtc);
    private record TransitionRequest(string Status);
}
