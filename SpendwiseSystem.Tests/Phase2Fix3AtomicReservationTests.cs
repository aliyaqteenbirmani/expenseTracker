using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.SplitExpenses;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class Phase2Fix3AtomicReservationTests
    {
        #region Mocks

        private class MockGroupRepository : ISplitGroupRepository
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
                Task.FromResult(new List<SplitGroupMemberDto>());
            public Task<SplitGroup> CreateGroupWithMemberAndActivityAsync(SplitGroup group, Guid ownerId, string ownerName) =>
                Task.FromResult(group);
            public Task<SplitGroup> CreateGroupWithMemberAndInvitationsAsync(
                SplitGroup group, Guid ownerId, string ownerName, List<SplitGroupInvitation> invitations, List<SplitGroupActivity> activities) =>
                Task.FromResult(group);
            public Task<List<SplitUserSearchResultDto>> SearchUsersAsync(string query, Guid currentUserId, int limit = 10) =>
                Task.FromResult(new List<SplitUserSearchResultDto>());
            public Task<bool> UpdateGroupAsync(SplitGroup group, Guid userId, string userName) => Task.FromResult(true);
            public Task<bool> SoftDeleteGroupAsync(Guid groupId, Guid userId, string userName) => Task.FromResult(true);
            public Task<bool> HasFinancialRecordsAsync(Guid groupId) => Task.FromResult(false);
            public Task<int> GetPendingSettlementsCountAsync(Guid groupId) => Task.FromResult(0);
            public Task<User?> GetUserByEmailOrIdAsync(string? email, Guid? userId) => Task.FromResult<User?>(null);
            public Task<SplitGroupInvitation?> GetInvitationByIdAsync(Guid invitationId) => Task.FromResult<SplitGroupInvitation?>(null);
            public Task<SplitGroupInvitation?> GetInvitationByTokenAsync(string token) => Task.FromResult<SplitGroupInvitation?>(null);
            public Task<SplitGroupInvitation?> GetPendingInvitationByEmailAsync(Guid groupId, string email) => Task.FromResult<SplitGroupInvitation?>(null);
            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId) => Task.FromResult(new List<SplitGroupInvitationDto>());
            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByUserAsync(Guid userId, string userEmail) => Task.FromResult(new List<SplitGroupInvitationDto>());
            public Task<SplitGroupInvitation> CreateInvitationAsync(SplitGroupInvitation invitation, string inviterName, string groupName) => Task.FromResult(invitation);
            public Task<bool> UpdateInvitationResendAsync(Guid invitationId) => Task.FromResult(true);
            public Task<List<SplitGroupInvitation>> LinkPendingInvitationsForUserAsync(Guid userId, string email) => Task.FromResult(new List<SplitGroupInvitation>());
            public Task<bool> RevokeInvitationAsync(Guid invitationId, Guid revokedByUserId, string revokerName, Guid groupId) => Task.FromResult(true);
            public Task<bool> AcceptInvitationAsync(Guid invitationId, Guid userId, string userName) => Task.FromResult(true);
            public Task<bool> RejectInvitationAsync(Guid invitationId, Guid userId, string userName) => Task.FromResult(true);
            public Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName) => Task.FromResult(true);
            public Task<List<SplitGroupActivityDto>> GetGroupActivitiesAsync(Guid groupId, int limit = 50) => Task.FromResult(new List<SplitGroupActivityDto>());
            public Task<PagedGroupActivityResultDto> GetPagedGroupActivitiesAsync(Guid groupId, SplitGroupActivityQueryParameters parameters) => Task.FromResult(new PagedGroupActivityResultDto());
            public Task<SplitGroupActivityDto?> GetActivityByIdAsync(Guid groupId, Guid activityId) => Task.FromResult<SplitGroupActivityDto?>(null);
            public Task LogActivityAsync(Guid groupId, Guid userId, string activityType, string description, Guid? referenceId = null) => Task.CompletedTask;
        }

        private class MockBalanceRepository : ISplitBalanceRepository
        {
            public List<MemberBalanceDto> Balances { get; set; } = new();

            public Task<List<MemberBalanceDto>> GetGroupBalancesAsync(Guid groupId) =>
                Task.FromResult(Balances.Select(b => new MemberBalanceDto
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
                }).ToList());

            public Task<MemberBalanceDto?> GetMemberBalanceAsync(Guid groupId, Guid userId)
            {
                var b = Balances.FirstOrDefault(x => x.UserId == userId);
                if (b == null) return Task.FromResult<MemberBalanceDto?>(null);
                return Task.FromResult<MemberBalanceDto?>(new MemberBalanceDto
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
                });
            }

            public Task<GroupFinancialSummaryDto?> GetGroupFinancialSummaryAsync(Guid groupId, Guid currentUserId) =>
                Task.FromResult<GroupFinancialSummaryDto?>(null);
        }

        private class MockSettlementRepository : ISplitSettlementRepository
        {
            private readonly object _lock = new();
            public List<SplitSettlement> Settlements { get; } = new();
            public List<SplitNotification> Notifications { get; } = new();
            public MockBalanceRepository? BalanceRepo { get; set; }

            public Task<SplitSettlement?> GetSettlementByIdAsync(Guid settlementId)
            {
                lock (_lock)
                {
                    return Task.FromResult(Settlements.FirstOrDefault(s => s.Id == settlementId && !s.IsDeleted));
                }
            }

            public Task<SplitSettlementDto?> GetSettlementDetailsAsync(Guid settlementId, Guid groupId)
            {
                lock (_lock)
                {
                    var s = Settlements.FirstOrDefault(x => x.Id == settlementId && x.GroupId == groupId && !x.IsDeleted);
                    return Task.FromResult(s == null ? null : MapDto(s));
                }
            }

            public Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId)
            {
                lock (_lock)
                {
                    return Task.FromResult(Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted).Select(MapDto).ToList());
                }
            }

            public Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId, Guid currentUserId, bool isOwner)
            {
                lock (_lock)
                {
                    return Task.FromResult(Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted && (isOwner || s.FromUserId == currentUserId || s.ToUserId == currentUserId))
                        .Select(MapDto).ToList());
                }
            }

            public Task<List<SplitSettlementDto>> GetPendingActionsAsync(Guid groupId, Guid currentUserId, bool isOwner)
            {
                lock (_lock)
                {
                    return Task.FromResult(Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted &&
                        ((s.Status == SplitSettlementStatus.PendingReceiver && s.ToUserId == currentUserId) ||
                         (s.Status == SplitSettlementStatus.PendingOwner && isOwner)))
                        .Select(MapDto).ToList());
                }
            }

            public Task<List<SplitSettlement>> GetPendingSettlementsAsync(Guid groupId)
            {
                lock (_lock)
                {
                    return Task.FromResult(Settlements.Where(s => s.GroupId == groupId && !s.IsDeleted &&
                        (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner)).ToList());
                }
            }

            public Task<decimal> GetPendingPayerToReceiverAmountAsync(Guid groupId, Guid fromUserId, Guid toUserId, Guid? excludeSettlementId = null)
            {
                lock (_lock)
                {
                    var sum = Settlements.Where(s => s.GroupId == groupId && s.FromUserId == fromUserId && s.ToUserId == toUserId
                        && !s.IsDeleted && (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner)
                        && (excludeSettlementId == null || s.Id != excludeSettlementId.Value))
                        .Sum(s => s.Amount);
                    return Task.FromResult(sum);
                }
            }

            public Task<decimal> GetPendingPaidByUserAmountAsync(Guid groupId, Guid userId, Guid? excludeSettlementId = null)
            {
                lock (_lock)
                {
                    var sum = Settlements.Where(s => s.GroupId == groupId && s.FromUserId == userId && !s.IsDeleted &&
                        (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner) &&
                        (excludeSettlementId == null || s.Id != excludeSettlementId.Value))
                        .Sum(s => s.Amount);
                    return Task.FromResult(sum);
                }
            }

            public Task<decimal> GetPendingReceivedByUserAmountAsync(Guid groupId, Guid userId, Guid? excludeSettlementId = null)
            {
                lock (_lock)
                {
                    var sum = Settlements.Where(s => s.GroupId == groupId && s.ToUserId == userId && !s.IsDeleted &&
                        (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner) &&
                        (excludeSettlementId == null || s.Id != excludeSettlementId.Value))
                        .Sum(s => s.Amount);
                    return Task.FromResult(sum);
                }
            }

            public Task<SplitSettlementDto> CreateSettlementAsync(SplitSettlement settlement, List<SplitNotification>? notifications = null)
            {
                lock (_lock)
                {
                    Settlements.Add(settlement);
                    if (notifications != null && notifications.Count > 0)
                        Notifications.AddRange(notifications);
                    return Task.FromResult(MapDto(settlement));
                }
            }

            public Task<SettlementCreationResult> CreateSettlementAtomicAsync(SplitSettlement settlement, List<SplitNotification>? notifications = null)
            {
                lock (_lock)
                {
                    if (BalanceRepo != null)
                    {
                        var sender = BalanceRepo.Balances.FirstOrDefault(b => b.UserId == settlement.FromUserId);
                        var receiver = BalanceRepo.Balances.FirstOrDefault(b => b.UserId == settlement.ToUserId);

                        if (sender == null || sender.NetBalance >= -0.005m)
                            return Task.FromResult(SettlementCreationResult.Failed("Sender currently has no outstanding debt to settle."));
                        if (receiver == null || receiver.NetBalance <= 0.005m)
                            return Task.FromResult(SettlementCreationResult.Failed("Receiver is not currently owed any money in this group."));

                        decimal senderDebt = Math.Abs(sender.NetBalance);
                        decimal receiverCredit = receiver.NetBalance;

                        decimal pendingPaid = Settlements.Where(s => s.GroupId == settlement.GroupId && s.FromUserId == settlement.FromUserId &&
                            !s.IsDeleted && (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner))
                            .Sum(s => s.Amount);

                        if (settlement.Amount + pendingPaid > senderDebt + 0.01m)
                        {
                            decimal remaining = Math.Max(0m, senderDebt - pendingPaid);
                            string note = pendingPaid > 0 ? $" (plus Rs. {pendingPaid:N2} already pending)" : "";
                            return Task.FromResult(SettlementCreationResult.Failed($"Settlement amount of Rs. {settlement.Amount:N2}{note} exceeds remaining debt of Rs. {remaining:N2}."));
                        }

                        decimal pendingRecv = Settlements.Where(s => s.GroupId == settlement.GroupId && s.ToUserId == settlement.ToUserId &&
                            !s.IsDeleted && (s.Status == SplitSettlementStatus.PendingReceiver || s.Status == SplitSettlementStatus.PendingOwner))
                            .Sum(s => s.Amount);

                        if (settlement.Amount + pendingRecv > receiverCredit + 0.01m)
                        {
                            decimal remaining = Math.Max(0m, receiverCredit - pendingRecv);
                            string note = pendingRecv > 0 ? $" (plus Rs. {pendingRecv:N2} already pending)" : "";
                            return Task.FromResult(SettlementCreationResult.Failed($"Settlement amount of Rs. {settlement.Amount:N2}{note} exceeds receiver's remaining credit of Rs. {remaining:N2}."));
                        }
                    }

                    Settlements.Add(settlement);
                    if (notifications != null && notifications.Count > 0)
                        Notifications.AddRange(notifications);

                    return Task.FromResult(SettlementCreationResult.Succeeded(MapDto(settlement)));
                }
            }

            public Task<int> ConfirmByReceiverAsync(Guid settlementId, string newStatus, Guid receiverUserId, List<SplitNotification>? notifications = null)
            {
                lock (_lock)
                {
                    var s = Settlements.FirstOrDefault(x => x.Id == settlementId && !x.IsDeleted);
                    if (s == null) return Task.FromResult(0);

                    if (BalanceRepo != null)
                    {
                        var sender = BalanceRepo.Balances.FirstOrDefault(b => b.UserId == s.FromUserId);
                        var receiver = BalanceRepo.Balances.FirstOrDefault(b => b.UserId == s.ToUserId);
                        if (sender == null || sender.NetBalance >= -0.005m || receiver == null || receiver.NetBalance <= 0.005m)
                            return Task.FromResult(-1);

                        decimal senderDebt = Math.Abs(sender.NetBalance);
                        decimal receiverCredit = receiver.NetBalance;

                        decimal otherPaid = Settlements.Where(x => x.GroupId == s.GroupId && x.FromUserId == s.FromUserId &&
                            x.Id != s.Id && !x.IsDeleted && (x.Status == SplitSettlementStatus.PendingReceiver || x.Status == SplitSettlementStatus.PendingOwner))
                            .Sum(x => x.Amount);
                        decimal otherRecv = Settlements.Where(x => x.GroupId == s.GroupId && x.ToUserId == s.ToUserId &&
                            x.Id != s.Id && !x.IsDeleted && (x.Status == SplitSettlementStatus.PendingReceiver || x.Status == SplitSettlementStatus.PendingOwner))
                            .Sum(x => x.Amount);

                        if (s.Amount + otherPaid > senderDebt + 0.01m || s.Amount + otherRecv > receiverCredit + 0.01m)
                            return Task.FromResult(-1);
                    }

                    s.Status = newStatus;
                    s.ReceiverActionBy = receiverUserId;
                    s.ReceiverActionAt = DateTime.UtcNow;
                    return Task.FromResult(1);
                }
            }

            public Task<int> ApproveByOwnerAsync(Guid settlementId, Guid ownerUserId, List<SplitNotification>? notifications = null)
            {
                lock (_lock)
                {
                    var s = Settlements.FirstOrDefault(x => x.Id == settlementId && !x.IsDeleted);
                    if (s == null) return Task.FromResult(0);

                    if (BalanceRepo != null)
                    {
                        var sender = BalanceRepo.Balances.FirstOrDefault(b => b.UserId == s.FromUserId);
                        var receiver = BalanceRepo.Balances.FirstOrDefault(b => b.UserId == s.ToUserId);
                        if (sender == null || sender.NetBalance >= -0.005m || receiver == null || receiver.NetBalance <= 0.005m)
                            return Task.FromResult(-1);

                        decimal senderDebt = Math.Abs(sender.NetBalance);
                        decimal receiverCredit = receiver.NetBalance;

                        decimal otherPaid = Settlements.Where(x => x.GroupId == s.GroupId && x.FromUserId == s.FromUserId &&
                            x.Id != s.Id && !x.IsDeleted && (x.Status == SplitSettlementStatus.PendingReceiver || x.Status == SplitSettlementStatus.PendingOwner))
                            .Sum(x => x.Amount);
                        decimal otherRecv = Settlements.Where(x => x.GroupId == s.GroupId && x.ToUserId == s.ToUserId &&
                            x.Id != s.Id && !x.IsDeleted && (x.Status == SplitSettlementStatus.PendingReceiver || x.Status == SplitSettlementStatus.PendingOwner))
                            .Sum(x => x.Amount);

                        if (s.Amount + otherPaid > senderDebt + 0.01m || s.Amount + otherRecv > receiverCredit + 0.01m)
                            return Task.FromResult(-1);
                    }

                    s.Status = SplitSettlementStatus.Confirmed;
                    s.OwnerActionBy = ownerUserId;
                    s.OwnerActionAt = DateTime.UtcNow;
                    return Task.FromResult(1);
                }
            }

            public Task<int> DeclineByReceiverAsync(Guid settlementId, string? reason, Guid receiverUserId, List<SplitNotification>? notifications = null) => Task.FromResult(1);
            public Task<int> DeclineByOwnerAsync(Guid settlementId, string? reason, Guid ownerUserId, List<SplitNotification>? notifications = null) => Task.FromResult(1);
            public Task<int> CancelByPayerAsync(Guid settlementId, Guid payerUserId, List<SplitNotification>? notifications = null) => Task.FromResult(1);
            public Task<bool> ReverseConfirmedSettlementAsync(Guid settlementId, Guid groupId, Guid ownerUserId, List<SplitNotification>? notifications = null) => Task.FromResult(true);
            public Task<List<SplitSettlementDto>> GetSettlementsNeedingReminderAsync(DateTime olderThan) => Task.FromResult(new List<SplitSettlementDto>());
            public Task<bool> AddReminderNotificationAsync(SplitNotification reminderNotification) => Task.FromResult(true);
            public Task<SplitSettlementDto> CreateSettlementWithActivityAsync(SplitSettlement settlement, string creatorName, string groupName) => CreateSettlementAsync(settlement);
            public Task<SplitSettlementDto?> UpdateSettlementWithActivityAsync(SplitSettlement settlement, string editorName, string groupName) => Task.FromResult<SplitSettlementDto?>(MapDto(settlement));
            public Task<bool> SoftDeleteSettlementWithActivityAsync(Guid settlementId, Guid groupId, Guid deletedByUserId, string deletedByName) => Task.FromResult(true);

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
        public async Task Reservation_PayerOwing1000_Submits600ToA_Then600ToB_SecondRejected_400Accepted()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var receiverAId = Guid.NewGuid();
            var receiverBId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverAId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverBId, Role = "Member", IsActive = true });

            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerId, FullName = "Payer", NetBalance = -1000m },
                    new() { UserId = receiverAId, FullName = "Receiver A", NetBalance = 600m },
                    new() { UserId = receiverBId, FullName = "Receiver B", NetBalance = 600m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = -200m }
                }
            };

            var settlementRepo = new MockSettlementRepository { BalanceRepo = balanceRepo };
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // 1. Submit 600 to A -> Succeeds (600 <= 1000)
            var res1 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverAId,
                Amount = 600m
            }, payerId, "Payer");
            Assert.True(res1.Success);

            // 2. Submit 600 to B -> Rejected (600 already pending, 600 + 600 = 1200 > 1000)
            var res2 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverBId,
                Amount = 600m
            }, payerId, "Payer");
            Assert.False(res2.Success);
            Assert.Contains("exceeds", res2.Message, StringComparison.OrdinalIgnoreCase);

            // 3. Submit 400 to B -> Accepted (600 pending + 400 = 1000 <= 1000)
            var res3 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverBId,
                Amount = 400m
            }, payerId, "Payer");
            Assert.True(res3.Success);
        }

        [Fact]
        public async Task Reservation_ReceiverOwed500_ReceivesTwoRequestsOf300_SecondRejected()
        {
            var ownerId = Guid.NewGuid();
            var payerAId = Guid.NewGuid();
            var payerBId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerAId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerBId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverId, Role = "Member", IsActive = true });

            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerAId, FullName = "Payer A", NetBalance = -500m },
                    new() { UserId = payerBId, FullName = "Payer B", NetBalance = -500m },
                    new() { UserId = receiverId, FullName = "Receiver", NetBalance = 500m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = 500m }
                }
            };

            var settlementRepo = new MockSettlementRepository { BalanceRepo = balanceRepo };
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // 1. Payer A submits 300 to Receiver -> Succeeds (300 <= 500)
            var res1 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerAId,
                ToUserId = receiverId,
                Amount = 300m
            }, payerAId, "Payer A");
            Assert.True(res1.Success);

            // 2. Payer B submits 300 to Receiver -> Rejected because Receiver only has 200 remaining credit!
            var res2 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerBId,
                ToUserId = receiverId,
                Amount = 300m
            }, payerBId, "Payer B");
            Assert.False(res2.Success);
            Assert.Contains("exceeds", res2.Message, StringComparison.OrdinalIgnoreCase);

            // 3. Payer B submits 200 to Receiver -> Accepted!
            var res3 = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerBId,
                ToUserId = receiverId,
                Amount = 200m
            }, payerBId, "Payer B");
            Assert.True(res3.Success);
        }

        [Fact]
        public async Task Atomicity_ConcurrentSubmissionsExceedingDebt_ExactlyOneSucceeds()
        {
            var ownerId = Guid.NewGuid();
            var payerId = Guid.NewGuid();
            var receiverAId = Guid.NewGuid();
            var receiverBId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            var groupRepo = new MockGroupRepository();
            groupRepo.Groups.Add(new SplitGroup { Id = groupId, Name = "Trip", OwnerId = ownerId, IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = payerId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverAId, Role = "Member", IsActive = true });
            groupRepo.Members.Add(new SplitGroupMember { GroupId = groupId, UserId = receiverBId, Role = "Member", IsActive = true });

            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerId, FullName = "Payer", NetBalance = -1000m },
                    new() { UserId = receiverAId, FullName = "Receiver A", NetBalance = 1000m },
                    new() { UserId = receiverBId, FullName = "Receiver B", NetBalance = 1000m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = -1000m }
                }
            };

            var settlementRepo = new MockSettlementRepository { BalanceRepo = balanceRepo };
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // Launch two concurrent 700m requests simultaneously (700 + 700 = 1400 > 1000)
            var barrier = new Barrier(2);
            var task1 = Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await service.RecordSettlementAsync(groupId, new RecordSettlementDto
                {
                    FromUserId = payerId,
                    ToUserId = receiverAId,
                    Amount = 700m
                }, payerId, "Payer");
            });

            var task2 = Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await service.RecordSettlementAsync(groupId, new RecordSettlementDto
                {
                    FromUserId = payerId,
                    ToUserId = receiverBId,
                    Amount = 700m
                }, payerId, "Payer");
            });

            var results = await Task.WhenAll(task1, task2);
            int successes = results.Count(r => r.Success);
            int failures = results.Count(r => !r.Success);

            // Exactly one must succeed and one must fail
            Assert.Equal(1, successes);
            Assert.Equal(1, failures);
        }

        [Fact]
        public async Task Recheck_ConfirmAndApproveFail_IfDebtReducedByExpenseChange()
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

            var balanceRepo = new MockBalanceRepository
            {
                Balances = new List<MemberBalanceDto>
                {
                    new() { UserId = payerId, FullName = "Payer", NetBalance = -500m },
                    new() { UserId = receiverId, FullName = "Receiver", NetBalance = 500m },
                    new() { UserId = ownerId, FullName = "Owner", NetBalance = 0m }
                }
            };

            var settlementRepo = new MockSettlementRepository { BalanceRepo = balanceRepo };
            var service = new SplitSettlementService(settlementRepo, groupRepo, balanceRepo, new DebtSimplificationService());

            // 1. Payer submits 500m to Receiver
            var submitRes = await service.RecordSettlementAsync(groupId, new RecordSettlementDto
            {
                FromUserId = payerId,
                ToUserId = receiverId,
                Amount = 500m
            }, payerId, "Payer");
            Assert.True(submitRes.Success);
            var settlementId = submitRes.Data!.Id;

            // 2. Someone edits an expense, reducing payer's debt to only 200m!
            balanceRepo.Balances.First(b => b.UserId == payerId).NetBalance = -200m;
            balanceRepo.Balances.First(b => b.UserId == receiverId).NetBalance = 200m;

            // 3. Receiver tries to confirm 500m -> Fails because 500 > 200 remaining debt!
            var confirmRes = await service.ConfirmSettlementAsync(groupId, settlementId, receiverId, "Receiver");
            Assert.False(confirmRes.Success);
            Assert.Contains("exceeds", confirmRes.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
