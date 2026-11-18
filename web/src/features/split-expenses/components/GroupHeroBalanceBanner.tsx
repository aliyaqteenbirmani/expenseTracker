import { ArrowUpRight, ArrowDownLeft, CheckCircle2 } from 'lucide-react'
import { formatMoney } from '@/lib/format'
import { cn } from '@/lib/utils'

interface GroupHeroBalanceBannerProps {
  balance: number
  memberCount: number
  owedToText?: string
}

export function GroupHeroBalanceBanner({ balance, memberCount, owedToText }: GroupHeroBalanceBannerProps) {
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
          <span className="text-[11px] font-bold uppercase tracking-wider opacity-80 block">
            {isPositive && 'Overall, you are owed'}
            {isNegative && (owedToText ? `Overall, you owe to ${owedToText}` : 'Overall, you owe')}
            {isSettled && 'Settled status'}
          </span>
          <div className="flex items-baseline gap-2 mt-1">
            <span
              className={cn(
                'text-2xl sm:text-3xl font-extrabold font-mono tabular-nums tracking-tight',
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
            {isNegative && owedToText
              ? `You owe ${formatMoney(Math.abs(balance))} to ${owedToText}`
              : `Across ${memberCount} active members in this group`}
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
