import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  ArrowLeft,
  Building2,
  Check,
  CheckCircle2,
    Copy,
  Crown,
    Info,
  Lock,
    MailPlus,
      RefreshCw,
  Search,
  Shield,
  ShieldCheck,
  SlidersHorizontal,
  Trash2,
  UserCheck,
  UserRound,
  UserRoundPlus,
  Wallet,
  X,
  XCircle
} from 'lucide-react'

type Business = { id: string; name: string }
type Cashbook = { id: string; cashBookName?: string; name?: string }
type Member = { userId: string; fullName: string; email: string; status: string; permissions: string[] }
type Invitation = {
  id: string
  invitationType: string
  businessName: string
  cashbookName?: string
  invitedEmail: string
  invitedByName: string
  status: string
  expiresOn?: string
  createdOn: string
  permissions: string[]
}
type Envelope<T> = { data?: T; Data?: T; message?: string; Message?: string }

const businessPermissions = [
  { id: 1, code: 'BUSINESS_VIEW', label: 'View business', description: 'Access workspace overview & dashboards' },
  { id: 2, code: 'CASHBOOK_LIST_VIEW', label: 'View cashbooks', description: 'Browse and inspect all business cashbooks' },
  { id: 3, code: 'BUSINESS_UPDATE', label: 'Edit business', description: 'Update business name and workspace settings' },
  { id: 4, code: 'CASHBOOK_CREATE', label: 'Create cashbooks', description: 'Create and initialize new cashbooks' },
  { id: 5, code: 'CASHBOOK_UPDATE', label: 'Edit cashbooks', description: 'Rename and reconfigure cashbooks' },
  { id: 6, code: 'CASHBOOK_DELETE', label: 'Delete cashbooks', description: 'Archive or permanently delete cashbooks' },
]

const cashbookPermissions = [
  { id: 1, code: 'CASHBOOK_VIEW', label: 'View cashbook', description: 'See this cashbook in the workspace list' },
  { id: 2, code: 'TRANSACTION_VIEW', label: 'View transactions', description: 'Inspect entries, amounts, dates, and attachments' },
  { id: 3, code: 'TRANSACTION_CREATE', label: 'Add transactions', description: 'Record cash in/out entries and scan receipts' },
  { id: 4, code: 'TRANSACTION_UPDATE', label: 'Edit transactions', description: 'Modify remarks, dates, and attachments' },
  { id: 5, code: 'TRANSACTION_DELETE', label: 'Delete transactions', description: 'Permanently remove recorded transactions' },
]

const permissionCodeMap: Record<string, { label: string; type: 'view' | 'create' | 'update' | 'delete' | 'default' }> = {
  BUSINESS_VIEW: { label: 'View business', type: 'view' },
  CASHBOOK_LIST_VIEW: { label: 'View cashbooks', type: 'view' },
  BUSINESS_UPDATE: { label: 'Edit business', type: 'update' },
  CASHBOOK_CREATE: { label: 'Create cashbooks', type: 'create' },
  CASHBOOK_UPDATE: { label: 'Edit cashbooks', type: 'update' },
  CASHBOOK_DELETE: { label: 'Delete cashbooks', type: 'delete' },
  CASHBOOK_VIEW: { label: 'View cashbook', type: 'view' },
  TRANSACTION_VIEW: { label: 'View transactions', type: 'view' },
  TRANSACTION_CREATE: { label: 'Add transactions', type: 'create' },
  TRANSACTION_UPDATE: { label: 'Edit transactions', type: 'update' },
  TRANSACTION_DELETE: { label: 'Delete transactions', type: 'delete' },
}

async function request<T>(path: string, token: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${token}`)
  if (options.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'}${path}`, { ...options, headers })
  const body = (await response.json().catch(() => ({}))) as Envelope<T>
  if (!response.ok) throw new Error(body.message ?? body.Message ?? `Request failed (${response.status})`)
  return (body.data ?? body.Data ?? body) as T
}

