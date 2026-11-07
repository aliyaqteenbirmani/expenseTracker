using Dapper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.DTOs.CashBookDtos;
using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using SpendwiseSystem.Domain.DTOs.MemberDto;
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
    public class MemberManagementRepository : IMemberManagementRepository
    {
        private readonly IDapperContext _dapper;

        public MemberManagementRepository(IDapperContext dapper)
        {
            _dapper = dapper;
        }

        public async Task<List<BusinessMemberDto>> GetBusinessMembersAsync(Guid businessId, Guid currentUserId)
        {
            var results = await _dapper.GetMultipleSelectsAsync(
                "SP_GetBusinessMembers",
                new
                {
                    BusinessId = businessId,
                    CurrentUserId = currentUserId
                },
                x => x.Read<BusinessMemberDto>().ToList(),
                x => x.Read<MemberPermissionRow>().ToList()
            );

            var members = (List<BusinessMemberDto>)results[0];
            var permissions = (List<MemberPermissionRow>)results[1];

            var lookup = permissions
                .GroupBy(x => x.MemberId)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            foreach (var member in members)
            {
                member.Permissions = lookup.TryGetValue(member.BusinessMemberId, out var list)
                    ? list
                    : new List<string>();
            }

            return members;
        }

        public async Task<List<CashBookMemberDto>> GetCashbookMembersAsync(Guid businessId, Guid cashbookId, Guid currentUserId)
        {
            var results = await _dapper.GetMultipleSelectsAsync(
                "SP_GetCashbookMembers",
                new
                {
                    BusinessId = businessId,
                    CashBookId = cashbookId,
                    CurrentUserId = currentUserId
                },
                x => x.Read<CashBookMemberDto>().ToList(),
                x => x.Read<MemberPermissionRow>().ToList()
            );

            var members = (List<CashBookMemberDto>)results[0];
            var permissions = (List<MemberPermissionRow>)results[1];

            var lookup = permissions
                .GroupBy(x => x.MemberId)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            foreach (var member in members)
            {
                member.Permissions = lookup.TryGetValue(member.CashbookMemberId, out var list)
                    ? list
                    : new List<string>();
            }

            return members;
        }

        public async Task<ApiResponse<Guid>> UpdateBusinessMemberPermissionsAsync(
            Guid businessId,
            Guid userId,
            Guid currentUserId,
            List<string> permissionCodes)
        {
            var parameters = new DynamicParameters(new
            {
                BusinessId = businessId,
                UserId = userId,
                CurrentUserId = currentUserId
            });

            parameters.Add(
                "@PermissionCodes",
                SqlTableTypeHelper.CreateStringListTable(permissionCodes)
                    .AsTableValuedParameter("dbo.StringList"));

            var result = await _dapper.GetSingleAsync<SpResult<Guid>>(
                "SP_UpdateBusinessMemberPermissions",
                parameters);

            return result.Success
                ? ApiResponse<Guid>.SuccessResponse(result.Message, result.Data)
                : ApiResponse<Guid>.FailureResponse(result.Message);
        }

        public async Task<ApiResponse<Guid>> UpdateCashbookMemberPermissionsAsync(
            Guid businessId,
            Guid cashbookId,
            Guid userId,
            Guid currentUserId,
            List<string> permissionCodes)
        {
            var parameters = new DynamicParameters(new
            {
                BusinessId = businessId,
                CashBookId = cashbookId,
                UserId = userId,
                CurrentUserId = currentUserId
            });

            parameters.Add(
                "@PermissionCodes",
                SqlTableTypeHelper.CreateStringListTable(permissionCodes)
                    .AsTableValuedParameter("dbo.StringList"));

            var result = await _dapper.GetSingleAsync<SpResult<Guid>>(
                "SP_UpdateCashbookMemberPermissions",
                parameters);

            return result.Success
                ? ApiResponse<Guid>.SuccessResponse(result.Message, result.Data)
                : ApiResponse<Guid>.FailureResponse(result.Message);
        }

        public async Task<ApiResponse<Guid>> RemoveBusinessMemberAsync(Guid businessId, Guid userId, Guid currentUserId)
        {
            var result = await _dapper.GetSingleAsync<SpResult<Guid>>(
                "SP_RemoveBusinessMember",
                new
                {
                    BusinessId = businessId,
                    UserId = userId,
                    CurrentUserId = currentUserId
                });

            return result.Success
                ? ApiResponse<Guid>.SuccessResponse(result.Message, result.Data)
                : ApiResponse<Guid>.FailureResponse(result.Message);
        }

        public async Task<ApiResponse<Guid>> RemoveCashbookMemberAsync(Guid businessId, Guid cashbookId, Guid userId, Guid currentUserId)
        {
            var result = await _dapper.GetSingleAsync<SpResult<Guid>>(
                "SP_RemoveCashbookMember",
                new
                {
                    BusinessId = businessId,
                    CashBookId = cashbookId,
                    UserId = userId,
                    CurrentUserId = currentUserId
                });

            return result.Success
                ? ApiResponse<Guid>.SuccessResponse(result.Message, result.Data)
                : ApiResponse<Guid>.FailureResponse(result.Message);
        }

        public async Task<UserWorkspaceAccessDto> GetUserWorkspaceAccessAsync(Guid businessId, Guid? cashbookId, Guid currentUserId)
        {
            var result = new UserWorkspaceAccessDto
            {
                IsOwner = false,
                BusinessPermissions = new List<string>(),
                CashbookPermissions = new List<string>()
            };

            const string bizSql = @"
                SELECT DISTINCT p.Code
                FROM dbo.BusinessMembers bm
                JOIN dbo.BusinessMemberPermissions bmp ON bmp.BusinessMemberId = bm.Id AND bmp.Granted = 1 AND bmp.IsDeleted = 0
                JOIN dbo.Permissions p ON p.Id = bmp.PermissionId AND p.IsDeleted = 0
                WHERE bm.BusinessId = @BusinessId AND bm.UserId = @UserId AND bm.Status = 'Active' AND bm.IsDeleted = 0";

            var bizPerms = await _dapper.GetListAsyncTable<string>(bizSql, new { BusinessId = businessId, UserId = currentUserId });
            result.BusinessPermissions = bizPerms?.ToList() ?? new List<string>();

            if (cashbookId.HasValue && cashbookId.Value != Guid.Empty)
            {
                const string cbSql = @"
                    SELECT DISTINCT p.Code
                    FROM dbo.CashbookMembers cm
                    JOIN dbo.CashbookMemberPermissions cmp ON cmp.CashbookMemberId = cm.Id AND cmp.Granted = 1 AND cmp.IsDeleted = 0
                    JOIN dbo.Permissions p ON p.Id = cmp.PermissionId AND p.IsDeleted = 0
                    WHERE cm.CashBookId = @CashBookId AND cm.UserId = @UserId AND cm.Status = 'Active' AND cm.IsDeleted = 0";

                var cbPerms = await _dapper.GetListAsyncTable<string>(cbSql, new { CashBookId = cashbookId.Value, UserId = currentUserId });
                var cbList = cbPerms?.ToList() ?? new List<string>();
                if (cbList.Any())
                {
                    if (!cbList.Contains("CASHBOOK_VIEW", StringComparer.OrdinalIgnoreCase))
                        cbList.Add("CASHBOOK_VIEW");
                    if (!cbList.Contains("TRANSACTION_VIEW", StringComparer.OrdinalIgnoreCase))
                        cbList.Add("TRANSACTION_VIEW");
                }
                result.CashbookPermissions = cbList;
            }

            return result;
        }
    }
}
