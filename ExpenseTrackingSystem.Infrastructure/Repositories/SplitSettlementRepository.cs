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
    public class SplitSettlementRepository : ISplitSettlementRepository
    {
        private readonly string _connectionString;

        public SplitSettlementRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnectionStr")
                ?? throw new InvalidOperationException("DefaultConnectionStr is not configured.");
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<SplitSettlement?> GetSettlementByIdAsync(Guid settlementId)
        {
            const string sql = @"
                SELECT Id, GroupId, FromUserId, ToUserId, Amount, SettlementDate, Note, Status, CreatedBy, 
                       ReceiverActionBy, ReceiverActionAt, OwnerActionBy, OwnerActionAt, DeclineReason, CancelledAt, CancelledBy,
                       CreatedAt, UpdatedAt, IsDeleted
                FROM dbo.SplitSettlements
                WHERE Id = @SettlementId AND IsDeleted = 0;";

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<SplitSettlement>(sql, new { SettlementId = settlementId });
        }

        public async Task<SplitSettlementDto?> GetSettlementDetailsAsync(Guid settlementId, Guid groupId)
        {
            const string sql = @"
                SELECT 
                    s.Id,
                    s.GroupId,
                    g.Name AS GroupName,
                    g.OwnerId AS GroupOwnerId,
                    s.FromUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uf.FirstName, ' ', uf.LastName))), ''), uf.Email) AS FromUserName,
                    uf.Email AS FromUserEmail,
                    s.ToUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ut.FirstName, ' ', ut.LastName))), ''), ut.Email) AS ToUserName,
                    ut.Email AS ToUserEmail,
                    s.Amount,
                    s.SettlementDate,
                    s.Note,
                    s.Status,
                    s.CreatedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uc.FirstName, ' ', uc.LastName))), ''), uc.Email) AS CreatedByName,
                    s.ReceiverActionBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ur.FirstName, ' ', ur.LastName))), ''), ur.Email) AS ReceiverActionByName,
                    s.ReceiverActionAt,
                    s.OwnerActionBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uo.FirstName, ' ', uo.LastName))), ''), uo.Email) AS OwnerActionByName,
                    s.OwnerActionAt,
                    s.DeclineReason,
                    s.CancelledAt,
                    s.CancelledBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ux.FirstName, ' ', ux.LastName))), ''), ux.Email) AS CancelledByName,
                    s.CreatedAt,
                    s.UpdatedAt
                FROM dbo.SplitSettlements s
                INNER JOIN dbo.SplitGroups g ON g.Id = s.GroupId
                LEFT JOIN dbo.Users uf ON uf.Id = s.FromUserId
                LEFT JOIN dbo.Users ut ON ut.Id = s.ToUserId
                LEFT JOIN dbo.Users uc ON uc.Id = s.CreatedBy
                LEFT JOIN dbo.Users ur ON ur.Id = s.ReceiverActionBy
                LEFT JOIN dbo.Users uo ON uo.Id = s.OwnerActionBy
                LEFT JOIN dbo.Users ux ON ux.Id = s.CancelledBy
                WHERE s.Id = @SettlementId AND s.GroupId = @GroupId AND s.IsDeleted = 0;";

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<SplitSettlementDto>(sql, new
            {
                SettlementId = settlementId,
                GroupId = groupId
            });
        }

        public async Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId)
        {
            // Backward compatibility: default to group owner view if caller doesn't specify user
            return await GetGroupSettlementsAsync(groupId, Guid.Empty, true);
        }

        public async Task<List<SplitSettlementDto>> GetGroupSettlementsAsync(Guid groupId, Guid currentUserId, bool isOwner)
        {
            const string sql = @"
                SELECT 
                    s.Id,
                    s.GroupId,
                    g.Name AS GroupName,
                    g.OwnerId AS GroupOwnerId,
                    s.FromUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uf.FirstName, ' ', uf.LastName))), ''), uf.Email) AS FromUserName,
                    uf.Email AS FromUserEmail,
                    s.ToUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ut.FirstName, ' ', ut.LastName))), ''), ut.Email) AS ToUserName,
                    ut.Email AS ToUserEmail,
                    s.Amount,
                    s.SettlementDate,
                    s.Note,
                    s.Status,
                    s.CreatedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uc.FirstName, ' ', uc.LastName))), ''), uc.Email) AS CreatedByName,
                    s.ReceiverActionBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ur.FirstName, ' ', ur.LastName))), ''), ur.Email) AS ReceiverActionByName,
                    s.ReceiverActionAt,
                    s.OwnerActionBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uo.FirstName, ' ', uo.LastName))), ''), uo.Email) AS OwnerActionByName,
                    s.OwnerActionAt,
                    s.DeclineReason,
                    s.CancelledAt,
                    s.CancelledBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ux.FirstName, ' ', ux.LastName))), ''), ux.Email) AS CancelledByName,
                    s.CreatedAt,
                    s.UpdatedAt
                FROM dbo.SplitSettlements s
                INNER JOIN dbo.SplitGroups g ON g.Id = s.GroupId
                LEFT JOIN dbo.Users uf ON uf.Id = s.FromUserId
                LEFT JOIN dbo.Users ut ON ut.Id = s.ToUserId
                LEFT JOIN dbo.Users uc ON uc.Id = s.CreatedBy
                LEFT JOIN dbo.Users ur ON ur.Id = s.ReceiverActionBy
                LEFT JOIN dbo.Users uo ON uo.Id = s.OwnerActionBy
                LEFT JOIN dbo.Users ux ON ux.Id = s.CancelledBy
                WHERE s.GroupId = @GroupId AND s.IsDeleted = 0
                  AND (@IsOwner = 1 OR s.FromUserId = @CurrentUserId OR s.ToUserId = @CurrentUserId)
                ORDER BY s.SettlementDate DESC, s.CreatedAt DESC;";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitSettlementDto>(sql, new
            {
                GroupId = groupId,
                CurrentUserId = currentUserId,
                IsOwner = isOwner ? 1 : 0
            });
            return results.ToList();
        }

        public async Task<List<SplitSettlementDto>> GetPendingActionsAsync(Guid groupId, Guid currentUserId, bool isOwner)
        {
            const string sql = @"
                SELECT 
                    s.Id,
                    s.GroupId,
                    g.Name AS GroupName,
                    g.OwnerId AS GroupOwnerId,
                    s.FromUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uf.FirstName, ' ', uf.LastName))), ''), uf.Email) AS FromUserName,
                    uf.Email AS FromUserEmail,
                    s.ToUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ut.FirstName, ' ', ut.LastName))), ''), ut.Email) AS ToUserName,
                    ut.Email AS ToUserEmail,
                    s.Amount,
                    s.SettlementDate,
                    s.Note,
                    s.Status,
                    s.CreatedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uc.FirstName, ' ', uc.LastName))), ''), uc.Email) AS CreatedByName,
                    s.ReceiverActionBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ur.FirstName, ' ', ur.LastName))), ''), ur.Email) AS ReceiverActionByName,
                    s.ReceiverActionAt,
                    s.OwnerActionBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uo.FirstName, ' ', uo.LastName))), ''), uo.Email) AS OwnerActionByName,
                    s.OwnerActionAt,
                    s.DeclineReason,
                    s.CancelledAt,
                    s.CancelledBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ux.FirstName, ' ', ux.LastName))), ''), ux.Email) AS CancelledByName,
                    s.CreatedAt,
                    s.UpdatedAt
                FROM dbo.SplitSettlements s
                INNER JOIN dbo.SplitGroups g ON g.Id = s.GroupId
                LEFT JOIN dbo.Users uf ON uf.Id = s.FromUserId
                LEFT JOIN dbo.Users ut ON ut.Id = s.ToUserId
                LEFT JOIN dbo.Users uc ON uc.Id = s.CreatedBy
                LEFT JOIN dbo.Users ur ON ur.Id = s.ReceiverActionBy
                LEFT JOIN dbo.Users uo ON uo.Id = s.OwnerActionBy
                LEFT JOIN dbo.Users ux ON ux.Id = s.CancelledBy
                WHERE s.GroupId = @GroupId AND s.IsDeleted = 0
                  AND ((s.Status = 'PendingReceiver' AND s.ToUserId = @CurrentUserId)
                       OR (s.Status = 'PendingOwner' AND @IsOwner = 1))
                ORDER BY s.CreatedAt ASC;";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitSettlementDto>(sql, new
            {
                GroupId = groupId,
                CurrentUserId = currentUserId,
                IsOwner = isOwner ? 1 : 0
            });
            return results.ToList();
        }

        public async Task<List<SplitSettlement>> GetPendingSettlementsAsync(Guid groupId)
        {
            const string sql = @"
                SELECT Id, GroupId, FromUserId, ToUserId, Amount, SettlementDate, Note, Status, CreatedBy, 
                       ReceiverActionBy, ReceiverActionAt, OwnerActionBy, OwnerActionAt, DeclineReason, CancelledAt, CancelledBy,
                       CreatedAt, UpdatedAt, IsDeleted
                FROM dbo.SplitSettlements
                WHERE GroupId = @GroupId AND Status IN ('PendingReceiver', 'PendingOwner') AND IsDeleted = 0;";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitSettlement>(sql, new { GroupId = groupId });
            return results.ToList();
        }

        public async Task<decimal> GetPendingPayerToReceiverAmountAsync(Guid groupId, Guid fromUserId, Guid toUserId, Guid? excludeSettlementId = null)
        {
            const string sql = @"
                SELECT ISNULL(SUM(Amount), CAST(0 AS DECIMAL(18,2)))
                FROM dbo.SplitSettlements
                WHERE GroupId = @GroupId 
                  AND FromUserId = @FromUserId 
                  AND ToUserId = @ToUserId 
                  AND Status IN ('PendingReceiver', 'PendingOwner') 
                  AND IsDeleted = 0
                  AND (@ExcludeSettlementId IS NULL OR Id <> @ExcludeSettlementId);";

            using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                GroupId = groupId,
                FromUserId = fromUserId,
                ToUserId = toUserId,
                ExcludeSettlementId = excludeSettlementId
            });
        }

        public async Task<SplitSettlementDto> CreateSettlementAsync(SplitSettlement settlement, List<SplitNotification>? notifications = null)
        {
            const string insertSql = @"
                INSERT INTO dbo.SplitSettlements
                    (Id, GroupId, FromUserId, ToUserId, Amount, SettlementDate, Note, Status, CreatedBy, 
                     ReceiverActionBy, ReceiverActionAt, OwnerActionBy, OwnerActionAt, DeclineReason, CancelledAt, CancelledBy,
                     CreatedAt, UpdatedAt, IsDeleted)
                VALUES
                    (@Id, @GroupId, @FromUserId, @ToUserId, @Amount, @SettlementDate, @Note, @Status, @CreatedBy, 
                     @ReceiverActionBy, @ReceiverActionAt, @OwnerActionBy, @OwnerActionAt, @DeclineReason, @CancelledAt, @CancelledBy,
                     @CreatedAt, @UpdatedAt, @IsDeleted);";

            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(insertSql, settlement, transaction);

                if (notifications != null && notifications.Count > 0)
                {
                    const string notifSql = @"
                        INSERT INTO dbo.SplitNotifications
                            (Id, UserId, Type, GroupId, ReferenceId, Title, Message, IsRead, CreatedAt)
                        VALUES
                            (@Id, @UserId, @Type, @GroupId, @ReferenceId, @Title, @Message, @IsRead, @CreatedAt);";
                    await connection.ExecuteAsync(notifSql, notifications, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return (await GetSettlementDetailsAsync(settlement.Id, settlement.GroupId))!;
        }

        public async Task<int> ConfirmByReceiverAsync(Guid settlementId, string newStatus, Guid receiverUserId, List<SplitNotification>? notifications = null)
        {
            const string sql = @"
                UPDATE dbo.SplitSettlements
                SET Status = @NewStatus,
                    ReceiverActionBy = @ReceiverUserId,
                    ReceiverActionAt = SYSUTCDATETIME(),
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @SettlementId AND Status = 'PendingReceiver' AND IsDeleted = 0;";

            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var rows = await connection.ExecuteAsync(sql, new
                {
                    SettlementId = settlementId,
                    NewStatus = newStatus,
                    ReceiverUserId = receiverUserId
                }, transaction);

                if (rows > 0)
                {
                    const string markReadSql = @"
                        UPDATE dbo.SplitNotifications
                        SET IsRead = 1,
                            ReadAt = SYSUTCDATETIME()
                        WHERE ReferenceId = @SettlementId AND UserId = @ReceiverUserId AND IsRead = 0;";
                    await connection.ExecuteAsync(markReadSql, new { SettlementId = settlementId, ReceiverUserId = receiverUserId }, transaction);

                    if (notifications != null && notifications.Count > 0)
                    {
                        const string notifSql = @"
                            INSERT INTO dbo.SplitNotifications
                                (Id, UserId, Type, GroupId, ReferenceId, Title, Message, IsRead, CreatedAt)
                            VALUES
                                (@Id, @UserId, @Type, @GroupId, @ReferenceId, @Title, @Message, @IsRead, @CreatedAt);";
                        await connection.ExecuteAsync(notifSql, notifications, transaction);
                    }
                }

                transaction.Commit();
                return rows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> DeclineByReceiverAsync(Guid settlementId, string? reason, Guid receiverUserId, List<SplitNotification>? notifications = null)
        {
            const string sql = @"
                UPDATE dbo.SplitSettlements
                SET Status = 'DeclinedByReceiver',
                    ReceiverActionBy = @ReceiverUserId,
                    ReceiverActionAt = SYSUTCDATETIME(),
                    DeclineReason = @Reason,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @SettlementId AND Status = 'PendingReceiver' AND IsDeleted = 0;";

            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var rows = await connection.ExecuteAsync(sql, new
                {
                    SettlementId = settlementId,
                    Reason = reason?.Trim(),
                    ReceiverUserId = receiverUserId
                }, transaction);

                if (rows > 0)
                {
                    const string markReadSql = @"
                        UPDATE dbo.SplitNotifications
                        SET IsRead = 1,
                            ReadAt = SYSUTCDATETIME()
                        WHERE ReferenceId = @SettlementId AND UserId = @ReceiverUserId AND IsRead = 0;";
                    await connection.ExecuteAsync(markReadSql, new { SettlementId = settlementId, ReceiverUserId = receiverUserId }, transaction);

                    if (notifications != null && notifications.Count > 0)
                    {
                        const string notifSql = @"
                            INSERT INTO dbo.SplitNotifications
                                (Id, UserId, Type, GroupId, ReferenceId, Title, Message, IsRead, CreatedAt)
                            VALUES
                                (@Id, @UserId, @Type, @GroupId, @ReferenceId, @Title, @Message, @IsRead, @CreatedAt);";
                        await connection.ExecuteAsync(notifSql, notifications, transaction);
                    }
                }

                transaction.Commit();
                return rows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> ApproveByOwnerAsync(Guid settlementId, Guid ownerUserId, List<SplitNotification>? notifications = null)
        {
            const string sql = @"
                UPDATE dbo.SplitSettlements
                SET Status = 'Confirmed',
                    OwnerActionBy = @OwnerUserId,
                    OwnerActionAt = SYSUTCDATETIME(),
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @SettlementId AND Status = 'PendingOwner' AND IsDeleted = 0;";

            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var rows = await connection.ExecuteAsync(sql, new
                {
                    SettlementId = settlementId,
                    OwnerUserId = ownerUserId
                }, transaction);

                if (rows > 0)
                {
                    const string markReadSql = @"
                        UPDATE dbo.SplitNotifications
                        SET IsRead = 1,
                            ReadAt = SYSUTCDATETIME()
                        WHERE ReferenceId = @SettlementId AND UserId = @OwnerUserId AND IsRead = 0;";
                    await connection.ExecuteAsync(markReadSql, new { SettlementId = settlementId, OwnerUserId = ownerUserId }, transaction);

                    if (notifications != null && notifications.Count > 0)
                    {
                        const string notifSql = @"
                            INSERT INTO dbo.SplitNotifications
                                (Id, UserId, Type, GroupId, ReferenceId, Title, Message, IsRead, CreatedAt)
                            VALUES
                                (@Id, @UserId, @Type, @GroupId, @ReferenceId, @Title, @Message, @IsRead, @CreatedAt);";
                        await connection.ExecuteAsync(notifSql, notifications, transaction);
                    }
                }

                transaction.Commit();
                return rows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> DeclineByOwnerAsync(Guid settlementId, string? reason, Guid ownerUserId, List<SplitNotification>? notifications = null)
        {
            const string sql = @"
                UPDATE dbo.SplitSettlements
                SET Status = 'DeclinedByOwner',
                    OwnerActionBy = @OwnerUserId,
                    OwnerActionAt = SYSUTCDATETIME(),
                    DeclineReason = @Reason,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @SettlementId AND Status = 'PendingOwner' AND IsDeleted = 0;";

            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var rows = await connection.ExecuteAsync(sql, new
                {
                    SettlementId = settlementId,
                    Reason = reason?.Trim(),
                    OwnerUserId = ownerUserId
                }, transaction);

                if (rows > 0)
                {
                    const string markReadSql = @"
                        UPDATE dbo.SplitNotifications
                        SET IsRead = 1,
                            ReadAt = SYSUTCDATETIME()
                        WHERE ReferenceId = @SettlementId AND UserId = @OwnerUserId AND IsRead = 0;";
                    await connection.ExecuteAsync(markReadSql, new { SettlementId = settlementId, OwnerUserId = ownerUserId }, transaction);

                    if (notifications != null && notifications.Count > 0)
                    {
                        const string notifSql = @"
                            INSERT INTO dbo.SplitNotifications
                                (Id, UserId, Type, GroupId, ReferenceId, Title, Message, IsRead, CreatedAt)
                            VALUES
                                (@Id, @UserId, @Type, @GroupId, @ReferenceId, @Title, @Message, @IsRead, @CreatedAt);";
                        await connection.ExecuteAsync(notifSql, notifications, transaction);
                    }
                }

                transaction.Commit();
                return rows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> CancelByPayerAsync(Guid settlementId, Guid payerUserId, List<SplitNotification>? notifications = null)
        {
            const string sql = @"
                UPDATE dbo.SplitSettlements
                SET Status = 'Cancelled',
                    CancelledBy = @PayerUserId,
                    CancelledAt = SYSUTCDATETIME(),
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @SettlementId AND Status IN ('PendingReceiver', 'PendingOwner') AND IsDeleted = 0;";

            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var rows = await connection.ExecuteAsync(sql, new
                {
                    SettlementId = settlementId,
                    PayerUserId = payerUserId
                }, transaction);

                if (rows > 0)
                {
                    const string updatePendingNotifSql = @"
                        UPDATE dbo.SplitNotifications
                        SET Type = 'SettlementCancelled',
                            Message = CASE 
                                WHEN Message LIKE '%(Cancelled by payer)%' THEN Message 
                                ELSE RTRIM(Message) + ' (Cancelled by payer)' 
                            END,
                            IsRead = 1,
                            ReadAt = SYSUTCDATETIME()
                        WHERE ReferenceId = @SettlementId 
                          AND Type IN ('SettlementRequested', 'SettlementAwaitingOwner', 'SettlementReminder');";
                    await connection.ExecuteAsync(updatePendingNotifSql, new { SettlementId = settlementId }, transaction);

                    if (notifications != null && notifications.Count > 0)
                    {
                        const string notifSql = @"
                            INSERT INTO dbo.SplitNotifications
                                (Id, UserId, Type, GroupId, ReferenceId, Title, Message, IsRead, CreatedAt)
                            VALUES
                                (@Id, @UserId, @Type, @GroupId, @ReferenceId, @Title, @Message, @IsRead, @CreatedAt);";
                        await connection.ExecuteAsync(notifSql, notifications, transaction);
                    }
                }

                transaction.Commit();
                return rows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> ReverseConfirmedSettlementAsync(Guid settlementId, Guid groupId, Guid ownerUserId, List<SplitNotification>? notifications = null)
        {
            const string sql = @"
                UPDATE dbo.SplitSettlements
                SET IsDeleted = 1,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @SettlementId AND GroupId = @GroupId AND Status = 'Confirmed' AND IsDeleted = 0;";

            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var rows = await connection.ExecuteAsync(sql, new
                {
                    SettlementId = settlementId,
                    GroupId = groupId,
                    OwnerUserId = ownerUserId
                }, transaction);

                if (rows > 0 && notifications != null && notifications.Count > 0)
                {
                    const string notifSql = @"
                        INSERT INTO dbo.SplitNotifications
                            (Id, UserId, Type, GroupId, ReferenceId, Title, Message, IsRead, CreatedAt)
                        VALUES
                            (@Id, @UserId, @Type, @GroupId, @ReferenceId, @Title, @Message, @IsRead, @CreatedAt);";
                    await connection.ExecuteAsync(notifSql, notifications, transaction);
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

        public async Task<List<SplitSettlementDto>> GetSettlementsNeedingReminderAsync(DateTime olderThan)
        {
            const string sql = @"
                SELECT 
                    s.Id,
                    s.GroupId,
                    g.Name AS GroupName,
                    g.OwnerId AS GroupOwnerId,
                    s.FromUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uf.FirstName, ' ', uf.LastName))), ''), uf.Email) AS FromUserName,
                    uf.Email AS FromUserEmail,
                    s.ToUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ut.FirstName, ' ', ut.LastName))), ''), ut.Email) AS ToUserName,
                    ut.Email AS ToUserEmail,
                    s.Amount,
                    s.SettlementDate,
                    s.Note,
                    s.Status,
                    s.CreatedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uc.FirstName, ' ', uc.LastName))), ''), uc.Email) AS CreatedByName,
                    s.ReceiverActionBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ur.FirstName, ' ', ur.LastName))), ''), ur.Email) AS ReceiverActionByName,
                    s.ReceiverActionAt,
                    s.OwnerActionBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uo.FirstName, ' ', uo.LastName))), ''), uo.Email) AS OwnerActionByName,
                    s.OwnerActionAt,
                    s.DeclineReason,
                    s.CancelledAt,
                    s.CancelledBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ux.FirstName, ' ', ux.LastName))), ''), ux.Email) AS CancelledByName,
                    s.CreatedAt,
                    s.UpdatedAt
                FROM dbo.SplitSettlements s
                INNER JOIN dbo.SplitGroups g ON g.Id = s.GroupId
                LEFT JOIN dbo.Users uf ON uf.Id = s.FromUserId
                LEFT JOIN dbo.Users ut ON ut.Id = s.ToUserId
                LEFT JOIN dbo.Users uc ON uc.Id = s.CreatedBy
                LEFT JOIN dbo.Users ur ON ur.Id = s.ReceiverActionBy
                LEFT JOIN dbo.Users uo ON uo.Id = s.OwnerActionBy
                LEFT JOIN dbo.Users ux ON ux.Id = s.CancelledBy
                WHERE s.Status IN ('PendingReceiver', 'PendingOwner')
                  AND s.CreatedAt <= @OlderThan
                  AND s.IsDeleted = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.SplitNotifications sn
                      WHERE sn.ReferenceId = s.Id AND sn.Type = 'SettlementReminder'
                  );";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitSettlementDto>(sql, new { OlderThan = olderThan });
            return results.ToList();
        }

        public async Task<bool> AddReminderNotificationAsync(SplitNotification reminderNotification)
        {
            const string checkSql = @"
                SELECT COUNT(*) 
                FROM dbo.SplitNotifications 
                WHERE ReferenceId = @ReferenceId AND Type = 'SettlementReminder' AND UserId = @UserId;";

            const string insertSql = @"
                INSERT INTO dbo.SplitNotifications
                    (Id, UserId, Type, GroupId, ReferenceId, Title, Message, IsRead, CreatedAt)
                VALUES
                    (@Id, @UserId, @Type, @GroupId, @ReferenceId, @Title, @Message, @IsRead, @CreatedAt);";

            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var exists = await connection.ExecuteScalarAsync<int>(checkSql, new
                {
                    reminderNotification.ReferenceId,
                    reminderNotification.UserId
                }, transaction);

                if (exists > 0)
                {
                    transaction.Rollback();
                    return false;
                }

                await connection.ExecuteAsync(insertSql, reminderNotification, transaction);
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<SplitSettlementDto> CreateSettlementWithActivityAsync(
            SplitSettlement settlement, string creatorName, string groupName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertSettlementSql = @"
                    INSERT INTO dbo.SplitSettlements
                        (Id, GroupId, FromUserId, ToUserId, Amount, SettlementDate, Note, CreatedBy, CreatedAt, UpdatedAt, IsDeleted)
                    VALUES
                        (@Id, @GroupId, @FromUserId, @ToUserId, @Amount, @SettlementDate, @Note, @CreatedBy, @CreatedAt, @UpdatedAt, @IsDeleted);";

                await connection.ExecuteAsync(insertSettlementSql, settlement, transaction);

                const string getUserNamesSql = @"
                    SELECT 
                        (SELECT ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(FirstName, ' ', LastName))), ''), Email) FROM dbo.Users WHERE Id = @FromUserId) AS FromName,
                        (SELECT ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(FirstName, ' ', LastName))), ''), Email) FROM dbo.Users WHERE Id = @ToUserId) AS ToName;";

                var names = await connection.QueryFirstOrDefaultAsync<dynamic>(getUserNamesSql, new
                {
                    FromUserId = settlement.FromUserId,
                    ToUserId = settlement.ToUserId
                }, transaction);

                string fromName = names?.FromName ?? "Member";
                string toName = names?.ToName ?? "Member";

                const string insertActivitySql = @"
                    INSERT INTO dbo.SplitGroupActivities 
                        (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                    VALUES 
                        (@ActivityId, @GroupId, @UserId, 'SettlementRecorded', @ReferenceId, @Description, SYSUTCDATETIME());";

                await connection.ExecuteAsync(insertActivitySql, new
                {
                    ActivityId = Guid.NewGuid(),
                    GroupId = settlement.GroupId,
                    UserId = settlement.CreatedBy,
                    ReferenceId = settlement.Id,
                    Description = $"{fromName} paid {toName} Rs. {settlement.Amount:N2}."
                }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return (await GetSettlementDetailsAsync(settlement.Id, settlement.GroupId))!;
        }

        public async Task<SplitSettlementDto?> UpdateSettlementWithActivityAsync(
            SplitSettlement settlement, string editorName, string groupName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateSql = @"
                    UPDATE dbo.SplitSettlements
                    SET FromUserId = @FromUserId,
                        ToUserId = @ToUserId,
                        Amount = @Amount,
                        SettlementDate = @SettlementDate,
                        Note = @Note,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @Id AND GroupId = @GroupId AND IsDeleted = 0;";

                var rows = await connection.ExecuteAsync(updateSql, new
                {
                    Id = settlement.Id,
                    GroupId = settlement.GroupId,
                    FromUserId = settlement.FromUserId,
                    ToUserId = settlement.ToUserId,
                    Amount = settlement.Amount,
                    SettlementDate = settlement.SettlementDate,
                    Note = settlement.Note
                }, transaction);

                if (rows == 0)
                {
                    transaction.Rollback();
                    return null;
                }

                const string insertActivitySql = @"
                    INSERT INTO dbo.SplitGroupActivities 
                        (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                    VALUES 
                        (@ActivityId, @GroupId, @UserId, 'SettlementUpdated', @ReferenceId, @Description, SYSUTCDATETIME());";

                await connection.ExecuteAsync(insertActivitySql, new
                {
                    ActivityId = Guid.NewGuid(),
                    GroupId = settlement.GroupId,
                    UserId = settlement.CreatedBy,
                    ReferenceId = settlement.Id,
                    Description = $"{editorName} updated settlement of Rs. {settlement.Amount:N2}."
                }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return await GetSettlementDetailsAsync(settlement.Id, settlement.GroupId);
        }

        public async Task<bool> SoftDeleteSettlementWithActivityAsync(
            Guid settlementId, Guid groupId, Guid deletedByUserId, string deletedByName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string getDetailsSql = @"
                    SELECT Amount FROM dbo.SplitSettlements
                    WHERE Id = @SettlementId AND GroupId = @GroupId AND IsDeleted = 0;";

                var amount = await connection.ExecuteScalarAsync<decimal?>(getDetailsSql, new
                {
                    SettlementId = settlementId,
                    GroupId = groupId
                }, transaction);

                if (!amount.HasValue)
                {
                    transaction.Rollback();
                    return false;
                }

                const string softDeleteSql = @"
                    UPDATE dbo.SplitSettlements
                    SET IsDeleted = 1,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @SettlementId AND GroupId = @GroupId AND IsDeleted = 0;";

                var rows = await connection.ExecuteAsync(softDeleteSql, new
                {
                    SettlementId = settlementId,
                    GroupId = groupId
                }, transaction);

                if (rows == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                const string insertActivitySql = @"
                    INSERT INTO dbo.SplitGroupActivities 
                        (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                    VALUES 
                        (@ActivityId, @GroupId, @UserId, 'SettlementDeleted', @ReferenceId, @Description, SYSUTCDATETIME());";

                await connection.ExecuteAsync(insertActivitySql, new
                {
                    ActivityId = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = deletedByUserId,
                    ReferenceId = settlementId,
                    Description = $"{deletedByName} deleted settlement of Rs. {amount.Value:N2}."
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
    }
}
