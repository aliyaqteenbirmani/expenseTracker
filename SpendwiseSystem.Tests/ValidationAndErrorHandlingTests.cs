using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.SplitExpenses;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class ValidationAndErrorHandlingTests
    {
        private class TestGroupRepository : ISplitGroupRepository
        {
            public List<SplitGroup> Groups { get; } = new();
            public List<SplitGroupMember> Members { get; } = new();
            public List<SplitGroupInvitation> Invitations { get; } = new();
            public List<User> Users { get; } = new();

            public Task<SplitGroup?> GetGroupByIdAsync(Guid groupId) =>
                Task.FromResult(Groups.FirstOrDefault(g => g.Id == groupId && !g.IsDeleted));

            public Task<SplitGroupMember?> GetMembershipAsync(Guid groupId, Guid userId) =>
                Task.FromResult(Members.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId));

            public Task<List<SplitGroupSummaryDto>> GetUserGroupsAsync(Guid userId) =>
                Task.FromResult(new List<SplitGroupSummaryDto>());

            public Task<SplitGroupDetailDto?> GetGroupDetailsAsync(Guid groupId, Guid userId) =>
                Task.FromResult<SplitGroupDetailDto?>(null);

            public Task<List<SplitGroupMemberDto>> GetGroupMembersAsync(Guid groupId) =>
                Task.FromResult(Members.Where(m => m.GroupId == groupId).Select(m => new SplitGroupMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    Role = m.Role,
                    IsActive = m.IsActive
                }).ToList());

            public Task<SplitGroup> CreateGroupWithMemberAndActivityAsync(SplitGroup group, Guid ownerId, string ownerName) =>
                Task.FromResult(group);

            public Task<SplitGroup> CreateGroupWithMemberAndInvitationsAsync(
                SplitGroup group, Guid ownerId, string ownerName, List<SplitGroupInvitation> invitations, List<SplitGroupActivity> activities) =>
                Task.FromResult(group);

            public Task<List<SplitUserSearchResultDto>> SearchUsersAsync(string query, Guid currentUserId, int limit = 10) =>
                Task.FromResult(new List<SplitUserSearchResultDto>());

            public Task<bool> UpdateGroupAsync(SplitGroup group, Guid userId, string userName) =>
                Task.FromResult(true);

            public Task<bool> SoftDeleteGroupAsync(Guid groupId, Guid userId, string userName) =>
                Task.FromResult(true);

            public Task<bool> HasFinancialRecordsAsync(Guid groupId) =>
                Task.FromResult(false);

            public Task<User?> GetUserByEmailOrIdAsync(string? email, Guid? userId) =>
                Task.FromResult(Users.FirstOrDefault(u =>
                    (!string.IsNullOrEmpty(email) && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                    (userId.HasValue && u.Id == userId.Value)));

            public Task<SplitGroupInvitation?> GetInvitationByIdAsync(Guid invitationId) =>
                Task.FromResult(Invitations.FirstOrDefault(i => i.Id == invitationId));

            public Task<SplitGroupInvitation?> GetPendingInvitationByEmailAsync(Guid groupId, string email) =>
                Task.FromResult(Invitations.FirstOrDefault(i =>
                    i.GroupId == groupId &&
                    i.Status == "Pending" &&
                    i.InvitedEmail.Equals(email, StringComparison.OrdinalIgnoreCase)));

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId) =>
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByUserAsync(Guid userId, string userEmail) =>
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<SplitGroupInvitation> CreateInvitationAsync(SplitGroupInvitation invitation, string inviterName, string groupName)
            {
                Invitations.Add(invitation);
                return Task.FromResult(invitation);
            }

            public Task<SplitGroupInvitation?> GetInvitationByTokenAsync(string token)
            {
                return Task.FromResult(Invitations.FirstOrDefault(i => i.InvitationToken == token || i.TokenHash == token));
            }

            public Task<bool> UpdateInvitationResendAsync(Guid invitationId)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite != null)
                {
                    invite.ResendCount++;
                    invite.LastSentAt = DateTime.UtcNow;
                }
                return Task.FromResult(true);
            }

            public Task<List<SplitGroupInvitation>> LinkPendingInvitationsForUserAsync(Guid userId, string email)
            {
                var pending = Invitations.Where(i => string.Equals(i.InvitedEmail, email, StringComparison.OrdinalIgnoreCase) && !i.InvitedUserId.HasValue).ToList();
                foreach (var p in pending)
                {
                    p.InvitedUserId = userId;
                }
                return Task.FromResult(pending);
            }

            public Task<bool> RevokeInvitationAsync(Guid invitationId, Guid revokedByUserId, string revokerName, Guid groupId) =>
                Task.FromResult(true);

            public Task<bool> AcceptInvitationAsync(Guid invitationId, Guid userId, string userName)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite == null || invite.Status != "Pending" || invite.ExpiresAt <= DateTime.UtcNow)
                    return Task.FromResult(false);
                invite.Status = "Accepted";
                return Task.FromResult(true);
            }

            public Task<bool> RejectInvitationAsync(Guid invitationId, Guid userId, string userName) =>
                Task.FromResult(true);

            public Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName) =>
                Task.FromResult(true);

            public Task<List<SplitGroupActivityDto>> GetGroupActivitiesAsync(Guid groupId, int limit = 50) =>
                Task.FromResult(new List<SplitGroupActivityDto>());

            public Task<PagedGroupActivityResultDto> GetPagedGroupActivitiesAsync(Guid groupId, SplitGroupActivityQueryParameters parameters) =>
                Task.FromResult(new PagedGroupActivityResultDto());

            public Task<SplitGroupActivityDto?> GetActivityByIdAsync(Guid groupId, Guid activityId) =>
                Task.FromResult<SplitGroupActivityDto?>(null);

            public Task LogActivityAsync(Guid groupId, Guid userId, string activityType, string description, Guid? referenceId = null) =>
                Task.CompletedTask;
        }

        private class TestExpenseRepository : ISplitExpenseRepository
        {
            public List<SplitExpense> Expenses { get; } = new();

            public Task<SplitExpense?> GetExpenseByIdAsync(Guid expenseId) =>
                Task.FromResult(Expenses.FirstOrDefault(e => e.Id == expenseId && !e.IsDeleted));

            public Task<SplitExpenseDetailDto?> GetExpenseDetailsAsync(Guid expenseId, Guid groupId) =>
                Task.FromResult<SplitExpenseDetailDto?>(null);

            public Task<PagedExpenseResultDto> GetExpensesListAsync(Guid groupId, Guid currentUserId, SplitExpenseQueryParameters parameters) =>
                Task.FromResult(new PagedExpenseResultDto());

            public Task<SplitExpenseDetailDto> CreateExpenseWithSharesAndActivityAsync(
                SplitExpense expense, List<SplitExpenseShare> shares, string creatorName, string groupName, List<SplitNotification>? notifications = null)
            {
                Expenses.Add(expense);
                return Task.FromResult(new SplitExpenseDetailDto { Id = expense.Id, Amount = expense.Amount });
            }

            public Task<SplitExpenseDetailDto?> UpdateExpenseWithSharesAndActivityAsync(
                SplitExpense expense, List<SplitExpenseShare> newShares, string editorName, string groupName, List<SplitNotification>? notifications = null) =>
                Task.FromResult<SplitExpenseDetailDto?>(null);

            public Task<bool> SoftDeleteExpenseWithActivityAsync(
                Guid expenseId, Guid groupId, Guid deletedByUserId, string deletedByName, List<SplitNotification>? notifications = null) =>
                Task.FromResult(true);

            public Task<List<SplitExpenseShare>> GetExpenseSharesAsync(Guid expenseId) =>
                Task.FromResult(new List<SplitExpenseShare>());
        }

        private class TestSettlementRepository : ISplitSettlementRepository
        {
            public List<SplitSettlement> Settlements { get; } = new();

            public Task<SplitSettlement?> GetSettlementByIdAsync(Guid settlementId) =>
                Task.FromResult(Settlements.FirstOrDefault(s => s.Id == settlementId && !s.IsDeleted));

            public Task<SplitSettlementDto?> GetSettlementDetailsAsync(Guid settlementId, Guid groupId) =>
                Task.FromResult<SplitSettlementDto?>(null);

            public Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId) =>
                Task.FromResult(new List<SplitSettlementDto>());

            public Task<SplitSettlementDto> CreateSettlementWithActivityAsync(
                SplitSettlement settlement, string creatorName, string groupName)
            {
                Settlements.Add(settlement);
                return Task.FromResult(new SplitSettlementDto { Id = settlement.Id, Amount = settlement.Amount });
            }

            public Task<SplitSettlementDto?> UpdateSettlementWithActivityAsync(
                SplitSettlement settlement, string editorName, string groupName) =>
                Task.FromResult<SplitSettlementDto?>(null);

            public Task<bool> SoftDeleteSettlementWithActivityAsync(
                Guid settlementId, Guid groupId, Guid deletedByUserId, string deletedByName) =>
                Task.FromResult(true);
        }

        private class TestBalanceRepository : ISplitBalanceRepository
        {
            public List<MemberBalanceDto> Balances { get; set; } = new();

            public Task<List<MemberBalanceDto>> GetGroupBalancesAsync(Guid groupId) =>
                Task.FromResult(Balances);

            public Task<MemberBalanceDto?> GetMemberBalanceAsync(Guid groupId, Guid userId) =>
                Task.FromResult(Balances.FirstOrDefault(b => b.UserId == userId));

            public Task<GroupFinancialSummaryDto?> GetGroupFinancialSummaryAsync(Guid groupId, Guid currentUserId) =>
                Task.FromResult<GroupFinancialSummaryDto?>(new GroupFinancialSummaryDto());
        }

        // --- Scenario 1: Amount is zero -> Reject request ---
        [Fact]
        public async Task Validation_01_AmountZero_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var calc = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calc);

            var groupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = user1, IsActive = true });

            var result = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Dinner",
                Amount = 0m,
                ParticipantUserIds = new List<Guid> { user1 }
            }, user1, "User 1");

            Assert.False(result.Success);
            Assert.Contains("greater than zero", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        // --- Scenario 2: Amount is negative -> Reject request ---
        [Fact]
        public async Task Validation_02_AmountNegative_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var calc = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calc);

            var groupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = user1, IsActive = true });

            var result = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Dinner",
                Amount = -500m,
                ParticipantUserIds = new List<Guid> { user1 }
            }, user1, "User 1");

            Assert.False(result.Success);
            Assert.Contains("greater than zero", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        // --- Scenario 3: No participant selected -> Reject request ---
        [Fact]
        public async Task Validation_03_NoParticipantSelected_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var calc = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calc);

            var groupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = user1, IsActive = true });

            var result = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Dinner",
                Amount = 1000m,
                ParticipantUserIds = new List<Guid>() // Empty!
            }, user1, "User 1");

            Assert.False(result.Success);
            Assert.Contains("At least one group member must be included", result.Message);
        }

        // --- Scenario 4: Invalid group ID -> Return appropriate error (404) ---
        [Fact]
        public async Task Validation_04_InvalidGroupId_ReturnsAppropriateError()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var settlementRepo = new TestSettlementRepository();
            var auth = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var nonExistentGroupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();

            var result = await auth.AuthorizeMembershipAsync(nonExistentGroupId, user1);

            Assert.False(result.IsAuthorized);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("NOT_FOUND", result.ErrorCode);
        }

        // --- Scenario 5: Non-member selected -> Reject request ---
        [Fact]
        public async Task Validation_05_NonMemberSelected_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var calc = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calc);

            var groupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var outsider = Guid.NewGuid(); // Not in group

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = user1, IsActive = true });

            var result = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Groceries",
                Amount = 1500m,
                ParticipantUserIds = new List<Guid> { user1, outsider }
            }, user1, "User 1");

            Assert.False(result.Success);
            Assert.Contains("not active members", result.Message);
        }

        // --- Scenario 6: Removed member selected -> Reject request ---
        [Fact]
        public async Task Validation_06_RemovedMemberSelected_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var calc = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calc);

            var groupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var userRemoved = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = user1, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = userRemoved, IsActive = false }); // Inactive!

            var result = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Lunch",
                Amount = 2000m,
                ParticipantUserIds = new List<Guid> { user1, userRemoved }
            }, user1, "User 1");

            Assert.False(result.Success);
            Assert.Contains("not active members", result.Message);
        }

        // --- Scenario 7: Invalid payer -> Reject request ---
        [Fact]
        public async Task Validation_07_InvalidPayer_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var calc = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calc);

            var groupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var nonMemberPayer = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = user1, IsActive = true });

            var result = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Fuel",
                Amount = 2500m,
                PayerId = nonMemberPayer,
                ParticipantUserIds = new List<Guid> { user1 }
            }, user1, "User 1");

            Assert.False(result.Success);
            Assert.Contains("specified payer is not an active member", result.Message);
        }

        // --- Scenario 8: Duplicate invitation -> Reject request ---
        [Fact]
        public async Task Validation_08_DuplicateInvitation_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var service = new SplitGroupService(groupRepo);

            var groupId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var invitedEmail = "invitee@test.com";

            var inviteeId = Guid.NewGuid();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Users.Add(new User { Id = inviteeId, Email = invitedEmail, FirstName = "Invitee", LastName = "User" });
            groupRepo.Invitations.Add(new SplitGroupInvitation
            {
                GroupId = groupId,
                InvitedUserId = inviteeId,
                InvitedEmail = invitedEmail,
                Status = "Pending",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            var result = await service.InviteMemberAsync(groupId, new InviteSplitGroupMemberDto
            {
                InvitedEmail = invitedEmail
            }, ownerId, "Owner");

            Assert.False(result.Success);
            Assert.Contains("pending invitation has already been sent", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        // --- Scenario 9: Unauthorized expense update -> Return 403 ---
        [Fact]
        public async Task Validation_09_UnauthorizedExpenseUpdate_Returns403()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var settlementRepo = new TestSettlementRepository();
            var auth = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var groupId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var bystanderId = Guid.NewGuid();
            var expenseId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = creatorId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = bystanderId, IsActive = true });

            expenseRepo.Expenses.Add(new SplitExpense
            {
                Id = expenseId,
                GroupId = groupId,
                CreatedBy = creatorId,
                PayerId = creatorId,
                Amount = 1000m
            });

            // Bystander attempts to update creator's expense
            var result = await auth.AuthorizeExpenseModificationAsync(groupId, expenseId, bystanderId);

            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
            Assert.Equal("FORBIDDEN", result.ErrorCode);
        }

        // --- Scenario 10: Unauthorized expense deletion -> Return 403 ---
        [Fact]
        public async Task Validation_10_UnauthorizedExpenseDeletion_Returns403()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var settlementRepo = new TestSettlementRepository();
            var auth = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var groupId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var bystanderId = Guid.NewGuid();
            var expenseId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = creatorId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = bystanderId, IsActive = true });

            expenseRepo.Expenses.Add(new SplitExpense
            {
                Id = expenseId,
                GroupId = groupId,
                CreatedBy = creatorId,
                PayerId = creatorId,
                Amount = 1000m
            });

            var result = await auth.AuthorizeExpenseModificationAsync(groupId, expenseId, bystanderId);

            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
        }

        // --- Scenario 11: Unauthorized group access -> Return 403 ---
        [Fact]
        public async Task Validation_11_UnauthorizedGroupAccess_Returns403()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var settlementRepo = new TestSettlementRepository();
            var auth = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var groupId = Guid.NewGuid();
            var outsiderId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Secret Group", IsActive = true });

            var result = await auth.AuthorizeMembershipAsync(groupId, outsiderId);

            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
            Assert.Equal("FORBIDDEN", result.ErrorCode);
        }

        // --- Scenario 12: Settlement amount is invalid -> Reject request ---
        [Fact]
        public async Task Validation_12_SettlementAmountInvalid_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var settlementRepo = new TestSettlementRepository();
            var balanceRepo = new TestBalanceRepository();
            var debtService = new DebtSimplificationService();
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, debtService);

            var groupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = user1, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = user2, IsActive = true });

            // Amount = 0
            var resultZero = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = user1,
                ToUserId = user2,
                Amount = 0m
            }, user1, "User 1");

            Assert.False(resultZero.Success);
            Assert.Contains("greater than zero", resultZero.Message);

            // Amount exceeds actual debt
            balanceRepo.Balances = new List<MemberBalanceDto>
            {
                new() { UserId = user1, FullName = "User 1", NetBalance = -500m },
                new() { UserId = user2, FullName = "User 2", NetBalance = 500m }
            };

            var resultExcess = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = user1,
                ToUserId = user2,
                Amount = 600m // Exceeds 500 debt!
            }, user1, "User 1");

            Assert.False(resultExcess.Success);
            Assert.Contains("exceeds", resultExcess.Message);
        }

        // --- Scenario 13: Sender equals receiver -> Reject request ---
        [Fact]
        public async Task Validation_13_SenderEqualsReceiver_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var settlementRepo = new TestSettlementRepository();
            var balanceRepo = new TestBalanceRepository();
            var debtService = new DebtSimplificationService();
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, debtService);

            var groupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = user1, IsActive = true });

            var result = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = user1,
                ToUserId = user1, // Same!
                Amount = 500m
            }, user1, "User 1");

            Assert.False(result.Success);
            Assert.Contains("must be different", result.Message);
        }

        // --- Scenario 14: Expired invitation -> Reject acceptance ---
        [Fact]
        public async Task Validation_14_ExpiredInvitation_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var service = new SplitGroupService(groupRepo);

            var invitationId = Guid.NewGuid();
            var invitedUser = Guid.NewGuid();
            var email = "user@test.com";

            groupRepo.Invitations.Add(new SplitGroupInvitation
            {
                Id = invitationId,
                InvitedEmail = email,
                Status = "Pending",
                ExpiresAt = DateTime.UtcNow.AddHours(-2) // Expired 2 hours ago!
            });

            var result = await service.AcceptInvitationAsync(invitationId, invitedUser, email, "Invited User");

            Assert.False(result.Success);
            Assert.Contains("expired", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        // --- Scenario 15: Duplicate membership -> Prevent duplicate record ---
        [Fact]
        public async Task Validation_15_DuplicateMembership_Prevented()
        {
            var groupRepo = new TestGroupRepository();
            var service = new SplitGroupService(groupRepo);

            var groupId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var existingMemberId = Guid.NewGuid();
            var existingEmail = "member@test.com";

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Test Group", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = existingMemberId, Role = "Member", IsActive = true });
            groupRepo.Users.Add(new User { Id = existingMemberId, Email = existingEmail, FirstName = "Existing", LastName = "Member" });

            // Attempt to invite an already active member
            var result = await service.InviteMemberAsync(groupId, new InviteSplitGroupMemberDto
            {
                InvitedEmail = existingEmail
            }, ownerId, "Owner");

            Assert.False(result.Success);
            Assert.Contains("already an active member", result.Message);
        }

        // --- Scenario 16: Expense does not belong to group -> Reject request ---
        [Fact]
        public async Task Validation_16_ExpenseDoesNotBelongToGroup_IsRejected()
        {
            var groupRepo = new TestGroupRepository();
            var expenseRepo = new TestExpenseRepository();
            var settlementRepo = new TestSettlementRepository();
            var auth = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var group1Id = Guid.NewGuid();
            var group2Id = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var expenseInGroup2 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = group1Id, Name = "Group 1", IsActive = true });
            groupRepo.Groups.Add(new SplitGroup { Id = group2Id, Name = "Group 2", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = group1Id, UserId = user1, IsActive = true });

            expenseRepo.Expenses.Add(new SplitExpense
            {
                Id = expenseInGroup2,
                GroupId = group2Id, // Belongs to Group 2
                Amount = 1000m
            });

            // Attempt to access Group 2's expense under Group 1's route context
            var result = await auth.AuthorizeExpenseAccessAsync(group1Id, expenseInGroup2, user1);

            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("does not belong to the specified group", result.ErrorMessage);
        }
    }
}
