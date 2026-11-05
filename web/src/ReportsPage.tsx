import { useMemo, useState } from 'react'
import {
  ArrowDownLeft,
  BriefcaseBusiness,
  Calendar,
  FileSpreadsheet,
  FileText,
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
import type { Cashbook } from './App'

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
  books?: Cashbook[]
  selectedBookId?: string
  onSelectBook?: (book: Cashbook) => void
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

export default function ReportsPage({
  transactions,
  cashbookName,
  businessName,
  books,
  selectedBookId,
  onSelectBook,
  onBack,
  onToast
}: Props) {
  const [period, setPeriod] = useState<'thisMonth' | 'lastMonth' | 'last30Days' | 'thisYear' | 'all'>('thisMonth')
  const [selectedCategoryFilter, setSelectedCategoryFilter] = useState<string | null>(null)

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

  const statsOverview = useMemo(() => {
    if (!filteredTransactions.length) {
      return { dailyAverage: 0, peakExpense: null }
    }

    let days = 30
    if (period === 'thisMonth' || period === 'lastMonth') days = 30
    else if (period === 'thisYear') days = 365
    else if (period === 'all') {
      const dates = filteredTransactions
        .map(t => (t.transactionDate ? new Date(t.transactionDate).getTime() : 0))
        .filter(t => t > 0)
      if (dates.length >= 2) {
        const diff = Math.max(...dates) - Math.min(...dates)
        days = Math.max(1, Math.round(diff / (1000 * 60 * 60 * 24)))
      }
    }
    const dailyAverage = Math.round(totalExpense / days)

    let peakExpense: Transaction | null = null
    for (const t of expenseTransactions) {
      if (!peakExpense || Number(t.amount) > Number(peakExpense.amount)) {
        peakExpense = t
      }
    }

    return { dailyAverage, peakExpense }
  }, [filteredTransactions, expenseTransactions, totalExpense, period])

  const timelineBuckets = useMemo(() => {
    const barsCount = 12
    if (!filteredTransactions.length) return []

    const sorted = [...filteredTransactions]
      .filter(t => t.transactionDate && !Number.isNaN(new Date(t.transactionDate).getTime()))
      .sort((a, b) => new Date(a.transactionDate!).getTime() - new Date(b.transactionDate!).getTime())

    if (!sorted.length) return []

    const minTime = new Date(sorted[0].transactionDate!).getTime()
    const maxTime = Math.max(new Date(sorted[sorted.length - 1].transactionDate!).getTime(), minTime + 1000)
    const step = (maxTime - minTime) / barsCount || 1

    const buckets = Array.from({ length: barsCount }).map((_, i) => {
      const bucketStart = minTime + i * step
      const bucketEnd = bucketStart + step
      const bucketTxns = sorted.filter(t => {
        const time = new Date(t.transactionDate!).getTime()
        return i === barsCount - 1
          ? time >= bucketStart && time <= bucketEnd + 1000
          : time >= bucketStart && time < bucketEnd
      })

      const inflow = bucketTxns
        .filter(t => t.transactionType?.toLowerCase() === 'cashin')
        .reduce((sum, t) => sum + Number(t.amount || 0), 0)
      const outflow = bucketTxns
        .filter(t => t.transactionType?.toLowerCase() === 'cashout')
        .reduce((sum, t) => sum + Number(t.amount || 0), 0)

      const startLabel = new Date(bucketStart).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
      const endLabel = new Date(bucketEnd).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
      const label = startLabel === endLabel ? startLabel : `${startLabel} - ${endLabel}`

      return {
        key: `report-bar-${i}`,
        label,
        inflow,
        outflow,
        net: inflow - outflow,
        count: bucketTxns.length
      }
    })

    const maxVal = Math.max(...buckets.map(b => Math.max(b.inflow, b.outflow)), 1)

    return buckets.map(b => ({
      ...b,
      inflowPct: b.inflow > 0 ? Math.max(8, Math.round((b.inflow / maxVal) * 100)) : 0,
      outflowPct: b.outflow > 0 ? Math.max(8, Math.round((b.outflow / maxVal) * 100)) : 0
    }))
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
    const list = selectedCategoryFilter
      ? expenseTransactions.filter(t => getCategoryInfo(t).label === selectedCategoryFilter)
      : expenseTransactions

    return [...list]
      .sort((a, b) => Number(b.amount || 0) - Number(a.amount || 0))
      .slice(0, 6)
  }, [expenseTransactions, selectedCategoryFilter])

  function exportReport() {
    const periodLabel = period === 'thisMonth' ? 'This Month' : period === 'lastMonth' ? 'Last Month' : period === 'last30Days' ? 'Last 30 Days' : period === 'thisYear' ? 'This Year' : 'All Time'
    const reportText = [
      `SPENDWISE FINANCIAL REPORT`,
      `Workspace: ${businessName}`,
      `Cashbook: ${cashbookName}`,
      `Period: ${periodLabel}`,
      `Generated: ${new Date().toLocaleDateString()}`,
      `----------------------------------------`,
      `Total Income:       Rs. ${totalIncome.toLocaleString('en-PK')}`,
      `Total Expenses:     Rs. ${totalExpense.toLocaleString('en-PK')}`,
      `Net Cash Flow:      Rs. ${netBalance.toLocaleString('en-PK')}`,
      `Savings Rate:       ${savingsRate}%`,
      `Avg Daily Spend:    Rs. ${statsOverview.dailyAverage.toLocaleString('en-PK')}`,
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
    onToast('Summary text report exported')
  }

  function exportCsvReport() {
    const periodLabel = period === 'thisMonth' ? 'This Month' : period === 'lastMonth' ? 'Last Month' : period === 'last30Days' ? 'Last 30 Days' : period === 'thisYear' ? 'This Year' : 'All Time'
    const escapeCell = (value: unknown) => `"${String(value ?? '').replace(/"/g, '""')}"`

    const rows: (string | number)[][] = [
      ['SPENDWISE FINANCIAL REPORT', ''],
      ['Workspace', businessName],
      ['Cashbook', cashbookName],
      ['Period', periodLabel],
      ['Generated On', new Date().toLocaleString()],
      [''],
      ['EXECUTIVE SUMMARY', ''],
      ['Metric', 'Amount (PKR)'],
      ['Total Inflow (Income)', totalIncome],
      ['Total Outflow (Expenses)', totalExpense],
      ['Net Cash Flow', netBalance],
      ['Savings Rate (%)', `${savingsRate}%`],
      ['Average Daily Spend', statsOverview.dailyAverage],
      ['Total Transactions', filteredTransactions.length],
      [''],
      ['EXPENSE BREAKDOWN BY CATEGORY', '', '', ''],
      ['Category', 'Total Amount (PKR)', 'Count', 'Share of Expenses (%)'],
      ...categoryBreakdown.map(c => [
        c.label,
        c.amount,
        c.count,
        `${c.percentage}%`
      ]),
      [''],
      ['TRANSACTION DETAILS', '', '', '', ''],
      ['Date', 'Remarks', 'Type', 'Category', 'Amount (PKR)'],
      ...filteredTransactions.map(t => [
        t.transactionDate?.slice(0, 10) || '',
        t.remarks || '',
        t.transactionType?.toLowerCase() === 'cashin' ? 'Income' : 'Expense',
        getCategoryInfo(t).label,
        Number(t.amount || 0)
      ])
    ]

    const csvContent = '\uFEFF' + rows.map(r => r.map(escapeCell).join(',')).join('\r\n')
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `spendwise-report-${period}.csv`
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
    onToast('CSV report exported successfully')
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
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
            {books && books.length > 1 ? (
              <select
                value={selectedBookId}
                onChange={e => {
                  const chosen = books.find(b => b.id === e.target.value)
                  if (chosen && onSelectBook) onSelectBook(chosen)
                }}
                style={{
                  border: '1px solid #d8e4da',
                  borderRadius: 7,
                  padding: '4px 8px',
                  fontSize: 11,
                  fontWeight: 700,
                  color: '#1a4734',
                  background: '#fff',
                  outline: 'none'
                }}
              >
                {books.map(b => (
                  <option key={b.id} value={b.id}>
                    {b.cashBookName ?? b.name ?? 'Cashbook'}
                  </option>
                ))}
              </select>
            ) : (
              <span className="muted" style={{ fontSize: 11 }}>{cashbookName}</span>
            )}
            <span className="muted" style={{ fontSize: 11 }}>· {businessName}</span>
          </div>
        </div>
        <div className="reports-top-actions">
          <button className="export-button" onClick={exportReport} disabled={filteredTransactions.length === 0} title="Download plain text summary">
            <FileText size={14} /> Summary (.txt)
          </button>
          <button className="button primary" onClick={exportCsvReport} disabled={filteredTransactions.length === 0} style={{ height: 36, padding: '0 14px', fontSize: 11 }} title="Download comprehensive CSV spreadsheet">
            <FileSpreadsheet size={14} strokeWidth={2} /> Export CSV
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

      {/* 4 KPI Summary Cards */}
      <div className="reports-kpi-grid four-cards">
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

        <article className="report-kpi cadence-card">
          <div className="kpi-icon purple">
            <Calendar size={18} />
          </div>
          <small>AVERAGE DAILY SPEND</small>
          <strong>Rs. {statsOverview.dailyAverage.toLocaleString('en-PK')}</strong>
          <span className="kpi-sub">
            {statsOverview.peakExpense ? `Peak: Rs. ${Number(statsOverview.peakExpense.amount).toLocaleString('en-PK')}` : 'No expenses recorded'}
          </span>
        </article>
      </div>

      {/* Cash Flow Comparison Timeline */}
      <article className="panel reports-timeline-panel">
        <div className="panel-head">
          <div>
            <h2>Cash Flow Trend & Comparison</h2>
            <p>Visualizing money coming in versus money going out over this timeframe.</p>
          </div>
          <div className="reports-chart-legend">
            <span className="legend-item"><span className="legend-dot green" /> Inflow</span>
            <span className="legend-item"><span className="legend-dot orange" /> Outflow</span>
          </div>
        </div>

        {timelineBuckets.length > 0 ? (
          <div className="reports-chart-wrapper">
            <div className="reports-dual-bars">
              {timelineBuckets.map(b => (
                <div
                  key={b.key}
                  className="dual-bar-column"
                  title={`${b.label}\nInflow: Rs. ${b.inflow.toLocaleString('en-PK')}\nOutflow: Rs. ${b.outflow.toLocaleString('en-PK')}\nNet: Rs. ${b.net.toLocaleString('en-PK')}`}
                >
                  <div className="bar-track">
                    <div
                      className="bar-fill green-fill"
                      style={{ height: `${b.inflowPct}%` }}
                    />
                    <div
                      className="bar-fill orange-fill"
                      style={{ height: `${b.outflowPct}%` }}
                    />
                  </div>
                  <span className="column-label">{b.label.split(' - ')[0]}</span>
                </div>
              ))}
            </div>
          </div>
        ) : (
          <div className="empty-category-state">
            <p className="muted">No transactions recorded for this period to chart.</p>
          </div>
        )}
      </article>

      <div className="reports-grid">
        <article className="panel category-panel">
          <div className="panel-head">
            <div>
              <h2>Spending by category</h2>
              <p>Where your money went during this period.</p>
            </div>
            <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
              {selectedCategoryFilter && (
                <button
                  type="button"
                  className="text-button"
                  style={{ fontSize: 10 }}
                  onClick={() => setSelectedCategoryFilter(null)}
                >
                  Clear filter ✕
                </button>
              )}
              <span className="panel-badge">{categoryBreakdown.length} Categories</span>
            </div>
          </div>

          {categoryBreakdown.length === 0 ? (
            <div className="empty-category-state">
              <p className="muted">No expenses recorded for this period.</p>
            </div>
          ) : (
            <div className="category-bars-list">
              {categoryBreakdown.map(cat => {
                const CatIcon = cat.Icon
                const isFiltered = selectedCategoryFilter === cat.label
                return (
                  <div
                    className={`category-bar-row ${isFiltered ? 'is-selected' : ''}`}
                    key={cat.label}
                    style={{ cursor: 'pointer' }}
                    onClick={() => setSelectedCategoryFilter(f => f === cat.label ? null : cat.label)}
                    title={`Click to filter top expenses by ${cat.label}`}
                  >
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
              <h2>Largest expenses {selectedCategoryFilter ? `(${selectedCategoryFilter})` : ''}</h2>
              <p>Top single purchases in this period.</p>
            </div>
            {selectedCategoryFilter && (
              <button
                type="button"
                className="text-button"
                style={{ fontSize: 11 }}
                onClick={() => setSelectedCategoryFilter(null)}
              >
                Show all
              </button>
            )}
          </div>

          {topExpenses.length === 0 ? (
            <div className="empty-category-state">
              <p className="muted">No expenses recorded for this selection.</p>
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
