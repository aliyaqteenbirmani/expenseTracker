import React from 'react'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { AnimatedNumber } from './AnimatedNumber'
import { TrendingUp, TrendingDown } from 'lucide-react'
import { cn } from '@/lib/utils'

interface StatCardProps {
  label: string
  value: number
  delta?: string
  trend?: 'up' | 'down' | 'neutral'
  icon?: React.ComponentType<{ className?: string }>
  formatFn?: (val: number) => string
  footer?: React.ReactNode
  className?: string
  valueColor?: string
  valueTitle?: string
}

export function StatCard({
  label,
  value,
  delta,
  trend,
  icon: Icon,
  formatFn,
  footer,
  className,
  valueColor,
  valueTitle,
}: StatCardProps) {
  const displayValue = formatFn ? formatFn(value) : value.toLocaleString()

  return (
    <Card
      className={cn(
        '@container relative overflow-hidden border border-border/80 bg-gradient-to-t from-primary/5 to-card p-6 shadow-xs transition-shadow hover:shadow-sm',
        className
      )}
    >
      <CardHeader className="p-0 flex flex-row items-center justify-between space-y-0 pb-2">
        <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
          {label}
        </span>
        {Icon && (
          <div className="flex size-8 items-center justify-center rounded-md bg-muted/60 text-muted-foreground">
            <Icon className="size-4 stroke-[1.75]" />
          </div>
        )}
      </CardHeader>
      <CardContent className="p-0 pt-2">
        <div className="flex flex-wrap items-baseline justify-between gap-x-2 gap-y-1.5 min-w-0">
          <div
            title={valueTitle ?? displayValue}
            className={cn(
              'text-lg @[180px]:text-xl @[240px]:text-2xl @[320px]:text-3xl font-semibold tabular-nums tracking-tight font-mono whitespace-nowrap',
              valueColor
            )}
          >
            <AnimatedNumber value={value} formatFn={formatFn} />
          </div>
          {delta && (
            <Badge
              variant="outline"
              className={cn(
                'font-mono text-xs font-medium shrink-0 inline-flex items-center gap-1',
                trend === 'up'
                  ? 'border-inflow/30 bg-inflow-muted/50 text-inflow'
                  : trend === 'down'
                  ? 'border-outflow/30 bg-outflow-muted/50 text-outflow'
                  : 'text-muted-foreground'
              )}
            >
              {trend === 'up' && <TrendingUp className="size-3" />}
              {trend === 'down' && <TrendingDown className="size-3" />}
              <span>{delta}</span>
            </Badge>
          )}
        </div>
        {footer && (
          <div className="mt-2 text-xs text-muted-foreground">
            {footer}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

