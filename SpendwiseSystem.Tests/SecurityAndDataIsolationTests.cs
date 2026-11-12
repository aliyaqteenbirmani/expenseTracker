using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.SplitExpenses;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class SecurityAndDataIsolationTests
    {
        private class FakeSplitGroupRepository : ISplitGroupRepository
        {
            public List<SplitGroup> Groups { get; } = new();
            public List<SplitGroupMember> Members { get; } = new();

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
                Task.FromResult<User?>(null);

            public Task<SplitGroupInvitation?> GetInvitationByIdAsync(Guid invitationId) =>
                Task.FromResult<SplitGroupInvitation?>(null);

            public Task<SplitGroupInvitation?> GetPendingInvitationByEmailAsync(Guid groupId, string email) =>
                Task.FromResult<SplitGroupInvitation?>(null);

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId) =>
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByUserAsync(Guid userId, string userEmail) =>
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<SplitGroupInvitation> CreateInvitationAsync(SplitGroupInvitation invitation, string inviterName, string groupName) =>
                Task.FromResult(invitation);

            public Task<SplitGroupInvitation?> GetInvitationByTokenAsync(string token) =>
                Task.FromResult<SplitGroupInvitation?>(null);

            public Task<bool> UpdateInvitationResendAsync(Guid invitationId) =>
                Task.FromResult(true);

            public Task<List<SplitGroupInvitation>> LinkPendingInvitationsForUserAsync(Guid userId, string email) =>
                Task.FromResult(new List<SplitGroupInvitation>());

            public Task<bool> RevokeInvitationAsync(Guid invitationId, Guid revokedByUserId, string revokerName, Guid groupId) =>
                Task.FromResult(true);

            public Task<bool> AcceptInvitationAsync(Guid invitationId, Guid userId, string userName) =>
                Task.FromResult(true);

            public Task<bool> RejectInvitationAsync(Guid invitationId, Guid userId, string userName) =>
                Task.FromResult(true);

            public Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName)
            {
                var member = Members.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userIdToRemove);
                if (member != null) member.IsActive = false;
                return Task.FromResult(true);
            }

            public Task<List<SplitGroupActivityDto>> GetGroupActivitiesAsync(Guid groupId, int limit = 50) =>
                Task.FromResult(new List<SplitGroupActivityDto>());

            public Task<PagedGroupActivityResultDto> GetPagedGroupActivitiesAsync(Guid groupId, SplitGroupActivityQueryParameters parameters) =>
                Task.FromResult(new PagedGroupActivityResultDto());

            public Task<SplitGroupActivityDto?> GetActivityByIdAsync(Guid groupId, Guid activityId) =>
                Task.FromResult<SplitGroupActivityDto?>(null);

            public Task LogActivityAsync(Guid groupId, Guid userId, string activityType, string description, Guid? referenceId = null) =>
                Task.CompletedTask;
        }

        private class FakeSplitExpenseRepository : ISplitExpenseRepository
        {
            public List<SplitExpense> Expenses { get; } = new();

            public Task<SplitExpense?> GetExpenseByIdAsync(Guid expenseId) =>
                Task.FromResult(Expenses.FirstOrDefault(e => e.Id == expenseId && !e.IsDeleted));

            public Task<SplitExpenseDetailDto?> GetExpenseDetailsAsync(Guid expenseId, Guid groupId) =>
                Task.FromResult<SplitExpenseDetailDto?>(null);

            public Task<PagedExpenseResultDto> GetExpensesListAsync(Guid groupId, Guid currentUserId, SplitExpenseQueryParameters parameters) =>
                Task.FromResult(new PagedExpenseResultDto());

            public Task<SplitExpenseDetailDto> CreateExpenseWithSharesAndActivityAsync(
                SplitExpense expense, List<SplitExpenseShare> shares, string creatorName, string groupName) =>
                Task.FromResult(new SplitExpenseDetailDto());

            public Task<SplitExpenseDetailDto?> UpdateExpenseWithSharesAndActivityAsync(
                SplitExpense expense, List<SplitExpenseShare> newShares, string editorName, string groupName) =>
                Task.FromResult<SplitExpenseDetailDto?>(null);

            public Task<bool> SoftDeleteExpenseWithActivityAsync(
                Guid expenseId, Guid groupId, Guid deletedByUserId, string deletedByName) =>
                Task.FromResult(true);

            public Task<List<SplitExpenseShare>> GetExpenseSharesAsync(Guid expenseId) =>
                Task.FromResult(new List<SplitExpenseShare>());
        }

        private class FakeSplitSettlementRepository : ISplitSettlementRepository
        {
            public List<SplitSettlement> Settlements { get; } = new();

            public Task<SplitSettlement?> GetSettlementByIdAsync(Guid settlementId) =>
                Task.FromResult(Settlements.FirstOrDefault(s => s.Id == settlementId && !s.IsDeleted));

            public Task<SplitSettlementDto?> GetSettlementDetailsAsync(Guid settlementId, Guid groupId) =>
                Task.FromResult<SplitSettlementDto?>(null);

            public Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId) =>
                Task.FromResult(new List<SplitSettlementDto>());

            public Task<SplitSettlementDto> CreateSettlementWithActivityAsync(
                SplitSettlement settlement, string creatorName, string groupName) =>
                Task.FromResult(new SplitSettlementDto());

            public Task<SplitSettlementDto?> UpdateSettlementWithActivityAsync(
                SplitSettlement settlement, string editorName, string groupName) =>
                Task.FromResult<SplitSettlementDto?>(null);

            public Task<bool> SoftDeleteSettlementWithActivityAsync(
                Guid settlementId, Guid groupId, Guid deletedByUserId, string deletedByName) =>
                Task.FromResult(true);
        }

        // --- Security Test 1: Member cannot access a group they do not belong to ---
        [Fact]
        public async Task Security_01_MemberCannotAccessGroupTheyDoNotBelongTo()
        {
            // Arrange
            var groupRepo = new FakeSplitGroupRepository();
            var expenseRepo = new FakeSplitExpenseRepository();
            var settlementRepo = new FakeSplitSettlementRepository();
            var authService = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var groupAId = Guid.NewGuid();
            var groupBId = Guid.NewGuid();
            var userAId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupAId, Name = "Group A", IsActive = true, IsDeleted = false });
            groupRepo.Groups.Add(new SplitGroup { Id = groupBId, Name = "Group B", IsActive = true, IsDeleted = false });

            // User A is only member of Group A
            groupRepo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupAId, UserId = userAId, IsActive = true });

            // Act - User A attempts to access Group B
            var result = await authService.AuthorizeMembershipAsync(groupBId, userAId);

            // Assert
            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
            Assert.Equal("FORBIDDEN", result.ErrorCode);
        }

        // --- Security Test 2: Member cannot modify another group's expenses ---
        [Fact]
        public async Task Security_02_MemberCannotModifyAnotherGroupsExpenses()
        {
            // Arrange
            var groupRepo = new FakeSplitGroupRepository();
            var expenseRepo = new FakeSplitExpenseRepository();
            var settlementRepo = new FakeSplitSettlementRepository();
            var authService = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var groupAId = Guid.NewGuid();
            var groupBId = Guid.NewGuid();
            var userAId = Guid.NewGuid();
            var userBId = Guid.NewGuid();
            var expenseInGroupBId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupAId, Name = "Group A", OwnerId = userAId, IsActive = true });
            groupRepo.Groups.Add(new SplitGroup { Id = groupBId, Name = "Group B", OwnerId = userBId, IsActive = true });

            groupRepo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupAId, UserId = userAId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupBId, UserId = userBId, IsActive = true });

            expenseRepo.Expenses.Add(new SplitExpense
            {
                Id = expenseInGroupBId,
                GroupId = groupBId,
                CreatedBy = userBId,
                PayerId = userBId,
                Amount = 1000m,
                Title = "Group B Expense"
            });

            // Act - User A tries to modify Group B's expense using Group A's route context
            var crossGroupCheck = await authService.AuthorizeExpenseAccessAsync(groupAId, expenseInGroupBId, userAId);

            // Assert
            Assert.False(crossGroupCheck.IsAuthorized);
            Assert.Equal(403, crossGroupCheck.StatusCode);
            Assert.Equal("FORBIDDEN", crossGroupCheck.ErrorCode);
            Assert.Contains("does not belong to the specified group", crossGroupCheck.ErrorMessage);
        }

        // --- Security Test 3: Member cannot record settlements in another group ---
        [Fact]
        public async Task Security_03_MemberCannotRecordSettlementsInAnotherGroup()
        {
            // Arrange
            var groupRepo = new FakeSplitGroupRepository();
            var expenseRepo = new FakeSplitExpenseRepository();
            var settlementRepo = new FakeSplitSettlementRepository();
            var authService = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var groupAId = Guid.NewGuid();
            var groupBId = Guid.NewGuid();
            var userAId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupAId, Name = "Group A", IsActive = true });
            groupRepo.Groups.Add(new SplitGroup { Id = groupBId, Name = "Group B", IsActive = true });

            groupRepo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupAId, UserId = userAId, IsActive = true });

            // Act - User A attempts to record settlement in Group B
            var result = await authService.AuthorizeMembershipAsync(groupBId, userAId);

            // Assert
            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
        }

        // --- Security Test 4: Member cannot access another group's balances ---
        [Fact]
        public async Task Security_04_MemberCannotAccessAnotherGroupsBalances()
        {
            // Arrange
            var groupRepo = new FakeSplitGroupRepository();
            var expenseRepo = new FakeSplitExpenseRepository();
            var settlementRepo = new FakeSplitSettlementRepository();
            var authService = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var groupAId = Guid.NewGuid();
            var groupBId = Guid.NewGuid();
            var userAId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupAId, Name = "Group A", IsActive = true });
            groupRepo.Groups.Add(new SplitGroup { Id = groupBId, Name = "Group B", IsActive = true });

            groupRepo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupAId, UserId = userAId, IsActive = true });

            // Act - User A attempts to query balances for Group B
            var result = await authService.AuthorizeMembershipAsync(groupBId, userAId);

            // Assert
            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
            Assert.Equal("FORBIDDEN", result.ErrorCode);
        }

        // --- Security Test 5: Removed member cannot perform new group operations ---
        [Fact]
        public async Task Security_05_RemovedMemberCannotPerformNewGroupOperations()
        {
            // Arrange
            var groupRepo = new FakeSplitGroupRepository();
            var expenseRepo = new FakeSplitExpenseRepository();
            var settlementRepo = new FakeSplitSettlementRepository();
            var authService = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var groupId = Guid.NewGuid();
            var userRemovedId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip Group", IsActive = true });
            // Member was removed (IsActive = false)
            groupRepo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = userRemovedId, IsActive = false });

            // Act
            var result = await authService.AuthorizeMembershipAsync(groupId, userRemovedId);

            // Assert
            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("not an active member", result.ErrorMessage);
        }

        // --- Security Test 6: Removed member's historical financial information remains intact ---
        [Fact]
        public void Security_06_RemovedMemberHistoricalFinancialInformationRemainsIntact()
        {
            // Arrange
            var aliId = Guid.NewGuid();
            var ahmedId = Guid.NewGuid(); // removed member

            var members = new List<MemberRecord>
            {
                new(aliId, "Ali", "ali@test.com", "Owner", true),
                new(ahmedId, "Ahmed", "ahmed@test.com", "Member", false) // Inactive/removed
            };

            // Historical expense: Ali paid 2000, shared equally with Ahmed before Ahmed left
            var expenses = new List<ExpenseRecord>
            {
                new(
                    Guid.NewGuid(),
                    aliId,
                    2000m,
                    false,
                    new List<ShareRecord>
                    {
                        new(aliId, 1000m),
                        new(ahmedId, 1000m)
                    }
                )
            };

            var settlements = new List<SettlementRecord>();

            // Act - compute ledger
            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, settlements);

            // Assert
            var aliBalance = balances.First(b => b.UserId == aliId);
            var ahmedBalance = balances.First(b => b.UserId == ahmedId);

            Assert.Equal(1000m, aliBalance.NetBalance);
            Assert.Equal(-1000m, ahmedBalance.NetBalance);
            Assert.Equal(1000m, ahmedBalance.TotalShare);
            Assert.False(ahmedBalance.IsActive); // Marked inactive, but financial obligations preserved!
        }

        // --- Security Test 7: Split Group membership grants zero Cashbook access ---
        [Fact]
        public void Security_07_SplitGroupMembershipGrantsZeroCashbookAccess()
        {
            // Verify Entity & Domain decoupling:
            // SplitGroup entities must have ZERO properties of Cashbook types
            var splitEntitiesAssembly = typeof(SplitGroup).Assembly;
            var splitTypes = splitEntitiesAssembly.GetTypes()
                .Where(t => t.Name.StartsWith("Split"))
                .ToList();

            foreach (var type in splitTypes)
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    Assert.DoesNotContain("CashBook", prop.PropertyType.Name);
                    Assert.DoesNotContain("CashTransaction", prop.PropertyType.Name);
                    Assert.DoesNotContain("BusinessMember", prop.PropertyType.Name);
                    Assert.DoesNotContain("CashbookMember", prop.PropertyType.Name);
                }
            }
        }

        // --- Security Test 8: Cashbook membership grants zero Split Group access ---
        [Fact]
        public async Task Security_08_CashbookMembershipGrantsZeroSplitGroupAccess()
        {
            // Arrange
            var groupRepo = new FakeSplitGroupRepository();
            var expenseRepo = new FakeSplitExpenseRepository();
            var settlementRepo = new FakeSplitSettlementRepository();
            var authService = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var splitGroupId = Guid.NewGuid();
            var cashbookOnlyUserId = Guid.NewGuid(); // User has Cashbook access, but is NOT in SplitGroupMembers

            groupRepo.Groups.Add(new SplitGroup { Id = splitGroupId, Name = "Friends Split Group", IsActive = true });
            // Notice: groupRepo.Members does NOT contain cashbookOnlyUserId

            // Act - Cashbook user tries to access Split Group
            var result = await authService.AuthorizeMembershipAsync(splitGroupId, cashbookOnlyUserId);

            // Assert
            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
            Assert.Equal("FORBIDDEN", result.ErrorCode);
        }
    }
}
