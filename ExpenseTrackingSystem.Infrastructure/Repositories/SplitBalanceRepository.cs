using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class SplitBalanceRepository : ISplitBalanceRepository
    {
        private readonly string _connectionString;

        public SplitBalanceRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnectionStr")
                ?? throw new InvalidOperationException("DefaultConnectionStr is not configured.");
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<List<MemberBalanceDto>> GetGroupBalancesAsync(Guid groupId)
        {
            const string sql = @"
                WITH GroupUsers AS (
                    -- Active group members
                    SELECT gm.UserId, gm.Role, gm.IsActive
                    FROM dbo.SplitGroupMembers gm
                    WHERE gm.GroupId = @GroupId AND gm.IsActive = 1

                    UNION

                    -- Removed/inactive members who have historical financial records in this group
                    SELECT DISTINCT u_hist.UserId, ISNULL(gm2.Role, 'Member') AS Role, ISNULL(gm2.IsActive, CAST(0 AS BIT)) AS IsActive
                    FROM (
                        SELECT PayerId AS UserId FROM dbo.SplitExpenses WHERE GroupId = @GroupId AND IsDeleted = 0
                        UNION
                        SELECT s.UserId FROM dbo.SplitExpenseShares s 
                        INNER JOIN dbo.SplitExpenses e ON e.Id = s.ExpenseId 
                        WHERE e.GroupId = @GroupId AND e.IsDeleted = 0
                        UNION
                        SELECT FromUserId AS UserId FROM dbo.SplitSettlements WHERE GroupId = @GroupId AND IsDeleted = 0
                        UNION
                        SELECT ToUserId AS UserId FROM dbo.SplitSettlements WHERE GroupId = @GroupId AND IsDeleted = 0
                    ) u_hist
                    LEFT JOIN dbo.SplitGroupMembers gm2 ON gm2.GroupId = @GroupId AND gm2.UserId = u_hist.UserId
                ),
                ExpensePaid AS (
                    SELECT PayerId AS UserId, SUM(Amount) AS TotalPaid
                    FROM dbo.SplitExpenses
                    WHERE GroupId = @GroupId AND IsDeleted = 0
                    GROUP BY PayerId
                ),
                ExpenseShares AS (
                    SELECT s.UserId, SUM(s.ShareAmount) AS TotalShare
                    FROM dbo.SplitExpenseShares s
                    INNER JOIN dbo.SplitExpenses e ON e.Id = s.ExpenseId
                    WHERE e.GroupId = @GroupId AND e.IsDeleted = 0
                    GROUP BY s.UserId
                ),
                SettlementsPaid AS (
                    SELECT FromUserId AS UserId, SUM(Amount) AS TotalSettlementsPaid
                    FROM dbo.SplitSettlements
                    WHERE GroupId = @GroupId AND IsDeleted = 0
                    GROUP BY FromUserId
                ),
                SettlementsReceived AS (
                    SELECT ToUserId AS UserId, SUM(Amount) AS TotalSettlementsReceived
                    FROM dbo.SplitSettlements
                    WHERE GroupId = @GroupId AND IsDeleted = 0
                    GROUP BY ToUserId
                )
                SELECT 
                    gu.UserId,
                    ISNULL(NULLIF(RTRIM(LTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''), u.Email) AS FullName,
                    u.Email,
                    gu.Role,
                    gu.IsActive,
                    ISNULL(ep.TotalPaid, CAST(0 AS DECIMAL(18,2))) AS TotalPaid,
                    ISNULL(es.TotalShare, CAST(0 AS DECIMAL(18,2))) AS TotalShare,
                    ISNULL(sp.TotalSettlementsPaid, CAST(0 AS DECIMAL(18,2))) AS SettlementsPaid,
                    ISNULL(sr.TotalSettlementsReceived, CAST(0 AS DECIMAL(18,2))) AS SettlementsReceived,
                    (ISNULL(ep.TotalPaid, 0) - ISNULL(es.TotalShare, 0) + ISNULL(sp.TotalSettlementsPaid, 0) - ISNULL(sr.TotalSettlementsReceived, 0)) AS NetBalance
                FROM GroupUsers gu
                INNER JOIN dbo.Users u ON u.Id = gu.UserId
                LEFT JOIN ExpensePaid ep ON ep.UserId = gu.UserId
                LEFT JOIN ExpenseShares es ON es.UserId = gu.UserId
                LEFT JOIN SettlementsPaid sp ON sp.UserId = gu.UserId
                LEFT JOIN SettlementsReceived sr ON sr.UserId = gu.UserId
                ORDER BY gu.IsActive DESC, NetBalance DESC, FullName ASC;";

            using var connection = CreateConnection();
            var results = (await connection.QueryAsync<MemberBalanceDto>(sql, new { GroupId = groupId })).ToList();

            foreach (var item in results)
            {
                item.StatusDescription = FormatStatusDescription(item.NetBalance, item.FullName);
            }

            return results;
        }

        public async Task<MemberBalanceDto?> GetMemberBalanceAsync(Guid groupId, Guid userId)
        {
            var balances = await GetGroupBalancesAsync(groupId);
            return balances.FirstOrDefault(b => b.UserId == userId);
        }

        public async Task<GroupFinancialSummaryDto?> GetGroupFinancialSummaryAsync(Guid groupId, Guid currentUserId)
        {
            const string groupSql = @"
                SELECT Id, Name
                FROM dbo.SplitGroups
                WHERE Id = @GroupId AND IsDeleted = 0;";

            using var connection = CreateConnection();
            var group = await connection.QueryFirstOrDefaultAsync<dynamic>(groupSql, new { GroupId = groupId });
            if (group == null) return null;

            const string totalsSql = @"
                SELECT 
                    ISNULL((SELECT SUM(Amount) FROM dbo.SplitExpenses WHERE GroupId = @GroupId AND IsDeleted = 0), CAST(0 AS DECIMAL(18,2))) AS TotalExpenses,
                    ISNULL((SELECT SUM(Amount) FROM dbo.SplitSettlements WHERE GroupId = @GroupId AND IsDeleted = 0), CAST(0 AS DECIMAL(18,2))) AS TotalSettlements;";

            var totals = await connection.QueryFirstOrDefaultAsync<dynamic>(totalsSql, new { GroupId = groupId });
            decimal totalExpenses = totals?.TotalExpenses ?? 0m;
            decimal totalSettlements = totals?.TotalSettlements ?? 0m;

            var allBalances = await GetGroupBalancesAsync(groupId);
            var currentUserBalance = allBalances.FirstOrDefault(b => b.UserId == currentUserId);

            var netBalance = currentUserBalance?.NetBalance ?? 0m;
            var userStatusText = FormatCurrentUserStatus(netBalance);

            return new GroupFinancialSummaryDto
            {
                GroupId = groupId,
                GroupName = group.Name,
                TotalExpenses = totalExpenses,
                TotalSettlements = totalSettlements,
                CurrentUserBalance = netBalance,
                CurrentUserTotalPaid = currentUserBalance?.TotalPaid ?? 0m,
                CurrentUserTotalShare = currentUserBalance?.TotalShare ?? 0m,
                CurrentUserStatusText = userStatusText,
                MemberCount = allBalances.Count,
                SettledMembersCount = allBalances.Count(b => b.NetBalance == 0m),
                UnsettledMembersCount = allBalances.Count(b => b.NetBalance != 0m)
            };
        }

        private static string FormatStatusDescription(decimal netBalance, string name)
        {
            if (netBalance > 0)
                return $"{name} is owed Rs. {netBalance:N2}";
            if (netBalance < 0)
                return $"{name} owes Rs. {Math.Abs(netBalance):N2}";
            return "Settled up";
        }

        private static string FormatCurrentUserStatus(decimal netBalance)
        {
            if (netBalance > 0)
                return $"You are owed Rs. {netBalance:N2}";
            if (netBalance < 0)
                return $"You owe Rs. {Math.Abs(netBalance):N2}";
            return "Settled up";
        }
    }
}
