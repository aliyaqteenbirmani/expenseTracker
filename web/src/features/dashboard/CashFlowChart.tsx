import { useMemo, useState, useRef, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { MotionToggleGroup } from '@/components/motion/motion-toggle-group'
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart'
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  ResponsiveContainer,
} from 'recharts'
import type { Transaction } from '@/App'
import { formatMoney, formatDate } from '@/lib/format'
import { subDays, isAfter, startOfDay } from 'date-fns'
import { cn } from '@/lib/utils'

interface CashFlowChartProps {
  transactions: Transaction[]
  className?: string
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

export function CashFlowChart({ transactions, className }: CashFlowChartProps) {
  const [range, setRange] = useState<'7' | '30' | '90'>('30')
  const hasAnimatedRef = useRef(false)

  useEffect(() => {
    hasAnimatedRef.current = true
  }, [])

  const chartData = useMemo(() => {
    const days = Number(range)
    const cutoff = startOfDay(subDays(new Date(), days))

    // Aggregate by day
    const dayMap = new Map<string, { date: string; displayDate: string; inflow: number; outflow: number }>()

    for (let i = days - 1; i >= 0; i--) {
      const d = subDays(new Date(), i)
      const key = d.toISOString().split('T')[0]
      dayMap.set(key, {
        date: key,
        displayDate: formatDate(d),
        inflow: 0,
        outflow: 0,
      })
    }

    transactions.forEach((t) => {
      if (!t.transactionDate) return
      const d = new Date(t.transactionDate)
      if (isNaN(d.getTime()) || !isAfter(d, cutoff)) return

      const key = d.toISOString().split('T')[0]
      const existing = dayMap.get(key)
      if (existing) {
        const amt = Number(t.amount || 0)
        if (t.transactionType?.toLowerCase() === 'cashin') {
          existing.inflow += amt
        } else {
          existing.outflow += amt
        }
      }
    })

    return Array.from(dayMap.values())
  }, [transactions, range])

  const totals = useMemo(() => {
    return chartData.reduce(
      (acc, item) => ({
        inflow: acc.inflow + item.inflow,
        outflow: acc.outflow + item.outflow,
      }),
      { inflow: 0, outflow: 0 }
    )
  }, [chartData])

  return (
    <Card className={cn('p-6 border-border/80 shadow-xs', className)}>
      <CardHeader className="p-0 pb-4 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between space-y-0">
        <div>
          <span className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
            Analytics
          </span>
          <CardTitle className="text-base font-semibold">Cash Flow</CardTitle>
          <CardDescription className="text-xs text-muted-foreground mt-0.5">
            Inflow vs. Outflow over time
          </CardDescription>
        </div>
        <div className="flex items-center gap-3">
          <div className="hidden lg:flex items-center gap-4 text-xs font-mono">
            <div className="flex items-center gap-1.5">
              <span className="size-2 rounded-full bg-inflow" />
              <span className="text-muted-foreground">In:</span>
              <span className="font-semibold text-inflow">{formatMoney(totals.inflow)}</span>
            </div>
            <div className="flex items-center gap-1.5">
              <span className="size-2 rounded-full bg-outflow" />
              <span className="text-muted-foreground">Out:</span>
              <span className="font-semibold text-outflow">{formatMoney(totals.outflow)}</span>
            </div>
          </div>
          <MotionToggleGroup
            value={range}
            onValueChange={(val) => setRange(val as '7' | '30' | '90')}
            items={[
              { value: '7', label: '7D' },
              { value: '30', label: '30D' },
              { value: '90', label: '90D' },
            ]}
            size="sm"
          />
        </div>
      </CardHeader>

      <CardContent className="p-0">
        <div className="h-64 w-full">
          <ChartContainer config={chartConfig} className="h-full w-full">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={chartData} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                <defs>
                  <linearGradient id="fillInflow" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="var(--color-inflow)" stopOpacity={0.4} />
                    <stop offset="95%" stopColor="var(--color-inflow)" stopOpacity={0.0} />
                  </linearGradient>
                  <linearGradient id="fillOutflow" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="var(--color-outflow)" stopOpacity={0.4} />
                    <stop offset="95%" stopColor="var(--color-outflow)" stopOpacity={0.0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" vertical={false} className="stroke-border/50" />
                <XAxis
                  dataKey="date"
                  tickLine={false}
                  axisLine={false}
                  tickMargin={8}
                  minTickGap={32}
                  tickFormatter={(val) => {
                    const d = new Date(val)
                    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
                  }}
                  className="text-[11px] fill-muted-foreground"
                />
                <YAxis
                  tickLine={false}
                  axisLine={false}
                  tickMargin={8}
                  tickFormatter={(val) => `${(val / 1000).toFixed(0)}k`}
                  className="text-[11px] font-mono fill-muted-foreground"
                />
                <ChartTooltip
                  cursor={{ strokeDasharray: '3 3', stroke: 'var(--color-border)', strokeWidth: 1 }}
                  content={
                    <ChartTooltipContent
                      labelFormatter={(val) => formatDate(val)}
                      formatter={(val, name) => (
                        <div className="flex items-center justify-between gap-2 w-full font-mono">
                          <span className="text-muted-foreground capitalize">{name}:</span>
                          <span className="font-semibold">{formatMoney(Number(val))}</span>
                        </div>
                      )}
                    />
                  }
                />
                <Area
                  type="monotone"
                  dataKey="inflow"
                  stroke="var(--color-inflow)"
                  strokeWidth={2}
                  fillOpacity={1}
                  fill="url(#fillInflow)"
                  isAnimationActive={!hasAnimatedRef.current}
                  animationDuration={600}
                  animationEasing="ease-out"
                />
                <Area
                  type="monotone"
                  dataKey="outflow"
                  stroke="var(--color-outflow)"
                  strokeWidth={2}
                  fillOpacity={1}
                  fill="url(#fillOutflow)"
                  isAnimationActive={!hasAnimatedRef.current}
                  animationDuration={600}
                  animationEasing="ease-out"
                />
              </AreaChart>
            </ResponsiveContainer>
          </ChartContainer>
        </div>
      </CardContent>
    </Card>
  )
}
