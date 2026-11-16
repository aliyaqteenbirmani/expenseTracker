using System;
using System.Collections.Generic;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitCalculatorService
    {
        /// <summary>
        /// Divides totalAmount equally across the provided participant user IDs.
        /// Guaranteed: The sum of returned share amounts will exactly equal totalAmount (cents/rounding safe).
        /// </summary>
        Dictionary<Guid, decimal> CalculateEqualSplit(decimal totalAmount, IReadOnlyList<Guid> participantUserIds);

        /// <summary>
        /// Validates and computes exact amount splits. Sum of exact amounts must equal totalAmount.
        /// </summary>
        Dictionary<Guid, decimal> CalculateExactSplit(decimal totalAmount, IReadOnlyList<ParticipantSplitRequestDto> splits);

        /// <summary>
        /// Computes percentage-based splits with fractional cent remainder resolution.
        /// </summary>
        Dictionary<Guid, decimal> CalculatePercentageSplit(decimal totalAmount, IReadOnlyList<ParticipantSplitRequestDto> splits);

        /// <summary>
        /// Computes proportional parts/shares splits with fractional cent remainder resolution.
        /// </summary>
        Dictionary<Guid, decimal> CalculateSharesSplit(decimal totalAmount, IReadOnlyList<ParticipantSplitRequestDto> splits);
    }
}
