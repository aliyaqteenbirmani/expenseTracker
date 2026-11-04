import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import {
  ArrowDownLeft,
  ArrowRight,
  ArrowUpRight,
  Bell,
  BookOpen,
  BriefcaseBusiness,
  CalendarDays,
  ChartNoAxesCombined,
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  CircleHelp,
  Download,
  Fuel,
  HeartPulse,
  House,
  KeyRound,
  LayoutDashboard,
  LogOut,
  Mail,
  Paperclip,
  Pencil,
  Plus,
  ReceiptText,
  Search,
  Settings,
  ShoppingCart,
  Sparkles,
  TrendingDown,
  TrendingUp,
  Trash2,
  Utensils,
  UsersRound,
  Wallet,
  X,
  type LucideIcon
} from 'lucide-react'
import MembersPage from './MembersPage'
import ReportsPage from './ReportsPage'
import NotificationsPopover from './NotificationsPopover'
import CreateBusinessDialog from './CreateBusinessDialog'
import TransactionAttachmentDialog from './TransactionAttachmentDialog'
import UserProfileDialog from './UserProfileDialog'
import './App.css'

const API = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

type Envelope<T> = { data?: T; Data?: T; message?: string; Message?: string }
type User = {
  firstName?: string
  FirstName?: string
  lastName?: string
  LastName?: string
  email?: string
  Email?: string
  accessToken?: string
  AccessToken?: string
  refreshToken?: string
  RefreshToken?: string
}
type Business = { id: string; name: string; category?: string; description?: string }
type Cashbook = { id: string; cashBookName?: string; name?: string; businessId: string }
type Transaction = {
  id: string
  amount: number
  transactionType: string
  remarks?: string
  transactionDate?: string
  fileName?: string
  filePath?: string
}
type TransactionCategory = { label: string; Icon: LucideIcon }

function getTransactionCategory(transaction: Transaction): TransactionCategory {
  const text = (transaction.remarks ?? '').toLowerCase()
  if (transaction.transactionType?.toLowerCase() === 'cashin') return { label: 'Income', Icon: BriefcaseBusiness }
  if (/rent|house|home|electric|utility|water bill/.test(text)) return { label: 'Housing', Icon: House }
  if (/fuel|petrol|gas station|uber|taxi|ride|bus/.test(text)) return { label: 'Transport', Icon: Fuel }
  if (/food|grocery|restaurant|cafe|meal/.test(text)) return { label: 'Food', Icon: Utensils }
  if (/medical|health|doctor|pharmacy|medicine/.test(text)) return { label: 'Health', Icon: HeartPulse }
  if (/shop|clothes|store|purchase/.test(text)) return { label: 'Shopping', Icon: ShoppingCart }
  return { label: 'Other expense', Icon: ReceiptText }
}

function TransactionSkeletonRows({ count = 4 }: { count?: number }) {
  return (
    <div className="skeleton-transaction-list" aria-hidden="true">
      {Array.from({ length: count }, (_, index) => (
        <div className="table-row skeleton-row" key={index}>
          <div className="table-transaction">
            <i className="skeleton-block skeleton-icon" />
            <span>
              <i className="skeleton-block skeleton-title" />
              <i className="skeleton-block skeleton-subtitle" />
            </span>
          </div>
          <i className="skeleton-block skeleton-date" />
          <i className="skeleton-block skeleton-pill" />
          <i className="skeleton-block skeleton-amount" />
          <i className="skeleton-block skeleton-actions" />
        </div>
      ))}
    </div>
  )
}

function CashbookSkeletonRows() {
  return (
    <div className="skeleton-cashbooks" aria-hidden="true">
      {[0, 1, 2].map(index => (
        <div className="skeleton-book-row" key={index}>
          <i className="skeleton-block skeleton-icon" />
          <span>
            <i className="skeleton-block skeleton-title" />
            <i className="skeleton-block skeleton-subtitle" />
          </span>
        </div>
      ))}
    </div>
  )
}

function DashboardSkeleton() {
  return (
    <main className="dashboard-skeleton" role="status" aria-label="Loading dashboard">
      <aside>
        <i className="skeleton-block skeleton-logo" />
        <i className="skeleton-block skeleton-workspace" />
        <i className="skeleton-block skeleton-nav" />
        <i className="skeleton-block skeleton-nav" />
        <i className="skeleton-block skeleton-nav" />
      </aside>
      <section>
        <header>
          <i className="skeleton-block skeleton-crumb" />
          <i className="skeleton-block skeleton-search" />
        </header>
        <div className="skeleton-heading">
          <i className="skeleton-block skeleton-title" />
          <i className="skeleton-block skeleton-subtitle" />
        </div>
        <div className="skeleton-metrics">
          {[0, 1, 2, 3].map(index => (
            <i className="skeleton-block" key={index} />
          ))}
        </div>
        <div className="skeleton-panels">
          <article>
            <div className="skeleton-title-row">
              <i className="skeleton-block skeleton-title" />
              <i className="skeleton-block skeleton-subtitle" />
            </div>
            <TransactionSkeletonRows count={4} />
          </article>
          <article>
            <div className="skeleton-title-row">
              <i className="skeleton-block skeleton-title" />
              <i className="skeleton-block skeleton-subtitle" />
            </div>
            <CashbookSkeletonRows />
          </article>
        </div>
      </section>
    </main>
  )
}

type ReceiptDraft = {
  draftId: string
  status: string
  recognizedText?: string
  suggestedMerchant?: string
  suggestedAmount?: number
  suggestedTransactionDate?: string
  suggestedTransactionType?: string
  suggestedCategory?: string
  parsingConfidence?: number
}

