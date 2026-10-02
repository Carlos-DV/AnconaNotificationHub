using Application.Abstractions.Handlers;

namespace Application.Abstractions.MessageBus;

public interface IMessageSubscriber
{
    Task SubscribeAsync<T, TH>(CancellationToken cancellationToken)
        where T : class
        where TH : IIntegrationEventHandler<T>;
}
