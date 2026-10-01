namespace AnconaNotificationHub.Contracts;

/// <summary>
/// Tipos de audiencia. user/all/branch/perm los une el servidor al conectar;
/// topic/entity los pide el frontend con Subscribe.
/// </summary>
public static class AudienceType
{
    public const string User = "user";
    public const string Branch = "branch";
    public const string Perm = "perm";
    public const string Topic = "topic";
    public const string Entity = "entity";
    public const string All = "all";
}
