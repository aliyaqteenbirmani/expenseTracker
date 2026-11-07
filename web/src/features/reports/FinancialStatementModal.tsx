import { Printer } from 'lucide-react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { formatMoney, formatDate } from '@/lib/format'
import { cn } from '@/lib/utils'

export type CategorySummary = {
  label: string
  amount: number
  count: number
  percentage: number
}

export type ReportTransaction = {
  id: string
  amount: number
  transactionType: string
  remarks?: string
  transactionDate?: string
}

interface FinancialStatementModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  businessName: string
  cashbookName: string
  periodLabel: string
  totalIncome: number
  totalExpense: number
  netBalance: number
  savingsRate: number
  categoryBreakdown: CategorySummary[]
  transactions: ReportTransaction[]
}

export function FinancialStatementModal({
  open,
  onOpenChange,
  businessName,
  cashbookName,
  periodLabel,
  totalIncome,
  totalExpense,
  netBalance,
  savingsRate,
  categoryBreakdown,
  transactions,
}: FinancialStatementModalProps) {
  const currentDate = new Intl.DateTimeFormat('en-US', {
    dateStyle: 'full',
    timeStyle: 'short',
  }).format(new Date())

  const sortedTransactions = [...transactions].sort((a, b) =>
    (b.transactionDate ?? '').localeCompare(a.transactionDate ?? '')
  )

  const handlePrint = () => {
    window.print()
  }

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      className="sm:max-w-4xl"
      title={
        <div className="flex items-center justify-between w-full pr-6">
          <div>
            <h2 className="text-lg font-bold">Formal Financial Statement</h2>
            <p className="text-xs text-muted-foreground">{businessName} · {cashbookName}</p>
          </div>
          <Button size="sm" onClick={handlePrint} className="h-8 gap-1.5 text-xs">
            <Printer className="size-3.5" />
            <span>Print / Save as PDF</span>
          </Button>
        </div>
      }
    >
      <div className="space-y-6 pt-2 print:p-0 print:space-y-4">
        {/* Statement Metadata */}
        <div className="rounded-xl border border-border/80 bg-muted/20 p-4 space-y-2">
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
            <div>
              <span className="text-muted-foreground block text-[11px]">Reporting Period</span>
              <strong className="font-semibold">{periodLabel}</strong>
            </div>
            <div>
              <span className="text-muted-foreground block text-[11px]">Generated At</span>
              <strong className="font-semibold">{currentDate}</strong>
            </div>
            <div>
              <span className="text-muted-foreground block text-[11px]">Currency</span>
              <strong className="font-semibold">PKR (₨)</strong>
            </div>
            <div>
              <span className="text-muted-foreground block text-[11px]">Operating Margin</span>
              <strong className={cn('font-semibold font-mono', savingsRate >= 0 ? 'text-inflow' : 'text-outflow')}>
                {savingsRate}%
              </strong>
            </div>
          </div>
        </div>

        {/* Executive Summary Grid */}
        <div className="grid grid-cols-3 gap-3">
          <div className="p-3 rounded-xl border border-inflow/30 bg-inflow-muted/50 text-left">
            <span className="text-[11px] font-medium uppercase text-inflow block">Total Inflow</span>
            <span className="text-xl font-bold font-mono text-inflow">{formatMoney(totalIncome)}</span>
          </div>
          <div className="p-3 rounded-xl border border-outflow/30 bg-outflow-muted/50 text-left">
            <span className="text-[11px] font-medium uppercase text-outflow block">Total Outflow</span>
            <span className="text-xl font-bold font-mono text-outflow">{formatMoney(totalExpense)}</span>
          </div>
          <div className="p-3 rounded-xl border border-border/80 bg-muted/30 text-left">
            <span className="text-[11px] font-medium uppercase text-muted-foreground block">Net Surplus</span>
            <span className={cn('text-xl font-bold font-mono', netBalance >= 0 ? 'text-inflow' : 'text-outflow')}>
              {formatMoney(netBalance)}
            </span>
          </div>
        </div>

        {/* Expense Category Breakdown */}
        {categoryBreakdown.length > 0 && (
          <div className="space-y-2">
            <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Expense Distribution Summary
            </h4>
            <div className="rounded-lg border border-border/80 overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow className="border-b">
                    <TableHead className="text-xs">Category</TableHead>
                    <TableHead className="text-xs text-right">Transactions</TableHead>
                    <TableHead className="text-xs text-right">Share (%)</TableHead>
                    <TableHead className="text-xs text-right">Subtotal</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {categoryBreakdown.map((cat) => (
                    <TableRow key={cat.label} className="text-xs">
                      <TableCell className="font-medium">{cat.label}</TableCell>
                      <TableCell className="text-right font-mono">{cat.count}</TableCell>
                      <TableCell className="text-right font-mono">{cat.percentage}%</TableCell>
                      <TableCell className="text-right font-mono font-semibold">
                        {formatMoney(cat.amount)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </div>
        )}

        {/* Audit Transaction Ledger */}
        <div className="space-y-2">
          <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Transaction Itemization ({sortedTransactions.length} records)
          </h4>
          <div className="rounded-lg border border-border/80 overflow-hidden max-h-72 overflow-y-auto">
            <Table>
              <TableHeader>
                <TableRow className="border-b">
                  <TableHead className="text-xs">Date</TableHead>
                  <TableHead className="text-xs">Type</TableHead>
                  <TableHead className="text-xs">Remarks</TableHead>
                  <TableHead className="text-xs text-right">Amount</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {sortedTransactions.map((t) => {
                  const isIncome = t.transactionType?.toLowerCase() === 'cashin'
                  return (
                    <TableRow key={t.id} className="text-xs">
                      <TableCell className="font-mono text-muted-foreground">
                        {formatDate(t.transactionDate)}
                      </TableCell>
                      <TableCell>
                        <span className={cn('font-semibold', isIncome ? 'text-inflow' : 'text-outflow')}>
                          {isIncome ? 'Cash In' : 'Cash Out'}
                        </span>
                      </TableCell>
                      <TableCell className="max-w-xs truncate">{t.remarks || '—'}</TableCell>
                      <TableCell className="text-right font-mono font-medium">
                        {formatMoney(Number(t.amount || 0))}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </div>
        </div>
      </div>
    </ResponsiveDialog>
  )
}
