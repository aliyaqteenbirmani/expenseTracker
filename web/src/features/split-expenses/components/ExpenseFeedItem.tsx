import { format } from 'date-fns'
import {
  Utensils,
  Car,
  ShoppingCart,
  Home,
  Lightbulb,
  Film,
  ShoppingBag,
  Wrench,
  HelpCircle,
} from 'lucide-react'
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
  'Shopping': ShoppingBag,
  'Services': Wrench,
  'Other': HelpCircle,
}

interface ExpenseFeedItemProps {
  expense: SplitExpenseListItemDto
  currentUserId?: string
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
    if (lentAmount > 0.005) {
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
  } else if (userShare > 0.005) {
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
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          onClick()
        }
      }}
      className="flex items-center justify-between p-3 sm:p-3.5 hover:bg-muted/40 rounded-xl transition-all cursor-pointer border border-transparent hover:border-border/60 group active:scale-[0.99]"
    >
      {/* Col 1 & 2: Date + Category Icon + Title + Payer */}
      <div className="flex items-center gap-3 min-w-0">
        {/* Date block */}
        <div className="flex flex-col items-center justify-center w-9 text-center shrink-0">
          <span className="text-[10px] font-bold uppercase text-muted-foreground tracking-wider leading-none">
            {format(dateObj, 'MMM')}
          </span>
          <span className="text-sm sm:text-base font-extrabold leading-tight text-foreground mt-0.5">
            {format(dateObj, 'dd')}
          </span>
        </div>

        {/* Category Icon */}
        <div className="w-9 h-9 sm:w-10 sm:h-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary group-hover:scale-105 transition-transform shrink-0">
          <IconComponent className="w-4 h-4 sm:w-5 sm:h-5" />
        </div>

        {/* Description & Payer */}
        <div className="min-w-0 pr-2">
          <h4 className="font-semibold text-sm leading-tight text-foreground truncate max-w-[160px] sm:max-w-xs md:max-w-md">
            {expense.title}
          </h4>
          <p className="text-xs text-muted-foreground mt-0.5 truncate">
            {isPayer ? 'You' : expense.payerName} paid{' '}
            <span className="font-medium text-foreground">{formatMoney(expense.amount)}</span>
          </p>
        </div>
      </div>

      {/* Col 3: Personal Financial Impact */}
      <div className="text-right shrink-0 pl-2">
        <span className="text-[10px] uppercase font-bold tracking-wider text-muted-foreground block leading-none mb-1">
          {ledgerState.label}
        </span>
        <span className={cn('font-mono font-semibold text-xs sm:text-sm tabular-nums', ledgerState.colorClass)}>
          {ledgerState.amountText}
        </span>
      </div>
    </div>
  )
}
