namespace Infrastructure.Settings;

public sealed class RabbitMqSetting
{
    public const string SectionName = "RabbitMQ";

    public string ClientProvidedName { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public string QueueExchangeName { get; set; } = string.Empty;
    public string[] QueueRoutingKeys { get; set; } = [];
    public ushort PrefetchCount { get; set; } = 100;
    public bool EnableDLQ { get; set; } = true;
}
