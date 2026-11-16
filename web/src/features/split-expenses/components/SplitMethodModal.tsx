import { useState } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import { Plus, Minus, AlertCircle, CheckCircle2 } from 'lucide-react'
import type { SplitGroupMemberDto } from '../types'
import { getMemberDisplayName, getMemberInitials } from './MemberSelector'
import { formatMoney } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { SplitMethod } from '../hooks/useSplitCalculator'

interface SplitMethodModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  members: SplitGroupMemberDto[]
  totalAmount: number
  method: SplitMethod
  setMethod: (m: SplitMethod) => void
  equalSelectedIds: string[]
  toggleEqualParticipant: (id: string) => void
  selectAllEqual: () => void
  deselectAllEqual: () => void
  exactValues: Record<string, string>
  setExactValue: (id: string, val: string) => void
  percentageValues: Record<string, string>
  setPercentageValue: (id: string, val: string) => void
  shareParts: Record<string, number>
  incrementSharePart: (id: string) => void
  decrementSharePart: (id: string) => void
  calculatedShares: Record<string, number>
  validation: { isValid: boolean; message: string; remainingAmount?: number; remainingPercentage?: number }
}

const TABS: { id: SplitMethod; label: string; icon: string }[] = [
  { id: 'Equal', label: 'Equally', icon: '=' },
  { id: 'Exact', label: 'Exact Amounts', icon: '123' },
  { id: 'Percentage', label: 'Percentages', icon: '%' },
  { id: 'Shares', label: 'By Shares', icon: '👥' },
]

