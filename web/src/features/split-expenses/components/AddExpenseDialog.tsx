import { useState, useMemo, useEffect } from 'react'
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
import { useSplitCalculator } from '../hooks/useSplitCalculator'
import { ChevronDown, ChevronUp, Sparkles } from 'lucide-react'

interface AddExpenseDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  groupId: string
  members: SplitGroupMemberDto[]
  currentUserId?: string
  token?: string
  onExpenseAdded: (expense: SplitExpenseDetailDto) => void
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

// Intelligent category keyword matcher (Splitwise style)
function guessCategory(title: string): string | null {
  const lower = title.toLowerCase()
  if (/(dinner|lunch|breakfast|food|burger|pizza|biryani|tea|chai|coffee|restaurant|cafe|snack|mcdonald|kfc)/i.test(lower)) {
    return 'Food & Dining'
  }
  if (/(uber|careem|taxi|fuel|petrol|diesel|toll|bus|train|flight|ticket|transport|parking)/i.test(lower)) {
    return 'Transport'
  }
  if (/(grocer|mart|supermarket|vegetable|fruit|milk|bread|meat|chicken|oil)/i.test(lower)) {
    return 'Groceries'
  }
  if (/(hotel|room|resort|lodge|airbnb|stay|rent)/i.test(lower)) {
    return 'Accommodation'
  }
  if (/(bill|electricity|gas|water|wifi|internet|utility)/i.test(lower)) {
    return 'Utilities'
  }
  if (/(movie|cinema|netflix|bowling|gaming|match|concert)/i.test(lower)) {
    return 'Entertainment'
  }
  return null
}

export function AddExpenseDialog({
  open,
  onOpenChange,
  groupId,
  members,
  currentUserId,
  token,
  onExpenseAdded,
}: AddExpenseDialogProps) {
  const [currentMembers, setCurrentMembers] = useState<SplitGroupMemberDto[]>(members)

  useEffect(() => {
    setCurrentMembers(members)
  }, [members])

  const activeMembers = useMemo(() => currentMembers.filter((m) => m.isActive), [currentMembers])

  const [title, setTitle] = useState('')
  const [amountStr, setAmountStr] = useState('')
  const [payerId, setPayerId] = useState<string>('')
  const [expenseDate, setExpenseDate] = useState<string>(
    new Date().toISOString().split('T')[0]
  )
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

  // Split calculation state machine
  const splitCalc = useSplitCalculator(activeMembers, amount, 'Equal')

  // Auto-category suggestion on title input
  const handleTitleChange = (val: string) => {
    setTitle(val)
    const suggested = guessCategory(val)
    if (suggested && suggested !== category) {
      setCategory(suggested)
    }
  }

  // Reset state on open
  useEffect(() => {
    if (open) {
      setTitle('')
      setAmountStr('')
      setNote('')
      setReceiptUrl('')
      setCategory('Food & Dining')
      setExpenseDate(new Date().toISOString().split('T')[0])
      setPayerId(currentUserId || (activeMembers.length > 0 ? activeMembers[0].userId : ''))
      setShowMoreOptions(false)
      splitCalc.resetToMembers(activeMembers)

      void splitExpensesApi
        .getGroupMembers(groupId, token)
        .then((fresh) => {
          if (fresh && fresh.length > 0) {
            setCurrentMembers(fresh)
            splitCalc.resetToMembers(fresh)
          }
        })
        .catch(() => {})
    }
  }, [open, currentUserId, groupId, token])

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
    try {
      setLoading(true)
      const splitsPayload = splitCalc.getSplitsPayload()

      const res = await splitExpensesApi.addExpense(
        groupId,
        {
          title: title.trim(),
          amount: amount,
          payerId: payerId || undefined,
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

      toast.success('Expense added successfully')
      onOpenChange(false)
      onExpenseAdded(res)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to add expense'
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

    // Payer-only safety check in equal mode
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
        title="Add an expense"
        description="Quickly record an expense and split it with group members."
      >
        <form onSubmit={handleSubmit} className="space-y-4 pt-1">
          {/* Title with live category icon hint */}
          <div className="space-y-1.5">
            <Label htmlFor="expense-title" className="text-xs font-semibold">
              Description *
            </Label>
            <div className="relative">
              <Input
                id="expense-title"
                placeholder="e.g. Dinner at Monal, Groceries, Petrol"
                value={title}
                onChange={(e) => handleTitleChange(e.target.value)}
                disabled={loading}
                required
                autoFocus
                className="pr-20"
              />
              {guessCategory(title) && (
                <span className="absolute right-2.5 top-1/2 -translate-y-1/2 flex items-center gap-1 text-[11px] font-medium text-primary/80 bg-primary/10 px-2 py-0.5 rounded-md">
                  <Sparkles className="w-3 h-3" />
                  {category}
                </span>
              )}
            </div>
          </div>

          {/* Amount: Large Splitwise Numeral Entry */}
          <div className="space-y-1.5">
            <Label htmlFor="expense-amount" className="text-xs font-semibold">
              Amount (Rs.) *
            </Label>
            <div className="relative flex items-center">
              <span className="absolute left-3.5 text-xl font-bold font-mono text-muted-foreground">
                Rs.
              </span>
              <Input
                id="expense-amount"
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

          {/* Natural Sentence Interactive Bar */}
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
                    <Label htmlFor="expense-date" className="text-xs">
                      Date
                    </Label>
                    <Input
                      id="expense-date"
                      type="date"
                      value={expenseDate}
                      onChange={(e) => setExpenseDate(e.target.value)}
                      disabled={loading}
                      className="text-xs"
                    />
                  </div>

                  <div className="space-y-1">
                    <Label htmlFor="expense-category" className="text-xs">
                      Category
                    </Label>
                    <Select value={category} onValueChange={(val) => val && setCategory(val)} disabled={loading}>
                      <SelectTrigger id="expense-category" className="text-xs">
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
                  <Label htmlFor="expense-note" className="text-xs">
                    Notes
                  </Label>
                  <Input
                    id="expense-note"
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
              {loading ? 'Saving...' : 'Save Expense'}
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
