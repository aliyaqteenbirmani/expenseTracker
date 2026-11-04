import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Check, Copy, MailPlus, RefreshCw, Search, Shield, Trash2, UserRound, UserRoundPlus, X } from 'lucide-react'

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
  { id: 1, code: 'BUSINESS_VIEW', label: 'View business' },
  { id: 2, code: 'CASHBOOK_LIST_VIEW', label: 'View cashbooks' },
  { id: 3, code: 'BUSINESS_UPDATE', label: 'Edit business' },
  { id: 4, code: 'CASHBOOK_CREATE', label: 'Create cashbooks' },
  { id: 5, code: 'CASHBOOK_UPDATE', label: 'Edit cashbooks' },
  { id: 6, code: 'CASHBOOK_DELETE', label: 'Delete cashbooks' },
]

const cashbookPermissions = [
  { id: 1, code: 'CASHBOOK_VIEW', label: 'View cashbook' },
  { id: 2, code: 'TRANSACTION_VIEW', label: 'View transactions' },
  { id: 3, code: 'TRANSACTION_CREATE', label: 'Create transactions' },
  { id: 4, code: 'TRANSACTION_UPDATE', label: 'Edit transactions' },
  { id: 5, code: 'TRANSACTION_DELETE', label: 'Delete transactions' },
]

