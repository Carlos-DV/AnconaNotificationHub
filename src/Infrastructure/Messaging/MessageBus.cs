using Application.Abstractions.Handlers;
using Application.Abstractions.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LilHermesConsumer = LilHermes.Infrastructure.Interfaces.IMessageConsumer;

namespace Infrastructure.Messaging;

/// <summary>Scope por mensaje; ack en éxito. Si el handler lanza, LilHermes reintenta y manda a la DLQ.</summary>
internal sealed class MessageBus(
    LilHermesConsumer consumer,
    IServiceProvider serviceProvider,
    ILogger<MessageBus> logger) : IMessageSubscriber
{
    public Task SubscribeAsync<T, TH>(CancellationToken cancellationToken)
        where T : class
        where TH : IIntegrationEventHandler<T>
    {
        return consumer.StartConsumingAsync<T>(async (@event, deliveryTag) =>
        {
            using var scope = serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<TH>();
            try
            {
                await handler.Handle(@event, cancellationToken);
                await consumer.AckAsync(deliveryTag);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling message of type {MessageType}", typeof(T).FullName);
                throw;
            }
        });
    }
}
