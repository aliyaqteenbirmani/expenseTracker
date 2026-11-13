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
    public class Phase2Fix2PrivacyLeakTests
    {
        #region Mock Repositories

        private class MockGroupRepository : ISplitGroupRepository
        {
            public List<SplitGroup> Groups { get; } = new();
            public List<SplitGroupMember> Members { get; } = new();
            public List<SplitGroupActivity> Activities { get; } = new();

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
                    FullName = "User " + m.UserId.ToString().Substring(0, 4),
                    Email = m.UserId.ToString().Substring(0, 4) + "@test.com",
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

            public Task<int> GetPendingSettlementsCountAsync(Guid groupId) =>
                Task.FromResult(0);

            public Task<User?> GetUserByEmailOrIdAsync(string? email, Guid? userId) =>
                Task.FromResult<User?>(null);

            public Task<SplitGroupInvitation?> GetInvitationByIdAsync(Guid invitationId) =>
                Task.FromResult<SplitGroupInvitation?>(null);

            public Task<SplitGroupInvitation?> GetInvitationByTokenAsync(string token) =>
                Task.FromResult<SplitGroupInvitation?>(null);

            public Task<SplitGroupInvitation?> GetPendingInvitationByEmailAsync(Guid groupId, string email) =>
                Task.FromResult<SplitGroupInvitation?>(null);

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId) =>
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByUserAsync(Guid userId, string userEmail) =>
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<SplitGroupInvitation> CreateInvitationAsync(SplitGroupInvitation invitation, string inviterName, string groupName) =>
                Task.FromResult(invitation);

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

            public Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName) =>
                Task.FromResult(true);

            public Task<List<SplitGroupActivityDto>> GetGroupActivitiesAsync(Guid groupId, int limit = 50)
            {
                var filtered = Activities.Where(a => a.GroupId == groupId && !a.ActivityType.StartsWith("Settlement", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(limit)
                    .Select(a => new SplitGroupActivityDto
                    {
                        Id = a.Id,
                        GroupId = a.GroupId,
                        UserId = a.UserId,
                        UserName = "User",
                        ActivityType = a.ActivityType,
                        Description = a.Description,
                        CreatedAt = a.CreatedAt
                    }).ToList();
                return Task.FromResult(filtered);
            }

            public Task<PagedGroupActivityResultDto> GetPagedGroupActivitiesAsync(Guid groupId, SplitGroupActivityQueryParameters parameters)
            {
                var filtered = Activities.Where(a => a.GroupId == groupId && !a.ActivityType.StartsWith("Settlement", StringComparison.OrdinalIgnoreCase));
                var items = filtered.Select(a => new SplitGroupActivityDto
                {
                    Id = a.Id,
                    GroupId = a.GroupId,
                    UserId = a.UserId,
                    UserName = "User",
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                }).ToList();

                return Task.FromResult(new PagedGroupActivityResultDto
                {
                    Items = items,
                    TotalCount = items.Count,
                    PageNumber = 1,
                    PageSize = 20
                });
            }

            public Task<SplitGroupActivityDto?> GetActivityByIdAsync(Guid groupId, Guid activityId)
            {
                var a = Activities.FirstOrDefault(x => x.GroupId == groupId && x.Id == activityId && !x.ActivityType.StartsWith("Settlement", StringComparison.OrdinalIgnoreCase));
                if (a == null) return Task.FromResult<SplitGroupActivityDto?>(null);
                return Task.FromResult<SplitGroupActivityDto?>(new SplitGroupActivityDto
                {
                    Id = a.Id,
                    GroupId = a.GroupId,
                    UserId = a.UserId,
                    UserName = "User",
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                });
            }

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

        private class MockBalanceRepository : ISplitBalanceRepository
        {
            public List<MemberBalanceDto> Balances { get; set; } = new();

            public Task<List<MemberBalanceDto>> GetGroupBalancesAsync(Guid groupId) =>
                Task.FromResult(Balances.Select(CloneBalance).ToList());

            public Task<MemberBalanceDto?> GetMemberBalanceAsync(Guid groupId, Guid userId)
            {
                var b = Balances.FirstOrDefault(x => x.UserId == userId);
                return Task.FromResult(b == null ? null : CloneBalance(b));
            }

            public Task<GroupFinancialSummaryDto?> GetGroupFinancialSummaryAsync(Guid groupId, Guid currentUserId)
            {
                var myBal = Balances.FirstOrDefault(b => b.UserId == currentUserId);
                return Task.FromResult<GroupFinancialSummaryDto?>(new GroupFinancialSummaryDto
                {
                    GroupId = groupId,
                    GroupName = "Trip",
                    TotalExpenses = 1000m,
                    TotalSettlements = 0m,
                    CurrentUserBalance = myBal?.NetBalance ?? 0m,
                    CurrentUserTotalPaid = myBal?.TotalPaid ?? 0m,
                    CurrentUserTotalShare = myBal?.TotalShare ?? 0m,
                    CurrentUserPendingPaid = myBal?.PendingPaid ?? 0m,
                    CurrentUserPendingReceived = myBal?.PendingReceived ?? 0m,
                    MemberCount = Balances.Count
                });
            }

            private static MemberBalanceDto CloneBalance(MemberBalanceDto b) => new()
            {
                UserId = b.UserId,
                FullName = b.FullName,
                Email = b.Email,
                Role = b.Role,
                IsActive = b.IsActive,
                TotalPaid = b.TotalPaid,
                TotalShare = b.TotalShare,
                SettlementsPaid = b.SettlementsPaid,
                SettlementsReceived = b.SettlementsReceived,
                PendingPaid = b.PendingPaid,
                PendingReceived = b.PendingReceived,
                NetBalance = b.NetBalance,
                StatusDescription = b.StatusDescription
            };
        }

        private class MockSettlementRepository : ISplitSettlementRepository
        {
            public List<SplitSettlement> Settlements { get; } = new();
            public List<SplitNotification> Notifications { get; } = new();

            public Task<SplitSettlement?> GetSettlementByIdAsync(Guid settlementId) =>
                Task.FromResult(Settlements.FirstOrDefault(s => s.Id == settlementId && !s.IsDeleted));

            public Task<SplitSettlementDto?> GetSettlementDetailsAsync(Guid settlementId, Guid groupId)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.GroupId == groupId && !x.IsDeleted);
                return Task.FromResult(s == null ? null : MapDto(s));
            }

            public Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId) =>
                Task.FromResult(Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted).Select(MapDto).ToList());

            public Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId, Guid currentUserId, bool isOwner) =>
                Task.FromResult(Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted && (isOwner || s.FromUserId == currentUserId || s.ToUserId == currentUserId))
                    .Select(MapDto).ToList());

            public Task<List<SplitSettlementDto>> GetPendingActionsAsync(Guid groupId, Guid currentUserId, bool isOwner) =>
                Task.FromResult(Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted &&
                    ((s.Status == SplitSettlementStatus.PendingReceiver && s.ToUserId == currentUserId) ||
                     (s.Status == SplitSettlementStatus.PendingOwner && isOwner)))
                    .Select(MapDto).ToList());

            public Task<List<SplitSettlement>> GetPendingSettlementsAsync(Guid groupId) =>
                Task.FromResult(Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted &&
                    (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner)).ToList());

            public Task<decimal> GetPendingPayerToReceiverAmountAsync(Guid groupId, Guid fromUserId, Guid toUserId, Guid? excludeSettlementId = null) =>
                Task.FromResult(0m);

            public Task<SplitSettlementDto> CreateSettlementAsync(SplitSettlement settlement, List<SplitNotification>? notifications = null)
            {
                Settlements.Add(settlement);
                return Task.FromResult(MapDto(settlement));
            }

            public Task<int> ConfirmByReceiverAsync(Guid settlementId, string newStatus, Guid receiverUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && !x.IsDeleted);
                if (s == null) return Task.FromResult(0);
                s.Status = newStatus;
                s.ReceiverActionBy = receiverUserId;
                s.ReceiverActionAt = DateTime.UtcNow;
                return Task.FromResult(1);
            }

            public Task<int> DeclineByReceiverAsync(Guid settlementId, string? reason, Guid receiverUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && !x.IsDeleted);
                if (s == null) return Task.FromResult(0);
                s.Status = SplitSettlementStatus.DeclinedByReceiver;
                s.ReceiverActionBy = receiverUserId;
                s.ReceiverActionAt = DateTime.UtcNow;
                s.DeclineReason = reason;
                return Task.FromResult(1);
            }

            public Task<int> ApproveByOwnerAsync(Guid settlementId, Guid ownerUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && !x.IsDeleted);
                if (s == null) return Task.FromResult(0);
                s.Status = SplitSettlementStatus.Confirmed;
                s.OwnerActionBy = ownerUserId;
                s.OwnerActionAt = DateTime.UtcNow;
                return Task.FromResult(1);
            }

            public Task<int> DeclineByOwnerAsync(Guid settlementId, string? reason, Guid ownerUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && !x.IsDeleted);
                if (s == null) return Task.FromResult(0);
                s.Status = SplitSettlementStatus.DeclinedByOwner;
                s.OwnerActionBy = ownerUserId;
                s.OwnerActionAt = DateTime.UtcNow;
                s.DeclineReason = reason;
                return Task.FromResult(1);
            }

            public Task<int> CancelByPayerAsync(Guid settlementId, Guid payerUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && !x.IsDeleted);
                if (s == null) return Task.FromResult(0);
                s.Status = SplitSettlementStatus.Cancelled;
                s.CancelledBy = payerUserId;
                s.CancelledAt = DateTime.UtcNow;
                return Task.FromResult(1);
            }

            public Task<bool> ReverseConfirmedSettlementAsync(Guid settlementId, Guid groupId, Guid ownerUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.GroupId == groupId && !x.IsDeleted);
                if (s == null) return Task.FromResult(false);
                s.IsDeleted = true;
                return Task.FromResult(true);
            }

            public Task<List<SplitSettlementDto>> GetSettlementsNeedingReminderAsync(DateTime olderThan) =>
                Task.FromResult(new List<SplitSettlementDto>());

            public Task<bool> AddReminderNotificationAsync(SplitNotification reminderNotification) =>
                Task.FromResult(true);

            public Task<SplitSettlementDto> CreateSettlementWithActivityAsync(SplitSettlement settlement, string creatorName, string groupName)
            {
                Settlements.Add(settlement);
                return Task.FromResult(MapDto(settlement));
            }

            public Task<SplitSettlementDto?> UpdateSettlementWithActivityAsync(SplitSettlement settlement, string editorName, string groupName) =>
                Task.FromResult<SplitSettlementDto?>(MapDto(settlement));

            public Task<bool> SoftDeleteSettlementWithActivityAsync(Guid settlementId, Guid groupId, Guid deletedByUserId, string deletedByName) =>
                Task.FromResult(true);

            private static SplitSettlementDto MapDto(SplitSettlement s) => new()
            {
                Id = s.Id,
                GroupId = s.GroupId,
                FromUserId = s.FromUserId,
                ToUserId = s.ToUserId,
                Amount = s.Amount,
                Status = s.Status,
                SettlementDate = s.SettlementDate,
                CreatedBy = s.CreatedBy
            };
        }

        #endregion

        [Fact]
        public async Task BalancePrivacy_NonOwnerNonParty_PendingAmountsAreZeroedOut()
        {
            var ownerId = Guid.NewGuid();
            var payerAId = Guid.NewGuid();
            var receiverBId = Guid.NewGuid();
            var observerCId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerAId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverBId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = observerCId, Role = "Member", IsActive = true });

            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerAId, FullName = "Alice", NetBalance = -500m, PendingPaid = 500m, PendingReceived = 0m },
                    new() { UserId = receiverBId, FullName = "Bob", NetBalance = 500m, PendingPaid = 0m, PendingReceived = 500m },
                    new() { UserId = observerCId, FullName = "Charlie", NetBalance = 0m, PendingPaid = 0m, PendingReceived = 0m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = 0m, PendingPaid = 0m, PendingReceived = 0m }
                }
            };

            var balanceService = new SplitBalanceService(balanceRepo, groupRepo);

            // 1. Observer C calls GetGroupBalancesAsync -> All other members' PendingPaid & PendingReceived are 0
            var cBalances = await balanceService.GetGroupBalancesAsync(groupId, observerCId);
            Assert.True(cBalances.Success);
            var aBalSeenByC = cBalances.Data!.First(b => b.UserId == payerAId);
            var bBalSeenByC = cBalances.Data!.First(b => b.UserId == receiverBId);
            Assert.Equal(0m, aBalSeenByC.PendingPaid);
            Assert.Equal(0m, aBalSeenByC.PendingReceived);
            Assert.Equal(0m, bBalSeenByC.PendingPaid);
            Assert.Equal(0m, bBalSeenByC.PendingReceived);

            // 2. Observer C calls GetMemberBalanceAsync for Alice -> Pending amounts are 0
            var cInspectsA = await balanceService.GetMemberBalanceAsync(groupId, payerAId, observerCId);
            Assert.True(cInspectsA.Success);
            Assert.Equal(0m, cInspectsA.Data!.PendingPaid);
            Assert.Equal(0m, cInspectsA.Data!.PendingReceived);

            // 3. Alice (payer) calls GetGroupBalancesAsync -> Sees her own PendingPaid, but Bob's pending is zeroed
            var aBalances = await balanceService.GetGroupBalancesAsync(groupId, payerAId);
            Assert.True(aBalances.Success);
            var aBalSeenByA = aBalances.Data!.First(b => b.UserId == payerAId);
            var bBalSeenByA = aBalances.Data!.First(b => b.UserId == receiverBId);
            Assert.Equal(500m, aBalSeenByA.PendingPaid);
            Assert.Equal(0m, bBalSeenByA.PendingReceived);

            // 4. Owner calls GetGroupBalancesAsync -> Sees all pending amounts
            var ownerBalances = await balanceService.GetGroupBalancesAsync(groupId, ownerId);
            Assert.True(ownerBalances.Success);
            var aBalSeenByOwner = ownerBalances.Data!.First(b => b.UserId == payerAId);
            var bBalSeenByOwner = ownerBalances.Data!.First(b => b.UserId == receiverBId);
            Assert.Equal(500m, aBalSeenByOwner.PendingPaid);
            Assert.Equal(500m, bBalSeenByOwner.PendingReceived);
        }

        [Fact]
        public async Task SettleUpOverview_NonPartyMember_SuggestionsNotAffectedByOthersPending()
        {
            var ownerId = Guid.NewGuid();
            var payerAId = Guid.NewGuid();
            var receiverBId = Guid.NewGuid();
            var memberCId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerAId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverBId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = memberCId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            // A pending settlement of 500 from Alice to Bob
            settlementRepo.Settlements.Add(new SplitSettlement
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                FromUserId = payerAId,
                ToUserId = receiverBId,
                Amount = 500m,
                Status = SplitSettlementStatus.PendingReceiver
            });

            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerAId, FullName = "Alice", NetBalance = -1000m, PendingPaid = 500m },
                    new() { UserId = receiverBId, FullName = "Bob", NetBalance = 500m, PendingReceived = 500m },
                    new() { UserId = memberCId, FullName = "Charlie", NetBalance = 500m, PendingPaid = 0m, PendingReceived = 0m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = 0m }
                }
            };

            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Member C calls GetSettleUpOverviewAsync:
            // C is not party to A->B pending settlement.
            // C's suggestions must be based on confirmed balances (A owes 1000, B gets 500, C gets 500).
            var cOverview = await service.GetSettleUpOverviewAsync(groupId, memberCId);
            Assert.True(cOverview.Success);

            // C sees Charlie getting 500 from Alice
            Assert.Contains(cOverview.Data!.SuggestedTransactions, t => t.FromUserId == payerAId && t.ToUserId == memberCId && t.Amount == 500m);
            // In Debtors/Creditors, C sees PendingPaid=0 and PendingReceived=0 for Alice and Bob
            var aliceDebtor = cOverview.Data.Debtors.First(d => d.UserId == payerAId);
            var bobCreditor = cOverview.Data.Creditors.First(c => c.UserId == receiverBId);
            Assert.Equal(0m, aliceDebtor.PendingPaid);
            Assert.Equal(0m, bobCreditor.PendingReceived);

            // Owner calls GetSettleUpOverviewAsync:
            // Owner sees full pending details
            var ownerOverview = await service.GetSettleUpOverviewAsync(groupId, ownerId);
            Assert.True(ownerOverview.Success);
            var aliceDebtorOwner = ownerOverview.Data!.Debtors.First(d => d.UserId == payerAId);
            Assert.Equal(500m, aliceDebtorOwner.PendingPaid);
        }

        [Fact]
        public async Task SettlementsListAndDetails_PrivacyIsolation()
        {
            var ownerId = Guid.NewGuid();
            var payerAId = Guid.NewGuid();
            var receiverBId = Guid.NewGuid();
            var observerCId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerAId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverBId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = observerCId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var settlement = new SplitSettlement
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                FromUserId = payerAId,
                ToUserId = receiverBId,
                Amount = 300m,
                Status = SplitSettlementStatus.PendingReceiver,
                CreatedBy = payerAId
            };
            settlementRepo.Settlements.Add(settlement);

            var balanceRepo = new MockBalanceRepository();
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // 1. Observer C calls GetGroupSettlementsAsync -> Returns empty list (filtered at repo level)
            var cList = await service.GetGroupSettlementsAsync(groupId, observerCId);
            Assert.True(cList.Success);
            Assert.Empty(cList.Data!);

            // 2. Observer C calls GetSettlementDetailsAsync -> 403 Forbidden
            var cDetail = await service.GetSettlementDetailsAsync(groupId, settlement.Id, observerCId);
            Assert.False(cDetail.Success);
            Assert.Equal(403, cDetail.StatusCode);

            // 3. Alice (payer) calls GetGroupSettlementsAsync -> Returns 1 item
            var aList = await service.GetGroupSettlementsAsync(groupId, payerAId);
            Assert.True(aList.Success);
            Assert.Single(aList.Data!);

            // 4. Alice calls GetSettlementDetailsAsync -> Returns details successfully
            var aDetail = await service.GetSettlementDetailsAsync(groupId, settlement.Id, payerAId);
            Assert.True(aDetail.Success);
            Assert.Equal(300m, aDetail.Data!.Amount);

            // 5. Owner calls GetSettlementDetailsAsync -> Returns details successfully
            var ownerDetail = await service.GetSettlementDetailsAsync(groupId, settlement.Id, ownerId);
            Assert.True(ownerDetail.Success);
            Assert.Equal(300m, ownerDetail.Data!.Amount);
        }

        [Fact]
        public async Task ActivityFeed_SettlementEventsAreExcludedFromSharedFeed()
        {
            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = memberId, Role = "Member", IsActive = true });

            var settlementActivityId = Guid.NewGuid();
            var expenseActivityId = Guid.NewGuid();

            groupRepo.Activities.Add(new SplitGroupActivity
            {
                Id = expenseActivityId,
                GroupId = groupId,
                UserId = memberId,
                ActivityType = "ExpenseCreated",
                Description = "Alice added Lunch",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            });
            groupRepo.Activities.Add(new SplitGroupActivity
            {
                Id = settlementActivityId,
                GroupId = groupId,
                UserId = memberId,
                ActivityType = "SettlementRecorded",
                Description = "Alice paid Bob Rs. 500",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            });

            var groupService = new SplitGroupService(groupRepo);

            // 1. GetGroupActivitiesAsync excludes SettlementRecorded
            var activities = await groupService.GetGroupActivitiesAsync(groupId, memberId);
            Assert.True(activities.Success);
            Assert.Single(activities.Data!);
            Assert.Equal("ExpenseCreated", activities.Data![0].ActivityType);

            // 2. GetPagedGroupActivitiesAsync excludes SettlementRecorded
            var paged = await groupService.GetPagedGroupActivitiesAsync(groupId, new SplitGroupActivityQueryParameters(), memberId);
            Assert.True(paged.Success);
            Assert.Single(paged.Data!.Items);
            Assert.Equal("ExpenseCreated", paged.Data.Items[0].ActivityType);

            // 3. GetActivityByIdAsync returns failure for Settlement activity
            var singleActivity = await groupService.GetActivityByIdAsync(groupId, settlementActivityId, memberId);
            Assert.False(singleActivity.Success);
        }
    }
}