class ApiError extends Error {
  readonly status: number
  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

function unwrap<T>(body: Envelope<T> | T): T {
  if (body && typeof body === 'object' && ('data' in body || 'Data' in body)) {
    const envelope = body as Envelope<T>
    return (envelope.data ?? envelope.Data) as T
  }
  return body as T
}

async function api<T>(path: string, token?: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json')

  const response = await fetch(`${API}${path}`, { ...options, headers })

  if (response.status === 401 && token) {
    const userStr = sessionStorage.getItem('spendwise-user')
    if (userStr) {
      try {
        const u = JSON.parse(userStr) as User
        const refToken = u.refreshToken ?? u.RefreshToken
        if (refToken) {
          const refreshRes = await fetch(`${API}/api/Auth/refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: refToken })
          })
          if (refreshRes.ok) {
            const refreshData = await refreshRes.json()
            const newTokens = refreshData.data ?? refreshData.Data
            if (newTokens?.accessToken) {
              u.accessToken = newTokens.accessToken
              u.refreshToken = newTokens.refreshToken ?? refToken
              sessionStorage.setItem('spendwise-user', JSON.stringify(u))
              headers.set('Authorization', `Bearer ${newTokens.accessToken}`)
              const retryRes = await fetch(`${API}${path}`, { ...options, headers })
              const retryBody = await retryRes.json().catch(() => ({})) as Envelope<T> & { message?: string; Message?: string }
              if (!retryRes.ok) {
                throw new ApiError(retryBody.message ?? retryBody.Message ?? `Request failed (${retryRes.status})`, retryRes.status)
              }
              return unwrap(retryBody)
            }
          }
        }
      } catch {
        // Fall through to normal 401 handling
      }
    }
  }

  const body = await response.json().catch(() => ({})) as Envelope<T> & { message?: string; Message?: string }
  if (!response.ok) {
    throw new ApiError(body.message ?? body.Message ?? `Request failed (${response.status})`, response.status)
  }
  return unwrap(body)
}

function Login({ onLogin }: { onLogin: (user: User) => void }) {
  const [mode, setMode] = useState<'login' | 'register' | 'forgot' | 'reset'>('login')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [forgotEmail, setForgotEmail] = useState('')
  const [resetToken, setResetToken] = useState('')
  const [resetEmail, setResetEmail] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const tokenParam = params.get('token') || params.get('resetToken')
    const emailParam = params.get('email')
    if (tokenParam) {
      setResetToken(tokenParam)
      if (emailParam) setResetEmail(emailParam)
      setMode('reset')
    }
  }, [])

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      const user = await api<User>('/api/Auth/Login', undefined, {
        method: 'POST',
        body: JSON.stringify({ userName: username, password })
      })
      if (!(user.accessToken ?? user.AccessToken)) throw new Error('Login response did not include an access token.')
      sessionStorage.setItem('spendwise-user', JSON.stringify(user))
      onLogin(user)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to sign in.')
    } finally {
      setBusy(false)
    }
  }

  async function register(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    setNotice('')
    const fields = new FormData(event.currentTarget)
    try {
      await api('/api/Auth/Registration', undefined, {
        method: 'POST',
        body: JSON.stringify({
          firstName: fields.get('firstName'),
          lastName: fields.get('lastName'),
          email: fields.get('email'),
          phone: fields.get('phone'),
          gender: fields.get('gender'),
          password: fields.get('password')
        })
      })
      setUsername(String(fields.get('email')))
      setPassword('')
      setMode('login')
      setNotice('Account created successfully! Sign in with your email and password.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to create your account.')
    } finally {
      setBusy(false)
    }
  }

  async function handleForgotPassword(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await api(`/api/Auth/forgot-password?email=${encodeURIComponent(forgotEmail)}`, undefined, {
        method: 'POST'
      })
      setNotice('If your email is registered, password reset instructions have been sent.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not process password reset request.')
    } finally {
      setBusy(false)
    }
  }

  async function handleResetPassword(event: FormEvent) {
    event.preventDefault()
    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await api('/api/Auth/reset-password', undefined, {
        method: 'POST',
        body: JSON.stringify({
          email: resetEmail,
          token: resetToken,
          password: newPassword,
          confirmPassword
        })
      })
      setNotice('Password reset successfully! You can now sign in with your new password.')
      setUsername(resetEmail)
      setMode('login')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not reset password. Ensure the token is valid.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="login-screen">
      <section className="login-art">
        <div className="art-brand">
          <span className="brand-symbol">
            <Wallet size={18} strokeWidth={1.9} />
          </span>{' '}
          spendwise<span>.</span>
        </div>
        <div className="art-copy">
          <div className="eyebrow">MONEY, MADE CLEAR</div>
          <h1>Your finances,<br />in a better place.</h1>
          <p>One calm space to see where your money goes and what matters next.</p>
        </div>
        <div className="art-card">
          <span>YOUR BALANCE</span>
          <strong>Rs. 248,560</strong>
          <div className="art-chart"><i /><i /><i /><i /><i /><i /><i /><i /><i /><i /><i /><i /></div>
          <small><b>↗ 12.8%</b> &nbsp; this month</small>
        </div>
        <span className="art-note note-one">✦ &nbsp; Spend with intention</span>
        <span className="art-note note-two">↗ &nbsp; Your goals, in sight</span>
        <div className="login-copyright">© 2026 Spendwise · Personal & Business finance, made simple</div>
      </section>

      <section className="login-panel">
        <div className="login-content">
          <div className="mobile-brand">
            <span className="brand-symbol">
              <Wallet size={18} strokeWidth={1.9} />
            </span>{' '}
            spendwise<span>.</span>
          </div>

          <div className="eyebrow">
            {mode === 'register' ? 'GET STARTED' : mode === 'forgot' ? 'ACCOUNT RECOVERY' : mode === 'reset' ? 'SET NEW PASSWORD' : 'WELCOME BACK'}
          </div>

          <h2>
            {mode === 'register'
              ? 'Create your account'
              : mode === 'forgot'
              ? 'Reset your password'
              : mode === 'reset'
              ? 'Choose a new password'
              : 'Sign in to your account'}
          </h2>

          <p className="muted">
            {mode === 'register'
              ? 'Set up your account to start tracking.'
              : mode === 'forgot'
              ? 'Enter your email to receive recovery instructions.'
              : mode === 'reset'
              ? 'Enter your verification token and your new password.'
              : 'Pick up right where you left off.'}
          </p>

          {notice && <div className="success-banner">{notice}</div>}

          {mode === 'register' && (
            <form className="login-form" onSubmit={register}>
              <label>First name<input name="firstName" autoComplete="given-name" maxLength={50} required /></label>
              <label>Last name<input name="lastName" autoComplete="family-name" maxLength={50} /></label>
              <label>Email address<input name="email" type="email" autoComplete="email" required /></label>
              <label>Phone number<input name="phone" type="tel" minLength={10} maxLength={15} pattern="[0-9+() -]{10,15}" required /></label>
              <label>
                Gender
                <select name="gender" defaultValue="" required>
                  <option value="" disabled>Select gender</option>
                  <option>Male</option>
                  <option>Female</option>
                  <option>Other</option>
                </select>
              </label>
              <label>Password<input name="password" type="password" autoComplete="new-password" minLength={8} maxLength={100} required /></label>
              {error && <div className="error-banner">{error}</div>}
              <button className="button primary full" disabled={busy}>
                {busy ? 'Creating account…' : 'Create account'} <span>→</span>
              </button>
            </form>
          )}

          {mode === 'login' && (
            <form className="login-form" onSubmit={submit}>
              <label>Email address<input type="email" autoComplete="username" required placeholder="you@example.com" value={username} onChange={e => setUsername(e.target.value)} /></label>
              <label>Password<input type="password" autoComplete="current-password" required placeholder="Enter your password" value={password} onChange={e => setPassword(e.target.value)} /></label>
              <div className="form-between">
                <label className="check-label"><input type="checkbox" /> Remember me</label>
                <button type="button" className="text-link" onClick={() => { setMode('forgot'); setError(''); setNotice(''); }}>Forgot password?</button>
              </div>
              {error && <div className="error-banner">{error}</div>}
              <button className="button primary full" disabled={busy}>
                {busy ? 'Signing in…' : 'Sign in'} <span>→</span>
              </button>
            </form>
          )}

          {mode === 'forgot' && (
            <form className="login-form" onSubmit={handleForgotPassword}>
              <label>
                Email address
                <input
                  type="email"
                  autoComplete="email"
                  required
                  placeholder="Enter your registered email"
                  value={forgotEmail}
                  onChange={e => setForgotEmail(e.target.value)}
                />
              </label>
              {error && <div className="error-banner">{error}</div>}
              <button className="button primary full" disabled={busy}>
                <Mail size={16} />
                {busy ? 'Sending link…' : 'Send reset link'} <span>→</span>
              </button>
              <button type="button" className="button secondary-action full" onClick={() => { setMode('login'); setError(''); }}>
                Back to Sign in
              </button>
            </form>
          )}

          {mode === 'reset' && (
            <form className="login-form" onSubmit={handleResetPassword}>
              <label>
                Account Email
                <input
                  type="email"
                  required
                  placeholder="you@example.com"
                  value={resetEmail}
                  onChange={e => setResetEmail(e.target.value)}
                />
              </label>
              <label>
                Reset Token
                <input
                  type="text"
                  required
                  placeholder="Enter token from email"
                  value={resetToken}
                  onChange={e => setResetToken(e.target.value)}
                />
              </label>
              <label>
                New Password
                <input
                  type="password"
                  required
                  minLength={8}
                  placeholder="At least 8 characters"
                  value={newPassword}
                  onChange={e => setNewPassword(e.target.value)}
                />
              </label>
              <label>
                Confirm New Password
                <input
                  type="password"
                  required
                  minLength={8}
                  placeholder="Repeat new password"
                  value={confirmPassword}
                  onChange={e => setConfirmPassword(e.target.value)}
                />
              </label>
              {error && <div className="error-banner">{error}</div>}
              <button className="button primary full" disabled={busy}>
                <KeyRound size={16} />
                {busy ? 'Updating password…' : 'Reset password'} <span>→</span>
              </button>
              <button type="button" className="button secondary-action full" onClick={() => { setMode('login'); setError(''); }}>
                Back to Sign in
              </button>
            </form>
          )}

          {mode !== 'forgot' && mode !== 'reset' && (
            <button className="switch-auth" onClick={() => { setMode(mode === 'register' ? 'login' : 'register'); setError(''); setNotice('') }}>
              {mode === 'register' ? 'Already have an account? Sign in' : 'New to Spendwise? Create an account'}
            </button>
          )}
          {mode === 'login' && <div className="secure-note"><span>✓</span> Sign-in is kept for this browser session</div>}
        </div>
        <div className="login-foot">Need help? <a href="mailto:support@spendwise.app">Contact support</a></div>
      </section>
    </main>
  )
}

function GettingStarted({ token, onComplete }: { token: string; onComplete: (business: Business, book: Cashbook) => void }) {
  const [step, setStep] = useState(1)
  const [business, setBusiness] = useState<Business | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function createBusiness(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    const fields = new FormData(event.currentTarget)
    const form = new FormData()
    form.append('Name', String(fields.get('name')))
    form.append('Category', String(fields.get('category')))
    form.append('Description', String(fields.get('description') ?? ''))
    try {
      const result = await api<Business>('/api/Business/create', token, { method: 'POST', body: form })
      const created = {
        ...result,
        id: result.id ?? (result as Business & { Id: string }).Id,
        name: result.name ?? (result as Business & { Name: string }).Name
      }
      if (!created.id) throw new Error('Business was created, but the API did not return its ID. Refresh the page and try adding a cashbook.')
      setBusiness(created)
      setStep(2)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not create the business.')
    } finally {
      setBusy(false)
    }
  }

  async function createCashbook(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!business) return
    setBusy(true)
    setError('')
    const fields = new FormData(event.currentTarget)
    const name = String(fields.get('cashBookName'))
    try {
      await api('/api/CashBook', token, { method: 'POST', body: JSON.stringify({ cashBookName: name, businessId: business.id }) })
      const result = await api<Cashbook[]>(`/api/CashBook/getall?id=${encodeURIComponent(business.id)}`, token)
      const books = (result ?? []).map(item => ({
        ...item,
        id: item.id ?? (item as Cashbook & { Id: string }).Id,
        cashBookName: item.cashBookName ?? (item as Cashbook & { CashBookName: string }).CashBookName,
        businessId: item.businessId ?? (item as Cashbook & { BusinessId: string }).BusinessId ?? business.id
      }))
      const created = books.find(item => item.cashBookName === name) ?? books[0]
      if (!created?.id) throw new Error('Cashbook was created, but the API did not return it. Refresh the page to continue.')
      onComplete(business, created)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not create the cashbook.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="setup-page">
      <section className="setup-card">
        <div className="brand">
          <span className="brand-symbol"><Wallet size={18} strokeWidth={1.9} /></span> spendwise<span>.</span>
        </div>
        <div className="eyebrow">YOUR WORKSPACE · STEP {step} OF 2</div>
        <h1>{step === 1 ? 'Let’s set up your business' : 'Create your first cashbook'}</h1>
        <p className="muted">
          {step === 1 ? 'Add the workspace where you’ll organize your finances.' : `Cashbooks keep transactions organized for ${business?.name ?? 'your business'}.`}
        </p>
        <div className="setup-progress">
          <i className={step >= 1 ? 'complete' : ''} />
          <i className={step >= 2 ? 'complete' : ''} />
        </div>
        {step === 1 ? (
          <form className="setup-form" onSubmit={createBusiness}>
            <label>Business name<input name="name" maxLength={150} required placeholder="e.g. Ali’s Store" /></label>
            <label>
              Category
              <select name="category" required defaultValue="">
                <option value="" disabled>Choose a category</option>
                <option>Retail</option>
                <option>Food & beverage</option>
                <option>Services</option>
                <option>Freelance</option>
                <option>Personal</option>
                <option>Other</option>
              </select>
            </label>
            <label>Description <span className="muted">(optional)</span><textarea name="description" maxLength={500} rows={3} placeholder="What is this workspace for?" /></label>
            <button className="button primary full" disabled={busy}>{busy ? 'Creating business…' : 'Continue'} <span>→</span></button>
          </form>
        ) : (
          <form className="setup-form" onSubmit={createCashbook}>
            <label>Cashbook name<input name="cashBookName" maxLength={200} required placeholder="e.g. Daily expenses" /></label>
            <div className="setup-tip"><span>▣</span><p>You can create more cashbooks later to track different accounts or activities.</p></div>
            <button className="button primary full" disabled={busy}>{busy ? 'Creating cashbook…' : 'Finish setup'} <span>→</span></button>
            <button type="button" className="setup-back" onClick={() => setStep(1)} disabled={busy}>← Back to business details</button>
          </form>
        )}
        {error && <div className="error-banner">{error}</div>}
      </section>
    </main>
  )
}

function CreateCashbookDialog({ token, businessId, onClose, onCreated }: { token: string; businessId: string; onClose: () => void; onCreated: (name: string) => Promise<void> }) {
  const [busy, setBusy] = useState(false)
  const [created, setCreated] = useState(false)
  const [error, setError] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    const name = String(new FormData(event.currentTarget).get('cashBookName')).trim()
    try {
      if (!created) {
        await api('/api/CashBook', token, { method: 'POST', body: JSON.stringify({ cashBookName: name, businessId }) })
        setCreated(true)
      }
      await onCreated(name)
      onClose()
    } catch (e) {
      setError(created ? `Cashbook created, but the list could not refresh. Retry to load it. ${e instanceof Error ? e.message : ''}` : e instanceof Error ? e.message : 'Could not create the cashbook.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && !busy && onClose()}>
      <section className="dialog create-cashbook-dialog">
        <header>
          <div>
            <div className="eyebrow">ORGANIZE YOUR MONEY</div>
            <h2>{created ? 'Cashbook created' : 'Create a cashbook'}</h2>
          </div>
          <button className="close" onClick={onClose} disabled={busy}><X size={16} strokeWidth={1.8} /></button>
        </header>
        <form className="dialog-content setup-form" onSubmit={submit}>
          <p className="muted">{created ? 'The cashbook is saved. Retry loading your cashbooks to select it.' : 'Create a cashbook for this business. You can switch between cashbooks from your workspace.'}</p>
          {!created && <label>Cashbook name<input name="cashBookName" autoFocus maxLength={200} required placeholder="e.g. Daily expenses" /></label>}
          {error && <div className="error-banner">{error}</div>}
          <button className="button primary full" disabled={busy}>{busy ? 'Please wait…' : created ? 'Retry loading cashbooks' : 'Create cashbook'} <span>→</span></button>
        </form>
      </section>
    </div>
  )
}

function ManageCashbookDialog({ token, book, mode, onClose, onSaved }: { token: string; book: Cashbook; mode: 'edit' | 'delete'; onClose: () => void; onSaved: (deleted: boolean) => Promise<void> }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      if (mode === 'edit') {
        const name = String(new FormData(event.currentTarget).get('cashBookName')).trim()
        await api(`/api/CashBook/${encodeURIComponent(book.id)}`, token, { method: 'PUT', body: JSON.stringify({ cashBook: name }) })
      } else {
        await api(`/api/CashBook/${encodeURIComponent(book.id)}`, token, { method: 'DELETE' })
      }
      await onSaved(mode === 'delete')
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : `Could not ${mode} the cashbook.`)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && !busy && onClose()}>
      <section className="dialog manage-cashbook-dialog">
        <header>
          <div>
            <div className="eyebrow">CASHBOOK SETTINGS</div>
            <h2>{mode === 'edit' ? 'Rename cashbook' : 'Delete cashbook?'}</h2>
          </div>
          <button className="close" onClick={onClose} disabled={busy}><X size={16} strokeWidth={1.8} /></button>
        </header>
        <form className="dialog-content setup-form" onSubmit={submit}>
          {mode === 'edit' ? (
            <label>Cashbook name<input name="cashBookName" autoFocus maxLength={200} required defaultValue={book.cashBookName ?? book.name ?? ''} /></label>
          ) : (
            <div className="delete-confirm">
              <strong>{book.cashBookName ?? book.name ?? 'This cashbook'}</strong>
              <p>This will permanently delete the cashbook. This action cannot be undone.</p>
            </div>
          )}
          {error && <div className="error-banner">{error}</div>}
          <button className={`button full ${mode === 'delete' ? 'danger' : 'primary'}`} disabled={busy}>
            {busy ? (mode === 'edit' ? 'Saving…' : 'Deleting…') : (mode === 'edit' ? 'Save changes' : 'Delete cashbook')} <span>→</span>
          </button>
        </form>
      </section>
    </div>
  )
}

function BusinessSettings({ token, business, onBack, onUpdated, onDeleted, onToast }: { token: string; business: Business; onBack: () => void; onUpdated: (business: Business) => void; onDeleted: () => void; onToast: (message: string) => void }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [confirmDelete, setConfirmDelete] = useState(false)

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    const fields = new FormData(event.currentTarget)
    const form = new FormData()
    form.append('Id', business.id)
    form.append('Name', String(fields.get('name')).trim())
    form.append('Category', String(fields.get('category')))
    form.append('Description', String(fields.get('description') ?? '').trim())
    try {
      const updated = await api<Business>('/api/Business', token, { method: 'PATCH', body: form })
      onUpdated({ ...business, ...updated, id: updated.id ?? business.id, name: updated.name ?? String(fields.get('name')).trim() })
      onToast('Business settings saved')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save business settings.')
    } finally {
      setBusy(false)
    }
  }

  async function remove() {
    setBusy(true)
    setError('')
    try {
      await api(`/api/Business/${encodeURIComponent(business.id)}`, token, { method: 'DELETE' })
      onToast('Business deleted')
      onDeleted()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not delete this business.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="business-settings">
      <div className="history-heading">
        <div>
          <button className="history-back" onClick={onBack}>← Back to overview</button>
          <div className="eyebrow">WORKSPACE SETTINGS</div>
          <h1>Business settings</h1>
          <p className="muted">Update the workspace details used across your cashbooks.</p>
        </div>
      </div>
      <div className="settings-grid">
        <article className="panel settings-panel">
          <div className="panel-head">
            <div>
              <h2><BriefcaseBusiness size={17} /> Business details</h2>
              <p>These details identify your workspace.</p>
            </div>
          </div>
          <form className="setup-form" onSubmit={save}>
            <label>Business name<input name="name" maxLength={150} required defaultValue={business.name} /></label>
            <label>
              Category
              <select name="category" defaultValue={business.category ?? ''}>
                <option value="">Choose a category</option>
                <option>Retail</option>
                <option>Food & beverage</option>
                <option>Services</option>
                <option>Freelance</option>
                <option>Personal</option>
                <option>Other</option>
              </select>
            </label>
            <label>Description <span className="muted">(optional)</span><textarea name="description" maxLength={500} rows={4} defaultValue={business.description ?? ''} /></label>
            {error && <div className="error-banner">{error}</div>}
            <button className="button primary" disabled={busy}>{busy ? 'Saving…' : 'Save business settings'} <Check size={15} /></button>
          </form>
        </article>
        <article className="panel danger-zone">
          <div className="panel-head">
            <div>
              <h2><Trash2 size={17} /> Delete business</h2>
              <p>Deleting a business also removes its cashbooks and transaction access.</p>
            </div>
          </div>
          {confirmDelete ? (
            <div className="delete-confirm">
              <strong>Delete {business.name}?</strong>
              <p>This action cannot be undone.</p>
              <div className="danger-actions">
                <button className="button danger" disabled={busy} onClick={() => void remove()}>{busy ? 'Deleting…' : 'Yes, delete business'}</button>
                <button className="button secondary-action" disabled={busy} onClick={() => setConfirmDelete(false)}>Cancel</button>
              </div>
            </div>
          ) : (
            <button className="button danger" onClick={() => setConfirmDelete(true)}><Trash2 size={15} /> Delete business</button>
          )}
        </article>
      </div>
    </section>
  )
}

function ManualTransactionDialog({ token, book, onClose, onDone }: { token: string; book: Cashbook; onClose: () => void; onDone: () => Promise<void> }) {
  const [busy, setBusy] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState('')
  const today = new Date()
  const localToday = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    const data = new FormData(event.currentTarget)
    const form = new FormData()
    form.append('CashBookId', book.id)
    form.append('Amount', String(data.get('amount')))
    form.append('TransactionType', String(data.get('type')))
    const date = String(data.get('date') ?? '')
    if (date) form.append('TransactionDate', new Date(`${date}T12:00:00`).toISOString())
    form.append('Remarks', String(data.get('remarks') ?? ''))
    const file = data.get('file')
    if (file instanceof File && file.size > 0) form.append('File', file)
    let wasSaved = saved
    try {
      if (!saved) {
        await api('/api/Transaction/AddCashTransaction', token, { method: 'POST', body: form })
        wasSaved = true
        setSaved(true)
      }
      await onDone()
      onClose()
    } catch (e) {
      setError(wasSaved ? `Transaction saved, but the list could not refresh. Retry to load it. ${e instanceof Error ? e.message : ''}` : e instanceof Error ? e.message : 'Could not save the transaction.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && !busy && onClose()}>
      <section className="dialog manual-transaction-dialog">
        <header>
          <div>
            <div className="eyebrow">CASHBOOK · {book.cashBookName ?? book.name ?? 'SELECTED'}</div>
            <h2>{saved ? 'Transaction saved' : 'Add a transaction'}</h2>
          </div>
          <button className="close" onClick={onClose} disabled={busy}><X size={16} strokeWidth={1.8} /></button>
        </header>
        <form className="dialog-content manual-form" onSubmit={submit}>
          {saved ? (
            <p className="muted">Your transaction was saved. Retry loading the cashbook activity to finish.</p>
          ) : (
            <>
              <p className="muted">Record money coming in or going out.</p>
              <label>Transaction type
                <select name="type" defaultValue="CashOut" required>
                  <option value="CashOut">Expense · money out</option>
                  <option value="CashIn">Income · money in</option>
                </select>
              </label>
              <label>Amount<input name="amount" type="number" inputMode="decimal" min="0.01" step="0.01" required placeholder="0.00" autoFocus /></label>
              <label>Date<input name="date" type="date" defaultValue={localToday} /></label>
              <label>Notes <span className="muted">(optional)</span><textarea name="remarks" maxLength={500} rows={3} placeholder="What was this transaction for?" /></label>
              <label>Receipt or Invoice attachment <span className="muted">(optional)</span><input name="file" type="file" accept="image/*,application/pdf" /></label>
            </>
          )}
          {error && <div className="error-banner">{error}</div>}
          <button className="button primary full" disabled={busy}>
            {busy ? (saved ? 'Refreshing…' : 'Saving transaction…') : (saved ? 'Retry activity refresh' : 'Save transaction')} <span>→</span>
          </button>
        </form>
      </section>
    </div>
  )
}

function ReceiptDialog({ token, book, onClose, onDone }: { token: string; book: Cashbook; onClose: () => void; onDone: () => void }) {
  const [file, setFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [draft, setDraft] = useState<ReceiptDraft | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (file && file.type.startsWith('image/')) {
      const url = URL.createObjectURL(file)
      setPreviewUrl(url)
      return () => URL.revokeObjectURL(url)
    }
    setPreviewUrl(null)
  }, [file])

  async function upload() {
    if (!file) return
    const form = new FormData()
    form.append('CashBookId', book.id)
    form.append('File', file)
    setBusy(true)
    setError('')
    try {
      setDraft(await api<ReceiptDraft>('/api/Transaction/receipts/drafts', token, { method: 'POST', body: form }))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Upload failed.')
    } finally {
      setBusy(false)
    }
  }

  async function analyze() {
    if (!draft) return
    setBusy(true)
    setError('')
    try {
      await api(`/api/Transaction/receipts/drafts/${draft.draftId}/analyze`, token, { method: 'POST' })
      await api(`/api/Transaction/receipts/drafts/${draft.draftId}/suggest`, token, { method: 'POST' })
      setDraft(await api<ReceiptDraft>(`/api/Transaction/receipts/drafts/${draft.draftId}`, token))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Receipt analysis failed.')
    } finally {
      setBusy(false)
    }
  }

  async function confirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!draft) return
    const data = new FormData(event.currentTarget)
    setBusy(true)
    setError('')
    try {
      await api(`/api/Transaction/receipts/drafts/${draft.draftId}/confirm`, token, {
        method: 'POST',
        body: JSON.stringify({
          amount: Number(data.get('amount')),
          transactionType: data.get('type'),
          transactionDate: new Date(String(data.get('date'))).toISOString(),
          merchant: data.get('merchant'),
          remarks: data.get('notes'),
          category: data.get('category')
        })
      })
      onDone()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not create transaction.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && onClose()}>
      <section className="dialog">
        <header>
          <div>
            <div className="eyebrow">RECEIPT ASSIST</div>
            <h2>Add from a receipt</h2>
          </div>
          <button className="close" onClick={onClose}><X size={16} strokeWidth={1.8} /></button>
        </header>
        <div className="dialog-content">
          {!draft && (
            <>
              <p className="muted">Upload a JPG, PNG, or PDF. Review the extracted details before saving.</p>
              <label className="dropzone">
                {previewUrl ? (
                  <div style={{ maxHeight: 110, overflow: 'hidden', borderRadius: 8, margin: '4px auto 8px' }}>
                    <img src={previewUrl} alt="Selected receipt" style={{ maxHeight: 110, maxWidth: '100%', objectFit: 'contain' }} />
                  </div>
                ) : (
                  <span className="upload-symbol"><ArrowUpRight size={20} strokeWidth={1.8} /></span>
                )}
                <strong>{file?.name ?? 'Choose a receipt file'}</strong>
                <small>{file ? `${(file.size / 1024 / 1024).toFixed(2)} MB` : 'JPG, PNG, or PDF · up to 10 MB'}</small>
                <input type="file" accept="image/jpeg,image/png,application/pdf,.jpg,.jpeg,.png,.pdf" onChange={e => setFile(e.target.files?.[0] ?? null)} />
              </label>
              <button className="button primary full" disabled={!file || busy} onClick={upload}>
                {busy ? 'Uploading…' : 'Upload receipt'} <span>→</span>
              </button>
            </>
          )}

          {draft && draft.status !== 'ReadyForReview' && (
            <div className="processing">
              <div className="processing-icon">▤</div>
              <h3>Receipt uploaded</h3>
              <p className="muted">It won’t be added as a transaction until you review and confirm it.</p>
              <button className="button primary full" disabled={busy} onClick={analyze}>
                {busy ? 'Reading receipt…' : 'Analyze receipt'} <span>→</span>
              </button>
            </div>
          )}

          {draft?.status === 'ReadyForReview' && (
            <form className="review-form" onSubmit={confirm}>
              <div className="review-alert">
                <span>✦</span>
                <div>
                  <strong>Draft ready for review</strong>
                  <small>Check each field before creating the transaction.</small>
                </div>
              </div>
              {previewUrl && (
                <div style={{ textAlign: 'center', marginBottom: 12 }}>
                  <img
                    src={previewUrl}
                    alt="Receipt preview"
                    style={{ maxHeight: 130, maxWidth: '100%', borderRadius: 8, border: '1px solid #dce8dd', objectFit: 'contain' }}
                  />
                </div>
              )}
              <div className="confidence">
                <span>Extraction confidence</span>
                <b>{Math.round((draft.parsingConfidence ?? 0) * 100)}%</b>
              </div>
              <div className="fields">
                <label>Merchant<input name="merchant" defaultValue={draft.suggestedMerchant ?? ''} placeholder="Merchant name" /></label>
                <label>Amount<input name="amount" type="number" min="0.01" step="0.01" required defaultValue={draft.suggestedAmount ?? ''} /></label>
                <label>Date<input name="date" type="date" required defaultValue={draft.suggestedTransactionDate?.slice(0, 10) ?? new Date().toISOString().slice(0, 10)} /></label>
                <label>
                  Type
                  <select name="type" defaultValue={draft.suggestedTransactionType ?? 'CashOut'}>
                    <option value="CashOut">Cash out</option>
                    <option value="CashIn">Cash in</option>
                  </select>
                </label>
                <label>Category<input name="category" defaultValue={draft.suggestedCategory ?? ''} placeholder="Category suggestion" /></label>
                <label>Notes<input name="notes" placeholder="Optional note" /></label>
              </div>
              <details>
                <summary>Recognized receipt text</summary>
                <pre>{draft.recognizedText}</pre>
              </details>
              <button className="button primary full" disabled={busy}>
                {busy ? 'Saving transaction…' : 'Confirm and add transaction'} <span>→</span>
              </button>
            </form>
          )}

          {error && <div className="error-banner">{error}</div>}
        </div>
      </section>
    </div>
  )
}

function TransactionEditDialog({ token, transaction, onClose, onSaved }: { token: string; transaction: Transaction; onClose: () => void; onSaved: () => Promise<void> }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    const body = new FormData()
    body.append('Id', transaction.id)
    body.append('TransactionDate', new Date(`${String(values.get('date'))}T12:00:00`).toISOString())
    body.append('Amount', String(values.get('amount')))
    body.append('TransactionType', String(values.get('type')))
    body.append('Remarks', String(values.get('remarks') ?? ''))
    const file = values.get('file')
    if (file instanceof File && file.size > 0) {
      body.append('File', file)
    }
    setBusy(true)
    setError('')
    try {
      await api('/api/Transaction/update', token, { method: 'PUT', body })
      await onSaved()
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not update this transaction.')
    } finally {
      setBusy(false)
    }
  }

  const initialDate = transaction.transactionDate ? new Date(transaction.transactionDate) : new Date()
  const dateValue = `${initialDate.getFullYear()}-${String(initialDate.getMonth() + 1).padStart(2, '0')}-${String(initialDate.getDate()).padStart(2, '0')}`

  return (
    <div className="overlay" onMouseDown={event => event.target === event.currentTarget && !busy && onClose()}>
      <section className="dialog manual-transaction-dialog">
        <header>
          <div>
            <div className="eyebrow">EDIT TRANSACTION</div>
            <h2>Update details</h2>
          </div>
          <button className="close" onClick={onClose} disabled={busy}><X size={16} strokeWidth={1.8} /></button>
        </header>
        <form className="dialog-content manual-form" onSubmit={submit}>
          <label>Date<input name="date" type="date" required defaultValue={dateValue} /></label>
          <label>
            Type
            <select name="type" defaultValue={transaction.transactionType} required>
              <option value="CashOut">Expense · money out</option>
              <option value="CashIn">Income · money in</option>
            </select>
          </label>
          <label>Amount<input name="amount" type="number" inputMode="decimal" min="0.01" step="0.01" required defaultValue={transaction.amount} /></label>
          <label>Notes <span className="muted">(optional)</span><textarea name="remarks" maxLength={500} rows={3} defaultValue={transaction.remarks ?? ''} placeholder="What was this transaction for?" /></label>
          <label>
            {transaction.fileName ? `Replace receipt (${transaction.fileName})` : 'Attach receipt or invoice'} <span className="muted">(optional)</span>
            <input name="file" type="file" accept="image/*,application/pdf" />
          </label>
          {error && <div className="error-banner">{error}</div>}
          <button className="button primary full" disabled={busy}>
            {busy ? 'Saving changes…' : 'Save changes'} <span>→</span>
          </button>
        </form>
      </section>
    </div>
  )
}

function TransactionDeleteDialog({ token, transaction, onClose, onDeleted }: { token: string; transaction: Transaction; onClose: () => void; onDeleted: () => Promise<void> }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function remove() {
    setBusy(true)
    setError('')
    try {
      await api(`/api/Transaction/${transaction.id}`, token, { method: 'DELETE' })
      await onDeleted()
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not delete this transaction.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="overlay" onMouseDown={event => event.target === event.currentTarget && !busy && onClose()}>
      <section className="dialog manage-cashbook-dialog">
        <header>
          <div>
            <div className="eyebrow">CASHBOOK ACTIVITY</div>
            <h2>Delete transaction?</h2>
          </div>
          <button className="close" onClick={onClose} disabled={busy}><X size={16} strokeWidth={1.8} /></button>
        </header>
        <div className="dialog-content">
          <p className="muted">This will remove “{transaction.remarks || (transaction.transactionType === 'CashIn' ? 'Cash received' : 'Cash expense')}” from this cashbook. This action cannot be undone.</p>
          {error && <div className="error-banner">{error}</div>}
          <div className="history-delete-actions">
            <button className="button secondary-action" onClick={onClose} disabled={busy}>Cancel</button>
            <button className="button danger" onClick={remove} disabled={busy}>{busy ? 'Deleting…' : 'Delete transaction'}</button>
          </div>
        </div>
      </section>
    </div>
  )
}

function TransactionRowActions({
  token,
  transaction,
  onRefresh,
  onToast
}: {
  token: string
  transaction: Transaction
  onRefresh: () => Promise<void>
  onToast: (message: string) => void
}) {
  const [editing, setEditing] = useState(false)
  const [deleting, setDeleting] = useState(false)

  return (
    <>
      <div className="history-row-actions">
        <button disabled={!transaction.id} title="Edit transaction" aria-label="Edit transaction" onClick={() => setEditing(true)}>
          <Pencil size={15} strokeWidth={1.8} />
        </button>
        <button disabled={!transaction.id} title="Delete transaction" aria-label="Delete transaction" onClick={() => setDeleting(true)}>
          <Trash2 size={15} strokeWidth={1.8} />
        </button>
      </div>
      {editing && (
        <TransactionEditDialog
          token={token}
          transaction={transaction}
          onClose={() => setEditing(false)}
          onSaved={async () => {
            await onRefresh()
            onToast('Transaction updated')
          }}
        />
      )}
      {deleting && (
        <TransactionDeleteDialog
          token={token}
          transaction={transaction}
          onClose={() => setDeleting(false)}
          onDeleted={async () => {
            await onRefresh()
            onToast('Transaction deleted')
          }}
        />
      )}
    </>
  )
}

function TransactionsPage({
  transactions,
  token,
  cashbookName,
  books,
  selectedBookId,
  onSelectBook,
  onBack,
  onAdd,
  onRefresh,
  onToast,
  onViewAttachment
}: {
  transactions: Transaction[]
  token: string
  cashbookName: string
  books?: Cashbook[]
  selectedBookId?: string
  onSelectBook?: (book: Cashbook) => void
  onBack: () => void
  onAdd: () => void
  onRefresh: () => Promise<void>
  onToast: (message: string) => void
  onViewAttachment: (transaction: Transaction) => void
}) {
  const [query, setQuery] = useState('')
  const [type, setType] = useState<'all' | 'income' | 'expense'>('all')
  const [categoryFilter, setCategoryFilter] = useState('all')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [datePickerOpen, setDatePickerOpen] = useState(false)
  const [draftFromDate, setDraftFromDate] = useState('')
  const [draftToDate, setDraftToDate] = useState('')
  const [calendarMonth, setCalendarMonth] = useState(() => new Date())

  const filteredTransactions = useMemo(() => transactions.filter(transaction => {
    const normalizedType = transaction.transactionType?.toLowerCase()
    const date = transaction.transactionDate?.slice(0, 10) ?? ''
    const matchesType = type === 'all' || (type === 'income' ? normalizedType === 'cashin' : normalizedType === 'cashout')
    const matchesQuery = (transaction.remarks ?? '').concat(' ', transaction.transactionType ?? '').toLowerCase().includes(query.trim().toLowerCase())
    const cat = getTransactionCategory(transaction)
    const matchesCategory = categoryFilter === 'all' || cat.label === categoryFilter
    return matchesType && matchesQuery && matchesCategory && (!fromDate || date >= fromDate) && (!toDate || date <= toDate)
  }).sort((a, b) => (b.transactionDate ?? '').localeCompare(a.transactionDate ?? '')), [transactions, query, type, categoryFilter, fromDate, toDate])

  const totalIncome = filteredTransactions.filter(t => t.transactionType?.toLowerCase() === 'cashin').reduce((sum, t) => sum + Number(t.amount || 0), 0)
  const totalExpense = filteredTransactions.filter(t => t.transactionType?.toLowerCase() === 'cashout').reduce((sum, t) => sum + Number(t.amount || 0), 0)
  const netBalance = totalIncome - totalExpense

  const dateInputValue = (date: Date) => new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 10)

  function setDatePreset(preset: 'month' | '30days') {
    const today = new Date()
    const start = preset === 'month' ? new Date(today.getFullYear(), today.getMonth(), 1) : new Date(today.getFullYear(), today.getMonth(), today.getDate() - 29)
    setFromDate(dateInputValue(start))
    setToDate(dateInputValue(today))
    setDatePickerOpen(false)
  }

  function chooseCalendarDate(date: Date) {
    const value = dateInputValue(date)
    if (!draftFromDate || draftToDate) {
      setDraftFromDate(value)
      setDraftToDate('')
      return
    }
    if (value < draftFromDate) {
      setDraftToDate(draftFromDate)
      setDraftFromDate(value)
    } else {
      setDraftToDate(value)
    }
  }

  const calendarDays = Array.from(
    { length: new Date(calendarMonth.getFullYear(), calendarMonth.getMonth() + 1, 0).getDate() },
    (_, index) => new Date(calendarMonth.getFullYear(), calendarMonth.getMonth(), index + 1)
  )
  const calendarOffset = new Date(calendarMonth.getFullYear(), calendarMonth.getMonth(), 1).getDay()
  const dateRangeLabel = fromDate || toDate ? `${fromDate ? new Date(`${fromDate}T00:00:00`).toLocaleDateString() : 'Start'} – ${toDate ? new Date(`${toDate}T00:00:00`).toLocaleDateString() : 'End'}` : 'Date range'

  const groups = filteredTransactions.reduce<{ label: string; transactions: Transaction[] }[]>((result, transaction) => {
    const date = transaction.transactionDate ? new Date(transaction.transactionDate) : null
    const now = new Date()
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
    const dateDay = date ? new Date(date.getFullYear(), date.getMonth(), date.getDate()) : null
    const daysAgo = dateDay ? Math.floor((today.getTime() - dateDay.getTime()) / 86400000) : Infinity
    const label = daysAgo === 0 ? 'Today' : daysAgo === 1 ? 'Yesterday' : daysAgo >= 2 && daysAgo <= 7 ? 'This week' : date && !Number.isNaN(date.getTime()) ? date.toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: 'numeric' }) : 'Date unavailable'
    const current = result[result.length - 1]
    if (current?.label === label) current.transactions.push(transaction)
    else result.push({ label, transactions: [transaction] })
    return result
  }, [])

  function exportCsv() {
    const escapeCell = (value: unknown) => `"${String(value ?? '').replace(/"/g, '""')}"`
    const rows = [
      ['Date', 'Description', 'Category', 'Type', 'Amount', 'Currency', 'Cashbook', 'Transaction ID'],
      ...filteredTransactions.map(transaction => [
        transaction.transactionDate?.slice(0, 10) ?? '',
        transaction.remarks || (transaction.transactionType?.toLowerCase() === 'cashin' ? 'Cash received' : 'Cash expense'),
        getTransactionCategory(transaction).label,
        transaction.transactionType?.toLowerCase() === 'cashin' ? 'Income' : 'Expense',
        Number(transaction.amount || 0), 'PKR', cashbookName, transaction.id,
      ]),
    ]
    const csv = '\uFEFF' + rows.map(row => row.map(escapeCell).join(',')).join('\r\n')
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }))
    const link = document.createElement('a')
    link.href = url
    link.download = `spendwise-transactions-${fromDate || 'all'}-${toDate || 'dates'}.csv`
    document.body.appendChild(link)
    link.click()
    link.remove()
    URL.revokeObjectURL(url)
    onToast(`Exported ${filteredTransactions.length} transaction${filteredTransactions.length === 1 ? '' : 's'} to CSV`)
  }