const permissionCodeMap: Record<string, string> = {
  BUSINESS_VIEW: 'View business',
  CASHBOOK_LIST_VIEW: 'View cashbooks',
  BUSINESS_UPDATE: 'Edit business',
  CASHBOOK_CREATE: 'Create cashbooks',
  CASHBOOK_UPDATE: 'Edit cashbooks',
  CASHBOOK_DELETE: 'Delete cashbooks',
  CASHBOOK_VIEW: 'View cashbook',
  TRANSACTION_VIEW: 'View transactions',
  TRANSACTION_CREATE: 'Create transactions',
  TRANSACTION_UPDATE: 'Edit transactions',
  TRANSACTION_DELETE: 'Delete transactions',
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
  const options = scope === 'business' ? businessPermissions : cashbookPermissions
  const [selected, setSelected] = useState<number[]>(() =>
    options
      .filter(option => member.permissions?.includes(option.code) || member.permissions?.includes(String(option.id)))
      .map(option => option.id)
  )
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')

  async function save() {
    setBusy(true)
    setMessage('')
    const path =
      scope === 'business'
        ? `/api/MemberManagement/businesses/${businessId}/members/${member.userId}/permissions`
        : `/api/MemberManagement/businesses/${businessId}/cashbooks/${cashbookId}/members/${member.userId}/permissions`
    try {
      await request(path, token, { method: 'PUT', body: JSON.stringify({ permissions: selected }) })
      await onSaved()
    } catch (e) {
      setMessage(e instanceof Error ? e.message : 'Could not update permissions.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <details className="permission-editor">
      <summary>Edit permissions</summary>
      <div className="permission-options">
        {options.map(option => (
          <label key={option.id}>
            <input
              type="checkbox"
              checked={selected.includes(option.id)}
              onChange={() =>
                setSelected(values =>
                  values.includes(option.id) ? values.filter(value => value !== option.id) : [...values, option.id]
                )
              }
            />
            {option.label}
          </label>
        ))}
      </div>
      {message && <small className="error-text">{message}</small>}
      <button type="button" disabled={busy || !selected.length} onClick={() => void save()}>
        {busy ? 'Saving…' : 'Save permissions'}
      </button>
    </details>
  )
}

export default function MembersPage({
  token,
  business,
  books,
  onBack,
  onToast
}: {
  token: string
  business: Business
  books: Cashbook[]
  onBack: () => void
  onToast: (message: string) => void
}) {
  const [scope, setScope] = useState<'business' | 'cashbook'>('business')
  const [cashbookId, setCashbookId] = useState(books[0]?.id ?? '')
  const [members, setMembers] = useState<Member[]>([])
  const [pending, setPending] = useState<Invitation[]>([])
  const [sent, setSent] = useState<Invitation[]>([])
  const [sentStatus, setSentStatus] = useState<string>('Pending')
  const [memberSearch, setMemberSearch] = useState('')
  const [email, setEmail] = useState('')
  const [remarks, setRemarks] = useState('')
  const [permissions, setPermissions] = useState<number[]>([1, 2])
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
      request<Invitation[]>(
        `/api/Invitations/mysentInvitations${sentStatus ? `?status=${encodeURIComponent(sentStatus)}` : ''}`,
        token
      )
    ])
    if (results[0].status === 'fulfilled') setMembers(results[0].value ?? [])
    else {
      setMembers([])
      setError(results[0].reason instanceof Error ? results[0].reason.message : 'Could not load members.')
    }
    setPending(results[1].status === 'fulfilled' ? results[1].value ?? [] : [])
    setSent(results[2].status === 'fulfilled' ? results[2].value ?? [] : [])
    setLoading(false)
  }, [memberUrl, token, scope, cashbookId, sentStatus])

  useEffect(() => {
    void reload()
  }, [reload])

  function handleScopeChange(newScope: 'business' | 'cashbook') {
    setScope(newScope)
    setPermissions(newScope === 'business' ? [1, 2] : [1, 2, 3])
  }

  async function sendInvite(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError('')
    const path =
      scope === 'business'
        ? `/api/Invitations/business/${business.id}`
        : `/api/Invitations/business/${business.id}/cashbooks/${cashbookId}`
    try {
      await request(path, token, {
        method: 'POST',
        body: JSON.stringify({ invitedEmail: email, remarks, permissions })
      })
      setEmail('')
      setRemarks('')
      onToast('Invitation sent')
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
      onToast(action === 'accept' ? 'Invitation accepted' : action === 'reject' ? 'Invitation declined' : 'Invitation revoked')
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : `Could not ${action} invitation.`)
    } finally {
      setBusy(false)
    }
  }

  async function removeMember(member: Member) {
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

  return (
    <section className="members-page">
      <div className="history-heading">
        <div>
          <button className="history-back" onClick={onBack}>
            ← Back to overview
          </button>
          <div className="eyebrow">WORKSPACE ACCESS</div>
          <h1>Members & invitations</h1>
          <p className="muted">Manage access to {business.name}.</p>
        </div>
        <button className="button secondary-action" onClick={() => void reload()} disabled={loading}>
          <RefreshCw size={14} /> Refresh
        </button>
      </div>

      {error && (
        <div className="error-banner">
          {error}
          <button onClick={() => setError('')}>
            <X size={14} /> Dismiss
          </button>
        </div>
      )}

      <div className="members-layout">
        <article className="panel invite-panel">
          <div className="panel-head">
            <div>
              <h2>
                <UserRoundPlus size={17} /> Invite someone
              </h2>
              <p>Send access to this business or one cashbook.</p>
            </div>
            <span className="member-panel-icon">
              <MailPlus size={18} />
            </span>
          </div>
          <form className="invite-form" onSubmit={sendInvite}>
            <label>
              Access scope
              <select value={scope} onChange={event => handleScopeChange(event.target.value as typeof scope)}>
                <option value="business">Entire business</option>
                <option value="cashbook">One cashbook</option>
              </select>
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
              Email address
              <input
                type="email"
                required
                value={email}
                onChange={event => setEmail(event.target.value)}
                placeholder="teammate@example.com"
              />
            </label>
            <label>
              Note <span className="muted">(optional)</span>
              <textarea
                rows={2}
                maxLength={300}
                value={remarks}
                onChange={event => setRemarks(event.target.value)}
                placeholder="Add a short note"
              />
            </label>
            <fieldset>
              <legend>Permissions</legend>
              <div className="permission-options">
                {selectedPermissions.map(item => (
                  <label key={item.id}>
                    <input type="checkbox" checked={permissions.includes(item.id)} onChange={() => togglePermission(item.id)} />
                    {item.label}
                  </label>
                ))}
              </div>
            </fieldset>
            <button className="button primary full" disabled={busy || (scope === 'cashbook' && !cashbookId)}>
              <MailPlus size={15} />
              {busy ? 'Sending…' : 'Send invitation'}
            </button>
          </form>
        </article>

        <article className="panel member-list-panel">
          <div className="panel-head">
            <div>
              <h2>
                <Shield size={17} /> Current members
              </h2>
              <p>{scope === 'business' ? business.name : books.find(book => book.id === cashbookId)?.cashBookName ?? 'Selected cashbook'}</p>
            </div>
            <div className="member-scope">
              <button className={scope === 'business' ? 'selected' : ''} onClick={() => handleScopeChange('business')}>
                Business
              </button>
              <button className={scope === 'cashbook' ? 'selected' : ''} onClick={() => handleScopeChange('cashbook')}>
                Cashbook
              </button>
            </div>
          </div>

          <div className="member-search-bar" style={{ padding: '0 0 10px', display: 'flex', alignItems: 'center', gap: 6 }}>
            <Search size={14} className="muted" />
            <input
              type="text"
              placeholder="Filter members by name or email…"
              value={memberSearch}
              onChange={e => setMemberSearch(e.target.value)}
              style={{
                border: '1px solid #e2ebe4',
                borderRadius: 7,
                padding: '6px 10px',
                fontSize: 11,
                width: '100%',
                outline: 'none'
              }}
            />
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
              {filteredMembers.map(member => (
                <div className="member-row" key={`${member.userId}-${scope}`}>
                  <span className="member-avatar">
                    <UserRound size={17} />
                  </span>
                  <div className="member-info">
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
                      <strong>{member.fullName || member.email}</strong>
                      <span className={`member-status-pill ${(member.status || 'active').toLowerCase()}`}>
                        {member.status || 'Active'}
                      </span>
                    </div>
                    <small>{member.email}</small>
                    <div className="member-perm-badges">
                      {member.permissions && member.permissions.length > 0 ? (
                        member.permissions.map(p => (
                          <span key={p} className="member-perm-tag">
                            {permissionCodeMap[p] || p}
                          </span>
                        ))
                      ) : (
                        <span className="member-perm-tag empty">No permissions listed</span>
                      )}
                    </div>
                    <MemberPermissionEditor
                      key={`${member.userId}-${scope}`}
                      member={member}
                      scope={scope}
                      businessId={business.id}
                      cashbookId={cashbookId}
                      token={token}
                      onSaved={async () => {
                        onToast('Member permissions updated')
                        await reload()
                      }}
                    />
                  </div>
                  <button
                    className="member-remove"
                    title="Remove member"
                    aria-label={`Remove ${member.fullName || member.email}`}
                    disabled={busy}
                    onClick={() => setConfirmRemove(member)}
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              ))}
            </div>
          ) : (
            <div className="member-empty">
              {members.length ? 'No members match your search.' : 'No members found for this scope.'}
            </div>
          )}
        </article>
      </div>

      <div className="invitation-columns">
        <article className="panel invitation-panel">
          <div className="panel-head">
            <div>
              <h2>Invitations for you</h2>
              <p>Pending invitations you can respond to.</p>
            </div>
            <span className="invitation-count">{pending.length}</span>
          </div>
          {pending.length ? (
            pending.map(invite => (
              <div className="invitation-row" key={invite.id}>
                <div>
                  <strong>
                    {invite.businessName}
                    {invite.cashbookName ? ` · ${invite.cashbookName}` : ''}
                  </strong>
                  <small>From {invite.invitedByName} · {invite.invitedEmail}</small>
                </div>
                <div className="invitation-actions">
                  <button disabled={busy} onClick={() => void invitationAction(invite.id, 'accept')}>
                    <Check size={14} /> Accept
                  </button>
                  <button disabled={busy} onClick={() => void invitationAction(invite.id, 'reject')}>
                    <X size={14} /> Decline
                  </button>
                </div>
              </div>
            ))
          ) : (
            <p className="muted invitation-empty">You’re all caught up.</p>
          )}
        </article>

        <article className="panel invitation-panel">
          <div className="panel-head">
            <div>
              <h2>Sent invitations</h2>
              <p>Invitations sent to teammates.</p>
            </div>
            <div className="sent-filter-chips" style={{ display: 'flex', gap: 4 }}>
              {['Pending', 'Accepted', 'Revoked'].map(st => (
                <button
                  key={st}
                  onClick={() => setSentStatus(st)}
                  style={{
                    border: '1px solid ' + (sentStatus === st ? '#188356' : '#e2ebe4'),
                    background: sentStatus === st ? '#188356' : '#fff',
                    color: sentStatus === st ? '#fff' : '#64796e',
                    borderRadius: 6,
                    padding: '3px 7px',
                    fontSize: 9,
                    fontWeight: 700,
                    cursor: 'pointer'
                  }}
                >
                  {st}
                </button>
              ))}
            </div>
          </div>
          {sent.length ? (
            sent.map(invite => (
              <div className="invitation-row" key={invite.id}>
                <div>
                  <strong>{invite.invitedEmail}</strong>
                  <small>
                    {invite.businessName}
                    {invite.cashbookName ? ` · ${invite.cashbookName}` : ''} · {invite.status}
                  </small>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  <button
                    type="button"
                    className="copy-invitation-btn"
                    title="Copy invite URL"
                    onClick={() => {
                      const inviteUrl = `${window.location.origin}?inviteToken=${invite.id}`
                      navigator.clipboard.writeText(inviteUrl)
                      onToast(`Invite link copied for ${invite.invitedEmail}`)
                    }}
                  >
                    <Copy size={13} strokeWidth={1.8} /> Copy link
                  </button>
                  {invite.status?.toLowerCase() === 'pending' && (
                    <button className="revoke-invitation" disabled={busy} onClick={() => void invitationAction(invite.id, 'revoke')}>
                      <X size={14} /> Revoke
                    </button>
                  )}
                </div>
              </div>
            ))
          ) : (
            <p className="muted invitation-empty">No {sentStatus.toLowerCase()} invitations sent.</p>
          )}
        </article>
      </div>

      {confirmRemove && (
        <div className="overlay" onMouseDown={e => e.target === e.currentTarget && !busy && setConfirmRemove(null)}>
          <section className="dialog" style={{ maxWidth: 420 }}>
            <header>
              <div>
                <div className="eyebrow" style={{ color: '#b95f4b' }}>REMOVE MEMBER</div>
                <h2>Remove access?</h2>
              </div>
              <button className="close" onClick={() => setConfirmRemove(null)} disabled={busy}><X size={16} /></button>
            </header>
            <div className="dialog-content">
              <p>
                Are you sure you want to remove <strong>{confirmRemove.fullName || confirmRemove.email}</strong> from {scope === 'business' ? business.name : 'this cashbook'}? They will lose access immediately.
              </p>
              <div className="form-between" style={{ marginTop: 20 }}>
                <button className="button secondary-action" disabled={busy} onClick={() => setConfirmRemove(null)}>Cancel</button>
                <button className="button danger" disabled={busy} onClick={() => void removeMember(confirmRemove)}>
                  {busy ? 'Removing…' : 'Yes, remove member'}
                </button>
              </div>
            </div>
          </section>
        </div>
      )}
    </section>
  )
}
