import { Printer, X } from 'lucide-react'

type CategorySummary = {
  label: string
  amount: number
  count: number
  percentage: number
}

type Transaction = {
  id: string
  amount: number
  transactionType: string
  remarks?: string
  transactionDate?: string
}

type Props = {
  businessName: string
  cashbookName: string
  periodLabel: string
  totalIncome: number
  totalExpense: number
  netBalance: number
  savingsRate: number
  categoryBreakdown: CategorySummary[]
  transactions: Transaction[]
  onClose: () => void
}

export default function FinancialStatementModal({
  businessName,
  cashbookName,
  periodLabel,
  totalIncome,
  totalExpense,
  netBalance,
  savingsRate,
  categoryBreakdown,
  transactions,
  onClose
}: Props) {
  const currentDate = new Intl.DateTimeFormat('en-US', {
    dateStyle: 'full',
    timeStyle: 'short'
  }).format(new Date())

  const sortedTransactions = [...transactions].sort((a, b) =>
    (b.transactionDate ?? '').localeCompare(a.transactionDate ?? '')
  )

  return (
    <div className="overlay statement-modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
      <div className="statement-modal-window" onClick={e => e.stopPropagation()}>
        {/* Screen Toolbar (hidden in print) */}
        <div className="statement-toolbar no-print">
          <div>
            <strong>Statement Preview</strong>
            <small style={{ display: 'block', color: '#748b7c', fontSize: 11 }}>
              Ready for high-resolution printing or saving as PDF.
            </small>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <button
              type="button"
              className="button primary"
              onClick={() => window.print()}
              style={{ minHeight: 36, height: 36, padding: '0 15px', fontSize: 11 }}
            >
              <Printer size={15} strokeWidth={2} /> Print / Save as PDF
            </button>
            <button
              type="button"
              className="close"
              onClick={onClose}
              aria-label="Close statement"
            >
              <X size={18} strokeWidth={2} />
            </button>
          </div>
        </div>

        {/* Printable Paper Document */}
        <div className="statement-paper">
          {/* Header */}
          <div className="statement-header">
            <div className="statement-branding">
              <span className="statement-logo">✦</span>
              <div>
                <h2>{businessName}</h2>
                <p>Cashbook: <strong>{cashbookName}</strong></p>
              </div>
            </div>
            <div className="statement-meta">
              <h3>FINANCIAL STATEMENT</h3>
              <p>Period: <strong>{periodLabel}</strong></p>
              <p>Generated: <span>{currentDate}</span></p>
            </div>
          </div>

          <div className="statement-divider" />

          {/* Executive KPI Summary */}
          <div className="statement-kpis">
            <div className="statement-kpi-box">
              <small>TOTAL INFLOW</small>
              <strong className="kpi-green">Rs. {totalIncome.toLocaleString('en-PK')}</strong>
            </div>
            <div className="statement-kpi-box">
              <small>TOTAL OUTFLOW</small>
              <strong className="kpi-red">Rs. {totalExpense.toLocaleString('en-PK')}</strong>
            </div>
            <div className="statement-kpi-box">
              <small>NET CASH FLOW</small>
              <strong className={netBalance >= 0 ? 'kpi-green' : 'kpi-red'}>
                {netBalance >= 0 ? '+' : '−'} Rs. {Math.abs(netBalance).toLocaleString('en-PK')}
              </strong>
            </div>
            <div className="statement-kpi-box">
              <small>SAVINGS RATE</small>
              <strong>{savingsRate}%</strong>
            </div>
          </div>

          {/* Category Expense Breakdown */}
          {categoryBreakdown.length > 0 && (
            <div className="statement-section">
              <h4>Expense Breakdown by Category</h4>
              <table className="statement-table">
                <thead>
                  <tr>
                    <th>Category</th>
                    <th style={{ textAlign: 'right' }}>Total (PKR)</th>
                    <th style={{ textAlign: 'center' }}>Transactions</th>
                    <th style={{ textAlign: 'right' }}>Share of Outflow</th>
                  </tr>
                </thead>
                <tbody>
                  {categoryBreakdown.map(cat => (
                    <tr key={cat.label}>
                      <td><strong>{cat.label}</strong></td>
                      <td style={{ textAlign: 'right' }}>Rs. {cat.amount.toLocaleString('en-PK')}</td>
                      <td style={{ textAlign: 'center' }}>{cat.count}</td>
                      <td style={{ textAlign: 'right' }}>{cat.percentage}%</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Itemized Transaction Register */}
          <div className="statement-section">
            <h4>Itemized Transaction Ledger ({sortedTransactions.length} entries)</h4>
            <table className="statement-table ledger-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Description / Remarks</th>
                  <th>Type</th>
                  <th style={{ textAlign: 'right' }}>Amount (PKR)</th>
                </tr>
              </thead>
              <tbody>
                {sortedTransactions.map(t => {
                  const isIncome = t.transactionType?.toLowerCase() === 'cashin'
                  const dateStr = t.transactionDate ? new Date(t.transactionDate).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric'
                  }) : '—'

                  return (
                    <tr key={t.id}>
                      <td style={{ whiteSpace: 'nowrap' }}>{dateStr}</td>
                      <td>{t.remarks || (isIncome ? 'Cash received' : 'Cash expense')}</td>
                      <td>
                        <span className={`statement-badge ${isIncome ? 'income' : 'expense'}`}>
                          {isIncome ? 'Inflow' : 'Outflow'}
                        </span>
                      </td>
                      <td style={{ textAlign: 'right', fontWeight: 700 }} className={isIncome ? 'kpi-green' : 'kpi-red'}>
                        {isIncome ? '+' : '−'} Rs. {Number(t.amount || 0).toLocaleString('en-PK')}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {/* Signature / Auditor Footer */}
          <div className="statement-footer">
            <p>Generated electronically from Spendwise Expense Tracker. Certified financial activity record.</p>
            <div className="statement-sign-row">
              <div className="statement-sign-box">
                <div className="sign-line" />
                <small>Prepared by: System Administrator</small>
              </div>
              <div className="statement-sign-box">
                <div className="sign-line" />
                <small>Approved by: Workspace Manager</small>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