  return (
    <section className="transaction-history">
      <div className="history-heading">
        <div>
          <button className="history-back" onClick={onBack}>
            <ArrowDownLeft size={14} strokeWidth={1.8} /> Back to overview
          </button>
          <div className="eyebrow">CASHBOOK ACTIVITY</div>
          <h1>All transactions</h1>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
            {books && books.length > 1 ? (
              <select
                value={selectedBookId}
                onChange={e => {
                  const chosen = books.find(b => b.id === e.target.value)
                  if (chosen && onSelectBook) onSelectBook(chosen)
                }}
                style={{
                  border: '1px solid #d8e4da',
                  borderRadius: 7,
                  padding: '4px 8px',
                  fontSize: 11,
                  fontWeight: 700,
                  color: '#1a4734',
                  background: '#fff',
                  outline: 'none'
                }}
              >
                {books.map(b => (
                  <option key={b.id} value={b.id}>
                    {b.cashBookName ?? b.name ?? 'Cashbook'}
                  </option>
                ))}
              </select>
            ) : (
              <span className="muted" style={{ fontSize: 11 }}>{cashbookName}</span>
            )}
            <span className="muted" style={{ fontSize: 11 }}>· {filteredTransactions.length} of {transactions.length} transactions</span>
          </div>
        </div>
        <button className="button primary" onClick={onAdd}>
          <Plus size={15} strokeWidth={1.8} /> Add transaction
        </button>
      </div>

      <div className="transaction-summary">
        <article className="summary-income">
          <span><TrendingUp size={16} /></span>
          <small>TOTAL INCOME</small>
          <strong>Rs. {totalIncome.toLocaleString('en-PK')}</strong>
        </article>
        <article className="summary-expense">
          <span><TrendingDown size={16} /></span>
          <small>TOTAL EXPENSES</small>
          <strong>Rs. {totalExpense.toLocaleString('en-PK')}</strong>
        </article>
        <article className="summary-balance">
          <span><Wallet size={16} /></span>
          <small>NET BALANCE</small>
          <strong>Rs. {netBalance.toLocaleString('en-PK')}</strong>
        </article>
      </div>

      <div className="history-filters">
        <label className="history-search">
          <Search size={15} strokeWidth={1.8} />
          <input aria-label="Search transactions" placeholder="Search notes or type" value={query} onChange={event => setQuery(event.target.value)} />
        </label>
        <div className="type-chips" role="group" aria-label="Filter by transaction type">
          <button className={type === 'all' ? 'selected' : ''} onClick={() => setType('all')}>All</button>
          <button className={type === 'income' ? 'selected' : ''} onClick={() => setType('income')}>Income</button>
          <button className={type === 'expense' ? 'selected' : ''} onClick={() => setType('expense')}>Expense</button>
        </div>
        <select
          aria-label="Filter by category"
          value={categoryFilter}
          onChange={e => setCategoryFilter(e.target.value)}
          style={{
            height: 36,
            border: '1px solid #e1e7e3',
            borderRadius: 8,
            padding: '0 10px',
            fontSize: 10,
            fontWeight: 650,
            color: '#4b6355',
            background: '#fff',
            outline: 'none'
          }}
        >
          <option value="all">All categories</option>
          <option value="Food">Food & Dining</option>
          <option value="Transport">Transport & Fuel</option>
          <option value="Housing">Housing & Bills</option>
          <option value="Health">Health & Medical</option>
          <option value="Shopping">Shopping</option>
          <option value="Income">Income</option>
          <option value="Other expense">Other</option>
        </select>
        <div className="date-range-control">
          <button className={'date-range-button ' + (datePickerOpen ? 'open' : '')} aria-expanded={datePickerOpen} onClick={() => { if (!datePickerOpen) { setDraftFromDate(fromDate); setDraftToDate(toDate); setCalendarMonth(fromDate ? new Date(`${fromDate}T00:00:00`) : new Date()) } setDatePickerOpen(!datePickerOpen) }}>
            <CalendarDays size={15} /><span>{dateRangeLabel}</span><ChevronDown size={14} />
          </button>
          {datePickerOpen && (
            <div className="date-range-popover">
              <div className="date-preset-list">
                <button onClick={() => setDatePreset('month')}>This month</button>
                <button onClick={() => setDatePreset('30days')}>Last 30 days</button>
              </div>
              <div className="calendar-heading">
                <button aria-label="Previous month" onClick={() => setCalendarMonth(new Date(calendarMonth.getFullYear(), calendarMonth.getMonth() - 1, 1))}><ChevronLeft size={16} /></button>
                <strong>{calendarMonth.toLocaleDateString(undefined, { month: 'long', year: 'numeric' })}</strong>
                <button aria-label="Next month" onClick={() => setCalendarMonth(new Date(calendarMonth.getFullYear(), calendarMonth.getMonth() + 1, 1))}><ChevronRight size={16} /></button>
              </div>
              <div className="calendar-grid">
                <span>Su</span><span>Mo</span><span>Tu</span><span>We</span><span>Th</span><span>Fr</span><span>Sa</span>
                {Array.from({ length: calendarOffset }, (_, index) => <i key={'blank-' + index} />)}
                {calendarDays.map(date => {
                  const value = dateInputValue(date)
                  const inRange = draftFromDate && draftToDate && value > draftFromDate && value < draftToDate
                  const selected = value === draftFromDate || value === draftToDate
                  return (
                    <button key={value} className={(selected ? 'selected' : '') + (inRange ? ' in-range' : '')} onClick={() => chooseCalendarDate(date)}>
                      {date.getDate()}
                    </button>
                  )
                })}
              </div>
              <div className="date-range-selection">
                <span>{draftFromDate ? new Date(`${draftFromDate}T00:00:00`).toLocaleDateString() : 'Start date'}</span>
                <ArrowRight size={13} />
                <span>{draftToDate ? new Date(`${draftToDate}T00:00:00`).toLocaleDateString() : 'End date'}</span>
              </div>
              <div className="date-range-footer">
                <button onClick={() => { setDraftFromDate(''); setDraftToDate('') }}>Reset</button>
                <button className="date-apply" onClick={() => { setFromDate(draftFromDate); setToDate(draftToDate); setDatePickerOpen(false) }}><Check size={14} /> Apply range</button>
              </div>
            </div>
          )}
        </div>
        <button className="export-button" onClick={exportCsv} disabled={!filteredTransactions.length}>
          <Download size={14} strokeWidth={1.8} /> Export CSV
        </button>
        <button className="history-clear" onClick={() => { setQuery(''); setType('all'); setCategoryFilter('all'); setFromDate(''); setToDate(''); setDatePickerOpen(false) }}>Clear filters</button>
      </div>

      <article className="panel history-panel">
        <div className="history-table-head">
          <span>TRANSACTION</span>
          <span>DATE</span>
          <span>TYPE</span>
          <span>AMOUNT</span>
          <span>ACTIONS</span>
        </div>
        {groups.length ? groups.map(group => (
          <div className="history-group" key={group.label}>
            <div className="history-date-group">{group.label}</div>
            {group.transactions.map(transaction => {
              const isIncome = transaction.transactionType?.toLowerCase() === 'cashin'
              const date = transaction.transactionDate ? new Date(transaction.transactionDate) : null
              const category = getTransactionCategory(transaction)
              const CategoryIcon = category.Icon
              const hasAttachment = Boolean(transaction.filePath || transaction.fileName)

              return (
                <div className="history-row" key={transaction.id}>
                  <div className="table-transaction">
                    <span className={'txn-icon ' + (isIncome ? 'txn-income' : 'txn-expense')}>
                      <CategoryIcon size={16} strokeWidth={1.8} />
                    </span>
                    <span>
                      <strong>{transaction.remarks || (isIncome ? 'Cash received' : 'Cash expense')}</strong>
                      <small>{category.label}</small>
                      {hasAttachment && (
                        <button
                          type="button"
                          className="txn-attachment-btn"
                          title="View receipt attachment"
                          onClick={() => onViewAttachment(transaction)}
                        >
                          <Paperclip size={11} strokeWidth={2} /> Receipt
                        </button>
                      )}
                    </span>
                  </div>
                  <span>{date && !Number.isNaN(date.getTime()) ? date.toLocaleDateString() : '—'}</span>
                  <span><em className={'pill ' + (isIncome ? 'income-pill' : 'expense-pill')}>{isIncome ? 'Income' : 'Expense'}</em></span>
                  <strong className={isIncome ? 'income-amount' : ''}>{isIncome ? '+' : '−'} Rs. {Number(transaction.amount || 0).toLocaleString('en-PK')}</strong>
                  <TransactionRowActions token={token} transaction={transaction} onRefresh={onRefresh} onToast={onToast} />
                </div>
              )
            })}
          </div>
        )) : (
          <div className="empty history-empty">
            <span className="empty-receipt"><Search size={20} /></span>
            <strong>{transactions.length ? 'No matching transactions' : 'No transactions yet'}</strong>
            <p>{transactions.length ? 'Try changing or clearing your filters.' : 'Add your first transaction to start tracking activity.'}</p>
            {!transactions.length && (
              <button className="button primary" onClick={onAdd}><Plus size={15} /> Add your first transaction</button>
            )}
          </div>
        )}
      </article>
    </section>
  )
}

