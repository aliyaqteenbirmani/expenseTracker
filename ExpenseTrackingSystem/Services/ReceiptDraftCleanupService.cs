using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpendwiseSystem.Application.Interfaces;

namespace SpendwiseSystem.API.Services
{
    /// <summary>Removes expired receipt uploads from private storage and then deletes their metadata.</summary>
    public sealed class ReceiptDraftCleanupService : BackgroundService
    {
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ReceiptDraftCleanupService> _logger;

        public ReceiptDraftCleanupService(
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment environment,
            ILogger<ReceiptDraftCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await CleanupExpiredDraftsAsync(stoppingToken);

            using var timer = new PeriodicTimer(CleanupInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await CleanupExpiredDraftsAsync(stoppingToken);
        }

        private async Task CleanupExpiredDraftsAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IReceiptDraftRepository>();
                var expiredDrafts = await repository.GetExpiredAsync(DateTime.UtcNow);
                var storageDirectory = Path.Combine(_environment.ContentRootPath, "App_Data", "receipt-drafts");

                foreach (var draft in expiredDrafts)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    try
                    {
                        var fileName = Path.GetFileName(draft.StoredFileName);
                        if (!string.Equals(fileName, draft.StoredFileName, StringComparison.Ordinal))
                        {
                            _logger.LogWarning("Skipping expired receipt draft {DraftId} with an unsafe stored filename.", draft.Id);
                            continue;
                        }

                        var filePath = Path.Combine(storageDirectory, fileName);
                        if (File.Exists(filePath))
                            File.Delete(filePath);

                        await repository.DeleteExpiredAsync(draft.Id, DateTime.UtcNow);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        _logger.LogWarning(exception, "Could not clean up expired receipt draft {DraftId}; it will be retried.", draft.Id);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal host shutdown.
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Receipt draft cleanup failed. It will run again on the next interval.");
            }
        }
    }
}
