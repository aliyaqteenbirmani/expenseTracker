using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitSettlementService
    {
        Task<ApiResponse<SplitSettlementDto>> RecordSettlementAsync(
            Guid groupId, RecordSettlementDto dto, Guid currentUserId, string currentUserName);

        Task<ApiResponse<List<SplitSettlementDto>>> GetGroupSettlementsAsync(
            Guid groupId, Guid currentUserId);

        Task<ApiResponse<SplitSettlementDto>> GetSettlementDetailsAsync(
            Guid groupId, Guid settlementId, Guid currentUserId);

        Task<ApiResponse<SplitSettlementDto>> UpdateSettlementAsync(
            Guid groupId, Guid settlementId, UpdateSettlementDto dto, Guid currentUserId, string currentUserName);

        Task<ApiResponse<bool>> DeleteSettlementAsync(
            Guid groupId, Guid settlementId, Guid currentUserId, string currentUserName);

        Task<ApiResponse<SettleUpOverviewDto>> GetSettleUpOverviewAsync(
            Guid groupId, Guid currentUserId);
    }
}
