using AnconaNotificationHub.Contracts;
using Application.Abstractions.MessageBus;
using Application.Features.DispatchEvent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

/// <summary>
/// Vive dentro de IIS: requiere app pool AlwaysRunning + Preload (ver README). Si falla, la API sigue
/// sirviendo el hub pero /health reporta Unhealthy.
/// </summary>
internal sealed class NotificationEventConsumer(
    IMessageSubscriber subscriber,
    ConsumerHealthState health,
    ILogger<NotificationEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Suscribiendo el consumer de notificaciones.");
        try
        {
            await subscriber.SubscribeAsync<NotificationEvent, DispatchNotificationEventHandler>(stoppingToken);
            health.MarkSubscribed();
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "El consumer de notificaciones se detuvo.");
        }
        finally
        {
            health.MarkStopped();
        }
    }
}
