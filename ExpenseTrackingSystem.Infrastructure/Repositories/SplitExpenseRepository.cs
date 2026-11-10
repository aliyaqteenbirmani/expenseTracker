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
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class SplitExpenseRepository : ISplitExpenseRepository
    {
        private readonly string _connectionString;

        public SplitExpenseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnectionStr")
                ?? throw new InvalidOperationException("DefaultConnectionStr is not configured.");
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<SplitExpense?> GetExpenseByIdAsync(Guid expenseId)
        {
            const string sql = @"
                SELECT Id, GroupId, PayerId, CreatedBy, Title, Description, Note, Amount, Category, 
                       ExpenseDate, ReceiptUrl, SplitType, CreatedAt, UpdatedAt, IsDeleted
                FROM dbo.SplitExpenses
                WHERE Id = @ExpenseId AND IsDeleted = 0;";

            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<SplitExpense>(sql, new { ExpenseId = expenseId });
        }

        public async Task<SplitExpenseDetailDto?> GetExpenseDetailsAsync(Guid expenseId, Guid groupId)
        {
            const string expenseSql = @"
                SELECT 
                    e.Id,
                    e.GroupId,
                    g.Name AS GroupName,
                    e.Title,
                    e.Amount,
                    e.PayerId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(up.FirstName, ' ', up.LastName))), ''), up.Email) AS PayerName,
                    up.Email AS PayerEmail,
                    e.CreatedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uc.FirstName, ' ', uc.LastName))), ''), uc.Email) AS CreatedByName,
                    uc.Email AS CreatedByEmail,
                    e.ExpenseDate,
                    e.Category,
                    e.Description,
                    e.Note,
                    e.ReceiptUrl,
                    e.SplitType,
                    e.CreatedAt,
                    e.UpdatedAt
                FROM dbo.SplitExpenses e
                INNER JOIN dbo.SplitGroups g ON g.Id = e.GroupId
                LEFT JOIN dbo.Users up ON up.Id = e.PayerId
                LEFT JOIN dbo.Users uc ON uc.Id = e.CreatedBy
                WHERE e.Id = @ExpenseId AND e.GroupId = @GroupId AND e.IsDeleted = 0;";

            using var connection = CreateConnection();
            var detail = await connection.QueryFirstOrDefaultAsync<SplitExpenseDetailDto>(expenseSql, new
            {
                ExpenseId = expenseId,
                GroupId = groupId
            });

            if (detail == null) return null;

            const string sharesSql = @"
                SELECT 
                    s.UserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''), u.Email) AS UserName,
                    u.Email AS UserEmail,
                    s.ShareAmount,
                    CASE WHEN s.UserId = @PayerId THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsPayer
                FROM dbo.SplitExpenseShares s
                LEFT JOIN dbo.Users u ON u.Id = s.UserId
                WHERE s.ExpenseId = @ExpenseId
                ORDER BY s.ShareAmount DESC, UserName ASC;";

            var shares = await connection.QueryAsync<SplitExpenseShareItemDto>(sharesSql, new
            {
                ExpenseId = expenseId,
                PayerId = detail.PayerId
            });

            detail.Shares = shares.ToList();
            return detail;
        }

        public async Task<PagedExpenseResultDto> GetExpensesListAsync(
            Guid groupId, Guid currentUserId, SplitExpenseQueryParameters parameters)
        {
            var sqlBuilder = new StringBuilder();
            var countSqlBuilder = new StringBuilder();
            var dynamicParams = new DynamicParameters();

            dynamicParams.Add("@GroupId", groupId);
            dynamicParams.Add("@CurrentUserId", currentUserId);

            countSqlBuilder.Append("SELECT COUNT(1) FROM dbo.SplitExpenses e WHERE e.GroupId = @GroupId AND e.IsDeleted = 0");
            sqlBuilder.Append(@"
                SELECT 
                    e.Id,
                    e.GroupId,
                    e.Title,
                    e.Amount,
                    e.PayerId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(up.FirstName, ' ', up.LastName))), ''), up.Email) AS PayerName,
                    e.ExpenseDate,
                    e.Category,
                    (SELECT COUNT(1) FROM dbo.SplitExpenseShares s WHERE s.ExpenseId = e.Id) AS ParticipantCount,
                    ISNULL((SELECT s2.ShareAmount FROM dbo.SplitExpenseShares s2 WHERE s2.ExpenseId = e.Id AND s2.UserId = @CurrentUserId), 0) AS CurrentUserShare,
                    e.CreatedBy,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(uc.FirstName, ' ', uc.LastName))), ''), uc.Email) AS CreatedByName,
                    e.CreatedAt
                FROM dbo.SplitExpenses e
                LEFT JOIN dbo.Users up ON up.Id = e.PayerId
                LEFT JOIN dbo.Users uc ON uc.Id = e.CreatedBy
                WHERE e.GroupId = @GroupId AND e.IsDeleted = 0");

            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                dynamicParams.Add("@SearchPattern", $"%{parameters.Search.Trim()}%");
                const string searchFilter = " AND (e.Title LIKE @SearchPattern OR e.Description LIKE @SearchPattern)";
                countSqlBuilder.Append(searchFilter);
                sqlBuilder.Append(searchFilter);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Category))
            {
                dynamicParams.Add("@Category", parameters.Category.Trim());
                const string categoryFilter = " AND e.Category = @Category";
                countSqlBuilder.Append(categoryFilter);
                sqlBuilder.Append(categoryFilter);
            }

            if (parameters.FromDate.HasValue)
            {
                dynamicParams.Add("@FromDate", parameters.FromDate.Value);
                const string fromDateFilter = " AND e.ExpenseDate >= @FromDate";
                countSqlBuilder.Append(fromDateFilter);
                sqlBuilder.Append(fromDateFilter);
            }

            if (parameters.ToDate.HasValue)
            {
                dynamicParams.Add("@ToDate", parameters.ToDate.Value);
                const string toDateFilter = " AND e.ExpenseDate <= @ToDate";
                countSqlBuilder.Append(toDateFilter);
                sqlBuilder.Append(toDateFilter);
            }

            var sortColumn = parameters.SortBy?.ToLowerInvariant() switch
            {
                "amount" => "e.Amount",
                "title" => "e.Title",
                _ => "e.ExpenseDate"
            };

            var sortDirection = parameters.IsDescending ? "DESC" : "ASC";
            sqlBuilder.Append($" ORDER BY {sortColumn} {sortDirection}, e.CreatedAt DESC");

            var pageNumber = parameters.PageNumber < 1 ? 1 : parameters.PageNumber;
            var pageSize = parameters.PageSize < 1 ? 20 : (parameters.PageSize > 100 ? 100 : parameters.PageSize);
            var offset = (pageNumber - 1) * pageSize;

            dynamicParams.Add("@Offset", offset);
            dynamicParams.Add("@PageSize", pageSize);

            sqlBuilder.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");

            using var connection = CreateConnection();
            var totalCount = await connection.ExecuteScalarAsync<int>(countSqlBuilder.ToString(), dynamicParams);
            var items = await connection.QueryAsync<SplitExpenseListItemDto>(sqlBuilder.ToString(), dynamicParams);

            return new PagedExpenseResultDto
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<SplitExpenseDetailDto> CreateExpenseWithSharesAndActivityAsync(
            SplitExpense expense, List<SplitExpenseShare> shares, string creatorName, string groupName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertExpenseSql = @"
                    INSERT INTO dbo.SplitExpenses
                        (Id, GroupId, PayerId, CreatedBy, Title, Description, Note, Amount, Category, 
                         ExpenseDate, ReceiptUrl, SplitType, CreatedAt, UpdatedAt, IsDeleted)
                    VALUES
                        (@Id, @GroupId, @PayerId, @CreatedBy, @Title, @Description, @Note, @Amount, @Category, 
                         @ExpenseDate, @ReceiptUrl, @SplitType, @CreatedAt, @UpdatedAt, @IsDeleted);";

                await connection.ExecuteAsync(insertExpenseSql, expense, transaction);

                const string insertShareSql = @"
                    INSERT INTO dbo.SplitExpenseShares
                        (Id, ExpenseId, UserId, ShareAmount, CreatedAt, UpdatedAt)
                    VALUES
                        (@Id, @ExpenseId, @UserId, @ShareAmount, @CreatedAt, @UpdatedAt);";

                await connection.ExecuteAsync(insertShareSql, shares, transaction);

                const string insertActivitySql = @"
                    INSERT INTO dbo.SplitGroupActivities 
                        (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                    VALUES 
                        (@ActivityId, @GroupId, @UserId, 'ExpenseAdded', @ReferenceId, @Description, SYSUTCDATETIME());";

                await connection.ExecuteAsync(insertActivitySql, new
                {
                    ActivityId = Guid.NewGuid(),
                    GroupId = expense.GroupId,
                    UserId = expense.CreatedBy,
                    ReferenceId = expense.Id,
                    Description = $"{creatorName} added expense \"{expense.Title}\" ({expense.Amount:N2})."
                }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return (await GetExpenseDetailsAsync(expense.Id, expense.GroupId))!;
        }

        public async Task<SplitExpenseDetailDto?> UpdateExpenseWithSharesAndActivityAsync(
            SplitExpense expense, List<SplitExpenseShare> newShares, string editorName, string groupName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string updateExpenseSql = @"
                    UPDATE dbo.SplitExpenses
                    SET Title = @Title,
                        Description = @Description,
                        Note = @Note,
                        Amount = @Amount,
                        Category = @Category,
                        ExpenseDate = @ExpenseDate,
                        PayerId = @PayerId,
                        ReceiptUrl = @ReceiptUrl,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @Id AND IsDeleted = 0;";

                await connection.ExecuteAsync(updateExpenseSql, new
                {
                    Id = expense.Id,
                    Title = expense.Title,
                    Description = expense.Description,
                    Note = expense.Note,
                    Amount = expense.Amount,
                    Category = expense.Category,
                    ExpenseDate = expense.ExpenseDate,
                    PayerId = expense.PayerId,
                    ReceiptUrl = expense.ReceiptUrl
                }, transaction);

                const string deleteSharesSql = @"
                    DELETE FROM dbo.SplitExpenseShares
                    WHERE ExpenseId = @ExpenseId;";

                await connection.ExecuteAsync(deleteSharesSql, new { ExpenseId = expense.Id }, transaction);

                const string insertShareSql = @"
                    INSERT INTO dbo.SplitExpenseShares
                        (Id, ExpenseId, UserId, ShareAmount, CreatedAt, UpdatedAt)
                    VALUES
                        (@Id, @ExpenseId, @UserId, @ShareAmount, @CreatedAt, @UpdatedAt);";

                await connection.ExecuteAsync(insertShareSql, newShares, transaction);

                const string insertActivitySql = @"
                    INSERT INTO dbo.SplitGroupActivities 
                        (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                    VALUES 
                        (@ActivityId, @GroupId, @UserId, 'ExpenseUpdated', @ReferenceId, @Description, SYSUTCDATETIME());";

                await connection.ExecuteAsync(insertActivitySql, new
                {
                    ActivityId = Guid.NewGuid(),
                    GroupId = expense.GroupId,
                    UserId = expense.CreatedBy,
                    ReferenceId = expense.Id,
                    Description = $"{editorName} updated expense \"{expense.Title}\" ({expense.Amount:N2})."
                }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return await GetExpenseDetailsAsync(expense.Id, expense.GroupId);
        }

        public async Task<bool> SoftDeleteExpenseWithActivityAsync(
            Guid expenseId, Guid groupId, Guid deletedByUserId, string deletedByName)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string getTitleSql = @"
                    SELECT Title FROM dbo.SplitExpenses
                    WHERE Id = @ExpenseId AND GroupId = @GroupId AND IsDeleted = 0;";

                var title = await connection.ExecuteScalarAsync<string>(getTitleSql, new
                {
                    ExpenseId = expenseId,
                    GroupId = groupId
                }, transaction);

                if (string.IsNullOrEmpty(title))
                {
                    transaction.Rollback();
                    return false;
                }

                const string softDeleteSql = @"
                    UPDATE dbo.SplitExpenses
                    SET IsDeleted = 1,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @ExpenseId;";

                await connection.ExecuteAsync(softDeleteSql, new { ExpenseId = expenseId }, transaction);

                const string insertActivitySql = @"
                    INSERT INTO dbo.SplitGroupActivities 
                        (Id, GroupId, UserId, ActivityType, ReferenceId, Description, CreatedAt)
                    VALUES 
                        (@ActivityId, @GroupId, @UserId, 'ExpenseDeleted', @ReferenceId, @Description, SYSUTCDATETIME());";

                await connection.ExecuteAsync(insertActivitySql, new
                {
                    ActivityId = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = deletedByUserId,
                    ReferenceId = expenseId,
                    Description = $"{deletedByName} deleted expense \"{title}\"."
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

        public async Task<List<SplitExpenseShare>> GetExpenseSharesAsync(Guid expenseId)
        {
            const string sql = @"
                SELECT Id, ExpenseId, UserId, ShareAmount, CreatedAt, UpdatedAt
                FROM dbo.SplitExpenseShares
                WHERE ExpenseId = @ExpenseId;";

            using var connection = CreateConnection();
            var shares = await connection.QueryAsync<SplitExpenseShare>(sql, new { ExpenseId = expenseId });
            return shares.ToList();
        }
    }
}
