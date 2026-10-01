using Application.Abstractions.Identity;
using Application.Features.Connections;
using Application.Features.DispatchEvent;
using Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationSetting>(configuration.GetSection(NotificationSetting.SectionName));
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<DispatchNotificationEventHandler>();
        services.AddScoped<ResolveConnectionGroupsHandler>();
        services.AddScoped<SubscriptionHandler>();
        services.AddSingleton<ISubscriptionPolicy, DefaultSubscriptionPolicy>();

        return services;
    }
}
