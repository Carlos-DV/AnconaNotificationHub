using Application.Abstractions.Identity;
using Application.Abstractions.MessageBus;
using Application.Abstractions.Realtime;
using Infrastructure.Health;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Realtime;
using Infrastructure.Settings;
using LilHermes.Abstractions.Enums;
using LilHermes.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        //Database
        var rootConnectionString = configuration.GetConnectionString("Root");
        if (string.IsNullOrWhiteSpace(rootConnectionString))
            throw new ArgumentException("Configura ConnectionStrings:Root (User Secrets en DEV, ConnectionStrings__Root en servidor).");

        services.AddSingleton(new RootDatabase(rootConnectionString));
        services.AddMemoryCache();
        services.AddSingleton<TenantConnectionProvider>();
        services.AddSingleton<IUserGroupResolver, UserGroupResolver>();

        //RabbitMQ
        var amq = configuration.GetSection(RabbitMqSetting.SectionName).Get<RabbitMqSetting>()
            ?? throw new ArgumentException("No se encontró la configuración de RabbitMQ");
        ValidateRabbitSettings(amq);
        var serviceName = $"LilHermes.{amq.ClientProvidedName}";

        services.AddLilHermesConsumer(pla =>
        {
            pla.SourceName = serviceName;
            //Connection
            pla.ConnectionOptions.ClientProvidedName = serviceName;
            pla.ConnectionOptions.HostName = amq.HostName;
            pla.ConnectionOptions.Port = amq.Port;
            pla.ConnectionOptions.Username = amq.Username;
            pla.ConnectionOptions.Password = amq.Password;
            //Consumer
            pla.ConsumerOptions.QueueName = amq.QueueName;
            pla.ConsumerOptions.ExchangeName = amq.QueueExchangeName;
            pla.ConsumerOptions.ExchangeType = RabbitMQExchangeType.Topic;
            pla.ConsumerOptions.RoutingKeys = amq.QueueRoutingKeys;
            pla.ConsumerOptions.PrefetchCount = amq.PrefetchCount;
            pla.ConsumerOptions.EnableDLQ = amq.EnableDLQ;
        });

        services.AddSingleton<MessageBus>();
        services.AddSingleton<IMessageSubscriber>(sp => sp.GetRequiredService<MessageBus>());
        services.AddSingleton<ConsumerHealthState>();
        services.AddHostedService<NotificationEventConsumer>();

        //SignalR
        services.AddSignalR();
        services.AddSingleton<IRealtimeNotifier, SignalRNotifier>();

        //Health
        services.AddHealthChecks()
            .AddCheck<ConsumerHealthCheck>("consumer")
            .AddCheck<RootDatabaseHealthCheck>("root-database");

        return services;
    }

    // LilHermes no valida la config del consumer: con QueueName vacío crea una cola amq.gen-… y con
    // QueueRoutingKeys vacío la cola queda sin bindings. En ambos casos arranca "bien" y nunca consume.
    private static void ValidateRabbitSettings(RabbitMqSetting amq)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(amq.HostName)) missing.Add(nameof(amq.HostName));
        if (string.IsNullOrWhiteSpace(amq.Username)) missing.Add(nameof(amq.Username));
        if (string.IsNullOrWhiteSpace(amq.QueueName)) missing.Add(nameof(amq.QueueName));
        if (string.IsNullOrWhiteSpace(amq.QueueExchangeName)) missing.Add(nameof(amq.QueueExchangeName));
        if (amq.QueueRoutingKeys.Length == 0) missing.Add(nameof(amq.QueueRoutingKeys));

        if (missing.Count > 0)
            throw new ArgumentException($"Configura RabbitMQ en appsettings.json antes de arrancar. Campos vacíos: {string.Join(", ", missing)}");
    }
}
