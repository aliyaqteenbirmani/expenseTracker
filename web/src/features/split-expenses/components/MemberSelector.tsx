import * as React from 'react'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import { Button } from '@/components/ui/button'
import { Check, ChevronsUpDown, User as UserIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface MemberOption {
  userId?: string
  id?: string
  fullName?: string
  name?: string
  userName?: string
  email?: string
  role?: string
  isActive?: boolean
}

export function getMemberId(m: MemberOption | undefined | null): string {
  if (!m) return ''
  return m.userId || m.id || ''
}

export function normalizeId(a?: string | null, b?: string | null): boolean {
  if (!a || !b) return false
  return a.trim().toLowerCase() === b.trim().toLowerCase()
}

const UUID_REGEX = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/

export function getMemberDisplayName(m: MemberOption | undefined | null): string {
  if (!m) return ''
  const candidates = [m.fullName, m.name, m.userName, m.email]
  for (const c of candidates) {
    if (c && typeof c === 'string' && c.trim()) {
      const trimmed = c.trim()
      // Never display a raw UUID string as a display name
      if (!UUID_REGEX.test(trimmed)) {
        return trimmed
      }
    }
  }
  // If email was a fallback or matches
  if (m.email && m.email.trim()) {
    return m.email.trim()
  }
  return 'Member'
}

export function getMemberInitials(nameOrEmail?: string | null): string {
  if (!nameOrEmail || !nameOrEmail.trim()) return '?'
  const clean = nameOrEmail.trim()
  if (clean.includes('@')) {
    const local = clean.split('@')[0]
    return (local.slice(0, 2) || '?').toUpperCase()
  }
  const parts = clean.split(/\s+/).filter(Boolean)
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase()
  }
  return ((parts[0][0] || '') + (parts[parts.length - 1][0] || '')).toUpperCase()
}

export interface MemberSelectorProps {
  value?: string
  onValueChange: (userId: string) => void
  members: MemberOption[]
  currentUserId?: string
  placeholder?: string
  disabled?: boolean
  className?: string
  excludeUserIds?: string[]
  id?: string
  required?: boolean
  'aria-label'?: string
}

export function MemberSelector({
  value,
  onValueChange,
  members,
  currentUserId,
  placeholder = 'Select a member',
  disabled = false,
  className,
  excludeUserIds = [],
  id,
  required,
  'aria-label': ariaLabel,
}: MemberSelectorProps) {
  const [open, setOpen] = React.useState(false)

  // Normalize excluded IDs
  const excludedSet = React.useMemo(() => {
    return new Set(excludeUserIds.map((x) => x.toLowerCase().trim()))
  }, [excludeUserIds])

  // Filter available members
  const availableMembers = React.useMemo(() => {
    return members.filter((m) => {
      const mid = getMemberId(m).toLowerCase().trim()
      return !mid || !excludedSet.has(mid)
    })
  }, [members, excludedSet])

  // Find the selected member by normalized ID
  const selectedMember = React.useMemo(() => {
    if (!value) return null
    return members.find((m) => normalizeId(getMemberId(m), value)) || null
  }, [members, value])

  const selectedName = selectedMember ? getMemberDisplayName(selectedMember) : ''
  const selectedInitials = selectedMember ? getMemberInitials(selectedName || selectedMember.email) : ''
  const isSelectedCurrent = selectedMember && currentUserId && normalizeId(getMemberId(selectedMember), currentUserId)

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger
        render={(triggerProps) => (
          <Button
            {...triggerProps}
            id={id}
            type="button"
            variant="outline"
            role="combobox"
            aria-expanded={open}
            aria-required={required}
            aria-label={ariaLabel || placeholder}
            disabled={disabled || members.length === 0}
            className={cn(
              'h-9 w-full justify-between gap-2 px-3 text-left font-normal border-input bg-transparent shadow-xs transition-colors hover:bg-muted/40 focus-visible:ring-1 focus-visible:ring-ring',
              !selectedMember && 'text-muted-foreground',
              className
            )}
          >
            <div className="flex items-center gap-2 truncate min-w-0 flex-1">
              {selectedMember ? (
                <>
                  <div className="flex size-5 shrink-0 items-center justify-center rounded-full bg-primary/10 text-[10px] font-semibold text-primary">
                    {selectedInitials}
                  </div>
                  <span className="truncate text-foreground font-medium">
                    {selectedName}
                  </span>
                  {isSelectedCurrent && (
                    <span className="text-xs text-muted-foreground shrink-0 font-normal">
                      (You)
                    </span>
                  )}
                </>
              ) : (
                <>
                  <UserIcon className="size-4 shrink-0 opacity-40" />
                  <span className="truncate text-muted-foreground">
                    {members.length === 0 ? 'No members available' : placeholder}
                  </span>
                </>
              )}
            </div>
            <ChevronsUpDown className="size-4 shrink-0 opacity-50 ml-1" />
          </Button>
        )}
      />
      <PopoverContent
        className="w-[--trigger-width] min-w-[260px] max-w-[380px] p-0"
        align="start"
      >
        <Command>
          <CommandInput placeholder="Search member by name or email..." />
          <CommandList className="max-h-60 overflow-y-auto">
            <CommandEmpty>No member found.</CommandEmpty>
            <CommandGroup heading="Group Members">
              {availableMembers.map((member) => {
                const memberId = getMemberId(member)
                const isSelected = normalizeId(memberId, value)
                const isCurrent = currentUserId && normalizeId(memberId, currentUserId)
                const name = getMemberDisplayName(member)
                const email = member.email?.trim()
                const initials = getMemberInitials(name || email)

                return (
                  <CommandItem
                    key={memberId}
                    value={`${name} ${email || ''} ${memberId}`}
                    onSelect={() => {
                      onValueChange(memberId)
                      setOpen(false)
                    }}
                    className="flex items-center justify-between gap-2 py-2 px-2.5 cursor-pointer"
                  >
                    <div className="flex items-center gap-2.5 truncate min-w-0 flex-1">
                      <div className="flex size-7 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                        {initials}
                      </div>
                      <div className="flex flex-col truncate min-w-0">
                        <div className="flex items-center gap-1.5 truncate">
                          <span className={cn('truncate text-sm', isSelected && 'font-semibold text-primary')}>
                            {name}
                          </span>
                          {isCurrent && (
                            <span className="rounded bg-muted px-1 py-0.2 text-[10px] font-medium text-muted-foreground shrink-0">
                              You
                            </span>
                          )}
                        </div>
                        {email && email !== name && (
                          <span className="truncate text-xs text-muted-foreground">
                            {email}
                          </span>
                        )}
                      </div>
                    </div>
                    {isSelected && (
                      <Check className="size-4 text-primary shrink-0 ml-1" />
                    )}
                  </CommandItem>
                )
              })}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
