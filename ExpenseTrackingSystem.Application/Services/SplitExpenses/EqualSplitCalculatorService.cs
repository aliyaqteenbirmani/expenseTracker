using SpendwiseSystem.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class EqualSplitCalculatorService : ISplitCalculatorService
    {
        /// <summary>
        /// Divides totalAmount equally across participant user IDs.
        /// Deterministically distributes remainder cents in a stable sorted order (by UserId).
        /// Absolute Guarantee: Sum(shares.Values) == totalAmount, always.
        /// </summary>
        public Dictionary<Guid, decimal> CalculateEqualSplit(decimal totalAmount, IReadOnlyList<Guid> participantUserIds)
        {
            if (participantUserIds == null || participantUserIds.Count == 0)
                throw new ArgumentException("At least one participant is required for split calculation.", nameof(participantUserIds));

            // Standardize currency precision to 2 decimal places
            totalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);

            if (totalAmount <= 0)
                throw new ArgumentException("Expense total amount must be greater than zero.", nameof(totalAmount));

            // Stable deterministic ordering by user ID so remainder distribution is always identical and idempotent
            var orderedParticipants = participantUserIds.Distinct().OrderBy(id => id).ToList();
            var count = orderedParticipants.Count;

            // Base share truncated to 2 decimal places
            var baseShare = Math.Floor((totalAmount / count) * 100m) / 100m;
            var allocatedTotal = baseShare * count;
            var remainderCents = (int)Math.Round((totalAmount - allocatedTotal) * 100m);

            var shares = new Dictionary<Guid, decimal>(count);

            for (var i = 0; i < count; i++)
            {
                var userId = orderedParticipants[i];
                var share = baseShare;

                // Deterministically assign extra minor unit (0.01) to the first k participants
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
