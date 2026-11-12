import { useState, useMemo, useEffect } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { splitExpensesApi } from '../splitExpensesApi'
import { toast } from 'sonner'
import type { SplitGroupMemberDto, SplitExpenseDetailDto } from '../types'
import { Users } from 'lucide-react'
import { MemberSelector, getMemberDisplayName, getMemberInitials } from './MemberSelector'

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

export function AddExpenseDialog({
  open,
  onOpenChange,
  groupId,
  members,
  currentUserId,
  token,
  onExpenseAdded,
}: AddExpenseDialogProps) {
  const activeMembers = useMemo(() => members.filter((m) => m.isActive), [members])

  const [title, setTitle] = useState('')
  const [amountStr, setAmountStr] = useState('')
  const [payerId, setPayerId] = useState<string>(
    currentUserId || (activeMembers.length > 0 ? activeMembers[0].userId : '')
  )
  const [expenseDate, setExpenseDate] = useState<string>(
    new Date().toISOString().split('T')[0]
  )
  const [category, setCategory] = useState<string>('Food & Dining')
  const [note, setNote] = useState('')
  const [receiptUrl, setReceiptUrl] = useState('')
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>(
    activeMembers.map((m) => m.userId)
  )
  const [loading, setLoading] = useState(false)

  // Sync state when dialog opens or active members change
  useEffect(() => {
    if (open) {
      if (!payerId && activeMembers.length > 0) {
        setPayerId(currentUserId || activeMembers[0].userId)
      }
      if (selectedUserIds.length === 0 && activeMembers.length > 0) {
        setSelectedUserIds(activeMembers.map((m) => m.userId))
      }
    }
  }, [open, activeMembers, currentUserId, payerId, selectedUserIds.length])

  // When dialog opens or members update, initialize selected user IDs
  const handleSelectAll = () => {
    if (selectedUserIds.length === activeMembers.length) {
      setSelectedUserIds([])
    } else {
      setSelectedUserIds(activeMembers.map((m) => m.userId))
    }
  }

  const toggleParticipant = (userId: string) => {
    setSelectedUserIds((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    )
  }

  const amount = parseFloat(amountStr) || 0

  // Calculate dynamic split preview exactly matching backend deterministic penny distribution
  const splitShares = useMemo(() => {
    if (amount <= 0 || selectedUserIds.length === 0) return {}
    const count = selectedUserIds.length
    const totalCents = Math.round(amount * 100)
    const baseCents = Math.floor(totalCents / count)
    let remainder = totalCents % count

    const sortedIds = [...selectedUserIds].sort((a, b) => a.localeCompare(b))
    const result: Record<string, number> = {}

    for (const id of sortedIds) {
      let cents = baseCents
      if (remainder > 0) {
        cents += 1
        remainder -= 1
      }
      result[id] = cents / 100
    }
    return result
  }, [amount, selectedUserIds])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!title.trim()) {
      toast.error('Expense title is required')
      return
    }

    if (amount <= 0) {
      toast.error('Amount must be greater than zero')
      return
    }

    if (selectedUserIds.length === 0) {
      toast.error('Select at least one member to split with')
      return
    }

    try {
      setLoading(true)
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
          participantUserIds: selectedUserIds,
        },
        token
      )
      toast.success('Expense added successfully')
      setTitle('')
      setAmountStr('')
      setNote('')
      setReceiptUrl('')
      onOpenChange(false)
      onExpenseAdded(res)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to add expense'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  const selectedCount = selectedUserIds.length
  const sampleShare = selectedCount > 0 ? (amount / selectedCount).toFixed(2) : '0.00'

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add Expense"
      description="Record a group expense and split it equally among selected members."
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-1.5 md:col-span-2">
            <Label htmlFor="expense-title">Title *</Label>
            <Input
              id="expense-title"
              placeholder="e.g. Dinner at Monal, Groceries, Toll Plaza"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              disabled={loading}
              required
              autoFocus
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="expense-amount">Amount (Rs.) *</Label>
            <div className="relative">
              <Input
                id="expense-amount"
                type="number"
                step="0.01"
                min="0.01"
                placeholder="0.00"
                value={amountStr}
                onChange={(e) => setAmountStr(e.target.value)}
                disabled={loading}
                className="font-mono"
                required
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="expense-payer">Paid by *</Label>
            <MemberSelector
              id="expense-payer"
              value={payerId}
              onValueChange={setPayerId}
              members={activeMembers}
              currentUserId={currentUserId}
              placeholder="Select payer"
              disabled={loading}
              required
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="expense-date">Date</Label>
            <div className="relative">
              <Input
                id="expense-date"
                type="date"
                value={expenseDate}
                onChange={(e) => setExpenseDate(e.target.value)}
                disabled={loading}
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>Category</Label>
            <Select value={category} onValueChange={(val) => setCategory(val ?? 'Food & Dining')} disabled={loading}>
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Category" />
              </SelectTrigger>
              <SelectContent>
                {CATEGORIES.map((cat) => (
                  <SelectItem key={cat} value={cat}>
                    {cat}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Dynamic Split Preview Banner */}
        <div className="rounded-lg border bg-muted/40 p-3 text-xs space-y-1">
          <div className="flex items-center justify-between font-medium text-foreground">
            <span className="flex items-center gap-1.5">
              <Users className="size-3.5 text-primary" />
              Split Breakdown (Equal)
            </span>
            <button
              type="button"
              onClick={handleSelectAll}
              className="text-xs text-primary hover:underline font-normal"
            >
              {selectedCount === activeMembers.length ? 'Deselect all' : 'Select all'}
            </button>
          </div>
          <div className="text-muted-foreground flex flex-wrap justify-between gap-1 pt-1">
            <span>
              Total: <strong>Rs. {amount > 0 ? amount.toLocaleString('en-PK', { minimumFractionDigits: 2 }) : '0.00'}</strong>
            </span>
            <span>
              Selected: <strong>{selectedCount} members</strong>
            </span>
            <span>
              Each member: <strong>Rs. {sampleShare}</strong>
            </span>
          </div>
        </div>

        {/* Member Checkboxes with Dynamic Share Amount */}
        <div className="space-y-2">
          <Label className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
            Split Among ({selectedCount}/{activeMembers.length})
          </Label>
          <div className="max-h-40 overflow-y-auto space-y-1.5 rounded-lg border p-2">
            {activeMembers.map((m) => {
              const isChecked = selectedUserIds.includes(m.userId)
              const share = splitShares[m.userId]
              const name = getMemberDisplayName(m)
              const initials = getMemberInitials(name || m.email)
              const isYou = m.userId === currentUserId
              return (
                <div
                  key={m.userId}
                  className={`flex items-center justify-between p-2 rounded-md transition-colors ${
                    isChecked ? 'bg-primary/5' : 'hover:bg-muted/40'
                  }`}
                >
                  <label
                    htmlFor={`user-${m.userId}`}
                    className="flex items-center gap-2.5 cursor-pointer select-none text-sm flex-1 truncate min-w-0"
                  >
                    <Checkbox
                      id={`user-${m.userId}`}
                      checked={isChecked}
                      onCheckedChange={() => toggleParticipant(m.userId)}
                      disabled={loading}
                    />
                    <div className="flex size-6 shrink-0 items-center justify-center rounded-full bg-primary/10 text-[10px] font-semibold text-primary">
                      {initials}
                    </div>
                    <div className="flex flex-col truncate min-w-0">
                      <span className="truncate text-foreground font-medium text-xs">
                        {name}
                        {isYou && <span className="text-muted-foreground font-normal ml-1">(You)</span>}
                      </span>
                      {m.email && m.email !== name && (
                        <span className="text-[11px] text-muted-foreground truncate">{m.email}</span>
                      )}
                    </div>
                  </label>
                  {isChecked && share !== undefined && (
                    <span className="text-xs font-mono font-medium text-muted-foreground ml-2 shrink-0">
                      Rs. {share.toFixed(2)}
                    </span>
                  )}
                </div>
              )
            })}
          </div>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="expense-note">Note (Optional)</Label>
          <Input
            id="expense-note"
            placeholder="Add any extra details or receipt link"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            disabled={loading}
            maxLength={1000}
          />
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={loading}
          >
            Cancel
          </Button>
          <Button type="submit" loading={loading} disabled={selectedCount === 0 || amount <= 0}>
            Save Expense
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
