using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SpendwiseSystem.API.Hubs;
using SpendwiseSystem.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SpendwiseSystem.API.Services.Realtime
{
    public class SplitRealtimeService : ISplitRealtimeService
    {
        private readonly IHubContext<AppHub> _hubContext;
        private readonly ILogger<SplitRealtimeService> _logger;

        public SplitRealtimeService(IHubContext<AppHub> hubContext, ILogger<SplitRealtimeService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyUserAsync(Guid userId, string eventName, object payload)
        {
            try
            {
                await _hubContext.Clients.Group($"user:{userId}")
                    .SendAsync(eventName, payload);
                _logger.LogInformation("SignalR: Broadcasted {Event} to user:{UserId}", eventName, userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast {Event} to user:{UserId}", eventName, userId);
            }
        }

        public async Task NotifyEmailAsync(string email, string eventName, object payload)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            try
            {
                var groupName = $"email:{email.Trim().ToLowerInvariant()}";
                await _hubContext.Clients.Group(groupName)
                    .SendAsync(eventName, payload);
                _logger.LogInformation("SignalR: Broadcasted {Event} to {Group}", eventName, groupName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast {Event} to email {Email}", eventName, email);
            }
        }
    }
}
