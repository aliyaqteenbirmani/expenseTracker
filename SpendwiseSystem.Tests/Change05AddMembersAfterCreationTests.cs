using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.EmailService;
using SpendwiseSystem.Application.Services.SplitExpenses;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class Change05AddMembersAfterCreationTests
    {
        private class TestGroupRepository : ISplitGroupRepository
        {
            public List<SplitGroup> Groups = new();
            public List<SplitGroupMember> Members = new();
            public List<SplitGroupInvitation> Invitations = new();
            public List<SplitGroupActivity> Activities = new();
            public List<User> Users = new();

            public Task<SplitGroup?> GetGroupByIdAsync(Guid groupId) =>
                Task.FromResult(Groups.FirstOrDefault(g => g.Id == groupId && !g.IsDeleted));

            public Task<SplitGroupMember?> GetMembershipAsync(Guid groupId, Guid userId) =>
                Task.FromResult(Members.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId));

            public Task<List<SplitGroupSummaryDto>> GetUserGroupsAsync(Guid userId) =>
                Task.FromResult(new List<SplitGroupSummaryDto>());

            public Task<SplitGroupDetailDto?> GetGroupDetailsAsync(Guid groupId, Guid userId) =>
                Task.FromResult<SplitGroupDetailDto?>(null);

            public Task<List<SplitGroupMemberDto>> GetGroupMembersAsync(Guid groupId) =>
                Task.FromResult(Members.Where(m => m.GroupId == groupId && m.IsActive).Select(m => new SplitGroupMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    Role = m.Role,
                    IsActive = m.IsActive
                }).ToList());

            public Task<SplitGroup> CreateGroupWithMemberAndActivityAsync(SplitGroup group, Guid ownerId, string ownerName)
            {
                Groups.Add(group);
                Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = group.Id, UserId = ownerId, Role = "Owner", IsActive = true });
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
                Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = group.Id, UserId = ownerId, Role = "Owner", IsActive = true });
                if (invitations != null) Invitations.AddRange(invitations);
                if (activities != null) Activities.AddRange(activities);
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
                var user = Users.FirstOrDefault(u =>
                    (userId.HasValue && u.Id == userId.Value) ||
                    (!string.IsNullOrWhiteSpace(email) && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));
                return Task.FromResult(user);
            }

            public Task<SplitGroupInvitation?> GetInvitationByIdAsync(Guid invitationId) =>
                Task.FromResult(Invitations.FirstOrDefault(i => i.Id == invitationId));

            public Task<SplitGroupInvitation?> GetInvitationByTokenAsync(string token) =>
                Task.FromResult(Invitations.FirstOrDefault(i => i.InvitationToken == token));

            public Task<SplitGroupInvitation?> GetPendingInvitationByEmailAsync(Guid groupId, string email) =>
                Task.FromResult(Invitations.FirstOrDefault(i => i.GroupId == groupId && string.Equals(i.InvitedEmail, email, StringComparison.OrdinalIgnoreCase) && i.Status == "Pending"));

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId) =>
                Task.FromResult(Invitations.Where(i => i.GroupId == groupId && i.Status == "Pending").Select(i => new SplitGroupInvitationDto
                {
                    Id = i.Id,
                    GroupId = i.GroupId,
                    InvitedEmail = i.InvitedEmail,
                    InvitedUserId = i.InvitedUserId,
                    Status = i.Status
                }).ToList());

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
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }

            public Task<List<SplitGroupInvitation>> LinkPendingInvitationsForUserAsync(Guid userId, string email) =>
                Task.FromResult(new List<SplitGroupInvitation>());

            public Task<bool> RevokeInvitationAsync(Guid invitationId, Guid revokedByUserId, string revokerName, Guid groupId)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite != null && invite.Status == "Pending")
                {
                    invite.Status = "Revoked";
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }

            public Task<bool> AcceptInvitationAsync(Guid invitationId, Guid userId, string userName)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite == null || invite.Status != "Pending") return Task.FromResult(false);

                invite.Status = "Accepted";
                invite.InvitedUserId = userId;

                var existingMember = Members.FirstOrDefault(m => m.GroupId == invite.GroupId && m.UserId == userId);
                if (existingMember != null)
                {
                    existingMember.IsActive = true;
                    existingMember.Role = "Member";
                }
                else
                {
                    Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = invite.GroupId, UserId = userId, Role = "Member", IsActive = true });
                }

                return Task.FromResult(true);
            }

            public Task<bool> RejectInvitationAsync(Guid invitationId, Guid userId, string userName)
            {
                var invite = Invitations.FirstOrDefault(i => i.Id == invitationId);
                if (invite == null || invite.Status != "Pending") return Task.FromResult(false);
                invite.Status = "Rejected";
                return Task.FromResult(true);
            }

            public Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName)
            {
                var m = Members.FirstOrDefault(x => x.GroupId == groupId && x.UserId == userIdToRemove && x.IsActive);
                if (m != null)
                {
                    m.IsActive = false;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
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

        private class TestNotificationRepository : ISplitNotificationRepository
        {
            public List<SplitNotification> Notifications = new();

            public Task<SplitNotification> CreateNotificationAsync(SplitNotification notification)
            {
                Notifications.Add(notification);
                return Task.FromResult(notification);
            }

            public Task<PagedSplitNotificationResultDto> GetNotificationsByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20) =>
                Task.FromResult(new PagedSplitNotificationResultDto());

            public Task<int> GetUnreadCountAsync(Guid userId) =>
                Task.FromResult(Notifications.Count(n => n.UserId == userId && !n.IsRead));

            public Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
            {
                var n = Notifications.FirstOrDefault(x => x.Id == notificationId && x.UserId == userId);
                if (n != null)
                {
                    n.IsRead = true;
                    n.ReadAt = DateTime.UtcNow;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
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

            public Task<int> MarkInvitationNotificationsAsReadAsync(Guid invitationId)
            {
                var list = Notifications.Where(x => x.ReferenceId == invitationId && !x.IsRead).ToList();
                foreach (var n in list)
                {
                    n.IsRead = true;
                    n.ReadAt = DateTime.UtcNow;
                }
                return Task.FromResult(list.Count);
            }
        }

        private class TestEmailService : IEmailService
        {
            public List<EmailMessage> SentEmails = new();
            public Task<ApiResponse<bool>> SendEmailAsync(EmailMessage message)
            {
                SentEmails.Add(message);
                return Task.FromResult(ApiResponse<bool>.SuccessResponse("Sent", true));
            }
        }

        private class TestRealtimeService : ISplitRealtimeService
        {
            public List<(Guid UserId, string EventName, object Payload)> UserEvents = new();
            public List<(string Email, string EventName, object Payload)> EmailEvents = new();

            public Task NotifyUserAsync(Guid userId, string eventName, object payload)
            {
                UserEvents.Add((userId, eventName, payload));
                return Task.CompletedTask;
            }

            public Task NotifyEmailAsync(string email, string eventName, object payload)
            {
                EmailEvents.Add((email, eventName, payload));
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task BatchInvite_NonOwner_ReturnsForbidden403()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var emailService = new TestEmailService();
            var realtime = new TestRealtimeService();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance, emailService, realtime);

            var ownerId = Guid.NewGuid();
            var nonOwnerId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            repo.Groups.Add(new SplitGroup
            {
                Id = groupId,
                Name = "Project Alpha",
                OwnerId = ownerId,
                IsActive = true
            });

            var dto = new BatchInviteMembersDto
            {
                Invitees = new List<CreateGroupInviteeInputDto>
                {
                    new CreateGroupInviteeInputDto { Email = "colleague@example.com" }
                }
            };

            var res = await service.BatchInviteMembersAsync(groupId, dto, nonOwnerId, "Eve");

            Assert.False(res.Success);
            Assert.Equal("FORBIDDEN", res.ErrorCode);
            Assert.Contains("Only the group owner", res.Message);
        }

        [Fact]
        public async Task BatchInvite_InactiveGroup_ReturnsFailure()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var emailService = new TestEmailService();
            var realtime = new TestRealtimeService();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance, emailService, realtime);

            var ownerId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            repo.Groups.Add(new SplitGroup
            {
                Id = groupId,
                Name = "Archived Project",
                OwnerId = ownerId,
                IsActive = false
            });

            var dto = new BatchInviteMembersDto
            {
                Invitees = new List<CreateGroupInviteeInputDto>
                {
                    new CreateGroupInviteeInputDto { Email = "colleague@example.com" }
                }
            };

            var res = await service.BatchInviteMembersAsync(groupId, dto, ownerId, "Alice");

            Assert.False(res.Success);
            Assert.Contains("inactive group", res.Message);
        }

        [Fact]
        public async Task BatchInvite_OwnerCanInviteMultipleUsers_ReusesNotificationAndEmail()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var emailService = new TestEmailService();
            var realtime = new TestRealtimeService();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance, emailService, realtime);

            var ownerId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var regUserId = Guid.NewGuid();

            repo.Groups.Add(new SplitGroup
            {
                Id = groupId,
                Name = "Ski Trip",
                OwnerId = ownerId,
                IsActive = true
            });
            repo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });

            repo.Users.Add(new User
            {
                Id = regUserId,
                FirstName = "David",
                LastName = "Miller",
                Email = "david@example.com"
            });

            var dto = new BatchInviteMembersDto
            {
                Invitees = new List<CreateGroupInviteeInputDto>
                {
                    new CreateGroupInviteeInputDto { UserId = regUserId, Name = "David Miller", Email = "david@example.com" },
                    new CreateGroupInviteeInputDto { Email = "sarah.new@example.com", Name = "Sarah" }
                }
            };

            var res = await service.BatchInviteMembersAsync(groupId, dto, ownerId, "Alice");

            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.Equal(2, res.Data.TotalCount);
            Assert.Equal(2, res.Data.SuccessCount);
            Assert.Equal(0, res.Data.FailureCount);

            // Verify in-app notification sent to registered user David
            Assert.Single(notifRepo.Notifications);
            Assert.Equal(regUserId, notifRepo.Notifications[0].UserId);
            Assert.False(notifRepo.Notifications[0].IsRead);

            // Verify email sent to unregistered user Sarah
            Assert.Contains(emailService.SentEmails, m => m.ToEmail == "sarah.new@example.com");

            // Verify invitations in repo
            Assert.Equal(2, repo.Invitations.Count);
        }

        [Fact]
        public async Task RevokingInvitation_NeutralizesNotification_AndPreventsAcceptance()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var emailService = new TestEmailService();
            var realtime = new TestRealtimeService();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance, emailService, realtime);

            var ownerId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            repo.Groups.Add(new SplitGroup { Id = groupId, Name = "Weekend Cabin", OwnerId = ownerId, IsActive = true });
            repo.Users.Add(new User { Id = targetUserId, FirstName = "Eve", LastName = "Adams", Email = "eve@example.com" });

            // Invite Eve
            var inviteRes = await service.InviteMemberAsync(groupId, new InviteSplitGroupMemberDto
            {
                InvitedUserId = targetUserId,
                InvitedEmail = "eve@example.com"
            }, ownerId, "Alice");

            Assert.True(inviteRes.Success);
            var invitationId = inviteRes.Data!.Id;

            // Notice Eve has an unread notification
            Assert.Single(notifRepo.Notifications);
            Assert.False(notifRepo.Notifications[0].IsRead);

            // Owner revokes invitation
            var revokeRes = await service.RevokeInvitationAsync(groupId, invitationId, ownerId, "Alice");
            Assert.True(revokeRes.Success);

            // Eve's notification must now be neutralized (IsRead = true)
            Assert.True(notifRepo.Notifications[0].IsRead);

            // Eve attempts to accept the revoked invitation -> must be rejected
            var acceptRes = await service.AcceptInvitationAsync(invitationId, targetUserId, "eve@example.com", "Eve Adams");
            Assert.False(acceptRes.Success);
            Assert.Contains("revoked", acceptRes.Message);
        }

        [Fact]
        public async Task RemoveMember_KeepsHistory_AndRejoiningReactivatesWithoutDuplicateRows()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var emailService = new TestEmailService();
            var realtime = new TestRealtimeService();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance, emailService, realtime);

            var ownerId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();

            repo.Groups.Add(new SplitGroup { Id = groupId, Name = "Dinner Club", OwnerId = ownerId, IsActive = true });
            repo.Members.Add(new SplitGroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = ownerId, Role = "Owner", IsActive = true });

            var memberRowId = Guid.NewGuid();
            repo.Members.Add(new SplitGroupMember { Id = memberRowId, GroupId = groupId, UserId = memberUserId, Role = "Member", IsActive = true });
            repo.Users.Add(new User { Id = memberUserId, FirstName = "Charlie", LastName = "Brown", Email = "charlie@example.com" });

            // 1. Owner removes Charlie
            var removeRes = await service.RemoveMemberAsync(groupId, memberUserId, ownerId, "Alice");
            Assert.True(removeRes.Success);

            var memberRecord = repo.Members.First(m => m.UserId == memberUserId);
            Assert.False(memberRecord.IsActive);
            Assert.Equal(2, repo.Members.Count); // Total rows stayed 2, not deleted

            // 2. Owner re-invites Charlie
            var reInviteRes = await service.InviteMemberAsync(groupId, new InviteSplitGroupMemberDto
            {
                InvitedUserId = memberUserId,
                InvitedEmail = "charlie@example.com"
            }, ownerId, "Alice");

            Assert.True(reInviteRes.Success);

            // 3. Charlie accepts re-invitation
            var acceptRes = await service.AcceptInvitationAsync(reInviteRes.Data!.Id, memberUserId, "charlie@example.com", "Charlie Brown");
            Assert.True(acceptRes.Success);

            // 4. Verify Charlie is active again and NO new row was created
            Assert.Equal(2, repo.Members.Count);
            Assert.True(memberRecord.IsActive);
            Assert.Equal(memberRowId, memberRecord.Id);
        }
    }
}
