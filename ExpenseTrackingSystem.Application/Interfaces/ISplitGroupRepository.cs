using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitGroupRepository
    {
        Task<SplitGroup?> GetGroupByIdAsync(Guid groupId);
        Task<SplitGroupMember?> GetMembershipAsync(Guid groupId, Guid userId);
        Task<List<SplitGroupSummaryDto>> GetUserGroupsAsync(Guid userId);
        Task<SplitGroupDetailDto?> GetGroupDetailsAsync(Guid groupId, Guid userId);
        Task<List<SplitGroupMemberDto>> GetGroupMembersAsync(Guid groupId);
        Task<SplitGroup> CreateGroupWithMemberAndActivityAsync(SplitGroup group, Guid ownerId, string ownerName);
        Task<SplitGroup> CreateGroupWithMemberAndInvitationsAsync(
            SplitGroup group,
            Guid ownerId,
            string ownerName,
            List<SplitGroupInvitation> invitations,
            List<SplitGroupActivity> activities);
        Task<List<SplitUserSearchResultDto>> SearchUsersAsync(string query, Guid currentUserId, int limit = 10);
        Task<bool> UpdateGroupAsync(SplitGroup group, Guid userId, string userName);
        Task<bool> SoftDeleteGroupAsync(Guid groupId, Guid userId, string userName);
        Task<bool> HasFinancialRecordsAsync(Guid groupId);
        Task<int> GetPendingSettlementsCountAsync(Guid groupId) => Task.FromResult(0);

        Task<User?> GetUserByEmailOrIdAsync(string? email, Guid? userId);
        Task<SplitGroupInvitation?> GetInvitationByIdAsync(Guid invitationId);
        Task<SplitGroupInvitation?> GetInvitationByTokenAsync(string token);
        Task<SplitGroupInvitation?> GetPendingInvitationByEmailAsync(Guid groupId, string email);
        Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId);
        Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId, bool includeHistory) => GetPendingInvitationsByGroupAsync(groupId);
        Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByUserAsync(Guid userId, string userEmail);
        Task<SplitGroupInvitation> CreateInvitationAsync(SplitGroupInvitation invitation, string inviterName, string groupName);
        Task<bool> UpdateInvitationResendAsync(Guid invitationId);
        Task<List<SplitGroupInvitation>> LinkPendingInvitationsForUserAsync(Guid userId, string email);
        Task<bool> RevokeInvitationAsync(Guid invitationId, Guid revokedByUserId, string revokerName, Guid groupId);
        Task<bool> AcceptInvitationAsync(Guid invitationId, Guid userId, string userName);
        Task<bool> RejectInvitationAsync(Guid invitationId, Guid userId, string userName);
        Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName);

        Task<List<SplitGroupActivityDto>> GetGroupActivitiesAsync(Guid groupId, int limit = 50);
        Task<PagedGroupActivityResultDto> GetPagedGroupActivitiesAsync(Guid groupId, SplitGroupActivityQueryParameters parameters);
        Task<SplitGroupActivityDto?> GetActivityByIdAsync(Guid groupId, Guid activityId);
        Task LogActivityAsync(Guid groupId, Guid userId, string activityType, string description, Guid? referenceId = null);
    }
}
