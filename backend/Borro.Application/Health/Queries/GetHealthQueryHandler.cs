using MediatR;

namespace Borro.Application.Health.Queries;

public sealed class GetHealthQueryHandler : IRequestHandler<GetHealthQuery, HealthResult>
{
    private readonly TimeProvider _timeProvider;

    public GetHealthQueryHandler(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<HealthResult> Handle(GetHealthQuery request, CancellationToken cancellationToken)
    {
        var result = new HealthResult(
            IsHealthy: true,
            Message: "Borro API is up and running 🚀",
            CheckedAtUtc: _timeProvider.GetUtcNow().UtcDateTime
        );

        return Task.FromResult(result);
    }
}
