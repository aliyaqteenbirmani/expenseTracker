using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpendwiseSystem.API.Services
{
    /// <summary>
    /// Periodic background service that checks for pending settlements older than 3 days
    /// and dispatches a single in-app reminder notification to whoever must act.
    /// </summary>
    public sealed class SettlementReminderBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SettlementReminderBackgroundService> _logger;

        public SettlementReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SettlementReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initial delay to avoid startup contention
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await CheckPendingSettlementsAsync(stoppingToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(CheckInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await CheckPendingSettlementsAsync(stoppingToken).ConfigureAwait(false);
            }
        }

        private async Task CheckPendingSettlementsAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var settlementRepo = scope.ServiceProvider.GetRequiredService<ISplitSettlementRepository>();
                var realtimeService = scope.ServiceProvider.GetService<ISplitRealtimeService>();

                // Pending older than 3 days
                var olderThan = DateTime.UtcNow.AddDays(-3);
                var pendingSettlements = await settlementRepo.GetSettlementsNeedingReminderAsync(olderThan);

                foreach (var settlement in pendingSettlements)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    Guid? targetUserId = null;
                    if (settlement.Status == "PendingReceiver")
                    {
                        targetUserId = settlement.ToUserId;
                    }
                    else if (settlement.Status == "PendingOwner")
                    {
                        targetUserId = settlement.GroupOwnerId;
                    }

                    if (!targetUserId.HasValue || targetUserId.Value == Guid.Empty)
                        continue;

                    var reminder = new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = targetUserId.Value,
                        Type = "SettlementReminder",
                        GroupId = settlement.GroupId,
                        ReferenceId = settlement.Id,
                        Title = "Settlement Reminder",
                        Message = $"Reminder: Settlement of Rs. {settlement.Amount:N2} between {settlement.FromUserName} and {settlement.ToUserName} is awaiting your action.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    bool added = await settlementRepo.AddReminderNotificationAsync(reminder);
                    if (added && realtimeService != null)
                    {
                        try
                        {
                            await realtimeService.NotifyUserAsync(targetUserId.Value, "ReceiveSplitNotification", reminder);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to broadcast realtime settlement reminder notification to user {UserId}", targetUserId.Value);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing SettlementReminderBackgroundService.");
            }
        }
    }
}
