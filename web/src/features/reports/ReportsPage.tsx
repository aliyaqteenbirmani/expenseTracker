import { useMemo, useState } from 'react'
import { PageHeader } from '@/components/app/PageHeader'
import { StatCard } from '@/components/app/StatCard'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  ResponsiveContainer,
} from 'recharts'
import {
  ArrowLeft,
  ArrowDownLeft,
  ArrowUpRight,
  Wallet,
  TrendingUp,
  TrendingDown,
  Download,
  FileSpreadsheet,
  FileText,
  Printer,
  BookOpen,
} from 'lucide-react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { getTransactionCategory, type Cashbook } from '@/App'
import { FinancialStatementModal, type CategorySummary, type ReportTransaction } from './FinancialStatementModal'
import { formatMoney } from '@/lib/format'
import { subDays, isAfter, startOfDay, getMonth, getYear } from 'date-fns'

interface ReportsPageProps {
  transactions: ReportTransaction[]
  cashbookName: string
  businessName: string
  books?: Cashbook[]
  selectedBookId?: string
  onSelectBook?: (book: Cashbook) => void
  onBack: () => void
  onToast: (msg: string) => void
}

const chartConfig: ChartConfig = {
  inflow: {
    label: 'Cash In',
    color: 'var(--color-inflow)',
  },
  outflow: {
    label: 'Cash Out',
    color: 'var(--color-outflow)',
  },
}

