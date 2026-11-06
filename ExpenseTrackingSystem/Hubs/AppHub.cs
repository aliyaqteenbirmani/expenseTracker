using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SpendwiseSystem.API.Hubs
{
    [Authorize]
    public class AppHub : Hub
    {
        private readonly ILogger<AppHub> _logger;

        public AppHub(ILogger<AppHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"email:{email.Trim().ToLowerInvariant()}");
            }

            _logger.LogInformation("SignalR connected: ConnectionId {ConnectionId}, UserId {UserId}, Email {Email}",
                Context.ConnectionId, userId, email);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("SignalR disconnected: ConnectionId {ConnectionId}, UserId {UserId}",
                Context.ConnectionId, userId);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinCashbook(string cashbookId)
        {
            if (!string.IsNullOrWhiteSpace(cashbookId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"cashbook:{cashbookId}");
                _logger.LogInformation("Connection {ConnectionId} joined cashbook:{CashbookId}", Context.ConnectionId, cashbookId);
            }
        }

        public async Task LeaveCashbook(string cashbookId)
        {
            if (!string.IsNullOrWhiteSpace(cashbookId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"cashbook:{cashbookId}");
                _logger.LogInformation("Connection {ConnectionId} left cashbook:{CashbookId}", Context.ConnectionId, cashbookId);
            }
        }

        public async Task JoinBusiness(string businessId)
        {
            if (!string.IsNullOrWhiteSpace(businessId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"business:{businessId}");
                _logger.LogInformation("Connection {ConnectionId} joined business:{BusinessId}", Context.ConnectionId, businessId);
            }
        }

        public async Task LeaveBusiness(string businessId)
        {
            if (!string.IsNullOrWhiteSpace(businessId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"business:{businessId}");
                _logger.LogInformation("Connection {ConnectionId} left business:{BusinessId}", Context.ConnectionId, businessId);
            }
        }
    }
}
