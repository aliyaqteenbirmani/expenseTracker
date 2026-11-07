import { useMemo, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart'
import { PieChart, Pie, Cell, ResponsiveContainer } from 'recharts'
import { getTransactionCategory, type Transaction } from '@/App'
import { formatMoney } from '@/lib/format'
import { EmptyState } from '@/components/app/EmptyState'
import { PieChart as PieChartIcon, ArrowRight } from 'lucide-react'
import { cn } from '@/lib/utils'

interface CategoryBreakdownCardProps {
  transactions: Transaction[]
  onSelectCategory?: (category: string) => void
  onViewAllTransactions?: () => void
  className?: string
}

const CHART_COLORS = [
  'var(--color-chart-1)',
  'var(--color-chart-2)',
  'var(--color-chart-3)',
  'var(--color-chart-4)',
  'var(--color-chart-5)',
  'var(--color-warning)',
  'var(--color-primary)',
]

export function CategoryBreakdownCard({
  transactions,
  onSelectCategory,
  onViewAllTransactions,
  className,
}: CategoryBreakdownCardProps) {
  const [activeCategory, setActiveCategory] = useState<string | null>(null)

  const expenseTransactions = useMemo(() => {
    return transactions.filter((t) => t.transactionType?.toLowerCase() === 'cashout')
  }, [transactions])

  const totalExpense = useMemo(() => {
    return expenseTransactions.reduce((sum, t) => sum + Number(t.amount || 0), 0)
  }, [expenseTransactions])

  const categoryData = useMemo(() => {
    if (totalExpense === 0) return []

    const map = new Map<string, { label: string; amount: number; count: number }>()

    for (const t of expenseTransactions) {
      const cat = getTransactionCategory(t)
      const current = map.get(cat.label) ?? {
        label: cat.label,
        amount: 0,
        count: 0,
      }
      current.amount += Number(t.amount || 0)
      current.count += 1
      map.set(cat.label, current)
    }

    return Array.from(map.values())
      .map((item, index) => ({
        ...item,
        percentage: Math.round((item.amount / totalExpense) * 100),
        color: CHART_COLORS[index % CHART_COLORS.length],
        fill: CHART_COLORS[index % CHART_COLORS.length],
      }))
      .sort((a, b) => b.amount - a.amount)
  }, [expenseTransactions, totalExpense])

  const chartConfig = useMemo<ChartConfig>(() => {
    const config: ChartConfig = {
      amount: { label: 'Amount' },
    }
    categoryData.forEach((item) => {
      config[item.label] = {
        label: item.label,
        color: item.color,
      }
    })
    return config
  }, [categoryData])

  if (!expenseTransactions.length || totalExpense === 0) {
    return (
      <Card className={cn('p-6 border-border/80 shadow-xs flex flex-col justify-between', className)}>
        <CardHeader className="p-0 pb-3">
          <span className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
            Spending Distribution
          </span>
          <CardTitle className="text-base font-semibold">Category Breakdown</CardTitle>
        </CardHeader>
        <CardContent className="p-0 flex flex-1 items-center justify-center py-6">
          <EmptyState
            icon={PieChartIcon}
            title="No expense data"
            description="Add transactions to visualize spending distribution across categories."
            className="border-0 p-0"
          />
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className={cn('p-6 border-border/80 shadow-xs flex flex-col justify-between', className)}>
      <CardHeader className="p-0 pb-2 flex flex-row items-center justify-between space-y-0">
        <div>
          <span className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
            Spending Distribution
          </span>
          <CardTitle className="text-base font-semibold">Category Breakdown</CardTitle>
        </div>
        {onViewAllTransactions && (
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-xs text-muted-foreground hover:text-foreground"
            onClick={onViewAllTransactions}
          >
            Details <ArrowRight className="size-3 ml-1" />
          </Button>
        )}
      </CardHeader>

      <CardContent className="p-0 space-y-4">
        {/* Donut Chart with center total */}
        <div className="relative h-44 w-full flex items-center justify-center">
          <ChartContainer config={chartConfig} className="h-full w-full">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <ChartTooltip
                  content={
                    <ChartTooltipContent
                      formatter={(val) => formatMoney(Number(val))}
                    />
                  }
                />
                <Pie
                  data={categoryData}
                  dataKey="amount"
                  nameKey="label"
                  innerRadius={50}
                  outerRadius={75}
                  paddingAngle={2}
                  isAnimationActive={true}
                  onClick={(entry: any) => {
                    if (entry && entry.label) {
                      setActiveCategory(entry.label)
                      onSelectCategory?.(entry.label)
                    }
                  }}
                >
                  {categoryData.map((entry, index) => (
                    <Cell
                      key={`cell-${index}`}
                      fill={entry.color}
                      className="cursor-pointer transition-opacity hover:opacity-80"
                    />
                  ))}
                </Pie>
              </PieChart>
            </ResponsiveContainer>
          </ChartContainer>

          {/* Center text */}
          <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
            <span className="text-[10px] font-medium uppercase tracking-wider text-muted-foreground">
              Total Outflow
            </span>
            <span className="font-mono text-sm font-semibold tabular-nums">
              {formatMoney(totalExpense)}
            </span>
          </div>
        </div>

        {/* Legend list */}
        <div className="space-y-1.5 pt-1">
          {categoryData.slice(0, 4).map((item) => {
            const isSelected = activeCategory === item.label
            return (
              <button
                key={item.label}
                type="button"
                onClick={() => {
                  setActiveCategory(isSelected ? null : item.label)
                  onSelectCategory?.(item.label)
                }}
                className={cn(
                  'w-full flex items-center justify-between py-1.5 px-2 rounded-md text-xs transition-colors hover:bg-muted/50 cursor-pointer text-left',
                  isSelected && 'bg-muted ring-1 ring-border'
                )}
              >
                <div className="flex items-center gap-2 truncate">
                  <span
                    className="size-2.5 rounded-full shrink-0"
                    style={{ backgroundColor: item.color }}
                  />
                  <span className="font-medium truncate">{item.label}</span>
                </div>
                <div className="flex items-center gap-2 font-mono tabular-nums shrink-0">
                  <span className="text-muted-foreground">{item.percentage}%</span>
                  <span className="font-medium">{formatMoney(item.amount)}</span>
                </div>
              </button>
            )
          })}
        </div>
      </CardContent>
    </Card>
  )
}
