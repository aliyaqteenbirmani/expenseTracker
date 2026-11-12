using Microsoft.Extensions.Logging;
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
        private readonly ISplitNotificationRepository? _notificationRepository;
        private readonly ISplitRealtimeService? _realtimeService;
        private readonly ILogger<SplitExpenseService>? _logger;

        public SplitExpenseService(
            ISplitExpenseRepository expenseRepository,
            ISplitGroupRepository groupRepository,
            ISplitCalculatorService calculatorService)
            : this(expenseRepository, groupRepository, calculatorService, null, null, null)
        {
        }

        public SplitExpenseService(
            ISplitExpenseRepository expenseRepository,
            ISplitGroupRepository groupRepository,
            ISplitCalculatorService calculatorService,
            ISplitNotificationRepository? notificationRepository,
            ISplitRealtimeService? realtimeService = null,
            ILogger<SplitExpenseService>? logger = null)
        {
            _expenseRepository = expenseRepository;
            _groupRepository = groupRepository;
            _calculatorService = calculatorService;
            _notificationRepository = notificationRepository;
            _realtimeService = realtimeService;
            _logger = logger;
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

            // Compute in-app expense notifications (In-app only, no emails)
            var notifications = new List<SplitNotification>();
            var notifiedUserIds = new HashSet<Guid>();

            // 1. Every participant (has a share) except creator
            foreach (var share in shares)
            {
                if (share.UserId == currentUserId) continue; // Rule 1: Never notify actor
                if (!activeMemberIdSet.Contains(share.UserId)) continue; // Rule 8: Only active group members

                notifiedUserIds.Add(share.UserId);
                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = share.UserId,
                    Type = "ExpenseAdded",
                    GroupId = groupId,
                    ReferenceId = expense.Id,
                    Title = "Expense Added",
                    Message = $"{currentUserName} added '{expense.Title}' (Rs. {expense.Amount:N2}). Your share: Rs. {share.ShareAmount:N2}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 2. Also the payer if payer != creator and not already notified as participant
            if (payerId != currentUserId && !notifiedUserIds.Contains(payerId) && activeMemberIdSet.Contains(payerId))
            {
                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = payerId,
                    Type = "ExpenseAdded",
                    GroupId = groupId,
                    ReferenceId = expense.Id,
                    Title = "Expense Added",
                    Message = $"{currentUserName} added '{expense.Title}' (Rs. {expense.Amount:N2}) with you as payer.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var result = await _expenseRepository.CreateExpenseWithSharesAndActivityAsync(
                expense, shares, currentUserName, group.Name, notifications);

            // In-memory test repository support
            if (_notificationRepository != null && _notificationRepository.GetType().Name != "SplitNotificationRepository")
            {
                foreach (var notif in notifications)
                {
                    await _notificationRepository.CreateNotificationAsync(notif);
                }
            }

            // Broadcast realtime notifications
            if (_realtimeService != null && notifications.Count > 0)
            {
                foreach (var notif in notifications)
                {
                    try
                    {
                        await _realtimeService.NotifyUserAsync(notif.UserId, "ReceiveSplitNotification", notif);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to send realtime notification to user {UserId}", notif.UserId);
                    }
                }
            }

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

            // Capture old state for computing diff notifications
            var oldAmount = existingExpense.Amount;
            var oldPayerId = existingExpense.PayerId;
            var oldTitle = existingExpense.Title;
            var oldShares = await _expenseRepository.GetExpenseSharesAsync(existingExpense.Id);
            var oldShareMap = oldShares.ToDictionary(s => s.UserId, s => s.ShareAmount);

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

            var newShareMap = calculatedShares;
            var newTitle = dto.Title.Trim();
            var newAmount = dto.Amount;
            var newPayerId = dto.PayerId;

            var notifications = new List<SplitNotification>();
            var candidateUserIds = oldShareMap.Keys.Union(newShareMap.Keys).ToHashSet();
            if (oldPayerId != Guid.Empty) candidateUserIds.Add(oldPayerId);
            if (newPayerId != Guid.Empty) candidateUserIds.Add(newPayerId);

            foreach (var userId in candidateUserIds)
            {
                if (userId == currentUserId) continue; // Never notify actor
                if (!activeMemberIdSet.Contains(userId)) continue; // Only active members

                bool inOldShares = oldShareMap.TryGetValue(userId, out var oldShare);
                bool inNewShares = newShareMap.TryGetValue(userId, out var newShare);

                string? message = null;

                if (!inOldShares && inNewShares)
                {
                    // Newly added participant
                    message = $"{currentUserName} updated '{newTitle}'. Your share is now Rs. {newShare:N2}";
                }
                else if (inOldShares && !inNewShares)
                {
                    // Removed participant
                    message = $"{currentUserName} removed you from '{newTitle}'.";
                }
                else if (inOldShares && inNewShares)
                {
                    // Continuing participant: check if share amount changed
                    if (oldShare != newShare)
                    {
                        message = $"{currentUserName} updated '{newTitle}'. Your share is now Rs. {newShare:N2}";
                    }
                    else if (oldAmount != newAmount || oldPayerId != newPayerId || oldTitle != newTitle)
                    {
                        // Share stayed identical, but expense details changed
                        message = $"{currentUserName} updated '{newTitle}'.";
                    }
                }
                else
                {
                    // Payer role changes
                    if (userId == newPayerId && oldPayerId != newPayerId)
                    {
                        message = $"{currentUserName} updated '{newTitle}'. You are now listed as payer.";
                    }
                    else if (userId == oldPayerId && oldPayerId != newPayerId)
                    {
                        message = $"{currentUserName} updated '{newTitle}'.";
                    }
                }

                if (message != null)
                {
                    notifications.Add(new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Type = "ExpenseUpdated",
                        GroupId = groupId,
                        ReferenceId = existingExpense.Id,
                        Title = "Expense Updated",
                        Message = message,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var updated = await _expenseRepository.UpdateExpenseWithSharesAndActivityAsync(
                existingExpense, newShares, currentUserName, group.Name, notifications);

            if (updated == null)
                return ApiResponse<SplitExpenseDetailDto>.FailureResponse("Failed to update expense.");

            // In-memory test repository support
            if (_notificationRepository != null && _notificationRepository.GetType().Name != "SplitNotificationRepository")
            {
                foreach (var notif in notifications)
                {
                    await _notificationRepository.CreateNotificationAsync(notif);
                }
            }

            // Realtime SignalR broadcast
            if (_realtimeService != null && notifications.Count > 0)
            {
                foreach (var notif in notifications)
                {
                    try
                    {
                        await _realtimeService.NotifyUserAsync(notif.UserId, "ReceiveSplitNotification", notif);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to send realtime notification to user {UserId}", notif.UserId);
                    }
                }
            }

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

            var shares = await _expenseRepository.GetExpenseSharesAsync(expenseId);
            var activeMembers = await _groupRepository.GetGroupMembersAsync(groupId);
            var activeMemberIdSet = activeMembers.Where(m => m.IsActive).Select(m => m.UserId).ToHashSet();

            var candidateUserIds = shares.Select(s => s.UserId).ToHashSet();
            if (existingExpense.PayerId != Guid.Empty)
            {
                candidateUserIds.Add(existingExpense.PayerId);
            }

            var notifications = new List<SplitNotification>();
            foreach (var userId in candidateUserIds)
            {
                if (userId == currentUserId) continue; // Rule 1: Never notify actor
                if (!activeMemberIdSet.Contains(userId)) continue; // Rule 8: Only active group members

                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = "ExpenseDeleted",
                    GroupId = groupId,
                    ReferenceId = expenseId,
                    Title = "Expense Deleted",
                    Message = $"{currentUserName} deleted '{existingExpense.Title}' (Rs. {existingExpense.Amount:N2})",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var success = await _expenseRepository.SoftDeleteExpenseWithActivityAsync(
                expenseId, groupId, currentUserId, currentUserName, notifications);

            if (success)
            {
                // In-memory test repository support
                if (_notificationRepository != null && _notificationRepository.GetType().Name != "SplitNotificationRepository")
                {
                    foreach (var notif in notifications)
                    {
                        await _notificationRepository.CreateNotificationAsync(notif);
                    }
                }

                // Broadcast realtime notifications
                if (_realtimeService != null && notifications.Count > 0)
                {
                    foreach (var notif in notifications)
                    {
                        try
                        {
                            await _realtimeService.NotifyUserAsync(notif.UserId, "ReceiveSplitNotification", notif);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to send realtime notification to user {UserId}", notif.UserId);
                        }
                    }
                }
            }

            return success
                ? ApiResponse<bool>.SuccessResponse("Expense deleted successfully.", true)
                : ApiResponse<bool>.FailureResponse("Failed to delete expense.");
        }
    }
}
