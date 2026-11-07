import { motion, useReducedMotion, useSpring, useTransform } from 'motion/react'
import { useEffect } from 'react'

interface AnimatedNumberProps {
  value: number
  formatFn?: (val: number) => string
  className?: string
}

export function AnimatedNumber({ value, formatFn, className }: AnimatedNumberProps) {
  const shouldReduceMotion = useReducedMotion()

  const spring = useSpring(value, {
    stiffness: 300,
    damping: 30,
    mass: 0.8,
  })

  useEffect(() => {
    spring.set(value)
  }, [spring, value])

  const display = useTransform(spring, (current) => {
    if (formatFn) return formatFn(current)
    return Math.round(current).toLocaleString()
  })

  if (shouldReduceMotion) {
    return <span className={className}>{formatFn ? formatFn(value) : value.toLocaleString()}</span>
  }

  return <motion.span className={className}>{display}</motion.span>
}
