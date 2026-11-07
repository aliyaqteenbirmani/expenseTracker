import { useMemo } from 'react'
import { PageHeader } from '@/components/app/PageHeader'
import { StatCard } from '@/components/app/StatCard'
import { Button } from '@/components/ui/button'
import { Gate } from '@/components/app/Gate'
import { CashFlowChart } from './CashFlowChart'
import { MonthlyBudgetCard } from './MonthlyBudgetCard'
import { CategoryBreakdownCard } from './CategoryBreakdownCard'
import { TransactionsDataTable } from '@/features/transactions/TransactionsDataTable'
import {
  Wallet,
  ArrowDownLeft,
  ArrowUpRight,
  Receipt,
  Plus,
} from 'lucide-react'
import type { Cashbook, Transaction } from '@/App'
import { formatMoney } from '@/lib/format'

interface DashboardPageProps {
  cashbook: Cashbook | null
  transactions: Transaction[]
  loading?: boolean
  canCreateTransaction?: boolean
  canEditTransaction?: boolean
  canDeleteTransaction?: boolean
  isOwner?: boolean
  onAddTransaction: (type?: 'cashin' | 'cashout') => void
  onAddReceipt: () => void
  onEditTransaction: (txn: Transaction) => void
  onDeleteTransaction: (txn: Transaction) => void
  onSelectTransaction: (txn: Transaction) => void
  onViewAttachment: (txn: Transaction) => void
  onViewAllTransactions: () => void
  onToast: (msg: string) => void
}

export function DashboardPage({
  cashbook,
  transactions,
  loading = false,
  canCreateTransaction = true,
  canEditTransaction = true,
  canDeleteTransaction = true,
  onAddTransaction,
  onAddReceipt,
  onEditTransaction,
  onDeleteTransaction,
  onSelectTransaction,
  onViewAttachment,
  onViewAllTransactions,
  onToast,
}: DashboardPageProps) {
  const cashbookName = cashbook?.cashBookName ?? cashbook?.name ?? 'Active Cashbook'
  const cashbookId = cashbook?.id ?? ''

  // Compute KPIs
  const { totalInflow, totalOutflow, netBalance, count } = useMemo(() => {
    let inSum = 0
    let outSum = 0
    transactions.forEach((t) => {
      const amt = Number(t.amount || 0)
      if (t.transactionType?.toLowerCase() === 'cashin') {
        inSum += amt
      } else {
        outSum += amt
      }
    })
    return {
      totalInflow: inSum,
      totalOutflow: outSum,
      netBalance: inSum - outSum,
      count: transactions.length,
    }
  }, [transactions])

  const recentTransactions = useMemo(() => {
    return transactions.slice(0, 7)
  }, [transactions])

  return (
    <div className="space-y-6 pb-12">
      {/* 1. Page Header with Action Button Group */}
      <PageHeader
        title="Overview"
        description={`Realtime financial ledger & cash flow metrics for ${cashbookName}.`}
        actions={
          <div className="flex items-center gap-2">
            <Gate
              allow={canCreateTransaction}
              reason="Add restricted — ask the cashbook owner for access"
            >
              <div className="flex items-center rounded-lg border border-border/80 p-0.5 bg-background shadow-xs">
                <Button
                  size="sm"
                  variant="ghost"
                  className="h-8 text-xs font-semibold text-inflow hover:text-inflow hover:bg-inflow-muted/50 gap-1.5"
                  onClick={() => onAddTransaction('cashin')}
                >
                  <ArrowDownLeft className="size-3.5 stroke-[2.5]" />
                  <span>+ Cash In</span>
                </Button>
                <div className="h-4 w-px bg-border/80" />
                <Button
                  size="sm"
                  variant="ghost"
                  className="h-8 text-xs font-semibold text-outflow hover:text-outflow hover:bg-outflow-muted/50 gap-1.5"
                  onClick={() => onAddTransaction('cashout')}
                >
                  <ArrowUpRight className="size-3.5 stroke-[2.5]" />
                  <span>− Cash Out</span>
                </Button>
              </div>
              <Button
                size="sm"
                className="h-8 text-xs gap-1.5"
                onClick={onAddReceipt}
              >
                <Plus className="size-3.5" />
                <span>Add Receipt</span>
              </Button>
            </Gate>
          </div>
        }
      />

      {/* 2. KPI Section Cards (4 columns) */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label="Net Balance"
          value={netBalance}
          icon={Wallet}
          formatFn={(v) => formatMoney(v)}
          valueColor={netBalance >= 0 ? 'text-inflow' : 'text-outflow'}
          trend={netBalance >= 0 ? 'up' : 'down'}
          delta={netBalance >= 0 ? 'Positive' : 'Deficit'}
          footer="Cumulative ledger balance"
        />
        <StatCard
          label="Total Cash In"
          value={totalInflow}
          icon={ArrowDownLeft}
          formatFn={(v) => formatMoney(v)}
          valueColor="text-inflow"
          trend="up"
          delta="Inflow"
          footer="Income recorded this period"
        />
        <StatCard
          label="Total Cash Out"
          value={totalOutflow}
          icon={ArrowUpRight}
          formatFn={(v) => formatMoney(v)}
          valueColor="text-outflow"
          trend="down"
          delta="Outflow"
          footer="Expenses recorded this period"
        />
        <StatCard
          label="Ledger Entries"
          value={count}
          icon={Receipt}
          formatFn={(v) => Math.round(v).toLocaleString()}
          footer="Total recorded transactions"
        />
      </div>

      {/* 3. Analytics & Budget Row */}
      <div className="grid grid-cols-1 gap-4 xl:grid-cols-12">
        <div className="xl:col-span-8">
          <CashFlowChart transactions={transactions} />
        </div>
        <div className="xl:col-span-4">
          <MonthlyBudgetCard
            cashbookId={cashbookId}
            cashbookName={cashbookName}
            transactions={transactions}
            canEdit={canCreateTransaction}
            onToast={onToast}
          />
        </div>
      </div>

      {/* 4. Category Breakdown & Recent Transactions */}
      <div className="grid grid-cols-1 gap-4 xl:grid-cols-12">
        <div className="xl:col-span-4">
          <CategoryBreakdownCard
            transactions={transactions}
            onViewAllTransactions={onViewAllTransactions}
          />
        </div>
        <div className="xl:col-span-8">
          <div className="rounded-xl border border-border/80 bg-card p-6 shadow-xs space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <span className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
                  Ledger Activity
                </span>
                <h3 className="text-base font-semibold">Recent Transactions</h3>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-8 text-xs text-muted-foreground hover:text-foreground"
                onClick={onViewAllTransactions}
              >
                View all ({transactions.length}) →
              </Button>
            </div>
            <TransactionsDataTable
              transactions={recentTransactions}
              loading={loading}
              canCreate={canCreateTransaction}
              canEdit={canEditTransaction}
              canDelete={canDeleteTransaction}
              onAddTransaction={() => onAddTransaction()}
              onEditTransaction={onEditTransaction}
              onDeleteTransaction={onDeleteTransaction}
              onSelectTransaction={onSelectTransaction}
              onViewAttachment={onViewAttachment}
              onToast={onToast}
              hideToolbar={true}
              pageSize={7}
            />
          </div>
        </div>
      </div>
    </div>
  )
}
