using Borro.Application.Health.Queries;
using MediatR;

namespace Borro.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health").WithTags("Health");

        group.MapGet("", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetHealthQuery());
            return Results.Ok(result);
        })
        .WithName("GetHealth")
        .Produces<HealthResult>(StatusCodes.Status200OK);

        return app;
    }
}
