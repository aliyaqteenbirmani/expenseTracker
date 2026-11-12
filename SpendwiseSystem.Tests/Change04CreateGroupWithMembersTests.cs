using Microsoft.Extensions.Logging.Abstractions;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.EmailService;
using SpendwiseSystem.Application.Services.SplitExpenses;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class Change04CreateGroupWithMembersTests
    {
        private class TestGroupRepository : ISplitGroupRepository
        {
            public List<SplitGroup> Groups = new();
            public List<SplitGroupMember> Members = new();
            public List<SplitGroupInvitation> Invitations = new();
            public List<SplitGroupActivity> Activities = new();
            public List<User> Users = new();
            public bool ShouldThrowOnCreate = false;

            public Task<SplitGroup?> GetGroupByIdAsync(Guid groupId) =>
                Task.FromResult(Groups.FirstOrDefault(g => g.Id == groupId));

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
                if (ShouldThrowOnCreate) throw new InvalidOperationException("Simulated database failure");
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
                if (ShouldThrowOnCreate) throw new InvalidOperationException("Simulated database failure");
                Groups.Add(group);
                Members.Add(new SplitGroupMember { GroupId = group.Id, UserId = ownerId, Role = "Owner", IsActive = true });
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
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByUserAsync(Guid userId, string userEmail) =>
                Task.FromResult(new List<SplitGroupInvitationDto>());

            public Task<SplitGroupInvitation> CreateInvitationAsync(SplitGroupInvitation invitation, string inviterName, string groupName)
            {
                Invitations.Add(invitation);
                return Task.FromResult(invitation);
            }

            public Task<bool> UpdateInvitationResendAsync(Guid invitationId) => Task.FromResult(true);
            public Task<List<SplitGroupInvitation>> LinkPendingInvitationsForUserAsync(Guid userId, string email) =>
                Task.FromResult(new List<SplitGroupInvitation>());
            public Task<bool> RevokeInvitationAsync(Guid invitationId, Guid revokedByUserId, string revokerName, Guid groupId) => Task.FromResult(true);
            public Task<bool> AcceptInvitationAsync(Guid invitationId, Guid userId, string userName) => Task.FromResult(true);
            public Task<bool> RejectInvitationAsync(Guid invitationId, Guid userId, string userName) => Task.FromResult(true);
            public Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName) => Task.FromResult(true);
            public Task<List<SplitGroupActivityDto>> GetGroupActivitiesAsync(Guid groupId, int limit = 50) => Task.FromResult(new List<SplitGroupActivityDto>());
            public Task<PagedGroupActivityResultDto> GetPagedGroupActivitiesAsync(Guid groupId, SplitGroupActivityQueryParameters parameters) => Task.FromResult(new PagedGroupActivityResultDto());
            public Task<SplitGroupActivityDto?> GetActivityByIdAsync(Guid groupId, Guid activityId) => Task.FromResult<SplitGroupActivityDto?>(null);
            public Task LogActivityAsync(Guid groupId, Guid userId, string activityType, string description, Guid? referenceId = null) => Task.CompletedTask;
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
            public Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId) => Task.FromResult(true);
            public Task<int> MarkAllAsReadAsync(Guid userId) => Task.FromResult(1);
            public Task<bool> MarkInvitationNotificationAsReadAsync(Guid invitationId, Guid userId) => Task.FromResult(true);
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
        public async Task CreateGroup_WithZeroMembers_SucceedsAndCreatesOwnerOnly()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var emailService = new TestEmailService();
            var realtime = new TestRealtimeService();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance, emailService, realtime);

            var ownerId = Guid.NewGuid();
            var dto = new CreateSplitGroupDto
            {
                Name = "Solo Group",
                Description = "A group without invitees",
                Icon = "👥",
                Invitees = null
            };

            var res = await service.CreateGroupAsync(dto, ownerId, "Alice");

            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.Equal("Solo Group", res.Data.Name);
            Assert.Equal("Owner", res.Data.CurrentUserRole);
            Assert.Empty(res.Data.Invitations);
            Assert.Single(repo.Groups);
            Assert.Single(repo.Members);
            Assert.Empty(repo.Invitations);
        }

        [Fact]
        public async Task CreateGroup_WithRegisteredAndUnregisteredInvitees_CreatesInvitationsAndSendsNotificationsAndEmails()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var emailService = new TestEmailService();
            var realtime = new TestRealtimeService();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance, emailService, realtime);

            var ownerId = Guid.NewGuid();
            var registeredUserId = Guid.NewGuid();

            repo.Users.Add(new User
            {
                Id = registeredUserId,
                FirstName = "Bob",
                LastName = "Builder",
                Email = "bob@example.com"
            });

            var dto = new CreateSplitGroupDto
            {
                Name = "Trip to Murree",
                Invitees = new List<CreateGroupInviteeInputDto>
                {
                    new CreateGroupInviteeInputDto { UserId = registeredUserId, Email = "bob@example.com" },
                    new CreateGroupInviteeInputDto { Email = "charlie@newbie.com" }
                }
            };

            var res = await service.CreateGroupAsync(dto, ownerId, "Alice");

            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.Equal(2, res.Data.Invitations.Count);

            // Registered invitee should have received in-app notification & realtime event
            var bobInvite = res.Data.Invitations.FirstOrDefault(i => i.Identifier == "bob@example.com");
            Assert.NotNull(bobInvite);
            Assert.Equal("Invited", bobInvite.Status);
            Assert.Contains(notifRepo.Notifications, n => n.UserId == registeredUserId && n.Type == "GroupInvitationReceived");
            Assert.Contains(realtime.UserEvents, e => e.UserId == registeredUserId);

            // Unregistered invitee should have received email
            var charlieInvite = res.Data.Invitations.FirstOrDefault(i => i.Identifier == "charlie@newbie.com");
            Assert.NotNull(charlieInvite);
            Assert.Equal("Invited", charlieInvite.Status);
            Assert.Contains(emailService.SentEmails, m => m.ToEmail == "charlie@newbie.com");

            // Database should have 2 pending invitations and 2 MemberInvited activities
            Assert.Equal(2, repo.Invitations.Count);
            Assert.All(repo.Invitations, i => Assert.Equal("Pending", i.Status));
            Assert.Equal(2, repo.Activities.Count(a => a.ActivityType == "MemberInvited"));
        }

        [Fact]
        public async Task CreateGroup_WithDuplicateEmails_RejectsWithClearError()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var dto = new CreateSplitGroupDto
            {
                Name = "Office Lunch",
                Invitees = new List<CreateGroupInviteeInputDto>
                {
                    new CreateGroupInviteeInputDto { Email = "colleague@office.com" },
                    new CreateGroupInviteeInputDto { Email = "COLLEAGUE@OFFICE.COM" }
                }
            };

            var res = await service.CreateGroupAsync(dto, ownerId, "Alice");

            Assert.False(res.Success);
            Assert.Contains("Duplicate invitee", res.Message);
            Assert.Empty(repo.Groups);
            Assert.Empty(repo.Invitations);
        }

        [Fact]
        public async Task CreateGroup_WithSelfAsInvitee_RejectsWithClearError()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            repo.Users.Add(new User { Id = ownerId, FirstName = "Alice", Email = "alice@spendwise.com" });

            var dto = new CreateSplitGroupDto
            {
                Name = "Self Group",
                Invitees = new List<CreateGroupInviteeInputDto>
                {
                    new CreateGroupInviteeInputDto { UserId = ownerId, Email = "alice@spendwise.com" }
                }
            };

            var res = await service.CreateGroupAsync(dto, ownerId, "Alice");

            Assert.False(res.Success);
            Assert.Contains("cannot invite yourself", res.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(repo.Groups);
        }

        [Fact]
        public async Task CreateGroup_WithInvalidEmail_RejectsWithClearError()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var dto = new CreateSplitGroupDto
            {
                Name = "Invalid Email Group",
                Invitees = new List<CreateGroupInviteeInputDto>
                {
                    new CreateGroupInviteeInputDto { Email = "not-an-email" }
                }
            };

            var res = await service.CreateGroupAsync(dto, ownerId, "Alice");

            Assert.False(res.Success);
            Assert.Contains("Invalid email", res.Message);
            Assert.Empty(repo.Groups);
        }

        [Fact]
        public async Task CreateGroup_Exceeding20InviteesLimit_RejectsWithClearError()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var invitees = new List<CreateGroupInviteeInputDto>();
            for (int i = 0; i < 21; i++)
            {
                invitees.Add(new CreateGroupInviteeInputDto { Email = $"user{i}@test.com" });
            }

            var dto = new CreateSplitGroupDto
            {
                Name = "Too Many Members",
                Invitees = invitees
            };

            var res = await service.CreateGroupAsync(dto, ownerId, "Alice");

            Assert.False(res.Success);
            Assert.Contains("Cannot invite more than 20", res.Message);
            Assert.Empty(repo.Groups);
        }

        [Fact]
        public async Task CreateGroup_DatabaseErrorDuringCreation_RollsBackAllInvitationsAtomically()
        {
            var repo = new TestGroupRepository { ShouldThrowOnCreate = true };
            var notifRepo = new TestNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var ownerId = Guid.NewGuid();
            var dto = new CreateSplitGroupDto
            {
                Name = "Failing Transaction",
                Invitees = new List<CreateGroupInviteeInputDto>
                {
                    new CreateGroupInviteeInputDto { Email = "test@user.com" }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateGroupAsync(dto, ownerId, "Alice"));

            Assert.Empty(repo.Groups);
            Assert.Empty(repo.Invitations);
            Assert.Empty(notifRepo.Notifications);
        }

        [Fact]
        public async Task SearchUsers_ExcludesCurrentUserId_AndMatchesPattern()
        {
            var repo = new TestGroupRepository();
            var notifRepo = new TestNotificationRepository();
            var service = new SplitGroupService(repo, notifRepo, NullLogger<SplitGroupService>.Instance);

            var currentUserId = Guid.NewGuid();
            repo.Users.Add(new User { Id = currentUserId, FirstName = "Current", Email = "current@user.com" });
            repo.Users.Add(new User { Id = Guid.NewGuid(), FirstName = "Dave", Email = "dave@example.com" });
            repo.Users.Add(new User { Id = Guid.NewGuid(), FirstName = "David", Email = "david@example.com" });
            repo.Users.Add(new User { Id = Guid.NewGuid(), FirstName = "Sarah", Email = "sarah@example.com" });

            var res = await service.SearchUsersAsync("Dav", currentUserId);

            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.Equal(2, res.Data.Count);
            Assert.DoesNotContain(res.Data, u => u.UserId == currentUserId);
        }
    }
}
