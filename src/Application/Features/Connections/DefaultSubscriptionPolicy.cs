using Application.Abstractions.Identity;

namespace Application.Features.Connections;

/// <summary>
/// Permite cualquier topic/entity dentro del tenant (payload pequeño, sistema interno).
/// Si un topic llega a llevar datos sensibles, reemplazar por una política que exija un permiso.
/// </summary>
internal sealed class DefaultSubscriptionPolicy : ISubscriptionPolicy
{
    public bool IsAllowed(string tenant, string type, string value) => true;
}
