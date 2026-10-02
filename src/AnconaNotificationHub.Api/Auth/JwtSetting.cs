namespace AnconaNotificationHub.Api.Auth;

public sealed class JwtSetting
{
    public const string SectionName = "JWTSettings";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
