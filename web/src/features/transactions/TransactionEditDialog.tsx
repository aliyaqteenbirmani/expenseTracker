import { useState, type FormEvent } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  Pencil,
  ArrowDownLeft,
  ArrowUpRight,
  Loader2,
  AlertCircle,
} from 'lucide-react'
import type { Transaction } from '@/App'

interface TransactionEditDialogProps {
  token: string
  transaction: Transaction
  onClose: () => void
  onSaved: () => Promise<void>
  api: <T>(path: string, token?: string, options?: RequestInit) => Promise<T>
}

export function TransactionEditDialog({
  token,
  transaction,
  onClose,
  onSaved,
  api,
}: TransactionEditDialogProps) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [txnType, setTxnType] = useState(transaction.transactionType || 'CashOut')
  const [amount, setAmount] = useState(String(transaction.amount || ''))
  const [remarks, setRemarks] = useState(transaction.remarks || '')

  const initialDate = transaction.transactionDate
    ? new Date(transaction.transactionDate)
    : new Date()
  const dateValue = `${initialDate.getFullYear()}-${String(initialDate.getMonth() + 1).padStart(2, '0')}-${String(initialDate.getDate()).padStart(2, '0')}`
  const [date, setDate] = useState(dateValue)
  const [file, setFile] = useState<File | null>(null)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')

    const body = new FormData()
    body.append('Id', transaction.id)
    body.append('TransactionDate', new Date(`${date}T12:00:00`).toISOString())
    body.append('Amount', String(amount))
    body.append('TransactionType', txnType)
    body.append('Remarks', remarks.trim())
    if (file && file.size > 0) {
      body.append('File', file)
    }

    try {
      await api('/api/Transaction/update', token, { method: 'PUT', body })
      await onSaved()
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not update this transaction.')
    } finally {
      setBusy(false)
    }
  }

  const isIncome = txnType?.toLowerCase() === 'cashin'

  return (
    <ResponsiveDialog
      open={true}
      onOpenChange={(open) => !open && !busy && onClose()}
      title={
        <div className="flex items-center gap-2">
          <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Pencil className="size-4" />
          </div>
          <div>
            <div className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              Edit Entry
            </div>
            <div className="text-base font-semibold">Update Transaction</div>
          </div>
        </div>
      }
      description="Modify details, remarks, amount or attached documentation."
    >
      <form onSubmit={submit} className="space-y-4 pt-2">
        <div className="space-y-1.5">
          <Label className="text-xs font-medium">Entry Direction</Label>
          <div className="grid grid-cols-2 gap-2">
            <Button
              type="button"
              variant={isIncome ? 'default' : 'outline'}
              className={`h-9 text-xs font-semibold gap-1.5 ${
                isIncome ? 'bg-inflow text-white hover:bg-inflow/90' : 'hover:text-inflow'
              }`}
              onClick={() => setTxnType('CashIn')}
            >
              <ArrowDownLeft className="size-4 stroke-[2.5]" />
              <span>+ Cash In</span>
            </Button>
            <Button
              type="button"
              variant={!isIncome ? 'default' : 'outline'}
              className={`h-9 text-xs font-semibold gap-1.5 ${
                !isIncome ? 'bg-outflow text-white hover:bg-outflow/90' : 'hover:text-outflow'
              }`}
              onClick={() => setTxnType('CashOut')}
            >
              <ArrowUpRight className="size-4 stroke-[2.5]" />
              <span>− Cash Out</span>
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1.5">
            <Label htmlFor="editAmount" className="text-xs font-medium">
              Amount (PKR)
            </Label>
            <div className="relative">
              <span className="absolute left-3 top-2.5 font-mono text-xs text-muted-foreground">
                ₨
              </span>
              <Input
                id="editAmount"
                type="number"
                inputMode="decimal"
                min="0.01"
                step="0.01"
                required
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="pl-8 font-mono text-sm font-semibold h-9"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="editDate" className="text-xs font-medium">
              Date
            </Label>
            <Input
              id="editDate"
              type="date"
              required
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="h-9 text-xs"
            />
          </div>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="editRemarks" className="text-xs font-medium">
            Notes / Remarks
          </Label>
          <Textarea
            id="editRemarks"
            value={remarks}
            onChange={(e) => setRemarks(e.target.value)}
            rows={2}
            className="text-xs"
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="editFile" className="text-xs font-medium">
            Replace Attachment <span className="text-muted-foreground font-normal">(optional)</span>
          </Label>
          <Input
            id="editFile"
            type="file"
            accept="image/*,application/pdf"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            className="text-xs file:mr-2 file:h-7 file:rounded-md file:border-0 file:bg-primary/10 file:px-2 file:text-xs file:font-semibold file:text-primary cursor-pointer"
          />
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
          <Button type="submit" disabled={busy || !amount} className="h-9 text-xs">
            {busy ? <Loader2 className="size-3.5 animate-spin mr-1.5" /> : null}
            <span>Save Changes</span>
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
