using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface IPermissionAccessRepository
    {
        Task<bool> IsBusinessOwnerAsync(Guid businessId, Guid userId);
        Task<bool> HasBusinessPermissionAsync(Guid businessId, Guid userId, string permissionCode);
        Task<bool> HasCashbookPermissionAsync(Guid cashbookId, Guid userId, string permissionCode);
        Task<Guid?> GetBusinessIdByCashbookIdAsync(Guid cashbookId);
    }
}
