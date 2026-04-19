using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Enums
{
    public static class PermissionScopes
    {
        public const string Business = "BUSINESS";
        public const string Cashbook = "CASHBOOK";
    }

    public static class InvitationTypes
    {
        public const string Business = "BUSINESS";
        public const string Cashbook = "CASHBOOK";
    }

    public static class InvitationStatuses
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Rejected = "Rejected";
        public const string Revoked = "Revoked";
        public const string Expired = "Expired";
    }

    public static class MembershipStatuses
    {
        public const string Active = "Active";
        public const string Revoked = "Revoked";
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
