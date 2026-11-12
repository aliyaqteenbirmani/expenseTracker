import { useState, useEffect, useRef } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { splitExpensesApi } from '../splitExpensesApi'
import { toast } from 'sonner'
import type {
  SplitGroupMemberDto,
  SplitGroupInvitationDto,
  SplitUserSearchResultDto,
  BatchInviteResultDto,
} from '../types'
import {
  Mail,
  UserPlus,
  Search,
  Plus,
  X,
  Check,
  Loader2,
  CheckCircle2,
  Users,
} from 'lucide-react'

interface InviteeItem {
  userId?: string
  email: string
  name?: string
  isRegistered?: boolean
}

interface InviteMemberDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  groupId: string
  groupName?: string
  existingMembers?: SplitGroupMemberDto[]
  pendingInvitations?: SplitGroupInvitationDto[]
  token?: string
  currentUserId?: string
  currentUserEmail?: string
  onMembersInvited: (result?: BatchInviteResultDto) => void
}

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function getMemberInitials(nameOrEmail: string): string {
  const parts = nameOrEmail.trim().split(/\s+/)
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase()
  }
  return nameOrEmail.slice(0, 2).toUpperCase()
}

export function InviteMemberDialog({
  open,
  onOpenChange,
  groupId,
  groupName,
  existingMembers = [],
  pendingInvitations = [],
  token,
  currentUserId,
  currentUserEmail,
  onMembersInvited,
}: InviteMemberDialogProps) {
  const [memberInput, setMemberInput] = useState('')
  const [invitees, setInvitees] = useState<InviteeItem[]>([])
  const [loading, setLoading] = useState(false)
  const [searchResults, setSearchResults] = useState<SplitUserSearchResultDto[]>([])
  const [searching, setSearching] = useState(false)
  const [showDropdown, setShowDropdown] = useState(false)

  const dropdownRef = useRef<HTMLDivElement>(null)
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  // Reset state when opening/closing
  useEffect(() => {
    if (!open) {
      setMemberInput('')
      setInvitees([])
      setSearchResults([])
      setShowDropdown(false)
    }
  }, [open])

  // Debounced search for registered users
  useEffect(() => {
    const query = memberInput.trim()
    if (query.length < 2) {
      setSearchResults([])
      setSearching(false)
      setShowDropdown(false)
      return
    }

    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current)
    }

    setSearching(true)
    searchTimeoutRef.current = setTimeout(async () => {
      try {
        const results = await splitExpensesApi.searchUsers(query, token)
        setSearchResults(results)
        setShowDropdown(results.length > 0)
      } catch {
        setSearchResults([])
      } finally {
        setSearching(false)
      }
    }, 250)

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current)
      }
    }
  }, [memberInput, token])

  // Close dropdown on outside click
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setShowDropdown(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  // Check if someone is already an active member
  const isAlreadyMember = (userId?: string, email?: string): boolean => {
    return existingMembers.some((m) => {
      if (userId && m.userId === userId) return true
      if (email && m.email.toLowerCase() === email.trim().toLowerCase()) return true
      return false
    })
  }

  // Check if someone already has a pending invitation
  const isAlreadyPending = (userId?: string, email?: string): boolean => {
    return pendingInvitations.some((inv) => {
      if (inv.status !== 'Pending') return false
      if (userId && inv.invitedUserId === userId) return true
      if (email && inv.invitedEmail.toLowerCase() === email.trim().toLowerCase()) return true
      return false
    })
  }

  // Helper to add an invitee with comprehensive client-side checks
  const handleAddInvitee = (item: {
    userId?: string
    email: string
    name?: string
    isRegistered?: boolean
  }) => {
    const cleanEmail = item.email.trim().toLowerCase()

    if (!cleanEmail) {
      toast.error('Please enter an email address')
      return
    }

    if (!EMAIL_REGEX.test(cleanEmail)) {
      toast.error('Please enter a valid email address (e.g. name@example.com)')
      return
    }

    // Check self-invite
    if (
      (currentUserEmail && cleanEmail === currentUserEmail.trim().toLowerCase()) ||
      (currentUserId && item.userId && item.userId === currentUserId)
    ) {
      toast.error('You cannot invite yourself to the group.')
      return
    }

    // Check already an active member
    if (isAlreadyMember(item.userId, cleanEmail)) {
      toast.error(
        `${item.name ? item.name : cleanEmail} is already an active member of this group.`
      )
      return
    }

    // Check already pending invite
    if (isAlreadyPending(item.userId, cleanEmail)) {
      toast.info(
        `An active invitation is already pending for ${item.name ? item.name : cleanEmail}.`
      )
      return
    }

    // Check duplicate in current chip list
    if (invitees.some((i) => i.email.toLowerCase() === cleanEmail)) {
      toast.error('This user is already added to the list.')
      return
    }

    // Check limit
    if (invitees.length >= 20) {
      toast.error('You can invite at most 20 members at a time.')
      return
    }

    setInvitees((prev) => [
      ...prev,
      {
        userId: item.userId,
        email: cleanEmail,
        name: item.name,
        isRegistered: item.isRegistered ?? !!item.userId,
      },
    ])

    setMemberInput('')
    setSearchResults([])
    setShowDropdown(false)
  }

  // Handle typing Enter or Comma in the input
  const handleInputKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault()
      const text = memberInput.trim().replace(/,$/, '')
      if (!text) return

      // If user selected or typed matched user
      const matched = searchResults.find(
        (u) =>
          u.email.toLowerCase() === text.toLowerCase() ||
          u.fullName.toLowerCase() === text.toLowerCase()
      )

      if (matched) {
        handleAddInvitee({
          userId: matched.userId,
          email: matched.email,
          name: matched.fullName,
          isRegistered: true,
        })
      } else {
        handleAddInvitee({
          email: text,
          isRegistered: false,
        })
      }
    }
  }

  const handleRemoveInvitee = (emailToRemove: string) => {
    setInvitees((prev) => prev.filter((i) => i.email !== emailToRemove))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    // If input still has text, try adding it first
    let currentInvitees = [...invitees]
    if (memberInput.trim()) {
      const text = memberInput.trim().replace(/,$/, '')
      const matched = searchResults.find(
        (u) =>
          u.email.toLowerCase() === text.toLowerCase() ||
          u.fullName.toLowerCase() === text.toLowerCase()
      )
      const newItem = matched
        ? {
            userId: matched.userId,
            email: matched.email,
            name: matched.fullName,
            isRegistered: true,
          }
        : {
            email: text,
            isRegistered: false,
          }

      // Check format
      if (EMAIL_REGEX.test(newItem.email.trim().toLowerCase())) {
        const cleanEmail = newItem.email.trim().toLowerCase()
        if (
          !isAlreadyMember(newItem.userId, cleanEmail) &&
          !currentInvitees.some((i) => i.email.toLowerCase() === cleanEmail)
        ) {
          currentInvitees.push({
            userId: newItem.userId,
            email: cleanEmail,
            name: newItem.name,
            isRegistered: newItem.isRegistered,
          })
          setInvitees(currentInvitees)
          setMemberInput('')
        }
      }
    }

    if (currentInvitees.length === 0) {
      toast.error('Please select at least one registered user or enter an email address.')
      return
    }

    try {
      setLoading(true)
      const res = await splitExpensesApi.batchInviteMembers(
        groupId,
        {
          invitees: currentInvitees.map((i) => ({
            userId: i.userId,
            email: i.email,
            name: i.name,
          })),
        },
        token
      )

      // Summary feedback
      if (res.results && res.results.length > 0) {
        const successCount = res.successCount
        const alreadyCount = res.results.filter(
          (r) => r.status === 'AlreadyMember' || r.status === 'AlreadyPending'
        ).length
        const failCount = res.results.filter(
          (r) => r.status !== 'Invited' && r.status !== 'AlreadyMember' && r.status !== 'AlreadyPending'
        ).length

        if (successCount > 0 && failCount === 0 && alreadyCount === 0) {
          toast.success(
            successCount === 1
              ? `Invitation sent to ${res.results[0].identifier}`
              : `Successfully sent ${successCount} invitations`
          )
        } else if (successCount > 0) {
          toast.info(
            `${successCount} invitation(s) sent${
              alreadyCount > 0 ? `, ${alreadyCount} already active/pending` : ''
            }${failCount > 0 ? `, ${failCount} failed` : ''}`
          )
        } else {
          toast.warning(
            `No new invitations sent. ${res.results.map((r) => `${r.identifier}: ${r.message}`).join('; ')}`
          )
        }
      } else {
        toast.success('Invitations processed successfully')
      }

      onOpenChange(false)
      onMembersInvited(res)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to send invitations'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Add Group Members"
      description={
        groupName
          ? `Invite new members to '${groupName}' by registered user search or email.`
          : 'Invite new members to this split group by registered user search or email.'
      }
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Multi-entry Search and Input */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor="invite-members-input">Search Users or Enter Email</Label>
            <span className="text-[11px] text-muted-foreground font-mono">
              {invitees.length}/20 selected
            </span>
          </div>

          <div className="relative" ref={dropdownRef}>
            <div className="flex items-center gap-2">
              <div className="relative flex-1">
                <Search className="size-4 absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="invite-members-input"
                  placeholder="Search registered user or type email..."
                  value={memberInput}
                  onChange={(e) => setMemberInput(e.target.value)}
                  onKeyDown={handleInputKeyDown}
                  onFocus={() => {
                    if (searchResults.length > 0) setShowDropdown(true)
                  }}
                  disabled={loading || invitees.length >= 20}
                  className="pl-9 pr-8"
                  autoFocus
                />
                {searching && (
                  <Loader2 className="size-3.5 absolute right-3 top-1/2 -translate-y-1/2 animate-spin text-muted-foreground" />
                )}
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={loading || !memberInput.trim() || invitees.length >= 20}
                onClick={() => {
                  const text = memberInput.trim().replace(/,$/, '')
                  if (!text) return
                  const matched = searchResults.find(
                    (u) =>
                      u.email.toLowerCase() === text.toLowerCase() ||
                      u.fullName.toLowerCase() === text.toLowerCase()
                  )
                  if (matched) {
                    handleAddInvitee({
                      userId: matched.userId,
                      email: matched.email,
                      name: matched.fullName,
                      isRegistered: true,
                    })
                  } else {
                    handleAddInvitee({
                      email: text,
                      isRegistered: false,
                    })
                  }
                }}
                className="gap-1 h-9 shrink-0"
              >
                <Plus className="size-4" />
                Add
              </Button>
            </div>

            {/* Dropdown with search results */}
            {showDropdown && searchResults.length > 0 && (
              <div className="absolute top-full left-0 right-0 z-50 mt-1 max-h-52 overflow-y-auto rounded-lg border bg-popover text-popover-foreground shadow-lg animate-in fade-in-50 duration-(--duration-fast)">
                <div className="p-1">
                  <div className="text-[10px] font-semibold text-muted-foreground uppercase px-2 py-1">
                    Registered Users
                  </div>
                  {searchResults.map((user) => {
                    const alreadyInGroup = isAlreadyMember(user.userId, user.email)
                    const alreadyPending = isAlreadyPending(user.userId, user.email)
                    const isAlreadyInChips = invitees.some(
                      (i) => i.email.toLowerCase() === user.email.toLowerCase()
                    )
                    const isSelf =
                      user.userId === currentUserId ||
                      (currentUserEmail && user.email.toLowerCase() === currentUserEmail.toLowerCase())
                    const isDisabled = alreadyInGroup || alreadyPending || isAlreadyInChips || isSelf

                    return (
                      <div
                        key={user.userId}
                        onClick={() => {
                          if (!isDisabled) {
                            handleAddInvitee({
                              userId: user.userId,
                              email: user.email,
                              name: user.fullName,
                              isRegistered: true,
                            })
                          }
                        }}
                        className={`flex items-center justify-between p-2 rounded-md text-xs transition-colors ${
                          isDisabled
                            ? 'opacity-60 cursor-not-allowed bg-muted/30'
                            : 'cursor-pointer hover:bg-muted/80'
                        }`}
                      >
                        <div className="flex items-center gap-2.5 truncate">
                          <div className="flex size-7 items-center justify-center rounded-full bg-primary/10 text-primary font-semibold text-xs shrink-0">
                            {getMemberInitials(user.fullName || user.email)}
                          </div>
                          <div className="truncate">
                            <div className="font-medium truncate">{user.fullName}</div>
                            <div className="text-[10px] text-muted-foreground truncate font-mono">
                              {user.email}
                            </div>
                          </div>
                        </div>

                        <div>
                          {alreadyInGroup && (
                            <Badge variant="secondary" className="text-[9px] h-4">
                              Member
                            </Badge>
                          )}
                          {alreadyPending && (
                            <Badge variant="outline" className="text-[9px] h-4">
                              Invited
                            </Badge>
                          )}
                          {isAlreadyInChips && (
                            <Badge variant="outline" className="text-[9px] h-4 gap-0.5">
                              <Check className="size-2.5" /> Selected
                            </Badge>
                          )}
                          {isSelf && (
                            <Badge variant="outline" className="text-[9px] h-4">
                              You (Owner)
                            </Badge>
                          )}
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            )}
          </div>

          {/* Chips list */}
          {invitees.length > 0 && (
            <div className="flex flex-wrap gap-1.5 pt-1">
              {invitees.map((inv) => (
                <div
                  key={inv.email}
                  className="inline-flex items-center gap-1.5 rounded-full border bg-muted/40 px-2.5 py-1 text-xs shadow-xs animate-in zoom-in-95 duration-100"
                >
                  {inv.isRegistered ? (
                    <div className="flex size-4 items-center justify-center rounded-full bg-primary/15 text-primary text-[9px] font-bold">
                      {getMemberInitials(inv.name || inv.email)}
                    </div>
                  ) : (
                    <Mail className="size-3 text-muted-foreground" />
                  )}
                  <span className="font-medium text-foreground">
                    {inv.name || inv.email}
                  </span>
                  {inv.name && (
                    <span className="text-[10px] text-muted-foreground font-mono">
                      ({inv.email})
                    </span>
                  )}
                  {!inv.isRegistered ? (
                    <Badge
                      variant="secondary"
                      className="text-[9px] px-1 py-0 h-4 font-normal bg-amber-500/10 text-amber-700 dark:text-amber-400 border border-amber-500/20"
                    >
                      Invite via email
                    </Badge>
                  ) : (
                    <Badge variant="outline" className="text-[9px] px-1 py-0 h-4 text-primary border-primary/20">
                      Registered
                    </Badge>
                  )}
                  <button
                    type="button"
                    onClick={() => handleRemoveInvitee(inv.email)}
                    className="ml-0.5 rounded-full p-0.5 hover:bg-muted hover:text-foreground text-muted-foreground transition-colors"
                    aria-label={`Remove ${inv.email}`}
                  >
                    <X className="size-3" />
                  </button>
                </div>
              ))}
            </div>
          )}

          {invitees.length === 0 && (
            <p className="text-[11px] text-muted-foreground">
              Type an email address or search registered users by name. Press Enter, comma, or click Add.
            </p>
          )}
        </div>

        {/* Informative Guidance */}
        <div className="rounded-lg border bg-muted/30 p-2.5 text-xs text-muted-foreground space-y-1.5">
          <div className="flex items-start gap-1.5">
            <CheckCircle2 className="size-3.5 text-primary shrink-0 mt-0.5" />
            <span>
              <strong>Registered users</strong> receive an instant in-app notification with Accept/Decline action buttons.
            </span>
          </div>
          <div className="flex items-start gap-1.5">
            <Mail className="size-3.5 text-primary shrink-0 mt-0.5" />
            <span>
              <strong>Unregistered users</strong> receive an email invitation with a secure link to join.
            </span>
          </div>
          <div className="flex items-start gap-1.5">
            <Users className="size-3.5 text-muted-foreground shrink-0 mt-0.5" />
            <span>
              Newly accepted members immediately gain access to view the ledger and be included in future expenses.
            </span>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={loading}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            loading={loading}
            disabled={invitees.length === 0 && !memberInput.trim()}
            className="gap-1.5"
          >
            <UserPlus className="size-4" />
            {invitees.length > 1
              ? `Send ${invitees.length} Invitations`
              : 'Send Invitation'}
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
