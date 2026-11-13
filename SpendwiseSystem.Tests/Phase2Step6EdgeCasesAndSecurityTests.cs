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
    public class Phase2Step6EdgeCasesAndSecurityTests
    {
        #region Mock Repositories

        private class MockGroupRepository : ISplitGroupRepository
        {
            public List<SplitGroup> Groups { get; } = new();
            public List<SplitGroupMember> Members { get; } = new();
            public List<SplitSettlement> Settlements { get; } = new();

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

            public Task<bool> SoftDeleteGroupAsync(Guid groupId, Guid userId, string userName)
            {
                var g = Groups.FirstOrDefault(x => x.Id == groupId);
                if (g != null) g.IsDeleted = true;
                return Task.FromResult(true);
            }

            public Task<bool> HasFinancialRecordsAsync(Guid groupId) =>
                Task.FromResult(false);

            public Task<int> GetPendingSettlementsCountAsync(Guid groupId) =>
                Task.FromResult(Settlements.Count(s => s.GroupId == groupId && !s.IsDeleted &&
                    (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner)));

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

            public Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName)
            {
                var m = Members.FirstOrDefault(x => x.GroupId == groupId && x.UserId == userIdToRemove);
                if (m != null) m.IsActive = false;
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

            public Task<decimal> GetPendingPayerToReceiverAmountAsync(Guid groupId, Guid fromUserId, Guid toUserId, Guid? excludeSettlementId = null)
            {
                var sum = Settlements.Where(s => s.GroupId == groupId && s.FromUserId == fromUserId && s.ToUserId == toUserId
                    && !s.IsDeleted && (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner)
                    && (excludeSettlementId == null || s.Id != excludeSettlementId))
                    .Sum(s => s.Amount);
                return Task.FromResult(sum);
            }

            public Task<SplitSettlementDto> CreateSettlementAsync(SplitSettlement settlement, List<SplitNotification>? notifications = null)
            {
                Settlements.Add(settlement);
                if (notifications != null && notifications.Count > 0)
                    Notifications.AddRange(notifications);
                return Task.FromResult(MapDto(settlement));
            }

            public Task<int> ConfirmByReceiverAsync(Guid settlementId, string newStatus, Guid receiverUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.Status == SplitSettlementStatus.PendingReceiver && !x.IsDeleted);
                if (s == null) return Task.FromResult(0);
                s.Status = newStatus;
                s.ReceiverActionBy = receiverUserId;
                s.ReceiverActionAt = DateTime.UtcNow;

                if (notifications != null && notifications.Count > 0)
                    Notifications.AddRange(notifications);

                return Task.FromResult(1);
            }

            public Task<int> DeclineByReceiverAsync(Guid settlementId, string? reason, Guid receiverUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.Status == SplitSettlementStatus.PendingReceiver && !x.IsDeleted);
                if (s == null) return Task.FromResult(0);
                s.Status = SplitSettlementStatus.DeclinedByReceiver;
                s.ReceiverActionBy = receiverUserId;
                s.ReceiverActionAt = DateTime.UtcNow;
                s.DeclineReason = reason;

                if (notifications != null && notifications.Count > 0)
                    Notifications.AddRange(notifications);

                return Task.FromResult(1);
            }

            public Task<int> ApproveByOwnerAsync(Guid settlementId, Guid ownerUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.Status == SplitSettlementStatus.PendingOwner && !x.IsDeleted);
                if (s == null) return Task.FromResult(0);
                s.Status = SplitSettlementStatus.Confirmed;
                s.OwnerActionBy = ownerUserId;
                s.OwnerActionAt = DateTime.UtcNow;

                if (notifications != null && notifications.Count > 0)
                    Notifications.AddRange(notifications);

                return Task.FromResult(1);
            }

            public Task<int> DeclineByOwnerAsync(Guid settlementId, string? reason, Guid ownerUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.Status == SplitSettlementStatus.PendingOwner && !x.IsDeleted);
                if (s == null) return Task.FromResult(0);
                s.Status = SplitSettlementStatus.DeclinedByOwner;
                s.OwnerActionBy = ownerUserId;
                s.OwnerActionAt = DateTime.UtcNow;
                s.DeclineReason = reason;

                if (notifications != null && notifications.Count > 0)
                    Notifications.AddRange(notifications);

                return Task.FromResult(1);
            }

            public Task<int> CancelByPayerAsync(Guid settlementId, Guid payerUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && !x.IsDeleted &&
                    (x.Status == SplitSettlementStatus.PendingReceiver || x.Status == SplitSettlementStatus.PendingOwner));
                if (s == null) return Task.FromResult(0);
                s.Status = SplitSettlementStatus.Cancelled;
                s.CancelledBy = payerUserId;
                s.CancelledAt = DateTime.UtcNow;

                if (notifications != null && notifications.Count > 0)
                    Notifications.AddRange(notifications);

                return Task.FromResult(1);
            }

            public Task<bool> ReverseConfirmedSettlementAsync(Guid settlementId, Guid groupId, Guid ownerUserId, List<SplitNotification>? notifications = null)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.GroupId == groupId && x.Status == SplitSettlementStatus.Confirmed && !x.IsDeleted);
                if (s == null) return Task.FromResult(false);
                s.IsDeleted = true;

                if (notifications != null && notifications.Count > 0)
                    Notifications.AddRange(notifications);

                return Task.FromResult(true);
            }

            public Task<SplitSettlementDto> CreateSettlementWithActivityAsync(
                SplitSettlement settlement, string creatorName, string groupName) =>
                CreateSettlementAsync(settlement);

            public Task<SplitSettlementDto?> UpdateSettlementWithActivityAsync(
                SplitSettlement settlement, string editorName, string groupName) =>
                Task.FromResult<SplitSettlementDto?>(MapDto(settlement));

            public Task<bool> SoftDeleteSettlementWithActivityAsync(
                Guid settlementId, Guid groupId, Guid deletedByUserId, string deletedByName) =>
                SoftDeleteSettlementAsync(settlementId, groupId);

            public Task<List<SplitSettlementDto>> GetSettlementsNeedingReminderAsync(DateTime olderThan)
            {
                var list = Settlements.Where(s => !s.IsDeleted &&
                    (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner) &&
                    s.CreatedAt <= olderThan &&
                    !Notifications.Any(n => n.ReferenceId == s.Id && n.Type == "SettlementReminder"))
                    .Select(MapDto).ToList();
                return Task.FromResult(list);
            }

            public Task<bool> AddReminderNotificationAsync(SplitNotification reminderNotification)
            {
                bool exists = Notifications.Any(n => n.ReferenceId == reminderNotification.ReferenceId &&
                    n.Type == "SettlementReminder" && n.UserId == reminderNotification.UserId);
                if (exists) return Task.FromResult(false);
                Notifications.Add(reminderNotification);
                return Task.FromResult(true);
            }

            public Task<bool> UpdateSettlementAsync(SplitSettlement settlement) => Task.FromResult(true);
            public Task<bool> SoftDeleteSettlementAsync(Guid settlementId, Guid groupId)
            {
                var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.GroupId == groupId);
                if (s != null) s.IsDeleted = true;
                return Task.FromResult(true);
            }

            private static SplitSettlementDto MapDto(SplitSettlement s) => new()
            {
                Id = s.Id,
                GroupId = s.GroupId,
                GroupName = "Test Group",
                FromUserId = s.FromUserId,
                FromUserName = "User " + s.FromUserId.ToString().Substring(0, 4),
                ToUserId = s.ToUserId,
                ToUserName = "User " + s.ToUserId.ToString().Substring(0, 4),
                Amount = s.Amount,
                SettlementDate = s.SettlementDate,
                Note = s.Note,
                Status = s.Status,
                CreatedBy = s.CreatedBy,
                CreatedByName = "User " + s.CreatedBy.ToString().Substring(0, 4),
                DeclineReason = s.DeclineReason,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            };
        }

        private class MockBalanceRepository : ISplitBalanceRepository
        {
            public List<MemberBalanceDto> Balances { get; set; } = new();

            public Task<List<MemberBalanceDto>> GetGroupBalancesAsync(Guid groupId) =>
                Task.FromResult(Balances);

            public Task<MemberBalanceDto?> GetMemberBalanceAsync(Guid groupId, Guid userId) =>
                Task.FromResult(Balances.FirstOrDefault(b => b.UserId == userId));

            public Task<GroupFinancialSummaryDto?> GetGroupFinancialSummaryAsync(Guid groupId, Guid currentUserId)
            {
                var myBal = Balances.FirstOrDefault(b => b.UserId == currentUserId);
                return Task.FromResult<GroupFinancialSummaryDto?>(new GroupFinancialSummaryDto
                {
                    GroupId = groupId,
                    GroupName = "Test Group",
                    CurrentUserBalance = myBal?.NetBalance ?? 0m,
                    CurrentUserPendingPaid = myBal?.PendingPaid ?? 0m,
                    CurrentUserPendingReceived = myBal?.PendingReceived ?? 0m,
                    TotalExpenses = 1000m,
                    TotalSettlements = 500m
                });
            }
        }

        #endregion

        #region 1. Expense Splitting Tests

        [Fact]
        public void EqualSplit_DefaultAllMembers_SharesSumExactTotalAmount()
        {
            var calc = new EqualSplitCalculatorService();
            var memberIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var amount = 100m;

            var shares = calc.CalculateEqualSplit(amount, memberIds);

            Assert.Equal(3, shares.Count);
            Assert.Equal(amount, shares.Values.Sum());
            Assert.Equal(33.34m, shares.Values.Max());
            Assert.Equal(33.33m, shares.Values.Min());
        }

        [Fact]
        public void EqualSplit_SubsetSelection_OnlyIncludedMembersPay_OthersZero()
        {
            var calc = new EqualSplitCalculatorService();
            var memberA = Guid.NewGuid();
            var memberB = Guid.NewGuid();
            var memberC = Guid.NewGuid();

            // Only A and B selected for Rs. 750 expense
            var selectedIds = new List<Guid> { memberA, memberB };
            var shares = calc.CalculateEqualSplit(750m, selectedIds);

            Assert.Equal(2, shares.Count);
            Assert.Equal(375m, shares[memberA]);
            Assert.Equal(375m, shares[memberB]);
            Assert.False(shares.ContainsKey(memberC));
        }

        #endregion

        #region 2. Settlement Approval Flow (Role Combinations)

        [Fact]
        public async Task SettlementFlow_StandardTwoStep_ReceiverConfirmThenOwnerApproval()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerId, FullName = "Payer", NetBalance = -500m },
                    new() { UserId = receiverId, FullName = "Receiver", NetBalance = 500m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = 0m }
                }
            };

            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Step 1: Payer submits settlement request
            var createRes = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 200m
            }, payerId, "Payer");

            Assert.True(createRes.Success);
            Assert.Equal(SplitSettlementStatus.PendingReceiver, createRes.Data!.Status);

            // Notification: Receiver notified, payer NOT notified
            Assert.Contains(settlementRepo.Notifications, n => n.UserId == receiverId && n.Type == "SettlementRequested");
            Assert.DoesNotContain(settlementRepo.Notifications, n => n.UserId == payerId);

            // Step 2: Receiver confirms receipt -> moves to PendingOwner
            var confirmRes = await service.ConfirmSettlementAsync(groupId, createRes.Data.Id, receiverId, "Receiver");
            Assert.True(confirmRes.Success);
            Assert.Equal(SplitSettlementStatus.PendingOwner, confirmRes.Data!.Status);

            // Notification: Owner notified for approval; Payer notified receiver confirmed
            Assert.Contains(settlementRepo.Notifications, n => n.UserId == ownerId && n.Type == "SettlementAwaitingOwner");
            Assert.Contains(settlementRepo.Notifications, n => n.UserId == payerId && n.Type == "SettlementReceiverConfirmed");

            // Step 3: Owner approves -> moves to Confirmed
            var approveRes = await service.ApproveSettlementByOwnerAsync(groupId, createRes.Data.Id, ownerId, "Owner");
            Assert.True(approveRes.Success);
            Assert.Equal(SplitSettlementStatus.Confirmed, approveRes.Data!.Status);

            // Notifications: Both payer and receiver notified of confirmation
            Assert.Contains(settlementRepo.Notifications, n => n.UserId == payerId && n.Type == "SettlementConfirmed");
            Assert.Contains(settlementRepo.Notifications, n => n.UserId == receiverId && n.Type == "SettlementConfirmed");
        }

        [Fact]
        public async Task SettlementFlow_ReceiverIsOwner_ShortensToSingleStepConfirmed()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerId, FullName = "Payer", NetBalance = -300m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = 300m }
                }
            };

            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Payer submits to Owner
            var createRes = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = ownerId,
                Amount = 150m
            }, payerId, "Payer");

            Assert.True(createRes.Success);
            Assert.Equal(SplitSettlementStatus.PendingReceiver, createRes.Data!.Status);

            // Owner confirms -> Immediate Confirmed
            var confirmRes = await service.ConfirmSettlementAsync(groupId, createRes.Data.Id, ownerId, "Owner");
            Assert.True(confirmRes.Success);
            Assert.Equal(SplitSettlementStatus.Confirmed, confirmRes.Data!.Status);
        }

        [Fact]
        public async Task SettlementFlow_PayerIsOwner_ReceiverConfirmationIsFinal()
        {
            var ownerId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = -400m },
                    new() { UserId = receiverId, FullName = "Receiver", NetBalance = 400m }
                }
            };

            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Owner submits payment to Receiver
            var createRes = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = ownerId,
                ToUserId = receiverId,
                Amount = 200m
            }, ownerId, "Owner");

            Assert.True(createRes.Success);
            Assert.Equal(SplitSettlementStatus.PendingReceiver, createRes.Data!.Status);

            // Receiver confirms -> Final Confirmed (owner cannot approve own payment)
            var confirmRes = await service.ConfirmSettlementAsync(groupId, createRes.Data.Id, receiverId, "Receiver");
            Assert.True(confirmRes.Success);
            Assert.Equal(SplitSettlementStatus.Confirmed, confirmRes.Data!.Status);
        }

        [Fact]
        public async Task SettlementFlow_OwnerRecordsOnBehalfOfMembers_ConfirmedImmediately()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerId, FullName = "Payer", NetBalance = -250m },
                    new() { UserId = receiverId, FullName = "Receiver", NetBalance = 250m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = 0m }
                }
            };

            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Owner records on behalf of members
            var createRes = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 250m
            }, ownerId, "Owner");

            Assert.True(createRes.Success);
            Assert.Equal(SplitSettlementStatus.Confirmed, createRes.Data!.Status);
            // Both members receive informational notifications
            Assert.Contains(settlementRepo.Notifications, n => n.UserId == payerId && n.Type == "SettlementRecordedForYou");
            Assert.Contains(settlementRepo.Notifications, n => n.UserId == receiverId && n.Type == "SettlementRecordedForYou");
        }

        [Fact]
        public async Task SettlementFlow_DeclineAndCancelPaths()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerId, FullName = "Payer", NetBalance = -500m },
                    new() { UserId = receiverId, FullName = "Receiver", NetBalance = 500m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = 0m }
                }
            };

            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Case A: Receiver declines
            var s1 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 100m
            }, payerId, "Payer");

            var decRecRes = await service.DeclineSettlementByReceiverAsync(groupId, s1.Data!.Id, "Did not receive funds", receiverId, "Receiver");
            Assert.True(decRecRes.Success);
            Assert.Equal(SplitSettlementStatus.DeclinedByReceiver, decRecRes.Data!.Status);
            Assert.Equal("Did not receive funds", decRecRes.Data.DeclineReason);

            // Case B: Payer cancels
            var s2 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 50m
            }, payerId, "Payer");

            var cancelRes = await service.CancelSettlementAsync(groupId, s2.Data!.Id, payerId, "Payer");
            Assert.True(cancelRes.Success);
            Assert.Equal(SplitSettlementStatus.Cancelled, cancelRes.Data!.Status);
        }

        [Fact]
        public async Task SettlementFlow_OwnerCanReverseConfirmedSettlement()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            settlementRepo.Settlements.Add(new SplitSettlement
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 150m,
                Status = SplitSettlementStatus.Confirmed,
                IsDeleted = false
            });

            var balanceRepo = new MockBalanceRepository();
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            var sId = settlementRepo.Settlements.First().Id;

            // Non-owner cannot reverse
            var nonOwnerRes = await service.ReverseSettlementAsync(groupId, sId, payerId, "Payer");
            Assert.False(nonOwnerRes.Success);

            // Owner reverses
            var ownerRes = await service.ReverseSettlementAsync(groupId, sId, ownerId, "Owner");
            Assert.True(ownerRes.Success);
            Assert.True(settlementRepo.Settlements.First().IsDeleted);
        }

        #endregion

        #region 3. Debt Reservation and Over-Reservation Checks

        [Fact]
        public async Task Settlement_OverReservationRejected_CannotExceedOutstandingDebt()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerId, FullName = "Payer", NetBalance = -100m },
                    new() { UserId = receiverId, FullName = "Receiver", NetBalance = 100m }
                }
            };

            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Submit 60 (pending)
            var res1 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 60m
            }, payerId, "Payer");
            Assert.True(res1.Success);

            // Try to submit 50 more (60 + 50 = 110 > 100) -> Must fail
            var res2 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 50m
            }, payerId, "Payer");

            Assert.False(res2.Success);
            Assert.Contains("exceeds", res2.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Settlement_RecheckAfterExpenseEdit_BlocksApprovalIfDebtDecreased()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerId, FullName = "Payer", NetBalance = -200m },
                    new() { UserId = receiverId, FullName = "Receiver", NetBalance = 200m }
                }
            };

            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Payer submits 200 request
            var res = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 200m
            }, payerId, "Payer");
            Assert.True(res.Success);

            // Expense is edited or deleted in background: debt shrinks to 50
            balanceRepo.Balances.First(b => b.UserId == payerId).NetBalance = -50m;
            balanceRepo.Balances.First(b => b.UserId == receiverId).NetBalance = 50m;

            // Approver tries to confirm 200 -> Blocked by re-check
            var confirmRes = await service.ConfirmSettlementAsync(groupId, res.Data!.Id, receiverId, "Receiver");
            Assert.False(confirmRes.Success);
            Assert.Contains("exceeds remaining debt", confirmRes.Message);
        }

        #endregion

        #region 4. Edge Cases: Group Archive, Member Removal, Reminders

        [Fact]
        public async Task GroupDelete_BlockedWhileSettlementsPending_NamesExactCount()
        {
            var ownerId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });

            // Add 2 pending settlements to group
            groupRepo.Settlements.Add(new SplitSettlement { Id = Guid.NewGuid(), GroupId = groupId, Status = SplitSettlementStatus.PendingReceiver, IsDeleted = false });
            groupRepo.Settlements.Add(new SplitSettlement { Id = Guid.NewGuid(), GroupId = groupId, Status = SplitSettlementStatus.PendingOwner, IsDeleted = false });

            var groupService = new SplitGroupService(groupRepo);

            var deleteRes = await groupService.DeleteGroupAsync(groupId, ownerId, "Owner");

            Assert.False(deleteRes.Success);
            Assert.Contains("2 settlement request(s) are pending approval", deleteRes.Message);
        }

        [Fact]
        public async Task MemberRemoval_RemovedMemberCannotCreateNewRequests()
        {
            var ownerId = Guid.NewGuid();
            var removedMemberId = Guid.NewGuid();
            var otherMemberId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            // Removed member has IsActive = false
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = removedMemberId, Role = "Member", IsActive = false });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = otherMemberId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var balanceRepo = new MockBalanceRepository();
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            var createRes = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = removedMemberId,
                ToUserId = otherMemberId,
                Amount = 100m
            }, removedMemberId, "Removed");

            Assert.False(createRes.Success);
            Assert.Contains("active group member", createRes.Message);
        }

        [Fact]
        public async Task ReminderJob_PendingSettlementOlderThan3Days_CreatesReminderOnce()
        {
            var settlementRepo = new MockSettlementRepository();
            var sId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();

            settlementRepo.Settlements.Add(new SplitSettlement
            {
                Id = sId,
                GroupId = Guid.NewGuid(),
                FromUserId = Guid.NewGuid(),
                ToUserId = receiverId,
                Amount = 250m,
                Status = SplitSettlementStatus.PendingReceiver,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                IsDeleted = false
            });

            var threshold = DateTime.UtcNow.AddDays(-3);
            var needingReminder = await settlementRepo.GetSettlementsNeedingReminderAsync(threshold);

            Assert.Single(needingReminder);
            Assert.Equal(sId, needingReminder[0].Id);

            // Add reminder notification
            var notif = new SplitNotification
            {
                Id = Guid.NewGuid(),
                UserId = receiverId,
                Type = "SettlementReminder",
                ReferenceId = sId,
                Title = "Settlement Reminder",
                Message = "Please confirm receipt",
                CreatedAt = DateTime.UtcNow
            };

            var added = await settlementRepo.AddReminderNotificationAsync(notif);
            Assert.True(added);

            // Subsequent check returns empty (no duplicate reminders)
            var needingReminderAfter = await settlementRepo.GetSettlementsNeedingReminderAsync(threshold);
            Assert.Empty(needingReminderAfter);

            // Re-adding same notification fails atomically
            var addedAgain = await settlementRepo.AddReminderNotificationAsync(notif);
            Assert.False(addedAgain);
        }

        #endregion

        #region 5. Security & Isolation

        [Fact]
        public async Task Security_NonPartyMemberCannotConfirmOrCancelSettlement()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var strangerId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = strangerId, Role = "Member", IsActive = true });

            var settlementRepo = new MockSettlementRepository();
            var sId = Guid.NewGuid();
            settlementRepo.Settlements.Add(new SplitSettlement
            {
                Id = sId,
                GroupId = groupId,
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 100m,
                Status = SplitSettlementStatus.PendingReceiver,
                IsDeleted = false
            });

            var balanceRepo = new MockBalanceRepository();
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Stranger tries to confirm
            var confirmRes = await service.ConfirmSettlementAsync(groupId, sId, strangerId, "Stranger");
            Assert.False(confirmRes.Success);
            Assert.Contains("Only the recipient", confirmRes.Message);

            // Stranger tries to cancel
            var cancelRes = await service.CancelSettlementAsync(groupId, sId, strangerId, "Stranger");
            Assert.False(cancelRes.Success);
            Assert.Contains("Only the payer", cancelRes.Message);
        }

        #endregion
    }
}
