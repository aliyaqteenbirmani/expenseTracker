namespace SpendwiseSystem.Domain.Enums
{
    public enum InvitationType
    {
        BUSINESS = 1,
        CASHBOOK = 2
    }

    public enum InvitationStatus
    {
        Pending = 1,
        Accepted = 2,
        Rejected = 3,
        Revoked = 4,
        Expired = 5
    }

    public enum MembershipStatus
    {
        Active = 1,
        Revoked = 2
    }

    public enum BusinessPermission
    {
        BUSINESS_VIEW = 1,
        CASHBOOK_LIST_VIEW = 2,
        BUSINESS_UPDATE = 3,
        CASHBOOK_CREATE = 4,
        CASHBOOK_UPDATE = 5,
        CASHBOOK_DELETE = 6
    }

    public enum CashbookPermission
    {
        CASHBOOK_VIEW = 1,
        TRANSACTION_VIEW = 2,
        TRANSACTION_CREATE = 3,
        TRANSACTION_UPDATE = 4,
        TRANSACTION_DELETE = 5
    }
}