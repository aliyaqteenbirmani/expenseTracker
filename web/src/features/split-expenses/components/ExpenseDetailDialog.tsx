import { useState } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
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
import { splitExpensesApi } from '../splitExpensesApi'
import { toast } from 'sonner'
import type { SplitExpenseDetailDto } from '../types'
import { Tag, Trash2, Pencil } from 'lucide-react'
import { format } from 'date-fns'
import { getMemberInitials } from './MemberSelector'

interface ExpenseDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  expense: SplitExpenseDetailDto | null
  currentUserId?: string
  isGroupOwner?: boolean
  token?: string
  onExpenseDeleted: (expenseId: string) => void
  onEditExpense?: (expense: SplitExpenseDetailDto) => void
}

export function ExpenseDetailDialog({
  open,
  onOpenChange,
  expense,
  currentUserId,
  isGroupOwner,
  token,
  onExpenseDeleted,
  onEditExpense,
}: ExpenseDetailDialogProps) {
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)
  const [deleting, setDeleting] = useState(false)

  if (!expense) return null

  const canModify = isGroupOwner || expense.createdBy === currentUserId || expense.payerId === currentUserId

  const handleDelete = async () => {
    try {
      setDeleting(true)
      await splitExpensesApi.deleteExpense(expense.groupId, expense.id, token)
      toast.success('Expense deleted successfully')
      setDeleteConfirmOpen(false)
      onOpenChange(false)
      onExpenseDeleted(expense.id)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to delete expense'
      toast.error(msg)
    } finally {
      setDeleting(false)
    }
  }

  const formattedDate = expense.expenseDate
    ? format(new Date(expense.expenseDate), 'MMM d, yyyy')
    : 'No date'

  const myShare = expense.shares?.find((s) => s.userId === currentUserId)
  const isIncluded = Boolean(myShare)

  return (
    <>
      <ResponsiveDialog
        open={open}
        onOpenChange={onOpenChange}
        title={expense.title}
        description={`Recorded by ${expense.createdByName} · ${formattedDate}`}
      >
        <div className="space-y-4">
          {/* Amount and Meta Summary */}
          <div className="rounded-xl border bg-muted/30 p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <span className="text-xs text-muted-foreground uppercase tracking-wider font-medium">
                Total Amount
              </span>
              <div className="text-2xl font-bold font-mono tracking-tight text-foreground">
                Rs. {expense.amount.toLocaleString('en-PK', { minimumFractionDigits: 2 })}
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {expense.category && (
                <Badge variant="secondary" className="gap-1 font-normal text-xs">
                  <Tag className="size-3" />
                  {expense.category}
                </Badge>
              )}
              <Badge variant="outline" className="text-xs">
                {expense.splitType || 'Equal'} Split
              </Badge>
            </div>
          </div>

          {/* Paid by info */}
          <div className="flex items-center gap-3 p-3 rounded-lg border bg-card text-sm">
            <div className="flex size-9 items-center justify-center rounded-full bg-primary/10 text-primary font-semibold text-xs">
              {getMemberInitials(expense.payerName || expense.payerEmail)}
            </div>
            <div className="flex-1 truncate">
              <span className="text-muted-foreground text-xs block">Paid by</span>
              <span className="font-semibold text-foreground truncate">
                {expense.payerName || expense.payerEmail || 'Member'} {expense.payerId === currentUserId && '(You)'}
              </span>
            </div>
            <span className="font-mono text-sm font-semibold text-inflow">
              +Rs. {expense.amount.toFixed(2)}
            </span>
          </div>

          {/* Current User Share Highlight */}
          {currentUserId && (
            <div
              className={`rounded-lg border p-2.5 text-xs flex items-center justify-between ${
                isIncluded
                  ? 'border-primary/20 bg-primary/5 text-foreground'
                  : 'border-border/60 bg-muted/30 text-muted-foreground'
              }`}
            >
              <span className="font-medium">Your share in this expense</span>
              {isIncluded && myShare ? (
                <span className="font-mono font-semibold text-primary">
                  Rs. {myShare.shareAmount.toFixed(2)}
                </span>
              ) : (
                <span className="font-mono font-medium text-muted-foreground flex items-center gap-1.5">
                  Rs. 0.00
                  <span className="text-[10px] bg-muted px-1.5 py-0.5 rounded font-sans border text-muted-foreground">
                    Not included
                  </span>
                </span>
              )}
            </div>
          )}

          {/* Shares Breakdown */}
          <div className="space-y-2">
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Participants & Shares ({expense.shares?.length || 0})
            </h4>
            <div className="max-h-52 overflow-y-auto rounded-lg border divide-y text-sm">
              {expense.shares?.map((share) => (
                <div key={share.userId} className="flex items-center justify-between p-2.5">
                  <div className="flex items-center gap-2.5 truncate">
                    <div className="flex size-7 items-center justify-center rounded-full bg-muted text-xs font-medium">
                      {getMemberInitials(share.userName || share.userEmail)}
                    </div>
                    <div className="truncate">
                      <div className="font-medium truncate">
                        {share.userName || share.userEmail || 'Member'} {share.userId === currentUserId && '(You)'}
                      </div>
                      <div className="text-xs text-muted-foreground truncate">{share.userEmail}</div>
                    </div>
                  </div>
                  <div className="text-right font-mono text-sm font-medium">
                    Rs. {share.shareAmount.toFixed(2)}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {expense.note && (
            <div className="text-xs text-muted-foreground p-3 rounded-lg bg-muted/40">
              <span className="font-semibold block text-foreground mb-0.5">Note:</span>
              {expense.note}
            </div>
          )}

          {/* Dialog Action Buttons */}
          <div className="flex items-center justify-between pt-2">
            <div className="flex items-center gap-2">
              {canModify && (
                <>
                  {onEditExpense && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      onClick={() => {
                        onOpenChange(false)
                        onEditExpense(expense)
                      }}
                    >
                      <Pencil className="size-3.5" />
                      Edit Expense
                    </Button>
                  )}
                  <Button
                    variant="destructive"
                    size="sm"
                    className="gap-1.5"
                    onClick={() => setDeleteConfirmOpen(true)}
                  >
                    <Trash2 className="size-3.5" />
                    Delete Expense
                  </Button>
                </>
              )}
            </div>
            <Button variant="outline" size="sm" onClick={() => onOpenChange(false)}>
              Close
            </Button>
          </div>
        </div>
      </ResponsiveDialog>

      {/* Delete Confirmation Alert */}
      <AlertDialog open={deleteConfirmOpen} onOpenChange={setDeleteConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this expense?</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{expense.title}" (Rs.{' '}
              {expense.amount.toFixed(2)})? This will reverse all member shares and update the group balance.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleting}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              disabled={deleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {deleting ? 'Deleting...' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
