import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Users, ArrowUpRight, ArrowDownLeft, CheckCircle2, ChevronRight, Crown } from 'lucide-react'
import type { SplitGroupSummaryDto } from '../types'

interface SplitGroupCardProps {
  group: SplitGroupSummaryDto
  onClick: () => void
}

export function SplitGroupCard({ group, onClick }: SplitGroupCardProps) {
  const balance = group.currentUserBalance
  const isSettled = Math.abs(balance) < 0.005
  const isOwed = balance > 0.005
  const owes = balance < -0.005

  return (
    <Card
      onClick={onClick}
      className="group cursor-pointer rounded-xl border border-border bg-card p-4 shadow-xs transition-[border-color,box-shadow,transform] duration-(--duration-fast) ease-out hover:border-primary/40 hover:shadow-sm active:scale-[0.99]"
    >
      <CardContent className="p-0 space-y-3">
        {/* Top Header: Icon, Title, Role */}
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="flex size-11 items-center justify-center rounded-xl bg-muted/60 text-2xl shadow-xs border">
              {group.icon || '👥'}
            </div>
            <div className="space-y-0.5">
              <h3 className="font-semibold text-base leading-tight text-foreground group-hover:text-primary transition-colors flex items-center gap-1.5">
                {group.name}
              </h3>
              <div className="flex items-center gap-2 text-xs text-muted-foreground">
                <span className="flex items-center gap-1">
                  <Users className="size-3" />
                  {group.memberCount} {group.memberCount === 1 ? 'member' : 'members'}
                </span>
                <span>·</span>
                <span className="truncate max-w-[120px]">
                  Owner: {group.ownerName}
                </span>
              </div>
            </div>
          </div>

          {group.currentUserRole === 'Owner' && (
            <Badge variant="outline" className="text-[10px] px-1.5 py-0 h-5 gap-1 border-primary/30 text-primary">
              <Crown className="size-2.5" />
              Owner
            </Badge>
          )}
        </div>

        {/* Group Description if present */}
        {group.description && (
          <p className="text-xs text-muted-foreground line-clamp-1">{group.description}</p>
        )}

        {/* Balance Status Footer */}
        <div className="flex items-center justify-between border-t pt-3 mt-2">
          <div>
            <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground block">
              Your Balance
            </span>
            {isSettled && (
              <span className="text-xs font-medium text-muted-foreground flex items-center gap-1">
                <CheckCircle2 className="size-3.5 text-muted-foreground" />
                Settled up
              </span>
            )}
            {isOwed && (
              <span className="text-xs font-semibold font-mono text-inflow flex items-center gap-0.5">
                <ArrowUpRight className="size-3.5" />
                You get back Rs. {Math.abs(balance).toFixed(2)}
              </span>
            )}
            {owes && (
              <span className="text-xs font-semibold font-mono text-outflow flex items-center gap-0.5">
                <ArrowDownLeft className="size-3.5" />
                You owe Rs. {Math.abs(balance).toFixed(2)}
              </span>
            )}
          </div>

          <div className="flex items-center text-xs text-muted-foreground group-hover:text-foreground transition-colors font-medium">
            <span>View group</span>
            <ChevronRight className="size-4 ml-0.5 group-hover:translate-x-0.5 transition-transform" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