function DirectInviteModal({
  token,
  inviteToken,
  onClose,
  onAccepted,
  onToast
}: {
  token: string
  inviteToken: string
  onClose: () => void
  onAccepted: () => void
  onToast: (msg: string) => void
}) {
  const [loading, setLoading] = useState(true)
  const [inviteData, setInviteData] = useState<{ id?: string; businessName?: string; cashbookName?: string; invitedByName?: string } | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    async function fetchByToken() {
      try {
        setLoading(true)
        const res = await api<unknown>(`/api/Invitations/by-token?token=${encodeURIComponent(inviteToken)}`, token)
        setInviteData(res as { id?: string; businessName?: string; cashbookName?: string; invitedByName?: string })
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Invalid or expired invitation link.')
      } finally {
        setLoading(false)
      }
    }
    void fetchByToken()
  }, [inviteToken, token])

  async function accept() {
    if (!inviteData?.id) return
    setBusy(true)
    setError('')
    try {
      await api(`/api/Invitations/${inviteData.id}/accept`, token, { method: 'POST' })
      onToast('Invitation accepted! Welcome to the workspace.')
      onAccepted()
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not accept invitation.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && onClose()}>
      <section className="dialog">
        <header>
          <div>
            <div className="eyebrow">WORKSPACE INVITATION</div>
            <h2>You’ve been invited!</h2>
          </div>
          <button className="close" onClick={onClose}><X size={16} /></button>
        </header>
        <div className="dialog-content">
          {loading && <p className="muted">Validating invitation link…</p>}
          {error && <div className="error-banner">{error}</div>}
          {!loading && !error && inviteData && (
            <div>
              <p>
                <strong>{inviteData.invitedByName || 'A teammate'}</strong> invited you to join{' '}
                <strong>{inviteData.businessName}</strong>
                {inviteData.cashbookName ? ` (${inviteData.cashbookName})` : ''}.
              </p>
              <div className="form-between" style={{ marginTop: 20 }}>
                <button className="button secondary-action" onClick={onClose} disabled={busy}>Cancel</button>
                <button className="button primary" onClick={accept} disabled={busy}>
                  {busy ? 'Accepting…' : 'Accept Invitation'}
                </button>
              </div>
            </div>
          )}
        </div>
      </section>
    </div>
  )
}

