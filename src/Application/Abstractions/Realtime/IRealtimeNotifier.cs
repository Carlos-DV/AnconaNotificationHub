using Domain;

namespace Application.Abstractions.Realtime;

public interface IRealtimeNotifier
{
    Task SendAsync(IReadOnlyList<GroupName> groups, ClientNotification notification, CancellationToken cancellationToken);
}
