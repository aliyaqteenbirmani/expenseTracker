using Microsoft.AspNetCore.SignalR;
using SpendwiseSystem.API.Hubs;
using SpendwiseSystem.Domain.DTOs.CashTransactionDtos;
using System;
using System.Threading.Tasks;

namespace SpendwiseSystem.API.Services.Realtime
{
    public class RealtimeNotificationService : IRealtimeNotificationService
    {
        private readonly IHubContext<AppHub> _hubContext;
        private readonly ILogger<RealtimeNotificationService> _logger;

        public RealtimeNotificationService(IHubContext<AppHub> hubContext, ILogger<RealtimeNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyInvitationCreatedAsync(
            Guid businessId,
            string businessName,
            Guid? cashbookId,
            string cashbookName,
            string invitedEmail,
            string inviterName,
            string remarks)
        {
            try
            {
                var payload = new
                {
                    Type = "INVITATION_RECEIVED",
                    BusinessId = businessId,
                    BusinessName = businessName,
                    CashbookId = cashbookId,
                    CashbookName = cashbookName,
                    InvitedByName = inviterName,
                    Remarks = remarks,
                    Message = !string.IsNullOrWhiteSpace(cashbookName)
                        ? $"{inviterName} invited you to collaborate on cashbook '{cashbookName}' ({businessName})."
                        : $"{inviterName} invited you to collaborate on workspace '{businessName}'."
                };

                await _hubContext.Clients.Group($"email:{invitedEmail.Trim().ToLowerInvariant()}")
                    .SendAsync("ReceiveInvitation", payload);

                _logger.LogInformation("SignalR: Sent ReceiveInvitation to email:{Email}", invitedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast ReceiveInvitation to {Email}", invitedEmail);
            }
        }

        public async Task NotifyInvitationAcceptedAsync(
            Guid businessId,
            string businessName,
            Guid? cashbookId,
            string cashbookName,
            string acceptedByName,
            Guid inviterUserId,
            Guid acceptedUserId)
        {
            try
            {
                var payload = new
                {
                    Type = "INVITATION_ACCEPTED",
                    BusinessId = businessId,
                    BusinessName = businessName,
                    CashbookId = cashbookId,
                    CashbookName = cashbookName,
                    AcceptedByName = acceptedByName,
                    AcceptedUserId = acceptedUserId,
                    Message = !string.IsNullOrWhiteSpace(cashbookName)
                        ? $"{acceptedByName} accepted your invitation to join cashbook '{cashbookName}'."
                        : $"{acceptedByName} accepted your invitation to join workspace '{businessName}'."
                };

                if (inviterUserId != Guid.Empty)
                {
                    await _hubContext.Clients.Group($"user:{inviterUserId}")
                        .SendAsync("InvitationAccepted", payload);
                }

                if (acceptedUserId != Guid.Empty)
                {
                    await _hubContext.Clients.Group($"user:{acceptedUserId}")
                        .SendAsync("WorkspacesChanged", payload);
                }

                await _hubContext.Clients.Group($"business:{businessId}")
                    .SendAsync("MemberJoined", payload);

                _logger.LogInformation("SignalR: Broadcasted InvitationAccepted for business:{BusinessId}", businessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast InvitationAccepted for business {BusinessId}", businessId);
            }
        }

        public async Task NotifyCashbookCreatedAsync(
            Guid businessId,
            Guid cashbookId,
            string cashbookName,
            string createdByName)
        {
            try
            {
                var payload = new
                {
                    Type = "CASHBOOK_CREATED",
                    BusinessId = businessId,
                    CashbookId = cashbookId,
                    CashbookName = cashbookName,
                    CreatedByName = createdByName,
                    Message = $"{createdByName} created a new cashbook '{cashbookName}'."
                };

                await _hubContext.Clients.Group($"business:{businessId}")
                    .SendAsync("CashbookCreated", payload);

                _logger.LogInformation("SignalR: Broadcasted CashbookCreated for business:{BusinessId}", businessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast CashbookCreated for business {BusinessId}", businessId);
            }
        }

        public async Task NotifyTransactionCreatedAsync(
            Guid cashBookId,
            string cashbookName,
            AllCashTransactionDto transaction,
            string createdByName,
            Guid creatorUserId)
        {
            try
            {
                var isIncome = string.Equals(transaction.TransactionType, "CashIn", StringComparison.OrdinalIgnoreCase);
                var actionWord = isIncome ? "added cash in" : "recorded an expense";
                var remarksSnippet = !string.IsNullOrWhiteSpace(transaction.Remarks) ? $" ({transaction.Remarks})" : "";

                var payload = new
                {
                    Type = "TRANSACTION_CREATED",
                    CashBookId = cashBookId,
                    CashbookName = cashbookName,
                    Transaction = transaction,
                    CreatedByName = createdByName,
                    CreatorUserId = creatorUserId,
                    Message = $"{createdByName} {actionWord} of Rs. {transaction.Amount:N0}{remarksSnippet} in '{cashbookName}'."
                };

                await _hubContext.Clients.Group($"cashbook:{cashBookId}")
                    .SendAsync("TransactionCreated", payload);

                _logger.LogInformation("SignalR: Broadcasted TransactionCreated for cashbook:{CashBookId}", cashBookId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast TransactionCreated for cashbook {CashBookId}", cashBookId);
            }
        }

        public async Task NotifyTransactionUpdatedAsync(
            Guid cashBookId,
            string cashbookName,
            AllCashTransactionDto transaction,
            string modifiedByName,
            Guid modifierUserId)
        {
            try
            {
                var payload = new
                {
                    Type = "TRANSACTION_UPDATED",
                    CashBookId = cashBookId,
                    CashbookName = cashbookName,
                    Transaction = transaction,
                    ModifiedByName = modifiedByName,
                    ModifierUserId = modifierUserId,
                    Message = $"{modifiedByName} updated a transaction in '{cashbookName}'."
                };

                await _hubContext.Clients.Group($"cashbook:{cashBookId}")
                    .SendAsync("TransactionUpdated", payload);

                _logger.LogInformation("SignalR: Broadcasted TransactionUpdated for cashbook:{CashBookId}", cashBookId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast TransactionUpdated for cashbook {CashBookId}", cashBookId);
            }
        }

        public async Task NotifyTransactionDeletedAsync(
            Guid cashBookId,
            string cashbookName,
            Guid transactionId,
            string deletedByName,
            Guid deleterUserId)
        {
            try
            {
                var payload = new
                {
                    Type = "TRANSACTION_DELETED",
                    CashBookId = cashBookId,
                    CashbookName = cashbookName,
                    TransactionId = transactionId,
                    DeletedByName = deletedByName,
                    DeleterUserId = deleterUserId,
                    Message = $"{deletedByName} deleted a transaction in '{cashbookName}'."
                };

                await _hubContext.Clients.Group($"cashbook:{cashBookId}")
                    .SendAsync("TransactionDeleted", payload);

                _logger.LogInformation("SignalR: Broadcasted TransactionDeleted for cashbook:{CashBookId}", cashBookId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast TransactionDeleted for cashbook {CashBookId}", cashBookId);
            }
        }
    }
}
