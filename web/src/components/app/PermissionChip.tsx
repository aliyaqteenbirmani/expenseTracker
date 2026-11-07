import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { Eye, Plus, Pencil, Trash2 } from 'lucide-react'

export type PermissionKind = 'view' | 'add' | 'edit' | 'delete'

interface PermissionChipProps {
  kind: PermissionKind
  className?: string
  size?: 'sm' | 'default'
}

const config: Record<
  PermissionKind,
  { label: string; icon: React.ComponentType<{ className?: string }>; classes: string }
> = {
  view: {
    label: 'View',
    icon: Eye,
    classes: 'border-perm-view/30 bg-inflow-muted/50 text-inflow hover:bg-inflow-muted',
  },
  add: {
    label: 'Add',
    icon: Plus,
    classes: 'border-perm-add/30 bg-blue-500/10 text-blue-600 dark:text-blue-400 hover:bg-blue-500/15',
  },
  edit: {
    label: 'Edit',
    icon: Pencil,
    classes: 'border-warning/30 bg-warning-muted/50 text-amber-600 dark:text-amber-400 hover:bg-warning-muted',
  },
  delete: {
    label: 'Delete',
    icon: Trash2,
    classes: 'border-outflow/30 bg-outflow-muted/50 text-outflow hover:bg-outflow-muted',
  },
}

export function PermissionChip({ kind, className, size = 'default' }: PermissionChipProps) {
  const item = config[kind]
  const Icon = item.icon

  return (
    <Badge
      variant="outline"
      className={cn(
        'font-normal inline-flex items-center gap-1 transition-colors',
        size === 'sm' ? 'px-1.5 py-0 text-[11px]' : 'px-2 py-0.5 text-xs',
        item.classes,
        className
      )}
    >
      <Icon className={size === 'sm' ? 'size-2.5' : 'size-3'} />
      <span>{item.label}</span>
    </Badge>
  )
}
