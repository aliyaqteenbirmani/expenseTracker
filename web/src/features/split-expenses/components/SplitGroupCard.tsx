import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { ArrowUpRight, ArrowDownLeft, CheckCircle2, ChevronRight, Crown } from 'lucide-react'
import type { SplitGroupSummaryDto } from '../types'
import { splitLabels, pluralMembers } from '../splitLabels'
import { getGroupVisual } from '../lib/groupVisuals'
import { cn } from '@/lib/utils'

interface SplitGroupCardProps {
  group: SplitGroupSummaryDto
  onClick: () => void
}

export function SplitGroupCard({ group, onClick }: SplitGroupCardProps) {
  const balance = group.currentUserBalance
  const isSettled = Math.abs(balance) < 0.005
  const isOwed = balance > 0.005
  const owes = balance < -0.005

  const visual = getGroupVisual(group.name, group.icon)

  return (
    <Card
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          onClick()
        }
      }}
      className="group cursor-pointer rounded-xl border border-border/80 bg-card p-5 shadow-xs transition-all duration-150 ease-out hover:border-primary/40 hover:shadow-sm active:scale-[0.99] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 flex flex-col justify-between h-full"
    >
      <CardContent className="p-0 flex flex-col justify-between h-full space-y-4">
        {/* Top: Icon, Group Name, Subtitle & Top-Right Total Spend */}
        <div className="space-y-2.5">
          <div className="flex items-start justify-between gap-3">
            {/* Left: Lighter colorful icon & Group Info */}
            <div className="flex items-center gap-3 min-w-0 flex-1">
              <div
                className={cn(
                  'flex size-11 shrink-0 items-center justify-center rounded-xl shadow-xs border text-2xl transition-transform group-hover:scale-105',
                  visual.bgClass
                )}
              >
                {visual.icon ? (
                  <span>{visual.icon}</span>
                ) : (
                  <span className="text-sm font-bold tracking-tight">{visual.initials}</span>
                )}
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-1.5 flex-wrap">
                  <h3
                    className="font-semibold text-base leading-tight text-foreground group-hover:text-primary transition-colors truncate"
                    title={group.name}
                  >
                    {group.name}
                  </h3>
                  {group.currentUserRole === 'Owner' && (
                    <Badge variant="outline" className="text-[10px] px-1.5 py-0 h-4.5 gap-1 border-primary/30 text-primary shrink-0">
                      <Crown className="size-2.5" />
                      Owner
                    </Badge>
                  )}
                </div>
                <p className="text-xs text-muted-foreground truncate mt-0.5">
                  {pluralMembers(group.memberCount)} · Owner: {group.ownerName}
                </p>
              </div>
            </div>

            {/* Top Right: Total Group Spend */}
            <div className="text-right shrink-0">
              <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground block">
                Total Spend
              </span>
              <span className="font-mono text-xs sm:text-sm font-semibold text-foreground tabular-nums">
                Rs. {group.totalExpenses.toLocaleString('en-PK', { minimumFractionDigits: 2 })}
              </span>
            </div>
          </div>

          {/* Group Description if present */}
          {group.description && (
            <p className="text-xs text-muted-foreground line-clamp-2">{group.description}</p>
          )}
        </div>

        {/* Balance Status Section with Soft Color Alert State */}
        <div
          className={cn(
            'space-y-1 rounded-lg p-2.5 border transition-colors',
            owes
              ? 'bg-destructive/[0.05] border-destructive/20 text-destructive dark:bg-destructive/15 dark:border-destructive/30'
              : isOwed
              ? 'bg-inflow/[0.05] border-inflow/20 text-inflow dark:bg-inflow/15 dark:border-inflow/30'
              : 'bg-muted/30 border-border/40 text-muted-foreground'
          )}
        >
          <span
            className={cn(
              'text-[10px] font-semibold uppercase tracking-wider block',
              owes
                ? 'text-destructive/80 dark:text-destructive/90'
                : isOwed
                ? 'text-inflow/80 dark:text-inflow/90'
                : 'text-muted-foreground'
            )}
          >
            {splitLabels.overview.yourBalance.title}
          </span>
          {isSettled && (
            <div className="text-sm font-medium text-muted-foreground flex items-center gap-1.5">
              <CheckCircle2 className="size-4 text-emerald-500/80" />
              <span>Settled up</span>
            </div>
          )}
          {isOwed && (
            <div className="text-sm font-semibold font-mono text-inflow flex items-center gap-1">
              <ArrowUpRight className="size-4 shrink-0" />
              <span>You are owed Rs. {Math.abs(balance).toLocaleString('en-PK', { minimumFractionDigits: 2 })}</span>
            </div>
          )}
          {owes && (
            <div className="text-sm font-semibold font-mono text-destructive flex items-center gap-1">
              <ArrowDownLeft className="size-4 shrink-0" />
              <span>You owe Rs. {Math.abs(balance).toLocaleString('en-PK', { minimumFractionDigits: 2 })}</span>
            </div>
          )}
        </div>

        {/* Footer: View Group link alone at the bottom right */}
        <div className="flex items-center justify-end border-t border-border/60 pt-3 text-xs">
          <div className="flex items-center text-muted-foreground group-hover:text-primary transition-colors font-medium">
            <span>View group</span>
            <ChevronRight className="size-4 ml-0.5 group-hover:translate-x-0.5 transition-transform" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
