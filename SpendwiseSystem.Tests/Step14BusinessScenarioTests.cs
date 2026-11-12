using System;
using System.Collections.Concurrent;
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
    public class Step14BusinessScenarioTests
    {
        #region Test In-Memory Repositories

        private class InMemoryGroupRepository : ISplitGroupRepository
        {
            public List<SplitGroup> Groups { get; } = new();
            public List<SplitGroupMember> Members { get; } = new();
            public List<SplitGroupInvitation> Invitations { get; } = new();
            public List<User> Users { get; } = new();
            public List<SplitGroupActivity> Activities { get; } = new();

            public Task<SplitGroup?> GetGroupByIdAsync(Guid groupId) =>
                Task.FromResult(Groups.FirstOrDefault(g => g.Id == groupId && !g.IsDeleted));

            public Task<SplitGroupMember?> GetMembershipAsync(Guid groupId, Guid userId) =>
                Task.FromResult(Members.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId));

            public Task<List<SplitGroupSummaryDto>> GetUserGroupsAsync(Guid userId)
            {
                var userGroupIds = Members.Where(m => m.UserId == userId && m.IsActive).Select(m => m.GroupId).ToHashSet();
                var list = Groups.Where(g => userGroupIds.Contains(g.Id) && !g.IsDeleted).Select(g => new SplitGroupSummaryDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    OwnerId = g.OwnerId,
                    MemberCount = Members.Count(m => m.GroupId == g.Id && m.IsActive),
                    CreatedAt = g.CreatedAt
                }).ToList();
                return Task.FromResult(list);
            }

            public Task<SplitGroupDetailDto?> GetGroupDetailsAsync(Guid groupId, Guid userId)
            {
                var group = Groups.FirstOrDefault(g => g.Id == groupId && !g.IsDeleted);
                if (group == null) return Task.FromResult<SplitGroupDetailDto?>(null);

                var members = Members.Where(m => m.GroupId == groupId).Select(m => new SplitGroupMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    Role = m.Role,
                    IsActive = m.IsActive,
                    FullName = Users.FirstOrDefault(u => u.Id == m.UserId) is { } u ? $"{u.FirstName} {u.LastName}".Trim() : "Member",
                    Email = Users.FirstOrDefault(u => u.Id == m.UserId)?.Email ?? "email@test.com"
                }).ToList();

                var currentMember = Members.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId);

                return Task.FromResult<SplitGroupDetailDto?>(new SplitGroupDetailDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    OwnerId = group.OwnerId,
                    CurrentUserRole = currentMember?.Role ?? string.Empty,
                    MemberCount = members.Count(m => m.IsActive),
                    Members = members,
                    CreatedAt = group.CreatedAt
                });
            }

            public Task<List<SplitGroupMemberDto>> GetGroupMembersAsync(Guid groupId)
            {
                var members = Members.Where(m => m.GroupId == groupId).Select(m => new SplitGroupMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    Role = m.Role,
                    IsActive = m.IsActive,
                    FullName = Users.FirstOrDefault(u => u.Id == m.UserId) is { } u ? $"{u.FirstName} {u.LastName}".Trim() : "Member",
                    Email = Users.FirstOrDefault(u => u.Id == m.UserId)?.Email ?? "email@test.com"
                }).ToList();
                return Task.FromResult(members);
            }

            public Task<SplitGroup> CreateGroupWithMemberAndActivityAsync(SplitGroup group, Guid ownerId, string ownerName)
            {
                Groups.Add(group);
                var member = new SplitGroupMember
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    UserId = ownerId,
                    Role = "Owner",
                    IsActive = true,
                    JoinedAt = group.CreatedAt,
                    CreatedAt = group.CreatedAt,
                    UpdatedAt = group.UpdatedAt
                };
                Members.Add(member);
                Activities.Add(new SplitGroupActivity
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    UserId = ownerId,
                    ActivityType = "GroupCreated",
                    Description = $"{ownerName} created the group.",
                    CreatedAt = group.CreatedAt
                });
                return Task.FromResult(group);
            }

            public Task<bool> UpdateGroupAsync(SplitGroup group, Guid userId, string userName)
            {
                var existing = Groups.FirstOrDefault(g => g.Id == group.Id);
                if (existing == null) return Task.FromResult(false);
                existing.Name = group.Name;
                existing.Description = group.Description;
                existing.Icon = group.Icon;
                existing.UpdatedAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }

            public Task<bool> SoftDeleteGroupAsync(Guid groupId, Guid userId, string userName)
            {
                var group = Groups.FirstOrDefault(g => g.Id == groupId);
                if (group == null) return Task.FromResult(false);
                group.IsDeleted = true;
                group.UpdatedAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }

            public Task<bool> HasFinancialRecordsAsync(Guid groupId) => Task.FromResult(false);

            public Task<User?> GetUserByEmailOrIdAsync(string? email, Guid? userId)
            {
                var user = Users.FirstOrDefault(u =>
                    (!string.IsNullOrEmpty(email) && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                    (userId.HasValue && u.Id == userId.Value));
                return Task.FromResult(user);
            }

            public Task<SplitGroupInvitation?> GetInvitationByIdAsync(Guid invitationId) =>
                Task.FromResult(Invitations.FirstOrDefault(i => i.Id == invitationId));

            public Task<SplitGroupInvitation?> GetPendingInvitationByEmailAsync(Guid groupId, string email) =>
                Task.FromResult(Invitations.FirstOrDefault(i =>
                    i.GroupId == groupId &&
                    i.Status == "Pending" &&
                    i.InvitedEmail.Equals(email, StringComparison.OrdinalIgnoreCase)));

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId)
            {
                var list = Invitations.Where(i => i.GroupId == groupId && i.Status == "Pending").Select(i => new SplitGroupInvitationDto
                {
                    Id = i.Id,
                    GroupId = i.GroupId,
                    InvitedUserId = i.InvitedUserId,
                    InvitedEmail = i.InvitedEmail,
                    Status = i.Status,
                    ExpiresAt = i.ExpiresAt,
                    CreatedAt = i.CreatedAt
                }).ToList();
                return Task.FromResult(list);
            }

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByUserAsync(Guid userId, string userEmail)
            {
                var list = Invitations.Where(i =>
                    (i.InvitedUserId == userId || i.InvitedEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase)) &&
                    i.Status == "Pending" && i.ExpiresAt > DateTime.UtcNow).Select(i => new SplitGroupInvitationDto
                {
                    Id = i.Id,
                    GroupId = i.GroupId,
                    InvitedUserId = i.InvitedUserId,
                    InvitedEmail = i.InvitedEmail,
                    Status = i.Status,
                    ExpiresAt = i.ExpiresAt,
                    CreatedAt = i.CreatedAt
                }).ToList();
                return Task.FromResult(list);
            }

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

            public Task<bool> RevokeInvitationAsync(Guid invitationId, Guid revokedByUserId, string revokerName, Guid groupId)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite == null) return Task.FromResult(false);
                invite.Status = "Revoked";
                return Task.FromResult(true);
            }

            public Task<bool> AcceptInvitationAsync(Guid invitationId, Guid userId, string userName)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite == null || invite.Status != "Pending" || invite.ExpiresAt <= DateTime.UtcNow)
                    return Task.FromResult(false);

                invite.Status = "Accepted";
                invite.RespondedAt = DateTime.UtcNow;

                var existingMember = Members.FirstOrDefault(m => m.GroupId == invite.GroupId && m.UserId == userId);
                if (existingMember != null)
                {
                    existingMember.IsActive = true;
                }
                else
                {
                    Members.Add(new SplitGroupMember
                    {
                        Id = Guid.NewGuid(),
                        GroupId = invite.GroupId,
                        UserId = userId,
                        Role = "Member",
                        IsActive = true,
                        JoinedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                return Task.FromResult(true);
            }

            public Task<bool> RejectInvitationAsync(Guid invitationId, Guid userId, string userName)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite == null || invite.Status != "Pending") return Task.FromResult(false);
                invite.Status = "Rejected";
                invite.RespondedAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }

            public Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName)
            {
                var member = Members.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userIdToRemove);
                if (member == null) return Task.FromResult(false);
                member.IsActive = false;
                member.UpdatedAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }

            public Task<List<SplitGroupActivityDto>> GetGroupActivitiesAsync(Guid groupId, int limit = 50) =>
                Task.FromResult(Activities.Where(a => a.GroupId == groupId).Select(a => new SplitGroupActivityDto
                {
                    Id = a.Id,
                    GroupId = a.GroupId,
                    UserId = a.UserId,
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                }).ToList());

            public Task<PagedGroupActivityResultDto> GetPagedGroupActivitiesAsync(Guid groupId, SplitGroupActivityQueryParameters parameters) =>
                Task.FromResult(new PagedGroupActivityResultDto());

            public Task<SplitGroupActivityDto?> GetActivityByIdAsync(Guid groupId, Guid activityId) =>
                Task.FromResult<SplitGroupActivityDto?>(null);

            public Task LogActivityAsync(Guid groupId, Guid userId, string activityType, string description, Guid? referenceId = null)
            {
                Activities.Add(new SplitGroupActivity
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = userId,
                    ActivityType = activityType,
                    Description = description,
                    ReferenceId = referenceId,
                    CreatedAt = DateTime.UtcNow
                });
                return Task.CompletedTask;
            }
        }

        private class InMemoryExpenseRepository : ISplitExpenseRepository
        {
            public List<SplitExpense> Expenses { get; } = new();
            public List<SplitExpenseShare> Shares { get; } = new();

            public Task<SplitExpense?> GetExpenseByIdAsync(Guid expenseId) =>
                Task.FromResult(Expenses.FirstOrDefault(e => e.Id == expenseId && !e.IsDeleted));

            public Task<SplitExpenseDetailDto?> GetExpenseDetailsAsync(Guid expenseId, Guid groupId)
            {
                var expense = Expenses.FirstOrDefault(e => e.Id == expenseId && e.GroupId == groupId && !e.IsDeleted);
                if (expense == null) return Task.FromResult<SplitExpenseDetailDto?>(null);

                var shares = Shares.Where(s => s.ExpenseId == expenseId).Select(s => new SplitExpenseShareItemDto
                {
                    UserId = s.UserId,
                    ShareAmount = s.ShareAmount,
                    IsPayer = s.UserId == expense.PayerId
                }).ToList();

                return Task.FromResult<SplitExpenseDetailDto?>(new SplitExpenseDetailDto
                {
                    Id = expense.Id,
                    GroupId = expense.GroupId,
                    PayerId = expense.PayerId,
                    Amount = expense.Amount,
                    Title = expense.Title,
                    Description = expense.Description,
                    ExpenseDate = expense.ExpenseDate,
                    Shares = shares
                });
            }

            public Task<PagedExpenseResultDto> GetExpensesListAsync(Guid groupId, Guid currentUserId, SplitExpenseQueryParameters parameters) =>
                Task.FromResult(new PagedExpenseResultDto());

            public Task<SplitExpenseDetailDto> CreateExpenseWithSharesAndActivityAsync(
                SplitExpense expense, List<SplitExpenseShare> shares, string creatorName, string groupName)
            {
                Expenses.Add(expense);
                Shares.AddRange(shares);
                return Task.FromResult(new SplitExpenseDetailDto
                {
                    Id = expense.Id,
                    GroupId = expense.GroupId,
                    PayerId = expense.PayerId,
                    Amount = expense.Amount,
                    Title = expense.Title,
                    Description = expense.Description,
                    ExpenseDate = expense.ExpenseDate,
                    Shares = shares.Select(s => new SplitExpenseShareItemDto
                    {
                        UserId = s.UserId,
                        ShareAmount = s.ShareAmount,
                        IsPayer = s.UserId == expense.PayerId
                    }).ToList()
                });
            }

            public Task<SplitExpenseDetailDto?> UpdateExpenseWithSharesAndActivityAsync(
                SplitExpense expense, List<SplitExpenseShare> newShares, string editorName, string groupName)
            {
                var existing = Expenses.FirstOrDefault(e => e.Id == expense.Id);
                if (existing == null) return Task.FromResult<SplitExpenseDetailDto?>(null);

                existing.Amount = expense.Amount;
                existing.Title = expense.Title;
                existing.Description = expense.Description;
                existing.ExpenseDate = expense.ExpenseDate;
                existing.UpdatedAt = DateTime.UtcNow;

                Shares.RemoveAll(s => s.ExpenseId == expense.Id);
                Shares.AddRange(newShares);

                return Task.FromResult<SplitExpenseDetailDto?>(new SplitExpenseDetailDto
                {
                    Id = existing.Id,
                    GroupId = existing.GroupId,
                    PayerId = existing.PayerId,
                    Amount = existing.Amount,
                    Title = existing.Title,
                    Description = existing.Description,
                    ExpenseDate = existing.ExpenseDate,
                    Shares = newShares.Select(s => new SplitExpenseShareItemDto
                    {
                        UserId = s.UserId,
                        ShareAmount = s.ShareAmount,
                        IsPayer = s.UserId == existing.PayerId
                    }).ToList()
                });
            }

            public Task<bool> SoftDeleteExpenseWithActivityAsync(
                Guid expenseId, Guid groupId, Guid deletedByUserId, string deletedByName)
            {
                var expense = Expenses.FirstOrDefault(e => e.Id == expenseId && e.GroupId == groupId);
                if (expense == null) return Task.FromResult(false);
                expense.IsDeleted = true;
                expense.UpdatedAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }

            public Task<List<SplitExpenseShare>> GetExpenseSharesAsync(Guid expenseId) =>
                Task.FromResult(Shares.Where(s => s.ExpenseId == expenseId).ToList());
        }

        private class InMemorySettlementRepository : ISplitSettlementRepository
        {
            public List<SplitSettlement> Settlements { get; } = new();

            public Task<SplitSettlement?> GetSettlementByIdAsync(Guid settlementId) =>
                Task.FromResult(Settlements.FirstOrDefault(s => s.Id == settlementId && !s.IsDeleted));

            public Task<SplitSettlementDto?> GetSettlementDetailsAsync(Guid settlementId, Guid groupId)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.GroupId == groupId && !x.IsDeleted);
                if (s == null) return Task.FromResult<SplitSettlementDto?>(null);
                return Task.FromResult<SplitSettlementDto?>(new SplitSettlementDto
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    FromUserId = s.FromUserId,
                    ToUserId = s.ToUserId,
                    Amount = s.Amount,
                    Note = s.Note,
                    SettlementDate = s.SettlementDate
                });
            }

            public Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId) =>
                Task.FromResult(Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted).Select(s => new SplitSettlementDto
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    FromUserId = s.FromUserId,
                    ToUserId = s.ToUserId,
                    Amount = s.Amount,
                    Note = s.Note,
                    SettlementDate = s.SettlementDate
                }).ToList());

            public Task<SplitSettlementDto> CreateSettlementWithActivityAsync(
                SplitSettlement settlement, string creatorName, string groupName)
            {
                Settlements.Add(settlement);
                return Task.FromResult(new SplitSettlementDto
                {
                    Id = settlement.Id,
                    GroupId = settlement.GroupId,
                    FromUserId = settlement.FromUserId,
                    ToUserId = settlement.ToUserId,
                    Amount = settlement.Amount,
                    Note = settlement.Note,
                    SettlementDate = settlement.SettlementDate
                });
            }

            public Task<SplitSettlementDto?> UpdateSettlementWithActivityAsync(
                SplitSettlement settlement, string editorName, string groupName) =>
                Task.FromResult<SplitSettlementDto?>(null);

            public Task<bool> SoftDeleteSettlementWithActivityAsync(
                Guid settlementId, Guid groupId, Guid deletedByUserId, string deletedByName)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.GroupId == groupId);
                if (s == null) return Task.FromResult(false);
                s.IsDeleted = true;
                return Task.FromResult(true);
            }
        }

        private class InMemoryBalanceRepository : ISplitBalanceRepository
        {
            private readonly InMemoryGroupRepository _groupRepo;
            private readonly InMemoryExpenseRepository _expenseRepo;
            private readonly InMemorySettlementRepository _settlementRepo;

            public InMemoryBalanceRepository(
                InMemoryGroupRepository groupRepo,
                InMemoryExpenseRepository expenseRepo,
                InMemorySettlementRepository settlementRepo)
            {
                _groupRepo = groupRepo;
                _expenseRepo = expenseRepo;
                _settlementRepo = settlementRepo;
            }

            public Task<List<MemberBalanceDto>> GetGroupBalancesAsync(Guid groupId)
            {
                var members = _groupRepo.Members.Where(m => m.GroupId == groupId).Select(m =>
                {
                    var user = _groupRepo.Users.FirstOrDefault(u => u.Id == m.UserId);
                    return new MemberRecord(
                        m.UserId,
                        user is { } u ? $"{u.FirstName} {u.LastName}".Trim() : "Member",
                        user?.Email ?? "member@spendwise.com",
                        m.Role,
                        m.IsActive
                    );
                }).ToList();

                var expenses = _expenseRepo.Expenses.Where(e => e.GroupId == groupId).Select(e =>
                {
                    var shares = _expenseRepo.Shares.Where(s => s.ExpenseId == e.Id)
                        .Select(s => new ShareRecord(s.UserId, s.ShareAmount)).ToList();
                    return new ExpenseRecord(e.Id, e.PayerId, e.Amount, e.IsDeleted, shares);
                }).ToList();

                var settlements = _settlementRepo.Settlements.Where(s => s.GroupId == groupId).Select(s =>
                    new SettlementRecord(s.Id, s.FromUserId, s.ToUserId, s.Amount, s.IsDeleted)).ToList();

                var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, settlements);
                return Task.FromResult(balances);
            }

            public async Task<MemberBalanceDto?> GetMemberBalanceAsync(Guid groupId, Guid userId)
            {
                var balances = await GetGroupBalancesAsync(groupId);
                return balances.FirstOrDefault(b => b.UserId == userId);
            }

            public async Task<GroupFinancialSummaryDto?> GetGroupFinancialSummaryAsync(Guid groupId, Guid currentUserId)
            {
                var balances = await GetGroupBalancesAsync(groupId);
                var activeExpenses = _expenseRepo.Expenses.Where(e => e.GroupId == groupId && !e.IsDeleted).ToList();
                var currentBalance = balances.FirstOrDefault(b => b.UserId == currentUserId);

                return new GroupFinancialSummaryDto
                {
                    GroupId = groupId,
                    TotalExpenses = activeExpenses.Sum(e => e.Amount),
                    TotalSettlements = _settlementRepo.Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted).Sum(s => s.Amount),
                    MemberCount = _groupRepo.Members.Count(m => m.GroupId == groupId && m.IsActive),
                    CurrentUserBalance = currentBalance?.NetBalance ?? 0m,
                    CurrentUserTotalPaid = currentBalance?.TotalPaid ?? 0m,
                    CurrentUserTotalShare = currentBalance?.TotalShare ?? 0m,
                    CurrentUserStatusText = currentBalance?.StatusDescription ?? "Settled up"
                };
            }
        }

        #endregion

        #region 1. Group Tests

        [Fact]
        public async Task GroupTest_01_CreateGroup_Success()
        {
            var groupRepo = new InMemoryGroupRepository();
            var service = new SplitGroupService(groupRepo);
            var ownerId = Guid.NewGuid();

            var dto = new CreateSplitGroupDto
            {
                Name = "Road Trip to Murree",
                Description = "Weekend getaway expenses",
                Icon = "car"
            };

            var result = await service.CreateGroupAsync(dto, ownerId, "Ali Khan");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Road Trip to Murree", result.Data.Name);
            Assert.Equal(ownerId, result.Data.OwnerId);
            Assert.Single(groupRepo.Groups);
        }

        [Fact]
        public async Task GroupTest_02_AutomaticallyAssignsOwnerMembership()
        {
            var groupRepo = new InMemoryGroupRepository();
            var service = new SplitGroupService(groupRepo);
            var ownerId = Guid.NewGuid();

            var dto = new CreateSplitGroupDto { Name = "Flat 402 Utilities" };
            var result = await service.CreateGroupAsync(dto, ownerId, "Bilal Tariq");

            Assert.True(result.Success);
            var membership = groupRepo.Members.FirstOrDefault(m => m.GroupId == result.Data!.Id && m.UserId == ownerId);

            Assert.NotNull(membership);
            Assert.Equal("Owner", membership.Role);
            Assert.True(membership.IsActive);
        }

        [Fact]
        public async Task GroupTest_03_InviteRegisteredUser_Success()
        {
            var groupRepo = new InMemoryGroupRepository();
            var service = new SplitGroupService(groupRepo);

            var ownerId = Guid.NewGuid();
            var inviteeId = Guid.NewGuid();
            var inviteeEmail = "usman@spendwise.com";

            groupRepo.Users.Add(new User { Id = inviteeId, Email = inviteeEmail, FirstName = "Usman", LastName = "Ali" });

            var createResult = await service.CreateGroupAsync(new CreateSplitGroupDto { Name = "Dinner Club" }, ownerId, "Owner");
            var groupId = createResult.Data!.Id;

            var inviteDto = new InviteSplitGroupMemberDto { InvitedEmail = inviteeEmail };
            var inviteResult = await service.InviteMemberAsync(groupId, inviteDto, ownerId, "Owner");

            Assert.True(inviteResult.Success);
            Assert.NotNull(inviteResult.Data);
            Assert.Equal("Pending", inviteResult.Data.Status);
            Assert.Equal(inviteeEmail, inviteResult.Data.InvitedEmail);
            Assert.Single(groupRepo.Invitations);
        }

        [Fact]
        public async Task GroupTest_04_AcceptInvitation_Success()
        {
            var groupRepo = new InMemoryGroupRepository();
            var service = new SplitGroupService(groupRepo);

            var ownerId = Guid.NewGuid();
            var inviteeId = Guid.NewGuid();
            var inviteeEmail = "hassan@spendwise.com";

            groupRepo.Users.Add(new User { Id = inviteeId, Email = inviteeEmail, FirstName = "Hassan", LastName = "Raza" });

            var group = await service.CreateGroupAsync(new CreateSplitGroupDto { Name = "Project Team" }, ownerId, "Owner");
            var groupId = group.Data!.Id;

            var inviteResult = await service.InviteMemberAsync(groupId, new InviteSplitGroupMemberDto { InvitedEmail = inviteeEmail }, ownerId, "Owner");
            var invitationId = inviteResult.Data!.Id;

            var acceptResult = await service.AcceptInvitationAsync(invitationId, inviteeId, inviteeEmail, "Hassan Raza");

            Assert.True(acceptResult.Success);
            var invitation = groupRepo.Invitations.First(i => i.Id == invitationId);
            Assert.Equal("Accepted", invitation.Status);

            var member = groupRepo.Members.FirstOrDefault(m => m.GroupId == groupId && m.UserId == inviteeId);
            Assert.NotNull(member);
            Assert.True(member.IsActive);
            Assert.Equal("Member", member.Role);
        }

        [Fact]
        public async Task GroupTest_05_RejectInvitation_Success()
        {
            var groupRepo = new InMemoryGroupRepository();
            var service = new SplitGroupService(groupRepo);

            var ownerId = Guid.NewGuid();
            var inviteeId = Guid.NewGuid();
            var inviteeEmail = "zain@spendwise.com";

            groupRepo.Users.Add(new User { Id = inviteeId, Email = inviteeEmail, FirstName = "Zain", LastName = "Abid" });

            var group = await service.CreateGroupAsync(new CreateSplitGroupDto { Name = "Badminton League" }, ownerId, "Owner");
            var invite = await service.InviteMemberAsync(group.Data!.Id, new InviteSplitGroupMemberDto { InvitedEmail = inviteeEmail }, ownerId, "Owner");

            var rejectResult = await service.RejectInvitationAsync(invite.Data!.Id, inviteeId, inviteeEmail, "Zain Abid");

            Assert.True(rejectResult.Success);
            var invitation = groupRepo.Invitations.First(i => i.Id == invite.Data.Id);
            Assert.Equal("Rejected", invitation.Status);

            var member = groupRepo.Members.FirstOrDefault(m => m.GroupId == group.Data.Id && m.UserId == inviteeId);
            Assert.Null(member);
        }

        [Fact]
        public async Task GroupTest_06_RemoveMember_Success()
        {
            var groupRepo = new InMemoryGroupRepository();
            var service = new SplitGroupService(groupRepo);

            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var group = await service.CreateGroupAsync(new CreateSplitGroupDto { Name = "Roommates" }, ownerId, "Owner");
            var groupId = group.Data!.Id;

            groupRepo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = memberId, Role = "Member", IsActive = true });

            var removeResult = await service.RemoveMemberAsync(groupId, memberId, ownerId, "Owner");

            Assert.True(removeResult.Success);
            var member = groupRepo.Members.First(m => m.GroupId == groupId && m.UserId == memberId);
            Assert.False(member.IsActive);
        }

        [Fact]
        public async Task GroupTest_07_PreventDuplicateMembership()
        {
            var groupRepo = new InMemoryGroupRepository();
            var service = new SplitGroupService(groupRepo);

            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var email = "existing@spendwise.com";

            groupRepo.Users.Add(new User { Id = memberId, Email = email, FirstName = "Existing", LastName = "User" });
            var group = await service.CreateGroupAsync(new CreateSplitGroupDto { Name = "Gaming Squad" }, ownerId, "Owner");
            var groupId = group.Data!.Id;

            groupRepo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = memberId, Role = "Member", IsActive = true });

            var inviteResult = await service.InviteMemberAsync(groupId, new InviteSplitGroupMemberDto { InvitedEmail = email }, ownerId, "Owner");

            Assert.False(inviteResult.Success);
            Assert.Contains("already an active member", inviteResult.Message);
        }

        [Fact]
        public async Task GroupTest_08_PreventUnauthorizedGroupUpdates()
        {
            var groupRepo = new InMemoryGroupRepository();
            var service = new SplitGroupService(groupRepo);

            var ownerId = Guid.NewGuid();
            var nonOwnerId = Guid.NewGuid();

            var group = await service.CreateGroupAsync(new CreateSplitGroupDto { Name = "Original Name" }, ownerId, "Owner");
            var groupId = group.Data!.Id;

            groupRepo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = nonOwnerId, Role = "Member", IsActive = true });

            var updateResult = await service.UpdateGroupAsync(groupId, new UpdateSplitGroupDto { Name = "Hacked Name" }, nonOwnerId, "NonOwner");

            Assert.False(updateResult.Success);
            Assert.Contains("Only the group owner", updateResult.Message);
            Assert.Equal("Original Name", groupRepo.Groups.First(g => g.Id == groupId).Name);
        }

        #endregion

        #region 2. Expense Tests

        [Fact]
        public async Task ExpenseTest_01_AddExpense_Success()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var aliId = Guid.NewGuid();
            var ahmedId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Lunch Group", OwnerId = aliId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = aliId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ahmedId, Role = "Member", IsActive = true });

            var dto = new CreateSplitExpenseDto
            {
                Title = "Biryani Lunch",
                Amount = 1500m,
                Description = "Biryani Lunch with friends",
                ExpenseDate = DateTime.UtcNow,
                PayerId = aliId,
                ParticipantUserIds = new List<Guid> { aliId, ahmedId }
            };

            var result = await service.AddExpenseAsync(groupId, dto, aliId, "Ali");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(1500m, result.Data.Amount);
            Assert.Equal(2, result.Data.Shares.Count);
            Assert.Equal(750m, result.Data.Shares[0].ShareAmount);
            Assert.Equal(750m, result.Data.Shares[1].ShareAmount);
        }

        [Fact]
        public async Task ExpenseTest_02_SplitAmongTwoMembers()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var u1 = Guid.NewGuid();
            var u2 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Duo", OwnerId = u1, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u1, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u2, Role = "Member", IsActive = true });

            var dto = new CreateSplitExpenseDto
            {
                Title = "Groceries",
                Amount = 500m,
                Description = "Groceries split",
                ExpenseDate = DateTime.UtcNow,
                PayerId = u1,
                ParticipantUserIds = new List<Guid> { u1, u2 }
            };

            var result = await service.AddExpenseAsync(groupId, dto, u1, "User1");

            Assert.True(result.Success);
            Assert.All(result.Data!.Shares, s => Assert.Equal(250m, s.ShareAmount));
            Assert.Equal(500m, result.Data.Shares.Sum(s => s.ShareAmount));
        }

        [Fact]
        public async Task ExpenseTest_03_SplitAmongThreeMembers()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var u1 = Guid.NewGuid();
            var u2 = Guid.NewGuid();
            var u3 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trio", OwnerId = u1, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u1, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u2, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u3, Role = "Member", IsActive = true });

            var dto = new CreateSplitExpenseDto
            {
                Title = "Snacks",
                Amount = 100m,
                Description = "Snacks split",
                ExpenseDate = DateTime.UtcNow,
                PayerId = u1,
                ParticipantUserIds = new List<Guid> { u1, u2, u3 }
            };

            var result = await service.AddExpenseAsync(groupId, dto, u1, "User1");

            Assert.True(result.Success);
            Assert.Equal(3, result.Data!.Shares.Count);
            Assert.Equal(100m, result.Data.Shares.Sum(s => s.ShareAmount));
            // Deterministic distribution: 33.34 for 1st participant, 33.33 for remaining two
            Assert.Contains(result.Data.Shares, s => s.ShareAmount == 33.34m);
            Assert.Equal(2, result.Data.Shares.Count(s => s.ShareAmount == 33.33m));
        }

        [Fact]
        public async Task ExpenseTest_04_SplitAmongAllGroupMembers()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var members = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Quintet", OwnerId = members[0], IsActive = true });
            foreach (var m in members)
            {
                groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = m, Role = "Member", IsActive = true });
            }

            var dto = new CreateSplitExpenseDto
            {
                Title = "Cabin rental",
                Amount = 1000m,
                Description = "Cabin rental",
                ExpenseDate = DateTime.UtcNow,
                PayerId = members[0],
                ParticipantUserIds = members
            };

            var result = await service.AddExpenseAsync(groupId, dto, members[0], "User1");

            Assert.True(result.Success);
            Assert.Equal(5, result.Data!.Shares.Count);
            Assert.All(result.Data.Shares, s => Assert.Equal(200m, s.ShareAmount));
        }

        [Fact]
        public async Task ExpenseTest_05_ExcludePayer_PayerGetsFullCreditWithoutShare()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var payer = Guid.NewGuid();
            var part1 = Guid.NewGuid();
            var part2 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Gift Group", OwnerId = payer, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payer, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = part1, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = part2, Role = "Member", IsActive = true });

            var dto = new CreateSplitExpenseDto
            {
                Title = "Gift for Friends",
                Amount = 300m,
                Description = "Gift for Friends",
                ExpenseDate = DateTime.UtcNow,
                PayerId = payer,
                // Payer excluded from participants
                ParticipantUserIds = new List<Guid> { part1, part2 }
            };

            var result = await service.AddExpenseAsync(groupId, dto, payer, "Payer");

            Assert.True(result.Success);
            Assert.DoesNotContain(result.Data!.Shares, s => s.UserId == payer);
            Assert.Equal(2, result.Data.Shares.Count);
            Assert.All(result.Data.Shares, s => Assert.Equal(150m, s.ShareAmount));
        }

        [Fact]
        public async Task ExpenseTest_06_SelectOnlyPayer_PayerShareEqualsTotal()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var payer = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Solo Payer", OwnerId = payer, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payer, Role = "Owner", IsActive = true });

            var dto = new CreateSplitExpenseDto
            {
                Title = "Individual Item",
                Amount = 250m,
                Description = "Individual Item",
                ExpenseDate = DateTime.UtcNow,
                PayerId = payer,
                ParticipantUserIds = new List<Guid> { payer }
            };

            var result = await service.AddExpenseAsync(groupId, dto, payer, "Payer");

            Assert.True(result.Success);
            Assert.Single(result.Data!.Shares);
            Assert.Equal(250m, result.Data.Shares[0].ShareAmount);
            Assert.Equal(payer, result.Data.Shares[0].UserId);
        }

        [Fact]
        public async Task ExpenseTest_07_EditExpenseAmount()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var u1 = Guid.NewGuid();
            var u2 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Group", OwnerId = u1, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u1, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u2, Role = "Member", IsActive = true });

            var addResult = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Initial amount",
                Amount = 100m,
                Description = "Initial amount",
                ExpenseDate = DateTime.UtcNow,
                PayerId = u1,
                ParticipantUserIds = new List<Guid> { u1, u2 }
            }, u1, "User1");

            var expenseId = addResult.Data!.Id;

            var editResult = await service.EditExpenseAsync(groupId, expenseId, new UpdateSplitExpenseDto
            {
                Title = "Updated amount",
                Amount = 300m,
                Description = "Updated amount",
                ExpenseDate = DateTime.UtcNow,
                PayerId = u1,
                ParticipantUserIds = new List<Guid> { u1, u2 }
            }, u1, "User1");

            Assert.True(editResult.Success);
            Assert.Equal(300m, editResult.Data!.Amount);
            Assert.All(editResult.Data.Shares, s => Assert.Equal(150m, s.ShareAmount));
        }

        [Fact]
        public async Task ExpenseTest_08_ChangeSelectedParticipants()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var u1 = Guid.NewGuid();
            var u2 = Guid.NewGuid();
            var u3 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Group", OwnerId = u1, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u1, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u2, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u3, Role = "Member", IsActive = true });

            var addResult = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Dinner",
                Amount = 300m,
                Description = "Dinner",
                ExpenseDate = DateTime.UtcNow,
                PayerId = u1,
                ParticipantUserIds = new List<Guid> { u1, u2 } // initially 2
            }, u1, "User1");

            var expenseId = addResult.Data!.Id;

            // Change to 3 participants
            var editResult = await service.EditExpenseAsync(groupId, expenseId, new UpdateSplitExpenseDto
            {
                Title = "Dinner with 3",
                Amount = 300m,
                Description = "Dinner with 3",
                ExpenseDate = DateTime.UtcNow,
                PayerId = u1,
                ParticipantUserIds = new List<Guid> { u1, u2, u3 }
            }, u1, "User1");

            Assert.True(editResult.Success);
            Assert.Equal(3, editResult.Data!.Shares.Count);
            Assert.All(editResult.Data.Shares, s => Assert.Equal(100m, s.ShareAmount));
        }

        [Fact]
        public async Task ExpenseTest_09_DeleteExpense_SoftDeletesRecord()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var u1 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Group", OwnerId = u1, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = u1, Role = "Owner", IsActive = true });

            var addResult = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "To be deleted",
                Amount = 100m,
                Description = "To be deleted",
                ExpenseDate = DateTime.UtcNow,
                PayerId = u1,
                ParticipantUserIds = new List<Guid> { u1 }
            }, u1, "User1");

            var expenseId = addResult.Data!.Id;
            var deleteResult = await service.DeleteExpenseAsync(groupId, expenseId, u1, "User1");

            Assert.True(deleteResult.Success);
            var storedExpense = expenseRepo.Expenses.First(e => e.Id == expenseId);
            Assert.True(storedExpense.IsDeleted);
        }

        [Fact]
        public async Task ExpenseTest_10_VerifyBalancesUpdateCorrectlyAcrossOperations()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var settlementRepo = new InMemorySettlementRepository();
            var balanceRepo = new InMemoryBalanceRepository(groupRepo, expenseRepo, settlementRepo);
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var ali = Guid.NewGuid();
            var ahmed = Guid.NewGuid();
            var bilal = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ali, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ali, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ahmed, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = bilal, Role = "Member", IsActive = true });

            // 1. Initial: Zero balances
            var initialBalances = await balanceRepo.GetGroupBalancesAsync(groupId);
            Assert.All(initialBalances, b => Assert.Equal(0m, b.NetBalance));

            // 2. Add expense: Ali pays 3,000 for Ali, Ahmed, Bilal (1,000 each)
            var addResult = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Hotel",
                Amount = 3000m,
                Description = "Hotel",
                ExpenseDate = DateTime.UtcNow,
                PayerId = ali,
                ParticipantUserIds = new List<Guid> { ali, ahmed, bilal }
            }, ali, "Ali");
            var expenseId = addResult.Data!.Id;

            var balancesAfterAdd = await balanceRepo.GetGroupBalancesAsync(groupId);
            Assert.Equal(2000m, balancesAfterAdd.First(b => b.UserId == ali).NetBalance);
            Assert.Equal(-1000m, balancesAfterAdd.First(b => b.UserId == ahmed).NetBalance);
            Assert.Equal(-1000m, balancesAfterAdd.First(b => b.UserId == bilal).NetBalance);

            // 3. Edit expense: Amount increased to 6,000 (2,000 each)
            await service.EditExpenseAsync(groupId, expenseId, new UpdateSplitExpenseDto
            {
                Title = "Luxury Hotel",
                Amount = 6000m,
                Description = "Luxury Hotel",
                ExpenseDate = DateTime.UtcNow,
                PayerId = ali,
                ParticipantUserIds = new List<Guid> { ali, ahmed, bilal }
            }, ali, "Ali");

            var balancesAfterEdit = await balanceRepo.GetGroupBalancesAsync(groupId);
            Assert.Equal(4000m, balancesAfterEdit.First(b => b.UserId == ali).NetBalance);
            Assert.Equal(-2000m, balancesAfterEdit.First(b => b.UserId == ahmed).NetBalance);
            Assert.Equal(-2000m, balancesAfterEdit.First(b => b.UserId == bilal).NetBalance);

            // 4. Delete expense: Balances return to zero
            await service.DeleteExpenseAsync(groupId, expenseId, ali, "Ali");

            var balancesAfterDelete = await balanceRepo.GetGroupBalancesAsync(groupId);
            Assert.All(balancesAfterDelete, b => Assert.Equal(0m, b.NetBalance));
        }

        #endregion

        #region 3. Rounding Tests

        [Fact]
        public void RoundingTest_100DividedBy3_SumEqualsTotal()
        {
            var calculator = new EqualSplitCalculatorService();
            var u1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var u2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var u3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var participants = new List<Guid> { u1, u2, u3 };

            var shares = calculator.CalculateEqualSplit(100m, participants);

            Assert.Equal(3, shares.Count);
            Assert.Equal(100m, shares.Values.Sum());
            Assert.Equal(33.34m, shares[u1]);
            Assert.Equal(33.33m, shares[u2]);
            Assert.Equal(33.33m, shares[u3]);
        }

        [Fact]
        public void RoundingTest_100DividedBy6_SumEqualsTotal()
        {
            var calculator = new EqualSplitCalculatorService();
            var participants = Enumerable.Range(1, 6).Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}")).ToList();

            var shares = calculator.CalculateEqualSplit(100m, participants);

            Assert.Equal(6, shares.Count);
            Assert.Equal(100m, shares.Values.Sum());
            // 100 / 6 = 16.66 with remainder 0.04 -> first 4 get 16.67, remaining 2 get 16.66
            Assert.Equal(4, shares.Values.Count(v => v == 16.67m));
            Assert.Equal(2, shares.Values.Count(v => v == 16.66m));
        }

        [Fact]
        public void RoundingTest_10DividedBy3_SumEqualsTotal()
        {
            var calculator = new EqualSplitCalculatorService();
            var u1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var u2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var u3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var participants = new List<Guid> { u1, u2, u3 };

            var shares = calculator.CalculateEqualSplit(10m, participants);

            Assert.Equal(3, shares.Count);
            Assert.Equal(10m, shares.Values.Sum());
            Assert.Equal(3.34m, shares[u1]);
            Assert.Equal(3.33m, shares[u2]);
            Assert.Equal(3.33m, shares[u3]);
        }

        [Fact]
        public void RoundingTest_OneCentDividedBy3_SumEqualsTotal()
        {
            var calculator = new EqualSplitCalculatorService();
            var u1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var u2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var u3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var participants = new List<Guid> { u1, u2, u3 };

            var shares = calculator.CalculateEqualSplit(0.01m, participants);

            Assert.Equal(3, shares.Count);
            Assert.Equal(0.01m, shares.Values.Sum());
            Assert.Equal(0.01m, shares[u1]);
            Assert.Equal(0.00m, shares[u2]);
            Assert.Equal(0.00m, shares[u3]);
        }

        [Fact]
        public void RoundingTest_LargeExpenseAmount_SumEqualsTotal()
        {
            var calculator = new EqualSplitCalculatorService();
            var participants = Enumerable.Range(0, 7).Select(_ => Guid.NewGuid()).ToList();
            const decimal largeAmount = 10_000_000.00m;

            var shares = calculator.CalculateEqualSplit(largeAmount, participants);

            Assert.Equal(7, shares.Count);
            Assert.Equal(largeAmount, shares.Values.Sum());
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(11)]
        [InlineData(13)]
        [InlineData(19)]
        public void RoundingTest_DifferentParticipantCounts_SumAlwaysEqualsOriginalExpenseAmount(int count)
        {
            var calculator = new EqualSplitCalculatorService();
            var participants = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
            var amounts = new[] { 50m, 100m, 123.45m, 999.99m, 10000.01m };

            foreach (var amount in amounts)
            {
                var shares = calculator.CalculateEqualSplit(amount, participants);
                Assert.Equal(amount, shares.Values.Sum());
            }
        }

        #endregion

        #region 4. Balance Tests

        [Fact]
        public void BalanceTest_OneMemberPaysForEveryone()
        {
            var ali = Guid.NewGuid();
            var ahmed = Guid.NewGuid();
            var bilal = Guid.NewGuid();

            var members = new List<MemberRecord>
            {
                new(ali, "Ali", "ali@test.com", "Owner", true),
                new(ahmed, "Ahmed", "ahmed@test.com", "Member", true),
                new(bilal, "Bilal", "bilal@test.com", "Member", true)
            };

            var expenses = new List<ExpenseRecord>
            {
                new(Guid.NewGuid(), ali, 3000m, false, new List<ShareRecord>
                {
                    new(ali, 1000m),
                    new(ahmed, 1000m),
                    new(bilal, 1000m)
                })
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            Assert.Equal(2000m, balances.First(b => b.UserId == ali).NetBalance);
            Assert.Equal(-1000m, balances.First(b => b.UserId == ahmed).NetBalance);
            Assert.Equal(-1000m, balances.First(b => b.UserId == bilal).NetBalance);
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void BalanceTest_MultipleMembersPaySeparateExpenses()
        {
            var ali = Guid.NewGuid();
            var ahmed = Guid.NewGuid();

            var members = new List<MemberRecord>
            {
                new(ali, "Ali", "ali@test.com", "Owner", true),
                new(ahmed, "Ahmed", "ahmed@test.com", "Member", true)
            };

            var expenses = new List<ExpenseRecord>
            {
                // Ali pays 1000 (500 each)
                new(Guid.NewGuid(), ali, 1000m, false, new List<ShareRecord> { new(ali, 500m), new(ahmed, 500m) }),
                // Ahmed pays 600 (300 each)
                new(Guid.NewGuid(), ahmed, 600m, false, new List<ShareRecord> { new(ali, 300m), new(ahmed, 300m) })
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            // Ali: paid 1000, share 800 -> +200
            // Ahmed: paid 600, share 800 -> -200
            Assert.Equal(200m, balances.First(b => b.UserId == ali).NetBalance);
            Assert.Equal(-200m, balances.First(b => b.UserId == ahmed).NetBalance);
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void BalanceTest_PayerExcludedFromExpense()
        {
            var payer = Guid.NewGuid();
            var consumer = Guid.NewGuid();

            var members = new List<MemberRecord>
            {
                new(payer, "Payer", "payer@test.com", "Owner", true),
                new(consumer, "Consumer", "consumer@test.com", "Member", true)
            };

            var expenses = new List<ExpenseRecord>
            {
                new(Guid.NewGuid(), payer, 500m, false, new List<ShareRecord> { new(consumer, 500m) })
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            Assert.Equal(500m, balances.First(b => b.UserId == payer).NetBalance);
            Assert.Equal(-500m, balances.First(b => b.UserId == consumer).NetBalance);
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void BalanceTest_MemberParticipatesWithoutPaying()
        {
            var payer = Guid.NewGuid();
            var freeRider = Guid.NewGuid();

            var members = new List<MemberRecord>
            {
                new(payer, "Payer", "payer@test.com", "Owner", true),
                new(freeRider, "FreeRider", "rider@test.com", "Member", true)
            };

            var expenses = new List<ExpenseRecord>
            {
                new(Guid.NewGuid(), payer, 200m, false, new List<ShareRecord> { new(payer, 100m), new(freeRider, 100m) }),
                new(Guid.NewGuid(), payer, 400m, false, new List<ShareRecord> { new(payer, 200m), new(freeRider, 200m) })
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            Assert.Equal(300m, balances.First(b => b.UserId == payer).NetBalance);
            Assert.Equal(-300m, balances.First(b => b.UserId == freeRider).NetBalance);
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void BalanceTest_SettlementReversed_BalancesRestored()
        {
            var ali = Guid.NewGuid();
            var ahmed = Guid.NewGuid();

            var members = new List<MemberRecord>
            {
                new(ali, "Ali", "ali@test.com", "Owner", true),
                new(ahmed, "Ahmed", "ahmed@test.com", "Member", true)
            };

            var expenses = new List<ExpenseRecord>
            {
                new(Guid.NewGuid(), ali, 1000m, false, new List<ShareRecord> { new(ali, 500m), new(ahmed, 500m) })
            };

            var settlementId = Guid.NewGuid();

            // Active settlement: Ahmed pays Ali 500 -> both at 0
            var activeSettlements = new List<SettlementRecord> { new(settlementId, ahmed, ali, 500m, false) };
            var settledBalances = GroupBalanceCalculator.CalculateBalances(members, expenses, activeSettlements);
            Assert.Equal(0m, settledBalances.First(b => b.UserId == ali).NetBalance);
            Assert.Equal(0m, settledBalances.First(b => b.UserId == ahmed).NetBalance);

            // Reversed/Deleted settlement: Ahmed pays Ali 500 is deleted -> Ali back to +500, Ahmed back to -500
            var reversedSettlements = new List<SettlementRecord> { new(settlementId, ahmed, ali, 500m, true) };
            var restoredBalances = GroupBalanceCalculator.CalculateBalances(members, expenses, reversedSettlements);
            Assert.Equal(500m, restoredBalances.First(b => b.UserId == ali).NetBalance);
            Assert.Equal(-500m, restoredBalances.First(b => b.UserId == ahmed).NetBalance);
            Assert.Equal(0m, restoredBalances.Sum(b => b.NetBalance));
        }

        [Fact]
        public void BalanceTest_MemberRemovedWithOutstandingBalance_HistoricalBalancePreserved()
        {
            var ali = Guid.NewGuid();
            var removedUser = Guid.NewGuid();

            var members = new List<MemberRecord>
            {
                new(ali, "Ali", "ali@test.com", "Owner", true),
                new(removedUser, "Removed", "removed@test.com", "Member", false) // Inactive
            };

            var expenses = new List<ExpenseRecord>
            {
                new(Guid.NewGuid(), ali, 1000m, false, new List<ShareRecord> { new(ali, 500m), new(removedUser, 500m) })
            };

            var balances = GroupBalanceCalculator.CalculateBalances(members, expenses, Enumerable.Empty<SettlementRecord>());

            var removedBalance = balances.First(b => b.UserId == removedUser);
            Assert.False(removedBalance.IsActive);
            Assert.Equal(-500m, removedBalance.NetBalance);
            Assert.Equal(500m, balances.First(b => b.UserId == ali).NetBalance);
            Assert.Equal(0m, balances.Sum(b => b.NetBalance));
        }

        #endregion

        #region 5. Security Tests

        [Fact]
        public async Task SecurityTest_01_MemberCannotAccessGroupTheyDoNotBelongTo()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var settlementRepo = new InMemorySettlementRepository();
            var authService = new SplitGroupAuthorizationService(groupRepo, expenseRepo, settlementRepo);

            var groupId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var intruderId = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Private Group", OwnerId = memberId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = memberId, Role = "Owner", IsActive = true });

            var memberAuth = await authService.AuthorizeMembershipAsync(groupId, memberId);
            var intruderAuth = await authService.AuthorizeMembershipAsync(groupId, intruderId);

            Assert.True(memberAuth.IsAuthorized);
            Assert.False(intruderAuth.IsAuthorized);
        }

        [Fact]
        public async Task SecurityTest_02_MemberCannotModifyAnotherGroupsExpenses()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var group1Id = Guid.NewGuid();
            var group2Id = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = group1Id, Name = "Group 1", OwnerId = user1, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = group1Id, UserId = user1, Role = "Owner", IsActive = true });

            groupRepo.Groups.Add(new SplitGroup { Id = group2Id, Name = "Group 2", OwnerId = user2, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = group2Id, UserId = user2, Role = "Owner", IsActive = true });

            // Create expense in Group 1
            var addResult = await service.AddExpenseAsync(group1Id, new CreateSplitExpenseDto
            {
                Title = "G1 Expense",
                Amount = 100m,
                Description = "G1 Expense",
                ExpenseDate = DateTime.UtcNow,
                PayerId = user1,
                ParticipantUserIds = new List<Guid> { user1 }
            }, user1, "User 1");
            var expenseId = addResult.Data!.Id;

            // User 2 (member of Group 2 only) tries to edit Group 1's expense
            var editResult = await service.EditExpenseAsync(group1Id, expenseId, new UpdateSplitExpenseDto
            {
                Title = "Hacked Expense",
                Amount = 500m,
                Description = "Hacked Expense",
                ExpenseDate = DateTime.UtcNow,
                PayerId = user2,
                ParticipantUserIds = new List<Guid> { user2 }
            }, user2, "User 2");

            Assert.False(editResult.Success);
            Assert.Contains("creator or the group owner", editResult.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SecurityTest_03_MemberCannotRecordSettlementsInAnotherGroup()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var settlementRepo = new InMemorySettlementRepository();
            var balanceRepo = new InMemoryBalanceRepository(groupRepo, expenseRepo, settlementRepo);
            var simplifier = new DebtSimplificationService();
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, simplifier);

            var group1Id = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = group1Id, Name = "Group 1", OwnerId = user1, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = group1Id, UserId = user1, Role = "Owner", IsActive = true });

            // User 2 (not in Group 1) tries to record a settlement
            var result = await service.RecordSettlementAsync(group1Id, new RecordSettlementDto
            {
                FromUserId = user2,
                ToUserId = user1,
                Amount = 50m,
                SettlementDate = DateTime.UtcNow
            }, user2, "User 2");

            Assert.False(result.Success);
            Assert.Contains("active group member", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SecurityTest_04_RemovedMemberCannotPerformNewOperations()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var owner = Guid.NewGuid();
            var removed = Guid.NewGuid();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Group", OwnerId = owner, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = owner, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = removed, Role = "Member", IsActive = false }); // removed

            var result = await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
            {
                Title = "Expense by removed user",
                Amount = 100m,
                Description = "Expense by removed user",
                ExpenseDate = DateTime.UtcNow,
                PayerId = removed,
                ParticipantUserIds = new List<Guid> { owner, removed }
            }, removed, "Removed User");

            Assert.False(result.Success);
            Assert.Contains("active group member", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SecurityTest_05_SplitGroupMembershipGrantsZeroCashbookAccess()
        {
            // Verify structural decoupling: SplitGroup entities do not reference Cashbooks
            var splitGroupTypes = typeof(SplitGroup).Assembly.GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.Contains("SplitExpense") && t.IsClass);

            foreach (var type in splitGroupTypes)
            {
                var cashbookProps = type.GetProperties()
                    .Where(p => p.Name.Contains("Cashbook", StringComparison.OrdinalIgnoreCase));
                Assert.Empty(cashbookProps);
            }
        }

        #endregion

        #region 6. Concurrency Tests

        [Fact]
        public async Task ConcurrencyTest_SimultaneousExpenseCreationAndEditing_MaintainsBalanceInvariant()
        {
            var groupRepo = new InMemoryGroupRepository();
            var expenseRepo = new InMemoryExpenseRepository();
            var settlementRepo = new InMemorySettlementRepository();
            var balanceRepo = new InMemoryBalanceRepository(groupRepo, expenseRepo, settlementRepo);
            var calculator = new EqualSplitCalculatorService();
            var service = new SplitExpenseService(expenseRepo, groupRepo, calculator);

            var groupId = Guid.NewGuid();
            var members = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();

            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Concurrent Group", OwnerId = members[0], IsActive = true });
            foreach (var m in members)
            {
                groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = m, Role = "Member", IsActive = true });
            }

            // Concurrently create 20 expenses with amounts across members
            var tasks = Enumerable.Range(1, 20).Select(async i =>
            {
                var payer = members[i % members.Count];
                var amount = 100m * i;
                await service.AddExpenseAsync(groupId, new CreateSplitExpenseDto
                {
                    Title = $"Concurrent Expense #{i}",
                    Amount = amount,
                    Description = $"Concurrent Expense #{i}",
                    ExpenseDate = DateTime.UtcNow,
                    PayerId = payer,
                    ParticipantUserIds = members
                }, payer, $"User {i}");
            });

            await Task.WhenAll(tasks);

            // Verify all shares equal the expense amount exactly
            foreach (var exp in expenseRepo.Expenses)
            {
                var shares = expenseRepo.Shares.Where(s => s.ExpenseId == exp.Id).ToList();
                Assert.Equal(exp.Amount, shares.Sum(s => s.ShareAmount));
            }

            // Verify net group balances sum to 0
            var finalBalances = await balanceRepo.GetGroupBalancesAsync(groupId);
            Assert.Equal(0m, finalBalances.Sum(b => b.NetBalance));
        }

        #endregion
    }
}
