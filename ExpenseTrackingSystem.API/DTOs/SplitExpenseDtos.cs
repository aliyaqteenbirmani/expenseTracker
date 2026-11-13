using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs.SplitExpenseDtos
{
    public class CreateGroupInviteeInputDto
    {
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
    }

    public class CreateGroupInviteeResultDto
    {
        public string Identifier { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Invited", "AlreadyPending", "Skipped", "Failed"
        public string Message { get; set; } = string.Empty;
        public Guid? InvitationId { get; set; }
    }

    public class CreateSplitGroupResultDto : SplitGroupSummaryDto
    {
        public List<CreateGroupInviteeResultDto> Invitations { get; set; } = new();
    }

    public class BatchInviteMembersDto
    {
        public List<CreateGroupInviteeInputDto> Invitees { get; set; } = new();
    }

    public class BatchInviteResultDto
    {
        public List<CreateGroupInviteeResultDto> Results { get; set; } = new();
        public int TotalCount => Results.Count;
        public int SuccessCount => Results.FindAll(r => r.Status == "Invited").Count;
        public int FailureCount => Results.FindAll(r => r.Status != "Invited").Count;
    }

    public class SplitUserSearchResultDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CreateSplitGroupDto
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "Group name must be between 1 and 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Icon cannot exceed 100 characters.")]
        public string? Icon { get; set; }

        public List<CreateGroupInviteeInputDto>? Invitees { get; set; }
    }

    public class UpdateSplitGroupDto
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "Group name must be between 1 and 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Icon cannot exceed 100 characters.")]
        public string? Icon { get; set; }
    }

    public class SplitGroupSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal CurrentUserBalance { get; set; }
        public string CurrentUserRole { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SplitGroupMemberDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        public decimal Balance { get; set; }
    }

    public class SplitGroupDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal CurrentUserBalance { get; set; }
        public string CurrentUserRole { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<SplitGroupMemberDto> Members { get; set; } = new();
    }

    public class InviteSplitGroupMemberDto
    {
        private string? _email;

        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        public string? InvitedEmail
        {
            get => _email;
            set => _email = value?.Trim();
        }

        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        public string? Email
        {
            get => _email;
            set => _email = value?.Trim();
        }

        public Guid? InvitedUserId { get; set; }
        public Guid? UserId
        {
            get => InvitedUserId;
            set => InvitedUserId = value;
        }
    }

    public class SplitGroupInvitationDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public Guid? InvitedUserId { get; set; }
        public string? InvitedUserName { get; set; }
        public string InvitedEmail { get; set; } = string.Empty;
        public Guid InvitedBy { get; set; }
        public string InvitedByName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? LastSentAt { get; set; }
        public int ResendCount { get; set; }
        public string? InvitationToken { get; set; }
    }

    public class SplitNotificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public Guid? GroupId { get; set; }
        public string? GroupName { get; set; }
        public Guid? ReferenceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class SplitNotificationUnreadCountDto
    {
        public int UnreadCount { get; set; }
    }

    public class PagedSplitNotificationResultDto
    {
        public List<SplitNotificationDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }

    public class SplitGroupActivityDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public Guid? ReferenceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SplitGroupActivityQueryParameters
    {
        public string? ActivityType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedGroupActivityResultDto
    {
        public List<SplitGroupActivityDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }

    // --- Expense Management DTOs ---

    public class CreateSplitExpenseDto
    {
        [Required(ErrorMessage = "Expense title is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Range(0.01, 1000000000.00, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [StringLength(1000, ErrorMessage = "Note cannot exceed 1000 characters.")]
        public string? Note { get; set; }

        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
        public string? Category { get; set; }

        public DateTime? ExpenseDate { get; set; }

        public Guid? PayerId { get; set; }

        [StringLength(1000, ErrorMessage = "Receipt URL cannot exceed 1000 characters.")]
        public string? ReceiptUrl { get; set; }

        [Required(ErrorMessage = "At least one participant must be included in the expense.")]
        [MinLength(1, ErrorMessage = "At least one participant must be included in the expense.")]
        public List<Guid> ParticipantUserIds { get; set; } = new();
    }

    public class UpdateSplitExpenseDto
    {
        [Required(ErrorMessage = "Expense title is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Range(0.01, 1000000000.00, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [StringLength(1000, ErrorMessage = "Note cannot exceed 1000 characters.")]
        public string? Note { get; set; }

        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
        public string? Category { get; set; }

        public DateTime? ExpenseDate { get; set; }

        [Required(ErrorMessage = "Payer is required.")]
        public Guid PayerId { get; set; }

        [StringLength(1000, ErrorMessage = "Receipt URL cannot exceed 1000 characters.")]
        public string? ReceiptUrl { get; set; }

        [Required(ErrorMessage = "At least one participant must be included in the expense.")]
        [MinLength(1, ErrorMessage = "At least one participant must be included in the expense.")]
        public List<Guid> ParticipantUserIds { get; set; } = new();
    }

    public class SplitExpenseListItemDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid PayerId { get; set; }
        public string PayerName { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public string? Category { get; set; }
        public int ParticipantCount { get; set; }
        public decimal CurrentUserShare { get; set; }
        public Guid CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SplitExpenseShareItemDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public decimal ShareAmount { get; set; }
        public bool IsPayer { get; set; }
    }

    public class SplitExpenseDetailDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid PayerId { get; set; }
        public string PayerName { get; set; } = string.Empty;
        public string PayerEmail { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string CreatedByEmail { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Note { get; set; }
        public string? ReceiptUrl { get; set; }
        public string SplitType { get; set; } = "Equal";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<SplitExpenseShareItemDto> Shares { get; set; } = new();
    }

    public class SplitExpenseQueryParameters
    {
        public string? Search { get; set; }
        public string? Category { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } // "date", "amount", "title"
        public bool IsDescending { get; set; } = true;
    }

    public class PagedExpenseResultDto
    {
        public List<SplitExpenseListItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }

    // --- Balance Tracking DTOs ---

    public class MemberBalanceDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public bool IsActive { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalShare { get; set; }
        public decimal SettlementsPaid { get; set; }
        public decimal SettlementsReceived { get; set; }
        public decimal PendingPaid { get; set; }
        public decimal PendingReceived { get; set; }
        public decimal PendingSettlementsPaid { get => PendingPaid; set => PendingPaid = value; }
        public decimal PendingSettlementsReceived { get => PendingReceived; set => PendingReceived = value; }
        public decimal NetBalance { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
    }

    public class GroupFinancialSummaryDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public decimal TotalExpenses { get; set; }
        public decimal TotalSettlements { get; set; }
        public decimal CurrentUserBalance { get; set; }
        public decimal CurrentUserTotalPaid { get; set; }
        public decimal CurrentUserTotalShare { get; set; }
        public decimal CurrentUserPendingPaid { get; set; }
        public decimal CurrentUserPendingReceived { get; set; }
        public string CurrentUserStatusText { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int SettledMembersCount { get; set; }
        public int UnsettledMembersCount { get; set; }
    }

    // --- Settlement Management DTOs ---

    public class RecordSettlementDto
    {
        [Required(ErrorMessage = "Payer (FromUserId) is required.")]
        public Guid FromUserId { get; set; }

        [Required(ErrorMessage = "Receiver (ToUserId) is required.")]
        public Guid ToUserId { get; set; }

        [Range(0.01, 1000000000.00, ErrorMessage = "Settlement amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public DateTime? SettlementDate { get; set; }

        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }

    public class DeclineSettlementDto
    {
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }
    }

    public class UpdateSettlementDto
    {
        [Required(ErrorMessage = "Payer (FromUserId) is required.")]
        public Guid FromUserId { get; set; }

        [Required(ErrorMessage = "Receiver (ToUserId) is required.")]
        public Guid ToUserId { get; set; }

        [Range(0.01, 1000000000.00, ErrorMessage = "Settlement amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public DateTime? SettlementDate { get; set; }

        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }

    public class SplitSettlementDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public Guid? GroupOwnerId { get; set; }
        public Guid FromUserId { get; set; }
        public string FromUserName { get; set; } = string.Empty;
        public string FromUserEmail { get; set; } = string.Empty;
        public Guid ToUserId { get; set; }
        public string ToUserName { get; set; } = string.Empty;
        public string ToUserEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime SettlementDate { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = "PendingReceiver";
        public Guid CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public Guid? ReceiverActionBy { get; set; }
        public string? ReceiverActionByName { get; set; }
        public DateTime? ReceiverActionAt { get; set; }
        public Guid? OwnerActionBy { get; set; }
        public string? OwnerActionByName { get; set; }
        public DateTime? OwnerActionAt { get; set; }
        public string? DeclineReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public Guid? CancelledBy { get; set; }
        public string? CancelledByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool CanConfirm { get; set; }
        public bool CanDecline { get; set; }
        public bool CanCancel { get; set; }
        public bool CanReverse { get; set; }
    }

    public class SuggestedSettlementDto
    {
        public Guid FromUserId { get; set; }
        public string FromUserName { get; set; } = string.Empty;
        public string FromUserEmail { get; set; } = string.Empty;
        public Guid ToUserId { get; set; }
        public string ToUserName { get; set; } = string.Empty;
        public string ToUserEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class SettleUpOverviewDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public decimal TotalOutstandingDebt { get; set; }
        public List<MemberBalanceDto> Debtors { get; set; } = new();
        public List<MemberBalanceDto> Creditors { get; set; } = new();
        public List<SuggestedSettlementDto> SuggestedTransactions { get; set; } = new();
    }
}
