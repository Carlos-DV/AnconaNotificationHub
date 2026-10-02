namespace Infrastructure.Persistence;

/// <summary>Cadena de la BD raíz: Company, User, UserRoles, RoleClaims, UserClaims.</summary>
internal sealed record RootDatabase(string ConnectionString);
