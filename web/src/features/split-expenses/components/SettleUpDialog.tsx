import { useState, useEffect } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { splitExpensesApi } from '../splitExpensesApi'
import { toast } from 'sonner'
import type { SplitGroupMemberDto, SplitSettlementDto, SuggestedSettlementDto } from '../types'
import { ArrowRight, CheckCircle2 } from 'lucide-react'
import { MemberSelector, getMemberDisplayName } from './MemberSelector'

interface SettleUpDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  groupId: string
  members: SplitGroupMemberDto[]
  currentUserId?: string
  initialSuggestion?: SuggestedSettlementDto | null
  token?: string
  onSettlementRecorded: (settlement: SplitSettlementDto) => void
}

export function SettleUpDialog({
  open,
  onOpenChange,
  groupId,
  members,
  currentUserId,
  initialSuggestion,
  token,
  onSettlementRecorded,
}: SettleUpDialogProps) {
  const activeMembers = members.filter((m) => m.isActive)

  const [fromUserId, setFromUserId] = useState<string>(
    initialSuggestion?.fromUserId || currentUserId || (activeMembers[0]?.userId ?? '')
  )
  const [toUserId, setToUserId] = useState<string>(
    initialSuggestion?.toUserId || (activeMembers.find((m) => m.userId !== (initialSuggestion?.fromUserId || currentUserId))?.userId ?? '')
  )
  const [amountStr, setAmountStr] = useState<string>(
    initialSuggestion?.amount ? initialSuggestion.amount.toString() : ''
  )
  const [note, setNote] = useState<string>('')
  const [loading, setLoading] = useState<boolean>(false)
  const [confirmed, setConfirmed] = useState<boolean>(false)

  // Sync with initialSuggestion or defaults when dialog opens or suggestion changes
  useEffect(() => {
    if (open) {
      if (initialSuggestion) {
        setFromUserId(initialSuggestion.fromUserId)
        setToUserId(initialSuggestion.toUserId)
        setAmountStr(initialSuggestion.amount ? initialSuggestion.amount.toString() : '')
      } else {
        const defaultFrom = currentUserId || (activeMembers[0]?.userId ?? '')
        setFromUserId((prev) => prev || defaultFrom)
        setToUserId((prev) => {
          if (prev && prev !== defaultFrom) return prev
          const other = activeMembers.find((m) => m.userId !== defaultFrom)
          return other?.userId ?? ''
        })
      }
      setConfirmed(false)
    }
  }, [open, initialSuggestion, currentUserId, activeMembers])

  const amount = parseFloat(amountStr) || 0

  const fromMember = activeMembers.find((m) => m.userId === fromUserId)
  const toMember = activeMembers.find((m) => m.userId === toUserId)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!fromUserId || !toUserId) {
      toast.error('Select both sender and receiver')
      return
    }

    if (fromUserId === toUserId) {
      toast.error('Sender and receiver must be different members')
      return
    }

    if (amount <= 0) {
      toast.error('Amount must be greater than zero')
      return
    }

    // Two-step confirmation for financial settlements
    if (!confirmed) {
      setConfirmed(true)
      return
    }

    try {
      setLoading(true)
      const res = await splitExpensesApi.recordSettlement(
        groupId,
        {
          fromUserId,
          toUserId,
          amount,
          note: note.trim() || undefined,
        },
        token
      )
      toast.success('Settlement payment recorded successfully')
      setConfirmed(false)
      setAmountStr('')
      setNote('')
      onOpenChange(false)
      onSettlementRecorded(res)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to record settlement'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={(v) => {
        if (!v) setConfirmed(false)
        onOpenChange(v)
      }}
      title="Record Settlement"
      description="Record a cash or digital payment made to settle group debt."
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {!confirmed ? (
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="settle-from-user">Payer (Who paid?) *</Label>
                <MemberSelector
                  id="settle-from-user"
                  value={fromUserId}
                  onValueChange={(val) => {
                    setFromUserId(val)
                    if (toUserId === val) {
                      const next = activeMembers.find((m) => m.userId !== val)
                      setToUserId(next?.userId ?? '')
                    }
                  }}
                  members={activeMembers}
                  currentUserId={currentUserId}
                  placeholder="Select payer"
                  disabled={loading}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="settle-to-user">Receiver (Who received?) *</Label>
                <MemberSelector
                  id="settle-to-user"
                  value={toUserId}
                  onValueChange={setToUserId}
                  members={activeMembers}
                  currentUserId={currentUserId}
                  excludeUserIds={fromUserId ? [fromUserId] : []}
                  placeholder="Select receiver"
                  disabled={loading}
                  required
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="settle-amount">Amount (Rs.) *</Label>
              <Input
                id="settle-amount"
                type="number"
                step="0.01"
                min="0.01"
                placeholder="0.00"
                value={amountStr}
                onChange={(e) => setAmountStr(e.target.value)}
                disabled={loading}
                className="font-mono text-base"
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="settle-note">Note / Payment method (Optional)</Label>
              <Input
                id="settle-note"
                placeholder="e.g. Paid via Easypaisa, JazzCash, Cash"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                disabled={loading}
                maxLength={500}
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
              <Button type="submit" disabled={amount <= 0 || fromUserId === toUserId}>
                Review & Confirm
              </Button>
            </div>
          </>
        ) : (
          /* Confirmation Screen */
          <div className="space-y-4">
            <div className="rounded-xl border bg-muted/30 p-4 text-center space-y-3">
              <div className="inline-flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary">
                <CheckCircle2 className="size-6" />
              </div>
              <div>
                <h4 className="font-semibold text-base">Confirm Payment Recording</h4>
                <p className="text-xs text-muted-foreground mt-0.5">
                  This transaction will update the net balance for both members immediately.
                </p>
              </div>

              <div className="flex items-center justify-center gap-2 py-2 text-sm font-medium">
                <span className="font-semibold">{getMemberDisplayName(fromMember) || 'Sender'}</span>
                <ArrowRight className="size-4 text-muted-foreground" />
                <span className="font-semibold">{getMemberDisplayName(toMember) || 'Receiver'}</span>
              </div>

              <div className="text-2xl font-bold font-mono text-foreground">
                Rs. {amount.toLocaleString('en-PK', { minimumFractionDigits: 2 })}
              </div>

              {note && <div className="text-xs text-muted-foreground italic">"{note}"</div>}
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setConfirmed(false)}
                disabled={loading}
              >
                Back
              </Button>
              <Button type="submit" loading={loading} className="gap-1.5">
                Confirm & Record
              </Button>
            </div>
          </div>
        )}
      </form>
    </ResponsiveDialog>
  )
}