function Dashboard({ user, onLogout }: { user: User; onLogout: () => void }) {
  const token = user.accessToken ?? user.AccessToken ?? ''
  const [businesses, setBusinesses] = useState<Business[]>([])
  const [businessId, setBusinessId] = useState('')
  const [books, setBooks] = useState<Cashbook[]>([])
  const [book, setBook] = useState<Cashbook | null>(null)
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [showReceipt, setShowReceipt] = useState(false)
  const [showManualTransaction, setShowManualTransaction] = useState(false)
  const [showAllTransactions, setShowAllTransactions] = useState(false)
  const [showMembers, setShowMembers] = useState(false)
  const [showSettings, setShowSettings] = useState(false)
  const [showReports, setShowReports] = useState(false)
  const [showCreateCashbook, setShowCreateCashbook] = useState(false)
  const [showCreateBusiness, setShowCreateBusiness] = useState(false)
  const [showNotifications, setShowNotifications] = useState(false)
  const [showProfile, setShowProfile] = useState(false)
  const [unreadInvitesCount, setUnreadInvitesCount] = useState(0)
  const [manageCashbook, setManageCashbook] = useState<{ book: Cashbook; mode: 'edit' | 'delete' } | null>(null)
  const [viewAttachment, setViewAttachment] = useState<Transaction | null>(null)
  const [pendingUrlInviteToken, setPendingUrlInviteToken] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [toast, setToast] = useState('')
  const [loading, setLoading] = useState(true)
  const [loadingBooks, setLoadingBooks] = useState(false)
  const [loadingTransactions, setLoadingTransactions] = useState(false)
  const [needsSetup, setNeedsSetup] = useState(false)

  const fullName = `${user.firstName ?? user.FirstName ?? ''} ${user.lastName ?? user.LastName ?? ''}`.trim() || user.email || user.Email || 'there'
  const searchInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        searchInputRef.current?.focus()
      }
      if (e.key === 'Escape') {
        setShowReceipt(false)
        setShowManualTransaction(false)
        setShowCreateCashbook(false)
        setShowCreateBusiness(false)
        setShowNotifications(false)
        setShowProfile(false)
        setManageCashbook(null)
        setViewAttachment(null)
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  function showToast(message: string) {
    setToast(message)
  }

  useEffect(() => {
    if (!toast) return
    const timeout = window.setTimeout(() => setToast(''), 3200)
    return () => window.clearTimeout(timeout)
  }, [toast])

  const fetchUnreadInvites = useCallback(async () => {
    try {
      const data = await api<unknown[]>('/api/Invitations/mypendingInvitations', token)
      setUnreadInvitesCount(Array.isArray(data) ? data.length : 0)
    } catch {
      // ignore badge fetch failures
    }
  }, [token])

  useEffect(() => {
    void fetchUnreadInvites()
    const timer = setInterval(() => void fetchUnreadInvites(), 45000)
    return () => clearInterval(timer)
  }, [fetchUnreadInvites])

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const inviteParam = params.get('token') || params.get('inviteToken')
    if (inviteParam) {
      setPendingUrlInviteToken(inviteParam)
    }
  }, [])

  const reloadBusinesses = useCallback(async () => {
    try {
      const list = await api<Business[]>('/api/Business/all', token)
      const normalized = (list ?? []).map(item => ({
        ...item,
        id: item.id ?? (item as Business & { Id: string }).Id,
        name: item.name ?? (item as Business & { Name: string }).Name
      }))
      setBusinesses(normalized)
      if (normalized.length) {
        setBusinessId(curr => (normalized.some(b => b.id === curr) ? curr : normalized[0].id))
        setNeedsSetup(false)
      } else {
        setNeedsSetup(true)
      }
    } catch (e) {
      if (e instanceof ApiError && e.status === 404) setNeedsSetup(true)
      else setError(e instanceof Error ? e.message : 'Could not load businesses.')
    } finally {
      setLoading(false)
    }
  }, [token])

  useEffect(() => {
    void reloadBusinesses()
  }, [reloadBusinesses])

  useEffect(() => {
    if (!businessId) {
      setBooks([])
      setBook(null)
      setLoadingBooks(false)
      return
    }
    setLoadingBooks(true)
    api<Cashbook[]>(`/api/CashBook/getall?id=${encodeURIComponent(businessId)}`, token)
      .then(list => {
        const fixed = (list ?? []).map(item => ({
          ...item,
          id: item.id ?? (item as unknown as { Id: string }).Id,
          cashBookName: item.cashBookName ?? (item as unknown as { CashBookName: string }).CashBookName
        }))
        setBooks(fixed)
        setBook(current => fixed.find(item => item.id === current?.id) ?? fixed[0] ?? null)
      })
      .catch(e => setError(e instanceof Error ? e.message : 'Could not load cashbooks.'))
      .finally(() => setLoadingBooks(false))
  }, [businessId, token])

  useEffect(() => {
    if (!book?.id) {
      setTransactions([])
      setLoadingTransactions(false)
      return
    }
    setLoadingTransactions(true)
    api<Transaction[]>(`/api/Transaction/getall?CashBookId=${encodeURIComponent(book.id)}`, token)
      .then(list => setTransactions(list ?? []))
      .catch(e => setError(e instanceof Error ? e.message : 'Could not load transactions.'))
      .finally(() => setLoadingTransactions(false))
  }, [book?.id, token])

  const visible = useMemo(
    () => transactions.filter(t => `${t.remarks ?? ''} ${t.transactionType}`.toLowerCase().includes(search.toLowerCase())),
    [transactions, search]
  )
  const income = transactions.filter(t => t.transactionType?.toLowerCase() === 'cashin').reduce((s, t) => s + Number(t.amount || 0), 0)
  const expense = transactions.filter(t => t.transactionType?.toLowerCase() === 'cashout').reduce((s, t) => s + Number(t.amount || 0), 0)
  const balance = income - expense

  async function reloadCashbooks(selectName?: string) {
    if (!businessId) return
    const list = await api<Cashbook[]>(`/api/CashBook/getall?id=${encodeURIComponent(businessId)}`, token)
    const fixed = (list ?? []).map(item => ({
      ...item,
      id: item.id ?? (item as unknown as { Id: string }).Id,
      cashBookName: item.cashBookName ?? (item as unknown as { CashBookName: string }).CashBookName,
      businessId: item.businessId ?? (item as unknown as { BusinessId: string }).BusinessId ?? businessId
    }))
    setBooks(fixed)
    setBook(fixed.find(item => item.cashBookName === selectName) ?? fixed.find(item => item.id === book?.id) ?? fixed[0] ?? null)
  }

  async function reloadTransactions(bookId = book?.id) {
    if (!bookId) return
    const list = await api<Transaction[]>(`/api/Transaction/getall?CashBookId=${encodeURIComponent(bookId)}`, token)
    setTransactions(list ?? [])
  }

  if (loading) return <DashboardSkeleton />
  if (needsSetup) {
    return (
      <GettingStarted
        token={token}
        onComplete={(createdBusiness, createdBook) => {
          setBusinesses([createdBusiness])
          setBusinessId(createdBusiness.id)
          setBooks([createdBook])
          setBook(createdBook)
          setNeedsSetup(false)
        }}
      />
    )
  }

  const selectedBusiness = businesses.find(item => item.id === businessId)

  function showOverview() {
    setShowMembers(false)
    setShowSettings(false)
    setShowReports(false)
    setShowAllTransactions(false)
  }

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-symbol"><Wallet size={18} strokeWidth={1.9} /></span> spendwise<span>.</span>
        </div>

        <small className="side-label">WORKSPACE</small>
        <div className="workspace-wrapper">
          <label className="workspace">
            <span className="workspace-icon">{(selectedBusiness?.name ?? 'S').slice(0, 1).toUpperCase()}</span>
            <select
              value={businessId}
              onChange={e => {
                setBusinessId(e.target.value)
                showOverview()
              }}
            >
              {businesses.map(b => (
                <option key={b.id} value={b.id}>{b.name}</option>
              ))}
            </select>
            <span><ChevronDown size={14} strokeWidth={1.8} /></span>
          </label>
          <button
            className="add-workspace-btn"
            title="Create new workspace"
            aria-label="Create new workspace"
            onClick={() => setShowCreateBusiness(true)}
          >
            <Plus size={16} strokeWidth={2} />
          </button>
        </div>

        <small className="side-label">MENU</small>
        <nav>
          <button
            className={'nav ' + (showAllTransactions || showMembers || showSettings || showReports ? '' : 'active')}
            onClick={showOverview}
          >
            <span><LayoutDashboard size={17} strokeWidth={1.8} /></span> Overview
          </button>
          <button
            className={'nav ' + (showAllTransactions ? 'active' : '')}
            onClick={() => {
              setShowMembers(false)
              setShowSettings(false)
              setShowReports(false)
              if (showAllTransactions) setShowAllTransactions(false)
              else document.getElementById('cashbooks')?.scrollIntoView({ behavior: 'smooth' })
            }}
          >
            <span><BookOpen size={17} strokeWidth={1.8} /></span> Cashbooks <i>{books.length}</i>
          </button>
          <button
            className={'nav ' + (showReports ? 'active' : '')}
            onClick={() => {
              setShowReports(true)
              setShowMembers(false)
              setShowSettings(false)
              setShowAllTransactions(false)
            }}
          >
            <span><ChartNoAxesCombined size={17} strokeWidth={1.8} /></span> Reports
          </button>
          <button
            className={'nav ' + (showMembers ? 'active' : '')}
            onClick={() => {
              setShowMembers(true)
              setShowReports(false)
              setShowSettings(false)
              setShowAllTransactions(false)
            }}
          >
            <span><UsersRound size={17} strokeWidth={1.8} /></span> Members
          </button>
          <div className="nav-rule" />
          <button
            className={'nav ' + (showSettings ? 'active' : '')}
            onClick={() => {
              setShowSettings(true)
              setShowReports(false)
              setShowMembers(false)
              setShowAllTransactions(false)
            }}
          >
            <span><Settings size={17} strokeWidth={1.8} /></span> Settings
          </button>
        </nav>

        <div className="sidebar-bottom">
          <div className="support-card">
            <span><CircleHelp size={16} strokeWidth={1.8} /></span>
            <strong>Need a hand?</strong>
            <p>Tips to make the most of Spendwise.</p>
            <a href="mailto:support@spendwise.app">Visit help center →</a>
          </div>
          <div className="user-row" onClick={() => setShowProfile(true)} style={{ cursor: 'pointer' }} title="View profile & account details">
            <span className="user-avatar">{fullName.slice(0, 1).toUpperCase()}</span>
            <span>
              <strong>{fullName}</strong>
              <small>{user.email ?? user.Email ?? 'My account'}</small>
            </span>
            <span className="logout" title="Sign out" onClick={e => { e.stopPropagation(); onLogout() }}><LogOut size={16} strokeWidth={1.8} /></span>
          </div>
        </div>
      </aside>

      <main className="main">
        <header className="topbar">
          <div className="crumb">
            Workspace <span>/</span>{' '}
            <button className="crumb-button" onClick={showOverview}>
              {showSettings
                ? 'Settings'
                : showMembers
                ? 'Members'
                : showReports
                ? 'Reports'
                : showAllTransactions
                ? 'Transactions'
                : 'Overview'}
            </button>
          </div>
          <label className="search">
            <Search size={17} strokeWidth={1.8} />
            <input
              ref={searchInputRef}
              placeholder="Search transactions..."
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
            <kbd>⌘ K</kbd>
          </label>
          <button
            className="notification"
            aria-label="Notifications"
            onClick={() => setShowNotifications(!showNotifications)}
          >
            <Bell size={18} strokeWidth={1.8} />
            {unreadInvitesCount > 0 && <span className="topbar-badge">{unreadInvitesCount}</span>}
          </button>
          <span
            className="top-user"
            style={{ cursor: 'pointer' }}
            title="View profile & account details"
            onClick={() => setShowProfile(true)}
          >
            {fullName.slice(0, 1).toUpperCase()}
          </span>
        </header>

        <div className="content">
          {showSettings && selectedBusiness ? (
            <BusinessSettings
              token={token}
              business={selectedBusiness}
              onBack={showOverview}
              onUpdated={updated => setBusinesses(items => items.map(item => (item.id === updated.id ? updated : item)))}
              onDeleted={() => {
                setBusinesses(items => items.filter(item => item.id !== businessId))
                setBooks([])
                setBook(null)
                setNeedsSetup(true)
                setShowSettings(false)
              }}
              onToast={showToast}
            />
          ) : showMembers ? (
            <MembersPage
              token={token}
              business={{ id: businessId, name: selectedBusiness?.name ?? 'Workspace' }}
              books={books}
              onBack={() => setShowMembers(false)}
              onToast={showToast}
            />
          ) : showReports ? (
            <ReportsPage
              transactions={transactions}
              cashbookName={book?.cashBookName ?? book?.name ?? 'Cashbook'}
              businessName={selectedBusiness?.name ?? 'Workspace'}
              onBack={showOverview}
              onToast={showToast}
            />
          ) : showAllTransactions ? (
            <TransactionsPage
              transactions={transactions}
              token={token}
              cashbookName={book?.cashBookName ?? book?.name ?? 'Cashbook'}
              books={books}
              selectedBookId={book?.id}
              onSelectBook={b => setBook(b)}
              onBack={() => setShowAllTransactions(false)}
              onAdd={() => {
                setShowAllTransactions(false)
                setShowManualTransaction(true)
              }}
              onRefresh={() => reloadTransactions()}
              onToast={showToast}
              onViewAttachment={txn => setViewAttachment(txn)}
            />
          ) : (
            <>
              <div className="heading">
                <div>
                  <div className="eyebrow">
                    {new Intl.DateTimeFormat('en', { weekday: 'long', month: 'long', day: 'numeric' }).format(new Date()).toUpperCase()}
                  </div>
                  <h1>Good morning, {fullName.split(' ')[0]} <span>✦</span></h1>
                  <p className="muted">Here’s what’s happening with your money today.</p>
                </div>
                <div className="heading-actions">
                  <button className="button secondary-action" disabled={!book} onClick={() => setShowManualTransaction(true)}>
                    <Plus size={15} strokeWidth={1.8} /> Add transaction
                  </button>
                  <button className="button primary" disabled={!book} onClick={() => setShowReceipt(true)}>
                    <Plus size={15} strokeWidth={1.8} /> Add receipt
                  </button>
                </div>
              </div>

              {error && (
                <div className="error-banner">
                  {error}
                  <button onClick={() => setError('')}>Dismiss</button>
                </div>
              )}

              <section className="hero">
                <div>
                  <div className="hero-kicker"><i /> YOUR MONEY AT A GLANCE</div>
                  <h2>A clearer view of<br />your financial day.</h2>
                  <p>Every transaction tells a story. Keep yours moving in the right direction.</p>
                  <button onClick={() => document.getElementById('transactions')?.scrollIntoView({ behavior: 'smooth' })}>
                    Explore your activity <ArrowRight size={15} strokeWidth={1.8} />
                  </button>
                </div>
                <div className="hero-chart">
                  <div className="hero-total">
                    <small>THIS MONTH</small>
                    <strong>Rs. {Math.abs(balance).toLocaleString('en-PK')}</strong>
                    <span><b>↗ 8.4%</b> vs last month</span>
                  </div>
                  <div className="chart-bars">
                    {[34, 52, 42, 66, 48, 72, 57, 87, 65, 96, 72, 82, 60, 91, 74].map((h, i) => (
                      <i key={i} style={{ height: `${h}%` }} />
                    ))}
                  </div>
                </div>
                <span className="hero-spark">✳</span>
              </section>

              <div className="section-heading">
                <div>
                  <h2>Your overview</h2>
                  <p>A snapshot of your selected cashbook.</p>
                </div>
                <button className="period" onClick={() => setShowReports(true)}>
                  View full report →
                </button>
              </div>

              <section className="metrics">
                <article>
                  <div className="metric-symbol green"><Wallet size={17} strokeWidth={1.8} /></div>
                  <span className="trend">↗ 8.4%</span>
                  <small>TOTAL BALANCE</small>
                  <strong>Rs. {balance.toLocaleString('en-PK')}</strong>
                  <p>Across selected cashbook</p>
                  <div className="sparkline green-line" />
                </article>
                <article>
                  <div className="metric-symbol blue"><TrendingUp size={17} strokeWidth={1.8} /></div>
                  <span className="trend">↗ 4.2%</span>
                  <small>TOTAL INCOME</small>
                  <strong>Rs. {income.toLocaleString('en-PK')}</strong>
                  <p>Money in this period</p>
                  <div className="sparkline blue-line" />
                </article>
                <article>
                  <div className="metric-symbol orange"><TrendingDown size={17} strokeWidth={1.8} /></div>
                  <span className="trend down">↘ 2.1%</span>
                  <small>TOTAL EXPENSES</small>
                  <strong>Rs. {expense.toLocaleString('en-PK')}</strong>
                  <p>Money out this period</p>
                  <div className="sparkline orange-line" />
                </article>
                <article>
                  <div className="metric-symbol purple"><ReceiptText size={17} strokeWidth={1.8} /></div>
                  <span className="trend neutral">THIS MONTH</span>
                  <small>TRANSACTIONS</small>
                  <strong>{transactions.length}</strong>
                  <p>Activity in this cashbook</p>
                  <div className="sparkline purple-line" />
                </article>
              </section>

              <section className="bottom-grid">
                <article className="panel transactions" id="transactions">
                  <div className="panel-head">
                    <div>
                      <h2>Recent transactions</h2>
                      <p>{book?.cashBookName ?? book?.name ?? 'Select a cashbook to see activity'}</p>
                    </div>
                    <button className="text-button" onClick={() => setShowAllTransactions(true)}>
                      View all <ArrowRight size={14} strokeWidth={1.8} />
                    </button>
                  </div>
                  <div className="table-head">
                    <span>TRANSACTION</span>
                    <span>DATE</span>
                    <span>TYPE</span>
                    <span>AMOUNT</span>
                    <span>ACTIONS</span>
                  </div>
                  {loadingTransactions ? (
                    <TransactionSkeletonRows count={5} />
                  ) : visible.length ? (
                    visible.slice(0, 7).map((t, i) => {
                      const hasAttachment = Boolean(t.filePath || t.fileName)
                      return (
                        <div className="table-row" key={t.id ?? `${t.remarks}-${i}`}>
                          <div className="table-transaction">
                            <span className={`txn-icon txn-${i % 4}`}>
                              {t.transactionType?.toLowerCase() === 'cashin' ? (
                                <ArrowUpRight size={16} strokeWidth={1.8} />
                              ) : (
                                <ArrowDownLeft size={16} strokeWidth={1.8} />
                              )}
                            </span>
                            <span>
                              <strong>{t.remarks || (t.transactionType === 'CashIn' ? 'Cash received' : 'Cash expense')}</strong>
                              <small>{getTransactionCategory(t).label}</small>
                              {hasAttachment && (
                                <button
                                  type="button"
                                  className="txn-attachment-btn"
                                  title="View receipt attachment"
                                  onClick={() => setViewAttachment(t)}
                                >
                                  <Paperclip size={11} strokeWidth={2} /> Receipt
                                </button>
                              )}
                            </span>
                          </div>
                          <span>{t.transactionDate ? new Date(t.transactionDate).toLocaleDateString() : '—'}</span>
                          <span>
                            <em className={t.transactionType === 'CashIn' ? 'pill income-pill' : 'pill expense-pill'}>
                              {t.transactionType === 'CashIn' ? 'Income' : 'Expense'}
                            </em>
                          </span>
                          <strong className={t.transactionType === 'CashIn' ? 'income-amount' : ''}>
                            {t.transactionType === 'CashIn' ? '+' : '−'} Rs. {Number(t.amount).toLocaleString('en-PK')}
                          </strong>
                          <TransactionRowActions token={token} transaction={t} onRefresh={() => reloadTransactions()} onToast={showToast} />
                        </div>
                      )
                    })
                  ) : (
                    <div className="empty">
                      <span className="empty-receipt"><ReceiptText size={20} strokeWidth={1.8} /></span>
                      <strong>No transactions yet</strong>
                      <p>Add a transaction or scan a receipt to get started.</p>
                      <button onClick={() => setShowReceipt(true)}>Add your first receipt →</button>
                    </div>
                  )}
                </article>

                <article className="panel cashbook-panel" id="cashbooks">
                  <div className="panel-head">
                    <div>
                      <h2>Your cashbooks</h2>
                      <p>Choose a book to focus your view.</p>
                    </div>
                    <button className="add-small" aria-label="Create a cashbook" onClick={() => setShowCreateCashbook(true)}>
                      <Plus size={16} strokeWidth={1.8} />
                    </button>
                  </div>
                  <div className="book-list">
                    {books.map((b, i) => (
                      <div className="book-entry" key={b.id}>
                        <button className={`book-row ${book?.id === b.id ? 'chosen' : ''}`} onClick={() => setBook(b)}>
                          <span className={`book-mark mark-${i % 3}`}><BookOpen size={16} strokeWidth={1.8} /></span>
                          <span>
                            <strong>{b.cashBookName ?? b.name ?? 'Cashbook'}</strong>
                            <small>{b.businessId ? 'Business cashbook' : 'Cashbook'}</small>
                          </span>
                          <span className="row-arrow">→</span>
                        </button>
                        <button className="book-action" aria-label={`Rename ${b.cashBookName ?? b.name ?? 'cashbook'}`} title="Rename cashbook" onClick={() => setManageCashbook({ book: b, mode: 'edit' })}>
                          <Pencil size={14} strokeWidth={1.8} />
                        </button>
                        <button className="book-action delete-action" aria-label={`Delete ${b.cashBookName ?? b.name ?? 'cashbook'}`} title="Delete cashbook" onClick={() => setManageCashbook({ book: b, mode: 'delete' })}>
                          <Trash2 size={14} strokeWidth={1.8} />
                        </button>
                      </div>
                    ))}
                    {loadingBooks ? <CashbookSkeletonRows /> : !books.length && <div className="book-empty">No cashbooks found for this business.</div>}
                  </div>
                  <button className="create-book" onClick={() => setShowCreateCashbook(true)}>
                    <Plus size={15} strokeWidth={1.8} /> Create a cashbook
                  </button>
                  <div className="insight-card">
                    <span><Sparkles size={16} strokeWidth={1.8} /></span>
                    <div>
                      <small>SPENDING INSIGHT</small>
                      <strong>Stay in the know</strong>
                      <p>Review activity to spot your spending patterns.</p>
                    </div>
                  </div>
                </article>
              </section>

              <footer>
                Made for a clearer money story <span>✦</span>
                <a href={`${API}/swagger`} target="_blank" rel="noreferrer">API Documentation</a>
              </footer>
            </>
          )}
        </div>
      </main>

      {showReceipt && book && (
        <ReceiptDialog
          token={token}
          book={book}
          onClose={() => setShowReceipt(false)}
          onDone={() => {
            setShowReceipt(false)
            api<Transaction[]>(`/api/Transaction/getall?CashBookId=${encodeURIComponent(book.id)}`, token)
              .then(setTransactions)
              .catch(e => setError(e instanceof Error ? e.message : 'Could not refresh transactions.'))
          }}
        />
      )}

      {showManualTransaction && book && (
        <ManualTransactionDialog
          token={token}
          book={book}
          onClose={() => setShowManualTransaction(false)}
          onDone={async () => reloadTransactions(book.id)}
        />
      )}

      {showCreateCashbook && businessId && (
        <CreateCashbookDialog
          token={token}
          businessId={businessId}
          onClose={() => setShowCreateCashbook(false)}
          onCreated={reloadCashbooks}
        />
      )}

      {showCreateBusiness && (
        <CreateBusinessDialog
          token={token}
          onClose={() => setShowCreateBusiness(false)}
          onCreated={created => {
            setBusinesses(prev => [...prev, created])
            setBusinessId(created.id)
            showToast(`Workspace "${created.name}" created`)
            showOverview()
          }}
        />
      )}

      {manageCashbook && (
        <ManageCashbookDialog
          token={token}
          book={manageCashbook.book}
          mode={manageCashbook.mode}
          onClose={() => setManageCashbook(null)}
          onSaved={async () => reloadCashbooks()}
        />
      )}

      {viewAttachment && (
        <TransactionAttachmentDialog
          token={token}
          transactionId={viewAttachment.id}
          fileName={viewAttachment.fileName}
          filePath={viewAttachment.filePath}
          remarks={viewAttachment.remarks}
          onClose={() => setViewAttachment(null)}
        />
      )}

      {showNotifications && (
        <NotificationsPopover
          token={token}
          onClose={() => setShowNotifications(false)}
          onInvitationProcessed={() => {
            void reloadBusinesses()
            void fetchUnreadInvites()
          }}
          onToast={showToast}
        />
      )}

      {pendingUrlInviteToken && (
        <DirectInviteModal
          token={token}
          inviteToken={pendingUrlInviteToken}
          onClose={() => setPendingUrlInviteToken(null)}
          onAccepted={() => {
            void reloadBusinesses()
            void fetchUnreadInvites()
          }}
          onToast={showToast}
        />
      )}

      {showProfile && (
        <UserProfileDialog
          user={user}
          token={token}
          workspaceCount={businesses.length}
          onClose={() => setShowProfile(false)}
          onLogout={onLogout}
          onToast={showToast}
        />
      )}

      {toast && (
        <div className="toast-message" role="status" aria-live="polite">
          <Check size={16} strokeWidth={2} />
          {toast}
        </div>
      )}
    </div>
  )
}

function App() {
  const [user, setUser] = useState<User | null>(() => {
    try {
      return JSON.parse(sessionStorage.getItem('spendwise-user') ?? 'null') as User | null
    } catch {
      return null
    }
  })

  function logout() {
    sessionStorage.removeItem('spendwise-user')
    setUser(null)
  }

  return user && (user.accessToken || user.AccessToken) ? (
    <Dashboard user={user} onLogout={logout} />
  ) : (
    <Login onLogin={setUser} />
  )
}

export default App
