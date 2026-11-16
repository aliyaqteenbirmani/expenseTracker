# Split Calculation Engine Architecture

## 1. Overview & Requirements

A robust Splitwise clone requires a rock-solid calculation engine capable of handling varied real-world splitting scenarios without fractional penny discrepancies:
1. **Zero Rounding Drift**: The sum of participant shares must **exactly equal** the expense amount:
   $$\sum_{i=1}^{n} \text{share}_i = \text{totalAmount}$$
2. **Deterministic Penny Allocation**: Cents/paisa remainders must be distributed consistently across client and server.
3. **Multiple Splitting Modes**: Equal, Exact, Percentage, Shares, and Adjustments.
4. **Multiple Payers**: Support for bills where more than one person contributed to the initial payment.
5. **Debt Simplification**: Reducing circular debts across group members to minimize total transactions.

---

## 2. Core Calculation Modes

### 2.1 Mode 1: Equal Split (`Equal`)

When dividing an amount $A$ across $k$ participants:
1. Convert total to cents: $C = \text{round}(A \times 100)$
2. Compute base share in cents: $B = \lfloor C / k \rfloor$
3. Compute remainder: $R = C \pmod k$
4. Sort participant IDs deterministically (e.g., ascending UUID or string sort).
5. For the first $R$ participants in sorted order, allocate $B + 1$ cents; for the remaining, allocate $B$ cents.

```typescript
export function calculateEqualSplit(
  totalAmount: number,
  participantIds: string[]
): Record<string, number> {
  if (totalAmount <= 0 || participantIds.length === 0) return {}
  const totalCents = Math.round(totalAmount * 100)
  const count = participantIds.length
  const baseCents = Math.floor(totalCents / count)
  let remainder = totalCents % count

  const sorted = [...participantIds].sort((a, b) => a.localeCompare(b))
  const shares: Record<string, number> = {}

  for (const id of sorted) {
    const cents = baseCents + (remainder > 0 ? 1 : 0)
    if (remainder > 0) remainder--
    shares[id] = cents / 100
  }
  return shares
}
```

---

### 2.2 Mode 2: Exact Amounts (`Exact`)

Each participant is assigned a specific currency amount:
- **Constraint**: $\sum \text{exactAmount}_i = \text{totalAmount}$
- **Validation**:
  $$\Delta = \text{round}(A \times 100) - \sum \text{round}(\text{exactAmount}_i \times 100)$$
  The submit button is enabled only when $\Delta = 0$.

```typescript
export interface ExactSplitInput {
  userId: string
  amount: number
}

export function validateExactSplit(
  totalAmount: number,
  entries: ExactSplitInput[]
): { isValid: boolean; remainingAmount: number; totalAllocated: number } {
  const totalCents = Math.round(totalAmount * 100)
  const allocatedCents = entries.reduce(
    (sum, e) => sum + Math.round((e.amount || 0) * 100),
    0
  )
  const remainingCents = totalCents - allocatedCents
  return {
    isValid: remainingCents === 0 && entries.every((e) => (e.amount || 0) >= 0),
    remainingAmount: remainingCents / 100,
    totalAllocated: allocatedCents / 100,
  }
}
```

---

### 2.3 Mode 3: Percentage Split (`Percentage`)

Each participant has a percentage weight $P_i \ge 0$:
- **Constraint**: $\sum_{i=1}^{k} P_i = 100.00\%$
- **Rounding Handling**:
  1. Base cents for each participant: $C_i = \lfloor (A \times P_i) \rfloor$ (where $A$ is in cents and $P_i$ is a fraction).
  2. The sum of $C_i$ might be slightly less than total cents due to truncation.
  3. The difference $D = \text{totalCents} - \sum C_i$ is distributed by highest fractional remainder order.

```typescript
export interface PercentageSplitInput {
  userId: string
  percentage: number // e.g. 33.33
}

export function calculatePercentageSplit(
  totalAmount: number,
  entries: PercentageSplitInput[]
): Record<string, number> {
  const totalCents = Math.round(totalAmount * 100)
  const rawShares = entries.map((e) => {
    const rawCents = (totalCents * e.percentage) / 100
    const baseCents = Math.floor(rawCents)
    const frac = rawCents - baseCents
    return { userId: e.userId, baseCents, frac }
  })

  let allocatedCents = rawShares.reduce((s, r) => s + r.baseCents, 0)
  let diff = totalCents - allocatedCents

  // Sort by highest fractional remainder to assign extra cent
  rawShares.sort((a, b) => b.frac - a.frac)

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
```

---

### 2.4 Mode 4: By Shares / Parts (`Shares`)

