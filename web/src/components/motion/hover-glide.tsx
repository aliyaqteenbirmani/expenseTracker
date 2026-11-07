import * as React from 'react'
import { motion } from 'motion/react'
import { spring } from '@/lib/motion'
import { cn } from '@/lib/utils'

interface HoverGlideItemProps extends React.HTMLAttributes<HTMLDivElement> {
  activeId?: string | null
  id: string
  onHover?: (id: string) => void
  onLeave?: () => void
  children: React.ReactNode
  layoutId?: string
  className?: string
  activeClassName?: string
}

export function HoverGlideItem({
  activeId,
  id,
  onHover,
  onLeave,
  children,
  layoutId = 'hover-glide',
  className,
  activeClassName,
  ...props
}: HoverGlideItemProps) {
  const isHovered = activeId === id

  return (
    <div
      onMouseEnter={() => onHover?.(id)}
      onMouseLeave={onLeave}
      className={cn('relative z-0', className)}
      {...props}
    >
      {isHovered && (
        <motion.div
          layoutId={layoutId}
          transition={spring.snappy}
          className={cn(
            'absolute inset-0 rounded-md bg-muted/60 pointer-events-none -z-10',
            activeClassName
          )}
        />
      )}
      {children}
    </div>
  )
}
