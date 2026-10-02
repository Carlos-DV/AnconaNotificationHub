using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Persistence;

/// <summary>Cadena de la BD del tenant desde [Company] (Finbuckle resuelve el claim tenant por Identifier).</summary>
internal sealed class TenantConnectionProvider(RootDatabase root, IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private const string Sql = "SELECT ConnectionString FROM [Company] WHERE Identifier = @tenant";

    public async Task<string> GetAsync(string tenant, CancellationToken cancellationToken)
    {
        var key = $"tenant-connection:{tenant}";
        if (cache.TryGetValue(key, out string? cached) && cached is not null)
            return cached;

        await using var connection = new SqlConnection(root.ConnectionString);
        var connectionString = await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(Sql, new { tenant }, cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"No existe la compañía '{tenant}' en [Company] o no tiene ConnectionString");

        cache.Set(key, connectionString, CacheDuration);
        return connectionString;
    }
}
