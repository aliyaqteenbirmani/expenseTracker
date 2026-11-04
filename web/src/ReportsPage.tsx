import { useMemo, useState } from 'react'
import {
  ArrowDownLeft,
  BriefcaseBusiness,
  Calendar,
  Download,
  Fuel,
  HeartPulse,
  House,
  ReceiptText,
  ShoppingCart,
  TrendingDown,
  TrendingUp,
  Utensils,
  Wallet,
  type LucideIcon
} from 'lucide-react'

type Transaction = {
  id: string
  amount: number
  transactionType: string
  remarks?: string
  transactionDate?: string
}

type Props = {
  transactions: Transaction[]
  cashbookName: string
  businessName: string
  onBack: () => void
  onToast: (message: string) => void
}

type CategorySummary = {
  label: string
  amount: number
  count: number
  percentage: number
  Icon: LucideIcon
  color: string
}

function getCategoryInfo(transaction: Transaction): { label: string; Icon: LucideIcon; color: string } {
  const text = (transaction.remarks ?? '').toLowerCase()
  if (transaction.transactionType?.toLowerCase() === 'cashin') {
    return { label: 'Income', Icon: BriefcaseBusiness, color: '#16835c' }
  }
  if (/rent|house|home|electric|utility|water bill/.test(text)) {
    return { label: 'Housing & Bills', Icon: House, color: '#457b9d' }
  }
  if (/fuel|petrol|diesel|gas station|uber|taxi|careem|bus|transport/.test(text)) {
    return { label: 'Transport & Travel', Icon: Fuel, color: '#e76f51' }
  }
  if (/food|grocery|restaurant|cafe|meal|nashta|khana|dinner|lunch|chai/.test(text)) {
    return { label: 'Food & Dining', Icon: Utensils, color: '#f4a261' }
  }
  if (/medical|health|doctor|pharmacy|medicine|dawa|hospital/.test(text)) {
    return { label: 'Health & Medical', Icon: HeartPulse, color: '#e63946' }
  }
  if (/shop|clothes|store|purchase|shoes|retail/.test(text)) {
    return { label: 'Shopping', Icon: ShoppingCart, color: '#9d4edd' }
  }
  return { label: 'Other Expenses', Icon: ReceiptText, color: '#6c757d' }
}

