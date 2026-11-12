import { useState } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { splitExpensesApi } from '../splitExpensesApi'
import { toast } from 'sonner'
import type { SplitGroupInvitationDto } from '../types'
import { Mail, UserPlus, CheckCircle2 } from 'lucide-react'

interface InviteMemberDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  groupId: string
  token?: string
  onMemberInvited: (invitation: SplitGroupInvitationDto) => void
}

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

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
    const cleanEmail = email.trim()

    if (!cleanEmail) {
      toast.error('Please enter an email address')
      return
    }

    if (!EMAIL_REGEX.test(cleanEmail)) {
      toast.error('Please enter a valid email address (e.g. user@example.com)')
      return
    }

    try {
      setLoading(true)
      const res = await splitExpensesApi.inviteMember(
        groupId,
        {
          email: cleanEmail,
          invitedEmail: cleanEmail,
        },
        token
      )

      toast.success(`Invitation sent to ${cleanEmail}`)
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
      description="Invite someone to join this split group by email."
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="invite-email">Member Email *</Label>
          <div className="relative">
            <Mail className="size-4 absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="invite-email"
              type="email"
              placeholder="name@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={loading}
              className="pl-9"
              required
              autoFocus
            />
          </div>
          <div className="rounded-lg border bg-muted/30 p-2.5 text-xs text-muted-foreground space-y-1 mt-2">
            <div className="flex items-start gap-1.5">
              <CheckCircle2 className="size-3.5 text-primary shrink-0 mt-0.5" />
              <span>
                <strong>Registered users</strong> receive an in-app notification with an instant Accept button.
              </span>
            </div>
            <div className="flex items-start gap-1.5">
              <CheckCircle2 className="size-3.5 text-primary shrink-0 mt-0.5" />
              <span>
                <strong>New users</strong> receive an email invitation with a secure link to register and join.
              </span>
            </div>
          </div>
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
