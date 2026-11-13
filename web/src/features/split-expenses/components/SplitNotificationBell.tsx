import { useState, useEffect, useCallback } from 'react'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Textarea } from '@/components/ui/textarea'
import {
  Bell,
  Check,
  X,
  Users,
  CheckCircle2,
  XCircle,
  Loader2,
  Inbox,
  Receipt,
  Pencil,
  Trash2,
  HandCoins,
  ShieldCheck,
  RotateCcw,
  Clock,
  Info,
} from 'lucide-react'
import { splitExpensesApi } from '../splitExpensesApi'
import { realtimeService } from '@/realtimeService'
import { toast } from 'sonner'
import { formatRelativeDate } from '@/lib/format'
import type { SplitNotificationDto } from '../types'
import { cn } from '@/lib/utils'

interface SplitNotificationBellProps {
  token?: string
  onInvitationProcessed?: () => void
  onSettlementProcessed?: () => void
  onNotificationClick?: (notification: SplitNotificationDto) => void
  className?: string
}

export function SplitNotificationBell({
  token,
  onInvitationProcessed,
  onSettlementProcessed,
  onNotificationClick,
  className,
}: SplitNotificationBellProps) {
  const [open, setOpen] = useState(false)
  const [notifications, setNotifications] = useState<SplitNotificationDto[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const [actionInProgressId, setActionInProgressId] = useState<string | null>(null)

  // Decline dialog state
  const [declineTarget, setDeclineTarget] = useState<SplitNotificationDto | null>(null)
  const [declineReason, setDeclineReason] = useState('')
  const [isDeclining, setIsDeclining] = useState(false)

  const fetchUnreadCount = useCallback(async () => {
    if (!token) return
    try {
      const res = await splitExpensesApi.getNotificationUnreadCount(token)
      setUnreadCount(res.unreadCount)
    } catch {
      // Non-fatal
    }
  }, [token])

  const fetchNotifications = useCallback(async () => {
    if (!token) return
    try {
      setLoading(true)
      const res = await splitExpensesApi.getMyNotifications(1, 20, token)
      setNotifications(res.items || [])
      setUnreadCount(res.unreadCount || 0)
    } catch (err: unknown) {
      console.warn('Failed to load notifications', err)
    } finally {
      setLoading(false)
    }
  }, [token])

  // Initial load & polling fallback (45s) & window focus
  useEffect(() => {
    void fetchUnreadCount()

    const interval = setInterval(() => {
      void fetchUnreadCount()
    }, 45000)

    const onFocus = () => {
      void fetchUnreadCount()
      if (open) void fetchNotifications()
    }
    window.addEventListener('focus', onFocus)

    // SignalR realtime event listener
    const unsubscribe = realtimeService.subscribeSplitNotification(() => {
      void fetchUnreadCount()
      if (open) void fetchNotifications()
    })

    return () => {
      clearInterval(interval)
      window.removeEventListener('focus', onFocus)
      unsubscribe()
    }
  }, [fetchUnreadCount, fetchNotifications, open])

  // Load full list when popover opens
  useEffect(() => {
    if (open) {
      void fetchNotifications()
    }
  }, [open, fetchNotifications])

  const handleAcceptInvitation = async (invitationId: string, notificationId: string) => {
    try {
      setActionInProgressId(notificationId)
      await splitExpensesApi.acceptInvitation(invitationId, token)
      toast.success('Joined split group successfully!')
      setNotifications((prev) =>
        prev.map((n) => (n.id === notificationId ? { ...n, isRead: true } : n))
      )
      setUnreadCount((c) => Math.max(0, c - 1))
      onInvitationProcessed?.()
      void fetchNotifications()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to accept invitation'
      toast.error(msg)
    } finally {
      setActionInProgressId(null)
    }
  }

  const handleDeclineInvitation = async (invitationId: string, notificationId: string) => {
    try {
      setActionInProgressId(notificationId)
      await splitExpensesApi.rejectInvitation(invitationId, token)
      toast.info('Invitation declined.')
      setNotifications((prev) =>
        prev.map((n) => (n.id === notificationId ? { ...n, isRead: true } : n))
      )
      setUnreadCount((c) => Math.max(0, c - 1))
      onInvitationProcessed?.()
      void fetchNotifications()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to decline invitation'
      toast.error(msg)
    } finally {
      setActionInProgressId(null)
    }
  }

  const handleAcceptSettlement = async (notif: SplitNotificationDto) => {
    if (!notif.groupId || !notif.referenceId) return
    try {
      setActionInProgressId(notif.id)
      if (notif.type === 'SettlementAwaitingOwner') {
        await splitExpensesApi.approveSettlementByOwner(notif.groupId, notif.referenceId, token)
        toast.success('Settlement approved and confirmed!')
      } else {
        try {
          await splitExpensesApi.confirmSettlement(notif.groupId, notif.referenceId, token)
          toast.success('Payment receipt confirmed!')
        } catch (err: any) {
          // If error indicates only owner can approve (e.g. reminder for owner), try owner approval
          if (err?.message?.toLowerCase().includes('owner')) {
            await splitExpensesApi.approveSettlementByOwner(notif.groupId, notif.referenceId, token)
            toast.success('Settlement approved and confirmed!')
          } else {
            throw err
          }
        }
      }

      setNotifications((prev) =>
        prev.map((n) => (n.id === notif.id ? { ...n, isRead: true } : n))
      )
      setUnreadCount((c) => Math.max(0, c - 1))
      onSettlementProcessed?.()
      void fetchNotifications()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to process settlement'
      toast.error(msg)
      void fetchNotifications()
    } finally {
      setActionInProgressId(null)
    }
  }

  const handleConfirmDeclineSettlement = async () => {
    if (!declineTarget || !declineTarget.groupId || !declineTarget.referenceId) return
    try {
      setIsDeclining(true)
      if (declineTarget.type === 'SettlementAwaitingOwner') {
        await splitExpensesApi.declineSettlementByOwner(
          declineTarget.groupId,
          declineTarget.referenceId,
          declineReason || undefined,
          token
        )
      } else {
        try {
          await splitExpensesApi.declineSettlement(
            declineTarget.groupId,
            declineTarget.referenceId,
            declineReason || undefined,
            token
          )
        } catch (err: any) {
          if (err?.message?.toLowerCase().includes('owner')) {
            await splitExpensesApi.declineSettlementByOwner(
              declineTarget.groupId,
              declineTarget.referenceId,
              declineReason || undefined,
              token
            )
          } else {
            throw err
          }
        }
      }

      toast.info('Settlement request declined.')
      setNotifications((prev) =>
        prev.map((n) => (n.id === declineTarget.id ? { ...n, isRead: true } : n))
      )
      setUnreadCount((c) => Math.max(0, c - 1))
      setDeclineTarget(null)
      setDeclineReason('')
      onSettlementProcessed?.()
      void fetchNotifications()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to decline settlement'
      toast.error(msg)
      void fetchNotifications()
    } finally {
      setIsDeclining(false)
    }
  }

  const handleMarkAsRead = async (notificationId: string) => {
    try {
      await splitExpensesApi.markNotificationAsRead(notificationId, token)
      setNotifications((prev) =>
        prev.map((n) => (n.id === notificationId ? { ...n, isRead: true } : n))
      )
      setUnreadCount((c) => Math.max(0, c - 1))
    } catch {
      // Non-fatal
    }
  }

  const handleMarkAllRead = async () => {
    try {
      await splitExpensesApi.markAllNotificationsAsRead(token)
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })))
      setUnreadCount(0)
      toast.success('All notifications marked as read')
    } catch {
      toast.error('Failed to mark all as read')
    }
  }

  const handleItemClick = (notif: SplitNotificationDto) => {
    if (!notif.isRead) {
      void handleMarkAsRead(notif.id)
    }
    if (onNotificationClick) {
      setOpen(false)
      onNotificationClick(notif)
    }
  }

  return (
    <>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger
          render={(props) => (
            <Button
              {...props}
              variant="outline"
              size="sm"
              aria-label="Split Expenses notifications"
              className={cn('relative h-8 w-8 p-0 border-border/80 shadow-none hover:bg-muted/60', className)}
            >
              <Bell className="size-4 text-foreground/80" />
              {unreadCount > 0 && (
                <span className="absolute -top-1 -right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold text-primary-foreground ring-2 ring-background animate-in zoom-in">
                  {unreadCount > 99 ? '99+' : unreadCount}
                </span>
              )}
            </Button>
          )}
        />
        <PopoverContent className="w-80 sm:w-96 p-0" align="end">
          <div className="flex items-center justify-between border-b px-4 py-3">
            <div className="flex items-center gap-2">
              <span className="font-semibold text-sm">Split Notifications</span>
              {unreadCount > 0 && (
                <Badge variant="secondary" className="text-[10px] h-4 px-1.5 font-mono">
                  {unreadCount} new
                </Badge>
              )}
            </div>
            {unreadCount > 0 && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleMarkAllRead}
                className="h-7 text-xs text-primary hover:text-primary/90 px-2"
              >
                Mark all read
              </Button>
            )}
          </div>

          <div className="max-h-[380px] overflow-y-auto divide-y divide-border/60">
            {loading && notifications.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-10 text-muted-foreground gap-2">
                <Loader2 className="size-5 animate-spin" />
                <span className="text-xs">Checking notifications...</span>
              </div>
            ) : notifications.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-10 px-4 text-center text-muted-foreground gap-1.5">
                <div className="flex size-10 items-center justify-center rounded-full bg-muted/60 mb-1">
                  <Inbox className="size-5 opacity-50" />
                </div>
                <p className="text-sm font-medium text-foreground">All caught up!</p>
                <p className="text-xs">No notifications for your Split Expenses groups.</p>
              </div>
            ) : (
              notifications.map((notif) => {
                const isInvited = notif.type === 'GroupInvitationReceived'
                const isAccepted = notif.type === 'InvitationAccepted'
                const isDeclined = notif.type === 'InvitationRejected'
                const isExpenseAdded = notif.type === 'ExpenseAdded'
                const isExpenseUpdated = notif.type === 'ExpenseUpdated'
                const isExpenseDeleted = notif.type === 'ExpenseDeleted'

                // Settlement notification types
                const isSettlementRequested = notif.type === 'SettlementRequested'
                const isSettlementAwaitingOwner = notif.type === 'SettlementAwaitingOwner'
                const isSettlementReminder = notif.type === 'SettlementReminder'
                const isSettlementConfirmed =
                  notif.type === 'SettlementConfirmed' || notif.type === 'SettlementReceiverConfirmed'
                const isSettlementDeclined = notif.type === 'SettlementDeclined'
                const isSettlementCancelled = notif.type === 'SettlementCancelled'
                const isSettlementReversed = notif.type === 'SettlementReversed'
                const isSettlementRecordedForYou = notif.type === 'SettlementRecordedForYou'

                const isSettlementActionable =
                  (isSettlementRequested || isSettlementAwaitingOwner || isSettlementReminder) &&
                  !notif.isRead &&
                  !!notif.referenceId &&
                  !!notif.groupId

                const isBusy = actionInProgressId === notif.id

                return (
                  <div
                    key={notif.id}
                    className={cn(
                      'p-3.5 text-xs transition-colors relative flex gap-3 cursor-pointer select-none',
                      !notif.isRead ? 'bg-primary/5 hover:bg-primary/8' : 'hover:bg-muted/40'
                    )}
                    onClick={() => handleItemClick(notif)}
                  >
                    <div className="shrink-0 mt-0.5">
                      {isInvited && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-primary/10 text-primary">
                          <Users className="size-3.5" />
                        </div>
                      )}
                      {isAccepted && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
                          <CheckCircle2 className="size-3.5" />
                        </div>
                      )}
                      {isDeclined && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-rose-500/10 text-rose-600 dark:text-rose-400">
                          <XCircle className="size-3.5" />
                        </div>
                      )}
                      {isExpenseAdded && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-primary/10 text-primary">
                          <Receipt className="size-3.5" />
                        </div>
                      )}
                      {isExpenseUpdated && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400">
                          <Pencil className="size-3.5" />
                        </div>
                      )}
                      {isExpenseDeleted && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-rose-500/10 text-rose-600 dark:text-rose-400">
                          <Trash2 className="size-3.5" />
                        </div>
                      )}
                      {isSettlementRequested && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400">
                          <HandCoins className="size-3.5" />
                        </div>
                      )}
                      {isSettlementAwaitingOwner && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-indigo-500/10 text-indigo-600 dark:text-indigo-400">
                          <ShieldCheck className="size-3.5" />
                        </div>
                      )}
                      {isSettlementReminder && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400">
                          <Clock className="size-3.5" />
                        </div>
                      )}
                      {isSettlementConfirmed && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
                          <CheckCircle2 className="size-3.5" />
                        </div>
                      )}
                      {isSettlementDeclined && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-rose-500/10 text-rose-600 dark:text-rose-400">
                          <XCircle className="size-3.5" />
                        </div>
                      )}
                      {isSettlementCancelled && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-muted text-muted-foreground">
                          <XCircle className="size-3.5" />
                        </div>
                      )}
                      {isSettlementReversed && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400">
                          <RotateCcw className="size-3.5" />
                        </div>
                      )}
                      {isSettlementRecordedForYou && (
                        <div className="flex size-7 items-center justify-center rounded-full bg-blue-500/10 text-blue-600 dark:text-blue-400">
                          <Info className="size-3.5" />
                        </div>
                      )}
                      {!isInvited &&
                        !isAccepted &&
                        !isDeclined &&
                        !isExpenseAdded &&
                        !isExpenseUpdated &&
                        !isExpenseDeleted &&
                        !isSettlementRequested &&
                        !isSettlementAwaitingOwner &&
                        !isSettlementReminder &&
                        !isSettlementConfirmed &&
                        !isSettlementDeclined &&
                        !isSettlementCancelled &&
                        !isSettlementReversed &&
                        !isSettlementRecordedForYou && (
                          <div className="flex size-7 items-center justify-center rounded-full bg-muted text-muted-foreground">
                            <Bell className="size-3.5" />
                          </div>
                        )}
                    </div>

                    <div className="flex-1 min-w-0 space-y-1">
                      <div className="flex items-center justify-between gap-1">
                        <span className="font-semibold text-foreground text-xs truncate">
                          {notif.title}
                        </span>
                        <span className="text-[10px] text-muted-foreground shrink-0 font-normal">
                          {formatRelativeDate(notif.createdAt)}
                        </span>
                      </div>

                      <p className="text-muted-foreground text-xs leading-relaxed line-clamp-2">
                        {notif.message}
                      </p>

                      {/* Final status badge if acted on */}
                      {notif.isRead && (
                        <div className="pt-0.5">
                          {isSettlementConfirmed && (
                            <span className="inline-block text-[10px] px-1.5 py-0.5 rounded bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 font-medium">
                              Confirmed
                            </span>
                          )}
                          {isSettlementDeclined && (
                            <span className="inline-block text-[10px] px-1.5 py-0.5 rounded bg-rose-500/10 text-rose-600 dark:text-rose-400 font-medium">
                              Declined
                            </span>
                          )}
                          {isSettlementCancelled && (
                            <span className="inline-block text-[10px] px-1.5 py-0.5 rounded bg-muted text-muted-foreground font-medium">
                              Cancelled
                            </span>
                          )}
                          {isSettlementReversed && (
                            <span className="inline-block text-[10px] px-1.5 py-0.5 rounded bg-amber-500/10 text-amber-600 dark:text-amber-400 font-medium">
                              Reversed
                            </span>
                          )}
                        </div>
                      )}

                      {/* Invitation Actions */}
                      {isInvited && notif.referenceId && !notif.isRead && (
                        <div className="flex items-center gap-2 pt-2">
                          <Button
                            size="sm"
                            className="h-7 text-xs gap-1 px-2.5 font-medium"
                            disabled={isBusy}
                            onClick={(e) => {
                              e.stopPropagation()
                              handleAcceptInvitation(notif.referenceId!, notif.id)
                            }}
                          >
                            {isBusy ? <Loader2 className="size-3 animate-spin" /> : <Check className="size-3" />}
                            Accept
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            className="h-7 text-xs gap-1 px-2.5 font-medium"
                            disabled={isBusy}
                            onClick={(e) => {
                              e.stopPropagation()
                              handleDeclineInvitation(notif.referenceId!, notif.id)
                            }}
                          >
                            <X className="size-3" />
                            Decline
                          </Button>
                        </div>
                      )}

                      {/* Settlement Inline Actions (Accept / Decline) */}
                      {isSettlementActionable && (
                        <div className="flex items-center gap-2 pt-2">
                          <Button
                            size="sm"
                            className="h-7 text-xs gap-1 px-2.5 font-medium"
                            disabled={isBusy}
                            onClick={(e) => {
                              e.stopPropagation()
                              handleAcceptSettlement(notif)
                            }}
                          >
                            {isBusy ? <Loader2 className="size-3 animate-spin" /> : <Check className="size-3" />}
                            Accept
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            className="h-7 text-xs gap-1 px-2.5 font-medium"
                            disabled={isBusy}
                            onClick={(e) => {
                              e.stopPropagation()
                              setDeclineTarget(notif)
                              setDeclineReason('')
                            }}
                          >
                            <X className="size-3" />
                            Decline
                          </Button>
                        </div>
                      )}
                    </div>

                    {!notif.isRead && (
                      <span className="size-2 rounded-full bg-primary shrink-0 self-start mt-1.5" />
                    )}
                  </div>
                )
              })
            )}
          </div>
        </PopoverContent>
      </Popover>

      {/* Decline Reason Dialog */}
      <Dialog open={!!declineTarget} onOpenChange={(val) => !val && setDeclineTarget(null)}>
        <DialogContent className="sm:max-w-[425px]">
          <DialogHeader>
            <DialogTitle className="text-base font-semibold">Decline Settlement Request</DialogTitle>
            <DialogDescription className="text-xs">
              {declineTarget?.title}: {declineTarget?.message}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2 py-2">
            <label className="text-xs font-medium text-foreground">
              Reason for declining (optional)
            </label>
            <Textarea
              placeholder="e.g. Payment not received in bank, incorrect amount..."
              value={declineReason}
              onChange={(e) => setDeclineReason(e.target.value)}
              className="resize-none text-xs"
              rows={3}
            />
          </div>
          <DialogFooter className="flex gap-2 sm:justify-end">
            <Button
              variant="outline"
              size="sm"
              disabled={isDeclining}
              onClick={() => setDeclineTarget(null)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              size="sm"
              disabled={isDeclining}
              onClick={handleConfirmDeclineSettlement}
              className="gap-1.5"
            >
              {isDeclining ? <Loader2 className="size-3 animate-spin" /> : <X className="size-3" />}
              Confirm Decline
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
