using System.Globalization;

namespace AnconaNotificationHub.Contracts;

/// <summary>Destino de un evento. Usar las fábricas en lugar de armar Type/Value a mano.</summary>
public sealed class Audience
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    /// <summary>Un usuario por su uid del JWT.</summary>
    public static Audience User(int userId) =>
        new() { Type = AudienceType.User, Value = userId.ToString(CultureInfo.InvariantCulture) };

    /// <summary>Todos los usuarios de una sucursal, por su código de 3 dígitos (U_SO1_01SUCURSAL).</summary>
    public static Audience Branch(string branchCode) => new() { Type = AudienceType.Branch, Value = branchCode };

    /// <summary>Todos los usuarios con un permiso (ClaimValue de RoleClaims/UserClaims).</summary>
    public static Audience Perm(string permission) => new() { Type = AudienceType.Perm, Value = permission };

    /// <summary>Quien tenga abierta una pantalla o colección (ej. "warranty.returns").</summary>
    public static Audience Topic(string topic) => new() { Type = AudienceType.Topic, Value = topic };

    /// <summary>Quien tenga abierto el detalle de una entidad: "{entityType}:{id}".</summary>
    public static Audience Entity(string entityType, string id) =>
        new() { Type = AudienceType.Entity, Value = $"{entityType}:{id}" };

    /// <summary>Todos los usuarios conectados del tenant.</summary>
    public static Audience All() => new() { Type = AudienceType.All };
}
