import { useState } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { splitExpensesApi } from '../splitExpensesApi'
import { toast } from 'sonner'
import type { SplitGroupInvitationDto } from '../types'
import { Mail, UserPlus } from 'lucide-react'

interface InviteMemberDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  groupId: string
  token?: string
  onMemberInvited: (invitation: SplitGroupInvitationDto) => void
}

export function InviteMemberDialog({
  open,
  onOpenChange,
  groupId,
  token,
  onMemberInvited,
}: InviteMemberDialogProps) {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!email.trim() || !email.includes('@')) {
      toast.error('Please enter a valid email address')
      return
    }

    try {
      setLoading(true)
      const res = await splitExpensesApi.inviteMember(
        groupId,
        { email: email.trim() },
        token
      )
      toast.success(`Invitation sent to ${email}`)
      setEmail('')
      onOpenChange(false)
      onMemberInvited(res)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to invite member'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Invite Group Member"
      description="Invite a registered user to join this group by email."
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="invite-email">User Email *</Label>
          <div className="relative">
            <Mail className="size-4 absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="invite-email"
              type="email"
              placeholder="user@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={loading}
              className="pl-9"
              required
              autoFocus
            />
          </div>
          <p className="text-xs text-muted-foreground">
            The invited user will see this invitation in their Split Expenses dashboard.
          </p>
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={loading}
          >
            Cancel
          </Button>
          <Button type="submit" loading={loading} className="gap-1.5">
            <UserPlus className="size-4" />
            Send Invitation
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
