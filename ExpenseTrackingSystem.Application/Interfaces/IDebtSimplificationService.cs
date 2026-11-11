using System.Collections.Generic;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface IDebtSimplificationService
    {
        /// <summary>
        /// Simplifies debts among group members to minimize the total number of transactions needed
        /// to settle all group balances to zero.
        /// Guaranteed: Sum of suggested settlements exactly balances out all positive and negative balances.
        /// </summary>
        List<SuggestedSettlementDto> SimplifyDebts(IEnumerable<MemberBalanceDto> memberBalances);
    }
}
