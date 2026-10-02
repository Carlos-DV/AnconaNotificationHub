using Application.Abstractions.Realtime;
using Domain;

namespace Application.UnitTests.Support;

internal sealed class FakeRealtimeNotifier : IRealtimeNotifier
{
    public List<(IReadOnlyList<GroupName> Groups, ClientNotification Notification)> Calls { get; } = [];

    public Task SendAsync(IReadOnlyList<GroupName> groups, ClientNotification notification, CancellationToken cancellationToken)
    {
        Calls.Add((groups, notification));
        return Task.CompletedTask;
    }
}
