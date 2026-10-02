using Application.Abstractions.Realtime;

namespace Infrastructure.Realtime;

/// <summary>Único método servidor → cliente. Agregar eventos no agrega métodos.</summary>
public interface INotificationClient
{
    Task ReceiveNotification(ClientNotification notification);
}
