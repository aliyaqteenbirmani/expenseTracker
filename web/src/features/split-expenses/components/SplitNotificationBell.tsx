import { useState, useEffect, useCallback } from 'react'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
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
  onNotificationClick?: (notification: SplitNotificationDto) => void
  className?: string
}

export function SplitNotificationBell({
  token,
  onInvitationProcessed,
  onNotificationClick,
  className,
}: SplitNotificationBellProps) {
  const [open, setOpen] = useState(false)
  const [notifications, setNotifications] = useState<SplitNotificationDto[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const [actionInProgressId, setActionInProgressId] = useState<string | null>(null)

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

  const handleAccept = async (invitationId: string, notificationId: string) => {
    try {
      setActionInProgressId(notificationId)
      await splitExpensesApi.acceptInvitation(invitationId, token)
      toast.success('Joined split group successfully!')
      // Mark notification as read locally
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

  const handleDecline = async (invitationId: string, notificationId: string) => {
    try {
      setActionInProgressId(notificationId)
      await splitExpensesApi.rejectInvitation(invitationId, token)
      toast.info('Invitation declined.')
      // Mark notification as read locally
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
                    {!isInvited && !isAccepted && !isDeclined && !isExpenseAdded && !isExpenseUpdated && !isExpenseDeleted && (
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

                    {isInvited && notif.referenceId && !notif.isRead && (
                      <div className="flex items-center gap-2 pt-2">
                        <Button
                          size="sm"
                          className="h-7 text-xs gap-1 px-2.5 font-medium"
                          disabled={isBusy}
                          onClick={(e) => {
                            e.stopPropagation()
                            handleAccept(notif.referenceId!, notif.id)
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
                            handleDecline(notif.referenceId!, notif.id)
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
  )
}
