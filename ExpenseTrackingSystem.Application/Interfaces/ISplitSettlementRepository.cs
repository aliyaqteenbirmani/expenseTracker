using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitSettlementRepository
    {
        Task<SplitSettlement?> GetSettlementByIdAsync(Guid settlementId);
        Task<SplitSettlementDto?> GetSettlementDetailsAsync(Guid settlementId, Guid groupId);
        Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId);
        Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId, Guid currentUserId, bool isOwner);
        Task<List<SplitSettlementDto>> GetPendingActionsAsync(Guid groupId, Guid currentUserId, bool isOwner);
        Task<List<SplitSettlement>> GetPendingSettlementsAsync(Guid groupId);
        Task<decimal> GetPendingPayerToReceiverAmountAsync(Guid groupId, Guid fromUserId, Guid toUserId, Guid? excludeSettlementId = null);
        Task<SplitSettlementDto> CreateSettlementAsync(SplitSettlement settlement);
        Task<int> ConfirmByReceiverAsync(Guid settlementId, string newStatus, Guid receiverUserId);
        Task<int> DeclineByReceiverAsync(Guid settlementId, string? reason, Guid receiverUserId);
        Task<int> ApproveByOwnerAsync(Guid settlementId, Guid ownerUserId);
        Task<int> DeclineByOwnerAsync(Guid settlementId, string? reason, Guid ownerUserId);
        Task<int> CancelByPayerAsync(Guid settlementId, Guid payerUserId);
        Task<bool> ReverseConfirmedSettlementAsync(Guid settlementId, Guid groupId, Guid ownerUserId);

        Task<SplitSettlementDto> CreateSettlementWithActivityAsync(
            SplitSettlement settlement, string creatorName, string groupName);
        Task<SplitSettlementDto?> UpdateSettlementWithActivityAsync(
            SplitSettlement settlement, string editorName, string groupName);
        Task<bool> SoftDeleteSettlementWithActivityAsync(
            Guid settlementId, Guid groupId, Guid deletedByUserId, string deletedByName);
    }
}