function MemberPermissionEditor({
  member,
  scope,
  businessId,
  cashbookId,
  token,
  onSaved
}: {
  member: Member
  scope: 'business' | 'cashbook'
  businessId: string
  cashbookId: string
  token: string
  onSaved: () => Promise<void>
}) {
  const [isOpen, setIsOpen] = useState(false)
  const options = scope === 'business' ? businessPermissions : cashbookPermissions
  const [selected, setSelected] = useState<number[]>(() =>
    options
      .filter(option => member.permissions?.includes(option.code) || member.permissions?.includes(String(option.id)))
      .map(option => option.id)
  )
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [saveSuccess, setSaveSuccess] = useState(false)

  useEffect(() => {
    setSelected(
      options
        .filter(option => member.permissions?.includes(option.code) || member.permissions?.includes(String(option.id)))
        .map(option => option.id)
    )
  }, [member.permissions, options])

  async function save() {
    setBusy(true)
    setMessage('')
    setSaveSuccess(false)
    const path =
      scope === 'business'
        ? `/api/MemberManagement/businesses/${businessId}/members/${member.userId}/permissions`
        : `/api/MemberManagement/businesses/${businessId}/cashbooks/${cashbookId}/members/${member.userId}/permissions`
    try {
      await request(path, token, { method: 'PUT', body: JSON.stringify({ permissions: selected }) })
      setSaveSuccess(true)
      await onSaved()
      setTimeout(() => {
        setSaveSuccess(false)
        setIsOpen(false)
      }, 900)
    } catch (e) {
      setMessage(e instanceof Error ? e.message : 'Could not update permissions.')
    } finally {
      setBusy(false)
    }
  }

  function applyPreset(ids: number[]) {
    setSelected(ids)
  }

  if (!isOpen) {
    return (
      <button
        type="button"
        className="member-edit-perms-btn"
        onClick={() => setIsOpen(true)}
      >
        <SlidersHorizontal size={12} strokeWidth={2} /> Manage Permissions
      </button>
    )
  }

  return (
    <div className="member-perm-editor-box">
      <div className="perm-editor-header">
        <div className="perm-editor-title">
          <Shield size={14} strokeWidth={2} />
          <strong>Edit Member Permissions</strong>
        </div>
        <button
          type="button"
          className="perm-editor-close-btn"
          onClick={() => setIsOpen(false)}
          disabled={busy}
        >
          <X size={14} />
        </button>
      </div>

      <div className="perm-presets-row">
        <span className="preset-label">Presets:</span>
        {scope === 'cashbook' ? (
          <>
            <button
              type="button"
              className="preset-chip"
              onClick={() => applyPreset([1, 2, 3, 4, 5])}
            >
              Full Access
            </button>
            <button
              type="button"
              className="preset-chip"
              onClick={() => applyPreset([1, 2, 3, 4])}
            >
              Add & Edit
            </button>
            <button
              type="button"
              className="preset-chip"
              onClick={() => applyPreset([1, 2, 3])}
            >
              Add Only
            </button>
            <button
              type="button"
              className="preset-chip"
              onClick={() => applyPreset([1, 2])}
            >
              View Only
            </button>
          </>
        ) : (
          <>
            <button
              type="button"
              className="preset-chip"
              onClick={() => applyPreset([1, 2, 3, 4, 5, 6])}
            >
              Admin
            </button>
            <button
              type="button"
              className="preset-chip"
              onClick={() => applyPreset([1, 2, 4, 5])}
            >
              Standard
            </button>
            <button
              type="button"
              className="preset-chip"
              onClick={() => applyPreset([1, 2])}
            >
              Viewer
            </button>
          </>
        )}
      </div>

      <div className="perm-cards-list">
        {options.map(option => {
          const checked = selected.includes(option.id)
          return (
            <label
              key={option.id}
              className={`perm-card-item ${checked ? 'selected' : ''}`}
            >
              <input
                type="checkbox"
                checked={checked}
                onChange={() =>
                  setSelected(values =>
                    values.includes(option.id)
                      ? values.filter(v => v !== option.id)
                      : [...values, option.id]
                  )
                }
              />
              <div className="perm-card-text">
                <span className="perm-card-label">{option.label}</span>
                <span className="perm-card-desc">{option.description}</span>
              </div>
            </label>
          )
        })}
      </div>

      {message && <div className="perm-editor-error">{message}</div>}
      {saveSuccess && (
        <div className="perm-editor-success">
          <Check size={13} strokeWidth={2.5} /> Permissions updated successfully!
        </div>
      )}

      <div className="perm-editor-actions">
        <button
          type="button"
          className="button secondary-action small"
          onClick={() => setIsOpen(false)}
          disabled={busy}
        >
          Cancel
        </button>
        <button
          type="button"
          className="button primary small"
          disabled={busy || !selected.length}
          onClick={() => void save()}
        >
          {busy ? 'Saving changes…' : 'Save Permissions'}
        </button>
      </div>
    </div>
  )
}

