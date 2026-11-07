import { useState, type FormEvent } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Pencil, Trash2, Loader2, AlertCircle } from 'lucide-react'
import type { Cashbook } from '@/App'

interface ManageCashbookDialogProps {
  token: string
  book: Cashbook
  mode: 'edit' | 'delete'
  onClose: () => void
  onSaved: (deleted: boolean) => Promise<void>
  api: <T>(path: string, token?: string, options?: RequestInit) => Promise<T>
}

export function ManageCashbookDialog({
  token,
  book,
  mode,
  onClose,
  onSaved,
  api,
}: ManageCashbookDialogProps) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [name, setName] = useState(book.cashBookName ?? book.name ?? '')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      if (mode === 'edit') {
        await api(`/api/CashBook/${encodeURIComponent(book.id)}`, token, {
          method: 'PUT',
          body: JSON.stringify({ cashBook: name.trim() }),
        })
      } else {
        await api(`/api/CashBook/${encodeURIComponent(book.id)}`, token, {
          method: 'DELETE',
        })
      }
      await onSaved(mode === 'delete')
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : `Could not ${mode} the cashbook.`)
    } finally {
      setBusy(false)
    }
  }

  if (mode === 'delete') {
    return (
      <AlertDialog open={true} onOpenChange={(open) => !open && !busy && onClose()}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2 text-destructive">
              <Trash2 className="size-4" />
              <span>Delete Cashbook?</span>
            </AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to permanently delete{' '}
              <strong>&ldquo;{book.cashBookName ?? book.name ?? 'This cashbook'}&rdquo;</strong>?
              All associated transactions, attachments, and permissions will be permanently removed.
            </AlertDialogDescription>
          </AlertDialogHeader>

          {error && (
            <Alert variant="destructive" className="py-2 text-xs">
              <AlertCircle className="size-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <AlertDialogFooter>
            <AlertDialogCancel disabled={busy}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={busy}
              onClick={(e) => {
                e.preventDefault()
                const dummyForm = document.createElement('form')
                submit({ preventDefault: () => {}, currentTarget: dummyForm } as any)
              }}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {busy ? <Loader2 className="size-3.5 animate-spin mr-1.5" /> : null}
              <span>Delete Permanently</span>
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    )
  }

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
              Cashbook Settings
            </div>
            <div className="text-base font-semibold">Rename Cashbook</div>
          </div>
        </div>
      }
      description={`Update the display title for "${book.cashBookName ?? book.name}".`}
    >
      <form onSubmit={submit} className="space-y-4 pt-2">
        <div className="space-y-1.5">
          <Label htmlFor="editCbName" className="text-xs font-medium">
            Cashbook Name
          </Label>
          <Input
            id="editCbName"
            value={name}
            onChange={(e) => setName(e.target.value)}
            autoFocus
            maxLength={200}
            required
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
          <Button type="button" variant="ghost" onClick={onClose} disabled={busy} className="h-9 text-xs">
            Cancel
          </Button>
          <Button type="submit" disabled={busy || !name.trim()} className="h-9 text-xs">
            {busy ? <Loader2 className="size-3.5 animate-spin mr-1.5" /> : null}
            <span>Save Changes</span>
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
