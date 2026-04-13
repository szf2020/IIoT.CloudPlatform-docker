using System.Diagnostics;

namespace IIoT.MigrationWorkApp;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<Worker> logger) : BackgroundService
{
    public const string ActivitySourceName = "IIoT.Migrations";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("Initialize database", ActivityKind.Client);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var orchestrator = scope.ServiceProvider
                .GetRequiredService<IDatabaseInitializationOrchestrator>();

            await orchestrator.InitializeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            logger.LogCritical(ex, "数据库初始化失败。");
            throw;
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }
}
