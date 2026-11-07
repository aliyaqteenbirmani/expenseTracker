import { useReducedMotion } from 'motion/react'
import { duration } from '@/lib/motion'

/** Returns variants with movement stripped when the user prefers reduced motion. */
export function useMotionSafe() {
  const reduce = useReducedMotion()
  return {
    reduce: !!reduce,
    fadeOnly: {
      hidden: { opacity: 0 },
      show: { opacity: 1, transition: { duration: duration.fast } },
      exit: { opacity: 0 },
    },
  }
}
