import { useState, useEffect, type FormEvent } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  ArrowDownLeft,
  ArrowUpRight,
  Plus,
  Loader2,
  AlertCircle,
} from 'lucide-react'
import type { Cashbook } from '@/App'

interface ManualTransactionDialogProps {
  token: string
  book: Cashbook
  initialType?: 'cashin' | 'cashout'
  onClose: () => void
  onDone: () => Promise<void>
  api: <T>(path: string, token?: string, options?: RequestInit) => Promise<T>
}

export function ManualTransactionDialog({
  token,
  book,
  initialType = 'cashout',
  onClose,
  onDone,
  api,
}: ManualTransactionDialogProps) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [txnType, setTxnType] = useState<'CashIn' | 'CashOut'>(
    initialType === 'cashin' ? 'CashIn' : 'CashOut'
  )
  const [amount, setAmount] = useState('')
  const [remarks, setRemarks] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [filePreview, setFilePreview] = useState<string | null>(null)

  const today = new Date()
  const localToday = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`
  const [date, setDate] = useState(localToday)

  useEffect(() => {
    if (file && file.type.startsWith('image/')) {
      const url = URL.createObjectURL(file)
      setFilePreview(url)
      return () => URL.revokeObjectURL(url)
    }
    setFilePreview(null)
  }, [file])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')

    const form = new FormData()
    form.append('CashBookId', book.id)
    form.append('Amount', String(amount))
    form.append('TransactionType', txnType)
    if (date) form.append('TransactionDate', new Date(`${date}T12:00:00`).toISOString())
    form.append('Remarks', remarks.trim())
    if (file && file.size > 0) form.append('File', file)

    try {
      await api('/api/Transaction/AddCashTransaction', token, {
        method: 'POST',
        body: form,
      })
      await onDone()
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save transaction.')
    } finally {
      setBusy(false)
    }
  }

  const isIncome = txnType === 'CashIn'

  return (
    <ResponsiveDialog
      open={true}
      onOpenChange={(open) => !open && !busy && onClose()}
      title={
        <div className="flex items-center gap-2">
          <div
            className={`flex size-8 items-center justify-center rounded-lg border ${
              isIncome
                ? 'border-inflow/30 bg-inflow-muted text-inflow'
                : 'border-outflow/30 bg-outflow-muted text-outflow'
            }`}
          >
            {isIncome ? <ArrowDownLeft className="size-4 stroke-[2.5]" /> : <ArrowUpRight className="size-4 stroke-[2.5]" />}
          </div>
          <div>
            <div className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              {book.cashBookName ?? book.name ?? 'Cashbook'}
            </div>
            <div className="text-base font-semibold">Record Transaction</div>
          </div>
        </div>
      }
      description="Record income or expenditure into your active financial ledger."
    >
      <form onSubmit={submit} className="space-y-4 pt-2">
        {/* Type Toggle */}
        <div className="space-y-1.5">
          <Label className="text-xs font-medium">Entry Direction</Label>
          <div className="grid grid-cols-2 gap-2">
            <Button
              type="button"
              variant={txnType === 'CashIn' ? 'default' : 'outline'}
              className={`h-9 text-xs font-semibold gap-1.5 ${
                txnType === 'CashIn'
                  ? 'bg-inflow text-white hover:bg-inflow/90'
                  : 'hover:text-inflow'
              }`}
              onClick={() => setTxnType('CashIn')}
            >
              <ArrowDownLeft className="size-4 stroke-[2.5]" />
              <span>+ Cash In (Income)</span>
            </Button>
            <Button
              type="button"
              variant={txnType === 'CashOut' ? 'default' : 'outline'}
              className={`h-9 text-xs font-semibold gap-1.5 ${
                txnType === 'CashOut'
                  ? 'bg-outflow text-white hover:bg-outflow/90'
                  : 'hover:text-outflow'
              }`}
              onClick={() => setTxnType('CashOut')}
            >
              <ArrowUpRight className="size-4 stroke-[2.5]" />
              <span>− Cash Out (Expense)</span>
            </Button>
          </div>
        </div>

        {/* Amount & Date in 2 columns */}
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1.5">
            <Label htmlFor="txnAmount" className="text-xs font-medium">
              Amount (PKR)
            </Label>
            <div className="relative">
              <span className="absolute left-3 top-2.5 font-mono text-xs text-muted-foreground">
                ₨
              </span>
              <Input
                id="txnAmount"
                type="number"
                inputMode="decimal"
                min="0.01"
                step="0.01"
                required
                placeholder="0.00"
                autoFocus
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="pl-8 font-mono text-sm font-semibold h-9"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="txnDate" className="text-xs font-medium">
              Date
            </Label>
            <Input
              id="txnDate"
              type="date"
              required
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="h-9 text-xs"
            />
          </div>
        </div>

        {/* Remarks */}
        <div className="space-y-1.5">
          <Label htmlFor="txnRemarks" className="text-xs font-medium">
            Notes / Description
          </Label>
          <Textarea
            id="txnRemarks"
            placeholder="e.g. Office supplies, Client consulting fee, Monthly rent"
            value={remarks}
            onChange={(e) => setRemarks(e.target.value)}
            rows={2}
            className="text-xs"
          />
        </div>

        {/* Receipt Attachment */}
        <div className="space-y-1.5">
          <Label htmlFor="txnFile" className="text-xs font-medium">
            Attach Receipt / Invoice <span className="text-muted-foreground font-normal">(optional)</span>
          </Label>
          <Input
            id="txnFile"
            type="file"
            accept="image/*,application/pdf"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            className="text-xs file:mr-2 file:h-7 file:rounded-md file:border-0 file:bg-primary/10 file:px-2 file:text-xs file:font-semibold file:text-primary cursor-pointer"
          />
          {filePreview && (
            <div className="mt-2 relative size-16 rounded-lg overflow-hidden border">
              <img src={filePreview} alt="Receipt preview" className="size-full object-cover" />
            </div>
          )}
        </div>

        {error && (
          <Alert variant="destructive" className="py-2 text-xs">
            <AlertCircle className="size-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <div className="flex justify-end gap-2 pt-2 border-t">
          <Button type="button" variant="ghost" onClick={onClose} disabled={busy} className="h-9 text-xs">
            Cancel
          </Button>
          <Button type="submit" disabled={busy || !amount} className="h-9 text-xs gap-1.5">
            {busy ? <Loader2 className="size-3.5 animate-spin" /> : <Plus className="size-3.5" />}
            <span>{busy ? 'Recording…' : 'Record Entry'}</span>
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
