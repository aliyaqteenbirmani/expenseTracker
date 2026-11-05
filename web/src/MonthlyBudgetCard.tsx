import { useEffect, useMemo, useState } from 'react'
import { AlertCircle, AlertTriangle, CheckCircle2, Flame, SlidersHorizontal, Sparkles, Target } from 'lucide-react'
import type { Transaction } from './App'
import BudgetConfigDialog from './BudgetConfigDialog'

type Props = {
  cashbookId: string
  cashbookName: string
  transactions: Transaction[]
  onToast: (msg: string) => void
}

export default function MonthlyBudgetCard({
  cashbookId,
  cashbookName,
  transactions,
  onToast
}: Props) {
  const [showConfig, setShowConfig] = useState(false)
  const [customBudget, setCustomBudget] = useState<number | null>(null)

  // Load persisted budget for this cashbook
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

  // Current month's expenses
  const currentMonthExpenses = useMemo(() => {
    return transactions.filter(t => {
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

  // Suggested budget based on activity
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
  const safeDailyRemaining = remainingBudget > 0 ? Math.round(remainingBudget / daysRemaining) : 0
  const projectedTotal = Math.round(dailyBurnRate * daysInMonth)

  const isOverBudget = spentSoFar > effectiveBudget
  const isPaceWarning = !isOverBudget && projectedTotal > effectiveBudget && percentUsed > 50

  function handleSaveBudget(amount: number) {
    setCustomBudget(amount)
    localStorage.setItem(`spendwise-budget-${cashbookId}`, String(amount))
    onToast(`Monthly budget set to Rs. ${amount.toLocaleString('en-PK')}`)
  }

  const statusInfo = useMemo(() => {
    if (isOverBudget) {
      return {
        label: `Over Budget by Rs. ${Math.abs(remainingBudget).toLocaleString('en-PK')}`,
        className: 'status-over',
        Icon: AlertCircle,
        advice: 'You have exceeded this month’s target limit. Consider reducing non-essential expenses.'
      }
    }
    if (isPaceWarning) {
      return {
        label: 'Pace Alert',
        className: 'status-warning',
        Icon: AlertTriangle,
        advice: `At current burn rate, you're projected to spend Rs. ${projectedTotal.toLocaleString('en-PK')} this month.`
      }
    }
    return {
      label: 'On Track',
      className: 'status-good',
      Icon: CheckCircle2,
      advice: `Healthy spending! You have Rs. ${safeDailyRemaining.toLocaleString('en-PK')}/day safe to spend for the next ${daysRemaining} days.`
    }
  }, [isOverBudget, isPaceWarning, remainingBudget, projectedTotal, safeDailyRemaining, daysRemaining])

  const StatusIcon = statusInfo.Icon

  return (
    <article className="monthly-budget-card">
      <div className="budget-card-header">
        <div className="budget-header-left">
          <span className={`budget-icon-badge ${statusInfo.className}`}>
            <Target size={18} strokeWidth={2} />
          </span>
          <div>
            <div className="budget-kicker">
              <span>{monthName} {year}</span> · <strong>{cashbookName}</strong>
            </div>
            <h3>Monthly Spending Target</h3>
          </div>
        </div>

        <div className="budget-header-right">
          <span className={`budget-status-pill ${statusInfo.className}`}>
            <StatusIcon size={12} strokeWidth={2.2} />
            {statusInfo.label}
          </span>
          <button
            type="button"
            className="budget-adjust-btn"
            onClick={() => setShowConfig(true)}
            title="Adjust monthly budget goal"
          >
            <SlidersHorizontal size={13} strokeWidth={1.8} />
            <span>{customBudget ? 'Adjust' : 'Set goal'}</span>
          </button>
        </div>
      </div>

      <div className="budget-card-body">
        {/* Metric Line */}
        <div className="budget-metric-row">
          <div>
            <span className="budget-spent-num">
              Rs. {spentSoFar.toLocaleString('en-PK')}
            </span>
            <span className="budget-target-num">
              {' '}of Rs. {effectiveBudget.toLocaleString('en-PK')}
            </span>
            {!customBudget && (
              <span className="budget-ai-tag" title="Auto-estimated target based on current activity">
                <Sparkles size={10} /> Auto-suggested
              </span>
            )}
          </div>
          <div className="budget-percent-badge">
            <strong>{percentUsed}%</strong>
            <small>used</small>
          </div>
        </div>

        {/* Dynamic Progress Bar */}
        <div className="budget-progress-track">
          <div
            className={`budget-progress-fill ${statusInfo.className}`}
            style={{ width: `${Math.min(100, Math.max(6, percentUsed))}%` }}
          />
        </div>

        {/* Insights & Cadence Footer */}
        <div className="budget-insights-grid">
          <div className="budget-insight-col">
            <small>REMAINING BUDGET</small>
            <strong className={remainingBudget < 0 ? 'neg' : ''}>
              {remainingBudget >= 0 ? '+' : '−'} Rs. {Math.abs(remainingBudget).toLocaleString('en-PK')}
            </strong>
          </div>
          <div className="budget-insight-col">
            <small>SAFE DAILY PACE</small>
            <strong>
              Rs. {safeDailyRemaining.toLocaleString('en-PK')} <span className="budget-unit">/ day</span>
            </strong>
          </div>
          <div className="budget-insight-col">
            <small>TIME REMAINING</small>
            <strong>
              {daysRemaining} <span className="budget-unit">days left</span>
            </strong>
          </div>
        </div>

        <div className="budget-footer-advice">
          <Flame size={13} className="burn-icon" />
          <p>{statusInfo.advice}</p>
        </div>
      </div>

      {showConfig && (
        <BudgetConfigDialog
          currentBudget={effectiveBudget}
          suggestedBudget={suggestedBudget}
          cashbookName={cashbookName}
          onSave={handleSaveBudget}
          onClose={() => setShowConfig(false)}
        />
      )}
    </article>
  )
}
