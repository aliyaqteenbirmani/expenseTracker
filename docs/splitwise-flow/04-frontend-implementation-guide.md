# Frontend Implementation Guide

## 1. Component Modularization & Architecture

The current frontend file `web/src/features/split-expenses/components/SplitGroupDetailsPage.tsx` is over **1,930 lines** long. To implement Splitwise's high-performance UX cleanly, we break it down into modular, single-responsibility components adhering strictly to our `.agents/rules/` guidelines.

### 1.1 Directory Structure
```
web/src/features/split-expenses/
├── SplitExpensesPage.tsx              # Main dashboard / group list
├── types.ts                           # Shared TypeScript contracts
├── splitExpensesApi.ts                # Axios API client
├── splitLabels.ts                     # Localization / labels
├── hooks/
│   ├── useSplitCalculator.ts          # Core split state & calculation hook
│   └── useGroupDebts.ts               # Debt simplification & balance query hook
└── components/
    ├── AddExpenseDialog.tsx           # Refactored dialog container
    ├── EditExpenseDialog.tsx          # Refactored edit container
    ├── SplitSentenceBar.tsx           # Interactive "Paid by X and split Y" pill bar
    ├── SplitMethodModal.tsx           # 4-tab split calculation dialog (Equal, Exact, %, Shares)
    ├── PayerSelectorModal.tsx         # Single vs Multiple payer picker
    ├── ExpenseFeed.tsx                # Chronological feed grouped by month
    ├── ExpenseFeedItem.tsx            # Splitwise 3-column micro-ledger card
    ├── GroupHeroBalanceBanner.tsx     # Prominent "You are owed / You owe" hero banner
    ├── GroupActionToolbar.tsx         # Settle up, Balances, Totals segmented bar
    ├── BalancesDrawer.tsx             # Pairwise member balances view
    ├── SimplifiedDebtsDrawer.tsx      # Min-cash-flow smart settlement view
    ├── SettleUpDialog.tsx             # 1-tap settlement modal
    └── CategoryPicker.tsx             # Category selector with keyword matching
```

---

## 2. Implementing the Interactive Sentence Bar

### `SplitSentenceBar.tsx`
```tsx
import { Button } from '@/components/ui/button'
import { ChevronDown, ArrowRightLeft } from 'lucide-react'
import { cn } from '@/lib/utils'

interface SplitSentenceBarProps {
  payerName: string
  isMultiplePayers: boolean
  splitMethodLabel: string
  onOpenPayerSelect: () => void
  onOpenSplitMethod: () => void
  disabled?: boolean
}

export function SplitSentenceBar({
  payerName,
  isMultiplePayers,
  splitMethodLabel,
  onOpenPayerSelect,
  onOpenSplitMethod,
  disabled = false,
}: SplitSentenceBarProps) {
  return (
    <div className="flex flex-wrap items-center justify-center gap-1.5 py-3 px-4 rounded-xl bg-muted/40 border border-border/60 text-sm">
      <span className="text-muted-foreground font-medium">Paid by</span>
      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={disabled}
        onClick={onOpenPayerSelect}
        className="h-8 px-2.5 font-semibold text-primary border-primary/30 bg-background hover:bg-primary/5 active:scale-95 transition-transform"
      >
        <span>{isMultiplePayers ? 'Multiple people' : payerName}</span>
        <ChevronDown className="ml-1 h-3.5 w-3.5 opacity-60" />
      </Button>

      <span className="text-muted-foreground font-medium">and split</span>
      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={disabled}
        onClick={onOpenSplitMethod}
        className="h-8 px-2.5 font-semibold text-primary border-primary/30 bg-background hover:bg-primary/5 active:scale-95 transition-transform"
      >
        <span>{splitMethodLabel}</span>
        <ChevronDown className="ml-1 h-3.5 w-3.5 opacity-60" />
      </Button>
    </div>
  )
}
```

---

## 3. The `useSplitCalculator` Hook

This hook handles user input, tab switching, and mathematical validation across all 4 split modes:

