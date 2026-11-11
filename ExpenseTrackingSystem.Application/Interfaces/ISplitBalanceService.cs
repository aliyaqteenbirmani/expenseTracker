using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitBalanceService
    {
        Task<ApiResponse<List<MemberBalanceDto>>> GetGroupBalancesAsync(Guid groupId, Guid currentUserId);
        Task<ApiResponse<MemberBalanceDto>> GetMemberBalanceAsync(Guid groupId, Guid targetUserId, Guid currentUserId);
        Task<ApiResponse<GroupFinancialSummaryDto>> GetGroupFinancialSummaryAsync(Guid groupId, Guid currentUserId);
    }
}
