using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs.SplitExpenseDtos
{
    public class CreateSplitGroupDto
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "Group name must be between 1 and 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Icon cannot exceed 100 characters.")]
        public string? Icon { get; set; }
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
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        public string? InvitedEmail { get; set; }

        public Guid? InvitedUserId { get; set; }
    }

    public class SplitGroupInvitationDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public Guid? InvitedUserId { get; set; }
        public string InvitedEmail { get; set; } = string.Empty;
        public Guid InvitedBy { get; set; }
        public string InvitedByName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class SplitGroupActivityDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public Guid? ReferenceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
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
        public string CurrentUserStatusText { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int SettledMembersCount { get; set; }
        public int UnsettledMembersCount { get; set; }
    }
}