Used when members have unequal weights (e.g. Ali has 2 shares, Usman has 1 share):
- Let $W_i$ be the weight/parts for participant $i$.
- $\text{TotalParts} = \sum_{i=1}^{k} W_i$
- Each member's ideal cents: $\text{idealCents}_i = \frac{\text{totalCents} \times W_i}{\text{TotalParts}}$
- Allocate $\lfloor \text{idealCents}_i \rfloor$ and distribute the remainder $R$ to the participants with largest fractional parts.

```typescript
export interface SharePartsInput {
  userId: string
  parts: number // integer >= 1
}

export function calculatePartsSplit(
  totalAmount: number,
  entries: SharePartsInput[]
): Record<string, number> {
  const totalCents = Math.round(totalAmount * 100)
  const totalParts = entries.reduce((s, e) => s + e.parts, 0)
  if (totalParts === 0) return {}

  const rawShares = entries.map((e) => {
    const rawCents = (totalCents * e.parts) / totalParts
    const baseCents = Math.floor(rawCents)
    const frac = rawCents - baseCents
    return { userId: e.userId, baseCents, frac }
  })

  let allocatedCents = rawShares.reduce((s, r) => s + r.baseCents, 0)
  let diff = totalCents - allocatedCents

  rawShares.sort((a, b) => b.frac - a.frac)

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
```

---

## 3. Multiple Payers Architecture

In Splitwise, a bill can be paid by multiple people (e.g., Ali paid Rs. 3,000 and Bilal paid Rs. 2,000 for a Rs. 5,000 restaurant dinner).

### 3.1 Data Model for Payers
```typescript
export interface ExpensePayerItemDto {
  userId: string
  amountPaid: number
}
```

### 3.2 Net Balance Formulation with Multiple Payers
For any member $u$, their net impact $\text{Net}_u$ from an expense is:
$$\text{Net}_u = \text{AmountPaid}_u - \text{ShareOwed}_u$$
- If $\text{Net}_u > 0$: The member is owed money (lent).
- If $\text{Net}_u < 0$: The member owes money (borrowed).
- If $\text{Net}_u = 0$: The member is completely balanced for this expense.

---

## 4. Debt Simplification Algorithm (Min-Cash-Flow)

### 4.1 The Problem
In a group of $N$ people, if pairwise debts are settled independently, there can be up to $N \times (N - 1) / 2$ individual payments, with many circular loops (e.g., A owes B $10, B owes C $10, C owes A $10).

### 4.2 The Greedy Min-Cash-Flow Algorithm
This algorithm reduces total transactions down to at most $N - 1$:
1. Calculate each member's net balance across all expenses and settlements:
   $$\text{Balance}(u) = \sum \text{Paid}(u) - \sum \text{Owed}(u)$$
2. Separate members into:
   - **Creditors** ($\text{Balance} > 0$)
   - **Debtors** ($\text{Balance} < 0$)
3. In each step:
   - Find the maximum debtor (owes most: $D$) and maximum creditor (owed most: $C$).
   - Determine transaction amount $M = \min(|D.\text{balance}|, C.\text{balance}|)$.
   - Generate transaction: **$D$ pays $C$ the amount $M$**.
   - Update balances: $D.\text{balance} += M$, $C.\text{balance} -= M$.
   - Remove any member whose balance becomes 0.
   - Repeat until all balances reach 0.

```typescript
export interface SimplifiedTransaction {
  fromUserId: string
  toUserId: string
  amount: number
}

export function simplifyDebts(memberBalances: { userId: string; netBalance: number }[]): SimplifiedTransaction[] {
  const creditors: { userId: string; amount: number }[] = []
  const debtors: { userId: string; amount: number }[] = []

  for (const m of memberBalances) {
    const balCents = Math.round(m.netBalance * 100)
    if (balCents > 0) creditors.push({ userId: m.userId, amount: balCents })
    else if (balCents < 0) debtors.push({ userId: m.userId, amount: -balCents })
  }

  const transactions: SimplifiedTransaction[] = []

  let i = 0 // debtors index
  let j = 0 // creditors index

  debtors.sort((a, b) => b.amount - a.amount)
  creditors.sort((a, b) => b.amount - a.amount)

  while (i < debtors.length && j < creditors.length) {
    const debtor = debtors[i]
    const creditor = creditors[j]
    const minAmount = Math.min(debtor.amount, creditor.amount)

    transactions.push({
      fromUserId: debtor.userId,
      toUserId: creditor.userId,
      amount: minAmount / 100,
    })

    debtor.amount -= minAmount
    creditor.amount -= minAmount

    if (debtor.amount === 0) i++
    if (creditor.amount === 0) j++
  }

  return transactions
}
```
