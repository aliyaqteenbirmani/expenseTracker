using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class DebtSimplificationService : IDebtSimplificationService
    {
        private class MemberBalanceItem
        {
            public Guid UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }

        public List<SuggestedSettlementDto> SimplifyDebts(IEnumerable<MemberBalanceDto> memberBalances)
        {
            if (memberBalances == null)
                return new List<SuggestedSettlementDto>();

            // Debtors: members with negative net balance (they owe money)
            var debtors = memberBalances
                .Where(b => b.NetBalance < -0.005m)
                .Select(b => new MemberBalanceItem
                {
                    UserId = b.UserId,
                    FullName = b.FullName,
                    Email = b.Email,
                    Amount = Math.Abs(b.NetBalance)
                })
                .OrderByDescending(d => d.Amount)
                .ThenBy(d => d.UserId)
                .ToList();

            // Creditors: members with positive net balance (they are owed money)
            var creditors = memberBalances
                .Where(b => b.NetBalance > 0.005m)
                .Select(b => new MemberBalanceItem
                {
                    UserId = b.UserId,
                    FullName = b.FullName,
                    Email = b.Email,
                    Amount = b.NetBalance
                })
                .OrderByDescending(c => c.Amount)
                .ThenBy(c => c.UserId)
                .ToList();

            var suggestions = new List<SuggestedSettlementDto>();
            int debtorIndex = 0;
            int creditorIndex = 0;

            while (debtorIndex < debtors.Count && creditorIndex < creditors.Count)
            {
                var debtor = debtors[debtorIndex];
                var creditor = creditors[creditorIndex];

                // Greedily settle the smaller of the debtor's debt or creditor's credit
                var paymentAmount = Math.Min(debtor.Amount, creditor.Amount);
                paymentAmount = Math.Round(paymentAmount, 2, MidpointRounding.AwayFromZero);

                if (paymentAmount > 0)
                {
                    suggestions.Add(new SuggestedSettlementDto
                    {
                        FromUserId = debtor.UserId,
                        FromUserName = debtor.FullName,
                        FromUserEmail = debtor.Email,
                        ToUserId = creditor.UserId,
                        ToUserName = creditor.FullName,
                        ToUserEmail = creditor.Email,
                        Amount = paymentAmount
                    });

                    debtor.Amount -= paymentAmount;
                    creditor.Amount -= paymentAmount;
                }

                if (debtor.Amount < 0.005m)
                    debtorIndex++;

                if (creditor.Amount < 0.005m)
                    creditorIndex++;
            }

            return suggestions;
        }
    }
}
