using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnconaNotificationHub.Contracts;
using Application.Exceptions;
using Domain;

namespace Application.Features.DispatchEvent;

/// <summary>Valida el sobre y lo traduce a grupos únicos del tenant.</summary>
internal static partial class NotificationEventValidator
{
    private static readonly HashSet<string> AudienceTypes =
    [
        AudienceType.User, AudienceType.Branch, AudienceType.Perm,
        AudienceType.Topic, AudienceType.Entity, AudienceType.All
    ];

    public static IReadOnlyList<GroupName> ValidateAndResolveGroups(NotificationEvent evt, int maxPayloadBytes)
    {
        if (evt.EventId == Guid.Empty)
            throw Invalid(evt, "EventId vacío");

        if (!EventTypePattern().IsMatch(evt.EventType ?? string.Empty))
            throw Invalid(evt, $"EventType inválido '{evt.EventType}'");

        if (string.IsNullOrWhiteSpace(evt.Tenant) || !GroupName.TryCreate(evt.Tenant, AudienceType.All, null, out _))
            throw Invalid(evt, $"Tenant inválido '{evt.Tenant}'");

        if (evt.Audience is null || evt.Audience.Count == 0)
            throw Invalid(evt, "Audience vacío");

        if (evt.Payload.ValueKind != JsonValueKind.Object)
            throw Invalid(evt, $"Payload debe ser un objeto JSON y es {evt.Payload.ValueKind}");

        var payloadBytes = Encoding.UTF8.GetByteCount(evt.Payload.GetRawText());
        if (payloadBytes > maxPayloadBytes)
            throw Invalid(evt, $"Payload de {payloadBytes} bytes excede el límite de {maxPayloadBytes}");

        var groups = new List<GroupName>();
        var seen = new HashSet<GroupName>();
        foreach (var audience in evt.Audience)
        {
            var type = audience?.Type?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!AudienceTypes.Contains(type))
                throw Invalid(evt, $"Tipo de audiencia desconocido '{audience?.Type}'");

            if (!GroupName.TryCreate(evt.Tenant, type, audience!.Value, out var group))
                throw Invalid(evt, $"Audiencia inválida '{audience.Type}:{audience.Value}'");

            if (seen.Add(group))
                groups.Add(group);
        }

        return groups;
    }

    private static InvalidEventDataException Invalid(NotificationEvent evt, string reason) =>
        new($"Evento {evt.EventId} de '{evt.Source}' inválido: {reason}");

    [GeneratedRegex("^[a-z0-9._-]{1,100}$")]
    private static partial Regex EventTypePattern();
}
