using System;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public class SplitAuthorizationResult
    {
        public bool IsAuthorized { get; set; }
        public int StatusCode { get; set; } = 200;
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public SplitGroup? Group { get; set; }
        public SplitGroupMember? Membership { get; set; }
        public SplitExpense? Expense { get; set; }
        public SplitSettlement? Settlement { get; set; }

        public static SplitAuthorizationResult Success(
            SplitGroup? group = null,
            SplitGroupMember? membership = null,
            SplitExpense? expense = null,
            SplitSettlement? settlement = null) =>
            new()
            {
                IsAuthorized = true,
                StatusCode = 200,
                Group = group,
                Membership = membership,
                Expense = expense,
                Settlement = settlement
            };

        public static SplitAuthorizationResult NotFound(string message = "Group not found.") =>
            new()
            {
                IsAuthorized = false,
                StatusCode = 404,
                ErrorCode = "NOT_FOUND",
                ErrorMessage = message
            };

        public static SplitAuthorizationResult Forbidden(string message = "You do not have permission to access this resource.") =>
            new()
            {
                IsAuthorized = false,
                StatusCode = 403,
                ErrorCode = "FORBIDDEN",
                ErrorMessage = message
            };
    }

    public interface ISplitGroupAuthorizationService
    {
        Task<SplitAuthorizationResult> AuthorizeMembershipAsync(Guid groupId, Guid userId);
        Task<SplitAuthorizationResult> AuthorizeOwnerAsync(Guid groupId, Guid userId);
        Task<SplitAuthorizationResult> AuthorizeExpenseAccessAsync(Guid groupId, Guid expenseId, Guid userId);
        Task<SplitAuthorizationResult> AuthorizeExpenseModificationAsync(Guid groupId, Guid expenseId, Guid userId);
        Task<SplitAuthorizationResult> AuthorizeSettlementAccessAsync(Guid groupId, Guid settlementId, Guid userId);
        Task<SplitAuthorizationResult> AuthorizeSettlementModificationAsync(Guid groupId, Guid settlementId, Guid userId);
    }
}
