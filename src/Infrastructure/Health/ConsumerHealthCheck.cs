using Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Health;

internal sealed class ConsumerHealthCheck(ConsumerHealthState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.IsSubscribed
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("El consumer de RabbitMQ no está suscrito"));
}
