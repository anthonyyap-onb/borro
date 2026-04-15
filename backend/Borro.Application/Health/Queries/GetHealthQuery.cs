using MediatR;

namespace Borro.Application.Health.Queries;

public record GetHealthQuery : IRequest<HealthResult>;

public record HealthResult(bool IsHealthy, string Message, DateTime CheckedAtUtc);
