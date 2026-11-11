using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class SplitGroupRepository : ISplitGroupRepository
    {
        private readonly string _connectionString;

        public SplitGroupRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnectionStr")
                ?? throw new InvalidOperationException("DefaultConnectionStr is not configured.");
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<SplitGroup?> GetGroupByIdAsync(Guid groupId)
        {
            const string sql = @"
                SELECT Id, Name, Description, Icon, OwnerId, CreatedAt, UpdatedAt, IsActive, IsDeleted
                FROM dbo.SplitGroups
                WHERE Id = @GroupId AND IsDeleted = 0;";

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<SplitGroup>(sql, new { GroupId = groupId });
        }

        public async Task<SplitGroupMember?> GetMembershipAsync(Guid groupId, Guid userId)
        {
            const string sql = @"
                SELECT Id, GroupId, UserId, Role, JoinedAt, CreatedAt, UpdatedAt, IsActive
                FROM dbo.SplitGroupMembers
                WHERE GroupId = @GroupId AND UserId = @UserId;";

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<SplitGroupMember>(sql, new { GroupId = groupId, UserId = userId });
        }

        public async Task<List<SplitGroupSummaryDto>> GetUserGroupsAsync(Guid userId)
        {
            const string sql = @"
                SELECT 
                    g.Id,
                    g.Name,
                    g.Description,
                    g.Icon,
                    g.OwnerId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''), u.Email) AS OwnerName,
                    u.Email AS OwnerEmail,
                    (SELECT COUNT(1) FROM dbo.SplitGroupMembers gm2 WHERE gm2.GroupId = g.Id AND gm2.IsActive = 1) AS MemberCount,
                    ISNULL((SELECT SUM(Amount) FROM dbo.SplitExpenses e WHERE e.GroupId = g.Id AND e.IsDeleted = 0), CAST(0 AS DECIMAL(18,2))) AS TotalExpenses,
                    (
                        ISNULL((SELECT SUM(e.Amount) FROM dbo.SplitExpenses e WHERE e.GroupId = g.Id AND e.PayerId = @UserId AND e.IsDeleted = 0), 0)
                        - ISNULL((SELECT SUM(s.ShareAmount) FROM dbo.SplitExpenseShares s INNER JOIN dbo.SplitExpenses e ON e.Id = s.ExpenseId WHERE e.GroupId = g.Id AND s.UserId = @UserId AND e.IsDeleted = 0), 0)
                        + ISNULL((SELECT SUM(st.Amount) FROM dbo.SplitSettlements st WHERE st.GroupId = g.Id AND st.FromUserId = @UserId AND st.IsDeleted = 0), 0)
                        - ISNULL((SELECT SUM(st.Amount) FROM dbo.SplitSettlements st WHERE st.GroupId = g.Id AND st.ToUserId = @UserId AND st.IsDeleted = 0), 0)
                    ) AS CurrentUserBalance,
                    gm.Role AS CurrentUserRole,
                    g.CreatedAt
                FROM dbo.SplitGroups g
                INNER JOIN dbo.SplitGroupMembers gm ON gm.GroupId = g.Id AND gm.UserId = @UserId AND gm.IsActive = 1
                LEFT JOIN dbo.Users u ON u.Id = g.OwnerId
                WHERE g.IsDeleted = 0
                ORDER BY g.CreatedAt DESC;";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitGroupSummaryDto>(sql, new { UserId = userId });
            return results.ToList();
        }

        public async Task<SplitGroupDetailDto?> GetGroupDetailsAsync(Guid groupId, Guid userId)
        {
            const string groupSql = @"
                SELECT 
                    g.Id,
                    g.Name,
                    g.Description,
                    g.Icon,
                    g.OwnerId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''), u.Email) AS OwnerName,
                    u.Email AS OwnerEmail,
                    (SELECT COUNT(1) FROM dbo.SplitGroupMembers gm2 WHERE gm2.GroupId = g.Id AND gm2.IsActive = 1) AS MemberCount,
                    ISNULL((SELECT SUM(Amount) FROM dbo.SplitExpenses e WHERE e.GroupId = g.Id AND e.IsDeleted = 0), CAST(0 AS DECIMAL(18,2))) AS TotalExpenses,
                    (
                        ISNULL((SELECT SUM(e.Amount) FROM dbo.SplitExpenses e WHERE e.GroupId = g.Id AND e.PayerId = @UserId AND e.IsDeleted = 0), 0)
                        - ISNULL((SELECT SUM(s.ShareAmount) FROM dbo.SplitExpenseShares s INNER JOIN dbo.SplitExpenses e ON e.Id = s.ExpenseId WHERE e.GroupId = g.Id AND s.UserId = @UserId AND e.IsDeleted = 0), 0)
                        + ISNULL((SELECT SUM(st.Amount) FROM dbo.SplitSettlements st WHERE st.GroupId = g.Id AND st.FromUserId = @UserId AND st.IsDeleted = 0), 0)
                        - ISNULL((SELECT SUM(st.Amount) FROM dbo.SplitSettlements st WHERE st.GroupId = g.Id AND st.ToUserId = @UserId AND st.IsDeleted = 0), 0)
                    ) AS CurrentUserBalance,
                    gm.Role AS CurrentUserRole,
                    g.IsActive,
                    g.CreatedAt,
                    g.UpdatedAt
                FROM dbo.SplitGroups g
                INNER JOIN dbo.SplitGroupMembers gm ON gm.GroupId = g.Id AND gm.UserId = @UserId AND gm.IsActive = 1
                LEFT JOIN dbo.Users u ON u.Id = g.OwnerId
                WHERE g.Id = @GroupId AND g.IsDeleted = 0;";

            using var connection = CreateConnection();
            var detail = await connection.QueryFirstOrDefaultAsync<SplitGroupDetailDto>(groupSql, new { GroupId = groupId, UserId = userId });
            if (detail == null) return null;

            detail.Members = await GetGroupMembersAsync(groupId);
            return detail;
        }

        public async Task<List<SplitGroupMemberDto>> GetGroupMembersAsync(Guid groupId)
        {
            const string sql = @"
                SELECT 
                    gm.Id,
                    gm.UserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''), u.Email) AS FullName,
                    u.Email,
                    gm.Role,
                    gm.JoinedAt,
                    gm.IsActive,
                    (
                        ISNULL((SELECT SUM(e.Amount) FROM dbo.SplitExpenses e WHERE e.GroupId = @GroupId AND e.PayerId = gm.UserId AND e.IsDeleted = 0), 0)
                        - ISNULL((SELECT SUM(s.ShareAmount) FROM dbo.SplitExpenseShares s INNER JOIN dbo.SplitExpenses e ON e.Id = s.ExpenseId WHERE e.GroupId = @GroupId AND s.UserId = gm.UserId AND e.IsDeleted = 0), 0)
                        + ISNULL((SELECT SUM(st.Amount) FROM dbo.SplitSettlements st WHERE st.GroupId = @GroupId AND st.FromUserId = gm.UserId AND st.IsDeleted = 0), 0)
                        - ISNULL((SELECT SUM(st.Amount) FROM dbo.SplitSettlements st WHERE st.GroupId = @GroupId AND st.ToUserId = gm.UserId AND st.IsDeleted = 0), 0)
                    ) AS Balance
                FROM dbo.SplitGroupMembers gm
                LEFT JOIN dbo.Users u ON u.Id = gm.UserId
                WHERE gm.GroupId = @GroupId AND gm.IsActive = 1
                ORDER BY gm.JoinedAt ASC;";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitGroupMemberDto>(sql, new { GroupId = groupId });
            return results.ToList();
        }

        public async Task<SplitGroup> CreateGroupWithMemberAndActivityAsync(SplitGroup group, Guid ownerId, string ownerName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertGroupSql = @"
                    INSERT INTO dbo.SplitGroups (Id, Name, Description, Icon, OwnerId, CreatedAt, UpdatedAt, IsActive, IsDeleted)
                    VALUES (@Id, @Name, @Description, @Icon, @OwnerId, @CreatedAt, @UpdatedAt, @IsActive, @IsDeleted);";

                await connection.ExecuteAsync(insertGroupSql, group, transaction);

                const string insertMemberSql = @"
                    INSERT INTO dbo.SplitGroupMembers (Id, GroupId, UserId, Role, JoinedAt, CreatedAt, UpdatedAt, IsActive)
                    VALUES (@MemberId, @GroupId, @UserId, 'Owner', @JoinedAt, @CreatedAt, @UpdatedAt, 1);";

                var memberId = Guid.NewGuid();
                await connection.ExecuteAsync(insertMemberSql, new
                {
                    MemberId = memberId,
                    GroupId = group.Id,
                    UserId = ownerId,
                    JoinedAt = group.CreatedAt,
                    CreatedAt = group.CreatedAt,
                    UpdatedAt = group.UpdatedAt
                }, transaction);

                const string insertActivitySql = @"
                    INSERT INTO dbo.SplitGroupActivities (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                    VALUES (@ActivityId, @GroupId, @UserId, 'GroupCreated', @ReferenceId, @Description, @CreatedAt);";

                await connection.ExecuteAsync(insertActivitySql, new
                {
                    ActivityId = Guid.NewGuid(),
                    GroupId = group.Id,
                    UserId = ownerId,
                    ReferenceId = group.Id,
                    Description = $"{ownerName} created the group \"{group.Name}\".",
                    CreatedAt = group.CreatedAt
                }, transaction);

                transaction.Commit();
                return group;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateGroupAsync(SplitGroup group, Guid userId, string userName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateSql = @"
                    UPDATE dbo.SplitGroups
                    SET Name = @Name,
                        Description = @Description,
                        Icon = @Icon,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @Id AND IsDeleted = 0;";

                var rows = await connection.ExecuteAsync(updateSql, new
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    Icon = group.Icon
                }, transaction);

                if (rows > 0)
                {
                    const string activitySql = @"
                        INSERT INTO dbo.SplitGroupActivities (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                        VALUES (@ActivityId, @GroupId, @UserId, 'GroupUpdated', @ReferenceId, @Description, SYSUTCDATETIME());";

                    await connection.ExecuteAsync(activitySql, new
                    {
                        ActivityId = Guid.NewGuid(),
                        GroupId = group.Id,
                        UserId = userId,
                        ReferenceId = group.Id,
                        Description = $"{userName} updated the group settings."
                    }, transaction);
                }

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> SoftDeleteGroupAsync(Guid groupId, Guid userId, string userName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string deleteSql = @"
                    UPDATE dbo.SplitGroups
                    SET IsDeleted = 1,
                        IsActive = 0,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @GroupId AND IsDeleted = 0;";

                var rows = await connection.ExecuteAsync(deleteSql, new { GroupId = groupId }, transaction);

                if (rows > 0)
                {
                    const string activitySql = @"
                        INSERT INTO dbo.SplitGroupActivities (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                        VALUES (@ActivityId, @GroupId, @UserId, 'GroupDeleted', @ReferenceId, @Description, SYSUTCDATETIME());";

                    await connection.ExecuteAsync(activitySql, new
                    {
                        ActivityId = Guid.NewGuid(),
                        GroupId = groupId,
                        UserId = userId,
                        ReferenceId = groupId,
                        Description = $"{userName} archived/deleted the group."
                    }, transaction);
                }

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> HasFinancialRecordsAsync(Guid groupId)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.SplitExpenses WHERE GroupId = @GroupId AND IsDeleted = 0
                    UNION ALL
                    SELECT 1 FROM dbo.SplitSettlements WHERE GroupId = @GroupId AND IsDeleted = 0
                ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;";

            using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<bool>(sql, new { GroupId = groupId });
        }

        public async Task<User?> GetUserByEmailOrIdAsync(string? email, Guid? userId)
        {
            const string sql = @"
                SELECT TOP 1 Id, FirstName, LastName, Email, IsActive, IsDeleted
                FROM dbo.Users
                WHERE (@UserId IS NOT NULL AND Id = @UserId)
                   OR (@Email IS NOT NULL AND LOWER(Email) = LOWER(@Email));";

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId, Email = email });
        }

        public async Task<SplitGroupInvitation?> GetInvitationByIdAsync(Guid invitationId)
        {
            const string sql = @"
                SELECT Id, GroupId, InvitedUserId, InvitedEmail, InvitedBy, Status, InvitationToken, ExpiresAt, CreatedAt, RespondedAt
                FROM dbo.SplitGroupInvitations
                WHERE Id = @InvitationId;";

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<SplitGroupInvitation>(sql, new { InvitationId = invitationId });
        }

        public async Task<SplitGroupInvitation?> GetPendingInvitationByEmailAsync(Guid groupId, string email)
        {
            const string sql = @"
                SELECT TOP 1 Id, GroupId, InvitedUserId, InvitedEmail, InvitedBy, Status, InvitationToken, ExpiresAt, CreatedAt, RespondedAt
                FROM dbo.SplitGroupInvitations
                WHERE GroupId = @GroupId AND LOWER(InvitedEmail) = LOWER(@Email) AND Status = 'Pending';";

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<SplitGroupInvitation>(sql, new { GroupId = groupId, Email = email });
        }

        public async Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByGroupAsync(Guid groupId)
        {
            const string sql = @"
                SELECT 
                    i.Id,
                    i.GroupId,
                    g.Name AS GroupName,
                    i.InvitedUserId,
                    i.InvitedEmail,
                    i.InvitedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''), u.Email) AS InvitedByName,
                    i.Status,
                    i.ExpiresAt,
                    i.CreatedAt,
                    i.RespondedAt
                FROM dbo.SplitGroupInvitations i
                INNER JOIN dbo.SplitGroups g ON g.Id = i.GroupId
                LEFT JOIN dbo.Users u ON u.Id = i.InvitedBy
                WHERE i.GroupId = @GroupId AND i.Status = 'Pending'
                ORDER BY i.CreatedAt DESC;";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitGroupInvitationDto>(sql, new { GroupId = groupId });
            return results.ToList();
        }

        public async Task<List<SplitGroupInvitationDto>> GetPendingInvitationsByUserAsync(Guid userId, string userEmail)
        {
            const string sql = @"
                SELECT 
                    i.Id,
                    i.GroupId,
                    g.Name AS GroupName,
                    i.InvitedUserId,
                    i.InvitedEmail,
                    i.InvitedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''), u.Email) AS InvitedByName,
                    i.Status,
                    i.ExpiresAt,
                    i.CreatedAt,
                    i.RespondedAt
                FROM dbo.SplitGroupInvitations i
                INNER JOIN dbo.SplitGroups g ON g.Id = i.GroupId AND g.IsDeleted = 0
                LEFT JOIN dbo.Users u ON u.Id = i.InvitedBy
                WHERE (i.InvitedUserId = @UserId OR LOWER(i.InvitedEmail) = LOWER(@Email))
                  AND i.Status = 'Pending'
                  AND i.ExpiresAt > SYSUTCDATETIME()
                ORDER BY i.CreatedAt DESC;";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitGroupInvitationDto>(sql, new { UserId = userId, Email = userEmail });
            return results.ToList();
        }

        public async Task<SplitGroupInvitation> CreateInvitationAsync(SplitGroupInvitation invitation, string inviterName, string groupName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertSql = @"
                    INSERT INTO dbo.SplitGroupInvitations 
                        (Id, GroupId, InvitedUserId, InvitedEmail, InvitedBy, Status, InvitationToken, ExpiresAt, CreatedAt)
                    VALUES 
                        (@Id, @GroupId, @InvitedUserId, @InvitedEmail, @InvitedBy, @Status, @InvitationToken, @ExpiresAt, @CreatedAt);";

                await connection.ExecuteAsync(insertSql, invitation, transaction);

                const string activitySql = @"
                    INSERT INTO dbo.SplitGroupActivities (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                    VALUES (@ActivityId, @GroupId, @UserId, 'MemberInvited', @ReferenceId, @Description, SYSUTCDATETIME());";

                await connection.ExecuteAsync(activitySql, new
                {
                    ActivityId = Guid.NewGuid(),
                    GroupId = invitation.GroupId,
                    UserId = invitation.InvitedBy,
                    ReferenceId = invitation.Id,
                    Description = $"{inviterName} invited {invitation.InvitedEmail} to join the group."
                }, transaction);

                transaction.Commit();
                return invitation;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> RevokeInvitationAsync(Guid invitationId, Guid revokedByUserId, string revokerName, Guid groupId)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateSql = @"
                    UPDATE dbo.SplitGroupInvitations
                    SET Status = 'Revoked',
                        RespondedAt = SYSUTCDATETIME()
                    WHERE Id = @InvitationId AND Status = 'Pending';";

                var rows = await connection.ExecuteAsync(updateSql, new { InvitationId = invitationId }, transaction);

                if (rows > 0)
                {
                    const string activitySql = @"
                        INSERT INTO dbo.SplitGroupActivities (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                        VALUES (@ActivityId, @GroupId, @UserId, 'InvitationRevoked', @ReferenceId, @Description, SYSUTCDATETIME());";

                    await connection.ExecuteAsync(activitySql, new
                    {
                        ActivityId = Guid.NewGuid(),
                        GroupId = groupId,
                        UserId = revokedByUserId,
                        ReferenceId = invitationId,
                        Description = $"{revokerName} revoked an invitation."
                    }, transaction);
                }

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> AcceptInvitationAsync(Guid invitationId, Guid userId, string userName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string selectSql = @"
                    SELECT Id, GroupId, InvitedUserId, InvitedEmail, Status, ExpiresAt
                    FROM dbo.SplitGroupInvitations
                    WHERE Id = @InvitationId;";

                var invitation = await connection.QueryFirstOrDefaultAsync<SplitGroupInvitation>(selectSql, new { InvitationId = invitationId }, transaction);
                if (invitation == null || invitation.Status != "Pending")
                {
                    transaction.Rollback();
                    return false;
                }

                const string updateInviteSql = @"
                    UPDATE dbo.SplitGroupInvitations
                    SET Status = 'Accepted',
                        InvitedUserId = @UserId,
                        RespondedAt = SYSUTCDATETIME()
                    WHERE Id = @InvitationId;";

                await connection.ExecuteAsync(updateInviteSql, new { InvitationId = invitationId, UserId = userId }, transaction);

                // Check if membership record already exists (e.g. previously removed)
                const string checkMemberSql = @"
                    SELECT Id, IsActive FROM dbo.SplitGroupMembers
                    WHERE GroupId = @GroupId AND UserId = @UserId;";

                var existingMember = await connection.QueryFirstOrDefaultAsync<SplitGroupMember>(checkMemberSql, new
                {
                    GroupId = invitation.GroupId,
                    UserId = userId
                }, transaction);

                if (existingMember != null)
                {
                    const string reactivateMemberSql = @"
                        UPDATE dbo.SplitGroupMembers
                        SET IsActive = 1,
                            Role = 'Member',
                            JoinedAt = SYSUTCDATETIME(),
                            UpdatedAt = SYSUTCDATETIME()
                        WHERE Id = @MemberId;";

                    await connection.ExecuteAsync(reactivateMemberSql, new { MemberId = existingMember.Id }, transaction);
                }
                else
                {
                    const string insertMemberSql = @"
                        INSERT INTO dbo.SplitGroupMembers (Id, GroupId, UserId, Role, JoinedAt, CreatedAt, UpdatedAt, IsActive)
                        VALUES (@Id, @GroupId, @UserId, 'Member', SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME(), 1);";

                    await connection.ExecuteAsync(insertMemberSql, new
                    {
                        Id = Guid.NewGuid(),
                        GroupId = invitation.GroupId,
                        UserId = userId
                    }, transaction);
                }

                const string activitySql = @"
                    INSERT INTO dbo.SplitGroupActivities (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                    VALUES (@ActivityId, @GroupId, @UserId, 'InvitationAccepted', @ReferenceId, @Description, SYSUTCDATETIME());";

                await connection.ExecuteAsync(activitySql, new
                {
                    ActivityId = Guid.NewGuid(),
                    GroupId = invitation.GroupId,
                    UserId = userId,
                    ReferenceId = invitationId,
                    Description = $"{userName} accepted the invitation and joined the group."
                }, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> RejectInvitationAsync(Guid invitationId, Guid userId, string userName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateSql = @"
                    UPDATE dbo.SplitGroupInvitations
                    SET Status = 'Rejected',
                        RespondedAt = SYSUTCDATETIME()
                    WHERE Id = @InvitationId AND Status = 'Pending';";

                var rows = await connection.ExecuteAsync(updateSql, new { InvitationId = invitationId }, transaction);

                if (rows > 0)
                {
                    const string selectGroupIdSql = "SELECT GroupId FROM dbo.SplitGroupInvitations WHERE Id = @InvitationId;";
                    var groupId = await connection.ExecuteScalarAsync<Guid>(selectGroupIdSql, new { InvitationId = invitationId }, transaction);

                    const string activitySql = @"
                        INSERT INTO dbo.SplitGroupActivities (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                        VALUES (@ActivityId, @GroupId, @UserId, 'InvitationRejected', @ReferenceId, @Description, SYSUTCDATETIME());";

                    await connection.ExecuteAsync(activitySql, new
                    {
                        ActivityId = Guid.NewGuid(),
                        GroupId = groupId,
                        UserId = userId,
                        ReferenceId = invitationId,
                        Description = $"{userName} declined the invitation."
                    }, transaction);
                }

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> RemoveMemberAsync(Guid groupId, Guid userIdToRemove, Guid removedByUserId, string removerName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string getUserNameSql = @"
                    SELECT ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(FirstName, ' ', LastName))), ''), Email)
                    FROM dbo.Users WHERE Id = @UserId;";

                var removedUserName = await connection.ExecuteScalarAsync<string>(getUserNameSql, new { UserId = userIdToRemove }, transaction) ?? "Member";

                const string deactivateSql = @"
                    UPDATE dbo.SplitGroupMembers
                    SET IsActive = 0,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE GroupId = @GroupId AND UserId = @UserId AND IsActive = 1;";

                var rows = await connection.ExecuteAsync(deactivateSql, new { GroupId = groupId, UserId = userIdToRemove }, transaction);

                if (rows > 0)
                {
                    const string activitySql = @"
                        INSERT INTO dbo.SplitGroupActivities (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                        VALUES (@ActivityId, @GroupId, @UserId, 'MemberRemoved', @ReferenceId, @Description, SYSUTCDATETIME());";

                    await connection.ExecuteAsync(activitySql, new
                    {
                        ActivityId = Guid.NewGuid(),
                        GroupId = groupId,
                        UserId = removedByUserId,
                        ReferenceId = userIdToRemove,
                        Description = $"{removerName} removed {removedUserName} from the group."
                    }, transaction);
                }

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<SplitGroupActivityDto>> GetGroupActivitiesAsync(Guid groupId, int limit = 50)
        {
            const string sql = @"
                SELECT TOP (@Limit)
                    a.Id,
                    a.GroupId,
                    a.UserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''), u.Email) AS UserName,
                    a.ActivityType,
                    a.ReferenceId,
                    a.Description,
                    a.CreatedAt
                FROM dbo.SplitGroupActivities a
                LEFT JOIN dbo.Users u ON u.Id = a.UserId
                WHERE a.GroupId = @GroupId
                ORDER BY a.CreatedAt DESC;";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitGroupActivityDto>(sql, new { GroupId = groupId, Limit = limit });
            return results.ToList();
        }

        public async Task LogActivityAsync(Guid groupId, Guid userId, string activityType, string description, Guid? referenceId = null)
        {
            const string sql = @"
                INSERT INTO dbo.SplitGroupActivities (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                VALUES (@Id, @GroupId, @UserId, @ActivityType, @ReferenceId, @Description, SYSUTCDATETIME());";

            using var connection = CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = userId,
                ActivityType = activityType,
                ReferenceId = referenceId,
                Description = description
            });
        }
    }
}
