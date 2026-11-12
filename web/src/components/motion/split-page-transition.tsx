import * as React from 'react'
import { motion, type Variants } from 'motion/react'
import { duration, ease } from '@/lib/motion'
import { useMotionSafe } from '@/hooks/use-motion-safe'
import { cn } from '@/lib/utils'

export const splitTransitionConfig = {
  /** Page entrance duration (280ms) */
  enterDuration: duration.slow,
  /** Page exit duration (140ms) */
  exitDuration: duration.fast,
  ease: ease.out,
  exitEase: ease.in,
} as const

/**
 * Polished, brief page entrance and exit variants.
 * Uses GPU-accelerated opacity, subtle Y translation (6px), and scale (0.99 -> 1).
 */
export const splitPageVariants: Variants = {
  hidden: {
    opacity: 0,
    y: 6,
    scale: 0.99,
  },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: {
      duration: splitTransitionConfig.enterDuration,
      ease: splitTransitionConfig.ease,
    },
  },
  exit: {
    opacity: 0,
    y: -4,
    scale: 0.995,
    transition: {
      duration: splitTransitionConfig.exitDuration,
      ease: splitTransitionConfig.exitEase,
    },
  },
}

/** Stagger container for lists, grids, and cards */
export const splitStaggerContainer: Variants = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.035,
      delayChildren: 0.02,
    },
  },
}

/** Card and row entrance variant for staggered items */
export const splitCardVariants: Variants = {
  hidden: { opacity: 0, y: 8, scale: 0.98 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: {
      duration: duration.base,
      ease: ease.out,
    },
  },
  exit: {
    opacity: 0,
    scale: 0.98,
    transition: {
      duration: duration.fast,
      ease: ease.in,
    },
  },
}

export interface SplitPageTransitionProps {
  children: React.ReactNode
  className?: string
  layoutId?: string
}

/**
 * Reusable motion wrapper for Split Expenses views.
 * Respects prefers-reduced-motion by falling back to pure opacity fade.
 */
export function SplitPageTransition({
  children,
  className,
  layoutId,
}: SplitPageTransitionProps) {
  const { reduce, fadeOnly } = useMotionSafe()
  const variants = reduce ? fadeOnly : splitPageVariants

  return (
    <motion.div
      layoutId={layoutId}
      variants={variants}
      initial="hidden"
      animate="show"
      exit="exit"
      className={cn('w-full', className)}
    >
      {children}
    </motion.div>
  )
}
