using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface IInvitationRepository
    {
        Task<ApiResponse<Guid>> CreateBusinessInvitationAsync(
            Guid businessId,
            string invitedEmail,
            Guid invitedByUserId,
            DateTime? expiresOn,
            string? remarks,
            List<string> permissionCodes);

        Task<ApiResponse<Guid>> CreateCashbookInvitationAsync(
            Guid businessId,
            Guid cashbookId,
            string invitedEmail,
            Guid invitedByUserId,
            DateTime? expiresOn,
            string? remarks,
            List<string> permissionCodes);

        Task<List<InvitationListItem>> GetMyPendingInvitationsAsync(Guid userId,string email);

        Task<ApiResponse<Guid>> AcceptInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail);

        Task<ApiResponse<Guid>> RejectInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail,string? remarks);

        Task<ApiResponse<Guid>> RevokeInvitationAsync( Guid invitationId,  Guid currentUserId);

        Task<BusinessEmailInfoDto?> GetBusinessEmailInfoAsync(Guid businessId);

        Task<CashbookEmailInfoDto?> GetCashbookEmailInfoAsync(Guid businessId, Guid cashbookId);

        Task<UserEmailInfoDto?> GetUserEmailInfoAsync(Guid userId);
    }
}