export default function MembersPage({
  token,
  business,
  books,
  isOwner = false,
  currentUserId,
  onBack,
  onToast
}: {
  token: string
  business: Business
  books: Cashbook[]
  isOwner?: boolean
  currentUserId?: string
  onBack: () => void
  onToast: (message: string) => void
}) {
  const [scope, setScope] = useState<'business' | 'cashbook'>('cashbook')
  const [cashbookId, setCashbookId] = useState(books[0]?.id ?? '')
  const [members, setMembers] = useState<Member[]>([])
  const [pending, setPending] = useState<Invitation[]>([])
  const [sent, setSent] = useState<Invitation[]>([])
  const [sentStatus, setSentStatus] = useState<string>('All')
  const [memberSearch, setMemberSearch] = useState('')
  const [email, setEmail] = useState('')
  const [remarks, setRemarks] = useState('')
  const [permissions, setPermissions] = useState<number[]>([1, 2, 3])
  const [confirmRemove, setConfirmRemove] = useState<Member | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const selectedPermissions = scope === 'business' ? businessPermissions : cashbookPermissions
  const memberUrl =
    scope === 'business'
      ? `/api/MemberManagement/businesses/${business.id}/members`
      : `/api/MemberManagement/businesses/${business.id}/cashbooks/${cashbookId}/members`

  const reload = useCallback(async () => {
    if (scope === 'cashbook' && !cashbookId) {
      setMembers([])
      setLoading(false)
      return
    }
    setLoading(true)
    setError('')
    const results = await Promise.allSettled([
      request<Member[]>(memberUrl, token),
      request<Invitation[]>('/api/Invitations/mypendingInvitations', token),
      isOwner
        ? request<Invitation[]>(
            `/api/Invitations/mysentInvitations${sentStatus && sentStatus !== 'All' ? `?status=${encodeURIComponent(sentStatus)}` : ''}`,
            token
          )
        : Promise.resolve([])
    ])
    if (results[0].status === 'fulfilled') setMembers(results[0].value ?? [])
    else {
      setMembers([])
      setError(results[0].reason instanceof Error ? results[0].reason.message : 'Could not load members.')
    }
    setPending(results[1].status === 'fulfilled' ? results[1].value ?? [] : [])
    setSent(results[2].status === 'fulfilled' ? results[2].value ?? [] : [])
    setLoading(false)
  }, [memberUrl, token, scope, cashbookId, sentStatus, isOwner])

  useEffect(() => {
    void reload()
  }, [reload])

  useEffect(() => {
    if (!cashbookId && books.length > 0) {
      setCashbookId(books[0].id)
    }
  }, [books, cashbookId])

  function handleScopeChange(newScope: 'business' | 'cashbook') {
    setScope(newScope)
    setPermissions(newScope === 'business' ? [1, 2] : [1, 2, 3])
    if (newScope === 'cashbook' && !cashbookId && books[0]?.id) {
      setCashbookId(books[0].id)
    }
  }

  function applyInvitePreset(preset: 'full' | 'contributor' | 'entry' | 'view') {
    if (scope === 'cashbook') {
      switch (preset) {
        case 'full':
          setPermissions([1, 2, 3, 4, 5])
          break
        case 'contributor':
          setPermissions([1, 2, 3, 4])
          break
        case 'entry':
          setPermissions([1, 2, 3])
          break
        case 'view':
          setPermissions([1, 2])
          break
      }
    } else {
      switch (preset) {
        case 'full':
          setPermissions([1, 2, 3, 4, 5, 6])
          break
        case 'contributor':
          setPermissions([1, 2, 4, 5])
          break
        case 'view':
        case 'entry':
          setPermissions([1, 2])
          break
      }
    }
  }

  async function sendInvite(event: FormEvent) {
    event.preventDefault()
    if (!isOwner) {
      setError('Only the cashbook owner can invite new members.')
      return
    }
    setBusy(true)
    setError('')
    const path =
      scope === 'business'
        ? `/api/Invitations/business/${business.id}`
        : `/api/Invitations/business/${business.id}/cashbooks/${cashbookId}`
    try {
      const response = await fetch(`${import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'}${path}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ invitedEmail: email, remarks, permissions })
      })
      const body = await response.json().catch(() => ({}))
      if (!response.ok) {
        throw new Error(body.message || body.Message || `Could not send invitation (${response.status})`)
      }
      setEmail('')
      setRemarks('')
      const msg = body.message || body.Message || ''
      if (msg.toLowerCase().includes('could not be sent')) {
        onToast('Invitation created, but email delivery failed (check SMTP settings). You can copy the invite link below.')
      } else {
        onToast(msg || 'Invitation sent successfully')
      }
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not send invitation.')
    } finally {
      setBusy(false)
    }
  }

  async function invitationAction(id: string, action: 'accept' | 'reject' | 'revoke') {
    setBusy(true)
    setError('')
    try {
      await request(`/api/Invitations/${id}/${action}`, token, {
        method: 'POST',
        ...(action === 'reject' ? { body: JSON.stringify({ remarks: '' }) } : {})
      })
      onToast(action === 'accept' ? 'Invitation accepted! Cashbook is now accessible.' : action === 'reject' ? 'Invitation declined.' : 'Invitation revoked.')
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : `Could not ${action} invitation.`)
    } finally {
      setBusy(false)
    }
  }

  async function removeMember(member: Member) {
    if (!isOwner) {
      setError('Only the cashbook owner can remove members.')
      return
    }
    setBusy(true)
    setError('')
    const path =
      scope === 'business'
        ? `/api/MemberManagement/businesses/${business.id}/members/${member.userId}`
        : `/api/MemberManagement/businesses/${business.id}/cashbooks/${cashbookId}/members/${member.userId}`
    try {
      await request(path, token, { method: 'DELETE' })
      setConfirmRemove(null)
      onToast('Member removed')
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not remove member.')
    } finally {
      setBusy(false)
    }
  }

  function togglePermission(id: number) {
    setPermissions(current => (current.includes(id) ? current.filter(value => value !== id) : [...current, id]))
  }

  const filteredMembers = useMemo(() => {
    if (!memberSearch.trim()) return members
    const term = memberSearch.toLowerCase()
    return members.filter(
      m => (m.fullName && m.fullName.toLowerCase().includes(term)) || (m.email && m.email.toLowerCase().includes(term))
    )
  }, [members, memberSearch])

  // Current user's member object in current scope
  const currentMemberRecord = useMemo(() => {
    if (!currentUserId) return null
    return members.find(m => m.userId?.toLowerCase() === currentUserId.toLowerCase())
  }, [members, currentUserId])

  const selectedCashbook = books.find(b => b.id === cashbookId)

  return (
    <section className="members-page">
      {/* Top Heading */}
      <div className="history-heading">
        <div>
          <button className="history-back" onClick={onBack}>
            <ArrowLeft size={14} strokeWidth={2} /> Back to overview
          </button>
          <div className="eyebrow">SECURITY & PERMISSION MANAGEMENT</div>
          <h1>Team & Access Control</h1>
          <p className="muted">
            {business.name} · {selectedCashbook?.cashBookName ?? selectedCashbook?.name ?? 'Workspace'}
          </p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div className={`user-role-badge-pill ${isOwner ? 'owner' : 'member'}`}>
            {isOwner ? <Crown size={13} strokeWidth={2.2} /> : <Shield size={13} strokeWidth={2.2} />}
            <span>{isOwner ? 'Cashbook Owner' : 'Invited Member'}</span>
          </div>
          <button className="button secondary-action" onClick={() => void reload()} disabled={loading}>
            <RefreshCw size={14} className={loading ? 'spin' : ''} /> Refresh
          </button>
        </div>
      </div>

      {error && (
        <div className="error-banner">
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <XCircle size={16} />
            <span>{error}</span>
          </div>
          <button onClick={() => setError('')}>
            <X size={14} /> Dismiss
          </button>
        </div>
      )}

      {/* Main Grid: Left Panel (Invite or Self-Status) + Right Panel (Member Directory) */}
      <div className="members-layout">
        {/* Left Column: Owner can invite; Non-owner gets full transparent capability card */}
        {isOwner ? (
          <article className="panel invite-panel">
            <div className="panel-head">
              <div>
                <h2>
                  <UserRoundPlus size={17} strokeWidth={2} /> Invite Member
                </h2>
                <p>Grant owner-controlled permissions to a teammate.</p>
              </div>
              <span className="member-panel-icon">
                <MailPlus size={18} />
              </span>
            </div>

            <form className="invite-form" onSubmit={sendInvite}>
              <label>
                Access Scope
                <div className="invite-scope-toggle">
                  <button
                    type="button"
                    className={`scope-pill-btn ${scope === 'cashbook' ? 'active' : ''}`}
                    onClick={() => handleScopeChange('cashbook')}
                  >
                    <Wallet size={13} /> This Cashbook
                  </button>
                  <button
                    type="button"
                    className={`scope-pill-btn ${scope === 'business' ? 'active' : ''}`}
                    onClick={() => handleScopeChange('business')}
                  >
                    <Building2 size={13} /> Entire Business
                  </button>
                </div>
              </label>

              {scope === 'cashbook' && (
                <label>
                  Cashbook
                  <select value={cashbookId} onChange={event => setCashbookId(event.target.value)}>
                    {books.map(book => (
                      <option key={book.id} value={book.id}>
                        {book.cashBookName ?? book.name ?? 'Cashbook'}
                      </option>
                    ))}
                  </select>
                </label>
              )}

              <label>
                Teammate Email
                <input
                  type="email"
                  required
                  value={email}
                  onChange={event => setEmail(event.target.value)}
                  placeholder="collaborator@example.com"
                />
              </label>

              <label>
                Note / Purpose <span className="muted">(optional)</span>
                <textarea
                  rows={2}
                  maxLength={300}
                  value={remarks}
                  onChange={event => setRemarks(event.target.value)}
                  placeholder="e.g. Assigned to manage daily cash registers"
                />
              </label>

              <fieldset className="invite-perms-fieldset">
                <div className="fieldset-header">
                  <legend>Assigned Permissions</legend>
                  <div className="invite-presets">
                    <button type="button" onClick={() => applyInvitePreset('full')}>Full</button>
                    <button type="button" onClick={() => applyInvitePreset('contributor')}>Add & Edit</button>
                    <button type="button" onClick={() => applyInvitePreset('entry')}>Add Only</button>
                    <button type="button" onClick={() => applyInvitePreset('view')}>View</button>
                  </div>
                </div>

                <div className="invite-perm-cards">
                  {selectedPermissions.map(item => {
                    const checked = permissions.includes(item.id)
                    return (
                      <label key={item.id} className={`invite-perm-card ${checked ? 'checked' : ''}`}>
                        <input
                          type="checkbox"
                          checked={checked}
                          onChange={() => togglePermission(item.id)}
                        />
                        <div className="perm-info">
                          <strong>{item.label}</strong>
                          <small>{item.description}</small>
                        </div>
                      </label>
                    )
                  })}
                </div>
              </fieldset>

              <div className="security-notice-callout">
                <ShieldCheck size={14} className="notice-icon" />
                <span>
                  <strong>Owner Security:</strong> You control what this member can do. Members can never elevate their own privileges or grant themselves permissions.
                </span>
              </div>

              <button className="button primary full" disabled={busy || !permissions.length || (scope === 'cashbook' && !cashbookId)}>
                <MailPlus size={15} />
                {busy ? 'Sending invitation…' : 'Send Invitation'}
              </button>
            </form>
          </article>
        ) : (
          <article className="panel invite-panel invite-panel-readonly">
            <div className="panel-head">
              <div>
                <h2>
                  <ShieldCheck size={18} strokeWidth={2} /> Your Cashbook Access
                </h2>
                <p>{selectedCashbook?.cashBookName ?? selectedCashbook?.name ?? 'Selected Cashbook'}</p>
              </div>
              <span className="member-panel-icon lock-icon">
                <Lock size={18} />
              </span>
            </div>

            <div className="my-permissions-dashboard">
              <div className="my-role-banner">
                <UserCheck size={16} strokeWidth={2} />
                <div>
                  <strong>Invited Collaborator</strong>
                  <p>You have access to this cashbook with permissions granted by the Owner.</p>
                </div>
              </div>

              <div className="my-capabilities-grid">
                <label className="section-subhead">Your Assigned Capabilities</label>
                {cashbookPermissions.map(cp => {
                  const isGranted = Boolean(
                    currentMemberRecord?.permissions?.includes(cp.code) ||
                    currentMemberRecord?.permissions?.includes(String(cp.id))
                  )

                  return (
                    <div key={cp.id} className={`capability-row ${isGranted ? 'granted' : 'denied'}`}>
                      <div className="capability-status-icon">
                        {isGranted ? (
                          <CheckCircle2 size={16} className="text-emerald" />
                        ) : (
                          <Lock size={14} className="text-muted" />
                        )}
                      </div>
                      <div className="capability-details">
                        <strong>{cp.label}</strong>
                        <small>{cp.description}</small>
                      </div>
                      <span className={`capability-pill ${isGranted ? 'pill-granted' : 'pill-denied'}`}>
                        {isGranted ? 'Allowed' : 'Restricted'}
                      </span>
                    </div>
                  )
                })}
              </div>

              <div className="owner-authority-footer">
                <Info size={14} className="text-muted" />
                <span>
                  Only the Cashbook Owner can assign or modify permissions. If you need Delete or Update privileges, contact the Owner.
                </span>
              </div>
            </div>
          </article>
        )}

        {/* Right Column: Member Directory */}
        <article className="panel member-list-panel">
          <div className="panel-head">
            <div>
              <h2>
                <Shield size={17} strokeWidth={2} /> Cashbook Members
              </h2>
              <p>
                {scope === 'business'
                  ? business.name
                  : books.find(book => book.id === cashbookId)?.cashBookName ?? 'Selected cashbook'}
              </p>
            </div>
            <div className="member-scope">
              <button
                className={scope === 'cashbook' ? 'selected' : ''}
                onClick={() => handleScopeChange('cashbook')}
              >
                <Wallet size={12} /> Cashbook
              </button>
              <button
                className={scope === 'business' ? 'selected' : ''}
                onClick={() => handleScopeChange('business')}
              >
                <Building2 size={12} /> Business
              </button>
            </div>
          </div>

          {scope === 'cashbook' && books.length > 0 && (
            <div className="cashbook-selector-bar">
              <span className="selector-label">Cashbook:</span>
              <select
                value={cashbookId}
                onChange={e => setCashbookId(e.target.value)}
                className="cashbook-dropdown"
              >
                {books.map(b => (
                  <option key={b.id} value={b.id}>
                    {b.cashBookName ?? b.name ?? 'Cashbook'}
                  </option>
                ))}
              </select>
            </div>
          )}

          <div className="member-search-bar">
            <Search size={14} className="muted search-icon" />
            <input
              type="text"
              placeholder="Search members by name or email…"
              value={memberSearch}
              onChange={e => setMemberSearch(e.target.value)}
              className="member-search-input"
            />
            {memberSearch && (
              <button className="search-clear-btn" onClick={() => setMemberSearch('')}>
                <X size={12} />
              </button>
            )}
          </div>

          {loading ? (
            <div className="member-skeletons">
              {[0, 1, 2].map(i => (
                <div className="member-skeleton" key={i}>
                  <i />
                  <span>
                    <b />
                    <small />
                  </span>
                </div>
              ))}
            </div>
          ) : filteredMembers.length ? (
            <div className="member-list">
              {filteredMembers.map(member => {
                const isCurrentUser = Boolean(currentUserId && member.userId?.toLowerCase() === currentUserId.toLowerCase())
                const canManageThisMember = isOwner && !isCurrentUser

                return (
                  <div className={`member-row-card ${isCurrentUser ? 'is-self' : ''}`} key={`${member.userId}-${scope}`}>
                    <div className="member-row-top">
                      <span className="member-avatar">
                        <UserRound size={17} />
                      </span>
                      <div className="member-info">
                        <div className="member-name-row">
                          <strong>{member.fullName || member.email}</strong>
                          {isCurrentUser && <span className="member-self-pill">You</span>}
                          <span className={`member-status-pill ${(member.status || 'active').toLowerCase()}`}>
                            {member.status || 'Active'}
                          </span>
                        </div>
                        <small className="member-email-text">{member.email}</small>

                        <div className="member-perm-badges">
                          {member.permissions && member.permissions.length > 0 ? (
                            member.permissions.map(p => {
                              const meta = permissionCodeMap[p]
                              const typeClass = meta?.type ? `perm-chip-${meta.type}` : 'perm-chip-default'
                              return (
                                <span key={p} className={`member-perm-tag ${typeClass}`}>
                                  {meta?.label || p}
                                </span>
                              )
                            })
                          ) : (
                            <span className="member-perm-tag empty">No permissions granted</span>
                          )}
                        </div>
                      </div>

                      {canManageThisMember && (
                        <button
                          className="member-remove"
                          title="Remove member from this cashbook"
                          aria-label={`Remove ${member.fullName || member.email}`}
                          disabled={busy}
                          onClick={() => setConfirmRemove(member)}
                        >
                          <Trash2 size={15} />
                        </button>
                      )}
                    </div>

                    {/* Only owner can edit permissions of OTHER members. Self-editing is completely forbidden. */}
                    {canManageThisMember && (
                      <div className="member-row-editor">
                        <MemberPermissionEditor
                          key={`${member.userId}-${scope}`}
                          member={member}
                          scope={scope}
                          businessId={business.id}
                          cashbookId={cashbookId}
                          token={token}
                          onSaved={async () => {
                            onToast('Member permissions updated successfully')
                            await reload()
                          }}
                        />
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          ) : (
            <div className="member-empty">
              <UserRound size={28} className="muted" />
              <strong>{members.length ? 'No matching members found' : 'No members yet'}</strong>
              <p>{members.length ? 'Try a different search term.' : 'Use the invite form to add teammates.'}</p>
            </div>
          )}
        </article>
      </div>

      {/* Bottom Invitations Columns */}
      <div className="invitation-columns">
        {/* Invitations Received */}
        <article className="panel invitation-panel">
          <div className="panel-head">
            <div>
              <h2>Invitations for you</h2>
              <p>Pending invitations from cashbook owners.</p>
            </div>
            <span className="invitation-count">{pending.length}</span>
          </div>
          {pending.length ? (
            pending.map(invite => (
              <div className="invitation-row-card" key={invite.id}>
                <div className="invite-card-content">
                  <div className="invite-title-row">
                    <strong>
                      {invite.businessName}
                      {invite.cashbookName ? ` · ${invite.cashbookName}` : ''}
                    </strong>
                    <span className="invite-badge">Pending</span>
                  </div>
                  <small className="muted">
                    Invited by <strong>{invite.invitedByName}</strong>
                  </small>
                  {invite.permissions && invite.permissions.length > 0 && (
                    <div className="invite-perms-preview">
                      <span className="preview-label">Grants:</span>
                      {invite.permissions.map(p => (
                        <span key={p} className="preview-perm-chip">
                          {permissionCodeMap[p]?.label || p}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
                <div className="invitation-actions">
                  <button className="button-accept" disabled={busy} onClick={() => void invitationAction(invite.id, 'accept')}>
                    <Check size={14} /> Accept
                  </button>
                  <button className="button-decline" disabled={busy} onClick={() => void invitationAction(invite.id, 'reject')}>
                    <X size={14} /> Decline
                  </button>
                </div>
              </div>
            ))
          ) : (
            <p className="muted invitation-empty">You’re all caught up. No pending invitations.</p>
          )}
        </article>

        {/* Sent Invitations (Owners only) */}
        {isOwner && (
          <article className="panel invitation-panel">
            <div className="panel-head">
              <div>
                <h2>Sent invitations</h2>
                <p>Invitations sent to your teammates.</p>
              </div>
              <div className="sent-filter-chips">
                {['All', 'Pending', 'Accepted', 'Revoked'].map(st => (
                  <button
                    key={st}
                    className={`sent-st-chip ${sentStatus === st ? 'active' : ''}`}
                    onClick={() => setSentStatus(st)}
                  >
                    {st}
                  </button>
                ))}
              </div>
            </div>
            {sent.length ? (
              sent.map(invite => {
                const isPending = invite.status?.toLowerCase() === 'pending'
                return (
                  <div className="invitation-row-card sent-card" key={invite.id}>
                    <div className="invite-card-content">
                      <div className="invite-title-row">
                        <strong>{invite.invitedEmail}</strong>
                        <span className={`invite-status-tag ${invite.status.toLowerCase()}`}>
                          {invite.status}
                        </span>
                      </div>
                      <small className="muted">
                        {invite.businessName}
                        {invite.cashbookName ? ` · ${invite.cashbookName}` : ''}
                      </small>
                      {invite.permissions && invite.permissions.length > 0 && (
                        <div className="invite-perms-preview">
                          <span className="preview-label">Permissions:</span>
                          {invite.permissions.map(p => (
                            <span key={p} className="preview-perm-chip">
                              {permissionCodeMap[p]?.label || p}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                    <div className="sent-card-actions">
                      <button
                        type="button"
                        className="copy-invitation-btn"
                        title="Copy direct invite link"
                        onClick={() => {
                          const inviteUrl = `${window.location.origin}?inviteToken=${invite.id}`
                          void navigator.clipboard.writeText(inviteUrl)
                          onToast(`Invite link copied for ${invite.invitedEmail}`)
                        }}
                      >
                        <Copy size={13} strokeWidth={1.8} /> Copy Link
                      </button>
                      {isPending && (
                        <button
                          className="revoke-invitation-btn"
                          disabled={busy}
                          onClick={() => void invitationAction(invite.id, 'revoke')}
                          title="Revoke this pending invitation"
                        >
                          <X size={13} /> Revoke
                        </button>
                      )}
                    </div>
                  </div>
                )
              })
            ) : (
              <p className="muted invitation-empty">
                {sentStatus === 'All' ? 'No invitations sent yet.' : `No ${sentStatus.toLowerCase()} invitations sent.`}
              </p>
            )}
          </article>
        )}
      </div>

      {/* Confirmation Modal to remove member */}
      {confirmRemove && (
        <div className="overlay" onMouseDown={e => e.target === e.currentTarget && !busy && setConfirmRemove(null)}>
          <section className="dialog" style={{ maxWidth: 420 }}>
            <header>
              <div>
                <div className="eyebrow" style={{ color: '#b95f4b' }}>REVOKE ACCESS</div>
                <h2>Remove Member Access?</h2>
              </div>
              <button className="close" onClick={() => setConfirmRemove(null)} disabled={busy}>
                <X size={16} />
              </button>
            </header>
            <div className="dialog-content">
              <p>
                Are you sure you want to remove <strong>{confirmRemove.fullName || confirmRemove.email}</strong> from {scope === 'business' ? business.name : 'this cashbook'}? Their access and permissions will be revoked immediately.
              </p>
              <div className="form-between" style={{ marginTop: 20 }}>
                <button className="button secondary-action" disabled={busy} onClick={() => setConfirmRemove(null)}>
                  Cancel
                </button>
                <button className="button danger" disabled={busy} onClick={() => void removeMember(confirmRemove)}>
                  {busy ? 'Removing…' : 'Yes, Remove Member'}
                </button>
              </div>
            </div>
          </section>
        </div>
      )}
    </section>
  )
}
