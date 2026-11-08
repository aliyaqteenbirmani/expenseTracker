import { useEffect, useState } from 'react'
import { motion, AnimatePresence } from 'motion/react'
import { duration, ease } from '@/lib/motion'

interface RouteProgressBarProps {
  loading: boolean
}

export function RouteProgressBar({ loading }: RouteProgressBarProps) {
  const [visible, setVisible] = useState(false)
  const [progress, setProgress] = useState(0)

  useEffect(() => {
    let timer: number | null = null

    if (loading) {
      setVisible(true)
      setProgress(25)

      timer = window.setTimeout(() => {
        setProgress(65)
      }, 150)
    } else {
      if (visible) {
        setProgress(100)
        timer = window.setTimeout(() => {
          setVisible(false)
          setProgress(0)
        }, 250)
      }
    }

    return () => {
      if (timer) clearTimeout(timer)
    }
  }, [loading, visible])

  return (
    <AnimatePresence>
      {visible && (
        <div className="fixed top-0 left-0 right-0 z-50 h-[2px] overflow-hidden pointer-events-none">
          <motion.div
            initial={{ scaleX: 0 }}
            animate={{ scaleX: progress / 100 }}
            exit={{ opacity: 0 }}
            transition={{
              scaleX: { duration: duration.base, ease: ease.out },
              opacity: { duration: duration.fast },
            }}
            style={{ transformOrigin: '0% 50%' }}
            className="h-full w-full bg-primary"
          />
        </div>
      )}
    </AnimatePresence>
  )
}
