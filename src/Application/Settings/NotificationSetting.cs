namespace Application.Settings;

/// <summary>Límites del hub. Sección "Notification" de appsettings.json.</summary>
public sealed class NotificationSetting
{
    public const string SectionName = "Notification";

    /// <summary>Eventos más viejos que esto se descartan: ya no sirven para refrescar una pantalla.</summary>
    public int MaxEventAgeSeconds { get; set; } = 300;

    public int MaxPayloadBytes { get; set; } = 32 * 1024;

    public int MaxSubscriptionsPerConnection { get; set; } = 50;

    /// <summary>Minutos que se cachean sucursales y permisos de un usuario.</summary>
    public int UserGroupsCacheMinutes { get; set; } = 5;
}
