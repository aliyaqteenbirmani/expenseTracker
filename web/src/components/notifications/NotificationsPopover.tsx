import { useState, useEffect, useCallback } from 'react'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  Bell,
  Check,
  X,
  Mail,
  Loader2,
  Inbox,
} from 'lucide-react'
import { formatRelativeDate } from '@/lib/format'
import { EmptyState } from '@/components/app/EmptyState'
import { motion, AnimatePresence } from 'motion/react'
import { spring } from '@/lib/motion'
import { AnimatedNumber } from '@/components/motion/animated-number'

export type Invitation = {
  id: string
  invitationType: string
  businessId?: string
  businessName: string
  cashbookId?: string
  cashbookName?: string
  invitedEmail: string
  invitedByName: string
  status: string
  createdOn: string
  remarks?: string
}

interface NotificationsPopoverProps {
  token: string
  unreadCount?: number
  onInvitationProcessed?: (target?: { businessId?: string; cashbookId?: string }) => void
  onToast: (msg: string) => void
  refreshTrigger?: number
}

export function NotificationsPopover({
  token,
  onInvitationProcessed,
  onToast,
  refreshTrigger = 0,
}: NotificationsPopoverProps) {
  const [open, setOpen] = useState(false)
  const [invitations, setInvitations] = useState<Invitation[]>([])
  const [loading, setLoading] = useState(false)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [error, setError] = useState('')

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  const loadPending = useCallback(async () => {
    if (!token) return
    setLoading(true)
    setError('')
    try {
      const response = await fetch(`${apiBase}/api/Invitations/mypendingInvitations`, {
        headers: { Authorization: `Bearer ${token}` },
      })
      const data = await response.json().catch(() => ({}))
      if (response.ok) {
        setInvitations(data?.data ?? data?.Data ?? data ?? [])
      } else {
        setError(data?.message || 'Could not load notifications.')
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error checking notifications.')
    } finally {
      setLoading(false)
    }
  }, [apiBase, token])

  useEffect(() => {
    void loadPending()
  }, [loadPending, refreshTrigger])

  async function handleAction(id: string, action: 'accept' | 'reject') {
    setBusyId(id)
    setError('')
    try {
      const response = await fetch(`${apiBase}/api/Invitations/${id}/${action}`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        ...(action === 'reject' ? { body: JSON.stringify({ remarks: '' }) } : {}),
      })
      const result = await response.json().catch(() => ({}))
      if (!response.ok) {
        throw new Error(result?.message || `Failed to ${action} invitation.`)
      }

      onToast(action === 'accept' ? 'Invitation accepted! Workspace added.' : 'Invitation declined.')
      const acceptedItem = invitations.find((i) => i.id === id)
      onInvitationProcessed?.(
        acceptedItem
          ? { businessId: acceptedItem.businessId, cashbookId: acceptedItem.cashbookId }
          : undefined
      )
      await loadPending()
    } catch (e) {
      setError(e instanceof Error ? e.message : `Could not ${action} invitation.`)
    } finally {
      setBusyId(null)
    }
  }

  const count = invitations.length

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger
        render={(props) => (
          <Button
            {...props}
            variant="ghost"
            size="icon"
            className="relative size-9 cursor-pointer"
            aria-label="Notifications"
          >
            <motion.div
              animate={count > 0 ? { rotate: [0, -12, 10, -6, 3, 0] } : {}}
              transition={{ duration: 0.5, ease: 'easeOut' }}
            >
              <Bell className="size-4" />
            </motion.div>
            <AnimatePresence>
              {count > 0 && (
                <motion.span
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  exit={{ scale: 0 }}
                  transition={spring.bouncy}
                  className="absolute -top-0.5 -right-0.5 flex size-4 items-center justify-center rounded-full bg-primary text-[10px] font-semibold text-primary-foreground tabular-nums shadow-xs"
                >
                  <AnimatedNumber value={count} />
                </motion.span>
              )}
            </AnimatePresence>
          </Button>
        )}
      />
      <PopoverContent
        align="end"
        sideOffset={8}
        className="w-80 md:w-96 p-0 rounded-xl shadow-lg border-border"
      >
        <div className="flex items-center justify-between border-b px-4 py-3">
          <div className="flex items-center gap-2">
            <h4 className="text-sm font-semibold tracking-tight">Notifications</h4>
            {count > 0 && (
              <Badge variant="secondary" className="px-1.5 py-0 text-[11px] font-mono">
                {count} new
              </Badge>
            )}
          </div>
          {count > 0 && (
            <Button
              variant="ghost"
              size="sm"
              className="text-xs h-7 px-2 text-muted-foreground hover:text-foreground"
              onClick={() => void loadPending()}
            >
              Refresh
            </Button>
          )}
        </div>

        {error && (
          <div className="p-3 text-xs text-destructive bg-destructive/10 border-b">
            {error}
          </div>
        )}

        <ScrollArea className="max-h-80">
          {loading ? (
            <div className="flex flex-col items-center justify-center py-10 text-muted-foreground gap-2">
              <Loader2 className="size-5 animate-spin" />
              <span className="text-xs">Checking notifications...</span>
            </div>
          ) : invitations.length === 0 ? (
            <div className="py-8 px-4 text-center">
              <EmptyState
                icon={Inbox}
                title="All caught up"
                description="You have no pending workspace or cashbook invitations."
                className="border-0 p-0"
              />
            </div>
          ) : (
            <div className="divide-y divide-border/60">
              {invitations.map((inv) => (
                <div key={inv.id} className="p-3.5 space-y-2 hover:bg-muted/40 transition-colors">
                  <div className="flex items-start justify-between gap-2">
                    <div className="space-y-0.5">
                      <div className="flex items-center gap-1.5">
                        <Mail className="size-3.5 text-primary shrink-0" />
                        <span className="text-xs font-semibold text-foreground">
                          {inv.businessName}
                        </span>
                      </div>
                      {inv.cashbookName && (
                        <p className="text-[11px] text-muted-foreground">
                          Cashbook: <strong className="text-foreground">{inv.cashbookName}</strong>
                        </p>
                      )}
                      <p className="text-[11px] text-muted-foreground">
                        Invited by <span className="font-medium">{inv.invitedByName}</span> ·{' '}
                        {formatRelativeDate(inv.createdOn)}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center justify-end gap-2 pt-1">
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-7 px-2.5 text-xs text-destructive hover:bg-destructive/10 hover:text-destructive"
                      disabled={busyId === inv.id}
                      onClick={() => handleAction(inv.id, 'reject')}
                    >
                      <X className="size-3 mr-1" />
                      Decline
                    </Button>
                    <Button
                      size="sm"
                      className="h-7 px-2.5 text-xs"
                      disabled={busyId === inv.id}
                      onClick={() => handleAction(inv.id, 'accept')}
                    >
                      {busyId === inv.id ? (
                        <Loader2 className="size-3 animate-spin mr-1" />
                      ) : (
                        <Check className="size-3 mr-1" />
                      )}
                      Accept
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </ScrollArea>
      </PopoverContent>
    </Popover>
  )
}
