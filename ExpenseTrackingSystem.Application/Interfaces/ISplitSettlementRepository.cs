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
        Task<SplitSettlementDto> CreateSettlementWithActivityAsync(
            SplitSettlement settlement, string creatorName, string groupName);
        Task<SplitSettlementDto?> UpdateSettlementWithActivityAsync(
            SplitSettlement settlement, string editorName, string groupName);
        Task<bool> SoftDeleteSettlementWithActivityAsync(
            Guid settlementId, Guid groupId, Guid deletedByUserId, string deletedByName);
    }
}