export default function ReportsPage({ transactions, cashbookName, businessName, onBack, onToast }: Props) {
  const [period, setPeriod] = useState<'thisMonth' | 'lastMonth' | 'last30Days' | 'thisYear' | 'all'>('thisMonth')

  const filteredTransactions = useMemo(() => {
    const now = new Date()
    return transactions.filter(t => {
      if (!t.transactionDate) return period === 'all'
      const tDate = new Date(t.transactionDate)
      if (Number.isNaN(tDate.getTime())) return period === 'all'

      if (period === 'thisMonth') {
        return tDate.getFullYear() === now.getFullYear() && tDate.getMonth() === now.getMonth()
      }
      if (period === 'lastMonth') {
        const lastMonth = new Date(now.getFullYear(), now.getMonth() - 1, 1)
        return tDate.getFullYear() === lastMonth.getFullYear() && tDate.getMonth() === lastMonth.getMonth()
      }
      if (period === 'last30Days') {
        const thirtyDaysAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000)
        return tDate >= thirtyDaysAgo && tDate <= now
      }
      if (period === 'thisYear') {
        return tDate.getFullYear() === now.getFullYear()
      }
      return true
    })
  }, [transactions, period])

  const totalIncome = useMemo(() => {
    return filteredTransactions
      .filter(t => t.transactionType?.toLowerCase() === 'cashin')
      .reduce((sum, t) => sum + Number(t.amount || 0), 0)
  }, [filteredTransactions])

  const totalExpense = useMemo(() => {
    return filteredTransactions
      .filter(t => t.transactionType?.toLowerCase() === 'cashout')
      .reduce((sum, t) => sum + Number(t.amount || 0), 0)
  }, [filteredTransactions])

  const netBalance = totalIncome - totalExpense
  const savingsRate = totalIncome > 0 ? Math.max(0, Math.round((netBalance / totalIncome) * 100)) : 0

  const expenseTransactions = useMemo(() => {
    return filteredTransactions.filter(t => t.transactionType?.toLowerCase() === 'cashout')
  }, [filteredTransactions])

  const categoryBreakdown = useMemo<CategorySummary[]>(() => {
    const map = new Map<string, { amount: number; count: number; Icon: LucideIcon; color: string }>()

    for (const t of expenseTransactions) {
      const { label, Icon, color } = getCategoryInfo(t)
      const existing = map.get(label) || { amount: 0, count: 0, Icon, color }
      existing.amount += Number(t.amount || 0)
      existing.count += 1
      map.set(label, existing)
    }

    const list: CategorySummary[] = []
    for (const [label, data] of map.entries()) {
      list.push({
        label,
        amount: data.amount,
        count: data.count,
        percentage: totalExpense > 0 ? Math.round((data.amount / totalExpense) * 100) : 0,
        Icon: data.Icon,
        color: data.color
      })
    }

    return list.sort((a, b) => b.amount - a.amount)
  }, [expenseTransactions, totalExpense])

  const topExpenses = useMemo(() => {
    return [...expenseTransactions]
      .sort((a, b) => Number(b.amount || 0) - Number(a.amount || 0))
      .slice(0, 5)
  }, [expenseTransactions])

  function exportReport() {
    const periodLabel = period === 'thisMonth' ? 'This Month' : period === 'lastMonth' ? 'Last Month' : period === 'last30Days' ? 'Last 30 Days' : period === 'thisYear' ? 'This Year' : 'All Time'
    const reportText = [
      `SPENDWISE FINANCIAL REPORT`,
      `Workspace: ${businessName}`,
      `Cashbook: ${cashbookName}`,
      `Period: ${periodLabel}`,
      `Generated: ${new Date().toLocaleDateString()}`,
      `----------------------------------------`,
      `Total Income:   Rs. ${totalIncome.toLocaleString('en-PK')}`,
      `Total Expenses: Rs. ${totalExpense.toLocaleString('en-PK')}`,
      `Net Cash Flow:  Rs. ${netBalance.toLocaleString('en-PK')}`,
      `Savings Rate:   ${savingsRate}%`,
      `----------------------------------------`,
      `EXPENSE BREAKDOWN BY CATEGORY:`,
      ...categoryBreakdown.map(c => `- ${c.label}: Rs. ${c.amount.toLocaleString('en-PK')} (${c.percentage}%) [${c.count} txns]`),
      `----------------------------------------`,
      `TOP EXPENSES:`,
      ...topExpenses.map(t => `- Rs. ${Number(t.amount).toLocaleString('en-PK')} | ${t.transactionDate?.slice(0, 10) || 'No date'} | ${t.remarks || 'No notes'}`)
    ].join('\r\n')

    const blob = new Blob([reportText], { type: 'text/plain;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `spendwise-report-${period}.txt`
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
    onToast('Report exported successfully')
  }

  return (
    <section className="reports-page">
      <div className="history-heading">
        <div>
          <button className="history-back" onClick={onBack}>
            <ArrowDownLeft size={14} strokeWidth={1.8} /> Back to overview
          </button>
          <div className="eyebrow">FINANCIAL INTELLIGENCE</div>
          <h1>Financial Reports & Insights</h1>
          <p className="muted">
            {businessName} · {cashbookName}
          </p>
        </div>
        <div className="reports-top-actions">
          <button className="export-button" onClick={exportReport} disabled={filteredTransactions.length === 0}>
            <Download size={14} /> Export summary
          </button>
        </div>
      </div>

      <div className="reports-filter-bar">
        <span className="filter-label"><Calendar size={14} /> Period:</span>
        <div className="type-chips">
          <button className={period === 'thisMonth' ? 'selected' : ''} onClick={() => setPeriod('thisMonth')}>
            This month
          </button>
          <button className={period === 'lastMonth' ? 'selected' : ''} onClick={() => setPeriod('lastMonth')}>
            Last month
          </button>
          <button className={period === 'last30Days' ? 'selected' : ''} onClick={() => setPeriod('last30Days')}>
            Last 30 days
          </button>
          <button className={period === 'thisYear' ? 'selected' : ''} onClick={() => setPeriod('thisYear')}>
            This year
          </button>
          <button className={period === 'all' ? 'selected' : ''} onClick={() => setPeriod('all')}>
            All time
          </button>
        </div>
      </div>

      <div className="reports-kpi-grid">
        <article className="report-kpi income-card">
          <div className="kpi-icon green">
            <TrendingUp size={18} />
          </div>
          <small>TOTAL INFLOW</small>
          <strong>Rs. {totalIncome.toLocaleString('en-PK')}</strong>
          <span className="kpi-sub">{filteredTransactions.filter(t => t.transactionType?.toLowerCase() === 'cashin').length} income events</span>
        </article>

        <article className="report-kpi expense-card">
          <div className="kpi-icon orange">
            <TrendingDown size={18} />
          </div>
          <small>TOTAL OUTFLOW</small>
          <strong>Rs. {totalExpense.toLocaleString('en-PK')}</strong>
          <span className="kpi-sub">{expenseTransactions.length} expenses recorded</span>
        </article>

        <article className="report-kpi balance-card">
          <div className="kpi-icon blue">
            <Wallet size={18} />
          </div>
          <small>NET CASH FLOW</small>
          <strong className={netBalance >= 0 ? 'pos' : 'neg'}>
            {netBalance >= 0 ? '+' : '−'} Rs. {Math.abs(netBalance).toLocaleString('en-PK')}
          </strong>
          <span className="kpi-sub">Savings rate: {savingsRate}%</span>
        </article>
      </div>

      <div className="reports-grid">
        <article className="panel category-panel">
          <div className="panel-head">
            <div>
              <h2>Spending by category</h2>
              <p>Where your money went during this period.</p>
            </div>
            <span className="panel-badge">{categoryBreakdown.length} Categories</span>
          </div>

          {categoryBreakdown.length === 0 ? (
            <div className="empty-category-state">
              <p className="muted">No expenses recorded for this period.</p>
            </div>
          ) : (
            <div className="category-bars-list">
              {categoryBreakdown.map(cat => {
                const CatIcon = cat.Icon
                return (
                  <div className="category-bar-row" key={cat.label}>
                    <div className="cat-title-row">
                      <div className="cat-left">
                        <span className="cat-icon-badge" style={{ backgroundColor: `${cat.color}18`, color: cat.color }}>
                          <CatIcon size={14} />
                        </span>
                        <strong>{cat.label}</strong>
                        <small className="muted">({cat.count} txns)</small>
                      </div>
                      <div className="cat-right">
                        <strong>Rs. {cat.amount.toLocaleString('en-PK')}</strong>
                        <span className="cat-pct">{cat.percentage}%</span>
                      </div>
                    </div>
                    <div className="cat-progress-track">
                      <div
                        className="cat-progress-fill"
                        style={{ width: `${cat.percentage}%`, backgroundColor: cat.color }}
                      />
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </article>

        <article className="panel top-expenses-panel">
          <div className="panel-head">
            <div>
              <h2>Largest expenses</h2>
              <p>Top single purchases in this period.</p>
            </div>
          </div>

          {topExpenses.length === 0 ? (
            <div className="empty-category-state">
              <p className="muted">No expenses recorded for this period.</p>
            </div>
          ) : (
            <div className="top-expenses-list">
              {topExpenses.map((txn, index) => {
                const cat = getCategoryInfo(txn)
                const CatIcon = cat.Icon
                return (
                  <div className="top-expense-row" key={txn.id ?? index}>
                    <div className="top-expense-left">
                      <span className="rank-num">#{index + 1}</span>
                      <span className="cat-icon-badge" style={{ backgroundColor: `${cat.color}18`, color: cat.color }}>
                        <CatIcon size={14} />
                      </span>
                      <div>
                        <strong>{txn.remarks || 'Expense'}</strong>
                        <small>{txn.transactionDate ? new Date(txn.transactionDate).toLocaleDateString() : 'Date missing'} · {cat.label}</small>
                      </div>
                    </div>
                    <strong className="top-expense-amount">
                      Rs. {Number(txn.amount).toLocaleString('en-PK')}
                    </strong>
                  </div>
                )
              })}
            </div>
          )}
        </article>
      </div>
    </section>
  )
}
