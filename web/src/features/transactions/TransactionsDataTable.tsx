import { useState, useMemo, useEffect, useRef } from 'react'
import {
  type ColumnDef,
  flexRender,
  getCoreRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  useReactTable,
  type SortingState,
  type ColumnFiltersState,
} from '@tanstack/react-table'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
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
import {
  ArrowDownLeft,
  ArrowUpRight,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  MoreHorizontal,
  Search,
  Paperclip,
  Download,
  Trash2,
  Pencil,
  Copy,
  Eye,
  Lock,
  Plus,
  RotateCcw,
} from 'lucide-react'
import { getTransactionCategory, type Transaction } from '@/App'
import { MoneyText } from '@/components/app/MoneyText'
import { EmptyState } from '@/components/app/EmptyState'
import { Skeleton } from '@/components/ui/skeleton'
import { formatDate, formatRelativeDate } from '@/lib/format'
import { cn } from '@/lib/utils'
import { motion, AnimatePresence } from 'motion/react'
import { spring } from '@/lib/motion'
import { useDelayedFlag } from '@/hooks/use-delayed-flag'
import { AnimatedNumber } from '@/components/motion/animated-number'
import { MotionToggleGroup } from '@/components/motion/motion-toggle-group'
import { Swap } from '@/components/motion/swap'

interface TransactionsDataTableProps {
  transactions: Transaction[]
  loading?: boolean
  canCreate?: boolean
  canEdit?: boolean
  canDelete?: boolean
  onAddTransaction?: () => void
  onEditTransaction?: (txn: Transaction) => void
  onDeleteTransaction?: (txn: Transaction) => void
  onSelectTransaction?: (txn: Transaction) => void
  onViewAttachment?: (txn: Transaction) => void
  onToast: (msg: string) => void
  pageSize?: number
  hideToolbar?: boolean
  className?: string
}

