import NumberFlow, { type Format } from '@number-flow/react'
import { cn } from '@/lib/utils'

interface AnimatedNumberProps {
  value: number
  format?: Format
  formatFn?: (val: number) => string
  className?: string
  currency?: string
}

export function AnimatedNumber({
  value,
  format,
  formatFn,
  className,
  currency,
}: AnimatedNumberProps) {
  // If a custom string formatter is provided and no direct NumberFlow format options
  if (formatFn && !format && !currency) {
    return (
      <span className={cn('tabular-nums font-mono', className)}>
        {formatFn(value)}
      </span>
    )
  }

  const formatOptions: Format = format ?? (currency ? {
    style: 'currency',
    currency,
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  } : {
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  })

  return (
    <NumberFlow
      value={value}
      format={formatOptions}
      transformTiming={{
        duration: 450,
        easing: 'cubic-bezier(0.22, 1, 0.36, 1)',
      }}
      className={cn('tabular-nums font-mono', className)}
    />
  )
}
