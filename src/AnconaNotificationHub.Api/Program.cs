using AnconaNotificationHub.Api.Auth;
using Application;
using Infrastructure;
using Infrastructure.Realtime;
using Serilog;

const string serviceName = "AnconaNotificationHub";
const string corsPolicy = "Frontends";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/bootstrap-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting {ServiceName} at {Timestamp}...", serviceName, DateTime.UtcNow);
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration));
    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // SignalR con credenciales exige orígenes explícitos (no "*").
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors(corsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHub<NotificationHub>(HubRoutes.Notifications);
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during bootstrapping.");
}
finally
{
    Log.CloseAndFlush();
}
