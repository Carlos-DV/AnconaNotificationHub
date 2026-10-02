namespace Infrastructure.Messaging;

/// <summary>Lo escribe el consumer y lo lee /health.</summary>
internal sealed class ConsumerHealthState
{
    private volatile bool _subscribed;

    public bool IsSubscribed => _subscribed;

    public void MarkSubscribed() => _subscribed = true;

    public void MarkStopped() => _subscribed = false;
}
