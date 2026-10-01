using System.Globalization;
using AnconaNotificationHub.Contracts;
using Application.Exceptions;
using Application.Features.Connections;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Realtime;

/// <summary>
/// Delgado: identidad al conectar (ResolveConnectionGroupsHandler) y Subscribe/Unsubscribe
/// (SubscriptionHandler). SignalR ejecuta las invocaciones de una conexión en serie, así que el
/// HashSet de Context.Items no necesita locks.
/// </summary>
[Authorize]
public sealed class NotificationHub(
    ResolveConnectionGroupsHandler resolveGroups,
    SubscriptionHandler subscriptions,
    ILogger<NotificationHub> logger) : Hub<INotificationClient>
{
    private const string TenantKey = "tenant";
    private const string SubscriptionsKey = "subscriptions";

    public override async Task OnConnectedAsync()
    {
        var tenant = Context.User?.FindFirst("tenant")?.Value?.Trim().ToLowerInvariant();
        var uidClaim = Context.User?.FindFirst("uid")?.Value;

        if (string.IsNullOrEmpty(tenant)
            || !GroupName.TryCreate(tenant, AudienceType.All, null, out _)
            || !int.TryParse(uidClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var userId))
        {
            logger.LogWarning("Conexión {ConnectionId} rechazada: el token no trae tenant/uid válidos", Context.ConnectionId);
            Context.Abort();
            return;
        }

        Context.Items[TenantKey] = tenant;
        Context.Items[SubscriptionsKey] = new HashSet<string>();

        var groups = await resolveGroups.HandleAsync(tenant, userId, Context.ConnectionAborted);
        foreach (var group in groups)
            await Groups.AddToGroupAsync(Context.ConnectionId, group.Value, Context.ConnectionAborted);

        logger.LogDebug("Usuario {UserId} ({Tenant}) conectado en {ConnectionId} con grupos {Groups}",
            userId, tenant, Context.ConnectionId, groups.Select(g => g.Value));

        await base.OnConnectedAsync();
    }

    public async Task Subscribe(string type, string value)
    {
        var (tenant, current) = GetConnectionState();

        GroupName group;
        try
        {
            group = subscriptions.ValidateSubscribe(tenant, type, value, current);
        }
        catch (SubscriptionRejectedException ex)
        {
            logger.LogWarning("Subscribe rechazado en {ConnectionId}: {Reason}", Context.ConnectionId, ex.Message);
            throw new HubException(ex.Message);
        }

        if (!current.Add(group.Value))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, group.Value);
    }

    public async Task Unsubscribe(string type, string value)
    {
        var (tenant, current) = GetConnectionState();
        var group = subscriptions.TryBuildUnsubscribe(tenant, type, value);
        if (group is null || !current.Remove(group.Value))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group.Value);
    }

    private (string Tenant, HashSet<string> Subscriptions) GetConnectionState() =>
        ((string)Context.Items[TenantKey]!, (HashSet<string>)Context.Items[SubscriptionsKey]!);
}
