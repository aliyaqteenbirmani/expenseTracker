using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.InvitationService
{
    public interface IInvitationService
    {
        Task<ApiResponse<Guid>> CreateBusinessInvitationAsync(
                    Guid businessId,
                    CreateBusinessInvitationRequest request,
                    Guid currentUserId);

        Task<ApiResponse<Guid>> CreateCashbookInvitationAsync(
            Guid businessId,
            Guid cashbookId,
            CreateCashbookInvitationRequest request,
            Guid currentUserId);

        Task<ApiResponse<List<InvitationListItem>>> GetMyPendingInvitationsAsync(
            Guid currentUserId,
            string currentUserEmail);

        Task<ApiResponse<List<InvitationListItem>>> GetSentInvitationsAsync(
            Guid userId,
            string userEmail);

        Task<ApiResponse<Guid>> AcceptInvitationAsync(
            Guid invitationId,
            Guid currentUserId,
            string currentUserEmail);

        Task<ApiResponse<Guid>> RejectInvitationAsync(
            Guid invitationId,
            RejectInvitationRequest request,
            Guid currentUserId,
            string currentUserEmail);

        Task<ApiResponse<Guid>> RevokeInvitationAsync(
            Guid invitationId,
            Guid currentUserId);

        Task<ApiResponse<List<InvitationByTokenDto>>> GetInvitationByTokenAsync(string token);
    }
}
