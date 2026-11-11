using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class SplitGroupAuthorizationService : ISplitGroupAuthorizationService
    {
        private readonly ISplitGroupRepository _groupRepository;
        private readonly ISplitExpenseRepository _expenseRepository;
        private readonly ISplitSettlementRepository _settlementRepository;

        public SplitGroupAuthorizationService(
            ISplitGroupRepository groupRepository,
            ISplitExpenseRepository expenseRepository,
            ISplitSettlementRepository settlementRepository)
        {
            _groupRepository = groupRepository;
            _expenseRepository = expenseRepository;
            _settlementRepository = settlementRepository;
        }

        public async Task<SplitAuthorizationResult> AuthorizeMembershipAsync(Guid groupId, Guid userId)
        {
            if (groupId == Guid.Empty || userId == Guid.Empty)
                return SplitAuthorizationResult.NotFound("Invalid group or user identifier.");

            var group = await _groupRepository.GetGroupByIdAsync(groupId);
            if (group == null || group.IsDeleted || !group.IsActive)
                return SplitAuthorizationResult.NotFound("Group not found.");

            var membership = await _groupRepository.GetMembershipAsync(groupId, userId);
            if (membership == null || !membership.IsActive)
                return SplitAuthorizationResult.Forbidden("You are not an active member of this group.");

            return SplitAuthorizationResult.Success(group: group, membership: membership);
        }

        public async Task<SplitAuthorizationResult> AuthorizeOwnerAsync(Guid groupId, Guid userId)
        {
            var auth = await AuthorizeMembershipAsync(groupId, userId);
            if (!auth.IsAuthorized)
                return auth;

            var isOwner = auth.Group?.OwnerId == userId ||
                          string.Equals(auth.Membership?.Role, "Owner", StringComparison.OrdinalIgnoreCase);

            if (!isOwner)
                return SplitAuthorizationResult.Forbidden("Only the group owner has permission to perform this action.");

            return auth;
        }

        public async Task<SplitAuthorizationResult> AuthorizeExpenseAccessAsync(Guid groupId, Guid expenseId, Guid userId)
        {
            var auth = await AuthorizeMembershipAsync(groupId, userId);
            if (!auth.IsAuthorized)
                return auth;

            if (expenseId == Guid.Empty)
                return SplitAuthorizationResult.NotFound("Expense not found.");

            var expense = await _expenseRepository.GetExpenseByIdAsync(expenseId);
            if (expense == null || expense.IsDeleted)
                return SplitAuthorizationResult.NotFound("Expense not found.");

            // Cross-group isolation check
            if (expense.GroupId != groupId)
                return SplitAuthorizationResult.Forbidden("The requested expense does not belong to the specified group.");

            return SplitAuthorizationResult.Success(
                group: auth.Group,
                membership: auth.Membership,
                expense: expense);
        }

        public async Task<SplitAuthorizationResult> AuthorizeExpenseModificationAsync(Guid groupId, Guid expenseId, Guid userId)
        {
            var auth = await AuthorizeExpenseAccessAsync(groupId, expenseId, userId);
            if (!auth.IsAuthorized)
                return auth;

            var expense = auth.Expense!;
            var isCreator = expense.CreatedBy == userId;
            var isPayer = expense.PayerId == userId;
            var isOwner = auth.Group?.OwnerId == userId;

            if (!isCreator && !isPayer && !isOwner)
                return SplitAuthorizationResult.Forbidden("Only the expense creator, payer, or group owner can modify or delete this expense.");

            return auth;
        }

        public async Task<SplitAuthorizationResult> AuthorizeSettlementAccessAsync(Guid groupId, Guid settlementId, Guid userId)
        {
            var auth = await AuthorizeMembershipAsync(groupId, userId);
            if (!auth.IsAuthorized)
                return auth;

            if (settlementId == Guid.Empty)
                return SplitAuthorizationResult.NotFound("Settlement not found.");

            var settlement = await _settlementRepository.GetSettlementByIdAsync(settlementId);
            if (settlement == null || settlement.IsDeleted)
                return SplitAuthorizationResult.NotFound("Settlement not found.");

            // Cross-group isolation check
            if (settlement.GroupId != groupId)
                return SplitAuthorizationResult.Forbidden("The requested settlement does not belong to the specified group.");

            return SplitAuthorizationResult.Success(
                group: auth.Group,
                membership: auth.Membership,
                settlement: settlement);
        }

        public async Task<SplitAuthorizationResult> AuthorizeSettlementModificationAsync(Guid groupId, Guid settlementId, Guid userId)
        {
            var auth = await AuthorizeSettlementAccessAsync(groupId, settlementId, userId);
            if (!auth.IsAuthorized)
                return auth;

            var settlement = auth.Settlement!;
            var isCreator = settlement.CreatedBy == userId;
            var isPayer = settlement.FromUserId == userId;
            var isOwner = auth.Group?.OwnerId == userId;

            if (!isCreator && !isPayer && !isOwner)
                return SplitAuthorizationResult.Forbidden("Only the settlement creator, payer, or group owner can modify or delete this settlement.");

            return auth;
        }
    }
}
