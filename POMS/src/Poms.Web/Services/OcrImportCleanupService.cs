using Poms.Infrastructure.Services;

namespace Poms.Web.Services;

public sealed class OcrImportCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<OcrImportCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync();
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await CleanupAsync();
    }

    private async Task CleanupAsync()
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
            await storage.CleanupExpiredOcrImportsAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not clean up expired OCR import scans");
        }
    }
}
