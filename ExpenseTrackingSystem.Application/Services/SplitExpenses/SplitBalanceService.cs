using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class SplitBalanceService : ISplitBalanceService
    {
        private readonly ISplitBalanceRepository _balanceRepository;
        private readonly ISplitGroupRepository _groupRepository;

        public SplitBalanceService(
            ISplitBalanceRepository balanceRepository,
            ISplitGroupRepository groupRepository)
        {
            _balanceRepository = balanceRepository;
            _groupRepository = groupRepository;
        }

        public async Task<ApiResponse<List<MemberBalanceDto>>> GetGroupBalancesAsync(Guid groupId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<List<MemberBalanceDto>>.ForbiddenResponse("You must be an active member of this group to view balances.");

            var balances = await _balanceRepository.GetGroupBalancesAsync(groupId);

            // Personalize status description for current user
            foreach (var b in balances)
            {
                if (b.UserId == currentUserId)
                {
                    if (b.NetBalance > 0)
                        b.StatusDescription = $"You are owed Rs. {b.NetBalance:N2}";
                    else if (b.NetBalance < 0)
                        b.StatusDescription = $"You owe Rs. {Math.Abs(b.NetBalance):N2}";
                    else
                        b.StatusDescription = "Settled up";
                }
            }

            return ApiResponse<List<MemberBalanceDto>>.SuccessResponse("Group balances retrieved successfully.", balances);
        }

        public async Task<ApiResponse<MemberBalanceDto>> GetMemberBalanceAsync(Guid groupId, Guid targetUserId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<MemberBalanceDto>.ForbiddenResponse("You must be an active member of this group to view balances.");

            var balance = await _balanceRepository.GetMemberBalanceAsync(groupId, targetUserId);
            if (balance == null)
                return ApiResponse<MemberBalanceDto>.FailureResponse("Member balance not found for this group.");

            if (targetUserId == currentUserId)
            {
                if (balance.NetBalance > 0)
                    balance.StatusDescription = $"You are owed Rs. {balance.NetBalance:N2}";
                else if (balance.NetBalance < 0)
                    balance.StatusDescription = $"You owe Rs. {Math.Abs(balance.NetBalance):N2}";
                else
                    balance.StatusDescription = "Settled up";
            }

            return ApiResponse<MemberBalanceDto>.SuccessResponse("Member balance retrieved successfully.", balance);
        }

        public async Task<ApiResponse<GroupFinancialSummaryDto>> GetGroupFinancialSummaryAsync(Guid groupId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<GroupFinancialSummaryDto>.ForbiddenResponse("You must be an active member of this group to view financial summary.");

            var summary = await _balanceRepository.GetGroupFinancialSummaryAsync(groupId, currentUserId);
            if (summary == null)
                return ApiResponse<GroupFinancialSummaryDto>.FailureResponse("Group not found.");

            return ApiResponse<GroupFinancialSummaryDto>.SuccessResponse("Group financial summary retrieved successfully.", summary);
        }
    }
}
