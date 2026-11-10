using System;
using System.Collections.Generic;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitCalculatorService
    {
        /// <summary>
        /// Divides totalAmount equally across the provided participant user IDs.
        /// Guaranteed: The sum of returned share amounts will exactly equal totalAmount (cents/rounding safe).
        /// </summary>
        Dictionary<Guid, decimal> CalculateEqualSplit(decimal totalAmount, IReadOnlyList<Guid> participantUserIds);
    }
}
