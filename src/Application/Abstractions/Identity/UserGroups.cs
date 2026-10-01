namespace Application.Abstractions.Identity;

/// <summary>Sucursales (código de 3 dígitos) y permisos (ClaimValue) de un usuario.</summary>
public sealed record UserGroups(IReadOnlyList<string> BranchCodes, IReadOnlyList<string> Permissions);
