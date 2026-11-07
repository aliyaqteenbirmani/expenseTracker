import { cn } from '@/lib/utils'

interface SuccessCheckProps {
  className?: string
  size?: number
}

export function SuccessCheck({ className, size = 48 }: SuccessCheckProps) {
  return (
    <div className={cn('inline-flex items-center justify-center text-inflow', className)}>
      <svg
        width={size}
        height={size}
        viewBox="0 0 52 52"
        fill="none"
        stroke="currentColor"
        strokeWidth={3.5}
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        {/* Circle draws in 300ms */}
        <circle
          cx="26"
          cy="26"
          r="23"
          pathLength={1}
          className="[stroke-dasharray:1] [stroke-dashoffset:1] animate-[draw-stroke_300ms_var(--ease-out)_forwards]"
        />
        {/* Checkmark draws in 200ms after 80ms delay */}
        <path
          d="M15 27l7 7 15-15"
          pathLength={1}
          className="[stroke-dasharray:1] [stroke-dashoffset:1] animate-[draw-stroke_200ms_var(--ease-out)_80ms_forwards]"
        />
      </svg>
    </div>
  )
}
