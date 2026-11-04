import { useState } from 'react'
import { BriefcaseBusiness, KeyRound, LogOut, Mail, Phone, ShieldCheck, User as UserIcon, X } from 'lucide-react'

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
  onToast
}: Props) {
  const [resetRequested, setResetRequested] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const email = user.email ?? user.Email ?? ''
  const firstName = user.firstName ?? user.FirstName ?? ''
  const lastName = user.lastName ?? user.LastName ?? ''
  const fullName = `${firstName} ${lastName}`.trim() || 'Spendwise User'
  const phone = user.phone ?? user.Phone ?? 'Not provided'
  const gender = String(user.gender ?? user.Gender ?? 'Not specified')

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  async function requestPasswordReset() {
    if (!email) return
    setBusy(true)
    setError('')
    try {
      const res = await fetch(`${apiBase}/api/Auth/forgot-password?email=${encodeURIComponent(email)}`, {
        method: 'POST'
      })
      if (res.ok) {
        setResetRequested(true)
        onToast('Password reset instructions sent to your email.')
      } else {
        const data = await res.json().catch(() => ({}))
        setError(data.message || 'Could not send password reset request.')
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error requesting password reset.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && onClose()}>
      <section className="dialog user-profile-dialog" style={{ maxWidth: 440 }}>
        <header>
          <div>
            <div className="eyebrow">MY ACCOUNT</div>
            <h2>User Profile</h2>
          </div>
          <button className="close" onClick={onClose} aria-label="Close dialog">
            <X size={16} strokeWidth={1.8} />
          </button>
        </header>

        <div className="dialog-content profile-dialog-content">
          <div className="profile-header-card">
            <div className="profile-big-avatar">
              {fullName.slice(0, 1).toUpperCase()}
            </div>
            <div className="profile-header-meta">
              <strong>{fullName}</strong>
              <small>{email}</small>
              <span className="profile-badge">
                <ShieldCheck size={12} /> Active Member
              </span>
            </div>
          </div>

          <div className="profile-details-list">
            <div className="profile-detail-row">
              <span className="detail-label">
                <UserIcon size={14} /> Full Name
              </span>
              <strong className="detail-value">{fullName}</strong>
            </div>

            <div className="profile-detail-row">
              <span className="detail-label">
                <Mail size={14} /> Email
              </span>
              <strong className="detail-value">{email}</strong>
            </div>

            <div className="profile-detail-row">
              <span className="detail-label">
                <Phone size={14} /> Phone
              </span>
              <strong className="detail-value">{phone}</strong>
            </div>

            <div className="profile-detail-row">
              <span className="detail-label">
                <BriefcaseBusiness size={14} /> Workspaces
              </span>
              <strong className="detail-value">{workspaceCount} active</strong>
            </div>

            {gender !== 'Not specified' && (
              <div className="profile-detail-row">
                <span className="detail-label">Gender</span>
                <strong className="detail-value">{gender}</strong>
              </div>
            )}
          </div>

          {error && <div className="error-banner">{error}</div>}

          <div className="profile-security-box">
            <div className="sec-header">
              <KeyRound size={16} />
              <div>
                <strong>Security & Password</strong>
                <p className="muted" style={{ margin: 0, fontSize: 11 }}>
                  Need to change your password? We will email you a secure link.
                </p>
              </div>
            </div>
            {resetRequested ? (
              <div className="success-banner" style={{ marginTop: 8 }}>
                Check your inbox for password reset instructions.
              </div>
            ) : (
              <button
                type="button"
                className="button secondary-action full"
                style={{ marginTop: 10 }}
                disabled={busy}
                onClick={requestPasswordReset}
              >
                <KeyRound size={14} />
                {busy ? 'Sending request…' : 'Request Password Reset Link'}
              </button>
            )}
          </div>

          <div className="profile-dialog-actions" style={{ marginTop: 18, display: 'flex', gap: 10 }}>
            <button
              type="button"
              className="button danger full"
              onClick={() => {
                onClose()
                onLogout()
              }}
            >
              <LogOut size={15} /> Sign out
            </button>
          </div>
        </div>
      </section>
    </div>
  )
}
