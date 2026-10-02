using Application.Abstractions.Identity;
using Application.Settings;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence;

/// <summary>
/// Permisos desde la BD raíz (mismo modelo que PermissionService de system-api) y sucursales desde la BD
/// del tenant (UserBranchOffice, como sale-api). Cacheado por (tenant, uid).
/// </summary>
internal sealed class UserGroupResolver(
    RootDatabase root,
    TenantConnectionProvider tenantConnections,
    IMemoryCache cache,
    IOptions<NotificationSetting> options) : IUserGroupResolver
{
    private const string PermissionsSql = """
        SELECT rc.ClaimValue
        FROM UserRoles ur
        JOIN RoleClaims rc ON rc.RoleId = ur.RoleId
        WHERE ur.UserId = @userId AND rc.ClaimType = 'permission' AND rc.ClaimValue IS NOT NULL
        UNION
        SELECT uc.ClaimValue
        FROM UserClaims uc
        WHERE uc.UserId = @userId AND uc.ClaimType = 'permission' AND uc.ClaimValue IS NOT NULL
        """;

    private const string BranchesSql = """
        SELECT bo.U_SO1_01SUCURSAL
        FROM UserBranchOffice ubo
        JOIN BranchOffice bo ON bo.pkBranchOffice = ubo.BranchOfficeId
        WHERE ubo.UserId = @userId AND bo.U_SO1_01SUCURSAL IS NOT NULL
        """;

    public async Task<UserGroups> ResolveAsync(string tenant, int userId, CancellationToken cancellationToken)
    {
        var key = $"user-groups:{tenant}:{userId}";
        if (cache.TryGetValue(key, out UserGroups? cached) && cached is not null)
            return cached;

        List<string> permissions;
        await using (var rootConnection = new SqlConnection(root.ConnectionString))
        {
            permissions = (await rootConnection.QueryAsync<string>(
                new CommandDefinition(PermissionsSql, new { userId }, cancellationToken: cancellationToken))).ToList();
        }

        var tenantConnectionString = await tenantConnections.GetAsync(tenant, cancellationToken);
        List<string> branchCodes;
        await using (var tenantConnection = new SqlConnection(tenantConnectionString))
        {
            branchCodes = (await tenantConnection.QueryAsync<string>(
                new CommandDefinition(BranchesSql, new { userId }, cancellationToken: cancellationToken))).ToList();
        }

        var result = new UserGroups(branchCodes, permissions);
        cache.Set(key, result, TimeSpan.FromMinutes(options.Value.UserGroupsCacheMinutes));
        return result;
    }
}
