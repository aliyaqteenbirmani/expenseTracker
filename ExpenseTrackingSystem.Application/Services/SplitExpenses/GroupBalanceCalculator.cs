using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public record ExpenseRecord(Guid Id, Guid PayerId, decimal Amount, bool IsDeleted, IReadOnlyList<ShareRecord> Shares);
    public record ShareRecord(Guid UserId, decimal ShareAmount);
    public record SettlementRecord(Guid Id, Guid FromUserId, Guid ToUserId, decimal Amount, bool IsDeleted, string Status = "Confirmed");
    public record MemberRecord(Guid UserId, string FullName, string Email, string Role, bool IsActive);

    public static class GroupBalanceCalculator
    {
        /// <summary>
        /// Pure financial calculation of group balances:
        /// Net Balance = (Total Paid - Total Shares) + (Settlements Paid - Settlements Received)
        /// Invariant: Sum(NetBalance) across all participants in a group == 0.00
        /// </summary>
        public static List<MemberBalanceDto> CalculateBalances(
            IEnumerable<MemberRecord> members,
            IEnumerable<ExpenseRecord> expenses,
            IEnumerable<SettlementRecord> settlements)
        {
            var activeExpenses = expenses.Where(e => !e.IsDeleted).ToList();
            var activeSettlements = settlements.Where(s => !s.IsDeleted && s.Status == "Confirmed").ToList();

            // Collect all user IDs from active members + historical participants
            var allUserIds = new HashSet<Guid>(members.Select(m => m.UserId));
            foreach (var e in activeExpenses)
            {
                allUserIds.Add(e.PayerId);
                foreach (var s in e.Shares)
                    allUserIds.Add(s.UserId);
            }
            foreach (var st in activeSettlements)
            {
                allUserIds.Add(st.FromUserId);
                allUserIds.Add(st.ToUserId);
            }

            var memberLookup = members.ToDictionary(m => m.UserId);
            var results = new List<MemberBalanceDto>();

            foreach (var userId in allUserIds)
            {
                var member = memberLookup.TryGetValue(userId, out var m) ? m : null;
                var fullName = member?.FullName ?? "Unknown User";
                var email = member?.Email ?? string.Empty;
                var role = member?.Role ?? "Member";
                var isActive = member?.IsActive ?? false;

                var totalPaid = activeExpenses
                    .Where(e => e.PayerId == userId)
                    .Sum(e => e.Amount);

                var totalShare = activeExpenses
                    .SelectMany(e => e.Shares)
                    .Where(s => s.UserId == userId)
                    .Sum(s => s.ShareAmount);

                var settlementsPaid = activeSettlements
                    .Where(s => s.FromUserId == userId)
                    .Sum(s => s.Amount);

                var settlementsReceived = activeSettlements
                    .Where(s => s.ToUserId == userId)
                    .Sum(s => s.Amount);

                var netBalance = (totalPaid - totalShare) + (settlementsPaid - settlementsReceived);

                string statusDescription;
                if (netBalance > 0)
                    statusDescription = $"{fullName} is owed Rs. {netBalance:N2}";
                else if (netBalance < 0)
                    statusDescription = $"{fullName} owes Rs. {Math.Abs(netBalance):N2}";
                else
                    statusDescription = "Settled up";

                results.Add(new MemberBalanceDto
                {
                    UserId = userId,
                    FullName = fullName,
                    Email = email,
                    Role = role,
                    IsActive = isActive,
                    TotalPaid = totalPaid,
                    TotalShare = totalShare,
                    SettlementsPaid = settlementsPaid,
                    SettlementsReceived = settlementsReceived,
                    NetBalance = netBalance,
                    StatusDescription = statusDescription
                });
            }

            return results
                .OrderByDescending(r => r.IsActive)
                .ThenByDescending(r => r.NetBalance)
                .ThenBy(r => r.FullName)
                .ToList();
        }
    }
}
