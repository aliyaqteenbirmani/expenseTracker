using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitGroupService
    {
        Task<ApiResponse<SplitGroupSummaryDto>> CreateGroupAsync(CreateSplitGroupDto dto, Guid userId, string userName);
        Task<ApiResponse<List<SplitGroupSummaryDto>>> GetMyGroupsAsync(Guid userId);
        Task<ApiResponse<SplitGroupDetailDto>> GetGroupDetailsAsync(Guid groupId, Guid userId);
        Task<ApiResponse<bool>> UpdateGroupAsync(Guid groupId, UpdateSplitGroupDto dto, Guid userId, string userName);
        Task<ApiResponse<bool>> DeleteGroupAsync(Guid groupId, Guid userId, string userName);

        Task<ApiResponse<SplitGroupInvitationDto>> InviteMemberAsync(Guid groupId, InviteSplitGroupMemberDto dto, Guid currentUserId, string currentUserName);
        Task<ApiResponse<List<SplitGroupInvitationDto>>> GetPendingInvitationsAsync(Guid groupId, Guid currentUserId);
        Task<ApiResponse<List<SplitGroupInvitationDto>>> GetMyInvitationsAsync(Guid currentUserId, string currentUserEmail);
        Task<ApiResponse<bool>> RevokeInvitationAsync(Guid groupId, Guid invitationId, Guid currentUserId, string currentUserName);
        Task<ApiResponse<bool>> AcceptInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, string currentUserName);
        Task<ApiResponse<bool>> RejectInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, string currentUserName);
        Task<ApiResponse<bool>> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid currentUserId, string currentUserName);

        Task<ApiResponse<List<SplitGroupActivityDto>>> GetGroupActivitiesAsync(Guid groupId, Guid currentUserId);
    }
}
