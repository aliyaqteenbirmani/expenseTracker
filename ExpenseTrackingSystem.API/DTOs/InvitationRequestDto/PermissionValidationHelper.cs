using SpendwiseSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.InvitationRequestDto
{
    public static class PermissionValidationHelper
    {
        private static readonly HashSet<string> BusinessPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            AppPermissions.BusinessView,
            AppPermissions.CashbookListView,
            AppPermissions.CashbookCreate,
            AppPermissions.CashbookUpdate,
            AppPermissions.CashbookDelete
        };

        private static readonly HashSet<string> CashbookPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            AppPermissions.CashbookView,
            AppPermissions.TransactionView,
            AppPermissions.TransactionAdd,
            AppPermissions.TransactionEdit,
            AppPermissions.TransactionDelete
        };

        public static bool AreValidBusinessPermissions(IEnumerable<string> permissions)
        {
            var list = permissions?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
            return list.Any() && list.All(x => BusinessPermissions.Contains(x.Trim()));
        }

        public static bool AreValidCashbookPermissions(IEnumerable<string> permissions)
        {
            var list = permissions?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
            return list.Any() && list.All(x => CashbookPermissions.Contains(x.Trim()));
        }

    }

    public static class AppPermissions
    {
        public const string BusinessView = "BUSINESS_VIEW";
        public const string CashbookListView = "CASHBOOK_LIST_VIEW";
        public const string CashbookCreate = "CASHBOOK_CREATE";
        public const string CashbookUpdate = "CASHBOOK_UPDATE";
        public const string CashbookDelete = "CASHBOOK_DELETE";

        public const string CashbookView = "CASHBOOK_VIEW";
        public const string TransactionView = "TRANSACTION_VIEW";
        public const string TransactionAdd = "TRANSACTION_ADD";
        public const string TransactionEdit = "TRANSACTION_EDIT";
        public const string TransactionDelete = "TRANSACTION_DELETE";
    }
}
