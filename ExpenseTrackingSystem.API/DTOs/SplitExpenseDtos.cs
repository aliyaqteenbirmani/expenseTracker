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
}
