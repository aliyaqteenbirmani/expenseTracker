using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.EmailService;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class SplitGroupService : ISplitGroupService
    {
        private readonly ISplitGroupRepository _groupRepository;
        private readonly ISplitNotificationRepository _notificationRepository;
        private readonly IEmailService? _emailService;
        private readonly ISplitRealtimeService? _realtimeService;
        private readonly AppLinkSettings _appLinkSettings;
        private readonly ILogger<SplitGroupService> _logger;

        public SplitGroupService(ISplitGroupRepository groupRepository)
            : this(
                groupRepository,
                new NullSplitNotificationRepository(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SplitGroupService>.Instance)
        {
        }

        public SplitGroupService(
            ISplitGroupRepository groupRepository,
            ISplitNotificationRepository notificationRepository,
            ILogger<SplitGroupService> logger,
            IEmailService? emailService = null,
            ISplitRealtimeService? realtimeService = null,
            IOptions<AppLinkSettings>? appLinkSettings = null)
        {
            _groupRepository = groupRepository;
            _notificationRepository = notificationRepository;
            _logger = logger;
            _emailService = emailService;
            _realtimeService = realtimeService;
            _appLinkSettings = appLinkSettings?.Value ?? new AppLinkSettings
            {
                InvitationBaseUrl = "http://localhost:5173"
            };
        }

        public async Task<ApiResponse<CreateSplitGroupResultDto>> CreateGroupAsync(CreateSplitGroupDto dto, Guid userId, string userName)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponse<CreateSplitGroupResultDto>.FailureResponse("Group name is required.");

            if (dto.Name.Trim().Length > 150)
                return ApiResponse<CreateSplitGroupResultDto>.FailureResponse("Group name cannot exceed 150 characters.");

            var inviteesInput = dto.Invitees ?? new List<CreateGroupInviteeInputDto>();

            // Enforce limit of 20 invitees
            if (inviteesInput.Count > 20)
                return ApiResponse<CreateSplitGroupResultDto>.FailureResponse("Cannot invite more than 20 members per request.");

            // Validate invitees for duplicates, self, and format
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenUserIds = new HashSet<Guid>();
            var preparedInvitees = new List<(Guid? targetUserId, string targetEmail, string displayName)>();

            foreach (var item in inviteesInput)
            {
                var rawEmail = item.Email?.Trim();
                var targetId = item.UserId;

                if (string.IsNullOrWhiteSpace(rawEmail) && !targetId.HasValue)
                    return ApiResponse<CreateSplitGroupResultDto>.FailureResponse("Each invitee must have a valid email or user ID.");

                // Self check by ID
                if (targetId.HasValue && targetId.Value == userId)
                    return ApiResponse<CreateSplitGroupResultDto>.FailureResponse("You cannot invite yourself to the group.");

                // Format validation if email provided
                if (!string.IsNullOrWhiteSpace(rawEmail))
                {
                    if (!new EmailAddressAttribute().IsValid(rawEmail))
                        return ApiResponse<CreateSplitGroupResultDto>.FailureResponse($"Invalid email address format: '{rawEmail}'.");

                    if (seenEmails.Contains(rawEmail))
                        return ApiResponse<CreateSplitGroupResultDto>.FailureResponse($"Duplicate invitee found: '{rawEmail}'.");

                    seenEmails.Add(rawEmail);
                }

                if (targetId.HasValue)
                {
                    if (seenUserIds.Contains(targetId.Value))
                        return ApiResponse<CreateSplitGroupResultDto>.FailureResponse("Duplicate invitee user ID found.");

                    seenUserIds.Add(targetId.Value);
                }

                // Look up user in database
                var userRecord = await _groupRepository.GetUserByEmailOrIdAsync(rawEmail, targetId);
                if (userRecord != null)
                {
                    if (userRecord.Id == userId)
                        return ApiResponse<CreateSplitGroupResultDto>.FailureResponse("You cannot invite yourself to the group.");

                    var effEmail = (userRecord.Email ?? rawEmail ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(effEmail) && !seenEmails.Contains(effEmail))
                        seenEmails.Add(effEmail);

                    preparedInvitees.Add((userRecord.Id, effEmail, userRecord.FirstName ?? item.Name ?? effEmail));
                }
                else
                {
                    preparedInvitees.Add((null, rawEmail!, item.Name ?? rawEmail!));
                }
            }

            var now = DateTime.UtcNow;
            var group = new SplitGroup
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Icon = dto.Icon?.Trim(),
                OwnerId = userId,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
                IsDeleted = false
            };

            var invitations = new List<SplitGroupInvitation>();
            var activities = new List<SplitGroupActivity>();

            foreach (var inv in preparedInvitees)
            {
                var tokenBytes = RandomNumberGenerator.GetBytes(32);
                var rawToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
                var tokenHash = TokenUtils.HashToken(rawToken);

                var invitation = new SplitGroupInvitation
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    InvitedUserId = inv.targetUserId,
                    InvitedEmail = inv.targetEmail,
                    InvitedBy = userId,
                    Status = "Pending",
                    InvitationToken = rawToken,
                    TokenHash = tokenHash,
                    ExpiresAt = now.AddDays(7),
                    CreatedAt = now,
                    LastSentAt = now,
                    ResendCount = 0
                };
                invitations.Add(invitation);

                var activity = new SplitGroupActivity
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    UserId = userId,
                    ActivityType = "MemberInvited",
                    ReferenceId = invitation.Id,
                    Description = $"{userName} invited {inv.displayName} to the group.",
                    CreatedAt = now
                };
                activities.Add(activity);
            }

            // Execute atomic transaction for group, owner membership, activities, and invitations
            await _groupRepository.CreateGroupWithMemberAndInvitationsAsync(group, userId, userName, invitations, activities);

            // After commit: send notifications to registered users and emails to unregistered users
            var inviteeResults = new List<CreateGroupInviteeResultDto>();

            foreach (var invitation in invitations)
            {
                var isRegistered = invitation.InvitedUserId.HasValue;
                var identifier = invitation.InvitedEmail;

                try
                {
                    if (isRegistered)
                    {
                        var notification = new SplitNotification
                        {
                            Id = Guid.NewGuid(),
                            UserId = invitation.InvitedUserId!.Value,
                            Type = "GroupInvitationReceived",
                            GroupId = group.Id,
                            ReferenceId = invitation.Id,
                            Title = "Group Invitation",
                            Message = $"{userName} invited you to join '{group.Name}'.",
                            IsRead = false,
                            CreatedAt = now
                        };

                        await _notificationRepository.CreateNotificationAsync(notification);

                        if (_realtimeService != null)
                        {
                            await _realtimeService.NotifyUserAsync(invitation.InvitedUserId.Value, "ReceiveSplitNotification", notification);
                            await _realtimeService.NotifyEmailAsync(invitation.InvitedEmail, "ReceiveSplitNotification", notification);
                        }
                    }
                    else
                    {
                        await TrySendInvitationEmailAsync(group.Name, userName, invitation.InvitedEmail, invitation.InvitationToken, invitation.ExpiresAt);
                    }

                    inviteeResults.Add(new CreateGroupInviteeResultDto
                    {
                        Identifier = identifier,
                        Status = "Invited",
                        Message = isRegistered ? "In-app invitation notification sent." : "Invitation email sent.",
                        InvitationId = invitation.Id
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deliver post-creation invitation for {Identifier}", identifier);
                    inviteeResults.Add(new CreateGroupInviteeResultDto
                    {
                        Identifier = identifier,
                        Status = "Failed",
                        Message = "Invitation recorded, but delivery failed. You can resend from group settings.",
                        InvitationId = invitation.Id
                    });
                }
            }

            var result = new CreateSplitGroupResultDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                Icon = group.Icon,
                OwnerId = userId,
                OwnerName = userName,
                OwnerEmail = string.Empty,
                MemberCount = 1,
                TotalExpenses = 0,
                CurrentUserBalance = 0,
                CurrentUserRole = "Owner",
                CreatedAt = group.CreatedAt,
                Invitations = inviteeResults
            };

            return ApiResponse<CreateSplitGroupResultDto>.SuccessResponse("Split group created successfully.", result);
        }

        public async Task<ApiResponse<List<SplitUserSearchResultDto>>> SearchUsersAsync(string query, Guid currentUserId)
        {
            var users = await _groupRepository.SearchUsersAsync(query, currentUserId);
            return ApiResponse<List<SplitUserSearchResultDto>>.SuccessResponse("Users found successfully.", users);
        }

        public async Task<ApiResponse<List<SplitGroupSummaryDto>>> GetMyGroupsAsync(Guid userId)
        {
            var groups = await _groupRepository.GetUserGroupsAsync(userId);
            return ApiResponse<List<SplitGroupSummaryDto>>.SuccessResponse("Split groups retrieved successfully.", groups);
        }

        public async Task<ApiResponse<SplitGroupDetailDto>> GetGroupDetailsAsync(Guid groupId, Guid userId)
        {
            var detail = await _groupRepository.GetGroupDetailsAsync(groupId, userId);
            if (detail == null)
                return ApiResponse<SplitGroupDetailDto>.FailureResponse("Group not found or you are not an active member.");

            return ApiResponse<SplitGroupDetailDto>.SuccessResponse("Group details retrieved successfully.", detail);
        }

        public async Task<ApiResponse<List<SplitGroupMemberDto>>> GetGroupMembersAsync(Guid groupId, Guid userId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, userId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<List<SplitGroupMemberDto>>.ForbiddenResponse("You are not an active member of this group.");

            var members = await _groupRepository.GetGroupMembersAsync(groupId);
            return ApiResponse<List<SplitGroupMemberDto>>.SuccessResponse("Members retrieved successfully.", members);
        }

        public async Task<ApiResponse<bool>> UpdateGroupAsync(Guid groupId, UpdateSplitGroupDto dto, Guid userId, string userName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<bool>.FailureResponse("Group not found.");

            if (group.OwnerId != userId)
                return ApiResponse<bool>.ForbiddenResponse("Only the group owner can update group settings.");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                group.Name = dto.Name.Trim();

            if (dto.Description != null)
                group.Description = dto.Description.Trim();

            if (dto.Icon != null)
                group.Icon = dto.Icon.Trim();

            group.UpdatedAt = DateTime.UtcNow;

            var success = await _groupRepository.UpdateGroupAsync(group, userId, userName);
            return success
                ? ApiResponse<bool>.SuccessResponse("Group updated successfully.", true)
                : ApiResponse<bool>.FailureResponse("Failed to update group.");
        }

        public async Task<ApiResponse<bool>> DeleteGroupAsync(Guid groupId, Guid userId, string userName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<bool>.FailureResponse("Group not found.");

            if (group.OwnerId != userId)
                return ApiResponse<bool>.ForbiddenResponse("Only the group owner can delete this group.");

            var pendingSettlementsCount = await _groupRepository.GetPendingSettlementsCountAsync(groupId);
            if (pendingSettlementsCount > 0)
            {
                return ApiResponse<bool>.FailureResponse(
                    $"Cannot delete or archive group while {pendingSettlementsCount} settlement request(s) are pending approval. Please accept, decline, or cancel all pending requests first.");
            }

            var hasFinances = await _groupRepository.HasFinancialRecordsAsync(groupId);
            if (hasFinances)
                return ApiResponse<bool>.FailureResponse("Cannot delete group because financial records (expenses or settlements) exist. You may deactivate it instead.");

            var success = await _groupRepository.SoftDeleteGroupAsync(groupId, userId, userName);
            return success
                ? ApiResponse<bool>.SuccessResponse("Group archived successfully.", true)
                : ApiResponse<bool>.FailureResponse("Failed to archive group.");
        }

        public async Task<ApiResponse<SplitGroupInvitationDto>> InviteMemberAsync(
            Guid groupId, InviteSplitGroupMemberDto dto, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Group not found.");

            if (!group.IsActive)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Cannot invite members to an inactive group.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<SplitGroupInvitationDto>.ForbiddenResponse("Only the group owner can invite members.");

            // Extract email & user ID cleanly
            var rawEmail = (dto.InvitedEmail ?? dto.Email)?.Trim();
            var targetUserId = dto.InvitedUserId ?? dto.UserId;

            if (string.IsNullOrWhiteSpace(rawEmail) && !targetUserId.HasValue)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Please provide an email address or user ID to invite.");

            // Prevent self-invitation
            if (targetUserId.HasValue && targetUserId.Value == currentUserId)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("You cannot invite yourself to the group.");

            // Look up existing user if any
            var targetUser = await _groupRepository.GetUserByEmailOrIdAsync(rawEmail, targetUserId);

            if (targetUser != null && targetUser.Id == currentUserId)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("You cannot invite yourself to the group.");

            string effectiveEmail;
            Guid? effectiveUserId;

            if (targetUser != null)
            {
                // CASE 1: Registered user
                effectiveEmail = (targetUser.Email ?? rawEmail ?? string.Empty).Trim();
                effectiveUserId = targetUser.Id;

                // Check if already an active member
                var existingMembership = await _groupRepository.GetMembershipAsync(groupId, targetUser.Id);
                if (existingMembership != null && existingMembership.IsActive)
                    return ApiResponse<SplitGroupInvitationDto>.FailureResponse("User is already an active member of this group.");
            }
            else
            {
                // CASE 2: Unregistered user
                if (string.IsNullOrWhiteSpace(rawEmail))
                    return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Please provide a valid email address.");

                if (!new EmailAddressAttribute().IsValid(rawEmail))
                    return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Please provide a valid email address.");

                effectiveEmail = rawEmail;
                effectiveUserId = null;
            }

            // Check if active pending invitation exists for this group + email
            var existingPendingInvite = await _groupRepository.GetPendingInvitationByEmailAsync(groupId, effectiveEmail);
            if (existingPendingInvite != null && existingPendingInvite.ExpiresAt > DateTime.UtcNow)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("An active pending invitation has already been sent to this user.");

            // Generate secure unguessable invitation token
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var rawToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
            var tokenHash = TokenUtils.HashToken(rawToken);

            var now = DateTime.UtcNow;
            var invitation = new SplitGroupInvitation
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                InvitedUserId = effectiveUserId,
                InvitedEmail = effectiveEmail,
                InvitedBy = currentUserId,
                Status = "Pending",
                InvitationToken = rawToken,
                TokenHash = tokenHash,
                ExpiresAt = now.AddDays(7),
                CreatedAt = now,
                LastSentAt = now,
                ResendCount = 0
            };

            await _groupRepository.CreateInvitationAsync(invitation, currentUserName, group.Name);

            // If registered user, create in-app notification & broadcast realtime
            if (effectiveUserId.HasValue)
            {
                try
                {
                    var notification = new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = effectiveUserId.Value,
                        Type = "GroupInvitationReceived",
                        GroupId = groupId,
                        ReferenceId = invitation.Id,
                        Title = "Group Invitation",
                        Message = $"{currentUserName} invited you to join '{group.Name}'.",
                        IsRead = false,
                        CreatedAt = now
                    };

                    await _notificationRepository.CreateNotificationAsync(notification);

                    if (_realtimeService != null)
                    {
                        await _realtimeService.NotifyUserAsync(effectiveUserId.Value, "ReceiveSplitNotification", notification);
                        await _realtimeService.NotifyEmailAsync(effectiveEmail, "ReceiveSplitNotification", notification);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create in-app notification for user {UserId}", effectiveUserId.Value);
                }
            }

            // Send invitation email
            await TrySendInvitationEmailAsync(group.Name, currentUserName, effectiveEmail, rawToken, invitation.ExpiresAt);

            var resultDto = new SplitGroupInvitationDto
            {
                Id = invitation.Id,
                GroupId = groupId,
                GroupName = group.Name,
                InvitedUserId = effectiveUserId,
                InvitedEmail = effectiveEmail,
                InvitedBy = currentUserId,
                InvitedByName = currentUserName,
                Status = invitation.Status,
                ExpiresAt = invitation.ExpiresAt,
                CreatedAt = invitation.CreatedAt,
                LastSentAt = invitation.LastSentAt,
                ResendCount = invitation.ResendCount,
                InvitationToken = rawToken
            };

            return ApiResponse<SplitGroupInvitationDto>.SuccessResponse("Invitation sent successfully.", resultDto);
        }

        public async Task<ApiResponse<BatchInviteResultDto>> BatchInviteMembersAsync(
            Guid groupId, BatchInviteMembersDto dto, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<BatchInviteResultDto>.FailureResponse("Group not found.");

            if (!group.IsActive)
                return ApiResponse<BatchInviteResultDto>.FailureResponse("Cannot invite members to an inactive group.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<BatchInviteResultDto>.ForbiddenResponse("Only the group owner can invite members.");

            if (dto?.Invitees == null || dto.Invitees.Count == 0)
                return ApiResponse<BatchInviteResultDto>.FailureResponse("Please specify at least one member or email to invite.");

            if (dto.Invitees.Count > 20)
                return ApiResponse<BatchInviteResultDto>.FailureResponse("Cannot invite more than 20 members at once.");

            var results = new List<CreateGroupInviteeResultDto>();

            foreach (var invitee in dto.Invitees)
            {
                var identifier = !string.IsNullOrWhiteSpace(invitee.Name)
                    ? invitee.Name.Trim()
                    : (!string.IsNullOrWhiteSpace(invitee.Email) ? invitee.Email.Trim() : invitee.UserId?.ToString() ?? "Unknown");

                try
                {
                    var singleDto = new InviteSplitGroupMemberDto
                    {
                        InvitedUserId = invitee.UserId,
                        UserId = invitee.UserId,
                        InvitedEmail = invitee.Email?.Trim(),
                        Email = invitee.Email?.Trim()
                    };

                    var singleResult = await InviteMemberAsync(groupId, singleDto, currentUserId, currentUserName);
                    if (singleResult.Success)
                    {
                        results.Add(new CreateGroupInviteeResultDto
                        {
                            Identifier = identifier,
                            Status = "Invited",
                            Message = singleResult.Message ?? "Invitation sent successfully.",
                            InvitationId = singleResult.Data?.Id
                        });
                    }
                    else
                    {
                        var status = "Failed";
                        var msg = singleResult.Message ?? "Failed to invite.";
                        if (msg.Contains("already an active member", StringComparison.OrdinalIgnoreCase))
                        {
                            status = "AlreadyMember";
                        }
                        else if (msg.Contains("pending invitation", StringComparison.OrdinalIgnoreCase))
                        {
                            status = "AlreadyPending";
                        }
                        else if (msg.Contains("yourself", StringComparison.OrdinalIgnoreCase))
                        {
                            status = "Self";
                        }

                        results.Add(new CreateGroupInviteeResultDto
                        {
                            Identifier = identifier,
                            Status = status,
                            Message = msg
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process batch invitation for {Identifier}", identifier);
                    results.Add(new CreateGroupInviteeResultDto
                    {
                        Identifier = identifier,
                        Status = "Failed",
                        Message = ex.Message
                    });
                }
            }

            var batchResult = new BatchInviteResultDto
            {
                Results = results
            };

            return ApiResponse<BatchInviteResultDto>.SuccessResponse(
                $"Processed {batchResult.TotalCount} invitations: {batchResult.SuccessCount} sent, {batchResult.FailureCount} skipped or failed.",
                batchResult);
        }

        public async Task<ApiResponse<SplitGroupInvitationDto>> ResendInvitationAsync(
            Guid groupId, Guid invitationId, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Group not found.");

            if (!group.IsActive)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Cannot resend invitations for an inactive group.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<SplitGroupInvitationDto>.ForbiddenResponse("Only the group owner can resend invitations.");

            var invitation = await _groupRepository.GetInvitationByIdAsync(invitationId);
            if (invitation == null || invitation.GroupId != groupId)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Invitation not found for this group.");

            if (invitation.Status != "Pending")
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse($"Cannot resend invitation with status '{invitation.Status}'.");

            // Throttle resending (at most once every 60 seconds)
            if (invitation.LastSentAt.HasValue && invitation.LastSentAt.Value > DateTime.UtcNow.AddSeconds(-60))
            {
                var remainingSec = 60 - (int)(DateTime.UtcNow - invitation.LastSentAt.Value).TotalSeconds;
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse($"Please wait {Math.Max(1, remainingSec)}s before resending.");
            }

            await _groupRepository.UpdateInvitationResendAsync(invitationId);

            // Send email
            await TrySendInvitationEmailAsync(
                group.Name,
                currentUserName,
                invitation.InvitedEmail,
                invitation.InvitationToken ?? string.Empty,
                invitation.ExpiresAt);

            // If user has account, re-notify via realtime
            if (invitation.InvitedUserId.HasValue && _realtimeService != null)
            {
                await _realtimeService.NotifyUserAsync(invitation.InvitedUserId.Value, "ReceiveSplitNotification", new
                {
                    Type = "GroupInvitationReceived",
                    GroupId = groupId,
                    ReferenceId = invitation.Id,
                    Message = $"{currentUserName} resent the invitation to join '{group.Name}'."
                });
            }

            var updatedInvite = await _groupRepository.GetInvitationByIdAsync(invitationId);

            var resultDto = new SplitGroupInvitationDto
            {
                Id = invitation.Id,
                GroupId = groupId,
                GroupName = group.Name,
                InvitedUserId = invitation.InvitedUserId,
                InvitedEmail = invitation.InvitedEmail,
                InvitedBy = currentUserId,
                InvitedByName = currentUserName,
                Status = invitation.Status,
                ExpiresAt = invitation.ExpiresAt,
                CreatedAt = invitation.CreatedAt,
                LastSentAt = updatedInvite?.LastSentAt ?? DateTime.UtcNow,
                ResendCount = updatedInvite?.ResendCount ?? (invitation.ResendCount + 1),
                InvitationToken = invitation.InvitationToken
            };

            return ApiResponse<SplitGroupInvitationDto>.SuccessResponse("Invitation resent successfully.", resultDto);
        }

        public async Task<ApiResponse<SplitGroupInvitationDto>> GetInvitationByTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Token is required.");

            var invite = await _groupRepository.GetInvitationByTokenAsync(token);
            if (invite == null)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Invitation not found or invalid token.");

            var group = await _groupRepository.GetGroupByIdAsync(invite.GroupId);

            var dto = new SplitGroupInvitationDto
            {
                Id = invite.Id,
                GroupId = invite.GroupId,
                GroupName = group?.Name ?? "Split Group",
                InvitedUserId = invite.InvitedUserId,
                InvitedEmail = invite.InvitedEmail,
                InvitedBy = invite.InvitedBy,
                Status = invite.Status,
                ExpiresAt = invite.ExpiresAt,
                CreatedAt = invite.CreatedAt,
                RespondedAt = invite.RespondedAt
            };

            return ApiResponse<SplitGroupInvitationDto>.SuccessResponse("Invitation details retrieved.", dto);
        }

        public async Task<int> LinkPendingInvitationsOnAuthAsync(Guid userId, string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return 0;

            try
            {
                var linked = await _groupRepository.LinkPendingInvitationsForUserAsync(userId, email.Trim());
                if (linked.Count == 0) return 0;

                foreach (var invite in linked)
                {
                    var group = await _groupRepository.GetGroupByIdAsync(invite.GroupId);
                    var notif = new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Type = "GroupInvitationReceived",
                        GroupId = invite.GroupId,
                        ReferenceId = invite.Id,
                        Title = "Group Invitation",
                        Message = $"You have a pending invitation to join '{group?.Name ?? "Split Group"}'.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationRepository.CreateNotificationAsync(notif);

                    if (_realtimeService != null)
                    {
                        await _realtimeService.NotifyUserAsync(userId, "ReceiveSplitNotification", notif);
                    }
                }

                _logger.LogInformation("Linked {Count} pending Split Group invitations for user {UserId} ({Email})", linked.Count, userId, email);
                return linked.Count;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error linking pending split invitations for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<ApiResponse<List<SplitGroupInvitationDto>>> GetPendingInvitationsAsync(
            Guid groupId, Guid currentUserId, bool includeHistory = false)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<List<SplitGroupInvitationDto>>.FailureResponse("Group not found.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<List<SplitGroupInvitationDto>>.ForbiddenResponse("Only the group owner can view pending invitations.");

            var invitations = await _groupRepository.GetPendingInvitationsByGroupAsync(groupId, includeHistory);
            return ApiResponse<List<SplitGroupInvitationDto>>.SuccessResponse("Pending invitations retrieved successfully.", invitations);
        }

        public async Task<ApiResponse<List<SplitGroupInvitationDto>>> GetMyInvitationsAsync(Guid currentUserId, string currentUserEmail)
        {
            var invitations = await _groupRepository.GetPendingInvitationsByUserAsync(currentUserId, currentUserEmail.Trim());
            return ApiResponse<List<SplitGroupInvitationDto>>.SuccessResponse("Your invitations retrieved successfully.", invitations);
        }

        public async Task<ApiResponse<bool>> RevokeInvitationAsync(Guid groupId, Guid invitationId, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<bool>.FailureResponse("Group not found.");

            if (!group.IsActive)
                return ApiResponse<bool>.FailureResponse("Cannot revoke invitations for an inactive group.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<bool>.ForbiddenResponse("Only the group owner can revoke invitations.");

            var invitation = await _groupRepository.GetInvitationByIdAsync(invitationId);
            if (invitation == null || invitation.GroupId != groupId)
                return ApiResponse<bool>.FailureResponse("Invitation not found for this group.");

            if (invitation.Status != "Pending")
                return ApiResponse<bool>.FailureResponse($"Cannot revoke invitation with status '{invitation.Status}'.");

            var success = await _groupRepository.RevokeInvitationAsync(invitationId, currentUserId, currentUserName, groupId);
            if (success)
            {
                // Neutralize notification so invitee cannot accept
                try
                {
                    await _notificationRepository.MarkInvitationNotificationsAsReadAsync(invitationId);
                    if (invitation.InvitedUserId.HasValue)
                    {
                        await _notificationRepository.MarkInvitationNotificationAsReadAsync(invitationId, invitation.InvitedUserId.Value);

                        if (_realtimeService != null)
                        {
                            await _realtimeService.NotifyUserAsync(invitation.InvitedUserId.Value, "ReceiveSplitNotification", new
                            {
                                Type = "InvitationRevoked",
                                GroupId = groupId,
                                ReferenceId = invitation.Id,
                                Message = $"{currentUserName} revoked the invitation to join '{group.Name}'."
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to neutralize notifications on revoke for invitation {InvitationId}", invitationId);
                }

                return ApiResponse<bool>.SuccessResponse("Invitation revoked successfully.", true);
            }

            return ApiResponse<bool>.FailureResponse("Failed to revoke invitation.");
        }

        public async Task<ApiResponse<bool>> AcceptInvitationAsync(
            Guid invitationId, Guid currentUserId, string currentUserEmail, string currentUserName)
        {
            var invitation = await _groupRepository.GetInvitationByIdAsync(invitationId);
            if (invitation == null)
                return ApiResponse<bool>.FailureResponse("Invitation not found.");

            if (invitation.Status != "Pending")
                return ApiResponse<bool>.FailureResponse($"This invitation has already been {invitation.Status.ToLowerInvariant()}.");

            if (invitation.ExpiresAt <= DateTime.UtcNow)
                return ApiResponse<bool>.FailureResponse("This invitation has expired.");

            // Recipient check
            var matchesUserId = invitation.InvitedUserId.HasValue && invitation.InvitedUserId.Value == currentUserId;
            var matchesEmail = !string.IsNullOrWhiteSpace(invitation.InvitedEmail)
                && string.Equals(invitation.InvitedEmail.Trim(), currentUserEmail.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!matchesUserId && !matchesEmail)
                return ApiResponse<bool>.ForbiddenResponse("You are not the designated recipient of this invitation.");

            var success = await _groupRepository.AcceptInvitationAsync(invitationId, currentUserId, currentUserName);
            if (!success)
                return ApiResponse<bool>.FailureResponse("Failed to accept invitation.");

            // Mark recipient's notification as read
            try
            {
                await _notificationRepository.MarkInvitationNotificationAsReadAsync(invitationId, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark invitation notification read for {UserId}", currentUserId);
            }

            // Create notification for group owner
            try
            {
                var group = await _groupRepository.GetGroupByIdAsync(invitation.GroupId);
                if (group != null && group.OwnerId != currentUserId)
                {
                    var ownerNotif = new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = group.OwnerId,
                        Type = "InvitationAccepted",
                        GroupId = group.Id,
                        ReferenceId = invitationId,
                        Title = "Invitation Accepted",
                        Message = $"{currentUserName} accepted your invitation to join '{group.Name}'.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationRepository.CreateNotificationAsync(ownerNotif);

                    if (_realtimeService != null)
                    {
                        await _realtimeService.NotifyUserAsync(group.OwnerId, "ReceiveSplitNotification", ownerNotif);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify owner of accepted invitation {InvitationId}", invitationId);
            }

            return ApiResponse<bool>.SuccessResponse("You have successfully joined the group.", true);
        }

        public async Task<ApiResponse<bool>> RejectInvitationAsync(
            Guid invitationId, Guid currentUserId, string currentUserEmail, string currentUserName)
        {
            var invitation = await _groupRepository.GetInvitationByIdAsync(invitationId);
            if (invitation == null)
                return ApiResponse<bool>.FailureResponse("Invitation not found.");

            if (invitation.Status != "Pending")
                return ApiResponse<bool>.FailureResponse($"This invitation has already been {invitation.Status.ToLowerInvariant()}.");

            var matchesUserId = invitation.InvitedUserId.HasValue && invitation.InvitedUserId.Value == currentUserId;
            var matchesEmail = !string.IsNullOrWhiteSpace(invitation.InvitedEmail)
                && string.Equals(invitation.InvitedEmail.Trim(), currentUserEmail.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!matchesUserId && !matchesEmail)
                return ApiResponse<bool>.ForbiddenResponse("You are not the designated recipient of this invitation.");

            var success = await _groupRepository.RejectInvitationAsync(invitationId, currentUserId, currentUserName);
            if (!success)
                return ApiResponse<bool>.FailureResponse("Failed to decline invitation.");

            // Mark recipient's notification as read
            try
            {
                await _notificationRepository.MarkInvitationNotificationAsReadAsync(invitationId, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark invitation notification read for {UserId}", currentUserId);
            }

            // Create notification for group owner
            try
            {
                var group = await _groupRepository.GetGroupByIdAsync(invitation.GroupId);
                if (group != null && group.OwnerId != currentUserId)
                {
                    var ownerNotif = new SplitNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = group.OwnerId,
                        Type = "InvitationRejected",
                        GroupId = group.Id,
                        ReferenceId = invitationId,
                        Title = "Invitation Declined",
                        Message = $"{currentUserName} declined your invitation to join '{group.Name}'.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationRepository.CreateNotificationAsync(ownerNotif);

                    if (_realtimeService != null)
                    {
                        await _realtimeService.NotifyUserAsync(group.OwnerId, "ReceiveSplitNotification", ownerNotif);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify owner of declined invitation {InvitationId}", invitationId);
            }

            return ApiResponse<bool>.SuccessResponse("Invitation declined.", true);
        }

        public async Task<ApiResponse<bool>> RemoveMemberAsync(
            Guid groupId, Guid userIdToRemove, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<bool>.FailureResponse("Group not found.");

            if (!group.IsActive)
                return ApiResponse<bool>.FailureResponse("Cannot remove members from an inactive group.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<bool>.ForbiddenResponse("Only the group owner can remove members.");

            if (userIdToRemove == group.OwnerId)
                return ApiResponse<bool>.FailureResponse("The group owner cannot be removed.");

            var membership = await _groupRepository.GetMembershipAsync(groupId, userIdToRemove);
            if (membership == null || !membership.IsActive)
                return ApiResponse<bool>.FailureResponse("Member not found in this group.");

            var success = await _groupRepository.RemoveMemberAsync(groupId, userIdToRemove, currentUserId, currentUserName);
            return success
                ? ApiResponse<bool>.SuccessResponse("Member removed successfully.", true)
                : ApiResponse<bool>.FailureResponse("Failed to remove member.");
        }

        public async Task<ApiResponse<List<SplitGroupActivityDto>>> GetGroupActivitiesAsync(Guid groupId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<List<SplitGroupActivityDto>>.ForbiddenResponse("You are not an active member of this group.");

            var activities = await _groupRepository.GetGroupActivitiesAsync(groupId, 50);
            activities = activities.Where(a => !a.ActivityType.StartsWith("Settlement", StringComparison.OrdinalIgnoreCase)).ToList();
            return ApiResponse<List<SplitGroupActivityDto>>.SuccessResponse("Activities retrieved successfully.", activities);
        }

        public async Task<ApiResponse<PagedGroupActivityResultDto>> GetPagedGroupActivitiesAsync(
            Guid groupId, SplitGroupActivityQueryParameters parameters, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<PagedGroupActivityResultDto>.ForbiddenResponse("You are not an active member of this group.");

            var result = await _groupRepository.GetPagedGroupActivitiesAsync(groupId, parameters);
            result.Items = result.Items.Where(a => !a.ActivityType.StartsWith("Settlement", StringComparison.OrdinalIgnoreCase)).ToList();
            return ApiResponse<PagedGroupActivityResultDto>.SuccessResponse("Paged activities retrieved successfully.", result);
        }

        public async Task<ApiResponse<SplitGroupActivityDto>> GetActivityByIdAsync(Guid groupId, Guid activityId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<SplitGroupActivityDto>.ForbiddenResponse("You are not an active member of this group.");

            var activity = await _groupRepository.GetActivityByIdAsync(groupId, activityId);
            if (activity == null || activity.ActivityType.StartsWith("Settlement", StringComparison.OrdinalIgnoreCase))
                return ApiResponse<SplitGroupActivityDto>.FailureResponse("Activity log entry not found.");

            return ApiResponse<SplitGroupActivityDto>.SuccessResponse("Activity retrieved successfully.", activity);
        }

        private async Task TrySendInvitationEmailAsync(
            string groupName, string inviterName, string toEmail, string token, DateTime expiresAt)
        {
            if (_emailService == null || string.IsNullOrWhiteSpace(toEmail))
                return;

            try
            {
                var baseUrl = _appLinkSettings?.InvitationBaseUrl?.TrimEnd('/') ?? "http://localhost:5173";
                var inviteLink = $"{baseUrl}/split-expenses?inviteToken={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(toEmail)}";

                var subject = $"Invitation to join '{groupName}' on Spendwise";
                var body = BuildSplitInvitationHtml(groupName, inviterName, inviteLink, expiresAt);

                var message = new EmailMessage
                {
                    ToEmail = toEmail,
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                var res = await _emailService.SendEmailAsync(message);
                if (!res.Success)
                {
                    _logger.LogWarning("Email delivery failed for {Email}: {Message}", toEmail, res.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Non-fatal error sending split invitation email to {Email}", toEmail);
            }
        }

        private static string BuildSplitInvitationHtml(
            string groupName, string inviterName, string inviteLink, DateTime expiresAt)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head><meta charset=\"UTF-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" /></head>");
            sb.AppendLine("<body style=\"margin:0;padding:24px;background-color:#f4f5f7;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;\">");
            sb.AppendLine("  <table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:540px;margin:0 auto;background:#ffffff;border-radius:12px;border:1px solid #e5e7eb;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.05);\">");
            sb.AppendLine("    <tr><td style=\"padding:28px 32px 20px;background:#0f172a;color:#ffffff;\">");
            sb.AppendLine("      <h2 style=\"margin:0;font-size:20px;font-weight:700;\">Spendwise Split Expenses</h2>");
            sb.AppendLine("    </td></tr>");
            sb.AppendLine("    <tr><td style=\"padding:32px;\">");
            sb.AppendLine($"      <p style=\"font-size:16px;color:#1e293b;margin:0 0 16px;\">Hi there,</p>");
            sb.AppendLine($"      <p style=\"font-size:15px;color:#334155;line-height:1.6;margin:0 0 24px;\">");
            sb.AppendLine($"        <strong>{System.Net.WebUtility.HtmlEncode(inviterName)}</strong> has invited you to join the expense sharing group <strong>'{System.Net.WebUtility.HtmlEncode(groupName)}'</strong>.");
            sb.AppendLine("      </p>");
            sb.AppendLine("      <div style=\"text-align:center;margin:32px 0;\">");
            sb.AppendLine($"        <a href=\"{inviteLink}\" style=\"display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;font-weight:600;font-size:14px;padding:12px 28px;border-radius:8px;\">");
            sb.AppendLine("          View & Accept Invitation");
            sb.AppendLine("        </a>");
            sb.AppendLine("      </div>");
            sb.AppendLine($"      <p style=\"font-size:12px;color:#64748b;line-height:1.5;margin:24px 0 0;\">");
            sb.AppendLine($"        This invitation link is valid until <strong>{expiresAt:MMM d, yyyy HH:mm} UTC</strong>.<br/>");
            sb.AppendLine("        If you don't have a Spendwise account yet, you will be guided to register using this email.");
            sb.AppendLine("      </p>");
            sb.AppendLine("    </td></tr>");
            sb.AppendLine("    <tr><td style=\"padding:16px 32px;background:#f8fafc;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;text-align:center;\">");
            sb.AppendLine("      Spendwise — Multi-workspace business cashbook & split expenses app");
            sb.AppendLine("    </td></tr>");
            sb.AppendLine("  </table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }
    }

    internal class NullSplitNotificationRepository : ISplitNotificationRepository
    {
        public Task<PagedSplitNotificationResultDto> GetNotificationsByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20) =>
            Task.FromResult(new PagedSplitNotificationResultDto());

        public Task<int> GetUnreadCountAsync(Guid userId) => Task.FromResult(0);

        public Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId) => Task.FromResult(true);

        public Task<int> MarkAllAsReadAsync(Guid userId) => Task.FromResult(0);

        public Task<SplitNotification> CreateNotificationAsync(SplitNotification notification) => Task.FromResult(notification);

        public Task<bool> MarkInvitationNotificationAsReadAsync(Guid invitationId, Guid userId) => Task.FromResult(true);

        public Task<int> MarkInvitationNotificationsAsReadAsync(Guid invitationId) => Task.FromResult(0);
    }
}
