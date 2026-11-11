using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class SplitGroupService : ISplitGroupService
    {
        private readonly ISplitGroupRepository _groupRepository;

        public SplitGroupService(ISplitGroupRepository groupRepository)
        {
            _groupRepository = groupRepository;
        }

        public async Task<ApiResponse<SplitGroupSummaryDto>> CreateGroupAsync(CreateSplitGroupDto dto, Guid userId, string userName)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponse<SplitGroupSummaryDto>.FailureResponse("Group name is required.");

            var group = new SplitGroup
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Icon = dto.Icon?.Trim(),
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };

            await _groupRepository.CreateGroupWithMemberAndActivityAsync(group, userId, userName);

            var summary = new SplitGroupSummaryDto
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
                CreatedAt = group.CreatedAt
            };

            return ApiResponse<SplitGroupSummaryDto>.SuccessResponse("Split group created successfully.", summary);
        }

        public async Task<ApiResponse<List<SplitGroupSummaryDto>>> GetMyGroupsAsync(Guid userId)
        {
            var groups = await _groupRepository.GetUserGroupsAsync(userId);
            return ApiResponse<List<SplitGroupSummaryDto>>.SuccessResponse("Groups retrieved successfully.", groups);
        }

        public async Task<ApiResponse<SplitGroupDetailDto>> GetGroupDetailsAsync(Guid groupId, Guid userId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, userId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<SplitGroupDetailDto>.ForbiddenResponse("You are not an active member of this group.");

            var details = await _groupRepository.GetGroupDetailsAsync(groupId, userId);
            if (details == null)
                return ApiResponse<SplitGroupDetailDto>.FailureResponse("Group not found.");

            return ApiResponse<SplitGroupDetailDto>.SuccessResponse("Group details retrieved successfully.", details);
        }

        public async Task<ApiResponse<bool>> UpdateGroupAsync(Guid groupId, UpdateSplitGroupDto dto, Guid userId, string userName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<bool>.FailureResponse("Group not found.");

            if (group.OwnerId != userId)
                return ApiResponse<bool>.ForbiddenResponse("Only the group owner can update group details.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponse<bool>.FailureResponse("Group name cannot be empty.");

            group.Name = dto.Name.Trim();
            group.Description = dto.Description?.Trim();
            group.Icon = dto.Icon?.Trim();

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
                return ApiResponse<bool>.ForbiddenResponse("Only the group owner can delete or archive this group.");

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

            if (group.OwnerId != currentUserId)
                return ApiResponse<SplitGroupInvitationDto>.ForbiddenResponse("Only the group owner can invite members.");

            if (string.IsNullOrWhiteSpace(dto.InvitedEmail) && !dto.InvitedUserId.HasValue)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("Please provide an email address or user ID to invite.");

            var targetUser = await _groupRepository.GetUserByEmailOrIdAsync(dto.InvitedEmail?.Trim(), dto.InvitedUserId);
            if (targetUser == null)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("The specified user does not exist in the system.");

            var effectiveEmail = targetUser.Email ?? dto.InvitedEmail?.Trim() ?? string.Empty;

            // Check if already an active member
            var existingMembership = await _groupRepository.GetMembershipAsync(groupId, targetUser.Id);
            if (existingMembership != null && existingMembership.IsActive)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("User is already an active member of this group.");

            // Check if active pending invitation exists
            var existingPendingInvite = await _groupRepository.GetPendingInvitationByEmailAsync(groupId, effectiveEmail);
            if (existingPendingInvite != null && existingPendingInvite.ExpiresAt > DateTime.UtcNow)
                return ApiResponse<SplitGroupInvitationDto>.FailureResponse("An active pending invitation has already been sent to this user.");

            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToHexString(tokenBytes);

            var invitation = new SplitGroupInvitation
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                InvitedUserId = targetUser.Id,
                InvitedEmail = effectiveEmail,
                InvitedBy = currentUserId,
                Status = "Pending",
                InvitationToken = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _groupRepository.CreateInvitationAsync(invitation, currentUserName, group.Name);

            var resultDto = new SplitGroupInvitationDto
            {
                Id = invitation.Id,
                GroupId = groupId,
                GroupName = group.Name,
                InvitedUserId = targetUser.Id,
                InvitedEmail = effectiveEmail,
                InvitedBy = currentUserId,
                InvitedByName = currentUserName,
                Status = invitation.Status,
                ExpiresAt = invitation.ExpiresAt,
                CreatedAt = invitation.CreatedAt
            };

            return ApiResponse<SplitGroupInvitationDto>.SuccessResponse("Invitation sent successfully.", resultDto);
        }

        public async Task<ApiResponse<List<SplitGroupInvitationDto>>> GetPendingInvitationsAsync(Guid groupId, Guid currentUserId)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<List<SplitGroupInvitationDto>>.FailureResponse("Group not found.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<List<SplitGroupInvitationDto>>.ForbiddenResponse("Only the group owner can view pending invitations.");

            var invitations = await _groupRepository.GetPendingInvitationsByGroupAsync(groupId);
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

            if (group.OwnerId != currentUserId)
                return ApiResponse<bool>.ForbiddenResponse("Only the group owner can revoke invitations.");

            var invitation = await _groupRepository.GetInvitationByIdAsync(invitationId);
            if (invitation == null || invitation.GroupId != groupId)
                return ApiResponse<bool>.FailureResponse("Invitation not found for this group.");

            if (invitation.Status != "Pending")
                return ApiResponse<bool>.FailureResponse($"Cannot revoke invitation with status '{invitation.Status}'.");

            var success = await _groupRepository.RevokeInvitationAsync(invitationId, currentUserId, currentUserName, groupId);
            return success
                ? ApiResponse<bool>.SuccessResponse("Invitation revoked successfully.", true)
                : ApiResponse<bool>.FailureResponse("Failed to revoke invitation.");
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
            return success
                ? ApiResponse<bool>.SuccessResponse("You have successfully joined the group.", true)
                : ApiResponse<bool>.FailureResponse("Failed to accept invitation.");
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
            return success
                ? ApiResponse<bool>.SuccessResponse("Invitation declined.", true)
                : ApiResponse<bool>.FailureResponse("Failed to decline invitation.");
        }

        public async Task<ApiResponse<bool>> RemoveMemberAsync(
            Guid groupId, Guid userIdToRemove, Guid currentUserId, string currentUserName)
        {
            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null)
                return ApiResponse<bool>.FailureResponse("Group not found.");

            if (group.OwnerId != currentUserId)
                return ApiResponse<bool>.ForbiddenResponse("Only the group owner can remove members.");

            if (userIdToRemove == group.OwnerId)
                return ApiResponse<bool>.FailureResponse("The group owner cannot be removed from the group.");

            var targetMembership = await _groupRepository.GetMembershipAsync(groupId, userIdToRemove);
            if (targetMembership == null || !targetMembership.IsActive)
                return ApiResponse<bool>.FailureResponse("User is not an active member of this group.");

            var success = await _groupRepository.RemoveMemberAsync(groupId, userIdToRemove, currentUserId, currentUserName);
            return success
                ? ApiResponse<bool>.SuccessResponse("Member removed from group successfully.", true)
                : ApiResponse<bool>.FailureResponse("Failed to remove member.");
        }

        public async Task<ApiResponse<List<SplitGroupActivityDto>>> GetGroupActivitiesAsync(Guid groupId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<List<SplitGroupActivityDto>>.ForbiddenResponse("You must be an active member of this group to view its activity history.");

            var activities = await _groupRepository.GetGroupActivitiesAsync(groupId);
            return ApiResponse<List<SplitGroupActivityDto>>.SuccessResponse("Activities retrieved successfully.", activities);
        }

        public async Task<ApiResponse<PagedGroupActivityResultDto>> GetPagedGroupActivitiesAsync(
            Guid groupId, SplitGroupActivityQueryParameters parameters, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<PagedGroupActivityResultDto>.ForbiddenResponse("You must be an active member of this group to view its activity history.");

            var paged = await _groupRepository.GetPagedGroupActivitiesAsync(groupId, parameters);
            return ApiResponse<PagedGroupActivityResultDto>.SuccessResponse("Activities retrieved successfully.", paged);
        }

        public async Task<ApiResponse<SplitGroupActivityDto>> GetActivityByIdAsync(
            Guid groupId, Guid activityId, Guid currentUserId)
        {
            var membership = await _groupRepository.GetMembershipAsync(groupId, currentUserId);
            if (membership == null || !membership.IsActive)
                return ApiResponse<SplitGroupActivityDto>.ForbiddenResponse("You must be an active member of this group to view activity details.");

            var activity = await _groupRepository.GetActivityByIdAsync(groupId, activityId);
            if (activity == null)
                return ApiResponse<SplitGroupActivityDto>.FailureResponse("Activity not found.");

            return ApiResponse<SplitGroupActivityDto>.SuccessResponse("Activity retrieved successfully.", activity);
        }
    }
}
