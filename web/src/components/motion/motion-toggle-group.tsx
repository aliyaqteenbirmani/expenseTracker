import * as React from 'react'
import { motion } from 'motion/react'
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import { cn } from '@/lib/utils'
import { spring } from '@/lib/motion'

interface MotionToggleGroupProps {
  value: string
  onValueChange: (val: string) => void
  items: Array<{
    value: string
    label: React.ReactNode
    tint?: 'inflow' | 'outflow' | 'default'
  }>
  className?: string
  size?: 'sm' | 'default' | 'lg'
}

export function MotionToggleGroup({
  value,
  onValueChange,
  items,
  className,
  size = 'default',
}: MotionToggleGroupProps) {
  const id = React.useId()

  const activeItem = items.find((i) => i.value === value)
  const pillTint =
    activeItem?.tint === 'inflow'
      ? 'bg-inflow-muted text-inflow border border-inflow/30'
      : activeItem?.tint === 'outflow'
      ? 'bg-outflow-muted text-outflow border border-outflow/30'
      : 'bg-background shadow-xs text-foreground dark:bg-input/50 dark:border dark:border-input'

  return (
    <ToggleGroup
      value={[value]}
      onValueChange={(vals) => {
        const next = Array.isArray(vals) ? vals[0] : vals
        if (next) onValueChange(next)
      }}
      className={cn(
        'relative inline-flex items-center rounded-lg border border-border/80 bg-muted/40 p-0.5',
        className
      )}
    >
      {items.map((item) => {
        const active = item.value === value
        return (
          <ToggleGroupItem
            key={item.value}
            value={item.value}
            size={size}
            className={cn(
              'relative z-10 h-7 text-xs font-medium px-2.5 rounded-md border-0 bg-transparent transition-colors duration-(--duration-fast)',
              'data-[state=on]:bg-transparent data-[state=on]:shadow-none hover:bg-transparent',
              active ? 'text-foreground' : 'text-muted-foreground hover:text-foreground'
            )}
          >
            {active && (
              <motion.span
                layoutId={`${id}-pill`}
                transition={spring.snappy}
                className={cn('absolute inset-0 rounded-md -z-10 transition-colors duration-(--duration-base)', pillTint)}
              />
            )}
            <span className="relative z-10 flex items-center gap-1.5">{item.label}</span>
          </ToggleGroupItem>
        )
      })}
    </ToggleGroup>
  )
}
