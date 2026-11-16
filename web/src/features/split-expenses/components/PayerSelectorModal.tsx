import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Check } from 'lucide-react'
import type { SplitGroupMemberDto } from '../types'
import { getMemberDisplayName, getMemberInitials } from './MemberSelector'
import { cn } from '@/lib/utils'

interface PayerSelectorModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  members: SplitGroupMemberDto[]
  selectedPayerId: string
  currentUserId?: string
  onSelectPayer: (userId: string) => void
}

export function PayerSelectorModal({
  open,
  onOpenChange,
  members,
  selectedPayerId,
  currentUserId,
  onSelectPayer,
}: PayerSelectorModalProps) {
  const activeMembers = members.filter((m) => m.isActive)

  const handleSelect = (userId: string) => {
    onSelectPayer(userId)
    onOpenChange(false)
  }

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Who paid?"
      description="Choose the group member who paid for this expense."
    >
      <div className="space-y-1.5 py-1 max-h-[60vh] overflow-y-auto">
        {activeMembers.map((member) => {
          const isSelected = member.userId === selectedPayerId
          const isYou = member.userId === currentUserId
          const displayName = `${getMemberDisplayName(member)}${isYou ? ' (You)' : ''}`
          const initials = getMemberInitials(member.fullName || member.email)

          return (
            <button
              key={member.userId}
              type="button"
              onClick={() => handleSelect(member.userId)}
              className={cn(
                'w-full flex items-center justify-between p-3 rounded-xl border transition-all text-left',
                isSelected
                  ? 'border-primary bg-primary/5 text-primary'
                  : 'border-border/60 hover:bg-muted/40 text-foreground'
              )}
            >
              <div className="flex items-center gap-3">
                <div
                  className={cn(
                    'w-9 h-9 rounded-full flex items-center justify-center text-xs font-bold shrink-0',
                    isSelected ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'
                  )}
                >
                  {initials}
                </div>
                <div>
                  <div className="font-semibold text-sm leading-tight">{displayName}</div>
                  <div className="text-xs text-muted-foreground mt-0.5">{member.email}</div>
                </div>
              </div>

              {isSelected && <Check className="w-4 h-4 text-primary shrink-0" />}
            </button>
          )
        })}
      </div>

      <div className="pt-2 flex justify-end">
        <Button variant="outline" size="sm" onClick={() => onOpenChange(false)}>
          Cancel
        </Button>
      </div>
    </ResponsiveDialog>
  )
}
