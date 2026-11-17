import * as React from 'react'
import { AlertCircle } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { cn } from '@/lib/utils'

export interface TransactionStatusCardProps {
  /**
   * Title of the transaction, e.g. "Payment to Ali Yaqteen"
   */
  title: string

  /**
   * Status of the transaction, e.g. "Declined", "Pending", "Confirmed"
   */
  status: 'Declined' | 'Pending' | 'Confirmed' | 'Cancelled' | string

  /**
   * Date string, e.g. "Nov 17, 2026"
   */
  date: string

  /**
   * Reason why the transaction was declined, e.g. "I didn't receive the payment"
   */
  declineReason?: string

  /**
   * Party who declined the transaction, e.g. "receiver" or "admin"
   */
  declinedBy?: 'receiver' | 'admin' | string

  /**
   * Optional transaction amount in currency units
   */
  amount?: number

  /**
   * Currency prefix (defaults to "Rs.")
   */
  currency?: string

  /**
   * Optional note associated with the transaction
   */
  note?: string

  /**
   * Optional secondary action button (e.g. Cancel or Retry)
   */
  actionButton?: React.ReactNode

  /**
   * Additional wrapper class names
   */
  className?: string
}

/**
 * TransactionStatusCard
 *
 * A modern, minimal, fintech-grade status card component for payments and settlements.
 * Highlights declined transactions with high visual hierarchy, clean alert banners,
 * and standard 16px padding on a neutral card base.
 */
export function TransactionStatusCard({
  title,
  status,
  date,
  declineReason,
  declinedBy = 'receiver',
  amount,
  currency = 'Rs.',
  note,
  actionButton,
  className,
}: TransactionStatusCardProps) {
  const isDeclined = status.toLowerCase().includes('decline')

  return (
    <div
      className={cn(
        'group relative overflow-hidden rounded-xl border border-border bg-card p-4 text-card-foreground shadow-xs transition-shadow duration-(--duration-fast) ease-out hover:shadow-sm',
        className
      )}
    >
      {/* Top Row: Title, Date, and Top-Right Status Badge */}
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          {/* Title: 16px Semi-Bold */}
          <h4
            className="text-base font-semibold leading-snug tracking-tight text-foreground truncate"
            title={title}
          >
            {title}
          </h4>

          {/* Date: 12px Grey */}
          <p className="mt-0.5 text-xs text-muted-foreground">
            {date}
            {note && <span className="text-muted-foreground/80"> · {note}</span>}
          </p>
        </div>

        {/* Top-Right: Amount (if provided) & Status Badge */}
        <div className="flex flex-col items-end gap-1.5 shrink-0">
          <Badge
            variant="outline"
            className={cn(
              'h-5 px-2.5 text-[11px] font-semibold tracking-wide rounded-full border transition-colors',
              isDeclined
                ? 'border-destructive/30 bg-destructive/10 text-destructive dark:bg-destructive/20 dark:border-destructive/40'
                : 'border-border bg-muted/60 text-muted-foreground'
            )}
          >
            {status}
          </Badge>

          {typeof amount === 'number' && (
            <span
              className={cn(
                'font-mono text-sm font-semibold tabular-nums',
                isDeclined ? 'text-muted-foreground line-through decoration-muted-foreground/60' : 'text-foreground'
              )}
            >
              {currency} {amount.toFixed(2)}
            </span>
          )}
        </div>
      </div>

      {/* Thin Divider Line between header and reason section */}
      {isDeclined && declineReason && (
        <>
          <Separator className="my-3.5 bg-border/70" />

          {/* Prominent Decline Reason Alert Box */}
          <div
            role="alert"
            className="flex items-start gap-2.5 rounded-lg border border-destructive/20 bg-destructive/[0.05] p-3 text-destructive dark:border-destructive/30 dark:bg-destructive/10"
          >
            {/* Warning Icon (16px) */}
            <AlertCircle className="size-4 shrink-0 text-destructive mt-0.5" aria-hidden="true" />

            {/* Reason Text: 14px subtle red with receiver quote in italics */}
            <div className="text-sm leading-relaxed text-destructive/90 min-w-0 break-words">
              <span className="font-semibold text-destructive">
                Declined by {declinedBy}:
              </span>{' '}
              <span className="italic font-normal">
                &ldquo;{declineReason}&rdquo;
              </span>
            </div>
          </div>
        </>
      )}

      {/* Optional Card Action Button (e.g. Cancel) */}
      {actionButton && (
        <div className="mt-3 flex justify-end">
          {actionButton}
        </div>
      )}
    </div>
  )
}