```typescript
import { useState, useMemo, useCallback } from 'react'
import type { SplitGroupMemberDto } from '../types'
import {
  calculateEqualSplit,
  validateExactSplit,
  calculatePercentageSplit,
  calculatePartsSplit,
} from './calculationEngine'

export type SplitMethod = 'equal' | 'exact' | 'percentage' | 'shares'

export function useSplitCalculator(members: SplitGroupMemberDto[], totalAmount: number) {
  const [method, setMethod] = useState<SplitMethod>('equal')
  
  // Equal Split state
  const [equalSelectedIds, setEqualSelectedIds] = useState<string[]>(
    members.map((m) => m.userId)
  )

  // Exact Split state: userId -> exact amount string
  const [exactValues, setExactValues] = useState<Record<string, string>>({})

  // Percentage Split state: userId -> percentage string
  const [percentageValues, setPercentageValues] = useState<Record<string, string>>({})

  // Shares Split state: userId -> share parts count (number)
  const [shareParts, setShareParts] = useState<Record<string, number>>(
    Object.fromEntries(members.map((m) => [m.userId, 1]))
  )

  // Live computed final shares (userId -> currency amount)
  const calculatedShares = useMemo<Record<string, number>>(() => {
    if (totalAmount <= 0) return {}

    switch (method) {
      case 'equal':
        return calculateEqualSplit(totalAmount, equalSelectedIds)

      case 'exact': {
        const entries = members.map((m) => ({
          userId: m.userId,
          amount: parseFloat(exactValues[m.userId] || '0') || 0,
        }))
        const res: Record<string, number> = {}
        for (const e of entries) res[e.userId] = e.amount
        return res
      }

      case 'percentage': {
        const entries = members.map((m) => ({
          userId: m.userId,
          percentage: parseFloat(percentageValues[m.userId] || '0') || 0,
        }))
        return calculatePercentageSplit(totalAmount, entries)
      }

      case 'shares': {
        const entries = members.map((m) => ({
          userId: m.userId,
          parts: shareParts[m.userId] ?? 1,
        }))
        return calculatePartsSplit(totalAmount, entries)
      }
    }
  }, [method, totalAmount, equalSelectedIds, exactValues, percentageValues, shareParts, members])

  // Validation Status & Deltas
  const validation = useMemo(() => {
    if (totalAmount <= 0) return { isValid: false, message: 'Enter a valid amount' }

    if (method === 'equal') {
      if (equalSelectedIds.length === 0) {
        return { isValid: false, message: 'Select at least one participant' }
      }
      return { isValid: true, message: `Split equally among ${equalSelectedIds.length} members` }
    }

    if (method === 'exact') {
      const entries = members.map((m) => ({
        userId: m.userId,
        amount: parseFloat(exactValues[m.userId] || '0') || 0,
      }))
      const check = validateExactSplit(totalAmount, entries)
      if (!check.isValid) {
        const delta = check.remainingAmount
        const msg = delta > 0
          ? `Rs. ${delta.toFixed(2)} left to allocate`
          : `Rs. ${Math.abs(delta).toFixed(2)} over total amount`
        return { isValid: false, message: msg }
      }
      return { isValid: true, message: 'All funds allocated exactly' }
    }

    if (method === 'percentage') {
      const totalPct = members.reduce(
        (sum, m) => sum + (parseFloat(percentageValues[m.userId] || '0') || 0),
        0
      )
      const diff = Math.round((100 - totalPct) * 100) / 100
      if (diff !== 0) {
        return {
          isValid: false,
          message: diff > 0 ? `${diff}% remaining` : `${Math.abs(diff)}% over 100%`,
        }
      }
      return { isValid: true, message: '100% of expense split' }
    }

    if (method === 'shares') {
      const totalShares = Object.values(shareParts).reduce((a, b) => a + b, 0)
      if (totalShares <= 0) {
        return { isValid: false, message: 'At least one member must have shares' }
      }
      return { isValid: true, message: `Divided into ${totalShares} total parts` }
    }

    return { isValid: true, message: '' }
  }, [method, totalAmount, equalSelectedIds, exactValues, percentageValues, shareParts, members])

  return {
    method,
    setMethod,
    equalSelectedIds,
    setEqualSelectedIds,
    exactValues,
    setExactValues,
    percentageValues,
    setPercentageValues,
    shareParts,
    setShareParts,
    calculatedShares,
    validation,
  }
}
```

---

## 4. Splitwise 3-Column Expense Feed Item

