import * as React from 'react'
import { motion, AnimatePresence } from 'motion/react'
import { pageTransition } from '@/lib/motion'
import { useMotionSafe } from '@/hooks/use-motion-safe'
import { cn } from '@/lib/utils'

interface AnimatedPageProps {
  currentKey: string
  children: React.ReactNode
  className?: string
}

export function AnimatedPage({ currentKey, children, className }: AnimatedPageProps) {
  const { reduce, fadeOnly } = useMotionSafe()
  const variants = reduce ? fadeOnly : pageTransition

  return (
    <AnimatePresence mode="popLayout" initial={false}>
      <motion.div
        key={currentKey}
        variants={variants}
        initial="hidden"
        animate="show"
        exit="exit"
        className={cn('w-full', className)}
      >
        {children}
      </motion.div>
    </AnimatePresence>
  )
}
