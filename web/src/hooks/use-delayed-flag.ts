import { useState, useEffect } from 'react'

/**
 * Returns true only after `delayMs` has elapsed while `active` is true.
 * Used for skeletons to avoid flashing skeletons on very fast responses (<150ms).
 */
export function useDelayedFlag(active: boolean, delayMs = 150): boolean {
  const [show, setShow] = useState(false)

  useEffect(() => {
    if (!active) {
      setShow(false)
      return
    }

    const timer = setTimeout(() => {
      setShow(true)
    }, delayMs)

    return () => clearTimeout(timer)
  }, [active, delayMs])

  return show
}
