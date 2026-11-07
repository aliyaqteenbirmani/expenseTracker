import { useState } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  User as UserIcon,
  Mail,
  Building2,
  KeyRound,
  LogOut,
  Loader2,
  CheckCircle2,
  ShieldCheck,
} from 'lucide-react'

type User = {
  firstName?: string
  FirstName?: string
  lastName?: string
  LastName?: string
  email?: string
  Email?: string
  phone?: string
  Phone?: string
  gender?: string | number
  Gender?: string | number
  id?: string
  Id?: string
}

type Props = {
  user: User
  token: string
  workspaceCount: number
  onClose: () => void
  onLogout: () => void
  onToast: (msg: string) => void
}

export default function UserProfileDialog({
  user,
  workspaceCount,
  onClose,
  onLogout,
  onToast,
}: Props) {
  const [resetRequested, setResetRequested] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const email = user.email ?? user.Email ?? ''
  const firstName = user.firstName ?? user.FirstName ?? ''
  const lastName = user.lastName ?? user.LastName ?? ''
  const fullName = `${firstName} ${lastName}`.trim() || 'Spendwise User'
  const initials = fullName.slice(0, 2).toUpperCase()

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  async function requestPasswordReset() {
    if (!email) return
    setBusy(true)
    setError('')
    try {
      const res = await fetch(
        `${apiBase}/api/Auth/forgot-password?email=${encodeURIComponent(email)}`,
        { method: 'POST' }
      )
      if (res.ok) {
        setResetRequested(true)
        onToast('Password reset instructions sent to your email.')
      } else {
        const data = await res.json().catch(() => ({}))
        setError(data.message || data.Message || 'Could not send reset link.')
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error requesting password reset.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <ResponsiveDialog
      open={true}
      onOpenChange={(open) => !open && onClose()}
      title={
        <div className="flex items-center gap-2">
          <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <UserIcon className="size-4" />
          </div>
          <div>
            <div className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              Account
            </div>
            <div className="text-base font-semibold">User Profile</div>
          </div>
        </div>
      }
      description="Manage your account profile, security credentials and linked workspaces."
    >
      <div className="space-y-5 pt-2">
        {/* User Card */}
        <div className="flex items-center gap-3 rounded-xl border border-border/80 bg-muted/30 p-3.5">
          <Avatar className="size-12 rounded-xl">
            <AvatarFallback className="rounded-xl bg-primary text-primary-foreground font-semibold text-sm">
              {initials}
            </AvatarFallback>
          </Avatar>
          <div className="flex-1 space-y-0.5">
            <div className="flex items-center gap-2">
              <h4 className="font-semibold text-sm text-foreground">{fullName}</h4>
              <Badge variant="outline" className="text-[10px] px-1.5 py-0 border-inflow/30 text-inflow bg-inflow-muted">
                Active
              </Badge>
            </div>
            <p className="text-xs text-muted-foreground flex items-center gap-1.5">
              <Mail className="size-3" />
              <span>{email}</span>
            </p>
          </div>
        </div>

        {/* Metrics */}
        <div className="grid grid-cols-2 gap-3 text-xs">
          <div className="rounded-lg border border-border/80 p-3 space-y-1">
            <span className="text-muted-foreground text-[11px] flex items-center gap-1">
              <Building2 className="size-3" /> Workspaces
            </span>
            <p className="font-mono text-base font-semibold">{workspaceCount}</p>
          </div>
          <div className="rounded-lg border border-border/80 p-3 space-y-1">
            <span className="text-muted-foreground text-[11px] flex items-center gap-1">
              <ShieldCheck className="size-3" /> Security
            </span>
            <p className="font-medium text-xs text-foreground">JWT Authenticated</p>
          </div>
        </div>

        {/* Password Reset Section */}
        <div className="rounded-xl border border-border/80 p-4 space-y-3">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <span className="text-xs font-semibold flex items-center gap-1.5">
                <KeyRound className="size-3.5 text-primary" /> Password & Security
              </span>
              <p className="text-[11px] text-muted-foreground">
                Receive an email reset link to update your credentials.
              </p>
            </div>
            <Button
              variant="outline"
              size="sm"
              disabled={busy || resetRequested}
              onClick={requestPasswordReset}
              className="text-xs h-8"
            >
              {busy ? (
                <Loader2 className="size-3 animate-spin mr-1" />
              ) : resetRequested ? (
                <CheckCircle2 className="size-3 text-inflow mr-1" />
              ) : null}
              <span>{resetRequested ? 'Sent!' : 'Reset password'}</span>
            </Button>
          </div>

          {resetRequested && (
            <Alert className="py-2 text-xs border-inflow/30 bg-inflow-muted/50 text-inflow">
              <CheckCircle2 className="size-4" />
              <AlertDescription>
                Check your inbox at <strong>{email}</strong> for instructions.
              </AlertDescription>
            </Alert>
          )}

          {error && (
            <Alert variant="destructive" className="py-2 text-xs">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
        </div>

        {/* Actions Footer */}
        <div className="flex items-center justify-between pt-2 border-t">
          <Button
            variant="ghost"
            size="sm"
            className="text-destructive hover:bg-destructive/10 hover:text-destructive h-8 text-xs gap-1.5"
            onClick={() => {
              onClose()
              onLogout()
            }}
          >
            <LogOut className="size-3.5" />
            <span>Sign out</span>
          </Button>
          <Button variant="outline" size="sm" onClick={onClose} className="h-8 text-xs">
            Close
          </Button>
        </div>
      </div>
    </ResponsiveDialog>
  )
}
