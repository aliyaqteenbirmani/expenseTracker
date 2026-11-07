import { cn } from '@/lib/utils'
import { formatMoney } from '@/lib/format'

interface MoneyTextProps {
  amount: number
  type?: 'inflow' | 'outflow' | 'net' | 'neutral'
  currency?: string
  className?: string
  showSign?: boolean
}

export function MoneyText({
  amount,
  type = 'neutral',
  currency = 'PKR',
  className,
  showSign = true,
}: MoneyTextProps) {
  const isPositive = amount > 0
  const isNegative = amount < 0

  let colorClass = 'text-foreground'
  let sign = ''

  if (type === 'inflow' || (type === 'net' && isPositive)) {
    colorClass = 'text-inflow'
    if (showSign) sign = '+'
  } else if (type === 'outflow' || (type === 'net' && isNegative)) {
    colorClass = 'text-outflow'
    if (showSign) sign = '−'
  }

  const formatted = formatMoney(Math.abs(amount), currency)

  return (
    <span className={cn('font-mono tabular-nums tracking-tight font-medium', colorClass, className)}>
      {sign ? `${sign} ` : ''}
      {formatted}
    </span>
  )
}
