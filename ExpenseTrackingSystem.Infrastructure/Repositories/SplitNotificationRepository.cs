using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class SplitNotificationRepository : ISplitNotificationRepository
    {
        private readonly string _connectionString;

        public SplitNotificationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnectionStr")
                ?? throw new InvalidOperationException("DefaultConnectionStr is not configured.");
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<PagedSplitNotificationResultDto> GetNotificationsByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var offset = (pageNumber - 1) * pageSize;

            const string countSql = @"
                SELECT COUNT(*) FROM dbo.SplitNotifications WHERE UserId = @UserId;
                SELECT COUNT(*) FROM dbo.SplitNotifications WHERE UserId = @UserId AND IsRead = 0;";

            const string dataSql = @"
                SELECT 
                    n.Id,
                    n.UserId,
                    n.Type,
                    n.GroupId,
                    g.Name AS GroupName,
                    n.ReferenceId,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt,
                    n.ReadAt
                FROM dbo.SplitNotifications n
                LEFT JOIN dbo.SplitGroups g ON g.Id = n.GroupId
                WHERE n.UserId = @UserId
                ORDER BY n.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var connection = CreateConnection();
            using var multi = await connection.QueryMultipleAsync(countSql, new { UserId = userId });
            var totalCount = await multi.ReadFirstAsync<int>();
            var unreadCount = await multi.ReadFirstAsync<int>();

            var items = (await connection.QueryAsync<SplitNotificationDto>(dataSql, new
            {
                UserId = userId,
                Offset = offset,
                PageSize = pageSize
            })).ToList();

            return new PagedSplitNotificationResultDto
            {
                Items = items,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM dbo.SplitNotifications 
                WHERE UserId = @UserId AND IsRead = 0;";

            using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            const string sql = @"
                UPDATE dbo.SplitNotifications
                SET IsRead = 1,
                    ReadAt = SYSUTCDATETIME()
                WHERE Id = @NotificationId AND UserId = @UserId AND IsRead = 0;";

            using var connection = CreateConnection();
            var rows = await connection.ExecuteAsync(sql, new { NotificationId = notificationId, UserId = userId });
            return rows > 0;
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            const string sql = @"
                UPDATE dbo.SplitNotifications
                SET IsRead = 1,
                    ReadAt = SYSUTCDATETIME()
                WHERE UserId = @UserId AND IsRead = 0;";

            using var connection = CreateConnection();
            return await connection.ExecuteAsync(sql, new { UserId = userId });
        }

        public async Task<SplitNotification> CreateNotificationAsync(SplitNotification notification)
        {
            const string sql = @"
                INSERT INTO dbo.SplitNotifications
                    (Id, UserId, Type, GroupId, ReferenceId, Title, Message, IsRead, CreatedAt, ReadAt)
                VALUES
                    (@Id, @UserId, @Type, @GroupId, @ReferenceId, @Title, @Message, @IsRead, @CreatedAt, @ReadAt);";

            using var connection = CreateConnection();
            await connection.ExecuteAsync(sql, notification);
            return notification;
        }

        public async Task<bool> MarkInvitationNotificationAsReadAsync(Guid invitationId, Guid userId)
        {
            const string sql = @"
                UPDATE dbo.SplitNotifications
                SET IsRead = 1,
                    ReadAt = SYSUTCDATETIME()
                WHERE ReferenceId = @InvitationId AND UserId = @UserId AND IsRead = 0;";

            using var connection = CreateConnection();
            var rows = await connection.ExecuteAsync(sql, new { InvitationId = invitationId, UserId = userId });
            return rows > 0;
        }
    }
}
