namespace Application.Abstractions.Handlers;

public interface IIntegrationEventHandler<in T> where T : class
{
    Task Handle(T @event, CancellationToken cancellationToken);
}
