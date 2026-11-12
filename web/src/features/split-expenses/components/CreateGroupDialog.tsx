import { useState, useEffect, useRef } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { splitExpensesApi } from '../splitExpensesApi'
import { toast } from 'sonner'
import { getMemberInitials } from './MemberSelector'
import type {
  CreateSplitGroupResultDto,
  SplitUserSearchResultDto,
} from '../types'
import {
  Plus,
  X,
  Mail,
  Search,
  Check,
  Loader2,
  Users,
} from 'lucide-react'

interface CreateGroupDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  token?: string
  currentUserId?: string
  currentUserEmail?: string
  onGroupCreated: (newGroup: CreateSplitGroupResultDto) => void
}

export interface InviteeChip {
  userId?: string
  email: string
  name?: string
  isRegistered?: boolean
}

const PRESET_ICONS = ['👥', '🏖️', '🏠', '🍔', '🚗', '✈️', '💼', '🎉', '☕', '🛒']
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function CreateGroupDialog({
  open,
  onOpenChange,
  token,
  currentUserId,
  currentUserEmail,
  onGroupCreated,
}: CreateGroupDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selectedIcon, setSelectedIcon] = useState('👥')
  const [loading, setLoading] = useState(false)

  // Multi-entry invitees state
  const [invitees, setInvitees] = useState<InviteeChip[]>([])
  const [memberInput, setMemberInput] = useState('')
  const [searchResults, setSearchResults] = useState<SplitUserSearchResultDto[]>([])
  const [searching, setSearching] = useState(false)
  const [showDropdown, setShowDropdown] = useState(false)
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Reset state when dialog closes
  useEffect(() => {
    if (!open) {
      setName('')
      setDescription('')
      setSelectedIcon('👥')
      setInvitees([])
      setMemberInput('')
      setSearchResults([])
      setShowDropdown(false)
    }
  }, [open])

  // Debounced search for registered users as user types
  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current)
    }

    const trimmed = memberInput.trim()
    if (trimmed.length < 2) {
      setSearchResults([])
      setSearching(false)
      setShowDropdown(false)
      return
    }

    setSearching(true)
    searchTimeoutRef.current = setTimeout(async () => {
      try {
        const users = await splitExpensesApi.searchUsers(trimmed, token)
        setSearchResults(users || [])
        setShowDropdown((users || []).length > 0)
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
      toast.error('You cannot invite yourself. You are automatically the group owner.')
      return
    }

    // Check duplicate
    if (invitees.some((i) => i.email.toLowerCase() === cleanEmail)) {
      toast.error('This user is already added to the invite list.')
      return
    }

    // Check limit
    if (invitees.length >= 20) {
      toast.error('You can invite at most 20 members per group.')
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

      // If user selected first item in search or typed direct email
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
    if (!name.trim()) {
      toast.error('Group name is required')
      return
    }

    try {
      setLoading(true)
      const res = await splitExpensesApi.createGroup(
        {
          name: name.trim(),
          description: description.trim() || undefined,
          icon: selectedIcon,
          invitees:
            invitees.length > 0
              ? invitees.map((i) => ({
                  userId: i.userId,
                  email: i.email,
                  name: i.name,
                }))
              : undefined,
        },
        token
      )

      // Rich summary feedback
      if (res.invitations && res.invitations.length > 0) {
        const sent = res.invitations.filter((i) => i.status === 'Invited').length
        const failed = res.invitations.filter((i) => i.status !== 'Invited')

        if (failed.length === 0) {
          toast.success(
            `Group "${res.name}" created! ${sent} invitation${sent === 1 ? '' : 's'} sent.`
          )
        } else {
          toast.warning(
            `Group "${res.name}" created with ${sent} invitation${
              sent === 1 ? '' : 's'
            } sent, but ${failed.length} failed to deliver.`
          )
        }
      } else {
        toast.success(`Split group "${res.name}" created successfully`)
      }

      onOpenChange(false)
      onGroupCreated(res)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to create group'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Create Split Group"
      description="Create a new expense-sharing group and optionally invite members to join."
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Group Name */}
        <div className="space-y-2">
          <Label htmlFor="group-name">Group Name *</Label>
          <Input
            id="group-name"
            placeholder="e.g. Trip to Murree, Flat 4B, Friday Dinner"
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={loading}
            maxLength={150}
            required
            autoFocus
          />
        </div>

        {/* Group Icon */}
        <div className="space-y-2">
          <Label>Group Icon</Label>
          <div className="flex flex-wrap gap-2">
            {PRESET_ICONS.map((icon) => (
              <button
                type="button"
                key={icon}
                onClick={() => setSelectedIcon(icon)}
                className={`flex size-10 items-center justify-center rounded-lg border text-lg transition-[border-color,transform] duration-(--duration-fast) ${
                  selectedIcon === icon
                    ? 'border-primary bg-primary/10 shadow-xs scale-105'
                    : 'border-border hover:bg-muted/50'
                }`}
              >
                {icon}
              </button>
            ))}
          </div>
        </div>

        {/* Add Members Section (Multi-Entry) */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor="add-members">Add Members (Optional)</Label>
            <span className="text-[11px] text-muted-foreground">
              {invitees.length}/20 invitees
            </span>
          </div>

          <div className="relative" ref={dropdownRef}>
            <div className="flex items-center gap-2">
              <div className="relative flex-1">
                <Search className="size-4 absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="add-members"
                  placeholder="Search user by name or type email..."
                  value={memberInput}
                  onChange={(e) => setMemberInput(e.target.value)}
                  onKeyDown={handleInputKeyDown}
                  onFocus={() => {
                    if (searchResults.length > 0) setShowDropdown(true)
                  }}
                  disabled={loading || invitees.length >= 20}
                  className="pl-9 pr-8"
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
                className="gap-1 h-9"
              >
                <Plus className="size-4" />
                Add
              </Button>
            </div>

            {/* Autocomplete Dropdown */}
            {showDropdown && searchResults.length > 0 && (
              <div className="absolute top-full left-0 right-0 z-50 mt-1 max-h-52 overflow-y-auto rounded-lg border bg-popover text-popover-foreground shadow-lg animate-in fade-in-50 duration-(--duration-fast)">
                <div className="p-1">
                  <div className="text-[10px] font-semibold text-muted-foreground uppercase px-2 py-1">
                    Registered Users
                  </div>
                  {searchResults.map((user) => {
                    const isAlreadyAdded = invitees.some(
                      (i) => i.email.toLowerCase() === user.email.toLowerCase()
                    )
                    return (
                      <div
                        key={user.userId}
                        onClick={() => {
                          if (!isAlreadyAdded) {
                            handleAddInvitee({
                              userId: user.userId,
                              email: user.email,
                              name: user.fullName,
                              isRegistered: true,
                            })
                          }
                        }}
                        className={`flex items-center justify-between p-2 rounded-md text-xs cursor-pointer transition-colors ${
                          isAlreadyAdded
                            ? 'opacity-50 cursor-not-allowed bg-muted/30'
                            : 'hover:bg-muted/80'
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
                        {isAlreadyAdded && (
                          <Badge variant="outline" className="text-[9px] h-4 gap-0.5">
                            <Check className="size-2.5" /> Added
                          </Badge>
                        )}
                      </div>
                    )
                  })}
                </div>
              </div>
            )}
          </div>

          {/* Added Member Chips */}
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
                  {!inv.isRegistered && (
                    <Badge
                      variant="secondary"
                      className="text-[9px] px-1 py-0 h-4 font-normal bg-amber-500/10 text-amber-700 dark:text-amber-400 border border-amber-500/20"
                    >
                      Invite via email
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
          <p className="text-[11px] text-muted-foreground">
            Type an email address or search registered users. Press Enter, comma, or click Add.
          </p>
        </div>

        {/* Description */}
        <div className="space-y-2">
          <Label htmlFor="group-desc">Description (Optional)</Label>
          <Textarea
            id="group-desc"
            placeholder="What is this group for?"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            disabled={loading}
            maxLength={500}
            rows={2}
          />
        </div>

        <div className="flex justify-end gap-2 pt-2 border-t">
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={loading}
          >
            Cancel
          </Button>
          <Button type="submit" loading={loading} className="gap-1.5">
            <Users className="size-4" />
            Create Group
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
