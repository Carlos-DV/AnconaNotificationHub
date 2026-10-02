using System.Globalization;
using AnconaNotificationHub.Contracts;
using Application.Abstractions.Identity;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application.Features.Connections;

/// <summary>
/// Grupos de identidad de una conexión: user y all siempre; sucursales y permisos si el resolver responde.
/// Si falla, la conexión sigue con user y all (mejor recibir algo que nada).
/// </summary>
public sealed class ResolveConnectionGroupsHandler(
    IUserGroupResolver resolver,
    ILogger<ResolveConnectionGroupsHandler> logger)
{
    public async Task<IReadOnlyList<GroupName>> HandleAsync(string tenant, int userId, CancellationToken cancellationToken)
    {
        var groups = new List<GroupName>();
        Add(groups, tenant, AudienceType.User, userId.ToString(CultureInfo.InvariantCulture));
        Add(groups, tenant, AudienceType.All, null);

        try
        {
            var userGroups = await resolver.ResolveAsync(tenant, userId, cancellationToken);
            foreach (var branchCode in userGroups.BranchCodes)
                Add(groups, tenant, AudienceType.Branch, branchCode);
            foreach (var permission in userGroups.Permissions)
                Add(groups, tenant, AudienceType.Perm, permission);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex,
                "No se pudieron resolver sucursales y permisos del usuario {UserId} ({Tenant}); se conecta solo con user y all",
                userId, tenant);
        }

        return groups.Distinct().ToList();
    }

    private void Add(List<GroupName> groups, string tenant, string type, string? value)
    {
        if (GroupName.TryCreate(tenant, type, value, out var group))
            groups.Add(group);
        else
            logger.LogDebug("Grupo {Type}:{Value} inválido para {Tenant}; se omite", type, value, tenant);
    }
}
