using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitBalanceRepository
    {
        Task<List<MemberBalanceDto>> GetGroupBalancesAsync(Guid groupId);
        Task<MemberBalanceDto?> GetMemberBalanceAsync(Guid groupId, Guid userId);
        Task<GroupFinancialSummaryDto?> GetGroupFinancialSummaryAsync(Guid groupId, Guid currentUserId);
    }
}
