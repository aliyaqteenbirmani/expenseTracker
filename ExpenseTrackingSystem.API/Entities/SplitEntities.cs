using System;

namespace SpendwiseSystem.Domain.Entities
{
    public class SplitGroup
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }

    public class SplitGroupMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "Member"; // "Owner" or "Member"
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class SplitGroupInvitation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; set; }
        public Guid? InvitedUserId { get; set; }
        public string InvitedEmail { get; set; } = string.Empty;
        public Guid InvitedBy { get; set; }
        public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Rejected", "Expired", "Revoked"
        public string? InvitationToken { get; set; }
        public string? TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public DateTime? LastSentAt { get; set; }
        public int ResendCount { get; set; } = 0;
    }

    public class SplitExpense
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; set; }
        public Guid PayerId { get; set; }
        public Guid CreatedBy { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Note { get; set; }
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? ReceiptUrl { get; set; }
        public string SplitType { get; set; } = "Equal";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }

    public class SplitExpenseShare
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ExpenseId { get; set; }
        public Guid UserId { get; set; }
        public decimal ShareAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SplitSettlement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; set; }
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime SettlementDate { get; set; }
        public string? Note { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }

    public class SplitGroupActivity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public Guid? ReferenceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SplitNotification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Type { get; set; } = "GroupInvitationReceived"; // "GroupInvitationReceived", "InvitationAccepted", "InvitationRejected"
        public Guid? GroupId { get; set; }
        public Guid? ReferenceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}
