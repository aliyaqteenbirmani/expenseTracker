import { useMemo } from 'react'
import { ArrowRight, PieChart } from 'lucide-react'
import { getTransactionCategory, type Transaction } from './App'

type Props = {
  transactions: Transaction[]
  onSelectCategory?: (category: string) => void
  onViewAllTransactions?: () => void
}

const CATEGORY_COLORS: Record<string, string> = {
  Food: '#e67e22',
  Transport: '#3498db',
  Housing: '#9b59b6',
  Shopping: '#e91e63',
  Health: '#1abc9c',
  'Other expense': '#7f8c8d'
}

export default function CategoryBreakdownCard({
  transactions,
  onSelectCategory,
  onViewAllTransactions
}: Props) {
  const expenseTransactions = useMemo(() => {
    return transactions.filter(t => t.transactionType?.toLowerCase() === 'cashout')
  }, [transactions])

  const totalExpense = useMemo(() => {
    return expenseTransactions.reduce((sum, t) => sum + Number(t.amount || 0), 0)
  }, [expenseTransactions])

  const categoryData = useMemo(() => {
    if (totalExpense === 0) return []

    const map = new Map<string, { label: string; amount: number; count: number; Icon: any }>()

    for (const t of expenseTransactions) {
      const cat = getTransactionCategory(t)
      const current = map.get(cat.label) ?? {
        label: cat.label,
        amount: 0,
        count: 0,
        Icon: cat.Icon
      }
      current.amount += Number(t.amount || 0)
      current.count += 1
      map.set(cat.label, current)
    }

    return Array.from(map.values())
      .map(item => ({
        ...item,
        percentage: Math.round((item.amount / totalExpense) * 100),
        color: CATEGORY_COLORS[item.label] ?? '#526b5c'
      }))
      .sort((a, b) => b.amount - a.amount)
  }, [expenseTransactions, totalExpense])

  if (!expenseTransactions.length) {
    return (
      <div className="category-breakdown-card empty-card">
        <div className="category-card-head">
          <div>
            <div className="eyebrow">SPENDING DISTRIBUTION</div>
            <h3>Category Breakdown</h3>
          </div>
          <span className="category-head-icon">
            <PieChart size={16} strokeWidth={1.8} />
          </span>
        </div>
        <p className="muted" style={{ fontSize: 12, margin: '14px 0 0' }}>
          No recorded expenses in this timeframe yet. Add transactions to see your categorical distribution.
        </p>
      </div>
    )
  }

  return (
    <div className="category-breakdown-card">
      <div className="category-card-head">
        <div>
          <div className="eyebrow">SPENDING DISTRIBUTION</div>
          <h3>Category Breakdown</h3>
          <p className="category-head-sub">
            Rs. {totalExpense.toLocaleString('en-PK')} across {expenseTransactions.length} expenses
          </p>
        </div>
        {onViewAllTransactions && (
          <button
            type="button"
            className="text-button"
            style={{ fontSize: 11 }}
            onClick={onViewAllTransactions}
          >
            Details <ArrowRight size={13} strokeWidth={1.8} />
          </button>
        )}
      </div>

      {/* Segmented Distribution Bar */}
      <div className="category-multi-bar" title="Category distribution proportion">
        {categoryData.map(cat => (
          <div
            key={cat.label}
            className="category-bar-segment"
            style={{
              width: `${Math.max(cat.percentage, 3)}%`,
              backgroundColor: cat.color
            }}
            title={`${cat.label}: ${cat.percentage}% (Rs. ${cat.amount.toLocaleString('en-PK')})`}
          />
        ))}
      </div>

      {/* Category List */}
      <div className="category-items-list">
        {categoryData.slice(0, 5).map(cat => {
          const Icon = cat.Icon
          return (
            <div
              key={cat.label}
              className={`category-item-row ${onSelectCategory ? 'interactive' : ''}`}
              onClick={() => onSelectCategory && onSelectCategory(cat.label)}
              title={onSelectCategory ? `Filter transactions by ${cat.label}` : undefined}
              role={onSelectCategory ? 'button' : undefined}
              tabIndex={onSelectCategory ? 0 : undefined}
            >
              <div className="category-item-left">
                <span
                  className="category-dot"
                  style={{ backgroundColor: cat.color }}
                />
                <span className="category-item-icon">
                  <Icon size={13} strokeWidth={2} color={cat.color} />
                </span>
                <span className="category-item-name">{cat.label}</span>
                <span className="category-item-count">{cat.count} txns</span>
              </div>

              <div className="category-item-right">
                <strong className="category-item-amount">
                  Rs. {cat.amount.toLocaleString('en-PK')}
                </strong>
                <span className="category-item-percent">{cat.percentage}%</span>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
