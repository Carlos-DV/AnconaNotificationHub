using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AnconaNotificationHub.Contracts;

/// <summary>
/// Sobre que publican los servicios al exchange anc.notifications.&lt;env&gt; con routing key = EventType.
/// Crear siempre con <see cref="Create"/>.
/// </summary>
public sealed class NotificationEvent
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web);

    public Guid EventId { get; set; }

    /// <summary>"{dominio}.{entidad}.{acción}" en minúsculas, ej. "warranty.return.status-changed".</summary>
    public string EventType { get; set; } = string.Empty;

    public string Tenant { get; set; } = string.Empty;

    /// <summary>Servicio que publica. Solo para logs.</summary>
    public string Source { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public List<Audience> Audience { get; set; } = new();

    /// <summary>Objeto JSON pequeño (máx. 32 KB) con lo que la pantalla necesita para actualizarse.</summary>
    public JsonElement Payload { get; set; }

    /// <summary>Arma el sobre: genera EventId, fecha UTC y serializa el payload en camelCase.</summary>
    public static NotificationEvent Create(
        string eventType,
        string tenant,
        string source,
        IEnumerable<Audience> audience,
        object payload) => new()
    {
        EventId = Guid.NewGuid(),
        EventType = eventType,
        Tenant = tenant,
        Source = source,
        OccurredAt = DateTimeOffset.UtcNow,
        Audience = audience.ToList(),
        Payload = JsonSerializer.SerializeToElement(payload, PayloadOptions)
    };
}
