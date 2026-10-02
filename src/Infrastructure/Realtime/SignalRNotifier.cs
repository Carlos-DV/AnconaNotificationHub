using Application.Abstractions.Realtime;
using Domain;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Realtime;

/// <summary>
/// Un solo envío a N grupos. SignalR no deduplica: si una conexión está en dos grupos recibe dos
/// veces; el frontend descarta por eventId.
/// </summary>
internal sealed class SignalRNotifier(IHubContext<NotificationHub, INotificationClient> hubContext) : IRealtimeNotifier
{
    public Task SendAsync(IReadOnlyList<GroupName> groups, ClientNotification notification, CancellationToken cancellationToken) =>
        hubContext.Clients.Groups(groups.Select(g => g.Value).ToList()).ReceiveNotification(notification);
}
