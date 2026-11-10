using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class SplitExpenseService : ISplitExpenseService
    {
        private readonly ISplitExpenseRepository _expenseRepository;
        private readonly ISplitGroupRepository _groupRepository;
        private readonly ISplitCalculatorService _calculatorService;

        public SplitExpenseService(
            ISplitExpenseRepository expenseRepository,
            ISplitGroupRepository groupRepository,
            ISplitCalculatorService calculatorService)
        {
            _expenseRepository = expenseRepository;
            _groupRepository = groupRepository;
            _calculatorService = calculatorService;
        }

        public async Task<ApiResponse<SplitExpenseDetailDto>> AddExpenseAsync(
            Guid groupId, CreateSplitExpenseDto dto, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Group not found.");

            // Current user must be an active member of the group
            var currentMembership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (currentMembership == null || !currentMembership.IsActive)
                return ApiResponse<SplitExpenseDetailDto>.ForbiddenResponse("You must be an active group member to add expenses.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Expense title cannot be empty.");

            if (dto.Amount <= 0)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Expense amount must be greater than zero.");

            // Resolve payer (defaults to current user if omitted)
            var payerId = dto.PayerId.HasValue && dto.PayerId.Value != Guid.Empty
                ? dto.PayerId.Value
                : currentUserId;

            var payerMembership = await _groupRepository.GetMembershipAsync(groupId, payerId);
            if (payerMembership == null || !payerMembership.IsActive)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("The specified payer is not an active member of this group.");

            // Validate participant list
            var participantIds = (dto.ParticipantUserIds ?? new List<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (participantIds.Count == 0)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("At least one group member must be included in the split.");

            // Verify all participants are active members of the group
            var activeMembers = await _groupRepository.GetGroupMembersAsync(groupId);
            var activeMemberIdSet = activeMembers.Where(m => m.IsActive).Select(m => m.UserId).ToHashSet();

            foreach (var participantId in participantIds)
            {
                if (!activeMemberIdSet.Contains(participantId))
                    return ApiResponse<SplitExpenseDetailDto>.FailureResponse("One or more selected participants are not active members of this group.");
            }

            // Calculate equal split shares
            var calculatedShares = _calculatorService.CalculateEqualSplit(dto.Amount, participantIds);

            var expense = new SplitExpense
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                PayerId = payerId,
                CreatedBy = currentUserId,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                Note = dto.Note?.Trim(),
                Amount = dto.Amount,
                Category = dto.Category?.Trim(),
                ExpenseDate = dto.ExpenseDate ?? DateTime.UtcNow,
                ReceiptUrl = dto.ReceiptUrl?.Trim(),
                SplitType = "Equal",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var shares = calculatedShares.Select(kv => new SplitExpenseShare
            {
                Id = Guid.NewGuid(),
                ExpenseId = expense.Id,
                UserId = kv.Key,
                ShareAmount = kv.Value,
                CreatedAt = expense.CreatedAt,
                UpdatedAt = expense.UpdatedAt
            }).ToList();

            var result = await _expenseRepository.CreateExpenseWithSharesAndActivityAsync(
                expense, shares, currentUserName, group.Name);

            return ApiResponse<SplitExpenseDetailDto>.SuccessResponse("Expense added successfully.", result);
        }

        public async Task<ApiResponse<PagedExpenseResultDto>> GetExpensesListAsync(
            Guid groupId, Guid currentUserId, SplitExpenseQueryParameters parameters)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<PagedExpenseResultDto>.ForbiddenResponse("You must be an active member of this group to view expenses.");

            var paged = await _expenseRepository.GetExpensesListAsync(groupId, currentUserId, parameters);
            return ApiResponse<PagedExpenseResultDto>.SuccessResponse("Expenses retrieved successfully.", paged);
        }

        public async Task<ApiResponse<SplitExpenseDetailDto>> GetExpenseDetailsAsync(
            Guid groupId, Guid expenseId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<SplitExpenseDetailDto>.ForbiddenResponse("You must be an active member of this group to view expense details.");

            var detail = await _expenseRepository.GetExpenseDetailsAsync(expenseId, groupId);
            if (detail == null)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Expense not found.");

            return ApiResponse<SplitExpenseDetailDto>.SuccessResponse("Expense details retrieved successfully.", detail);
        }

        public async Task<ApiResponse<SplitExpenseDetailDto>> EditExpenseAsync(
            Guid groupId, Guid expenseId, UpdateSplitExpenseDto dto, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Group not found.");

            var existingExpense = await _expenseRepository.GetExpenseByIdAsync(expenseId);
            if (existingExpense == null || existingExpense.GroupId != groupId)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Expense not found for this group.");

            // Authorization: Only the creator of the expense or the group owner can edit
            var isCreator = existingExpense.CreatedBy == currentUserId;
            var isGroupOwner = group.OwnerId == currentUserId;

            if (!isCreator && !isGroupOwner)
                return ApiResponse<SplitExpenseDetailDto>.ForbiddenResponse("Only the expense creator or the group owner can edit this expense.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Expense title cannot be empty.");

            if (dto.Amount <= 0)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Expense amount must be greater than zero.");

            // Verify payer is active member
            var payerMembership = await _groupRepository.GetMembershipAsync(groupId, dto.PayerId);
            if (payerMembership == null || !payerMembership.IsActive)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("The specified payer is not an active member of this group.");

            // Validate participant list
            var participantIds = (dto.ParticipantUserIds ?? new List<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (participantIds.Count == 0)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("At least one group member must be included in the split.");

            var activeMembers = await _groupRepository.GetGroupMembersAsync(groupId);
            var activeMemberIdSet = activeMembers.Where(m => m.IsActive).Select(m => m.UserId).ToHashSet();

            foreach (var participantId in participantIds)
            {
                if (!activeMemberIdSet.Contains(participantId))
                    return ApiResponse<SplitExpenseDetailDto>.FailureResponse("One or more selected participants are not active members of this group.");
            }

            // Recalculate shares
            var calculatedShares = _calculatorService.CalculateEqualSplit(dto.Amount, participantIds);

            existingExpense.Title = dto.Title.Trim();
            existingExpense.Description = dto.Description?.Trim();
            existingExpense.Note = dto.Note?.Trim();
            existingExpense.Amount = dto.Amount;
            existingExpense.Category = dto.Category?.Trim();
            existingExpense.ExpenseDate = dto.ExpenseDate ?? existingExpense.ExpenseDate;
            existingExpense.PayerId = dto.PayerId;
            existingExpense.ReceiptUrl = dto.ReceiptUrl?.Trim();
            existingExpense.UpdatedAt = DateTime.UtcNow;

            var newShares = calculatedShares.Select(kv => new SplitExpenseShare
            {
                Id = Guid.NewGuid(),
                ExpenseId = existingExpense.Id,
                UserId = kv.Key,
                ShareAmount = kv.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            var updated = await _expenseRepository.UpdateExpenseWithSharesAndActivityAsync(
                existingExpense, newShares, currentUserName, group.Name);

            if (updated == null)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Failed to update expense.");

            return ApiResponse<SplitExpenseDetailDto>.SuccessResponse("Expense updated successfully.", updated);
        }

        public async Task<ApiResponse<bool>> DeleteExpenseAsync(
            Guid groupId, Guid expenseId, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<bool>.FailureResponse("Group not found.");

            var existingExpense = await _expenseRepository.GetExpenseByIdAsync(expenseId);
            if (existingExpense == null || existingExpense.GroupId != groupId)
                return ApiResponse<bool>.FailureResponse("Expense not found for this group.");

            // Authorization: Only the creator of the expense or the group owner can delete
            var isCreator = existingExpense.CreatedBy == currentUserId;
            var isGroupOwner = group.OwnerId == currentUserId;

            if (!isCreator && !isGroupOwner)
                return ApiResponse<bool>.ForbiddenResponse("Only the expense creator or the group owner can delete this expense.");

            var success = await _expenseRepository.SoftDeleteExpenseWithActivityAsync(
                expenseId, groupId, currentUserId, currentUserName);

            return success
                ? ApiResponse<bool>.SuccessResponse("Expense deleted successfully.", true)
                : ApiResponse<bool>.FailureResponse("Failed to delete expense.");
        }
    }
}
