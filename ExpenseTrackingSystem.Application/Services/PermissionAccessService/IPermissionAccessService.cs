using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.PermissionAccessService
{
    public interface IPermissionAccessService
    {
        Task<bool> IsBusinessOwnerAsync(Guid businessId, Guid userId);
        Task<bool> HasBusinessPermissionAsync(Guid businessId, Guid userId, string permissionCode);
        Task<bool> HasCashbookPermissionAsync(Guid cashbookId, Guid userId, string permissionCode);
        Task<bool> HasBusinessOrOwnerAccessAsync(Guid businessId, Guid userId, string permissionCode);
        Task<bool> HasCashbookOrOwnerAccessAsync(Guid cashbookId, Guid userId, string permissionCode);
        Task<Guid?> GetBusinessIdByCashbookIdAsync(Guid cashbookId);
    }
}
