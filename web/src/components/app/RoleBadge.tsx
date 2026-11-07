import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { Crown, ShieldCheck } from 'lucide-react'

interface RoleBadgeProps {
  role: 'owner' | 'member' | string
  className?: string
  size?: 'sm' | 'default'
}

export function RoleBadge({ role, className, size = 'default' }: RoleBadgeProps) {
  const isOwner = role.toLowerCase().includes('owner')

  if (isOwner) {
    return (
      <Badge
        variant="outline"
        className={cn(
          'inline-flex items-center gap-1 border-owner/40 bg-owner-muted/60 text-amber-700 dark:text-amber-400 font-medium',
          size === 'sm' ? 'px-1.5 py-0 text-[11px]' : 'px-2 py-0.5 text-xs',
          className
        )}
      >
        <Crown className={size === 'sm' ? 'size-2.5 text-amber-600 dark:text-amber-400' : 'size-3 text-amber-600 dark:text-amber-400'} />
        <span>Owner</span>
      </Badge>
    )
  }

  return (
    <Badge
      variant="outline"
      className={cn(
        'inline-flex items-center gap-1 border-blue-500/30 bg-blue-500/10 text-blue-700 dark:text-blue-300 font-medium',
        size === 'sm' ? 'px-1.5 py-0 text-[11px]' : 'px-2 py-0.5 text-xs',
        className
      )}
    >
      <ShieldCheck className={size === 'sm' ? 'size-2.5 text-blue-600 dark:text-blue-400' : 'size-3 text-blue-600 dark:text-blue-400'} />
      <span>Member</span>
    </Badge>
  )
}
