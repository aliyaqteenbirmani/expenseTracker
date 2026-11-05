import { useState, type FormEvent } from 'react'
import { Check, Sparkles, Target, X } from 'lucide-react'

type Props = {
  currentBudget: number
  suggestedBudget: number
  cashbookName: string
  onSave: (amount: number) => void
  onClose: () => void
}

const PRESET_AMOUNTS = [25000, 50000, 75000, 100000, 150000, 250000]

export default function BudgetConfigDialog({
  currentBudget,
  suggestedBudget,
  cashbookName,
  onSave,
  onClose
}: Props) {
  const [amountStr, setAmountStr] = useState(currentBudget > 0 ? String(currentBudget) : '')
  const [error, setError] = useState('')

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const val = Number(amountStr.replace(/[^0-9.]/g, ''))
    if (isNaN(val) || val <= 0) {
      setError('Please enter a valid monthly budget amount (greater than 0).')
      return
    }
    onSave(val)
    onClose()
  }

  function handlePreset(val: number) {
    setAmountStr(String(val))
    setError('')
  }

  return (
    <div className="overlay" onClick={onClose} role="dialog" aria-modal="true">
      <div className="dialog budget-dialog" onClick={e => e.stopPropagation()}>
        <header>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <span className="budget-dialog-icon">
              <Target size={18} strokeWidth={2} />
            </span>
            <div>
              <div className="eyebrow">SPENDING TARGETS</div>
              <h2>Set Monthly Budget</h2>
            </div>
          </div>
          <button className="close" onClick={onClose} aria-label="Close dialog">
            <X size={18} strokeWidth={1.8} />
          </button>
        </header>

        <form onSubmit={handleSubmit} className="dialog-content budget-dialog-content">
          <p className="muted" style={{ fontSize: 12, lineHeight: 1.6, margin: '0 0 16px' }}>
            Set a maximum monthly spending goal for &ldquo;<strong>{cashbookName}</strong>&rdquo;. Spendwise will monitor your real-time pace and safe daily allowance throughout the month.
          </p>

          {error && (
            <div className="error-banner" style={{ margin: '0 0 14px' }}>
              {error}
            </div>
          )}

          <label className="budget-input-label">
            <span>MONTHLY SPENDING LIMIT (PKR)</span>
            <div className="budget-input-wrap">
              <span className="budget-input-currency">Rs.</span>
              <input
                type="number"
                min="1000"
                step="500"
                placeholder="e.g. 50,000"
                value={amountStr}
                onChange={e => {
                  setAmountStr(e.target.value)
                  setError('')
                }}
                autoFocus
              />
            </div>
          </label>

          {suggestedBudget > 0 && (
            <div
              className="budget-suggestion-box"
              onClick={() => handlePreset(suggestedBudget)}
              title="Click to apply suggested budget"
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                <Sparkles size={14} color="#18835c" />
                <span style={{ fontSize: 11, fontWeight: 700, color: '#1a5438' }}>
                  Smart Recommendation:
                </span>
              </div>
              <span style={{ fontSize: 11, color: '#385845' }}>
                Rs. {suggestedBudget.toLocaleString('en-PK')} / month
              </span>
            </div>
          )}

          <div className="budget-presets-wrapper">
            <small className="budget-presets-label">QUICK AMOUNTS</small>
            <div className="budget-preset-chips">
              {PRESET_AMOUNTS.map(preset => {
                const isSelected = Number(amountStr) === preset
                return (
                  <button
                    key={preset}
                    type="button"
                    className={`budget-chip ${isSelected ? 'active' : ''}`}
                    onClick={() => handlePreset(preset)}
                  >
                    {isSelected && <Check size={11} strokeWidth={2.5} />}
                    Rs. {preset >= 100000 ? `${preset / 1000}k` : preset.toLocaleString('en-PK')}
                  </button>
                )
              })}
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 22, paddingTop: 14, borderTop: '1px solid #edf2ee' }}>
            <button type="button" className="button secondary-action" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="button primary">
              Save Monthly Budget
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
