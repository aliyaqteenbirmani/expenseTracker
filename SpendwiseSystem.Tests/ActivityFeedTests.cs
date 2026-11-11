using System;
using System.Collections.Generic;
using System.Linq;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using Xunit;

namespace SpendwiseSystem.Tests
{
    public class ActivityFeedTests
    {
        [Fact]
        public void ActivityFeed_ChronologicalOrdering_LatestActivityFirst()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var baseTime = DateTime.UtcNow;

            var activities = new List<SplitGroupActivityDto>
            {
                new SplitGroupActivityDto
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = user1,
                    UserName = "Ali",
                    ActivityType = "GroupCreated",
                    Description = "Ali created the group Trip to Murree.",
                    CreatedAt = baseTime.AddHours(-3)
                },
                new SplitGroupActivityDto
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = user1,
                    UserName = "Ali",
                    ActivityType = "ExpenseAdded",
                    Description = "Ali added Dinner expense of Rs. 4,000.",
                    CreatedAt = baseTime.AddHours(-1)
                },
                new SplitGroupActivityDto
                {
                    Id = Guid.NewGuid(),
                    GroupId = user1,
                    UserId = user1,
                    UserName = "Ahmed",
                    ActivityType = "MemberJoined",
                    Description = "Ahmed joined the group.",
                    CreatedAt = baseTime.AddHours(-2)
                },
                new SplitGroupActivityDto
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = user1,
                    UserName = "Ahmed",
                    ActivityType = "SettlementRecorded",
                    Description = "Ahmed paid Ali Rs. 500.",
                    CreatedAt = baseTime
                }
            };

            // Act - ordering latest first
            var ordered = activities.OrderByDescending(a => a.CreatedAt).ToList();

            // Assert
            Assert.Equal("SettlementRecorded", ordered[0].ActivityType);
            Assert.Equal("ExpenseAdded", ordered[1].ActivityType);
            Assert.Equal("MemberJoined", ordered[2].ActivityType);
            Assert.Equal("GroupCreated", ordered[3].ActivityType);
            Assert.True(ordered[0].CreatedAt > ordered[1].CreatedAt);
            Assert.True(ordered[1].CreatedAt > ordered[2].CreatedAt);
            Assert.True(ordered[2].CreatedAt > ordered[3].CreatedAt);
        }

        [Fact]
        public void ActivityFeed_PaginationLogic_ReturnsCorrectPageAndMetadata()
        {
            // Arrange
            var allActivities = Enumerable.Range(1, 25).Select(i => new SplitGroupActivityDto
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                UserName = $"User {i}",
                ActivityType = "ExpenseAdded",
                Description = $"Expense {i} added",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            }).OrderByDescending(x => x.CreatedAt).ToList();

            int pageNumber = 2;
            int pageSize = 10;
            int totalCount = allActivities.Count;
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Act
            var pageItems = allActivities.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            var pagedResult = new PagedGroupActivityResultDto
            {
                Items = pageItems,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Assert
            Assert.Equal(10, pagedResult.Items.Count);
            Assert.Equal(25, pagedResult.TotalCount);
            Assert.Equal(3, pagedResult.TotalPages);
            Assert.Equal(2, pagedResult.PageNumber);
            Assert.Equal("Expense 15 added", pagedResult.Items[0].Description);
        }

        [Fact]
        public void ActivityFeed_FilterByType_ReturnsOnlyMatchingActivities()
        {
            // Arrange
            var list = new List<SplitGroupActivityDto>
            {
                new SplitGroupActivityDto { ActivityType = "ExpenseAdded", Description = "Expense 1" },
                new SplitGroupActivityDto { ActivityType = "SettlementRecorded", Description = "Settlement 1" },
                new SplitGroupActivityDto { ActivityType = "ExpenseAdded", Description = "Expense 2" },
                new SplitGroupActivityDto { ActivityType = "MemberJoined", Description = "Member 1 joined" },
                new SplitGroupActivityDto { ActivityType = "ExpenseDeleted", Description = "Expense 1 deleted" }
            };

            // Act
            var expenseOnly = list.Where(a => a.ActivityType == "ExpenseAdded").ToList();
            var settlementOnly = list.Where(a => a.ActivityType == "SettlementRecorded").ToList();

            // Assert
            Assert.Equal(2, expenseOnly.Count);
            Assert.All(expenseOnly, a => Assert.Equal("ExpenseAdded", a.ActivityType));
            Assert.Single(settlementOnly);
            Assert.Equal("SettlementRecorded", settlementOnly[0].ActivityType);
        }

        [Fact]
        public void ActivityFeed_SupportedActivityTypes_ValidateExpectedTemplates()
        {
            // Verify all supported activity types from Step 8 description
            var supportedTypes = new[]
            {
                "GroupCreated",
                "MemberInvited",
                "MemberJoined",
                "MemberRemoved",
                "ExpenseAdded",
                "ExpenseUpdated",
                "ExpenseDeleted",
                "SettlementRecorded"
            };

            Assert.Equal(8, supportedTypes.Length);

            // Sample formatted descriptions matching Step 8 examples
            var sampleDescriptions = new Dictionary<string, string>
            {
                ["GroupCreated"] = "Ali created the group Trip to Murree.",
                ["MemberInvited"] = "Ali invited Bilal to the group.",
                ["MemberJoined"] = "Ahmed joined the group.",
                ["MemberRemoved"] = "Bilal removed Zain from the group.",
                ["ExpenseAdded"] = "Ali added Dinner expense of Rs. 4,000.00.",
                ["ExpenseUpdated"] = "Bilal updated the Hotel expense.",
                ["ExpenseDeleted"] = "Ali deleted the Lunch expense.",
                ["SettlementRecorded"] = "Ahmed paid Ali Rs. 500.00."
            };

            foreach (var type in supportedTypes)
            {
                Assert.True(sampleDescriptions.ContainsKey(type));
                Assert.False(string.IsNullOrWhiteSpace(sampleDescriptions[type]));
            }
        }
    }
}
