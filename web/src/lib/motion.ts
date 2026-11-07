import type { Transition, Variants } from 'motion/react'

export const duration = {
  instant: 0.08,
  fast: 0.14,
  base: 0.2,
  slow: 0.28,
  slower: 0.4,
} as const

export const ease = {
  out: [0.22, 1, 0.36, 1],
  outExpo: [0.16, 1, 0.3, 1],
  in: [0.55, 0, 1, 0.45],
  inOut: [0.65, 0, 0.35, 1],
} as const

export const spring = {
  /** Active pills, nav indicator, segmented controls */
  snappy: { type: 'spring', stiffness: 500, damping: 38, mass: 0.7 },
  /** Layout changes, list reorder, card resize */
  smooth: { type: 'spring', stiffness: 300, damping: 32, mass: 0.9 },
  /** Toggles, small pops */
  bouncy: { type: 'spring', stiffness: 600, damping: 28, mass: 0.6 },
} satisfies Record<string, Transition>

export const fadeUp: Variants = {
  hidden: { opacity: 0, y: 6 },
  show: { opacity: 1, y: 0, transition: { duration: duration.base, ease: ease.out } },
  exit: { opacity: 0, y: -4, transition: { duration: duration.fast, ease: ease.in } },
}

export const fadeScale: Variants = {
  hidden: { opacity: 0, scale: 0.97 },
  show: { opacity: 1, scale: 1, transition: { duration: duration.base, ease: ease.out } },
  exit: { opacity: 0, scale: 0.98, transition: { duration: duration.fast, ease: ease.in } },
}

export const stagger = (step = 0.035, delay = 0): Variants => ({
  hidden: {},
  show: { transition: { staggerChildren: step, delayChildren: delay } },
})

export const pageTransition: Variants = {
  hidden: { opacity: 0, y: 8, filter: 'blur(2px)' },
  show: { opacity: 1, y: 0, filter: 'blur(0px)', transition: { duration: duration.slow, ease: ease.out } },
  exit: { opacity: 0, transition: { duration: duration.fast, ease: ease.in } },
}

/** Row insert/remove for realtime tables */
export const rowPresence: Variants = {
  hidden: { opacity: 0, height: 0 },
  show: { opacity: 1, height: 'auto', transition: { height: spring.smooth, opacity: { duration: duration.base } } },
  exit: { opacity: 0, height: 0, transition: { duration: duration.fast, ease: ease.in } },
}

// Backward-compatible aliases for legacy imports
export const springTransition = spring.smooth
export const smoothTransition: Transition = { duration: duration.slow, ease: ease.outExpo }
export const fastTransition: Transition = { duration: duration.fast, ease: ease.outExpo }
export const fadeUpVariants = fadeUp
export const staggerContainer = stagger(0.04, 0.02)
export const rowVariants = rowPresence
