using SpendwiseSystem.Domain.DTOs.CashTransactionDtos;
using System;
using System.Threading.Tasks;

namespace SpendwiseSystem.API.Services.Realtime
{
    public interface IRealtimeNotificationService
    {
        Task NotifyInvitationCreatedAsync(
            Guid businessId,
            string businessName,
            Guid? cashbookId,
            string cashbookName,
            string invitedEmail,
            string inviterName,
            string remarks);

        Task NotifyInvitationAcceptedAsync(
            Guid businessId,
            string businessName,
            Guid? cashbookId,
            string cashbookName,
            string acceptedByName,
            Guid inviterUserId,
            Guid acceptedUserId);

        Task NotifyCashbookCreatedAsync(
            Guid businessId,
            Guid cashbookId,
            string cashbookName,
            string createdByName);

        Task NotifyTransactionCreatedAsync(
            Guid cashBookId,
            string cashbookName,
            AllCashTransactionDto transaction,
            string createdByName,
            Guid creatorUserId);

        Task NotifyTransactionUpdatedAsync(
            Guid cashBookId,
            string cashbookName,
            AllCashTransactionDto transaction,
            string modifiedByName,
            Guid modifierUserId);

        Task NotifyTransactionDeletedAsync(
            Guid cashBookId,
            string cashbookName,
            Guid transactionId,
            string deletedByName,
            Guid deleterUserId);
    }
}
