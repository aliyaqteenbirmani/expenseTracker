import { useMemo } from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { ArrowRight, Sparkles, HandCoins } from 'lucide-react'
import { simplifyDebts, type SimplifiedTransaction } from '../lib/calculationEngine'
import type { MemberBalanceDto, SplitGroupMemberDto } from '../types'
import { formatMoney } from '@/lib/format'
import { getMemberDisplayName } from './MemberSelector'

interface SimplifiedDebtsCardProps {
  memberBalances: MemberBalanceDto[]
  members: SplitGroupMemberDto[]
  currentUserId?: string
  onSettleUp: (targetUserId?: string, targetAmount?: number) => void
}

export function SimplifiedDebtsCard({
  memberBalances,
  members,
  currentUserId,
  onSettleUp,
}: SimplifiedDebtsCardProps) {
  const simplifiedTransactions = useMemo<SimplifiedTransaction[]>(() => {
    if (!memberBalances || memberBalances.length === 0) return []
    const mapped = memberBalances.map((m) => ({
      userId: m.userId,
      netBalance: m.netBalance,
    }))
    return simplifyDebts(mapped)
  }, [memberBalances])

  const memberMap = useMemo(() => {
    const map = new Map<string, SplitGroupMemberDto>()
    for (const m of members) map.set(m.userId, m)
    return map
  }, [members])

  if (simplifiedTransactions.length === 0) {
    return null
  }

  return (
    <Card className="p-4 sm:p-5 rounded-2xl border-border/70 space-y-3 bg-card/60">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Sparkles className="w-4 h-4 text-primary" />
          <h3 className="text-sm font-semibold text-foreground">Suggested Settlements</h3>
        </div>
        <span className="text-[11px] font-medium text-muted-foreground bg-muted/60 px-2 py-0.5 rounded-full">
          Simplified Debts
        </span>
      </div>

      <p className="text-xs text-muted-foreground">
        Optimized payment paths to clear all group balances with minimum transactions:
      </p>

      <div className="space-y-2 pt-1">
        {simplifiedTransactions.map((tx, idx) => {
          const fromMember = memberMap.get(tx.fromUserId)
          const toMember = memberMap.get(tx.toUserId)

          const isYouFrom = tx.fromUserId === currentUserId
          const isYouTo = tx.toUserId === currentUserId

          const fromName = isYouFrom
            ? 'You'
            : fromMember
            ? getMemberDisplayName(fromMember)
            : 'Member'
          const toName = isYouTo
            ? 'You'
            : toMember
            ? getMemberDisplayName(toMember)
            : 'Member'

          return (
            <div
              key={`${tx.fromUserId}-${tx.toUserId}-${idx}`}
              className="flex items-center justify-between p-2.5 sm:p-3 rounded-xl border border-border/50 bg-background/80 hover:bg-muted/30 transition-colors"
            >
              <div className="flex items-center gap-2 text-xs sm:text-sm min-w-0">
                <span className="font-semibold text-foreground truncate max-w-[90px] sm:max-w-[130px]">
                  {fromName}
                </span>
                <ArrowRight className="w-3.5 h-3.5 text-muted-foreground shrink-0" />
                <span className="font-semibold text-foreground truncate max-w-[90px] sm:max-w-[130px]">
                  {toName}
                </span>
              </div>

              <div className="flex items-center gap-2 shrink-0">
                <span className="font-mono font-bold text-xs sm:text-sm tabular-nums text-foreground">
                  {formatMoney(tx.amount)}
                </span>

                {isYouFrom && (
                  <Button
                    size="sm"
                    variant="default"
                    className="h-7 text-xs px-2.5 font-medium"
                    onClick={() => onSettleUp(tx.toUserId, tx.amount)}
                  >
                    <HandCoins className="w-3 h-3 mr-1" />
                    Pay Now
                  </Button>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </Card>
  )
}