export function SplitMethodModal({
  open,
  onOpenChange,
  members,
  totalAmount,
  method,
  setMethod,
  equalSelectedIds,
  toggleEqualParticipant,
  selectAllEqual,
  deselectAllEqual,
  exactValues,
  setExactValue,
  percentageValues,
  setPercentageValue,
  shareParts,
  incrementSharePart,
  decrementSharePart,
  calculatedShares,
  validation,
}: SplitMethodModalProps) {
  const activeMembers = members.filter((m) => m.isActive)
  const [activeTab, setActiveTab] = useState<SplitMethod>(method)

  const handleConfirm = () => {
    setMethod(activeTab)
    onOpenChange(false)
  }

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Split Method"
      description={`Choose how to divide ${formatMoney(totalAmount)} across group members.`}
    >
      <div className="space-y-4">
        {/* Method Switcher Buttons */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-1.5 p-1 bg-muted/60 rounded-xl">
          {TABS.map((t) => (
            <button
              key={t.id}
              type="button"
              onClick={() => setActiveTab(t.id)}
              className={cn(
                'flex items-center justify-center gap-1.5 py-2 px-2 text-xs font-semibold rounded-lg transition-all',
                activeTab === t.id
                  ? 'bg-background text-foreground shadow-xs'
                  : 'text-muted-foreground hover:text-foreground hover:bg-background/40'
              )}
            >
              <span className="font-mono opacity-70">{t.icon}</span>
              <span>{t.label}</span>
            </button>
          ))}
        </div>

        {/* Tab 1: Equally */}
        {activeTab === 'Equal' && (
          <div className="space-y-3">
            <div className="flex items-center justify-between text-xs text-muted-foreground px-1">
              <span>Select members involved:</span>
              <div className="space-x-2">
                <button
                  type="button"
                  onClick={selectAllEqual}
                  className="hover:text-foreground font-medium underline-offset-4 hover:underline"
                >
                  All
                </button>
                <span>•</span>
                <button
                  type="button"
                  onClick={deselectAllEqual}
                  className="hover:text-foreground font-medium underline-offset-4 hover:underline"
                >
                  None
                </button>
              </div>
            </div>

            <div className="space-y-1.5 max-h-[45vh] overflow-y-auto">
              {activeMembers.map((member) => {
                const checked = equalSelectedIds.includes(member.userId)
                const share = calculatedShares[member.userId] || 0
                return (
                  <div
                    key={member.userId}
                    onClick={() => toggleEqualParticipant(member.userId)}
                    className={cn(
                      'flex items-center justify-between p-2.5 rounded-xl border transition-all cursor-pointer',
                      checked
                        ? 'border-primary/40 bg-primary/5'
                        : 'border-border/60 hover:bg-muted/40 opacity-70'
                    )}
                  >
                    <div className="flex items-center gap-3">
                      <Checkbox
                        checked={checked}
                        onCheckedChange={() => toggleEqualParticipant(member.userId)}
                        onClick={(e) => e.stopPropagation()}
                      />
                      <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center text-xs font-bold text-muted-foreground">
                        {getMemberInitials(member.fullName || member.email)}
                      </div>
                      <span className="text-sm font-semibold">{getMemberDisplayName(member)}</span>
                    </div>

                    <div className="text-right">
                      <span className="font-mono text-sm font-semibold tabular-nums">
                        {checked ? formatMoney(share) : 'Rs. 0.00'}
                      </span>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        )}

        {/* Tab 2: Exact Amounts */}
        {activeTab === 'Exact' && (
          <div className="space-y-3">
            <p className="text-xs text-muted-foreground">
              Enter the exact amount each member should pay:
            </p>

            <div className="space-y-2 max-h-[45vh] overflow-y-auto pr-1">
              {activeMembers.map((member) => {
                const val = exactValues[member.userId] || ''
                return (
                  <div
                    key={member.userId}
                    className="flex items-center justify-between gap-3 p-2.5 rounded-xl border border-border/60 bg-card"
                  >
                    <div className="flex items-center gap-2.5 min-w-0">
                      <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center text-xs font-bold text-muted-foreground shrink-0">
                        {getMemberInitials(member.fullName || member.email)}
                      </div>
                      <span className="text-sm font-semibold truncate max-w-[120px] sm:max-w-[160px]">
                        {getMemberDisplayName(member)}
                      </span>
                    </div>

                    <div className="flex items-center gap-1.5 shrink-0">
                      <span className="text-xs text-muted-foreground font-mono">Rs.</span>
                      <Input
                        type="number"
                        step="0.01"
                        min="0"
                        placeholder="0.00"
                        value={val}
                        onChange={(e) => setExactValue(member.userId, e.target.value)}
                        className="w-28 h-8 text-right font-mono text-sm tabular-nums"
                      />
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        )}

        {/* Tab 3: Percentages */}
        {activeTab === 'Percentage' && (
          <div className="space-y-3">
            <p className="text-xs text-muted-foreground">
              Enter the percentage each member should pay (must sum to 100%):
            </p>

            <div className="space-y-2 max-h-[45vh] overflow-y-auto pr-1">
              {activeMembers.map((member) => {
                const val = percentageValues[member.userId] || ''
                const computed = calculatedShares[member.userId] || 0
                return (
                  <div
                    key={member.userId}
                    className="flex items-center justify-between gap-3 p-2.5 rounded-xl border border-border/60 bg-card"
                  >
                    <div className="flex items-center gap-2.5 min-w-0">
                      <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center text-xs font-bold text-muted-foreground shrink-0">
                        {getMemberInitials(member.fullName || member.email)}
                      </div>
                      <div className="truncate max-w-[120px] sm:max-w-[160px]">
                        <div className="text-sm font-semibold truncate">
                          {getMemberDisplayName(member)}
                        </div>
                        <div className="text-[11px] font-mono text-muted-foreground">
                          {formatMoney(computed)}
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center gap-1.5 shrink-0">
                      <Input
                        type="number"
                        step="0.1"
                        min="0"
                        max="100"
                        placeholder="0"
                        value={val}
                        onChange={(e) => setPercentageValue(member.userId, e.target.value)}
                        className="w-20 h-8 text-right font-mono text-sm tabular-nums"
                      />
                      <span className="text-xs text-muted-foreground font-mono">%</span>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        )}

        {/* Tab 4: By Shares */}
        {activeTab === 'Shares' && (
          <div className="space-y-3">
            <p className="text-xs text-muted-foreground">
              Assign fractional parts (e.g. 2 for couple/room, 1 for single):
            </p>

            <div className="space-y-2 max-h-[45vh] overflow-y-auto pr-1">
              {activeMembers.map((member) => {
                const parts = shareParts[member.userId] ?? 1
                const computed = calculatedShares[member.userId] || 0
                return (
                  <div
                    key={member.userId}
                    className="flex items-center justify-between gap-3 p-2.5 rounded-xl border border-border/60 bg-card"
                  >
                    <div className="flex items-center gap-2.5 min-w-0">
                      <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center text-xs font-bold text-muted-foreground shrink-0">
                        {getMemberInitials(member.fullName || member.email)}
                      </div>
                      <div className="truncate max-w-[120px] sm:max-w-[160px]">
                        <div className="text-sm font-semibold truncate">
                          {getMemberDisplayName(member)}
                        </div>
                        <div className="text-[11px] font-mono text-muted-foreground">
                          {formatMoney(computed)}
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center gap-2 shrink-0">
                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        className="h-7 w-7 rounded-md"
                        onClick={() => decrementSharePart(member.userId)}
                        disabled={parts <= 0}
                      >
                        <Minus className="h-3 w-3" />
                      </Button>
                      <span className="w-6 text-center font-mono font-bold text-sm tabular-nums">
                        {parts}
                      </span>
                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        className="h-7 w-7 rounded-md"
                        onClick={() => incrementSharePart(member.userId)}
                      >
                        <Plus className="h-3 w-3" />
                      </Button>
                      <span className="text-xs text-muted-foreground min-w-[36px] text-right">
                        {parts === 1 ? 'part' : 'parts'}
                      </span>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        )}

        {/* Live Feedback / Validation Status */}
        <div
          className={cn(
            'flex items-center gap-2 p-2.5 rounded-xl text-xs font-medium border',
            validation.isValid
              ? 'bg-inflow-muted/20 border-inflow/30 text-inflow-foreground'
              : 'bg-outflow-muted/20 border-outflow/30 text-outflow-foreground'
          )}
        >
          {validation.isValid ? (
            <CheckCircle2 className="w-4 h-4 text-inflow shrink-0" />
          ) : (
            <AlertCircle className="w-4 h-4 text-outflow shrink-0" />
          )}
          <span className="truncate">{validation.message}</span>
        </div>

        {/* Modal Actions */}
        <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/60">
          <Button variant="outline" size="sm" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button
            size="sm"
            disabled={!validation.isValid}
            onClick={handleConfirm}
            className="font-semibold"
          >
            Confirm Split
          </Button>
        </div>
      </div>
    </ResponsiveDialog>
  )
}
