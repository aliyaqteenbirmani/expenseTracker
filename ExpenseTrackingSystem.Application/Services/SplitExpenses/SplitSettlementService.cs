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
    public class SplitSettlementService : ISplitSettlementService
    {
        private readonly ISplitSettlementRepository _settlementRepository;
        private readonly ISplitGroupRepository _groupRepository;
        private readonly ISplitBalanceRepository _balanceRepository;
        private readonly IDebtSimplificationService _debtSimplificationService;
        private readonly ISplitNotificationRepository? _notificationRepository;
        private readonly ISplitRealtimeService? _realtimeService;
        private readonly ILogger<SplitSettlementService>? _logger;

        public SplitSettlementService(
            ISplitSettlementRepository settlementRepository,
            ISplitGroupRepository groupRepository,
            ISplitBalanceRepository balanceRepository,
            IDebtSimplificationService debtSimplificationService)
            : this(settlementRepository, groupRepository, balanceRepository, debtSimplificationService, null, null, null)
        {
        }

        public SplitSettlementService(
            ISplitSettlementRepository settlementRepository,
            ISplitGroupRepository groupRepository,
            ISplitBalanceRepository balanceRepository,
            IDebtSimplificationService debtSimplificationService,
            ISplitNotificationRepository? notificationRepository,
            ISplitRealtimeService? realtimeService = null,
            ILogger<SplitSettlementService>? logger = null)
        {
            _settlementRepository = settlementRepository;
            _groupRepository = groupRepository;
            _balanceRepository = balanceRepository;
            _debtSimplificationService = debtSimplificationService;
            _notificationRepository = notificationRepository;
            _realtimeService = realtimeService;
            _logger = logger;
        }

        private async Task BroadcastNotificationsAsync(IEnumerable<SplitNotification> notifications)
        {
            if (_realtimeService == null) return;
            foreach (var notif in notifications)
            {
                try
                {
                    await _realtimeService.NotifyUserAsync(notif.UserId, "ReceiveSplitNotification", notif);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to broadcast realtime notification {NotificationId} to user {UserId}", notif.Id, notif.UserId);
                }
            }
        }

        public async Task<ApiResponse<SplitSettlementDto>> RecordSettlementAsync(
            Guid groupId, RecordSettlementDto dto, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Group not found.");

            var currentMembership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (currentMembership == null || !currentMembership.IsActive)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("You must be an active group member to record settlements.");

            if (dto.Amount <= 0)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement amount must be greater than zero.");

            if (dto.FromUserId == dto.ToUserId)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Sender and receiver must be different members.");

            bool isPayer = currentUserId == dto.FromUserId;
            bool isReceiver = currentUserId == dto.ToUserId;
            bool isOwner = group.OwnerId == currentUserId;

            if (!isPayer && !isReceiver && !isOwner)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("You can only submit settlement requests for yourself, or as the group owner.");

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
            var receiverCredit = receiver.NetBalance;

            // Reserved amounts check: Confirmed + Pending from payer to receiver must not exceed outstanding debt/credit
            var existingPending = await _settlementRepository.GetPendingPayerToReceiverAmountAsync(
                groupId, dto.FromUserId, dto.ToUserId);

            if (amount + existingPending > senderDebt + 0.01m)
            {
                var pendingNote = existingPending > 0 ? $" (plus Rs. {existingPending:N2} already pending)" : "";
                return ApiResponse<SplitSettlementDto>.FailureResponse(
                    $"Settlement amount of Rs. {amount:N2}{pendingNote} exceeds {sender.FullName}'s outstanding debt of Rs. {senderDebt:N2}.");
            }

            if (amount + existingPending > receiverCredit + 0.01m)
            {
                var pendingNote = existingPending > 0 ? $" (plus Rs. {existingPending:N2} already pending)" : "";
                return ApiResponse<SplitSettlementDto>.FailureResponse(
                    $"Settlement amount of Rs. {amount:N2}{pendingNote} exceeds {receiver.FullName}'s outstanding credit of Rs. {receiverCredit:N2}.");
            }

            // Determine initial status and audit trail
            string initialStatus;
            Guid? receiverActionBy = null;
            DateTime? receiverActionAt = null;
            Guid? ownerActionBy = null;
            DateTime? ownerActionAt = null;

            if (isReceiver)
            {
                // Receiver-recorded payment ("X paid me"): confirmation is already given by receiver
                receiverActionBy = currentUserId;
                receiverActionAt = DateTime.UtcNow;

                if (isOwner)
                {
                    // Receiver is the group owner: Confirmed immediately
                    initialStatus = SplitSettlementStatus.Confirmed;
                    ownerActionBy = currentUserId;
                    ownerActionAt = DateTime.UtcNow;
                }
                else
                {
                    // Receiver is not owner: waits for group owner final approval
                    initialStatus = SplitSettlementStatus.PendingOwner;
                }
            }
            else if (isPayer)
            {
                // Payer submits (whether member or owner): starts at PendingReceiver waiting for receiver confirmation
                initialStatus = SplitSettlementStatus.PendingReceiver;
            }
            else if (isOwner)
            {
                // Owner records on behalf of members (neither payer nor receiver): Confirmed immediately
                initialStatus = SplitSettlementStatus.Confirmed;
                ownerActionBy = currentUserId;
                ownerActionAt = DateTime.UtcNow;
            }
            else
            {
                initialStatus = SplitSettlementStatus.PendingReceiver;
            }

            var settlement = new SplitSettlement
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                FromUserId = dto.FromUserId,
                ToUserId = dto.ToUserId,
                Amount = amount,
                SettlementDate = dto.SettlementDate ?? DateTime.UtcNow,
                Note = dto.Note?.Trim(),
                Status = initialStatus,
                CreatedBy = currentUserId,
                ReceiverActionBy = receiverActionBy,
                ReceiverActionAt = receiverActionAt,
                OwnerActionBy = ownerActionBy,
                OwnerActionAt = ownerActionAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var notifications = new List<SplitNotification>();

            if (isReceiver)
            {
                // Receiver-recorded payment ("X paid me")
                if (initialStatus == SplitSettlementStatus.PendingOwner)
                {
                    // Payer notification (informational: "Y recorded your payment of Rs. X, waiting for approval")
                    if (dto.FromUserId != currentUserId)
                    {
                        notifications.Add(new SplitNotification
                        {
                            Id = Guid.NewGuid(),
                            UserId = dto.FromUserId,
                            Type = "SettlementRecordedForYou",
                            GroupId = groupId,
                            ReferenceId = settlement.Id,
                            Title = "Payment Recorded For You",
                            Message = $"{receiver.FullName} recorded your payment of Rs. {amount:N2}, waiting for group owner approval.",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Owner notification: Accept / Decline
                    if (group.OwnerId != currentUserId)
                    {
                        notifications.Add(new SplitNotification
                        {
                            Id = Guid.NewGuid(),
                            UserId = group.OwnerId,
                            Type = "SettlementAwaitingOwner",
                            GroupId = groupId,
                            ReferenceId = settlement.Id,
                            Title = "Settlement Awaiting Approval",
                            Message = $"{receiver.FullName} recorded a payment of Rs. {amount:N2} from {sender.FullName}. Group owner approval required.",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else // initialStatus == Confirmed (receiver is owner)
                {
                    // Payer gets informational
                    if (dto.FromUserId != currentUserId)
                    {
                        notifications.Add(new SplitNotification
                        {
                            Id = Guid.NewGuid(),
                            UserId = dto.FromUserId,
                            Type = "SettlementRecordedForYou",
                            GroupId = groupId,
                            ReferenceId = settlement.Id,
                            Title = "Payment Recorded For You",
                            Message = $"{receiver.FullName} recorded and confirmed your payment of Rs. {amount:N2}.",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            else if (isPayer)
            {
                // Payer submits request: Receiver gets Accept / Decline
                if (dto.ToUserId != currentUserId)
                {
                    notifications.Add(new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = dto.ToUserId,
                        Type = "SettlementRequested",
                        GroupId = groupId,
                        ReferenceId = settlement.Id,
                        Title = "Payment Settlement Requested",
                        Message = $"{sender.FullName} recorded a payment of Rs. {amount:N2} to you. Please confirm receipt.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            else if (isOwner)
            {
                // Owner records on behalf of members: informational for payer and receiver
                if (dto.FromUserId != currentUserId)
                {
                    notifications.Add(new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = dto.FromUserId,
                        Type = "SettlementRecordedForYou",
                        GroupId = groupId,
                        ReferenceId = settlement.Id,
                        Title = "Payment Recorded For You",
                        Message = $"{currentUserName} recorded a payment of Rs. {amount:N2} from you to {receiver.FullName}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (dto.ToUserId != currentUserId)
                {
                    notifications.Add(new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = dto.ToUserId,
                        Type = "SettlementRecordedForYou",
                        GroupId = groupId,
                        ReferenceId = settlement.Id,
                        Title = "Payment Recorded For You",
                        Message = $"{currentUserName} recorded a payment of Rs. {amount:N2} to you from {sender.FullName}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var result = await _settlementRepository.CreateSettlementAsync(settlement, notifications);
            await BroadcastNotificationsAsync(notifications);
            EnrichSettlementFlags(result, currentUserId, isOwner);

            string successMessage = initialStatus switch
            {
                SplitSettlementStatus.Confirmed => "Settlement recorded and confirmed successfully.",
                SplitSettlementStatus.PendingOwner => "Settlement recorded. Waiting for owner final approval.",
                _ => "Settlement request submitted. Waiting for receiver confirmation."
            };

            return ApiResponse<SplitSettlementDto>.SuccessResponse(successMessage, result);
        }

        public async Task<ApiResponse<SplitSettlementDto>> ConfirmSettlementAsync(
            Guid groupId, Guid settlementId, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Group not found.");

            var currentMembership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (currentMembership == null || !currentMembership.IsActive)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("You must be an active group member to confirm settlements.");

            var settlement = await _settlementRepository.GetSettlementByIdAsync(settlementId);
            if (settlement == null || settlement.GroupId != groupId || settlement.IsDeleted)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement not found.");

            if (settlement.Status != SplitSettlementStatus.PendingReceiver)
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Settlement has already been processed (current status: {settlement.Status}).");

            if (settlement.ToUserId != currentUserId)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("Only the recipient of this settlement can confirm receipt.");

            // Re-check debt and credit reservations (debt may have changed after expense edits)
            var balances = await _balanceRepository.GetGroupBalancesAsync(groupId);
            var sender = balances.FirstOrDefault(b => b.UserId == settlement.FromUserId);
            var receiver = balances.FirstOrDefault(b => b.UserId == settlement.ToUserId);

            if (sender == null || sender.NetBalance >= -0.005m || receiver == null || receiver.NetBalance <= 0.005m)
            {
                return ApiResponse<SplitSettlementDto>.FailureResponse(
                    "Cannot confirm settlement: The sender no longer has outstanding debt or receiver is no longer owed money in this group. You may decline this request.");
            }

            var senderDebt = Math.Abs(sender.NetBalance);
            var receiverCredit = receiver.NetBalance;
            var otherPending = await _settlementRepository.GetPendingPayerToReceiverAmountAsync(
                groupId, settlement.FromUserId, settlement.ToUserId, excludeSettlementId: settlement.Id);

            if (settlement.Amount + otherPending > senderDebt + 0.01m || settlement.Amount + otherPending > receiverCredit + 0.01m)
            {
                return ApiResponse<SplitSettlementDto>.FailureResponse(
                    $"Cannot confirm settlement: Amount (Rs. {settlement.Amount:N2}) exceeds remaining debt (Rs. {senderDebt:N2}) or credit (Rs. {receiverCredit:N2}). You may decline this request.");
            }

            // Determine next status:
            // If receiver is group owner OR payer is group owner -> Confirmed immediately!
            bool isReceiverOwner = group.OwnerId == currentUserId;
            bool isPayerOwner = group.OwnerId == settlement.FromUserId;
            string nextStatus = (isReceiverOwner || isPayerOwner)
                ? SplitSettlementStatus.Confirmed
                : SplitSettlementStatus.PendingOwner;

            var notifications = new List<SplitNotification>();

            if (nextStatus == SplitSettlementStatus.PendingOwner)
            {
                // Receiver confirms and receiver != owner (and payer != owner)
                // Owner: Accept / Decline
                if (group.OwnerId != currentUserId)
                {
                    notifications.Add(new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = group.OwnerId,
                        Type = "SettlementAwaitingOwner",
                        GroupId = groupId,
                        ReferenceId = settlement.Id,
                        Title = "Settlement Awaiting Approval",
                        Message = $"{sender?.FullName ?? "Member"} paid Rs. {settlement.Amount:N2} to {receiver?.FullName ?? "Member"} (confirmed by {receiver?.FullName ?? "Member"}). Group owner approval required.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Payer: "Receiver confirmed, waiting for admin approval"
                if (settlement.FromUserId != currentUserId)
                {
                    notifications.Add(new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = settlement.FromUserId,
                        Type = "SettlementReceiverConfirmed",
                        GroupId = groupId,
                        ReferenceId = settlement.Id,
                        Title = $"Payment Received by {receiver?.FullName ?? "Recipient"}",
                        Message = $"{receiver?.FullName ?? "Recipient"} confirmed receiving your payment of Rs. {settlement.Amount:N2}. Waiting for group owner final approval.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            else // Confirmed (receiver = owner OR payer = owner)
            {
                // Payer gets Confirmed notification
                if (settlement.FromUserId != currentUserId)
                {
                    notifications.Add(new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = settlement.FromUserId,
                        Type = "SettlementConfirmed",
                        GroupId = groupId,
                        ReferenceId = settlement.Id,
                        Title = "Payment Settlement Confirmed",
                        Message = $"Your payment of Rs. {settlement.Amount:N2} to {receiver?.FullName ?? "Recipient"} was confirmed.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            int rows = await _settlementRepository.ConfirmByReceiverAsync(settlement.Id, nextStatus, currentUserId, notifications);
            if (rows == 0)
            {
                var current = await _settlementRepository.GetSettlementByIdAsync(settlementId);
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Settlement has already been processed (current status: {current?.Status ?? "Unknown"}).");
            }

            await BroadcastNotificationsAsync(notifications);

            var detail = await _settlementRepository.GetSettlementDetailsAsync(settlement.Id, groupId);
            if (detail == null)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Failed to load settlement details.");

            EnrichSettlementFlags(detail, currentUserId, group.OwnerId == currentUserId);

            string message = nextStatus == SplitSettlementStatus.Confirmed
                ? "Settlement confirmed successfully. Balances have been updated."
                : "Receipt confirmed. Waiting for group owner final approval.";

            return ApiResponse<SplitSettlementDto>.SuccessResponse(message, detail);
        }

        public async Task<ApiResponse<SplitSettlementDto>> DeclineSettlementByReceiverAsync(
            Guid groupId, Guid settlementId, string? reason, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Group not found.");

            var currentMembership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (currentMembership == null || !currentMembership.IsActive)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("You must be an active group member to decline settlements.");

            var settlement = await _settlementRepository.GetSettlementByIdAsync(settlementId);
            if (settlement == null || settlement.GroupId != groupId || settlement.IsDeleted)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement not found.");

            if (settlement.Status != SplitSettlementStatus.PendingReceiver)
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Settlement has already been processed (current status: {settlement.Status}).");

            if (settlement.ToUserId != currentUserId)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("Only the recipient of this settlement can decline receipt.");

            var notifications = new List<SplitNotification>();
            if (settlement.FromUserId != currentUserId)
            {
                var reasonSuffix = !string.IsNullOrWhiteSpace(reason) ? $": \"{reason.Trim()}\"" : ".";
                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = settlement.FromUserId,
                    Type = "SettlementDeclined",
                    GroupId = groupId,
                    ReferenceId = settlement.Id,
                    Title = "Payment Settlement Declined",
                    Message = $"{currentUserName} declined your payment of Rs. {settlement.Amount:N2}{reasonSuffix}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            int rows = await _settlementRepository.DeclineByReceiverAsync(settlement.Id, reason, currentUserId, notifications);
            if (rows == 0)
            {
                var current = await _settlementRepository.GetSettlementByIdAsync(settlementId);
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Settlement has already been processed (current status: {current?.Status ?? "Unknown"}).");
            }

            await BroadcastNotificationsAsync(notifications);

            var detail = await _settlementRepository.GetSettlementDetailsAsync(settlement.Id, groupId);
            if (detail != null)
                EnrichSettlementFlags(detail, currentUserId, group.OwnerId == currentUserId);

            return ApiResponse<SplitSettlementDto>.SuccessResponse("Settlement request declined.", detail!);
        }

        public async Task<ApiResponse<SplitSettlementDto>> ApproveSettlementByOwnerAsync(
            Guid groupId, Guid settlementId, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Group not found.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("Only the group owner can give final approval.");

            var settlement = await _settlementRepository.GetSettlementByIdAsync(settlementId);
            if (settlement == null || settlement.GroupId != groupId || settlement.IsDeleted)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement not found.");

            if (settlement.Status != SplitSettlementStatus.PendingOwner)
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Settlement is not waiting for owner approval (current status: {settlement.Status}).");

            // Re-check debt and credit reservations
            var balances = await _balanceRepository.GetGroupBalancesAsync(groupId);
            var sender = balances.FirstOrDefault(b => b.UserId == settlement.FromUserId);
            var receiver = balances.FirstOrDefault(b => b.UserId == settlement.ToUserId);

            if (sender == null || sender.NetBalance >= -0.005m || receiver == null || receiver.NetBalance <= 0.005m)
            {
                return ApiResponse<SplitSettlementDto>.FailureResponse(
                    "Cannot approve settlement: The sender no longer has outstanding debt or receiver is no longer owed money in this group. You may decline this request.");
            }

            var senderDebt = Math.Abs(sender.NetBalance);
            var receiverCredit = receiver.NetBalance;
            var otherPending = await _settlementRepository.GetPendingPayerToReceiverAmountAsync(
                groupId, settlement.FromUserId, settlement.ToUserId, excludeSettlementId: settlement.Id);

            if (settlement.Amount + otherPending > senderDebt + 0.01m || settlement.Amount + otherPending > receiverCredit + 0.01m)
            {
                return ApiResponse<SplitSettlementDto>.FailureResponse(
                    $"Cannot approve settlement: Amount (Rs. {settlement.Amount:N2}) exceeds remaining debt (Rs. {senderDebt:N2}) or credit (Rs. {receiverCredit:N2}). You may decline this request.");
            }

            var notifications = new List<SplitNotification>();

            // Payer gets Confirmed
            if (settlement.FromUserId != currentUserId)
            {
                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = settlement.FromUserId,
                    Type = "SettlementConfirmed",
                    GroupId = groupId,
                    ReferenceId = settlement.Id,
                    Title = "Payment Settlement Confirmed",
                    Message = $"Your payment of Rs. {settlement.Amount:N2} to {receiver?.FullName ?? "Recipient"} was approved by {currentUserName}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Receiver gets Confirmed
            if (settlement.ToUserId != currentUserId)
            {
                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = settlement.ToUserId,
                    Type = "SettlementConfirmed",
                    GroupId = groupId,
                    ReferenceId = settlement.Id,
                    Title = "Payment Settlement Confirmed",
                    Message = $"Payment of Rs. {settlement.Amount:N2} from {sender?.FullName ?? "Member"} was approved by {currentUserName}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            int rows = await _settlementRepository.ApproveByOwnerAsync(settlement.Id, currentUserId, notifications);
            if (rows == 0)
            {
                var current = await _settlementRepository.GetSettlementByIdAsync(settlementId);
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Settlement has already been processed (current status: {current?.Status ?? "Unknown"}).");
            }

            await BroadcastNotificationsAsync(notifications);

            var detail = await _settlementRepository.GetSettlementDetailsAsync(settlement.Id, groupId);
            if (detail != null)
                EnrichSettlementFlags(detail, currentUserId, true);

            return ApiResponse<SplitSettlementDto>.SuccessResponse("Settlement approved and confirmed. Balances have been updated.", detail!);
        }

        public async Task<ApiResponse<SplitSettlementDto>> DeclineSettlementByOwnerAsync(
            Guid groupId, Guid settlementId, string? reason, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Group not found.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("Only the group owner can decline this settlement.");

            var settlement = await _settlementRepository.GetSettlementByIdAsync(settlementId);
            if (settlement == null || settlement.GroupId != groupId || settlement.IsDeleted)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement not found.");

            if (settlement.Status != SplitSettlementStatus.PendingOwner)
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Settlement is not waiting for owner approval (current status: {settlement.Status}).");

            var balances = await _balanceRepository.GetGroupBalancesAsync(groupId);
            var sender = balances.FirstOrDefault(b => b.UserId == settlement.FromUserId);
            var receiver = balances.FirstOrDefault(b => b.UserId == settlement.ToUserId);

            var notifications = new List<SplitNotification>();
            var reasonSuffix = !string.IsNullOrWhiteSpace(reason) ? $": \"{reason.Trim()}\"" : ".";

            // Payer gets Declined
            if (settlement.FromUserId != currentUserId)
            {
                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = settlement.FromUserId,
                    Type = "SettlementDeclined",
                    GroupId = groupId,
                    ReferenceId = settlement.Id,
                    Title = "Payment Settlement Declined",
                    Message = $"{currentUserName} declined the settlement of Rs. {settlement.Amount:N2} to {receiver?.FullName ?? "Recipient"}{reasonSuffix}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Receiver gets Declined
            if (settlement.ToUserId != currentUserId)
            {
                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = settlement.ToUserId,
                    Type = "SettlementDeclined",
                    GroupId = groupId,
                    ReferenceId = settlement.Id,
                    Title = "Payment Settlement Declined",
                    Message = $"{currentUserName} declined the settlement of Rs. {settlement.Amount:N2} from {sender?.FullName ?? "Member"}{reasonSuffix}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            int rows = await _settlementRepository.DeclineByOwnerAsync(settlement.Id, reason, currentUserId, notifications);
            if (rows == 0)
            {
                var current = await _settlementRepository.GetSettlementByIdAsync(settlementId);
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Settlement has already been processed (current status: {current?.Status ?? "Unknown"}).");
            }

            await BroadcastNotificationsAsync(notifications);

            var detail = await _settlementRepository.GetSettlementDetailsAsync(settlement.Id, groupId);
            if (detail != null)
                EnrichSettlementFlags(detail, currentUserId, true);

            return ApiResponse<SplitSettlementDto>.SuccessResponse("Settlement declined by owner.", detail!);
        }

        public async Task<ApiResponse<SplitSettlementDto>> CancelSettlementAsync(
            Guid groupId, Guid settlementId, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Group not found.");

            var settlement = await _settlementRepository.GetSettlementByIdAsync(settlementId);
            if (settlement == null || settlement.GroupId != groupId || settlement.IsDeleted)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement not found.");

            if (settlement.FromUserId != currentUserId && settlement.CreatedBy != currentUserId)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("Only the payer or creator can cancel this settlement request.");

            if (settlement.Status != SplitSettlementStatus.PendingReceiver && settlement.Status != SplitSettlementStatus.PendingOwner)
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Only pending settlements can be cancelled (current status: {settlement.Status}).");

            Guid pendingApproverId = settlement.Status == SplitSettlementStatus.PendingOwner
                ? group.OwnerId
                : settlement.ToUserId;

            int rows = await _settlementRepository.CancelByPayerAsync(settlement.Id, currentUserId);
            if (rows == 0)
            {
                var current = await _settlementRepository.GetSettlementByIdAsync(settlementId);
                return ApiResponse<SplitSettlementDto>.FailureResponse($"Settlement has already been processed (current status: {current?.Status ?? "Unknown"}).");
            }

            if (pendingApproverId != currentUserId && _realtimeService != null)
            {
                try
                {
                    await _realtimeService.NotifyUserAsync(pendingApproverId, "ReceiveSplitNotification", new
                    {
                        Id = Guid.NewGuid(),
                        UserId = pendingApproverId,
                        Type = "SettlementCancelled",
                        GroupId = groupId,
                        ReferenceId = settlement.Id,
                        Title = "Settlement Cancelled",
                        Message = $"Settlement request of Rs. {settlement.Amount:N2} was cancelled by payer."
                    });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to broadcast cancellation notification to approver {UserId}", pendingApproverId);
                }
            }

            var detail = await _settlementRepository.GetSettlementDetailsAsync(settlement.Id, groupId);
            if (detail != null)
                EnrichSettlementFlags(detail, currentUserId, group.OwnerId == currentUserId);

            return ApiResponse<SplitSettlementDto>.SuccessResponse("Settlement request cancelled.", detail!);
        }

        public async Task<ApiResponse<bool>> ReverseSettlementAsync(
            Guid groupId, Guid settlementId, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<bool>.FailureResponse("Group not found.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<bool>.ForbiddenResponse("Only the group owner can reverse confirmed settlements.");

            var settlement = await _settlementRepository.GetSettlementByIdAsync(settlementId);
            if (settlement == null || settlement.GroupId != groupId || settlement.IsDeleted)
                return ApiResponse<bool>.FailureResponse("Settlement not found for this group.");

            if (settlement.Status != SplitSettlementStatus.Confirmed)
                return ApiResponse<bool>.FailureResponse($"Only confirmed settlements can be reversed (current status: {settlement.Status}).");

            var balances = await _balanceRepository.GetGroupBalancesAsync(groupId);
            var sender = balances.FirstOrDefault(b => b.UserId == settlement.FromUserId);
            var receiver = balances.FirstOrDefault(b => b.UserId == settlement.ToUserId);

            var notifications = new List<SplitNotification>();

            // Payer gets Reversed
            if (settlement.FromUserId != currentUserId)
            {
                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = settlement.FromUserId,
                    Type = "SettlementReversed",
                    GroupId = groupId,
                    ReferenceId = settlement.Id,
                    Title = "Payment Settlement Reversed",
                    Message = $"The settlement of Rs. {settlement.Amount:N2} to {receiver?.FullName ?? "Recipient"} was reversed by {currentUserName}. Balances have been restored.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Receiver gets Reversed
            if (settlement.ToUserId != currentUserId)
            {
                notifications.Add(new SplitNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = settlement.ToUserId,
                    Type = "SettlementReversed",
                    GroupId = groupId,
                    ReferenceId = settlement.Id,
                    Title = "Payment Settlement Reversed",
                    Message = $"The settlement of Rs. {settlement.Amount:N2} from {sender?.FullName ?? "Member"} was reversed by {currentUserName}. Balances have been restored.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var success = await _settlementRepository.ReverseConfirmedSettlementAsync(
                settlementId, groupId, currentUserId, notifications);

            if (success)
            {
                await BroadcastNotificationsAsync(notifications);
            }

            return success
                ? ApiResponse<bool>.SuccessResponse("Settlement reversed successfully. Balances have been restored.", true)
                : ApiResponse<bool>.FailureResponse("Failed to reverse settlement.");
        }

        public async Task<ApiResponse<List<SplitSettlementDto>>> GetPendingActionsAsync(
            Guid groupId, Guid currentUserId)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<List<SplitSettlementDto>>.FailureResponse("Group not found.");

            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<List<SplitSettlementDto>>.ForbiddenResponse("You must be an active member to view pending actions.");

            bool isOwner = group.OwnerId == currentUserId;
            var actions = await _settlementRepository.GetPendingActionsAsync(groupId, currentUserId, isOwner);

            foreach (var a in actions)
                EnrichSettlementFlags(a, currentUserId, isOwner);

            return ApiResponse<List<SplitSettlementDto>>.SuccessResponse("Pending actions retrieved successfully.", actions);
        }

        public async Task<ApiResponse<List<SplitSettlementDto>>> GetGroupSettlementsAsync(
            Guid groupId, Guid currentUserId)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<List<SplitSettlementDto>>.FailureResponse("Group not found.");

            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<List<SplitSettlementDto>>.ForbiddenResponse("You must be an active member to view settlements.");

            bool isOwner = group.OwnerId == currentUserId;
            var settlements = await _settlementRepository.GetGroupSettlementsAsync(groupId, currentUserId, isOwner);

            foreach (var s in settlements)
                EnrichSettlementFlags(s, currentUserId, isOwner);

            return ApiResponse<List<SplitSettlementDto>>.SuccessResponse("Settlements retrieved successfully.", settlements);
        }

        public async Task<ApiResponse<SplitSettlementDto>> GetSettlementDetailsAsync(
            Guid groupId, Guid settlementId, Guid currentUserId)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Group not found.");

            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("You must be an active member to view settlement details.");

            var detail = await _settlementRepository.GetSettlementDetailsAsync(settlementId, groupId);
            if (detail == null)
                return ApiResponse<SplitSettlementDto>.FailureResponse("Settlement not found.");

            bool isOwner = group.OwnerId == currentUserId;
            bool isParty = detail.FromUserId == currentUserId || detail.ToUserId == currentUserId;

            if (!isOwner && !isParty)
                return ApiResponse<SplitSettlementDto>.ForbiddenResponse("You do not have permission to view this settlement.");

            EnrichSettlementFlags(detail, currentUserId, isOwner);
            return ApiResponse<SplitSettlementDto>.SuccessResponse("Settlement details retrieved successfully.", detail);
        }

        public Task<ApiResponse<SplitSettlementDto>> UpdateSettlementAsync(
            Guid groupId, Guid settlementId, UpdateSettlementDto dto, Guid currentUserId, string currentUserName)
        {
            // Amount rule: Settlement amounts cannot be edited. Payer can only cancel and resubmit.
            return Task.FromResult(ApiResponse<SplitSettlementDto>.FailureResponse(
                "Settlement amounts cannot be edited. Please cancel this settlement and submit a new request."));
        }

        public Task<ApiResponse<bool>> DeleteSettlementAsync(
            Guid groupId, Guid settlementId, Guid currentUserId, string currentUserName)
        {
            // Delete delegates to ReverseSettlementAsync (owner only)
            return ReverseSettlementAsync(groupId, settlementId, currentUserId, currentUserName);
        }

        public async Task<ApiResponse<SettleUpOverviewDto>> GetSettleUpOverviewAsync(
            Guid groupId, Guid currentUserId)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || !group.IsActive)
                return ApiResponse<SettleUpOverviewDto>.FailureResponse("Group not found.");

            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<SettleUpOverviewDto>.ForbiddenResponse("You must be an active member to view settle up details.");

            var balances = await _balanceRepository.GetGroupBalancesAsync(groupId);

            // Fetch pending settlements in this group to subtract pending amounts from debt suggestions
            var pendingSettlements = await _settlementRepository.GetPendingSettlementsAsync(groupId);
            var pendingPaidByUser = pendingSettlements.GroupBy(p => p.FromUserId).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
            var pendingReceivedByUser = pendingSettlements.GroupBy(p => p.ToUserId).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            // Create adjusted balance items for debt simplification so pending amounts are not prompted to be paid twice
            var adjustedBalances = balances.Select(b =>
            {
                decimal pPaid = pendingPaidByUser.TryGetValue(b.UserId, out var pp) ? pp : 0m;
                decimal pReceived = pendingReceivedByUser.TryGetValue(b.UserId, out var pr) ? pr : 0m;
                decimal effectiveNet = b.NetBalance + pPaid - pReceived;

                return new MemberBalanceDto
                {
                    UserId = b.UserId,
                    FullName = b.FullName,
                    Email = b.Email,
                    Role = b.Role,
                    IsActive = b.IsActive,
                    TotalPaid = b.TotalPaid,
                    TotalShare = b.TotalShare,
                    SettlementsPaid = b.SettlementsPaid,
                    SettlementsReceived = b.SettlementsReceived,
                    PendingPaid = pPaid,
                    PendingReceived = pReceived,
                    NetBalance = effectiveNet, // Debt simplification uses effective remaining debt
                    StatusDescription = b.StatusDescription
                };
            }).ToList();

            var suggestions = _debtSimplificationService.SimplifyDebts(adjustedBalances);

            var debtors = balances.Where(b => b.NetBalance < -0.005m).ToList();
            var creditors = balances.Where(b => b.NetBalance > 0.005m).ToList();
            var totalDebt = debtors.Sum(d => Math.Abs(d.NetBalance));

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

        private static void EnrichSettlementFlags(SplitSettlementDto dto, Guid currentUserId, bool isOwner)
        {
            dto.CanConfirm = dto.Status == SplitSettlementStatus.PendingReceiver && dto.ToUserId == currentUserId;
            dto.CanDecline = (dto.Status == SplitSettlementStatus.PendingReceiver && dto.ToUserId == currentUserId)
                          || (dto.Status == SplitSettlementStatus.PendingOwner && isOwner);
            dto.CanCancel = (dto.Status == SplitSettlementStatus.PendingReceiver || dto.Status == SplitSettlementStatus.PendingOwner)
                         && (dto.FromUserId == currentUserId || dto.CreatedBy == currentUserId);
            dto.CanReverse = dto.Status == SplitSettlementStatus.Confirmed && isOwner;
        }
    }
}
