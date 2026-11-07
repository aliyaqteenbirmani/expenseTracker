import { useState } from 'react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetFooter,
} from '@/components/ui/sheet'
import {
  Drawer,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
  DrawerFooter,
} from '@/components/ui/drawer'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Card } from '@/components/ui/card'
import {
  ArrowDownLeft,
  ArrowUpRight,
  Calendar,
  Clock,
  Copy,
  Check,
  Paperclip,
  Pencil,
  Trash2,
  Lock,
  ExternalLink,
  BookOpen,
} from 'lucide-react'
import { useIsMobile } from '@/hooks/use-mobile'
import { getTransactionCategory, type Transaction } from '@/App'
import { formatMoney, formatDate, formatDateTime, formatRelativeDate } from '@/lib/format'
import { AnimatedNumber } from '@/components/app/AnimatedNumber'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'

interface TransactionDetailSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  transaction: Transaction | null
  cashbookName?: string
  businessName?: string
  canEdit?: boolean
  canDelete?: boolean
  onEdit?: (txn: Transaction) => void
  onDelete?: (txn: Transaction) => void
  onViewAttachment?: (txn: Transaction) => void
  onToast: (msg: string) => void
}

export function TransactionDetailSheet({
  open,
  onOpenChange,
  transaction,
  cashbookName = 'Cashbook',
  businessName = 'Workspace',
  canEdit = true,
  canDelete = true,
  onEdit,
  onDelete,
  onViewAttachment,
  onToast,
}: TransactionDetailSheetProps) {
  const isMobile = useIsMobile()
  const [copied, setCopied] = useState(false)

  if (!transaction) return null

  const isIncome = transaction.transactionType?.toLowerCase() === 'cashin'
  const category = getTransactionCategory(transaction)
  const CategoryIcon = category.Icon
  const hasAttachment = Boolean(transaction.filePath || transaction.fileName)
  const authorName = transaction.createdBy || 'Unknown User'
  const authorInitials = authorName.slice(0, 2).toUpperCase()

  const copyId = () => {
    if (transaction.id) {
      navigator.clipboard.writeText(transaction.id)
      setCopied(true)
      onToast('Transaction ID copied to clipboard')
      setTimeout(() => setCopied(false), 2000)
    }
  }

  const content = (
    <div className="space-y-6">
      {/* 1. Hero Band */}
      <div
        className={cn(
          'relative -mx-6 -mt-6 p-6 pb-8 border-b space-y-3',
          isIncome
            ? 'bg-gradient-to-b from-inflow-muted/80 to-background border-inflow/20'
            : 'bg-gradient-to-b from-outflow-muted/80 to-background border-outflow/20'
        )}
      >
        <div className="flex items-center justify-between">
          <Badge
            variant="outline"
            className={cn(
              'font-mono text-xs font-semibold px-2.5 py-0.5 inline-flex items-center gap-1.5',
              isIncome
                ? 'border-inflow/30 bg-inflow-muted text-inflow'
                : 'border-outflow/30 bg-outflow-muted text-outflow'
            )}
          >
            {isIncome ? <ArrowDownLeft className="size-3.5" /> : <ArrowUpRight className="size-3.5" />}
            {isIncome ? 'Cash In (Income)' : 'Cash Out (Expense)'}
          </Badge>
          <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <CategoryIcon className="size-3.5" />
            <span>{category.label}</span>
          </div>
        </div>

        <div className="pt-2">
          <p className="text-xs uppercase tracking-wider text-muted-foreground font-medium">
            Amount
          </p>
          <div
            className={cn(
              'text-4xl font-mono font-bold tracking-tight tabular-nums',
              isIncome ? 'text-inflow' : 'text-outflow'
            )}
          >
            {isIncome ? '+ ' : '− '}
            <AnimatedNumber
              value={Number(transaction.amount || 0)}
              formatFn={(v) => formatMoney(v)}
            />
          </div>
        </div>
      </div>

      {/* 2. Metadata Grid */}
      <div className="grid grid-cols-2 gap-3">
        {/* Date */}
        <Card className="p-3 border-border/80 shadow-none space-y-1">
          <div className="flex items-center gap-1.5 text-muted-foreground text-xs">
            <Calendar className="size-3.5" />
            <span>Transaction Date</span>
          </div>
          <p className="text-xs font-medium">{formatDate(transaction.transactionDate) || '—'}</p>
          <p className="text-[11px] text-muted-foreground">
            {formatRelativeDate(transaction.transactionDate)}
          </p>
        </Card>

        {/* Recorded By */}
        <Card className="p-3 border-border/80 shadow-none space-y-1">
          <div className="flex items-center gap-1.5 text-muted-foreground text-xs">
            <Clock className="size-3.5" />
            <span>Recorded By</span>
          </div>
          <div className="flex items-center gap-2 pt-0.5">
            <Avatar className="size-5">
              <AvatarFallback className="text-[10px] bg-primary/10 text-primary font-semibold">
                {authorInitials}
              </AvatarFallback>
            </Avatar>
            <span className="text-xs font-medium truncate">{authorName}</span>
          </div>
          <p className="text-[11px] text-muted-foreground">Author attribution</p>
        </Card>

        {/* Cashbook & Workspace */}
        <Card className="p-3 border-border/80 shadow-none space-y-1">
          <div className="flex items-center gap-1.5 text-muted-foreground text-xs">
            <BookOpen className="size-3.5" />
            <span>Cashbook</span>
          </div>
          <p className="text-xs font-medium truncate">{cashbookName}</p>
          <p className="text-[11px] text-muted-foreground truncate">{businessName}</p>
        </Card>

        {/* Transaction ID */}
        <Card className="p-3 border-border/80 shadow-none space-y-1">
          <div className="flex items-center justify-between text-muted-foreground text-xs">
            <span>Transaction ID</span>
            <Button
              variant="ghost"
              size="icon"
              className="size-5 h-5 w-5"
              onClick={copyId}
              title="Copy ID"
            >
              {copied ? <Check className="size-3 text-inflow" /> : <Copy className="size-3" />}
            </Button>
          </div>
          <p className="font-mono text-xs text-foreground truncate" title={transaction.id}>
            {transaction.id ? `${transaction.id.slice(0, 10)}…` : '—'}
          </p>
          <p className="text-[11px] text-muted-foreground">Internal reference</p>
        </Card>
      </div>

      {/* 3. Notes / Remarks */}
      <div className="space-y-1.5">
        <label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
          Notes & Description
        </label>
        <div className="rounded-lg border border-border/80 bg-muted/30 p-3 text-sm">
          {transaction.remarks ? (
            <p className="leading-relaxed whitespace-pre-wrap">{transaction.remarks}</p>
          ) : (
            <p className="italic text-muted-foreground text-xs">No notes provided for this transaction.</p>
          )}
        </div>
      </div>

      {/* 4. Receipt Attachment */}
      {hasAttachment && (
        <div className="space-y-2">
          <label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
            Receipt Attachment
          </label>
          <div className="flex items-center justify-between rounded-lg border border-border/80 bg-card p-3">
            <div className="flex items-center gap-2.5 truncate">
              <div className="flex size-8 items-center justify-center rounded-md bg-muted text-muted-foreground">
                <Paperclip className="size-4" />
              </div>
              <div className="truncate">
                <p className="text-xs font-medium truncate">
                  {transaction.fileName || 'Attached Receipt'}
                </p>
                <p className="text-[11px] text-muted-foreground">Scanned document</p>
              </div>
            </div>
            <Button
              size="sm"
              variant="outline"
              className="text-xs h-8 gap-1.5"
              onClick={() => onViewAttachment?.(transaction)}
            >
              <ExternalLink className="size-3.5" />
              <span>View</span>
            </Button>
          </div>
        </div>
      )}

      {/* Audit timestamp */}
      {transaction.createdOn && (
        <p className="text-[11px] text-muted-foreground text-center pt-2">
          Created on {formatDateTime(transaction.createdOn)}
        </p>
      )}
    </div>
  )

  const footer = (
    <div className="flex items-center justify-end gap-2 w-full pt-4 border-t">
      {/* Delete button */}
      {canDelete ? (
        <Button
          variant="outline"
          size="sm"
          className="text-destructive hover:bg-destructive/10 hover:text-destructive h-9"
          onClick={() => {
            onOpenChange(false)
            onDelete?.(transaction)
          }}
        >
          <Trash2 className="size-4 mr-1.5" />
          Delete
        </Button>
      ) : (
        <Tooltip>
          <TooltipTrigger className="cursor-not-allowed opacity-60">
            <Button variant="outline" size="sm" disabled className="h-9 pointer-events-none">
              <Lock className="size-3.5 mr-1.5" />
              Delete Restricted
            </Button>
          </TooltipTrigger>
          <TooltipContent side="top">
            <p className="text-xs">Delete restricted — ask the cashbook owner for access</p>
          </TooltipContent>
        </Tooltip>
      )}

      {/* Edit button */}
      {canEdit ? (
        <Button
          variant="default"
          size="sm"
          className="h-9"
          onClick={() => {
            onOpenChange(false)
            onEdit?.(transaction)
          }}
        >
          <Pencil className="size-4 mr-1.5" />
          Edit Entry
        </Button>
      ) : (
        <Tooltip>
          <TooltipTrigger className="cursor-not-allowed opacity-60">
            <Button size="sm" disabled className="h-9 pointer-events-none">
              <Lock className="size-3.5 mr-1.5" />
              Edit Restricted
            </Button>
          </TooltipTrigger>
          <TooltipContent side="top">
            <p className="text-xs">Edit restricted — ask the cashbook owner for access</p>
          </TooltipContent>
        </Tooltip>
      )}
    </div>
  )

  if (isMobile) {
    return (
      <Drawer open={open} onOpenChange={onOpenChange}>
        <DrawerContent className="max-h-[92vh]">
          <DrawerHeader className="text-left px-6 pt-4 pb-2">
            <DrawerTitle className="text-base font-semibold">Transaction Details</DrawerTitle>
          </DrawerHeader>
          <div className="p-6 overflow-y-auto">{content}</div>
          <DrawerFooter className="px-6 pb-6 pt-2">{footer}</DrawerFooter>
        </DrawerContent>
      </Drawer>
    )
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-md w-full overflow-y-auto flex flex-col justify-between">
        <div>
          <SheetHeader className="pb-4">
            <SheetTitle className="text-base font-semibold">Transaction Details</SheetTitle>
          </SheetHeader>
          {content}
        </div>
        <SheetFooter>{footer}</SheetFooter>
      </SheetContent>
    </Sheet>
  )
}
