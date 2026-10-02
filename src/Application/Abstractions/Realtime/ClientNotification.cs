using System.Text.Json;

namespace Application.Abstractions.Realtime;

/// <summary>Lo único que llega al navegador. Sin tenant, audience ni source.</summary>
public sealed record ClientNotification(Guid EventId, string EventType, DateTimeOffset OccurredAt, JsonElement Payload);
