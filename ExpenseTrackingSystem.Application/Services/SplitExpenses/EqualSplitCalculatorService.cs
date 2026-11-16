using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
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

        /// <summary>
        /// Validates and computes exact amount splits. Sum of exact amounts must equal totalAmount.
        /// </summary>
        public Dictionary<Guid, decimal> CalculateExactSplit(decimal totalAmount, IReadOnlyList<ParticipantSplitRequestDto> splits)
        {
            if (splits == null || splits.Count == 0)
                throw new ArgumentException("At least one participant split is required.", nameof(splits));

            totalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
            if (totalAmount <= 0)
                throw new ArgumentException("Expense total amount must be greater than zero.", nameof(totalAmount));

            var totalCents = (long)Math.Round(totalAmount * 100m);
            var shares = new Dictionary<Guid, decimal>();
            long allocatedCents = 0;

            foreach (var item in splits)
            {
                if (item.UserId == Guid.Empty)
                    throw new ArgumentException("Invalid participant User ID.", nameof(splits));

                var exact = item.ExactAmount ?? 0m;
                if (exact < 0)
                    throw new ArgumentException("Exact split amounts cannot be negative.", nameof(splits));

                var cents = (long)Math.Round(exact * 100m);
                allocatedCents += cents;
                shares[item.UserId] = cents / 100m;
            }

            if (allocatedCents != totalCents)
            {
                var diff = (totalCents - allocatedCents) / 100m;
                throw new ArgumentException(
                    $"Exact split total ({allocatedCents / 100m:F2}) does not equal expense amount ({totalAmount:F2}). Difference: {diff:F2}",
                    nameof(splits));
            }

            return shares;
        }

        /// <summary>
        /// Computes percentage-based splits with fractional cent remainder resolution.
        /// Guaranteed: Sum(shares.Values) == totalAmount, always.
        /// </summary>
        public Dictionary<Guid, decimal> CalculatePercentageSplit(decimal totalAmount, IReadOnlyList<ParticipantSplitRequestDto> splits)
        {
            if (splits == null || splits.Count == 0)
                throw new ArgumentException("At least one participant split is required.", nameof(splits));

            totalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
            if (totalAmount <= 0)
                throw new ArgumentException("Expense total amount must be greater than zero.", nameof(totalAmount));

            var totalPct = splits.Sum(s => s.Percentage ?? 0m);
            if (Math.Abs(totalPct - 100m) > 0.05m)
            {
                throw new ArgumentException($"Split percentages must sum to 100%. Current sum: {totalPct:F2}%", nameof(splits));
            }

            var totalCents = (long)Math.Round(totalAmount * 100m);
            var rawList = splits.Select(s =>
            {
                var pct = Math.Max(0m, s.Percentage ?? 0m);
                var rawCents = (totalCents * pct) / 100m;
                var baseCents = (long)Math.Floor(rawCents);
                var frac = rawCents - baseCents;
                return new { s.UserId, BaseCents = baseCents, Frac = frac };
            }).ToList();

            var allocatedCents = rawList.Sum(r => r.BaseCents);
            var diffCents = (int)(totalCents - allocatedCents);

            // Sort by largest fractional remainder, then deterministically by UserId
            var ordered = rawList
                .OrderByDescending(r => r.Frac)
                .ThenBy(r => r.UserId)
                .ToList();

            var shares = new Dictionary<Guid, decimal>();
            foreach (var item in ordered)
            {
                var cents = item.BaseCents;
                if (diffCents > 0)
                {
                    cents += 1;
                    diffCents--;
                }
                shares[item.UserId] = cents / 100m;
            }

            return shares;
        }

        /// <summary>
        /// Computes proportional parts/shares splits with fractional cent remainder resolution.
        /// Guaranteed: Sum(shares.Values) == totalAmount, always.
        /// </summary>
        public Dictionary<Guid, decimal> CalculateSharesSplit(decimal totalAmount, IReadOnlyList<ParticipantSplitRequestDto> splits)
        {
            if (splits == null || splits.Count == 0)
                throw new ArgumentException("At least one participant split is required.", nameof(splits));

            totalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
            if (totalAmount <= 0)
                throw new ArgumentException("Expense total amount must be greater than zero.", nameof(totalAmount));

            var totalParts = splits.Sum(s => Math.Max(0, s.ShareParts ?? 1));
            if (totalParts <= 0)
                throw new ArgumentException("Total parts must be greater than zero.", nameof(splits));

            var totalCents = (long)Math.Round(totalAmount * 100m);
            var rawList = splits.Select(s =>
            {
                var parts = Math.Max(0, s.ShareParts ?? 1);
                var rawCents = ((decimal)totalCents * parts) / totalParts;
                var baseCents = (long)Math.Floor(rawCents);
                var frac = rawCents - baseCents;
                return new { s.UserId, BaseCents = baseCents, Frac = frac };
            }).ToList();

            var allocatedCents = rawList.Sum(r => r.BaseCents);
            var diffCents = (int)(totalCents - allocatedCents);

            // Sort by largest fractional remainder, then deterministically by UserId
            var ordered = rawList
                .OrderByDescending(r => r.Frac)
                .ThenBy(r => r.UserId)
                .ToList();

            var shares = new Dictionary<Guid, decimal>();
            foreach (var item in ordered)
            {
                var cents = item.BaseCents;
                if (diffCents > 0)
                {
                    cents += 1;
                    diffCents--;
                }
                shares[item.UserId] = cents / 100m;
            }

            return shares;
        }
    }
}
