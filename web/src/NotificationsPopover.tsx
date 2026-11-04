import { useCallback, useEffect, useState } from 'react'
import { Bell, Check, RefreshCw, UserCheck, X } from 'lucide-react'

type Invitation = {
  id: string
  invitationType: string
  businessName: string
  cashbookName?: string
  invitedEmail: string
  invitedByName: string
  status: string
  createdOn: string
  remarks?: string
}

type Props = {
  token: string
  onClose: () => void
  onInvitationProcessed: () => void
  onToast: (message: string) => void
}

export default function NotificationsPopover({ token, onClose, onInvitationProcessed, onToast }: Props) {
  const [invitations, setInvitations] = useState<Invitation[]>([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [error, setError] = useState('')

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  const loadPending = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const response = await fetch(`${apiBase}/api/Invitations/mypendingInvitations`, {
        headers: { Authorization: `Bearer ${token}` }
      })
      const data = await response.json().catch(() => ({}))
      if (response.ok) {
        setInvitations(data?.data ?? data?.Data ?? data ?? [])
      } else {
        setError(data?.message || 'Could not load notifications.')
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error checking notifications.')
    } finally {
      setLoading(false)
    }
  }, [apiBase, token])

  useEffect(() => {
    void loadPending()
  }, [loadPending])

  async function handleAction(id: string, action: 'accept' | 'reject') {
    setBusyId(id)
    setError('')
    try {
      const response = await fetch(`${apiBase}/api/Invitations/${id}/${action}`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        ...(action === 'reject' ? { body: JSON.stringify({ remarks: '' }) } : {})
      })
      const result = await response.json().catch(() => ({}))
      if (!response.ok) {
        throw new Error(result?.message || `Failed to ${action} invitation.`)
      }

      onToast(action === 'accept' ? 'Invitation accepted! Workspace added.' : 'Invitation declined.')
      onInvitationProcessed()
      await loadPending()
    } catch (e) {
      setError(e instanceof Error ? e.message : `Could not ${action} invitation.`)
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="notifications-popover-overlay" onMouseDown={e => e.target === e.currentTarget && onClose()}>
      <div className="notifications-popover">
        <div className="popover-head">
          <div className="popover-title">
            <Bell size={16} />
            <strong>Notifications & Invites</strong>
            {invitations.length > 0 && <span className="unread-pill">{invitations.length}</span>}
          </div>
          <button className="popover-close" onClick={onClose} aria-label="Close">
            <X size={15} />
          </button>
        </div>

        {error && <div className="popover-error">{error}</div>}

        <div className="popover-body">
          {loading ? (
            <div className="popover-loading">
              <RefreshCw size={18} className="spin" />
              <span>Checking for invitations…</span>
            </div>
          ) : invitations.length === 0 ? (
            <div className="popover-empty">
              <UserCheck size={28} strokeWidth={1.5} />
              <strong>No pending invitations</strong>
              <p className="muted">You are all caught up. Any invites to collaborate on workspaces will appear here.</p>
            </div>
          ) : (
            <div className="popover-list">
              {invitations.map(invite => (
                <div className="popover-item" key={invite.id}>
                  <div className="popover-item-info">
                    <strong>{invite.businessName}</strong>
                    {invite.cashbookName && <span className="popover-sub">{invite.cashbookName}</span>}
                    <small>Invited by {invite.invitedByName || 'a workspace owner'}</small>
                    {invite.remarks && <p className="popover-note">“{invite.remarks}”</p>}
                  </div>
                  <div className="popover-actions">
                    <button
                      className="popover-btn accept"
                      disabled={busyId === invite.id}
                      onClick={() => void handleAction(invite.id, 'accept')}
                      title="Accept invitation"
                    >
                      <Check size={14} /> Accept
                    </button>
                    <button
                      className="popover-btn decline"
                      disabled={busyId === invite.id}
                      onClick={() => void handleAction(invite.id, 'reject')}
                      title="Decline invitation"
                    >
                      <X size={14} /> Decline
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
