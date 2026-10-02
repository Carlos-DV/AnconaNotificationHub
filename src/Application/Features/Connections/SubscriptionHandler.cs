using AnconaNotificationHub.Contracts;
using Application.Abstractions.Identity;
using Application.Exceptions;
using Application.Settings;
using Domain;
using Microsoft.Extensions.Options;

namespace Application.Features.Connections;

/// <summary>
/// Reglas de Subscribe/Unsubscribe. Solo topic y entity: los grupos de identidad los pone el servidor,
/// así nadie puede escuchar lo de otro usuario o sucursal. El tenant viene del token, nunca del cliente.
/// </summary>
public sealed class SubscriptionHandler(ISubscriptionPolicy policy, IOptions<NotificationSetting> options)
{
    private static readonly HashSet<string> SubscribableTypes = [AudienceType.Topic, AudienceType.Entity];

    public GroupName ValidateSubscribe(string tenant, string? type, string? value, IReadOnlyCollection<string> currentGroups)
    {
        var normalizedType = Normalize(type);
        var group = Build(tenant, normalizedType, value)
            ?? throw new SubscriptionRejectedException($"Suscripción inválida '{type}:{value}'");

        if (currentGroups.Contains(group.Value))
            return group;

        var max = options.Value.MaxSubscriptionsPerConnection;
        if (currentGroups.Count >= max)
            throw new SubscriptionRejectedException($"Límite de {max} suscripciones por conexión alcanzado");

        if (!policy.IsAllowed(tenant, normalizedType, Normalize(value)))
            throw new SubscriptionRejectedException($"Suscripción no permitida '{type}:{value}'");

        return group;
    }

    public GroupName? TryBuildUnsubscribe(string tenant, string? type, string? value) =>
        Build(tenant, Normalize(type), value);

    private static GroupName? Build(string tenant, string normalizedType, string? value) =>
        SubscribableTypes.Contains(normalizedType) && GroupName.TryCreate(tenant, normalizedType, value, out var group)
            ? group
            : null;

    private static string Normalize(string? part) => (part ?? string.Empty).Trim().ToLowerInvariant();
}
