using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.PermissionAccessService
{
    public class PermissionAccessService : IPermissionAccessService
    {
        private readonly IPermissionAccessRepository _permissionAccessRepository;

        public PermissionAccessService(IPermissionAccessRepository permissionAccessRepository)
        {
            _permissionAccessRepository = permissionAccessRepository;
        }

        public async Task<bool> IsBusinessOwnerAsync(Guid businessId, Guid userId)
        {
            if (businessId == Guid.Empty || userId == Guid.Empty)
                return false;

            return await _permissionAccessRepository.IsBusinessOwnerAsync(businessId, userId);
        }

        public async Task<bool> IsCashbookOwnerAsync(Guid cashbookId, Guid userId)
        {
            if (cashbookId == Guid.Empty || userId == Guid.Empty)
                return false;

            return await _permissionAccessRepository.IsCashbookOwnerAsync(cashbookId, userId);
        }

        public async Task<bool> HasBusinessPermissionAsync(Guid businessId, Guid userId, string permissionCode)
        {
            if (businessId == Guid.Empty || userId == Guid.Empty || string.IsNullOrWhiteSpace(permissionCode))
                return false;

            return await _permissionAccessRepository.HasBusinessPermissionAsync(
                businessId,
                userId,
                permissionCode.Trim());
        }

        public async Task<bool> HasCashbookPermissionAsync(Guid cashbookId, Guid userId, string permissionCode)
        {
            if (cashbookId == Guid.Empty || userId == Guid.Empty || string.IsNullOrWhiteSpace(permissionCode))
                return false;

            return await _permissionAccessRepository.HasCashbookPermissionAsync(
                cashbookId,
                userId,
                permissionCode.Trim());
        }

        public async Task<bool> HasAnyCashbookAccessInBusinessAsync(Guid businessId, Guid userId)
        {
            if (businessId == Guid.Empty || userId == Guid.Empty)
                return false;

            return await _permissionAccessRepository.HasAnyCashbookAccessInBusinessAsync(businessId, userId);
        }

        public async Task<bool> HasBusinessOrOwnerAccessAsync(Guid businessId, Guid userId, string permissionCode)
        {
            if (await IsBusinessOwnerAsync(businessId, userId))
                return true;

            if (await HasBusinessPermissionAsync(businessId, userId, permissionCode))
                return true;

            if (string.Equals(permissionCode, BusinessPermission.CASHBOOK_LIST_VIEW.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (await HasBusinessPermissionAsync(businessId, userId, BusinessPermission.BUSINESS_VIEW.ToString()))
                    return true;
            }

            return false;
        }

        public async Task<bool> HasCashbookOrOwnerAccessAsync(Guid cashbookId, Guid userId, string permissionCode)
        {
            if (cashbookId == Guid.Empty || userId == Guid.Empty || string.IsNullOrWhiteSpace(permissionCode))
                return false;

            // 1. Check if user is the cashbook owner (creator or business owner)
            if (await IsCashbookOwnerAsync(cashbookId, userId))
                return true;

            // 2. Check explicitly assigned member permission
            if (await HasCashbookPermissionAsync(cashbookId, userId, permissionCode))
                return true;

            // 3. Business-level members with view permissions can view cashbooks and their transactions
            if (string.Equals(permissionCode, CashbookPermission.CASHBOOK_VIEW.ToString(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permissionCode, CashbookPermission.TRANSACTION_VIEW.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var businessId = await GetBusinessIdByCashbookIdAsync(cashbookId);
                if (businessId.HasValue && businessId.Value != Guid.Empty)
                {
                    if (await HasBusinessPermissionAsync(businessId.Value, userId, BusinessPermission.BUSINESS_VIEW.ToString()) ||
                        await HasBusinessPermissionAsync(businessId.Value, userId, BusinessPermission.CASHBOOK_LIST_VIEW.ToString()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<Guid?> GetBusinessIdByCashbookIdAsync(Guid cashbookId)
        {
            if (cashbookId == Guid.Empty)
                return null;

            return await _permissionAccessRepository.GetBusinessIdByCashbookIdAsync(cashbookId);
        }
    }
}
