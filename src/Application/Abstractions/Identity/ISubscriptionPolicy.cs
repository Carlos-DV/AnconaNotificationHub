namespace Application.Abstractions.Identity;

/// <summary>
/// Regla extra para Subscribe (después de validar tipo, formato y límite).
/// Punto de extensión: exigir un permiso por topic.
/// </summary>
public interface ISubscriptionPolicy
{
    bool IsAllowed(string tenant, string type, string value);
}