export function TransactionsDataTable({
  transactions,
  loading = false,
  canCreate = true,
  canEdit = true,
  canDelete = true,
  onAddTransaction,
  onEditTransaction,
  onDeleteTransaction,
  onSelectTransaction,
  onViewAttachment,
  onToast,
  pageSize = 10,
  hideToolbar = false,
  className,
}: TransactionsDataTableProps) {
  const [sorting, setSorting] = useState<SortingState>([])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [globalFilter, setGlobalFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState<'all' | 'cashin' | 'cashout'>('all')
  const [rowSelection, setRowSelection] = useState({})
  const [deleteCandidate, setDeleteCandidate] = useState<Transaction | null>(null)
  const showSkeleton = useDelayedFlag(loading, 150)
  const selectedCount = Object.keys(rowSelection).length

  // Track recently added or updated transactions for realtime row flashing
  const [recentTxnIds, setRecentTxnIds] = useState<Set<string>>(new Set())
  const prevTransactionsRef = useRef<Transaction[]>(transactions)

  useEffect(() => {
    if (prevTransactionsRef.current !== transactions) {
      const prevMap = new Map(prevTransactionsRef.current.map((t) => [t.id, t]))
      const updatedOrNewIds = new Set<string>()

      for (const t of transactions) {
        const prev = prevMap.get(t.id)
        if (!prev) {
          updatedOrNewIds.add(t.id)
        } else if (
          prev.amount !== t.amount ||
          prev.remarks !== t.remarks ||
          prev.transactionType !== t.transactionType
        ) {
          updatedOrNewIds.add(t.id)
        }
      }

      if (updatedOrNewIds.size > 0 && prevTransactionsRef.current.length > 0) {
        setRecentTxnIds(updatedOrNewIds)
        const timer = setTimeout(() => {
          setRecentTxnIds(new Set())
        }, 1500)
        prevTransactionsRef.current = transactions
        return () => clearTimeout(timer)
      }
      prevTransactionsRef.current = transactions
    }
  }, [transactions])

  const filteredData = useMemo(() => {
    return transactions.filter((t) => {
      // Type filter
      if (typeFilter !== 'all') {
        if (t.transactionType?.toLowerCase() !== typeFilter) return false
      }
      // Global search
      if (globalFilter.trim()) {
        const query = globalFilter.toLowerCase()
        const remarks = (t.remarks ?? '').toLowerCase()
        const author = (t.createdBy ?? '').toLowerCase()
        const amount = String(t.amount || '')
        if (!remarks.includes(query) && !author.includes(query) && !amount.includes(query)) {
          return false
        }
      }
      return true
    })
  }, [transactions, typeFilter, globalFilter])

  const columns = useMemo<ColumnDef<Transaction>[]>(() => [
    {
      id: 'select',
      header: ({ table }) => (
        <Checkbox
          checked={table.getIsAllPageRowsSelected()}
          onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
          aria-label="Select all"
          className="translate-y-0.5"
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          checked={row.getIsSelected()}
          onCheckedChange={(value) => row.toggleSelected(!!value)}
          aria-label="Select row"
          className="translate-y-0.5"
          onClick={(e) => e.stopPropagation()}
        />
      ),
      enableSorting: false,
      enableHiding: false,
    },
    {
      accessorKey: 'transactionType',
      header: 'Type',
      cell: ({ row }) => {
        const t = row.original
        const isIncome = t.transactionType?.toLowerCase() === 'cashin'
        return (
          <div
            className={cn(
              'flex size-8 shrink-0 items-center justify-center rounded-lg border',
              isIncome
                ? 'border-inflow/30 bg-inflow-muted/60 text-inflow'
                : 'border-outflow/30 bg-outflow-muted/60 text-outflow'
            )}
            title={isIncome ? 'Cash In (Income)' : 'Cash Out (Expense)'}
          >
            {isIncome ? (
              <ArrowDownLeft className="size-4 stroke-[2]" />
            ) : (
              <ArrowUpRight className="size-4 stroke-[2]" />
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'remarks',
      header: ({ column }) => {
        const sorted = column.getIsSorted()
        return (
          <Button
            variant="ghost"
            size="sm"
            className="-ml-3 h-8 text-xs font-semibold"
            onClick={() => column.toggleSorting(sorted === 'asc')}
          >
            Description & Details
            <Swap k={String(sorted)} className="ml-1.5">
              {sorted === 'asc' ? (
                <ArrowUp className="size-3 text-primary" />
              ) : sorted === 'desc' ? (
                <ArrowDown className="size-3 text-primary" />
              ) : (
                <ArrowUpDown className="size-3 text-muted-foreground" />
              )}
            </Swap>
          </Button>
        )
      },
      cell: ({ row }) => {
        const t = row.original
        const cat = getTransactionCategory(t)
        const hasAttachment = Boolean(t.filePath || t.fileName)

        return (
          <div className="space-y-1 max-w-sm sm:max-w-md">
            <p className="font-medium text-sm text-foreground truncate">
              {t.remarks || 'Untitled entry'}
            </p>
            <div className="flex flex-wrap items-center gap-1.5 text-xs text-muted-foreground">
              <Badge variant="secondary" className="px-1.5 py-0 text-[10px] font-normal">
                {cat.label}
              </Badge>
              {t.createdBy && (
                <span className="text-[11px]">
                  by <strong className="text-foreground font-medium">{t.createdBy}</strong>
                </span>
              )}
              {hasAttachment && (
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation()
                    onViewAttachment?.(t)
                  }}
                  className="inline-flex items-center gap-1 text-[11px] text-primary hover:underline ml-1"
                >
                  <Paperclip className="size-3" />
                  <span>Receipt</span>
                </button>
              )}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'transactionDate',
      header: ({ column }) => {
        const sorted = column.getIsSorted()
        return (
          <Button
            variant="ghost"
            size="sm"
            className="-ml-3 h-8 text-xs font-semibold"
            onClick={() => column.toggleSorting(sorted === 'asc')}
          >
            Date
            <Swap k={String(sorted)} className="ml-1.5">
              {sorted === 'asc' ? (
                <ArrowUp className="size-3 text-primary" />
              ) : sorted === 'desc' ? (
                <ArrowDown className="size-3 text-primary" />
              ) : (
                <ArrowUpDown className="size-3 text-muted-foreground" />
              )}
            </Swap>
          </Button>
        )
      },
      cell: ({ row }) => {
        const date = row.original.transactionDate
        return (
          <div className="space-y-0.5">
            <p className="text-xs font-medium text-foreground">{formatDate(date) || '—'}</p>
            <p className="text-[11px] text-muted-foreground">{formatRelativeDate(date)}</p>
          </div>
        )
      },
    },
    {
      accessorKey: 'amount',
      header: () => <div className="text-right text-xs font-semibold">Amount</div>,
      cell: ({ row }) => {
        const t = row.original
        const isIncome = t.transactionType?.toLowerCase() === 'cashin'
        return (
          <div className="text-right">
            <MoneyText
              amount={Number(t.amount || 0)}
              type={isIncome ? 'inflow' : 'outflow'}
              className="text-sm font-semibold"
            />
          </div>
        )
      },
    },
    {
      id: 'actions',
      cell: ({ row }) => {
        const t = row.original

        if (!canEdit && !canDelete) {
          return (
            <Badge
              variant="outline"
              className="text-[10px] px-1.5 py-0 gap-1 text-muted-foreground border-border/80"
              title="Read-only: permission controlled by owner"
            >
              <Lock className="size-2.5" />
              <span>Read only</span>
            </Badge>
          )
        }

        return (
          <DropdownMenu>
            <DropdownMenuTrigger
              render={(props) => (
                <Button
                  {...props}
                  variant="ghost"
                  size="icon"
                  className="size-8 cursor-pointer"
                  onClick={(e) => e.stopPropagation()}
                >
                  <MoreHorizontal className="size-4" />
                </Button>
              )}
            />
            <DropdownMenuContent align="end" className="w-40">
              <DropdownMenuItem
                onClick={(e) => {
                  e.stopPropagation()
                  onSelectTransaction?.(t)
                }}
              >
                <Eye className="size-4 mr-2" />
                <span>View Details</span>
              </DropdownMenuItem>
              {canEdit && (
                <DropdownMenuItem
                  onClick={(e) => {
                    e.stopPropagation()
                    onEditTransaction?.(t)
                  }}
                >
                  <Pencil className="size-4 mr-2" />
                  <span>Edit Entry</span>
                </DropdownMenuItem>
              )}
              <DropdownMenuItem
                onClick={(e) => {
                  e.stopPropagation()
                  navigator.clipboard.writeText(t.id)
                  onToast('Transaction ID copied')
                }}
              >
                <Copy className="size-4 mr-2" />
                <span>Copy ID</span>
              </DropdownMenuItem>
              {canDelete && (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="text-destructive focus:text-destructive"
                    onClick={(e) => {
                      e.stopPropagation()
                      setDeleteCandidate(t)
                    }}
                  >
                    <Trash2 className="size-4 mr-2" />
                    <span>Delete</span>
                  </DropdownMenuItem>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ], [canEdit, canDelete, onSelectTransaction, onEditTransaction, onViewAttachment, onToast])

  const table = useReactTable({
    data: filteredData,
    columns,
    state: {
      sorting,
      columnFilters,
      rowSelection,
    },
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    initialState: {
      pagination: {
        pageSize,
      },
    },
  })

  const exportCSV = () => {
    if (!transactions.length) return
    const headers = ['ID', 'Date', 'Type', 'Amount', 'Remarks', 'Author']
    const rows = transactions.map((t) => [
      t.id,
      t.transactionDate || '',
      t.transactionType || '',
      String(t.amount || 0),
      `"${(t.remarks || '').replace(/"/g, '""')}"`,
      t.createdBy || '',
    ])
    const csvContent =
      'data:text/csv;charset=utf-8,' +
      [headers.join(','), ...rows.map((e) => e.join(','))].join('\n')
    const encodedUri = encodeURI(csvContent)
    const link = document.createElement('a')
    link.setAttribute('href', encodedUri)
    link.setAttribute('download', `transactions_${new Date().toISOString().split('T')[0]}.csv`)
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    onToast('Transactions exported as CSV')
  }

  return (
    <div className={cn('space-y-3', className)}>
      {/* Toolbar */}
      {!hideToolbar && (
        <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-1 items-center gap-2">
            <div className="relative flex-1 max-w-xs">
              <Search className="absolute left-2.5 top-2.5 size-3.5 text-muted-foreground" />
              <Input
                placeholder="Search transactions..."
                value={globalFilter}
                onChange={(e) => setGlobalFilter(e.target.value)}
                className="pl-8 h-8 text-xs"
              />
            </div>
            <MotionToggleGroup
              value={typeFilter}
              onValueChange={(val) => setTypeFilter(val as 'all' | 'cashin' | 'cashout')}
              items={[
                { value: 'all', label: 'All' },
                { value: 'cashin', label: 'Inflow', tint: 'inflow' },
                { value: 'cashout', label: 'Outflow', tint: 'outflow' },
              ]}
              size="sm"
            />
            {(globalFilter || typeFilter !== 'all') && (
              <Button
                variant="ghost"
                size="icon"
                className="size-8"
                onClick={() => {
                  setGlobalFilter('')
                  setTypeFilter('all')
                }}
                title="Reset filters"
              >
                <RotateCcw className="size-3.5" />
              </Button>
            )}
          </div>

          <div className="flex items-center gap-2 shrink-0">
            <Button
              variant="outline"
              size="sm"
              className="h-8 text-xs gap-1.5"
              onClick={exportCSV}
            >
              <Download className="size-3.5" />
              <span>Export</span>
            </Button>
            {canCreate && onAddTransaction && (
              <Button
                size="sm"
                className="h-8 text-xs gap-1.5"
                onClick={onAddTransaction}
              >
                <Plus className="size-3.5" />
                <span>Add Entry</span>
              </Button>
            )}
          </div>
        </div>
      )}

      {/* Table Container */}
      <div className="rounded-xl border border-border/80 bg-card overflow-hidden shadow-xs">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id} className="hover:bg-transparent border-b">
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id} className="h-10 text-xs font-semibold">
                    {header.isPlaceholder
                      ? null
                      : flexRender(header.column.columnDef.header, header.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {showSkeleton ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i} className="h-14">
                  <TableCell className="w-10">
                    <Skeleton className="size-4 rounded" />
                  </TableCell>
                  <TableCell className="w-12">
                    <Skeleton className="size-8 rounded-lg" />
                  </TableCell>
                  <TableCell>
                    <div className="space-y-1">
                      <Skeleton className="h-4 w-44" />
                      <Skeleton className="h-3 w-28" />
                    </div>
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-24" />
                  </TableCell>
                  <TableCell className="text-right">
                    <Skeleton className="h-4 w-20 ml-auto" />
                  </TableCell>
                  <TableCell className="w-10">
                    <Skeleton className="size-6 rounded" />
                  </TableCell>
                </TableRow>
              ))
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => {
                const isRecent = recentTxnIds.has(row.original.id)
                return (
                  <TableRow
                    key={row.id}
                    data-state={row.getIsSelected() && 'selected'}
                    onClick={() => onSelectTransaction?.(row.original)}
                    className={cn(
                      "cursor-pointer hover:bg-muted/50 transition-colors duration-(--duration-instant) h-14",
                      isRecent && "animate-row-flash"
                    )}
                  >
                    {row.getVisibleCells().map((cell) => (
                      <TableCell key={cell.id} className="py-2.5 text-xs">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </TableCell>
                    ))}
                  </TableRow>
                )
              })
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-48 text-center p-0">
                  <EmptyState
                    title="No transactions found"
                    description={
                      globalFilter || typeFilter !== 'all'
                        ? 'Try clearing your search query or filters to find what you need.'
                        : 'Record your first cash in or cash out entry to get started.'
                    }
                    action={
                      canCreate && onAddTransaction ? (
                        <Button size="sm" onClick={onAddTransaction} className="gap-1.5">
                          <Plus className="size-3.5" />
                          <span>Record first transaction</span>
                        </Button>
                      ) : undefined
                    }
                    className="border-0 shadow-none"
                  />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>

        {/* Pagination bar */}
        {!loading && filteredData.length > 0 && (
          <div className="flex items-center justify-between px-4 py-3 border-t text-xs text-muted-foreground">
            <div>
              Showing{' '}
              <strong className="text-foreground">
                {table.getState().pagination.pageIndex * table.getState().pagination.pageSize + 1}
              </strong>{' '}
              to{' '}
              <strong className="text-foreground">
                {Math.min(
                  (table.getState().pagination.pageIndex + 1) *
                    table.getState().pagination.pageSize,
                  filteredData.length
                )}
              </strong>{' '}
              of <strong className="text-foreground">{filteredData.length}</strong> transactions
            </div>
            <div className="flex items-center gap-1.5">
              <Button
                variant="outline"
                size="sm"
                className="h-7 text-xs px-2.5"
                onClick={() => table.previousPage()}
                disabled={!table.getCanPreviousPage()}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="h-7 text-xs px-2.5"
                onClick={() => table.nextPage()}
                disabled={!table.getCanNextPage()}
              >
                Next
              </Button>
            </div>
          </div>
        )}
      </div>

      {/* Floating Bulk Selection Action Bar */}
      <AnimatePresence>
        {selectedCount > 0 && (
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 16 }}
            transition={spring.smooth}
            className="fixed bottom-6 left-1/2 -translate-x-1/2 z-40 flex items-center gap-3 rounded-xl border border-border/80 bg-popover px-4 py-2 text-xs shadow-xl text-popover-foreground backdrop-blur-md"
          >
            <span className="font-semibold flex items-center gap-1.5">
              <span className="size-2 rounded-full bg-primary animate-pulse" />
              <AnimatedNumber value={selectedCount} />
              <span>selected</span>
            </span>
            <div className="h-4 w-px bg-border/80" />
            <Button
              size="sm"
              variant="outline"
              className="h-7 text-xs"
              onClick={exportCSV}
            >
              <Download className="size-3 mr-1" />
              <span>Export Selected</span>
            </Button>
            <Button
              size="sm"
              variant="ghost"
              className="h-7 text-xs text-muted-foreground hover:text-foreground"
              onClick={() => setRowSelection({})}
            >
              Deselect All
            </Button>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Delete Confirmation Alert Dialog */}
      <AlertDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Transaction?</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to permanently delete this transaction of{' '}
              <strong>
                {deleteCandidate
                  ? `${deleteCandidate.transactionType === 'cashin' ? '+' : '−'}${deleteCandidate.amount}`
                  : ''}
              </strong>
              ? This operation cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => {
                if (deleteCandidate) {
                  onDeleteTransaction?.(deleteCandidate)
                  setDeleteCandidate(null)
                }
              }}
            >
              Delete Permanently
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
