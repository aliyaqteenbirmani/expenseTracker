import { useEffect, useMemo, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import {
  SlidersHorizontal,
  Flame,
  AlertTriangle,
  CheckCircle2,
  CalendarDays,
  Target,
} from 'lucide-react'
import type { Transaction } from '@/App'
import { BudgetConfigDialog } from './BudgetConfigDialog'
import { formatMoney } from '@/lib/format'
import { Gate } from '@/components/app/Gate'
import { AnimatedNumber } from '@/components/motion/animated-number'
import { cn } from '@/lib/utils'

interface MonthlyBudgetCardProps {
  cashbookId: string
  cashbookName: string
  transactions: Transaction[]
  canEdit?: boolean
  onToast: (msg: string) => void
  className?: string
}

export function MonthlyBudgetCard({
  cashbookId,
  cashbookName,
  transactions,
  canEdit = true,
  onToast,
  className,
}: MonthlyBudgetCardProps) {
  const [showConfig, setShowConfig] = useState(false)
  const [customBudget, setCustomBudget] = useState<number | null>(null)

  useEffect(() => {
    if (!cashbookId) return
    const stored = localStorage.getItem(`spendwise-budget-${cashbookId}`)
    if (stored) {
      const val = Number(stored)
      if (!isNaN(val) && val > 0) {
        setCustomBudget(val)
        return
      }
    }
    setCustomBudget(null)
  }, [cashbookId])

  const now = new Date()
  const monthName = now.toLocaleString('en-US', { month: 'long' })
  const year = now.getFullYear()
  const daysInMonth = new Date(year, now.getMonth() + 1, 0).getDate()
  const currentDay = now.getDate()
  const daysRemaining = Math.max(1, daysInMonth - currentDay + 1)

  const currentMonthExpenses = useMemo(() => {
    return transactions.filter((t) => {
      if (!t.transactionDate) return false
      const d = new Date(t.transactionDate)
      return (
        !isNaN(d.getTime()) &&
        d.getFullYear() === year &&
        d.getMonth() === now.getMonth() &&
        t.transactionType?.toLowerCase() === 'cashout'
      )
    })
  }, [transactions, year, now])

  const spentSoFar = useMemo(() => {
    return currentMonthExpenses.reduce((sum, t) => sum + Number(t.amount || 0), 0)
  }, [currentMonthExpenses])

  const suggestedBudget = useMemo(() => {
    if (spentSoFar > 0) {
      const base = spentSoFar * 1.3
      return Math.max(10000, Math.ceil(base / 5000) * 5000)
    }
    return 50000
  }, [spentSoFar])

  const effectiveBudget = customBudget ?? suggestedBudget
  const percentUsed = effectiveBudget > 0 ? Math.round((spentSoFar / effectiveBudget) * 100) : 0
  const remainingBudget = effectiveBudget - spentSoFar

  const dailyBurnRate = currentDay > 0 ? Math.round(spentSoFar / currentDay) : 0
  const projectedTotal = Math.round(dailyBurnRate * daysInMonth)

  const isOverBudget = spentSoFar > effectiveBudget
  const isWarning = percentUsed >= 75 && percentUsed <= 90
  const isDanger = percentUsed > 90

  const handleSaveBudget = (amount: number) => {
    localStorage.setItem(`spendwise-budget-${cashbookId}`, String(amount))
    setCustomBudget(amount)
    onToast(`Monthly budget updated to ₨${amount.toLocaleString()}`)
  }

  // Color for progress indicator
  const progressColorClass = isDanger
    ? 'bg-outflow'
    : isWarning
    ? 'bg-warning'
    : 'bg-inflow'

  return (
    <>
      <Card
        className={cn(
          'flex flex-col justify-between border-border/80 shadow-xs p-6',
          className
        )}
      >
        <CardHeader className="p-0 pb-3 flex flex-row items-center justify-between space-y-0">
          <div className="space-y-0.5">
            <span className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              {monthName} Target
            </span>
            <CardTitle className="text-base font-semibold">Monthly Budget</CardTitle>
          </div>
          <Gate
            allow={canEdit}
            reason="Budget settings restricted — ask the cashbook owner for access"
          >
            <Button
              variant="ghost"
              size="icon"
              className="size-8"
              onClick={() => setShowConfig(true)}
              aria-label="Configure budget"
            >
              <SlidersHorizontal className="size-4" />
            </Button>
          </Gate>
        </CardHeader>

        <CardContent className="p-0 space-y-4">
          {/* Numbers row */}
          <div className="flex items-baseline justify-between">
            <div>
              <p className="text-2xl font-semibold font-mono tabular-nums tracking-tight">
                <AnimatedNumber value={spentSoFar} formatFn={(v) => formatMoney(v)} />
              </p>
              <p className="text-xs text-muted-foreground">
                spent of <span className="font-mono">{formatMoney(effectiveBudget)}</span> limit
              </p>
            </div>
            <Badge
              variant="outline"
              className={cn(
                'font-mono text-xs font-medium',
                isDanger
                  ? 'border-outflow/30 bg-outflow-muted/50 text-outflow'
                  : isWarning
                  ? 'border-warning/30 bg-warning-muted/50 text-amber-600 dark:text-amber-400'
                  : 'border-inflow/30 bg-inflow-muted/50 text-inflow'
              )}
            >
              <AnimatedNumber value={percentUsed} />% used
            </Badge>
          </div>

          {/* Progress bar */}
          <div className="space-y-1">
            <Progress value={Math.min(100, percentUsed)} className="h-2">
              <div
                className={cn('h-full rounded-full transition-[width] duration-(--duration-slow) ease-out', progressColorClass)}
                style={{ width: `${Math.min(100, percentUsed)}%` }}
              />
            </Progress>
          </div>

          {/* Forecast & Metrics footer */}
          <div className="grid grid-cols-2 gap-2 pt-2 border-t text-xs">
            <div className="space-y-0.5">
              <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                <CalendarDays className="size-3" /> Days remaining
              </span>
              <p className="font-mono font-medium">{daysRemaining} days</p>
            </div>
            <div className="space-y-0.5 text-right">
              <span className="text-[11px] text-muted-foreground flex items-center justify-end gap-1">
                <Flame className="size-3" /> Daily pace
              </span>
              <p className="font-mono font-medium">{formatMoney(dailyBurnRate)}/day</p>
            </div>
          </div>

          {/* Forecast message */}
          <div
            className={cn(
              'rounded-lg p-2.5 text-xs flex items-center gap-2',
              isOverBudget
                ? 'bg-outflow-muted/60 text-outflow border border-outflow/20'
                : isDanger
                ? 'bg-warning-muted/60 text-amber-700 dark:text-amber-400 border border-warning/20'
                : 'bg-muted/50 text-muted-foreground'
            )}
          >
            {isOverBudget ? (
              <>
                <AlertTriangle className="size-3.5 shrink-0" />
                <span>Over budget by {formatMoney(Math.abs(remainingBudget))}</span>
              </>
            ) : projectedTotal <= effectiveBudget ? (
              <>
                <CheckCircle2 className="size-3.5 shrink-0 text-inflow" />
                <span>
                  On track to finish {formatMoney(effectiveBudget - projectedTotal)} under budget
                </span>
              </>
            ) : (
              <>
                <Target className="size-3.5 shrink-0 text-amber-500" />
                <span>Paced to exceed limit if spending continues</span>
              </>
            )}
          </div>
        </CardContent>
      </Card>

      <BudgetConfigDialog
        open={showConfig}
        onOpenChange={setShowConfig}
        currentBudget={customBudget ?? 0}
        suggestedBudget={suggestedBudget}
        cashbookName={cashbookName}
        onSave={handleSaveBudget}
      />
    </>
  )
}
