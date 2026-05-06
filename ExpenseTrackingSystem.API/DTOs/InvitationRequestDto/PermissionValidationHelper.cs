using SpendwiseSystem.Domain.Enums;

namespace SpendwiseSystem.Domain.DTOs.InvitationRequestDto
{
    public static class PermissionValidationHelper
    {
        public static bool AreValidBusinessPermissions(IEnumerable<BusinessPermission> permissions)
        {
            var list = permissions?.ToList() ?? new List<BusinessPermission>();

            return list.Any() &&
                   list.All(x => Enum.IsDefined(typeof(BusinessPermission), x));
        }

        public static bool AreValidCashbookPermissions(IEnumerable<CashbookPermission> permissions)
        {
            var list = permissions?.ToList() ?? new List<CashbookPermission>();

            return list.Any() &&
                   list.All(x => Enum.IsDefined(typeof(CashbookPermission), x));
        }
    }
}