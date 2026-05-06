using SpendwiseSystem.Application.Interfaces;
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

        public async Task<bool> HasBusinessOrOwnerAccessAsync(Guid businessId, Guid userId, string permissionCode)
        {
            if (await IsBusinessOwnerAsync(businessId, userId))
                return true;

            return await HasBusinessPermissionAsync(businessId, userId, permissionCode);
        }

        public async Task<bool> HasCashbookOrOwnerAccessAsync(Guid cashbookId, Guid userId, string permissionCode)
        {
            var businessId = await GetBusinessIdByCashbookIdAsync(cashbookId);

            if (!businessId.HasValue || businessId.Value == Guid.Empty)
                return false;

            if (await IsBusinessOwnerAsync(businessId.Value, userId))
                return true;

            return await HasCashbookPermissionAsync(cashbookId, userId, permissionCode);
        }

        public async Task<Guid?> GetBusinessIdByCashbookIdAsync(Guid cashbookId)
        {
            if (cashbookId == Guid.Empty)
                return null;

            return await _permissionAccessRepository.GetBusinessIdByCashbookIdAsync(cashbookId);
        }
    }
}
