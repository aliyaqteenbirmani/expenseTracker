import { useState, useEffect, useMemo, useRef } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { splitExpensesApi } from '../splitExpensesApi'
import { toast } from 'sonner'
import type { SplitGroupMemberDto, SplitSettlementDto, SuggestedSettlementDto } from '../types'
import { ArrowRight, CheckCircle2, Loader2, Info, ArrowLeftRight, Sparkles } from 'lucide-react'
import { formatMoney } from '@/lib/format'
import { MemberSelector, getMemberDisplayName, normalizeId } from './MemberSelector'

interface SettleUpDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  groupId: string
  members: SplitGroupMemberDto[]
  currentUserId?: string
  isOwner?: boolean
  ownerId?: string
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
  isOwner,
  ownerId,
  initialSuggestion,
  token,
  onSettlementRecorded,
}: SettleUpDialogProps) {
  const [currentMembers, setCurrentMembers] = useState<SplitGroupMemberDto[]>(members)

  useEffect(() => {
    setCurrentMembers(members)
  }, [members])

  const activeMembers = useMemo(() => currentMembers.filter((m) => m.isActive), [currentMembers])

  const [fromUserId, setFromUserId] = useState<string>(
    initialSuggestion?.fromUserId || currentUserId || (activeMembers[0]?.userId ?? '')
  )
  const [toUserId, setToUserId] = useState<string>(
    initialSuggestion?.toUserId || (activeMembers.find((m) => !normalizeId(m.userId, initialSuggestion?.fromUserId || currentUserId))?.userId ?? '')
  )
  const [amountStr, setAmountStr] = useState<string>(
    initialSuggestion?.amount ? initialSuggestion.amount.toString() : ''
  )
  const [note, setNote] = useState<string>('')
  const [loading, setLoading] = useState<boolean>(false)
  const [confirmed, setConfirmed] = useState<boolean>(false)

  // Track previous open state and suggestion so form initialization ONLY runs when dialog opens
  // or when initialSuggestion changes, NEVER resetting confirmed or inputs on re-renders.
  const prevOpenRef = useRef(false)
  const prevSuggestionRef = useRef(initialSuggestion)

  useEffect(() => {
    const wasOpened = !prevOpenRef.current && open
    const suggestionChanged = prevSuggestionRef.current !== initialSuggestion
    prevOpenRef.current = open
    prevSuggestionRef.current = initialSuggestion

    if (!open) {
      setConfirmed(false)
      return
    }

    if (wasOpened || suggestionChanged) {
      if (initialSuggestion) {
        setFromUserId(initialSuggestion.fromUserId)
        setToUserId(initialSuggestion.toUserId)
        setAmountStr(initialSuggestion.amount ? initialSuggestion.amount.toString() : '')
      } else {
        const defaultFrom = currentUserId || (activeMembers[0]?.userId ?? '')
        setFromUserId(defaultFrom)
        const other = activeMembers.find((m) => !normalizeId(m.userId, defaultFrom))
        setToUserId(other?.userId ?? '')
        setAmountStr('')
      }
      setNote('')
      setConfirmed(false)

      // Fetch fresh member list to guarantee latest active members
      if (token && groupId) {
        splitExpensesApi
          .getGroupMembers(groupId, token)
          .then((freshMembers) => {
            if (Array.isArray(freshMembers) && freshMembers.length > 0) {
              setCurrentMembers(freshMembers)
            }
          })
          .catch(() => {
            // fallback to props
          })
      }
    }
  }, [open, initialSuggestion, currentUserId, activeMembers, token, groupId])

  const amount = parseFloat(amountStr) || 0

  const fromMember = activeMembers.find((m) => normalizeId(m.userId, fromUserId))
  const toMember = activeMembers.find((m) => normalizeId(m.userId, toUserId))

  // Determine if settlement will be confirmed immediately:
  // Owner recording on behalf of others, or owner is the receiver ("X paid me")
  const effectiveOwner = isOwner || Boolean(ownerId && normalizeId(currentUserId, ownerId))
  const isImmediate = Boolean(
    effectiveOwner && (!normalizeId(fromUserId, currentUserId) || normalizeId(toUserId, ownerId))
  )

  const suggestedBalance = useMemo(() => {
    if (initialSuggestion?.amount && initialSuggestion.amount > 0) {
      return initialSuggestion.amount
    }
    const fromMemberBal = fromMember?.balance ?? 0
    if (fromMemberBal < -0.005) {
      return Math.abs(fromMemberBal)
    }
    const toMemberBal = toMember?.balance ?? 0
    if (toMemberBal > 0.005) {
      return toMemberBal
    }
    return 0
  }, [initialSuggestion, fromMember, toMember])

  const handleSwap = () => {
    const prevFrom = fromUserId
    const prevTo = toUserId
    setFromUserId(prevTo)
    setToUserId(prevFrom)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!fromUserId || !toUserId) {
      toast.error('Select both sender and receiver')
      return
    }

    if (normalizeId(fromUserId, toUserId)) {
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
      
      if (res.status === 'Confirmed') {
        toast.success('Settlement payment recorded and confirmed successfully')
      } else {
        toast.success('Settlement request submitted. Awaiting confirmation.')
      }

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
            <div className="rounded-lg border border-primary/20 bg-primary/5 p-3 text-xs text-muted-foreground flex gap-2.5 items-start">
              <Info className="size-4 text-primary shrink-0 mt-0.5" />
              <div>
                {isImmediate ? (
                  <p>
                    As the group owner, recording this payment will confirm the settlement and update group balances immediately.
                  </p>
                ) : (
                  <p>
                    Submitting this payment sends a settlement request. It will be confirmed by{' '}
                    <strong className="text-foreground">{getMemberDisplayName(toMember) || 'the receiver'}</strong>
                    {' '}(and approved by the group owner if required) before balances are adjusted.
                  </p>
                )}
              </div>
            </div>

            <div className="relative grid grid-cols-1 md:grid-cols-2 gap-3 items-end">
              <div className="space-y-1.5">
                <Label htmlFor="settle-from-user">Payer (Who paid?) *</Label>
                <MemberSelector
                  id="settle-from-user"
                  value={fromUserId}
                  onValueChange={(val) => {
                    setFromUserId(val)
                    if (normalizeId(toUserId, val)) {
                      const next = activeMembers.find((m) => !normalizeId(m.userId, val))
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

              {/* Direction Swap Button */}
              <div className="hidden md:flex absolute left-1/2 -translate-x-1/2 bottom-1 z-10">
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  onClick={handleSwap}
                  className="size-8 rounded-full bg-background shadow-xs border-border/80 hover:bg-muted/60 active:scale-95 transition-transform"
                  title="Swap payer and receiver"
                  aria-label="Swap payer and receiver"
                >
                  <ArrowLeftRight className="size-3.5" />
                </Button>
              </div>

              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <Label htmlFor="settle-to-user">Receiver (Who received?) *</Label>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={handleSwap}
                    className="md:hidden h-5 px-1.5 text-[11px] text-muted-foreground gap-1"
                  >
                    <ArrowLeftRight className="size-3" />
                    Swap
                  </Button>
                </div>
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

            {/* Large Numeral Amount Input with 1-Click Balance Pill */}
            <div className="space-y-1.5">
              <Label htmlFor="settle-amount">Amount (Rs.) *</Label>
              <div className="relative flex items-center">
                <span className="absolute left-3.5 text-xl font-bold font-mono text-muted-foreground">
                  Rs.
                </span>
                <Input
                  id="settle-amount"
                  type="number"
                  step="0.01"
                  min="0.01"
                  placeholder="0.00"
                  value={amountStr}
                  onChange={(e) => setAmountStr(e.target.value)}
                  disabled={loading}
                  className="pl-14 text-2xl sm:text-3xl font-extrabold font-mono tabular-nums tracking-tight h-13"
                  required
                />
              </div>

              {/* 1-Click Fill Balance Pill */}
              {suggestedBalance > 0.005 && (
                <div className="flex items-center justify-end pt-1">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setAmountStr(suggestedBalance.toFixed(2))}
                    className="h-6 text-[11px] font-mono px-2 rounded-full border-primary/30 text-primary bg-primary/5 hover:bg-primary/10 gap-1"
                  >
                    <Sparkles className="size-3" />
                    Set to full balance: {formatMoney(suggestedBalance)}
                  </Button>
                </div>
              )}
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
              <Button type="submit" disabled={amount <= 0 || normalizeId(fromUserId, toUserId)}>
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
                <h4 className="font-semibold text-base">
                  {isImmediate ? 'Confirm Payment Recording' : 'Review Settlement Request'}
                </h4>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {isImmediate
                    ? 'This transaction will update the balance for both members immediately.'
                    : `Your request will be sent to ${getMemberDisplayName(toMember) || 'the receiver'} for confirmation before balances change.`}
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
              <Button type="submit" disabled={loading} className="gap-1.5">
                {loading ? (
                  <>
                    <Loader2 className="size-3.5 animate-spin" />
                    Submitting...
                  </>
                ) : isImmediate ? (
                  'Confirm & Record'
                ) : (
                  'Submit Settlement Request'
                )}
              </Button>
            </div>
          </div>
        )}
      </form>
    </ResponsiveDialog>
  )
}
