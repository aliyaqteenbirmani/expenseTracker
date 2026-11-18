import type { MemberBalanceDto, SuggestedSettlementDto } from '../types'

export interface SimplifiedDebt {
  fromUserId: string
  fromUserName: string
  toUserId: string
  toUserName: string
  amount: number
}

/**
 * Greedily simplifies debts between group members based on net balances.
 * Perfectly mirrors backend DebtSimplificationService.
 */
export function simplifyDebtsFromBalances(balances: MemberBalanceDto[]): SimplifiedDebt[] {
  if (!balances || balances.length === 0) return []

  const debtors = balances
    .filter((b) => b.netBalance < -0.005)
    .map((b) => ({
      userId: b.userId,
      fullName: b.fullName,
      amount: Math.abs(b.netBalance),
    }))
    .sort((a, b) => b.amount - a.amount || a.userId.localeCompare(b.userId))

  const creditors = balances
    .filter((b) => b.netBalance > 0.005)
    .map((b) => ({
      userId: b.userId,
      fullName: b.fullName,
      amount: b.netBalance,
    }))
    .sort((a, b) => b.amount - a.amount || a.userId.localeCompare(b.userId))

  const debts: SimplifiedDebt[] = []
  let debtorIdx = 0
  let creditorIdx = 0

  while (debtorIdx < debtors.length && creditorIdx < creditors.length) {
    const debtor = debtors[debtorIdx]
    const creditor = creditors[creditorIdx]
    const payment = Math.round(Math.min(debtor.amount, creditor.amount) * 100) / 100

    if (payment > 0) {
      debts.push({
        fromUserId: debtor.userId,
        fromUserName: debtor.fullName,
        toUserId: creditor.userId,
        toUserName: creditor.fullName,
        amount: payment,
      })
      debtor.amount -= payment
      creditor.amount -= payment
    }

    if (debtor.amount < 0.005) debtorIdx++
    if (creditor.amount < 0.005) creditorIdx++
  }

  return debts
}

/**
 * Formats a list of recipient names into a human-friendly string.
 * e.g. ["Ali"] -> "Ali"
 * e.g. ["Ali", "Ahmed"] -> "Ali and Ahmed"
 * e.g. ["Ali", "Ahmed", "Bilal"] -> "Ali, Ahmed and Bilal"
 */
export function formatRecipientNames(names: string[]): string {
  const cleanNames = Array.from(new Set(names.filter(Boolean)))
  if (cleanNames.length === 0) return ''
  if (cleanNames.length === 1) return cleanNames[0]
  if (cleanNames.length === 2) return `${cleanNames[0]} and ${cleanNames[1]}`
  return `${cleanNames.slice(0, -1).join(', ')} and ${cleanNames[cleanNames.length - 1]}`
}

/**
 * Returns who a specific user owes, given a list of simplified debts or suggested transactions.
 */
export function getCreditorNamesForUser(
  userId: string,
  debts: (SimplifiedDebt | SuggestedSettlementDto)[]
): string[] {
  if (!debts || debts.length === 0) return []
  const userDebts = debts.filter((d) => d.fromUserId === userId)
  return Array.from(new Set(userDebts.map((d) => d.toUserName).filter(Boolean)))
}

/**
 * Returns who owes a specific user, given a list of simplified debts or suggested transactions.
 */
export function getDebtorNamesForUser(
  userId: string,
  debts: (SimplifiedDebt | SuggestedSettlementDto)[]
): string[] {
  if (!debts || debts.length === 0) return []
  const userCredits = debts.filter((d) => d.toUserId === userId)
  return Array.from(new Set(userCredits.map((d) => d.fromUserName).filter(Boolean)))
}
