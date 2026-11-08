import { useState, type FormEvent } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Target, Sparkles, Check } from 'lucide-react'

interface BudgetConfigDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  currentBudget: number
  suggestedBudget: number
  cashbookName: string
  onSave: (amount: number) => void
}

const PRESET_AMOUNTS = [25000, 50000, 75000, 100000, 150000, 250000]

export function BudgetConfigDialog({
  open,
  onOpenChange,
  currentBudget,
  suggestedBudget,
  cashbookName,
  onSave,
}: BudgetConfigDialogProps) {
  const [amountStr, setAmountStr] = useState(
    currentBudget > 0 ? String(currentBudget) : ''
  )
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const val = Number(amountStr.replace(/[^0-9.]/g, ''))
    if (isNaN(val) || val <= 0) {
      setError('Please enter a valid monthly budget amount (greater than 0).')
      return
    }
    setSaved(true)
    setTimeout(() => {
      onSave(val)
      onOpenChange(false)
      setSaved(false)
    }, 700)
  }

  function handlePreset(val: number) {
    setAmountStr(String(val))
    setError('')
  }

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title={
        <div className="flex items-center gap-2">
          <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Target className="size-4" />
          </div>
          <div>
            <div className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              Spending Targets
            </div>
            <div className="text-base font-semibold">Set Monthly Budget</div>
          </div>
        </div>
      }
      description={`Set a monthly spending limit for "${cashbookName}". Spendwise will track pace and calculate safe daily allowances.`}
    >
      <form onSubmit={handleSubmit} className="space-y-4 pt-2">
        <div className="space-y-2">
          <Label htmlFor="budgetAmount">Monthly Budget Amount (PKR)</Label>
          <div className="relative">
            <span className="absolute left-3 top-2.5 font-mono text-xs text-muted-foreground">
              ₨
            </span>
            <Input
              id="budgetAmount"
              type="text"
              inputMode="numeric"
              placeholder="e.g. 50,000"
              value={amountStr}
              onChange={(e) => {
                setAmountStr(e.target.value)
                setError('')
              }}
              className="pl-8 font-mono"
              autoFocus
            />
          </div>
          {error && <p className="text-xs text-destructive">{error}</p>}
        </div>

        {/* Suggested amount */}
        {suggestedBudget > 0 && (
          <div className="flex items-center justify-between rounded-lg border border-border/80 bg-muted/40 p-3">
            <div className="flex items-center gap-2">
              <Sparkles className="size-4 text-primary" />
              <div>
                <p className="text-xs font-medium">Suggested Budget</p>
                <p className="text-[11px] text-muted-foreground">
                  Based on recent spending + 30% cushion
                </p>
              </div>
            </div>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="text-xs h-7"
              onClick={() => handlePreset(suggestedBudget)}
            >
              Use ₨{suggestedBudget.toLocaleString()}
            </Button>
          </div>
        )}

        {/* Quick Presets */}
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">Quick Presets</Label>
          <div className="flex flex-wrap gap-1.5">
            {PRESET_AMOUNTS.map((val) => {
              const active = Number(amountStr) === val
              return (
                <Button
                  key={val}
                  type="button"
                  variant={active ? 'default' : 'outline'}
                  size="sm"
                  className="h-7 text-xs font-mono"
                  onClick={() => handlePreset(val)}
                >
                  {active && <Check className="size-3 mr-1" />}
                  ₨{(val / 1000).toFixed(0)}k
                </Button>
              )
            })}
          </div>
        </div>

        <div className="flex justify-end gap-2 pt-2 border-t">
          <Button type="button" variant="ghost" onClick={() => onOpenChange(false)} disabled={saved}>
            Cancel
          </Button>
          <Button type="submit" success={saved} successLabel="Saved">Save Budget</Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
