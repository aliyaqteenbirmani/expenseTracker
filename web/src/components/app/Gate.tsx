import React from 'react'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { Lock } from 'lucide-react'
import { cn } from '@/lib/utils'

interface GateProps {
  allow: boolean
  reason?: string
  fallback?: React.ReactNode
  hideIfDisallowed?: boolean
  children: React.ReactNode
  className?: string
}

export function Gate({
  allow,
  reason = 'Restricted — ask the cashbook owner for access',
  fallback,
  hideIfDisallowed = false,
  children,
  className,
}: GateProps) {
  if (allow) {
    return <>{children}</>
  }

  if (hideIfDisallowed) {
    return fallback ? <>{fallback}</> : null
  }

  if (fallback) {
    return <>{fallback}</>
  }

  return (
    <Tooltip>
      <TooltipTrigger
        className={cn('inline-flex items-center cursor-not-allowed opacity-60 text-left', className)}
      >
        <span className="pointer-events-none inline-flex items-center">
          {children}
        </span>
      </TooltipTrigger>
      <TooltipContent side="top">
        <div className="flex items-center gap-1.5 text-xs">
          <Lock className="size-3 shrink-0 text-amber-400" />
          <span>{reason}</span>
        </div>
      </TooltipContent>
    </Tooltip>
  )
}