export function ReportsPage({
  transactions,
  cashbookName,
  businessName,
  books,
  selectedBookId,
  onSelectBook,
  onBack,
  onToast,
}: ReportsPageProps) {
  const [period, setPeriod] = useState<'30' | '90' | 'year' | 'all'>('30')
  const [showStatement, setShowStatement] = useState(false)

  // Filter transactions by period
  const filteredTransactions = useMemo(() => {
    if (period === 'all') return transactions
    const now = new Date()
    const days = period === '30' ? 30 : period === '90' ? 90 : 365
    const cutoff = startOfDay(subDays(now, days))

    return transactions.filter((t) => {
      if (!t.transactionDate) return false
      const d = new Date(t.transactionDate)
      return !isNaN(d.getTime()) && isAfter(d, cutoff)
    })
  }, [transactions, period])

  // Financial calculations
  const { totalIncome, totalExpense, netBalance, savingsRate } = useMemo(() => {
    let income = 0
    let expense = 0
    filteredTransactions.forEach((t) => {
      const amt = Number(t.amount || 0)
      if (t.transactionType?.toLowerCase() === 'cashin') {
        income += amt
      } else {
        expense += amt
      }
    })
    const net = income - expense
    const rate = income > 0 ? Math.round((net / income) * 100) : 0
    return {
      totalIncome: income,
      totalExpense: expense,
      netBalance: net,
      savingsRate: rate,
    }
  }, [filteredTransactions])

  // Category breakdown
  const categoryBreakdown = useMemo<CategorySummary[]>(() => {
    const expenseTxns = filteredTransactions.filter(
      (t) => t.transactionType?.toLowerCase() === 'cashout'
    )
    if (!expenseTxns.length || totalExpense === 0) return []

    const map = new Map<string, { label: string; amount: number; count: number }>()

    expenseTxns.forEach((t) => {
      const cat = getTransactionCategory(t as any)
      const current = map.get(cat.label) ?? { label: cat.label, amount: 0, count: 0 }
      current.amount += Number(t.amount || 0)
      current.count += 1
      map.set(cat.label, current)
    })

    return Array.from(map.values())
      .map((item) => ({
        ...item,
        percentage: Math.round((item.amount / totalExpense) * 100),
      }))
      .sort((a, b) => b.amount - a.amount)
  }, [filteredTransactions, totalExpense])

  // Monthly aggregated data for bar chart
  const monthlyChartData = useMemo(() => {
    const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
    const dataMap = new Map<string, { month: string; inflow: number; outflow: number }>()

    filteredTransactions.forEach((t) => {
      if (!t.transactionDate) return
      const d = new Date(t.transactionDate)
      if (isNaN(d.getTime())) return
      const key = `${monthNames[getMonth(d)]} ${getYear(d)}`
      const current = dataMap.get(key) ?? { month: key, inflow: 0, outflow: 0 }
      const amt = Number(t.amount || 0)
      if (t.transactionType?.toLowerCase() === 'cashin') {
        current.inflow += amt
      } else {
        current.outflow += amt
      }
      dataMap.set(key, current)
    })

    return Array.from(dataMap.values())
  }, [filteredTransactions])

  const exportCSV = () => {
    if (!filteredTransactions.length) return
    const headers = ['ID', 'Date', 'Type', 'Amount', 'Remarks']
    const rows = filteredTransactions.map((t) => [
      t.id,
      t.transactionDate || '',
      t.transactionType || '',
      String(t.amount || 0),
      `"${(t.remarks || '').replace(/"/g, '""')}"`,
    ])
    const csvContent =
      'data:text/csv;charset=utf-8,' +
      [headers.join(','), ...rows.map((e) => e.join(','))].join('\n')
    const encodedUri = encodeURI(csvContent)
    const link = document.createElement('a')
    link.setAttribute('href', encodedUri)
    link.setAttribute('download', `financial_report_${period}d.csv`)
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    onToast('Exported CSV report')
  }

  const periodLabel =
    period === '30'
      ? 'Past 30 Days'
      : period === '90'
      ? 'Past 90 Days'
      : period === 'year'
      ? 'Past 12 Months'
      : 'All-Time Record'

  return (
    <div className="space-y-6 pb-12">
      <PageHeader
        title="Financial Reports"
        description={`Performance analytics, operating margins and statement ledger for ${cashbookName}.`}
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
            {books && books.length > 1 && onSelectBook && (
              <Select
                value={selectedBookId}
                onValueChange={(val) => {
                  const b = books.find((x) => x.id === val)
                  if (b) onSelectBook(b)
                }}
              >
                <SelectTrigger className="h-8 text-xs w-44">
                  <BookOpen className="size-3.5 mr-1 text-muted-foreground" />
                  <SelectValue placeholder="Select cashbook" />
                </SelectTrigger>
                <SelectContent>
                  {books.map((b) => (
                    <SelectItem key={b.id} value={b.id} className="text-xs">
                      {b.cashBookName ?? b.name ?? 'Cashbook'}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}

            <Select value={period} onValueChange={(v) => setPeriod(v as any)}>
              <SelectTrigger className="h-8 text-xs w-36">
                <SelectValue placeholder="Period" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="30" className="text-xs">
                  Last 30 Days
                </SelectItem>
                <SelectItem value="90" className="text-xs">
                  Last 90 Days
                </SelectItem>
                <SelectItem value="year" className="text-xs">
                  Last 12 Months
                </SelectItem>
                <SelectItem value="all" className="text-xs">
                  All Records
                </SelectItem>
              </SelectContent>
            </Select>

            <DropdownMenu>
              <DropdownMenuTrigger
                render={(props) => (
                  <Button {...props} variant="outline" size="sm" className="h-8 text-xs gap-1.5">
                    <Download className="size-3.5" />
                    <span>Export</span>
                  </Button>
                )}
              />
              <DropdownMenuContent align="end" className="w-48">
                <DropdownMenuItem onClick={exportCSV}>
                  <FileSpreadsheet className="size-4 mr-2" />
                  <span>Export CSV</span>
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => setShowStatement(true)}>
                  <FileText className="size-4 mr-2" />
                  <span>Financial Statement</span>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            <Button
              size="sm"
              onClick={() => setShowStatement(true)}
              className="h-8 text-xs gap-1.5"
            >
              <Printer className="size-3.5" />
              <span>Statement</span>
            </Button>
          </div>
        }
      />

      {/* KPI Row (4 Cards) */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label="Total Revenue / Inflow"
          value={totalIncome}
          icon={ArrowDownLeft}
          formatFn={(v) => formatMoney(v)}
          valueColor="text-inflow"
          trend="up"
          delta="Revenue"
          footer={periodLabel}
        />
        <StatCard
          label="Total Operating Outflow"
          value={totalExpense}
          icon={ArrowUpRight}
          formatFn={(v) => formatMoney(v)}
          valueColor="text-outflow"
          trend="down"
          delta="Expenses"
          footer={periodLabel}
        />
        <StatCard
          label="Net Cash Surplus"
          value={netBalance}
          icon={Wallet}
          formatFn={(v) => formatMoney(v)}
          valueColor={netBalance >= 0 ? 'text-inflow' : 'text-outflow'}
          trend={netBalance >= 0 ? 'up' : 'down'}
          delta={netBalance >= 0 ? 'Surplus' : 'Deficit'}
          footer="Operating profit"
        />
        <StatCard
          label="Operating Savings Margin"
          value={savingsRate}
          icon={savingsRate >= 0 ? TrendingUp : TrendingDown}
          formatFn={(v) => `${Math.round(v)}%`}
          valueColor={savingsRate >= 0 ? 'text-inflow' : 'text-outflow'}
          footer="Margin of total income"
        />
      </div>

      {/* Tabs Layout */}
      <Tabs defaultValue="cashflow" className="space-y-4">
        <TabsList className="h-9">
          <TabsTrigger value="cashflow" className="text-xs px-3">
            Income vs Expense Comparison
          </TabsTrigger>
          <TabsTrigger value="categories" className="text-xs px-3">
            Category Breakdown
          </TabsTrigger>
        </TabsList>

        <TabsContent value="cashflow" className="space-y-4">
          <Card className="p-6 border-border/80 shadow-xs">
            <CardHeader className="p-0 pb-4">
              <span className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
                Monthly Breakdown
              </span>
              <CardTitle className="text-base font-semibold">Grouped Inflow vs Outflow</CardTitle>
              <CardDescription className="text-xs text-muted-foreground">
                Comparative bar distribution across financial periods.
              </CardDescription>
            </CardHeader>
            <CardContent className="p-0">
              <div className="h-72 w-full">
                <ChartContainer config={chartConfig} className="h-full w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={monthlyChartData} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} className="stroke-border/50" />
                      <XAxis
                        dataKey="month"
                        tickLine={false}
                        axisLine={false}
                        className="text-[11px] fill-muted-foreground"
                      />
                      <YAxis
                        tickLine={false}
                        axisLine={false}
                        tickFormatter={(val) => `${(val / 1000).toFixed(0)}k`}
                        className="text-[11px] font-mono fill-muted-foreground"
                      />
                      <ChartTooltip
                        content={
                          <ChartTooltipContent
                            formatter={(val, name) => (
                              <div className="flex items-center justify-between gap-2 w-full font-mono">
                                <span className="text-muted-foreground capitalize">{name}:</span>
                                <span className="font-semibold">{formatMoney(Number(val))}</span>
                              </div>
                            )}
                          />
                        }
                      />
                      <Bar dataKey="inflow" fill="var(--color-inflow)" radius={[4, 4, 0, 0]} />
                      <Bar dataKey="outflow" fill="var(--color-outflow)" radius={[4, 4, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                </ChartContainer>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="categories" className="space-y-4">
          <Card className="border-border/80 shadow-xs">
            <CardHeader className="p-6 pb-3">
              <CardTitle className="text-base font-semibold">Spending by Category</CardTitle>
              <CardDescription className="text-xs text-muted-foreground">
                Itemized distribution of expenses in {periodLabel}.
              </CardDescription>
            </CardHeader>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow className="border-b">
                    <TableHead className="text-xs font-semibold">Category</TableHead>
                    <TableHead className="text-right text-xs font-semibold">Transactions</TableHead>
                    <TableHead className="text-right text-xs font-semibold">Share</TableHead>
                    <TableHead className="text-right text-xs font-semibold">Total Amount</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {categoryBreakdown.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={4} className="h-24 text-center text-xs text-muted-foreground">
                        No expense records in this reporting period.
                      </TableCell>
                    </TableRow>
                  ) : (
                    categoryBreakdown.map((cat) => (
                      <TableRow key={cat.label} className="h-12 hover:bg-muted/40 text-xs">
                        <TableCell className="font-medium">{cat.label}</TableCell>
                        <TableCell className="text-right font-mono">{cat.count} entries</TableCell>
                        <TableCell className="text-right font-mono">
                          <Badge variant="secondary" className="text-[10px] px-1.5 py-0">
                            {cat.percentage}%
                          </Badge>
                        </TableCell>
                        <TableCell className="text-right font-mono font-semibold">
                          {formatMoney(cat.amount)}
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Financial Statement Modal */}
      <FinancialStatementModal
        open={showStatement}
        onOpenChange={setShowStatement}
        businessName={businessName}
        cashbookName={cashbookName}
        periodLabel={periodLabel}
        totalIncome={totalIncome}
        totalExpense={totalExpense}
        netBalance={netBalance}
        savingsRate={savingsRate}
        categoryBreakdown={categoryBreakdown}
        transactions={filteredTransactions}
      />
    </div>
  )
}
