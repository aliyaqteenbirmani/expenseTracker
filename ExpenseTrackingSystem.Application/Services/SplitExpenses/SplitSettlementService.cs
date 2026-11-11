using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class SplitSettlementService : ISplitSettlementService
    {
        private readonly ISplitSettlementRepository _settlementRepository;
        private readonly ISplitGroupRepository _groupRepository;
        private readonly ISplitBalanceRepository _balanceRepository;
        private readonly IDebtSimplificationService _debtSimplificationService;

        public SplitSettlementService(
            ISplitSettlementRepository settlementRepository,
            ISplitGroupRepository groupRepository,
            ISplitBalanceRepository balanceRepository,
            IDebtSimplificationService debtSimplificationService)
        {
            _settlementRepository = settlementRepository;
            _groupRepository = groupRepository;
            _balanceRepository = balanceRepository;
            _debtSimplificationService = debtSimplificationService;
        }

        public async Task<ApiResponse<SplitSettlementDto>> RecordSettlementAsync(
            Guid groupId, RecordSettlementDto dto, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Group not found.");

            var currentMembership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (currentMembership == null || !currentMembership.IsActive)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("You must be an active group member to record settlements.");

            if (dto.Amount <= 0)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement amount must be greater than zero.");

            if (dto.FromUserId == dto.ToUserId)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Sender and receiver must be different members.");

            // Standardize amount precision to 2 decimal places
            var amount = Math.Round(dto.Amount, 2, MidpointRounding.AwayFromZero);

            // Fetch balances to validate that FromUser has debt and ToUser is owed money
            var balances = await _balanceRepository.GetGroupBalancesAsync(groupId);

            var sender = balances.FirstOrDefault(b => b.UserId == dto.FromUserId);
            if (sender == null)
                return ApiResponse<SplitSettlementDto>.FailureResponse("The specified sender does not belong to this group.");

            var receiver = balances.FirstOrDefault(b => b.UserId == dto.ToUserId);
            if (receiver == null)
                return ApiResponse<SplitSettlementDto>.FailureResponse("The specified receiver does not belong to this group.");

            if (sender.NetBalance >= -0.005m)
                return ApiResponse<SplitSettlementDto>.FailureResponse($"{sender.FullName} currently has no outstanding debt to settle.");

            if (receiver.NetBalance <= 0.005m)
                return ApiResponse<SplitSettlementDto>.FailureResponse($"{receiver.FullName} is not currently owed any money in this group.");

            var senderDebt = Math.Abs(sender.NetBalance);
            if (amount > senderDebt + 0.01m)
                return ApiResponse<SplitSettlementDto>.FailureResponse(
                    $"Settlement amount (Rs. {amount:N2}) exceeds {sender.FullName}'s outstanding debt of Rs. {senderDebt:N2}.");

            var receiverCredit = receiver.NetBalance;
            if (amount > receiverCredit + 0.01m)
                return ApiResponse<SplitSettlementDto>.FailureResponse(
                    $"Settlement amount (Rs. {amount:N2}) exceeds {receiver.FullName}'s outstanding credit of Rs. {receiverCredit:N2}.");

            var settlement = new SplitSettlement
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                FromUserId = dto.FromUserId,
                ToUserId = dto.ToUserId,
                Amount = amount,
                SettlementDate = dto.SettlementDate ?? DateTime.UtcNow,
                Note = dto.Note?.Trim(),
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var result = await _settlementRepository.CreateSettlementWithActivityAsync(
                settlement, currentUserName, group.Name);

            return ApiResponse<SplitSettlementDto>.SuccessResponse("Settlement recorded successfully.", result);
        }

        public async Task<ApiResponse<List<SplitSettlementDto>>> GetGroupSettlementsAsync(
            Guid groupId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<List<SplitSettlementDto>>.ForbiddenResponse("You must be an active member to view settlements.");

            var settlements = await _settlementRepository.GetGroupSettlementsAsync(groupId);
            return ApiResponse<List<SplitSettlementDto>>.SuccessResponse("Settlements retrieved successfully.", settlements);
        }

        public async Task<ApiResponse<SplitSettlementDto>> GetSettlementDetailsAsync(
            Guid groupId, Guid settlementId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("You must be an active member to view settlement details.");

            var detail = await _settlementRepository.GetSettlementDetailsAsync(settlementId, groupId);
            if (detail == null)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement not found.");

            return ApiResponse<SplitSettlementDto>.SuccessResponse("Settlement details retrieved successfully.", detail);
        }

        public async Task<ApiResponse<SplitSettlementDto>> UpdateSettlementAsync(
            Guid groupId, Guid settlementId, UpdateSettlementDto dto, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Group not found.");

            var existing = await _settlementRepository.GetSettlementByIdAsync(settlementId);
            if (existing == null || existing.GroupId != groupId)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement not found for this group.");

            var isCreator = existing.CreatedBy == currentUserId;
            var isGroupOwner = group.OwnerId == currentUserId;

            if (!isCreator && !isGroupOwner)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("Only the settlement creator or group owner can update this settlement.");

            if (dto.Amount <= 0)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement amount must be greater than zero.");

            if (dto.FromUserId == dto.ToUserId)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Sender and receiver must be different members.");

            existing.FromUserId = dto.FromUserId;
            existing.ToUserId = dto.ToUserId;
            existing.Amount = Math.Round(dto.Amount, 2, MidpointRounding.AwayFromZero);
            existing.SettlementDate = dto.SettlementDate ?? existing.SettlementDate;
            existing.Note = dto.Note?.Trim();
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _settlementRepository.UpdateSettlementWithActivityAsync(
                existing, currentUserName, group.Name);

            if (updated == null)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Failed to update settlement.");

            return ApiResponse<SplitSettlementDto>.SuccessResponse("Settlement updated successfully.", updated);
        }

        public async Task<ApiResponse<bool>> DeleteSettlementAsync(
            Guid groupId, Guid settlementId, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<bool>.FailureResponse("Group not found.");

            var existing = await _settlementRepository.GetSettlementByIdAsync(settlementId);
            if (existing == null || existing.GroupId != groupId)
                return ApiResponse<bool>.FailureResponse("Settlement not found for this group.");

            var isCreator = existing.CreatedBy == currentUserId;
            var isGroupOwner = group.OwnerId == currentUserId;

            if (!isCreator && !isGroupOwner)
                return ApiResponse<bool>.ForbiddenResponse("Only the settlement creator or group owner can delete this settlement.");

            var success = await _settlementRepository.SoftDeleteSettlementWithActivityAsync(
                settlementId, groupId, currentUserId, currentUserName);

            return success
                ? ApiResponse<bool>.SuccessResponse("Settlement deleted successfully.", true)
                : ApiResponse<bool>.FailureResponse("Failed to delete settlement.");
        }

        public async Task<ApiResponse<SettleUpOverviewDto>> GetSettleUpOverviewAsync(
            Guid groupId, Guid currentUserId)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<SettleUpOverviewDto>.FailureResponse("Group not found.");

            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<SettleUpOverviewDto>.ForbiddenResponse("You must be an active member to view settle up details.");

            var balances = await _balanceRepository.GetGroupBalancesAsync(groupId);

            var debtors = balances.Where(b => b.NetBalance < -0.005m).ToList();
            var creditors = balances.Where(b => b.NetBalance > 0.005m).ToList();
            var totalDebt = debtors.Sum(d => Math.Abs(d.NetBalance));

            var suggestions = _debtSimplificationService.SimplifyDebts(balances);

            var overview = new SettleUpOverviewDto
            {
                GroupId = groupId,
                GroupName = group.Name,
                TotalOutstandingDebt = totalDebt,
                Debtors = debtors,
                Creditors = creditors,
                SuggestedTransactions = suggestions
            };

            return ApiResponse<SettleUpOverviewDto>.SuccessResponse("Settle up overview retrieved successfully.", overview);
        }
    }
}
