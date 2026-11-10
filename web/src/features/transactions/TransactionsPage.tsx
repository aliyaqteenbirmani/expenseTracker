import { useState } from 'react'
import { PageHeader } from '@/components/app/PageHeader'
import { Button } from '@/components/ui/button'
import { Gate } from '@/components/app/Gate'
import { TransactionsDataTable } from './TransactionsDataTable'
import {
  ArrowLeft,
  Plus,
  RefreshCw,
  BookOpen,
} from 'lucide-react'
import type { Cashbook, Transaction } from '@/App'
import { cn } from '@/lib/utils'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

interface TransactionsPageProps {
  transactions: Transaction[]
  token: string
  cashbookName: string
  books: Cashbook[]
  selectedBookId?: string
  canCreate?: boolean
  canEdit?: boolean
  canDelete?: boolean
  onSelectBook: (book: Cashbook) => void
  onBack: () => void
  onAdd: () => void
  onAddReceipt: () => void
  onRefresh: () => void
  onToast: (msg: string) => void
  onViewAttachment: (txn: Transaction) => void
  onOpenDetail: (txn: Transaction) => void
  onEditTransaction?: (txn: Transaction) => void
  onDeleteTransaction?: (txn: Transaction) => void
  initialSearch?: string
}

export function TransactionsPage({
  transactions,
  cashbookName,
  books,
  selectedBookId,
  canCreate = true,
  canEdit = true,
  canDelete = true,
  onSelectBook,
  onBack,
  onAdd,
  onAddReceipt,
  onRefresh,
  onToast,
  onViewAttachment,
  onOpenDetail,
  onEditTransaction,
  onDeleteTransaction,
}: TransactionsPageProps) {
  const [refreshing, setRefreshing] = useState(false)
  const activeBook = books.find((b) => b.id === selectedBookId)
  const activeCashbookName =
    activeBook?.cashBookName ?? activeBook?.name ?? cashbookName ?? 'Cashbook'

  const handleRefresh = async () => {
    setRefreshing(true)
    try {
      await onRefresh()
      onToast('Transactions refreshed')
    } finally {
      setTimeout(() => setRefreshing(false), 400)
    }
  }

  return (
    <div className="space-y-6 pb-12">
      <PageHeader
        title="Transactions Ledger"
        description={`Showing all records for ${cashbookName}. Filter, search, inspect and audit entries.`}
        breadcrumb={
          <Button
            variant="ghost"
            size="sm"
            onClick={onBack}
            className="-ml-2 h-7 gap-1 text-xs text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="size-3.5" />
            <span>Back to Overview</span>
          </Button>
        }
        actions={
          <div className="flex flex-wrap items-center gap-2">
            {books.length > 1 ? (
              <Select
                value={selectedBookId}
                items={books.map((b) => {
                  const label = b.cashBookName ?? b.name ?? 'Cashbook'
                  return {
                    value: b.id,
                    label: b.isOwner === false ? `${label} (Shared)` : label,
                  }
                })}
                onValueChange={(val) => {
                  const b = books.find((x) => x.id === val)
                  if (b) onSelectBook(b)
                }}
              >
                <SelectTrigger className="h-8 text-xs min-w-44 max-w-64 gap-1.5">
                  <BookOpen className="size-3.5 text-muted-foreground shrink-0" />
                  <SelectValue placeholder="Select cashbook">
                    {activeCashbookName}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {books.map((b) => {
                    const label = b.cashBookName ?? b.name ?? 'Cashbook'
                    const displayLabel = b.isOwner === false ? `${label} (Shared)` : label
                    return (
                      <SelectItem key={b.id} value={b.id} label={displayLabel} className="text-xs">
                        <span className="flex items-center gap-1.5">
                          <span>{label}</span>
                          {b.isOwner === false && (
                            <span className="rounded bg-amber-500/10 px-1 py-0.5 text-[9px] font-medium text-amber-600 dark:text-amber-400">
                              Shared
                            </span>
                          )}
                        </span>
                      </SelectItem>
                    )
                  })}
                </SelectContent>
              </Select>
            ) : books.length === 1 ? (
              <div className="flex h-8 items-center gap-1.5 rounded-lg border border-border/80 bg-muted/40 px-2.5 text-xs font-medium text-foreground">
                <BookOpen className="size-3.5 text-muted-foreground shrink-0" />
                <span className="truncate max-w-48">{activeCashbookName}</span>
              </div>
            ) : null}

            <Button
              variant="outline"
              size="sm"
              className="h-8 text-xs"
              onClick={handleRefresh}
              disabled={refreshing}
            >
              <RefreshCw className={cn('size-3.5 mr-1.5', refreshing && 'animate-spin')} />
              <span>Refresh</span>
            </Button>

            <Gate
              allow={canCreate}
              reason="Add restricted — ask the cashbook owner for access"
            >
              <Button size="sm" variant="outline" className="h-8 text-xs" onClick={onAddReceipt}>
                <Plus className="size-3.5 mr-1" />
                <span>Add Receipt</span>
              </Button>
              <Button size="sm" className="h-8 text-xs" onClick={onAdd}>
                <Plus className="size-3.5 mr-1" />
                <span>Add Transaction</span>
              </Button>
            </Gate>
          </div>
        }
      />

      <TransactionsDataTable
        transactions={transactions}
        canCreate={canCreate}
        canEdit={canEdit}
        canDelete={canDelete}
        onAddTransaction={onAdd}
        onEditTransaction={onEditTransaction}
        onDeleteTransaction={onDeleteTransaction}
        onSelectTransaction={onOpenDetail}
        onViewAttachment={onViewAttachment}
        onToast={onToast}
        pageSize={25}
      />
    </div>
  )
}
