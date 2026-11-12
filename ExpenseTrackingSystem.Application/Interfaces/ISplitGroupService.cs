using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitGroupService
    {
        Task<ApiResponse<CreateSplitGroupResultDto>> CreateGroupAsync(CreateSplitGroupDto dto, Guid userId, string userName);
        Task<ApiResponse<List<SplitUserSearchResultDto>>> SearchUsersAsync(string query, Guid currentUserId);
        Task<ApiResponse<List<SplitGroupSummaryDto>>> GetMyGroupsAsync(Guid userId);
        Task<ApiResponse<SplitGroupDetailDto>> GetGroupDetailsAsync(Guid groupId, Guid userId);
        Task<ApiResponse<List<SplitGroupMemberDto>>> GetGroupMembersAsync(Guid groupId, Guid userId);
        Task<ApiResponse<bool>> UpdateGroupAsync(Guid groupId, UpdateSplitGroupDto dto, Guid userId, string userName);
        Task<ApiResponse<bool>> DeleteGroupAsync(Guid groupId, Guid userId, string userName);

        Task<ApiResponse<SplitGroupInvitationDto>> InviteMemberAsync(Guid groupId, InviteSplitGroupMemberDto dto, Guid currentUserId, string currentUserName);
        Task<ApiResponse<BatchInviteResultDto>> BatchInviteMembersAsync(Guid groupId, BatchInviteMembersDto dto, Guid currentUserId, string currentUserName);
        Task<ApiResponse<SplitGroupInvitationDto>> ResendInvitationAsync(Guid groupId, Guid invitationId, Guid currentUserId, string currentUserName);
        Task<ApiResponse<SplitGroupInvitationDto>> GetInvitationByTokenAsync(string token);
        Task<int> LinkPendingInvitationsOnAuthAsync(Guid userId, string email);
        Task<ApiResponse<List<SplitGroupInvitationDto>>> GetPendingInvitationsAsync(Guid groupId, Guid currentUserId, bool includeHistory = false);
        Task<ApiResponse<List<SplitGroupInvitationDto>>> GetMyInvitationsAsync(Guid currentUserId, string currentUserEmail);
        Task<ApiResponse<bool>> RevokeInvitationAsync(Guid groupId, Guid invitationId, Guid currentUserId, string currentUserName);
        Task<ApiResponse<bool>> AcceptInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, string currentUserName);
        Task<ApiResponse<bool>> RejectInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, string currentUserName);
        Task<ApiResponse<bool>> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid currentUserId, string currentUserName);

        Task<ApiResponse<List<SplitGroupActivityDto>>> GetGroupActivitiesAsync(Guid groupId, Guid currentUserId);
        Task<ApiResponse<PagedGroupActivityResultDto>> GetPagedGroupActivitiesAsync(Guid groupId, SplitGroupActivityQueryParameters parameters, Guid currentUserId);
        Task<ApiResponse<SplitGroupActivityDto>> GetActivityByIdAsync(Guid groupId, Guid activityId, Guid currentUserId);
    }
}
