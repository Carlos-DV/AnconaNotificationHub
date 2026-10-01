using Dapper;
using Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Health;

internal sealed class RootDatabaseHealthCheck(RootDatabase root) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(root.ConnectionString);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("BD raíz no disponible", ex);
        }
    }
}
