import { useState, useMemo, useEffect, useRef } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
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
import type { SplitGroupMemberDto, SplitExpenseDetailDto } from '../types'
import { getMemberDisplayName } from './MemberSelector'
import { SplitSentenceBar } from './SplitSentenceBar'
import { SplitMethodModal } from './SplitMethodModal'
import { PayerSelectorModal } from './PayerSelectorModal'
import { useSplitCalculator, type SplitMethod } from '../hooks/useSplitCalculator'
import { ChevronDown, ChevronUp } from 'lucide-react'

interface EditExpenseDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  groupId: string
  expense: SplitExpenseDetailDto | null
  members: SplitGroupMemberDto[]
  currentUserId?: string
  token?: string
  onExpenseUpdated: (updatedExpense: SplitExpenseDetailDto) => void
}

const CATEGORIES = [
  'Food & Dining',
  'Groceries',
  'Transport',
  'Accommodation',
  'Utilities',
  'Entertainment',
  'Shopping',
  'Services',
  'Other',
]

export function EditExpenseDialog({
  open,
  onOpenChange,
  groupId,
  expense,
  members,
  currentUserId,
  token,
  onExpenseUpdated,
}: EditExpenseDialogProps) {
  const activeMembers = useMemo(() => members.filter((m) => m.isActive), [members])

  const [title, setTitle] = useState('')
  const [amountStr, setAmountStr] = useState('')
  const [payerId, setPayerId] = useState<string>('')
  const [expenseDate, setExpenseDate] = useState<string>('')
  const [category, setCategory] = useState<string>('Food & Dining')
  const [note, setNote] = useState('')
  const [receiptUrl, setReceiptUrl] = useState('')
  const [loading, setLoading] = useState(false)
  const [showMoreOptions, setShowMoreOptions] = useState(false)

  // Modals state
  const [payerModalOpen, setPayerModalOpen] = useState(false)
  const [splitMethodModalOpen, setSplitMethodModalOpen] = useState(false)
  const [payerOnlyConfirmOpen, setPayerOnlyConfirmOpen] = useState(false)

  const amount = parseFloat(amountStr) || 0

  const initialMethod: SplitMethod = useMemo(() => {
    const raw = (expense?.splitType || 'Equal').toLowerCase()
    if (raw.includes('exact')) return 'Exact'
    if (raw.includes('percent')) return 'Percentage'
    if (raw.includes('share')) return 'Shares'
    return 'Equal'
  }, [expense?.splitType])

  const splitCalc = useSplitCalculator(activeMembers, amount, initialMethod)

  const prevOpenRef = useRef(false)
  const prevExpenseIdRef = useRef<string | null>(null)

  useEffect(() => {
    const wasOpened = !prevOpenRef.current && open
    const expenseChanged = prevExpenseIdRef.current !== expense?.id
    prevOpenRef.current = open
    prevExpenseIdRef.current = expense?.id ?? null

    if (open && expense && (wasOpened || expenseChanged)) {
      setTitle(expense.title || '')
      setAmountStr(expense.amount ? expense.amount.toString() : '')
      setPayerId(expense.payerId || '')
      setExpenseDate(
        expense.expenseDate
          ? new Date(expense.expenseDate).toISOString().split('T')[0]
          : new Date().toISOString().split('T')[0]
      )
      setCategory(expense.category || 'Food & Dining')
      setNote(expense.note || '')
      setReceiptUrl(expense.receiptUrl || '')

      const shareUserIds = (expense.shares || []).map((s) => s.userId)
      if (shareUserIds.length > 0) {
        splitCalc.setEqualSelectedIds(shareUserIds)
      } else {
        splitCalc.setEqualSelectedIds(activeMembers.map((m) => m.userId))
      }

      // If exact amounts were recorded
      if (initialMethod === 'Exact' && expense.shares) {
        const map: Record<string, string> = {}
        for (const s of expense.shares) {
          map[s.userId] = s.shareAmount.toString()
        }
        for (const m of activeMembers) {
          if (!map[m.userId]) map[m.userId] = '0'
        }
        splitCalc.setExactValues(map)
      }
    }
  }, [open, expense, activeMembers, initialMethod])

  const payerName = useMemo(() => {
    const payer = activeMembers.find((m) => m.userId === payerId)
    if (!payer) return 'You'
    return payer.userId === currentUserId ? 'You' : getMemberDisplayName(payer)
  }, [activeMembers, payerId, currentUserId])

  const splitMethodDisplayLabel = useMemo(() => {
    switch (splitCalc.method) {
      case 'Exact':
        return 'by exact amounts (123)'
      case 'Percentage':
        return 'by percentages (%)'
      case 'Shares':
        return 'by shares (👥)'
      case 'Equal':
      default: {
        const count = splitCalc.equalSelectedIds.length
        return count === activeMembers.length ? 'equally (=)' : `unequally (${count} people)`
      }
    }
  }, [splitCalc.method, splitCalc.equalSelectedIds, activeMembers.length])

  const executeSubmit = async () => {
    if (!expense) return

    try {
      setLoading(true)
      const splitsPayload = splitCalc.getSplitsPayload()

      const res = await splitExpensesApi.updateExpense(
        groupId,
        expense.id,
        {
          title: title.trim(),
          amount: amount,
          payerId: payerId,
          expenseDate: expenseDate ? new Date(expenseDate).toISOString() : undefined,
          category: category || undefined,
          note: note.trim() || undefined,
          receiptUrl: receiptUrl.trim() || undefined,
          splitType: splitsPayload.splitType,
          participantSplits: splitsPayload.participantSplits,
          participantUserIds: splitsPayload.participantUserIds,
        },
        token
      )

      toast.success('Expense updated successfully')
      onOpenChange(false)
      onExpenseUpdated(res)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to update expense'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    if (!title.trim()) {
      toast.error('Expense title is required')
      return
    }

    if (amount <= 0) {
      toast.error('Amount must be greater than zero')
      return
    }

    if (!splitCalc.validation.isValid) {
      toast.error(splitCalc.validation.message || 'Please check split allocations')
      return
    }

    if (!payerId) {
      toast.error('Please select who paid for the expense')
      return
    }

    if (
      splitCalc.method === 'Equal' &&
      activeMembers.length > 1 &&
      splitCalc.equalSelectedIds.length === 1 &&
      splitCalc.equalSelectedIds[0] === payerId
    ) {
      setPayerOnlyConfirmOpen(true)
      return
    }

    executeSubmit()
  }

  return (
    <>
      <ResponsiveDialog
        open={open}
        onOpenChange={onOpenChange}
        title="Edit expense"
        description="Update the details and split division for this group expense."
      >
        <form onSubmit={handleSubmit} className="space-y-4 pt-1">
          {/* Description */}
          <div className="space-y-1.5">
            <Label htmlFor="edit-expense-title" className="text-xs font-semibold">
              Description *
            </Label>
            <Input
              id="edit-expense-title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              disabled={loading}
              required
            />
          </div>

          {/* Amount: Large Splitwise Numeral Entry */}
          <div className="space-y-1.5">
            <Label htmlFor="edit-expense-amount" className="text-xs font-semibold">
              Amount (Rs.) *
            </Label>
            <div className="relative flex items-center">
              <span className="absolute left-3.5 text-xl font-bold font-mono text-muted-foreground">
                Rs.
              </span>
              <Input
                id="edit-expense-amount"
                type="number"
                step="0.01"
                min="0.01"
                placeholder="0.00"
                value={amountStr}
                onChange={(e) => setAmountStr(e.target.value)}
                disabled={loading}
                required
                className="pl-14 text-2xl sm:text-3xl font-extrabold font-mono tabular-nums tracking-tight h-13"
              />
            </div>
          </div>

          {/* Interactive Sentence Bar */}
          <SplitSentenceBar
            payerName={payerName}
            splitMethodLabel={splitMethodDisplayLabel}
            onOpenPayerSelect={() => setPayerModalOpen(true)}
            onOpenSplitMethod={() => setSplitMethodModalOpen(true)}
            disabled={loading}
          />

          {/* Live Calculation Feedback Pill */}
          <div className="text-center">
            <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground bg-muted/30 px-3 py-1 rounded-full border border-border/50">
              {splitCalc.validation.message || `Split across ${activeMembers.length} active members`}
            </span>
          </div>

          {/* Collapsible More Options: Date, Category, Notes */}
          <div className="pt-1 border-t border-border/50">
            <button
              type="button"
              onClick={() => setShowMoreOptions(!showMoreOptions)}
              className="flex items-center justify-between w-full py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors"
            >
              <span>More details (Date, Category, Notes)</span>
              {showMoreOptions ? (
                <ChevronUp className="w-3.5 h-3.5" />
              ) : (
                <ChevronDown className="w-3.5 h-3.5" />
              )}
            </button>

            {showMoreOptions && (
              <div className="space-y-3 pt-2 text-left">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <Label htmlFor="edit-expense-date" className="text-xs">
                      Date
                    </Label>
                    <Input
                      id="edit-expense-date"
                      type="date"
                      value={expenseDate}
                      onChange={(e) => setExpenseDate(e.target.value)}
                      disabled={loading}
                      className="text-xs"
                    />
                  </div>

                  <div className="space-y-1">
                    <Label htmlFor="edit-expense-category" className="text-xs">
                      Category
                    </Label>
                    <Select value={category} onValueChange={(val) => val && setCategory(val)} disabled={loading}>
                      <SelectTrigger id="edit-expense-category" className="text-xs">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {CATEGORIES.map((c) => (
                          <SelectItem key={c} value={c} className="text-xs">
                            {c}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="space-y-1">
                  <Label htmlFor="edit-expense-note" className="text-xs">
                    Notes
                  </Label>
                  <Input
                    id="edit-expense-note"
                    placeholder="Optional details or items"
                    value={note}
                    onChange={(e) => setNote(e.target.value)}
                    disabled={loading}
                    className="text-xs"
                  />
                </div>
              </div>
            )}
          </div>

          {/* Submit Actions */}
          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/60">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={loading}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              size="sm"
              disabled={loading || amount <= 0 || !splitCalc.validation.isValid}
              className="font-semibold px-5"
            >
              {loading ? 'Saving Changes...' : 'Save Changes'}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Payer Selector Modal */}
      <PayerSelectorModal
        open={payerModalOpen}
        onOpenChange={setPayerModalOpen}
        members={activeMembers}
        selectedPayerId={payerId}
        currentUserId={currentUserId}
        onSelectPayer={setPayerId}
      />

      {/* Split Method Modal */}
      <SplitMethodModal
        open={splitMethodModalOpen}
        onOpenChange={setSplitMethodModalOpen}
        members={activeMembers}
        totalAmount={amount}
        method={splitCalc.method}
        setMethod={splitCalc.setMethod}
        equalSelectedIds={splitCalc.equalSelectedIds}
        toggleEqualParticipant={splitCalc.toggleEqualParticipant}
        selectAllEqual={splitCalc.selectAllEqual}
        deselectAllEqual={splitCalc.deselectAllEqual}
        exactValues={splitCalc.exactValues}
        setExactValue={splitCalc.setExactValue}
        percentageValues={splitCalc.percentageValues}
        setPercentageValue={splitCalc.setPercentageValue}
        shareParts={splitCalc.shareParts}
        incrementSharePart={splitCalc.incrementSharePart}
        decrementSharePart={splitCalc.decrementSharePart}
        calculatedShares={splitCalc.calculatedShares}
        validation={splitCalc.validation}
      />

      {/* Payer Only Confirmation Dialog */}
      <AlertDialog open={payerOnlyConfirmOpen} onOpenChange={setPayerOnlyConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Split with yourself only?</AlertDialogTitle>
            <AlertDialogDescription>
              You are recording this expense for yourself only. No other members will owe anything.
              Do you want to proceed?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={() => executeSubmit()}>Proceed</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
