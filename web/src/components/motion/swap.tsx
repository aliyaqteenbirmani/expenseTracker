import * as React from 'react'
import { AnimatePresence, motion } from 'motion/react'
import { duration, ease } from '@/lib/motion'
import { cn } from '@/lib/utils'

interface SwapProps {
  k: string | number
  children: React.ReactNode
  className?: string
  direction?: 'up' | 'down'
}

export function Swap({ k, children, className, direction = 'down' }: SwapProps) {
  const yOffset = direction === 'down' ? 6 : -6

  return (
    <AnimatePresence mode="popLayout" initial={false}>
      <motion.span
        key={k}
        initial={{ opacity: 0, y: -yOffset, filter: 'blur(2px)' }}
        animate={{ opacity: 1, y: 0, filter: 'blur(0px)' }}
        exit={{ opacity: 0, y: yOffset, filter: 'blur(2px)' }}
        transition={{
          duration: duration.fast,
          ease: ease.out,
        }}
        className={cn('inline-flex items-center justify-center', className)}
      >
        {children}
      </motion.span>
    </AnimatePresence>
  )
}
