using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.SplitExpenses;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class Change02InvitationNotificationTests
    {
        private class TestSplitGroupRepository : ISplitGroupRepository
        {
            public List<SplitGroup> Groups { get; } = new();
            public List<SplitGroupMember> Members { get; } = new();
            public List<User> Users { get; } = new();
            public List<SplitGroupInvitation> Invitations { get; } = new();
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
                Task.FromResult(new List<SplitGroupMemberDto>());

            public Task<SplitGroup> CreateGroupWithMemberAndActivityAsync(SplitGroup group, Guid ownerId, string ownerName)
            {
                Groups.Add(group);
                Members.Add(new SplitGroupMember { GroupId = group.Id, UserId = ownerId, Role = "Owner", IsActive = true });
                return Task.FromResult(group);
            }

            public Task<SplitGroup> CreateGroupWithMemberAndInvitationsAsync(
                SplitGroup group,
                Guid ownerId,
                string ownerName,
                List<SplitGroupInvitation> invitations,
                List<SplitGroupActivity> activities)
            {
                Groups.Add(group);
                Members.Add(new SplitGroupMember { GroupId = group.Id, UserId = ownerId, Role = "Owner", IsActive = true });
                if (invitations != null) Invitations.AddRange(invitations);
                return Task.FromResult(group);
            }

            public Task<List<SplitUserSearchResultDto>> SearchUsersAsync(string query, Guid currentUserId, int limit = 10)
            {
                var list = Users.Where(u => u.Id != currentUserId &&
                    ((u.Email != null && u.Email.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                     (u.FirstName != null && u.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase))))
                    .Take(limit)
                    .Select(u => new SplitUserSearchResultDto
                    {
                        UserId = u.Id,
                        FullName = $"{u.FirstName} {u.LastName}".Trim(),
                        Email = u.Email ?? string.Empty
                    }).ToList();
                return Task.FromResult(list);
            }

            public Task<bool> UpdateGroupAsync(SplitGroup group, Guid userId, string userName) => Task.FromResult(true);
            public Task<bool> SoftDeleteGroupAsync(Guid groupId, Guid userId, string userName) => Task.FromResult(true);
            public Task<bool> HasFinancialRecordsAsync(Guid groupId) => Task.FromResult(false);

            public Task<User?> GetUserByEmailOrIdAsync(string? email, Guid? userId)
            {
                return Task.FromResult(Users.FirstOrDefault(u =>
                    (!string.IsNullOrEmpty(email) && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                    (userId.HasValue && u.Id == userId.Value)));
            }

            public Task<SplitGroupInvitation?> GetInvitationByIdAsync(Guid invitationId) =>
                Task.FromResult(Invitations.FirstOrDefault(i => i.Id == invitationId));

            public Task<SplitGroupInvitation?> GetInvitationByTokenAsync(string token) =>
                Task.FromResult(Invitations.FirstOrDefault(i => i.InvitationToken == token || i.TokenHash == token));

            public Task<SplitGroupInvitation?> GetPendingInvitationByEmailAsync(Guid groupId, string email) =>
                Task.FromResult(Invitations.FirstOrDefault(i =>
                    i.GroupId == groupId &&
                    i.Status == "Pending" &&
                    string.Equals(i.InvitedEmail, email, StringComparison.OrdinalIgnoreCase)));

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId) =>
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByUserAsync(Guid userId, string userEmail) =>
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<SplitGroupInvitation> CreateInvitationAsync(SplitGroupInvitation invitation, string inviterName, string groupName)
            {
                Invitations.Add(invitation);
                return Task.FromResult(invitation);
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
                var list = Invitations.Where(i =>
                    string.Equals(i.InvitedEmail, email, StringComparison.OrdinalIgnoreCase) &&
                    !i.InvitedUserId.HasValue &&
                    i.Status == "Pending").ToList();

                foreach (var inv in list)
                {
                    inv.InvitedUserId = userId;
                }
                return Task.FromResult(list);
            }

            public Task<bool> RevokeInvitationAsync(Guid invitationId, Guid revokedByUserId, string revokerName, Guid groupId) =>
                Task.FromResult(true);

            public Task<bool> AcceptInvitationAsync(Guid invitationId, Guid userId, string userName)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite == null || invite.Status != "Pending") return Task.FromResult(false);
                invite.Status = "Accepted";
                invite.InvitedUserId = userId;
                Members.Add(new SplitGroupMember { GroupId = invite.GroupId, UserId = userId, Role = "Member", IsActive = true });
                return Task.FromResult(true);
            }

            public Task<bool> RejectInvitationAsync(Guid invitationId, Guid userId, string userName)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite == null || invite.Status != "Pending") return Task.FromResult(false);
                invite.Status = "Rejected";
                return Task.FromResult(true);
            }

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

        private class TestSplitNotificationRepository : ISplitNotificationRepository
        {
            public List<SplitNotification> Notifications { get; } = new();

            public Task<PagedSplitNotificationResultDto> GetNotificationsByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
            {
                var userNotifs = Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).ToList();
                var dtos = userNotifs.Select(n => new SplitNotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Type = n.Type,
                    GroupId = n.GroupId,
                    ReferenceId = n.ReferenceId,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                }).ToList();

                return Task.FromResult(new PagedSplitNotificationResultDto
                {
                    Items = dtos,
                    TotalCount = dtos.Count,
                    UnreadCount = dtos.Count(d => !d.IsRead),
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }

            public Task<int> GetUnreadCountAsync(Guid userId) =>
                Task.FromResult(Notifications.Count(n => n.UserId == userId && !n.IsRead));

            public Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
            {
                var n = Notifications.FirstOrDefault(x => x.Id == notificationId && x.UserId == userId);
                if (n == null) return Task.FromResult(false);
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }

            public Task<int> MarkAllAsReadAsync(Guid userId)
            {
                var list = Notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
                foreach (var n in list)
                {
                    n.IsRead = true;
                    n.ReadAt = DateTime.UtcNow;
                }
                return Task.FromResult(list.Count);
            }

            public Task<SplitNotification> CreateNotificationAsync(SplitNotification notification)
            {
                Notifications.Add(notification);
                return Task.FromResult(notification);
            }

            public Task<bool> MarkInvitationNotificationAsReadAsync(Guid invitationId, Guid userId)
            {
                var n = Notifications.FirstOrDefault(x => x.ReferenceId == invitationId && x.UserId == userId && !x.IsRead);
                if (n != null)
                {
                    n.IsRead = true;
                    n.ReadAt = DateTime.UtcNow;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        [Fact]
        public async Task Invite_ExistingUser_CreatesInvitationAndInAppNotification()
        {
            var repo = new TestSplitGroupRepository();
            var notifRepo = new TestSplitNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var group = new SplitGroup { Id = Guid.NewGuid(), Name = "Trip Group", OwnerId = ownerId };
            await repo.CreateGroupWithMemberAndActivityAsync(group, ownerId, "Owner");

            var existingUser = new User { Id = Guid.NewGuid(), Email = "friend@example.com", FirstName = "Friend", LastName = "User" };
            repo.Users.Add(existingUser);

            var result = await service.InviteMemberAsync(
                group.Id,
                new InviteSplitGroupMemberDto { Email = "friend@example.com" },
                ownerId,
                "Owner");

            Assert.True(result.Success);
            Assert.Equal(existingUser.Id, result.Data!.InvitedUserId);
            Assert.Equal("friend@example.com", result.Data.InvitedEmail);

            // Verify in-app notification was created for existing user
            var userNotifs = await notifRepo.GetNotificationsByUserAsync(existingUser.Id);
            Assert.Single(userNotifs.Items);
            Assert.Equal("GroupInvitationReceived", userNotifs.Items[0].Type);
            Assert.False(userNotifs.Items[0].IsRead);
            Assert.Equal(1, await notifRepo.GetUnreadCountAsync(existingUser.Id));
        }

        [Fact]
        public async Task Invite_UnregisteredUser_CreatesPendingInvitationWithNullUserId()
        {
            var repo = new TestSplitGroupRepository();
            var notifRepo = new TestSplitNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var group = new SplitGroup { Id = Guid.NewGuid(), Name = "Colleague Lunch", OwnerId = ownerId };
            await repo.CreateGroupWithMemberAndActivityAsync(group, ownerId, "Owner");

            var result = await service.InviteMemberAsync(
                group.Id,
                new InviteSplitGroupMemberDto { Email = "unregistered@external.com" },
                ownerId,
                "Owner");

            Assert.True(result.Success);
            Assert.Null(result.Data!.InvitedUserId);
            Assert.Equal("unregistered@external.com", result.Data.InvitedEmail);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.InvitationToken));
        }

        [Fact]
        public async Task Invite_Self_ReturnsFailure()
        {
            var repo = new TestSplitGroupRepository();
            var notifRepo = new TestSplitNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var group = new SplitGroup { Id = Guid.NewGuid(), Name = "Trip Group", OwnerId = ownerId };
            await repo.CreateGroupWithMemberAndActivityAsync(group, ownerId, "Owner");

            var ownerUser = new User { Id = ownerId, Email = "owner@test.com" };
            repo.Users.Add(ownerUser);

            var result = await service.InviteMemberAsync(
                group.Id,
                new InviteSplitGroupMemberDto { Email = "owner@test.com" },
                ownerId,
                "Owner");

            Assert.False(result.Success);
            Assert.Contains("cannot invite yourself", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Invite_DuplicateActivePending_ReturnsFailure()
        {
            var repo = new TestSplitGroupRepository();
            var notifRepo = new TestSplitNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var group = new SplitGroup { Id = Guid.NewGuid(), Name = "Weekend", OwnerId = ownerId };
            await repo.CreateGroupWithMemberAndActivityAsync(group, ownerId, "Owner");

            await service.InviteMemberAsync(group.Id, new InviteSplitGroupMemberDto { Email = "guest@test.com" }, ownerId, "Owner");

            // Attempt duplicate invite
            var dupResult = await service.InviteMemberAsync(group.Id, new InviteSplitGroupMemberDto { Email = "guest@test.com" }, ownerId, "Owner");
            Assert.False(dupResult.Success);
            Assert.Contains("active pending invitation has already been sent", dupResult.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResendInvitation_ThrottlesWithinOneMinute()
        {
            var repo = new TestSplitGroupRepository();
            var notifRepo = new TestSplitNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var group = new SplitGroup { Id = Guid.NewGuid(), Name = "Weekend", OwnerId = ownerId };
            await repo.CreateGroupWithMemberAndActivityAsync(group, ownerId, "Owner");

            var inviteResult = await service.InviteMemberAsync(group.Id, new InviteSplitGroupMemberDto { Email = "guest@test.com" }, ownerId, "Owner");
            var inviteId = inviteResult.Data!.Id;

            // Immediate resend should be throttled
            var resend1 = await service.ResendInvitationAsync(group.Id, inviteId, ownerId, "Owner");
            Assert.False(resend1.Success);
            Assert.Contains("wait", resend1.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LinkPendingInvitationsOnAuth_LinksPendingInvitesAndCreatesNotifications()
        {
            var repo = new TestSplitGroupRepository();
            var notifRepo = new TestSplitNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var group = new SplitGroup { Id = Guid.NewGuid(), Name = "Roommates", OwnerId = ownerId };
            await repo.CreateGroupWithMemberAndActivityAsync(group, ownerId, "Owner");

            // Invite unregistered user
            var email = "newuser@example.com";
            await service.InviteMemberAsync(group.Id, new InviteSplitGroupMemberDto { Email = email }, ownerId, "Owner");

            // Now user registers with that email
            var newUserId = Guid.NewGuid();
            var linkedCount = await service.LinkPendingInvitationsOnAuthAsync(newUserId, email);

            Assert.Equal(1, linkedCount);

            var invite = repo.Invitations.First();
            Assert.Equal(newUserId, invite.InvitedUserId);

            // Check notification created
            var notifs = await notifRepo.GetNotificationsByUserAsync(newUserId);
            Assert.Single(notifs.Items);
            Assert.Equal("GroupInvitationReceived", notifs.Items[0].Type);
        }

        [Fact]
        public async Task AcceptInvitation_MarksNotificationRead_AndNotifiesOwner()
        {
            var repo = new TestSplitGroupRepository();
            var notifRepo = new TestSplitNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var group = new SplitGroup { Id = Guid.NewGuid(), Name = "Trip", OwnerId = ownerId };
            await repo.CreateGroupWithMemberAndActivityAsync(group, ownerId, "Owner");

            var recipient = new User { Id = Guid.NewGuid(), Email = "recip@test.com", FirstName = "Recip" };
            repo.Users.Add(recipient);

            var inviteResult = await service.InviteMemberAsync(group.Id, new InviteSplitGroupMemberDto { Email = recipient.Email }, ownerId, "Owner");
            var inviteId = inviteResult.Data!.Id;

            Assert.Equal(1, await notifRepo.GetUnreadCountAsync(recipient.Id));

            // Recipient accepts
            var acceptResult = await service.AcceptInvitationAsync(inviteId, recipient.Id, recipient.Email, "Recip");
            Assert.True(acceptResult.Success);

            // Recipient's invitation notification should now be read
            var recipNotifs = await notifRepo.GetNotificationsByUserAsync(recipient.Id);
            Assert.True(recipNotifs.Items[0].IsRead);
            Assert.Equal(0, await notifRepo.GetUnreadCountAsync(recipient.Id));

            // Owner should now have an "InvitationAccepted" notification
            var ownerNotifs = await notifRepo.GetNotificationsByUserAsync(ownerId);
            Assert.Single(ownerNotifs.Items);
            Assert.Equal("InvitationAccepted", ownerNotifs.Items[0].Type);
            Assert.False(ownerNotifs.Items[0].IsRead);
        }
    }
}
