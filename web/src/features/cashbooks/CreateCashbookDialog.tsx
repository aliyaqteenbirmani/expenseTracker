import { useState, type FormEvent } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { BookOpen, Plus, AlertCircle } from 'lucide-react'

interface CreateCashbookDialogProps {
  token: string
  businessId: string
  onClose: () => void
  onCreated: (name: string) => Promise<void>
  api: <T>(path: string, token?: string, options?: RequestInit) => Promise<T>
}

export function CreateCashbookDialog({
  token,
  businessId,
  onClose,
  onCreated,
  api,
}: CreateCashbookDialogProps) {
  const [busy, setBusy] = useState(false)
  const [created, setCreated] = useState(false)
  const [error, setError] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    const name = String(new FormData(event.currentTarget).get('cashBookName')).trim()
    try {
      await api('/api/CashBook', token, {
        method: 'POST',
        body: JSON.stringify({ cashBookName: name, businessId }),
      })
      await onCreated(name)
      setCreated(true)
      setTimeout(() => {
        onClose()
      }, 700)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not create cashbook.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <ResponsiveDialog
      open={true}
      onOpenChange={(open) => !open && !busy && onClose()}
      title={
        <div className="flex items-center gap-2">
          <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <BookOpen className="size-4" />
          </div>
          <div>
            <div className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              New Ledger
            </div>
            <div className="text-base font-semibold">Create a Cashbook</div>
          </div>
        </div>
      }
      description="Create a dedicated cashbook to track income, expenses, and team collaborator permissions."
    >
      <form onSubmit={submit} className="space-y-4 pt-2">
        <div className="space-y-1.5">
          <Label htmlFor="cbName" className="text-xs font-medium">
            Cashbook Name
          </Label>
          <Input
            id="cbName"
            name="cashBookName"
            autoFocus
            maxLength={200}
            required
            placeholder="e.g. Daily Operations, Office Petty Cash"
            className="h-9 text-xs"
          />
        </div>

        {error && (
          <Alert variant="destructive" className="py-2 text-xs">
            <AlertCircle className="size-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <div className="flex justify-end gap-2 pt-2 border-t">
          <Button type="button" variant="ghost" onClick={onClose} disabled={busy || created} className="h-9 text-xs">
            Cancel
          </Button>
          <Button type="submit" disabled={busy} loading={busy} success={created} successLabel="Created!" className="h-9 text-xs gap-1.5">
            <Plus className="size-3.5" />
            <span>Create Cashbook</span>
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
