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
                SELECT Id, GroupId, FromUserId, ToUserId, Amount, SettlementDate, Note, CreatedBy, CreatedAt, UpdatedAt, IsDeleted
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
                    s.FromUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uf.FirstName, ' ', uf.LastName))), ''), uf.Email) AS FromUserName,
                    uf.Email AS FromUserEmail,
                    s.ToUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ut.FirstName, ' ', ut.LastName))), ''), ut.Email) AS ToUserName,
                    ut.Email AS ToUserEmail,
                    s.Amount,
                    s.SettlementDate,
                    s.Note,
                    s.CreatedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uc.FirstName, ' ', uc.LastName))), ''), uc.Email) AS CreatedByName,
                    s.CreatedAt,
                    s.UpdatedAt
                FROM dbo.SplitSettlements s
                INNER JOIN dbo.SplitGroups g ON g.Id = s.GroupId
                LEFT JOIN dbo.Users uf ON uf.Id = s.FromUserId
                LEFT JOIN dbo.Users ut ON ut.Id = s.ToUserId
                LEFT JOIN dbo.Users uc ON uc.Id = s.CreatedBy
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
            const string sql = @"
                SELECT 
                    s.Id,
                    s.GroupId,
                    g.Name AS GroupName,
                    s.FromUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uf.FirstName, ' ', uf.LastName))), ''), uf.Email) AS FromUserName,
                    uf.Email AS FromUserEmail,
                    s.ToUserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(ut.FirstName, ' ', ut.LastName))), ''), ut.Email) AS ToUserName,
                    ut.Email AS ToUserEmail,
                    s.Amount,
                    s.SettlementDate,
                    s.Note,
                    s.CreatedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uc.FirstName, ' ', uc.LastName))), ''), uc.Email) AS CreatedByName,
                    s.CreatedAt,
                    s.UpdatedAt
                FROM dbo.SplitSettlements s
                INNER JOIN dbo.SplitGroups g ON g.Id = s.GroupId
                LEFT JOIN dbo.Users uf ON uf.Id = s.FromUserId
                LEFT JOIN dbo.Users ut ON ut.Id = s.ToUserId
                LEFT JOIN dbo.Users uc ON uc.Id = s.CreatedBy
                WHERE s.GroupId = @GroupId AND s.IsDeleted = 0
                ORDER BY s.SettlementDate DESC, s.CreatedAt DESC;";

            using var connection = CreateConnection();
            var results = await connection.QueryAsync<SplitSettlementDto>(sql, new { GroupId = groupId });
            return results.ToList();
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
                    WHERE Id = @Id AND IsDeleted = 0;";

                await connection.ExecuteAsync(updateSql, new
                {
                    Id = settlement.Id,
                    FromUserId = settlement.FromUserId,
                    ToUserId = settlement.ToUserId,
                    Amount = settlement.Amount,
                    SettlementDate = settlement.SettlementDate,
                    Note = settlement.Note
                }, transaction);

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
                    WHERE Id = @SettlementId;";

                await connection.ExecuteAsync(softDeleteSql, new { SettlementId = settlementId }, transaction);

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
