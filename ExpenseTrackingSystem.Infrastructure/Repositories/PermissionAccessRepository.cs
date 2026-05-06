using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.PermissionDto;
using SpendwiseSystem.Infrastructure.Data.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class PermissionAccessRepository : IPermissionAccessRepository
    {
        private readonly IDapperContext _dapper;

        public PermissionAccessRepository(IDapperContext dapper)
        {
            _dapper = dapper;
        }

        public async Task<bool> IsBusinessOwnerAsync(Guid businessId, Guid userId)
        {
            var result = await _dapper.GetSingleAsync<PermissionCheckResult>(
                "SP_IsBusinessOwner",
                new
                {
                    BusinessId = businessId,
                    UserId = userId
                });

            return result?.IsAllowed ?? false;
        }

        public async Task<bool> HasBusinessPermissionAsync(Guid businessId, Guid userId, string permissionCode)
        {
            var result = await _dapper.GetSingleAsync<PermissionCheckResult>(
                "SP_HasBusinessPermission",
                new
                {
                    BusinessId = businessId,
                    UserId = userId,
                    PermissionCode = permissionCode
                });

            return result?.IsAllowed ?? false;
        }

        public async Task<bool> HasCashbookPermissionAsync(Guid cashbookId, Guid userId, string permissionCode)
        {
            var result = await _dapper.GetSingleAsync<PermissionCheckResult>(
                "SP_HasCashbookPermission",
                new
                {
                    CashBookId = cashbookId,
                    UserId = userId,
                    PermissionCode = permissionCode
                });

            return result?.IsAllowed ?? false;
        }

        public async Task<Guid?> GetBusinessIdByCashbookIdAsync(Guid cashbookId)
        {
            return await _dapper.GetSingleAsync<Guid?>(
                "SP_GetBusinessIdByCashbookId",
                new
                {
                    CashBookId = cashbookId
                });
        }
    }
}
