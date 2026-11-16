/**
 * Split Calculation Engine for Spendwise Split Expenses
 *
 * Guarantees:
 * 1. Zero Rounding Drift: Sum of participant shares exactly equals total amount in cents.
 * 2. Deterministic Penny Distribution: Remainders are assigned based on deterministic sorting.
 * 3. Min-Cash-Flow Debt Simplification: Graph reduction minimizing total settlement transactions.
 */

export interface ExactSplitEntry {
  userId: string
  amount: number
}

export interface PercentageSplitEntry {
  userId: string
  percentage: number // 0 to 100
}

export interface SharePartsEntry {
  userId: string
  parts: number // integer >= 1
}

export interface SimplifiedTransaction {
  fromUserId: string
  toUserId: string
  amount: number
}

/**
 * Deterministically splits totalAmount equally among selected participant IDs.
 * Remainders in cents are distributed one by one to participants ordered alphabetically by userId.
 */
export function calculateEqualSplit(
  totalAmount: number,
  participantIds: string[]
): Record<string, number> {
  if (totalAmount <= 0 || !participantIds || participantIds.length === 0) {
    return {}
  }

  const totalCents = Math.round(totalAmount * 100)
  const count = participantIds.length
  const baseCents = Math.floor(totalCents / count)
  let remainder = totalCents % count

  const sorted = [...participantIds].sort((a, b) => a.localeCompare(b))
  const result: Record<string, number> = {}

  for (const id of sorted) {
    const cents = baseCents + (remainder > 0 ? 1 : 0)
    if (remainder > 0) remainder--
    result[id] = cents / 100
  }

  return result
}

/**
 * Validates exact amount allocations against totalAmount.
 * Returns whether allocations exactly match, along with remaining/over amounts.
 */
export function validateExactSplit(
  totalAmount: number,
  entries: ExactSplitEntry[]
): {
  isValid: boolean
  remainingAmount: number
  totalAllocated: number
  error?: string
} {
  const totalCents = Math.round(totalAmount * 100)
  let allocatedCents = 0

  for (const e of entries) {
    const entryCents = Math.round((e.amount || 0) * 100)
    if (entryCents < 0) {
      return {
        isValid: false,
        remainingAmount: (totalCents - allocatedCents) / 100,
        totalAllocated: allocatedCents / 100,
        error: 'Amounts cannot be negative',
      }
    }
    allocatedCents += entryCents
  }

  const remainingCents = totalCents - allocatedCents

  return {
    isValid: remainingCents === 0 && totalCents > 0,
    remainingAmount: remainingCents / 100,
    totalAllocated: allocatedCents / 100,
    error:
      remainingCents !== 0
        ? remainingCents > 0
          ? `Rs. ${(remainingCents / 100).toFixed(2)} left to allocate`
          : `Rs. ${(Math.abs(remainingCents) / 100).toFixed(2)} over total amount`
        : undefined,
  }
}

/**
 * Computes exact currency shares from percentages (must sum to 100%).
 * Any cent discrepancy due to fractional percentages is distributed by highest fractional remainder.
 */
export function calculatePercentageSplit(
  totalAmount: number,
  entries: PercentageSplitEntry[]
): Record<string, number> {
  if (totalAmount <= 0 || !entries || entries.length === 0) {
    return {}
  }

  const totalCents = Math.round(totalAmount * 100)
  const rawShares = entries.map((e) => {
    const validPct = Math.max(0, e.percentage || 0)
    const rawCents = (totalCents * validPct) / 100
    const baseCents = Math.floor(rawCents)
    const frac = rawCents - baseCents
    return { userId: e.userId, baseCents, frac }
  })

  const allocatedCents = rawShares.reduce((s, r) => s + r.baseCents, 0)
  let diff = totalCents - allocatedCents

  // Sort descending by largest fraction, then deterministically by userId
  rawShares.sort((a, b) => b.frac - a.frac || a.userId.localeCompare(b.userId))

  const result: Record<string, number> = {}
  for (const item of rawShares) {
    let cents = item.baseCents
    if (diff > 0) {
      cents += 1
      diff -= 1
    }
    result[item.userId] = cents / 100
  }

  return result
}

/**
 * Computes proportional shares from parts/weights (e.g. 2 parts for a couple, 1 part for single).
 * Fractional cents remainder is distributed by highest remainder.
 */
export function calculatePartsSplit(
  totalAmount: number,
  entries: SharePartsEntry[]
): Record<string, number> {
  if (totalAmount <= 0 || !entries || entries.length === 0) {
    return {}
  }

  const totalParts = entries.reduce((s, e) => s + Math.max(0, Math.floor(e.parts || 0)), 0)
  if (totalParts === 0) return {}

  const totalCents = Math.round(totalAmount * 100)
  const rawShares = entries.map((e) => {
    const parts = Math.max(0, Math.floor(e.parts || 0))
    const rawCents = (totalCents * parts) / totalParts
    const baseCents = Math.floor(rawCents)
    const frac = rawCents - baseCents
    return { userId: e.userId, baseCents, frac }
  })

  const allocatedCents = rawShares.reduce((s, r) => s + r.baseCents, 0)
  let diff = totalCents - allocatedCents

  rawShares.sort((a, b) => b.frac - a.frac || a.userId.localeCompare(b.userId))

  const result: Record<string, number> = {}
  for (const item of rawShares) {
    let cents = item.baseCents
    if (diff > 0) {
      cents += 1
      diff -= 1
    }
    result[item.userId] = cents / 100
  }

  return result
}

/**
 * Greedy Min-Cash-Flow Debt Simplification Algorithm.
 * Given net balances of group members, finds minimum number of payments (at most N - 1)
 * to settle all debts and eliminate circular loops.
 */
export function simplifyDebts(
  memberBalances: { userId: string; netBalance: number }[]
): SimplifiedTransaction[] {
  const creditors: { userId: string; amount: number }[] = []
  const debtors: { userId: string; amount: number }[] = []

  for (const m of memberBalances) {
    const balCents = Math.round(m.netBalance * 100)
    if (balCents > 0) {
      creditors.push({ userId: m.userId, amount: balCents })
    } else if (balCents < 0) {
      debtors.push({ userId: m.userId, amount: -balCents })
    }
  }

  // Sort descending by amount
  debtors.sort((a, b) => b.amount - a.amount || a.userId.localeCompare(b.userId))
  creditors.sort((a, b) => b.amount - a.amount || a.userId.localeCompare(b.userId))

  const transactions: SimplifiedTransaction[] = []
  let i = 0 // debtor index
  let j = 0 // creditor index

  while (i < debtors.length && j < creditors.length) {
    const debtor = debtors[i]
    const creditor = creditors[j]
    const minAmount = Math.min(debtor.amount, creditor.amount)

    if (minAmount > 0) {
      transactions.push({
        fromUserId: debtor.userId,
        toUserId: creditor.userId,
        amount: minAmount / 100,
      })
    }

    debtor.amount -= minAmount
    creditor.amount -= minAmount

    if (debtor.amount === 0) i++
    if (creditor.amount === 0) j++
  }

  return transactions
}
