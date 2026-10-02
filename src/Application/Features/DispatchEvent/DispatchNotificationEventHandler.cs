using AnconaNotificationHub.Contracts;
using Application.Abstractions.Handlers;
using Application.Abstractions.Realtime;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Features.DispatchEvent;

/// <summary>
/// Valida el sobre, descarta eventos viejos y emite a los grupos. No conoce ningún dominio.
/// </summary>
public sealed class DispatchNotificationEventHandler(
    IRealtimeNotifier notifier,
    IOptions<NotificationSetting> options,
    TimeProvider timeProvider,
    ILogger<DispatchNotificationEventHandler> logger) : IIntegrationEventHandler<NotificationEvent>
{
    private readonly NotificationSetting _settings = options.Value;

    public async Task Handle(NotificationEvent @event, CancellationToken cancellationToken)
    {
        var groups = NotificationEventValidator.ValidateAndResolveGroups(@event, _settings.MaxPayloadBytes);

        var age = timeProvider.GetUtcNow() - @event.OccurredAt;
        if (age > TimeSpan.FromSeconds(_settings.MaxEventAgeSeconds))
        {
            logger.LogDebug("Evento {EventId} ({EventType}) de {Source} descartado por antigüedad ({AgeSeconds:F0}s)",
                @event.EventId, @event.EventType, @event.Source, age.TotalSeconds);
            return;
        }

        var notification = new ClientNotification(@event.EventId, @event.EventType, @event.OccurredAt, @event.Payload);
        await notifier.SendAsync(groups, notification, cancellationToken);

        logger.LogDebug("Evento {EventId} ({EventType}) de {Source} emitido a {GroupCount} grupos de {Tenant}",
            @event.EventId, @event.EventType, @event.Source, groups.Count, @event.Tenant);
    }
}