### `ExpenseFeedItem.tsx`
```tsx
import { format } from 'date-fns'
import { Utensils, Car, ShoppingCart, Home, Lightbulb, Film, HelpCircle } from 'lucide-react'
import { cn } from '@/lib/utils'
import { formatMoney } from '@/lib/format'
import type { SplitExpenseListItemDto } from '../types'

const CATEGORY_ICONS: Record<string, React.ElementType> = {
  'Food & Dining': Utensils,
  'Transport': Car,
  'Groceries': ShoppingCart,
  'Accommodation': Home,
  'Utilities': Lightbulb,
  'Entertainment': Film,
  'Other': HelpCircle,
}

interface ExpenseFeedItemProps {
  expense: SplitExpenseListItemDto
  currentUserId: string
  onClick: () => void
}

export function ExpenseFeedItem({ expense, currentUserId, onClick }: ExpenseFeedItemProps) {
  const isPayer = expense.payerId === currentUserId
  const userShare = expense.currentUserShare || 0
  const dateObj = new Date(expense.expenseDate || expense.createdAt)

  // Compute Personal Ledger Position
  let ledgerState: { label: string; amountText: string; colorClass: string }
  if (isPayer) {
    const lentAmount = expense.amount - userShare
    if (lentAmount > 0) {
      ledgerState = {
        label: 'you lent',
        amountText: `+${formatMoney(lentAmount)}`,
        colorClass: 'text-inflow',
      }
    } else {
      ledgerState = {
        label: 'you paid for self',
        amountText: formatMoney(userShare),
        colorClass: 'text-muted-foreground',
      }
    }
  } else if (userShare > 0) {
    ledgerState = {
      label: 'you borrowed',
      amountText: `-${formatMoney(userShare)}`,
      colorClass: 'text-outflow',
    }
  } else {
    ledgerState = {
      label: 'not involved',
      amountText: 'Rs. 0.00',
      colorClass: 'text-muted-foreground/60',
    }
  }

  const IconComponent = CATEGORY_ICONS[expense.category || 'Other'] || HelpCircle

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={(e) => e.key === 'Enter' && onClick()}
      className="flex items-center justify-between p-3.5 hover:bg-muted/40 rounded-xl transition-all cursor-pointer border border-transparent hover:border-border/60 group"
    >
      {/* Col 1: Date & Category Icon */}
      <div className="flex items-center gap-3">
        <div className="flex flex-col items-center justify-center w-10 text-center shrink-0">
          <span className="text-[10px] font-semibold uppercase text-muted-foreground tracking-wider">
            {format(dateObj, 'MMM')}
          </span>
          <span className="text-base font-bold leading-none text-foreground">
            {format(dateObj, 'dd')}
          </span>
        </div>

        <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary group-hover:scale-105 transition-transform shrink-0">
          <IconComponent className="w-5 h-5" />
        </div>

        {/* Col 2: Title & Payer */}
        <div className="min-w-0">
          <h4 className="font-semibold text-sm leading-tight text-foreground truncate max-w-[200px] sm:max-w-xs md:max-w-md">
            {expense.title}
          </h4>
          <p className="text-xs text-muted-foreground mt-0.5 truncate">
            {isPayer ? 'You' : expense.payerName} paid{' '}
            <span className="font-medium text-foreground">{formatMoney(expense.amount)}</span>
          </p>
        </div>
      </div>

      {/* Col 3: Personal Financial Impact */}
      <div className="text-right shrink-0 pl-3">
        <span className="text-[10px] uppercase font-bold tracking-wider text-muted-foreground block">
          {ledgerState.label}
        </span>
        <span className={cn('font-mono font-semibold text-sm tabular-nums', ledgerState.colorClass)}>
          {ledgerState.amountText}
        </span>
      </div>
    </div>
  )
}
```

---

## 5. Group Hero Balance Banner

### `GroupHeroBalanceBanner.tsx`
```tsx
import { ArrowUpRight, ArrowDownLeft, CheckCircle2 } from 'lucide-react'
import { formatMoney } from '@/lib/format'
import { cn } from '@/lib/utils'

interface GroupHeroBalanceBannerProps {
  balance: number
  memberCount: number
}

export function GroupHeroBalanceBanner({ balance, memberCount }: GroupHeroBalanceBannerProps) {
  const isPositive = balance > 0.005
  const isNegative = balance < -0.005
  const isSettled = !isPositive && !isNegative

  return (
    <div
      className={cn(
        'p-5 rounded-2xl border transition-all',
        isPositive && 'bg-inflow-muted/20 border-inflow/30 text-inflow-foreground',
        isNegative && 'bg-outflow-muted/20 border-outflow/30 text-outflow-foreground',
        isSettled && 'bg-muted/30 border-border/60 text-muted-foreground'
      )}
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <span className="text-xs font-semibold uppercase tracking-wider opacity-80 block">
            {isPositive && 'Overall, you are owed'}
            {isNegative && 'Overall, you owe'}
            {isSettled && 'Settled status'}
          </span>
          <div className="flex items-baseline gap-2 mt-1">
            <span
              className={cn(
                'text-3xl font-extrabold font-mono tabular-nums tracking-tight',
                isPositive && 'text-inflow',
                isNegative && 'text-outflow',
                isSettled && 'text-foreground'
              )}
            >
              {isPositive && `+${formatMoney(balance)}`}
              {isNegative && `-${formatMoney(Math.abs(balance))}`}
              {isSettled && 'Settled up'}
            </span>
          </div>
          <p className="text-xs text-muted-foreground mt-1">
            Across {memberCount} active members in this group
          </p>
        </div>

        <div className="flex items-center gap-2">
          {isPositive && (
            <div className="p-3 rounded-xl bg-inflow/10 text-inflow shrink-0">
              <ArrowDownLeft className="w-6 h-6" />
            </div>
          )}
          {isNegative && (
            <div className="p-3 rounded-xl bg-outflow/10 text-outflow shrink-0">
              <ArrowUpRight className="w-6 h-6" />
            </div>
          )}
          {isSettled && (
            <div className="p-3 rounded-xl bg-muted text-muted-foreground shrink-0">
              <CheckCircle2 className="w-6 h-6" />
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
```
