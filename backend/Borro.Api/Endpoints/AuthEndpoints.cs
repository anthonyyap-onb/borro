using Borro.Application.Auth.Commands;
using MediatR;

namespace Borro.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest req, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(
                    new RegisterCommand(req.Email, req.Password, req.FirstName, req.LastName), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/login", async (LoginRequest req, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new LoginCommand(req.Email, req.Password), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException)
            {
                // Return 401 without leaking whether email or password was wrong
                return Results.Unauthorized();
            }
        });

        group.MapPost("/google", async (GoogleLoginRequest req, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GoogleLoginCommand(req.AccessToken), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return app;
    }

    private record RegisterRequest(string Email, string Password, string FirstName, string LastName);
    private record LoginRequest(string Email, string Password);
    private record GoogleLoginRequest(string AccessToken);
}
