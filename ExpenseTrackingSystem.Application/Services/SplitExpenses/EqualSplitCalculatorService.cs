using SpendwiseSystem.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class EqualSplitCalculatorService : ISplitCalculatorService
    {
        public Dictionary<Guid, decimal> CalculateEqualSplit(decimal totalAmount, IReadOnlyList<Guid> participantUserIds)
        {
            if (participantUserIds == null || participantUserIds.Count == 0)
                throw new ArgumentException("At least one participant is required for split calculation.", nameof(participantUserIds));

            if (totalAmount <= 0)
                throw new ArgumentException("Expense total amount must be greater than zero.", nameof(totalAmount));

            var count = participantUserIds.Count;
            // Truncate to 2 decimal places to get base share
            var baseShare = Math.Floor((totalAmount / count) * 100m) / 100m;
            var allocatedTotal = baseShare * count;
            var remainderCents = (int)Math.Round((totalAmount - allocatedTotal) * 100m);

            var shares = new Dictionary<Guid, decimal>(count);

            for (var i = 0; i < count; i++)
            {
                var userId = participantUserIds[i];
                var share = baseShare;

                // Distribute remainder 1 cent (0.01) at a time to the first k participants
                if (remainderCents > 0)
                {
                    share += 0.01m;
                    remainderCents--;
                }

                shares[userId] = share;
            }

            return shares;
        }
    }
}
