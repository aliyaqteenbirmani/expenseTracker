using Dapper;
using Microsoft.EntityFrameworkCore;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Infrastructure.Data;
using SpendwiseSystem.Infrastructure.Data.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class InvitationRepository : IInvitationRepository
    {
        private readonly IDapperContext _dapper;

        public InvitationRepository(IDapperContext dapper)
        {
            _dapper = dapper;
        }

        public async Task<ApiResponse<Guid>> AcceptInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail)
        {
            var result = await _dapper.GetSingleAsync<SpResult<Guid>>(
                "SP_AcceptInvitation",
                new { InvitationId = invitationId, CurrentUserId = currentUserId, CurrentUserEmail = currentUserEmail },
                commandType: System.Data.CommandType.StoredProcedure
            );

            return result.Success
                ? new ApiResponse<Guid> { Success = true, Data = result.Data, Message = result.Message }
                : new ApiResponse<Guid> { Success = false, Data = Guid.Empty, Message = result.Message };
        }

        public async Task<ApiResponse<Guid>> CreateBusinessInvitationAsync(Guid businessId, string invitedEmail, Guid invitedByUserId, DateTime? expiresOn, DateTime? tokenExpiresOn, string inviteToken, string remarks, List<string> permissionCodes)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BusinessId", businessId);
            parameters.Add("@InvitedEmail", invitedEmail);
            parameters.Add("@InvitedByUserId", invitedByUserId);
            parameters.Add("@ExpiresOn", expiresOn);
            parameters.Add("@TokenExpiresOn", tokenExpiresOn);
            parameters.Add("@InviteToken", inviteToken);
            parameters.Add("@Remarks", remarks);
            parameters.Add(
                        "@PermissionCodes",
                        SqlTableTypeHelper.CreateStringListTable(permissionCodes)
                        .AsTableValuedParameter("dbo.StringList")
                    );

            var result = await _dapper.GetSingleAsync<SpResult<Guid>>("sp_CreateBusinessInvitation", parameters, commandType: System.Data.CommandType.StoredProcedure);

            return result.Success
                ? new ApiResponse<Guid> { Success = true, Data = result.Data, Message = result.Message }
                : new ApiResponse<Guid> { Success = false, Data = Guid.Empty, Message = result.Message };
        }

        public async Task<ApiResponse<Guid>> CreateCashbookInvitationAsync(Guid businessId, Guid cashbookId, string invitedEmail, Guid invitedByUserId, DateTime? expiresOn, DateTime? tokenExpiresOn, string inviteToken, string remarks, List<string> permissionCodes )
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BusinessId", businessId);
            parameters.Add("@CashBookId", cashbookId);
            parameters.Add("@InvitedEmail", invitedEmail);
            parameters.Add("@InvitedByUserId", invitedByUserId);
            parameters.Add("@ExpiresOn", expiresOn);
            parameters.Add("@TokenExpiresOn", tokenExpiresOn);
            parameters.Add("@InviteToken", inviteToken);
            parameters.Add("@Remarks", remarks);

            parameters.Add(
                "@PermissionCodes",
                SqlTableTypeHelper.CreateStringListTable(permissionCodes)
                .AsTableValuedParameter("dbo.StringList")
            );

            var result = await _dapper.GetSingleAsync<SpResult<Guid>>(
                "SP_CreateCashbookInvitation",
                parameters
            );

            return result.Success
                ? new ApiResponse<Guid> { Success = true, Data = result.Data, Message = result.Message }
                : new ApiResponse<Guid> { Success = false, Data = Guid.Empty, Message = result.Message };
        }

        public async Task<List<InvitationListItem>> GetMyPendingInvitationsAsync(Guid userId, string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            parameters.Add("@Email", email);

            var results = await _dapper.GetMultipleSelectsAsync(
                "SP_GetMyPendingInvitations",
                parameters,
                x => x.Read<InvitationListItem>().ToList(),
                x => x.Read<InvitationPermissionRow>().ToList()
            );

            var invitations = (List<InvitationListItem>)results[0];
            var permissions = (List<InvitationPermissionRow>)results[1];

            var lookup = permissions
                .GroupBy(x => x.InvitationId)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Code).ToList());

            foreach (var item in invitations)
            {
                item.Permissions = lookup.TryGetValue(item.Id, out var list)
                    ? list
                    : new List<string>();
            }

            return invitations;
        }

        public async Task<List<InvitationListItem>> GetSentInvitationsAsync(Guid userId, string status)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            parameters.Add("@Status", status);
            var results = await _dapper.GetMultipleSelectsAsync(
                "SP_GetMySentInvitations",
                parameters,
                x => x.Read<InvitationListItem>().ToList(),
                x => x.Read<InvitationPermissionRow>().ToList()
            );
            var invitations = (List<InvitationListItem>)results[0];
            var permissions = (List<InvitationPermissionRow>)results[1];
            var lookup = permissions
                .GroupBy(x => x.InvitationId)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Code).ToList());
            foreach (var item in invitations)
            {
                item.Permissions = lookup.TryGetValue(item.Id, out var list)
                    ? list
                    : new List<string>();
            }
            return invitations;
        }
        public async Task<ApiResponse<Guid>> RejectInvitationAsync(Guid invitationId, Guid currentUserId, string currentUserEmail, string remarks)
        {
            var result = await _dapper.GetSingleAsync<SpResult<Guid>>(
                "SP_RejectInvitation",
                new { InvitationId = invitationId, CurrentUserId = currentUserId, CurrentUserEmail = currentUserEmail, Remarks = remarks },
                commandType: System.Data.CommandType.StoredProcedure
            );

            return result.Success
                ? new ApiResponse<Guid> { Success = true, Data = result.Data, Message = result.Message }
                : new ApiResponse<Guid> { Success = false, Data = Guid.Empty, Message = result.Message };
        }

        public async Task<ApiResponse<Guid>> RevokeInvitationAsync(Guid invitationId, Guid currentUserId)
        {
            var result = await _dapper.GetSingleAsync<SpResult<Guid>>(
                "SP_RevokeInvitation",
                new { InvitationId = invitationId, CurrentUserId = currentUserId },
                commandType: System.Data.CommandType.StoredProcedure
            );

            return result.Success
                ? new ApiResponse<Guid> { Success = true, Data = result.Data, Message = result.Message }
                : new ApiResponse<Guid> { Success = false, Data = Guid.Empty, Message = result.Message };
        }



        public async Task<BusinessEmailInfoDto?> GetBusinessEmailInfoAsync(Guid businessId)
        {
            return await _dapper.GetSingleAsync<BusinessEmailInfoDto>(
                "SP_GetBusinessEmailInfo",
                new { BusinessId = businessId });
        }

        public async Task<CashbookEmailInfoDto?> GetCashbookEmailInfoAsync(Guid businessId, Guid cashbookId)
        {
            return await _dapper.GetSingleAsync<CashbookEmailInfoDto>(
                "SP_GetCashbookEmailInfo",
                new
                {
                    BusinessId = businessId,
                    CashBookId = cashbookId
                });
        }

        public async Task<UserEmailInfoDto?> GetUserEmailInfoAsync(Guid userId)
        {
            return await _dapper.GetSingleAsync<UserEmailInfoDto>(
                "SP_GetUserEmailInfo",
                new { UserId = userId });
        }

        public async Task<InvitationByTokenDto?> GetInvitationByTokenAsync(string inviteToken)
        {
            var results = await _dapper.GetMultipleSelectsAsync(
                "SP_GetInvitationByToken",
                new { InviteToken = inviteToken },
                x => x.Read<InvitationByTokenDto>().FirstOrDefault(),
                x => x.Read<InvitationPermissionRow>().ToList()
            );

            var invitation = (InvitationByTokenDto?)results[0];
            var permissions = (List<InvitationPermissionRow>)results[1];

            if (invitation == null)
                return null;

            invitation.Permissions = permissions
                .Select(x => x.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return invitation;
        }
    }
}
