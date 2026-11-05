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
  CheckCircle2,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  CircleHelp,
  Download,
  Fuel,
  HeartPulse,
  House,
  Image as ImageIcon,
  KeyRound,
  LayoutDashboard,
  LogOut,
  Mail,
  Menu,
  Paperclip,
  Pencil,
  Plus,
  Receipt,
  ReceiptText,
  RefreshCw,
  Search,
  Settings,
  ShoppingCart,
  Sparkles,
  TrendingDown,
  TrendingUp,
  Trash2,
  Upload,
  Utensils,
  UsersRound,
  Wallet,
  X,
  type LucideIcon
} from 'lucide-react'
import MembersPage from './MembersPage'
import ReportsPage from './ReportsPage'
import CashbooksPage from './CashbooksPage'
import NotificationsPopover from './NotificationsPopover'
import CreateBusinessDialog from './CreateBusinessDialog'
import TransactionAttachmentDialog from './TransactionAttachmentDialog'
import UserProfileDialog from './UserProfileDialog'
import TransactionDetailDialog from './TransactionDetailDialog'
import MonthlyBudgetCard from './MonthlyBudgetCard'
import ReceiptsVaultPage from './ReceiptsVaultPage'
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
type Business = { id: string; name: string; category?: string; description?: string; fileUrl?: string; fileName?: string }
export type Cashbook = { id: string; cashBookName?: string; name?: string; businessId: string }
export type Transaction = {
  id: string
  amount: number
  transactionType: string
  remarks?: string
  transactionDate?: string
  fileName?: string
  filePath?: string
}
export type TransactionCategory = { label: string; Icon: LucideIcon }

export function getTransactionCategory(transaction: Transaction): TransactionCategory {
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
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [logoPreview, setLogoPreview] = useState<string | null>(business.fileUrl ?? null)

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
    if (logoFile) {
      form.append('File', logoFile)
    }
    try {
      const updated = await api<Business>('/api/Business', token, { method: 'PATCH', body: form })
      onUpdated({
        ...business,
        ...updated,
        id: updated.id ?? business.id,
        name: updated.name ?? String(fields.get('name')).trim(),
        category: updated.category ?? String(fields.get('category')),
        description: updated.description ?? String(fields.get('description') ?? '').trim(),
        fileUrl: updated.fileUrl ?? logoPreview ?? business.fileUrl,
        fileName: updated.fileName ?? business.fileName
      })
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
            <div className="workspace-logo-editor" style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 14 }}>
              <div
                style={{
                  width: 54,
                  height: 54,
                  borderRadius: 12,
                  background: '#eef5f0',
                  border: '1px solid #d9e6dc',
                  display: 'grid',
                  placeItems: 'center',
                  overflow: 'hidden'
                }}
              >
                {logoPreview ? (
                  <img src={logoPreview} alt={business.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                ) : (
                  <span style={{ fontSize: 20, fontWeight: 800, color: '#1b6343' }}>
                    {(business.name || 'W').slice(0, 1).toUpperCase()}
                  </span>
                )}
              </div>
              <div>
                <label style={{ cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: 6, fontSize: 11, fontWeight: 700, color: '#16754d', background: '#edf7ef', padding: '6px 10px', borderRadius: 7 }}>
                  <Upload size={13} strokeWidth={2} /> Change workspace logo
                  <input
                    type="file"
                    accept="image/*"
                    style={{ display: 'none' }}
                    onChange={e => {
                      const f = e.target.files?.[0]
                      if (f) {
                        setLogoFile(f)
                        setLogoPreview(URL.createObjectURL(f))
                      }
                    }}
                  />
                </label>
                {logoFile && <small style={{ display: 'block', fontSize: 10, color: '#687e72', marginTop: 3 }}>Selected: {logoFile.name}</small>}
              </div>
            </div>

            <label>Business name<input name="name" maxLength={150} required defaultValue={business.name} /></label>
            <label>
              Category
              <select name="category" defaultValue={business.category ?? ''}>
                <option value="">Choose a category</option>
                <option>Retail</option>
                <option>Food & beverage</option>
                <option>Services</option>
                <option>Freelance</option>
                <option>Healthcare</option>
                <option>Technology</option>
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

const QUICK_AMOUNT_PRESETS = [500, 1000, 2000, 5000, 10000]
const QUICK_REMARK_SUGGESTIONS = [
  'Food & Dining',
  'Fuel & Transport',
  'Groceries',
  'Office Supplies',
  'Utilities Bill',
  'Rent & Maintenance',
  'Salary / Payment'
]

function ManualTransactionDialog({ token, book, onClose, onDone }: { token: string; book: Cashbook; onClose: () => void; onDone: () => Promise<void> }) {
  const [busy, setBusy] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState('')
  const [amount, setAmount] = useState('')
  const [remarks, setRemarks] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [filePreview, setFilePreview] = useState<string | null>(null)
  const today = new Date()
  const localToday = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`

  useEffect(() => {
    if (file && file.type.startsWith('image/')) {
      const url = URL.createObjectURL(file)
      setFilePreview(url)
      return () => URL.revokeObjectURL(url)
    }
    setFilePreview(null)
  }, [file])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    const data = new FormData(event.currentTarget)
    const form = new FormData()
    form.append('CashBookId', book.id)
    form.append('Amount', String(data.get('amount') || amount))
    form.append('TransactionType', String(data.get('type')))
    const date = String(data.get('date') ?? '')
    if (date) form.append('TransactionDate', new Date(`${date}T12:00:00`).toISOString())
    form.append('Remarks', String(data.get('remarks') || remarks || ''))
    if (file && file.size > 0) form.append('File', file)
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

              <div>
                <label>
                  Amount (Rs.)
                  <input
                    name="amount"
                    type="number"
                    inputMode="decimal"
                    min="0.01"
                    step="0.01"
                    required
                    placeholder="0.00"
                    autoFocus
                    value={amount}
                    onChange={e => setAmount(e.target.value)}
                  />
                </label>
                <div className="quick-amount-chips">
                  {QUICK_AMOUNT_PRESETS.map(val => (
                    <button
                      key={val}
                      type="button"
                      className="quick-amount-chip"
                      onClick={() => {
                        const cur = Number(amount) || 0
                        setAmount(String(cur + val))
                      }}
                    >
                      +{val.toLocaleString('en-PK')}
                    </button>
                  ))}
                  {amount && (
                    <button
                      type="button"
                      className="quick-amount-chip clear"
                      onClick={() => setAmount('')}
                    >
                      Clear
                    </button>
                  )}
                </div>
              </div>

              <label>Date<input name="date" type="date" defaultValue={localToday} /></label>

              <div>
                <label>
                  Notes <span className="muted">(optional)</span>
                  <textarea
                    name="remarks"
                    maxLength={500}
                    rows={2}
                    placeholder="What was this transaction for?"
                    value={remarks}
                    onChange={e => setRemarks(e.target.value)}
                  />
                </label>
                <div className="quick-category-chips">
                  {QUICK_REMARK_SUGGESTIONS.map(s => (
                    <button
                      key={s}
                      type="button"
                      className={`quick-category-chip ${remarks === s ? 'selected' : ''}`}
                      onClick={() => setRemarks(s)}
                    >
                      {s}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label>Receipt or Invoice attachment <span className="muted">(optional)</span></label>
                {file ? (
                  <div className="selected-attachment-preview">
                    {filePreview ? (
                      <img src={filePreview} alt="Receipt preview" className="preview-thumb" />
                    ) : (
                      <Paperclip size={18} style={{ color: '#276944' }} />
                    )}
                    <div className="file-info">
                      <strong>{file.name}</strong>
                      <small>{(file.size / 1024 / 1024).toFixed(2)} MB</small>
                    </div>
                    <button
                      type="button"
                      className="remove-file-btn"
                      title="Remove attachment"
                      onClick={() => setFile(null)}
                    >
                      ✕
                    </button>
                  </div>
                ) : (
                  <input
                    name="file"
                    type="file"
                    accept="image/*,application/pdf"
                    onChange={e => setFile(e.target.files?.[0] ?? null)}
                  />
                )}
              </div>
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

const QUICK_RECEIPT_CATEGORIES = [
  'Food & Dining',
  'Groceries',
  'Fuel & Transport',
  'Office Supplies',
  'Utilities',
  'Software & Services',
  'General Expense'
]

type ReceiptScanStep = 'idle' | 'uploading' | 'analyzing' | 'suggesting' | 'review'

function ReceiptDialog({
  token,
  book,
  onClose,
  onDone,
  onToast
}: {
  token: string
  book: Cashbook
  onClose: () => void
  onDone: () => void
  onToast?: (msg: string) => void
}) {
  const [file, setFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [draft, setDraft] = useState<ReceiptDraft | null>(null)
  const [step, setStep] = useState<ReceiptScanStep>('idle')
  const [stepMessage, setStepMessage] = useState('')
  const [error, setError] = useState('')
  const [isDragging, setIsDragging] = useState(false)
  const [selectedCategory, setSelectedCategory] = useState('')
  const [copiedText, setCopiedText] = useState(false)

  useEffect(() => {
    if (file && file.type.startsWith('image/')) {
      const url = URL.createObjectURL(file)
      setPreviewUrl(url)
      return () => URL.revokeObjectURL(url)
    }
    setPreviewUrl(null)
  }, [file])

  useEffect(() => {
    if (draft?.suggestedCategory) {
      setSelectedCategory(draft.suggestedCategory)
    }
  }, [draft?.suggestedCategory])

  async function runScanPipeline(targetFile: File) {
    setError('')
    setFile(targetFile)

    // 1. Upload draft
    setStep('uploading')
    setStepMessage('Uploading receipt image...')
    let uploadedDraft: ReceiptDraft
    try {
      const form = new FormData()
      form.append('CashBookId', book.id)
      form.append('File', targetFile)
      uploadedDraft = await api<ReceiptDraft>('/api/Transaction/receipts/drafts', token, {
        method: 'POST',
        body: form
      })
      setDraft(uploadedDraft)
    } catch (e) {
      setStep('idle')
      setError(e instanceof Error ? e.message : 'Upload failed.')
      return
    }

    // 2. OCR text extraction
    setStep('analyzing')
    setStepMessage('Extracting text with OCR engine...')
    try {
      await api(`/api/Transaction/receipts/drafts/${uploadedDraft.draftId}/analyze`, token, { method: 'POST' })
    } catch (e) {
      setStep('idle')
      setError(
        e instanceof Error
          ? `${e.message} (Tip: Ensure receipt is well-lit, upright, and legible.)`
          : 'Receipt OCR analysis failed.'
      )
      return
    }

    // 3. Suggestion parsing
    setStep('suggesting')
    setStepMessage('Detecting merchant, amount & date...')
    try {
      await api(`/api/Transaction/receipts/drafts/${uploadedDraft.draftId}/suggest`, token, { method: 'POST' })
      const latest = await api<ReceiptDraft>(`/api/Transaction/receipts/drafts/${uploadedDraft.draftId}`, token)
      setDraft(latest)
      setStep('review')
    } catch (e) {
      try {
        const latest = await api<ReceiptDraft>(`/api/Transaction/receipts/drafts/${uploadedDraft.draftId}`, token)
        setDraft(latest)
        setStep('review')
        setError('Auto-suggestion was partial. Please review and complete remaining fields.')
      } catch {
        setStep('idle')
        setError(e instanceof Error ? e.message : 'Could not generate receipt suggestions.')
      }
    }
  }

  function handleFileSelect(e: React.ChangeEvent<HTMLInputElement>) {
    const f = e.target.files?.[0]
    if (f) {
      runScanPipeline(f)
    }
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setIsDragging(false)
    const f = e.dataTransfer.files?.[0]
    if (f) {
      runScanPipeline(f)
    }
  }

  async function confirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!draft) return
    const data = new FormData(event.currentTarget)
    setStep('suggesting')
    setStepMessage('Saving transaction...')
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
          category: selectedCategory || data.get('category')
        })
      })
      onToast?.('Transaction created from receipt!')
      onDone()
    } catch (e) {
      setStep('review')
      setError(e instanceof Error ? e.message : 'Could not create transaction.')
    }
  }

  function copyOcrText() {
    if (draft?.recognizedText) {
      navigator.clipboard.writeText(draft.recognizedText)
      setCopiedText(true)
      setTimeout(() => setCopiedText(false), 2000)
    }
  }

  const confidence = Math.round((draft?.parsingConfidence ?? 0) * 100)
  const isBusy = step === 'uploading' || step === 'analyzing' || (step === 'suggesting' && !draft?.status)

  const progressPercent =
    step === 'uploading' ? 33 : step === 'analyzing' ? 66 : step === 'suggesting' ? 92 : step === 'review' ? 100 : 0

  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && onClose()}>
      <section className="dialog receipt-dialog">
        <header>
          <div>
            <div className="eyebrow">SMART RECEIPT OCR</div>
            <h2>Add from a receipt</h2>
          </div>
          <button className="close" onClick={onClose}><X size={16} strokeWidth={1.8} /></button>
        </header>

        <div className="dialog-content">
          {step === 'idle' && !draft && (
            <>
              <p className="muted">
                Drop your receipt image or document below. We’ll automatically extract the merchant, date, amount, and category for review.
              </p>
              <label
                className={`dropzone ${isDragging ? 'receipt-dropzone-drag' : ''}`}
                onDragOver={e => {
                  e.preventDefault()
                  setIsDragging(true)
                }}
                onDragLeave={() => setIsDragging(false)}
                onDrop={handleDrop}
              >
                {previewUrl ? (
                  <div style={{ maxHeight: 120, overflow: 'hidden', borderRadius: 8, margin: '4px auto 8px' }}>
                    <img src={previewUrl} alt="Selected receipt" style={{ maxHeight: 120, maxWidth: '100%', objectFit: 'contain' }} />
                  </div>
                ) : (
                  <span className="upload-symbol"><Upload size={20} strokeWidth={1.8} /></span>
                )}
                <strong>{file?.name ?? 'Choose or drag & drop a receipt'}</strong>
                <small>{file ? `${(file.size / 1024 / 1024).toFixed(2)} MB` : 'JPG, PNG, or PDF · up to 10 MB'}</small>
                <input
                  type="file"
                  accept="image/jpeg,image/png,application/pdf,.jpg,.jpeg,.png,.pdf"
                  onChange={handleFileSelect}
                />
              </label>

              {file && (
                <button
                  className="button primary full"
                  disabled={isBusy}
                  onClick={() => runScanPipeline(file)}
                >
                  <Sparkles size={16} strokeWidth={1.8} /> Scan & Extract with OCR <span>→</span>
                </button>
              )}
            </>
          )}

          {isBusy && (
            <div className="receipt-pipeline-steps">
              <div style={{ textAlign: 'center', marginBottom: 8 }}>
                <div className="processing-icon">
                  <RefreshCw size={24} className="spin" style={{ animation: 'spin 1.2s linear infinite' }} />
                </div>
                <h3 style={{ margin: '14px 0 6px', fontSize: 17, color: '#1f4837' }}>{stepMessage}</h3>
                <p className="muted" style={{ margin: 0, fontSize: 12 }}>
                  Analyzing receipt geometry and recognizing printed values…
                </p>
              </div>

              <div className="receipt-progress-bar-bg">
                <div className="receipt-progress-bar-fill" style={{ width: `${progressPercent}%` }} />
              </div>

              <div className="receipt-step-indicators">
                {(() => {
                  const stepIndex = step === 'uploading' ? 1 : step === 'analyzing' ? 2 : step === 'suggesting' ? 3 : 0
                  return (
                    <>
                      <span className={`receipt-step-item ${stepIndex === 1 ? 'active' : stepIndex > 1 ? 'completed' : ''}`}>
                        {stepIndex > 1 ? <CheckCircle2 size={13} /> : '1.'} Upload
                      </span>
                      <span className={`receipt-step-item ${stepIndex === 2 ? 'active' : stepIndex > 2 ? 'completed' : ''}`}>
                        {stepIndex > 2 ? <CheckCircle2 size={13} /> : '2.'} OCR Scan
                      </span>
                      <span className={`receipt-step-item ${stepIndex === 3 ? 'active' : stepIndex > 3 ? 'completed' : ''}`}>
                        {stepIndex > 3 ? <CheckCircle2 size={13} /> : '3.'} Suggest details
                      </span>
                    </>
                  )
                })()}
              </div>
            </div>
          )}

          {step === 'review' && draft && (
            <form className="review-form" onSubmit={confirm}>
              <div className="review-alert">
                <Sparkles size={18} strokeWidth={2} style={{ color: '#277e4e' }} />
                <div style={{ flex: 1 }}>
                  <strong>Draft extracted & ready for review</strong>
                  <small>Verify the detected amounts and fields against the receipt below.</small>
                </div>
                <span className={`confidence-pill ${confidence >= 70 ? 'high' : confidence >= 40 ? 'medium' : 'low'}`}>
                  {confidence}% confidence
                </span>
              </div>

              <div className="receipt-review-split">
                {/* Left: Receipt Preview */}
                <div className="receipt-preview-card">
                  <div className="receipt-preview-img-container">
                    {previewUrl ? (
                      <img src={previewUrl} alt="Receipt" className="receipt-preview-img" />
                    ) : (
                      <div style={{ padding: 24, color: '#7a8e82' }}>
                        <ImageIcon size={32} strokeWidth={1.5} />
                        <div style={{ fontSize: 11, marginTop: 6 }}>Receipt document attached</div>
                      </div>
                    )}
                  </div>
                  <div className="receipt-preview-meta">
                    <strong>{file?.name ?? 'Receipt File'}</strong>
                    {file?.size && <span>{(file.size / 1024 / 1024).toFixed(2)} MB</span>}
                  </div>
                  <button
                    type="button"
                    className="button secondary-action"
                    style={{ minHeight: 32, fontSize: 11, width: '100%' }}
                    onClick={() => {
                      setDraft(null)
                      setStep('idle')
                      setFile(null)
                    }}
                  >
                    Scan another receipt
                  </button>
                </div>

                {/* Right: Extracted Fields */}
                <div>
                  <div className="fields">
                    <label>
                      Merchant
                      <input
                        name="merchant"
                        defaultValue={draft.suggestedMerchant ?? ''}
                        placeholder="e.g. Metro Mart, Shell, Office Depot"
                      />
                    </label>
                    <label>
                      Amount (Rs.)
                      <input
                        name="amount"
                        type="number"
                        min="0.01"
                        step="0.01"
                        required
                        defaultValue={draft.suggestedAmount ?? ''}
                        placeholder="0.00"
                      />
                    </label>
                    <label>
                      Date
                      <input
                        name="date"
                        type="date"
                        required
                        defaultValue={draft.suggestedTransactionDate?.slice(0, 10) ?? new Date().toISOString().slice(0, 10)}
                      />
                    </label>
                    <label>
                      Type
                      <select name="type" defaultValue={draft.suggestedTransactionType ?? 'CashOut'}>
                        <option value="CashOut">Cash out (Expense)</option>
                        <option value="CashIn">Cash in (Income / Refund)</option>
                      </select>
                    </label>
                  </div>

                  <div style={{ marginTop: 12 }}>
                    <label style={{ display: 'grid', gap: 6, color: '#53675b', fontSize: 9, fontWeight: 700 }}>
                      Category
                      <input
                        name="category"
                        value={selectedCategory}
                        onChange={e => setSelectedCategory(e.target.value)}
                        placeholder="e.g. Food & Dining, Office Supplies, Fuel"
                        style={{ height: 38, fontSize: 10, padding: '0 10px' }}
                      />
                    </label>
                    <div className="quick-category-chips">
                      {QUICK_RECEIPT_CATEGORIES.map(cat => (
                        <button
                          key={cat}
                          type="button"
                          className={`quick-category-chip ${selectedCategory === cat ? 'selected' : ''}`}
                          onClick={() => setSelectedCategory(cat)}
                        >
                          {cat}
                        </button>
                      ))}
                    </div>
                  </div>

                  <div style={{ marginTop: 12 }}>
                    <label style={{ display: 'grid', gap: 6, color: '#53675b', fontSize: 9, fontWeight: 700 }}>
                      Notes / Remarks
                      <input
                        name="notes"
                        defaultValue={draft.suggestedMerchant ? `Receipt from ${draft.suggestedMerchant}` : ''}
                        placeholder="Optional remarks or memo"
                        style={{ height: 38, fontSize: 10, padding: '0 10px' }}
                      />
                    </label>
                  </div>

                  {draft.recognizedText && (
                    <details style={{ marginTop: 14 }}>
                      <summary style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                        <span>Recognized receipt text</span>
                        <button
                          type="button"
                          onClick={e => {
                            e.preventDefault()
                            copyOcrText()
                          }}
                          style={{
                            border: 0,
                            background: 'transparent',
                            color: copiedText ? '#28734b' : '#627a6c',
                            fontSize: 10,
                            fontWeight: 700,
                            cursor: 'pointer'
                          }}
                        >
                          {copiedText ? '✓ Copied' : 'Copy raw text'}
                        </button>
                      </summary>
                      <pre>{draft.recognizedText}</pre>
                    </details>
                  )}

                  <div style={{ display: 'flex', gap: 10, marginTop: 18 }}>
                    <button
                      type="button"
                      className="button secondary-action"
                      style={{ flex: 1 }}
                      onClick={onClose}
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      className="button primary"
                      style={{ flex: 2 }}
                      disabled={isBusy}
                    >
                      {isBusy ? 'Saving transaction…' : 'Confirm & Add Transaction'} <span>→</span>
                    </button>
                  </div>
                </div>
              </div>
            </form>
          )}

          {error && (
            <div className="error-banner" style={{ marginTop: 14 }}>
              <span>{error}</span>
              <button
                type="button"
                onClick={() => {
                  setError('')
                  if (step !== 'review') {
                    setStep('idle')
                    setDraft(null)
                  }
                }}
              >
                Dismiss
              </button>
            </div>
          )}
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
  onAddReceipt,
  onRefresh,
  onToast,
  onViewAttachment,
  onOpenDetail,
  initialSearch
}: {
  transactions: Transaction[]
  token: string
  cashbookName: string
  books?: Cashbook[]
  selectedBookId?: string
  onSelectBook?: (book: Cashbook) => void
  onBack: () => void
  onAdd: () => void
  onAddReceipt?: () => void
  onRefresh: () => Promise<void>
  onToast: (message: string) => void
  onViewAttachment: (transaction: Transaction) => void
  onOpenDetail?: (transaction: Transaction) => void
  initialSearch?: string
}) {
  const [query, setQuery] = useState(initialSearch ?? '')

  useEffect(() => {
    if (initialSearch !== undefined) {
      setQuery(initialSearch)
    }
  }, [initialSearch])
  const [type, setType] = useState<'all' | 'income' | 'expense'>('all')
  const [categoryFilter, setCategoryFilter] = useState('all')
  const [sortBy, setSortBy] = useState<'date-desc' | 'date-asc' | 'amount-desc' | 'amount-asc'>('date-desc')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [datePickerOpen, setDatePickerOpen] = useState(false)
  const [draftFromDate, setDraftFromDate] = useState('')
  const [draftToDate, setDraftToDate] = useState('')
  const [calendarMonth, setCalendarMonth] = useState(() => new Date())
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [bulkDeleting, setBulkDeleting] = useState(false)
  const [showBulkDeleteConfirm, setShowBulkDeleteConfirm] = useState(false)

  // Clear selected IDs when cashbook or active filters change
  useEffect(() => {
    setSelectedIds(new Set())
  }, [cashbookName, type, categoryFilter, fromDate, toDate])

  // Reset page when filters or sorting change
  useEffect(() => {
    setPage(1)
  }, [query, type, categoryFilter, sortBy, fromDate, toDate, pageSize])

  const filteredTransactions = useMemo(() => transactions.filter(transaction => {
    const normalizedType = transaction.transactionType?.toLowerCase()
    const date = transaction.transactionDate?.slice(0, 10) ?? ''
    const matchesType = type === 'all' || (type === 'income' ? normalizedType === 'cashin' : normalizedType === 'cashout')
    const matchesQuery = (transaction.remarks ?? '').concat(' ', transaction.transactionType ?? '').toLowerCase().includes(query.trim().toLowerCase())
    const cat = getTransactionCategory(transaction)
    const matchesCategory = categoryFilter === 'all' || cat.label === categoryFilter
    return matchesType && matchesQuery && matchesCategory && (!fromDate || date >= fromDate) && (!toDate || date <= toDate)
  }).sort((a, b) => {
    if (sortBy === 'date-desc') return (b.transactionDate ?? '').localeCompare(a.transactionDate ?? '')
    if (sortBy === 'date-asc') return (a.transactionDate ?? '').localeCompare(b.transactionDate ?? '')
    if (sortBy === 'amount-desc') return Number(b.amount || 0) - Number(a.amount || 0)
    if (sortBy === 'amount-asc') return Number(a.amount || 0) - Number(b.amount || 0)
    return 0
  }), [transactions, query, type, categoryFilter, fromDate, toDate, sortBy])

  const totalPages = Math.max(1, Math.ceil(filteredTransactions.length / pageSize))
  const paginatedTransactions = useMemo(() => {
    const start = (page - 1) * pageSize
    return filteredTransactions.slice(start, start + pageSize)
  }, [filteredTransactions, page, pageSize])

  const totalIncome = filteredTransactions.filter(t => t.transactionType?.toLowerCase() === 'cashin').reduce((sum, t) => sum + Number(t.amount || 0), 0)
  const totalExpense = filteredTransactions.filter(t => t.transactionType?.toLowerCase() === 'cashout').reduce((sum, t) => sum + Number(t.amount || 0), 0)
  const netBalance = totalIncome - totalExpense

  const pageIds = useMemo(() => paginatedTransactions.map(t => t.id).filter(Boolean), [paginatedTransactions])
  const allPageSelected = pageIds.length > 0 && pageIds.every(id => selectedIds.has(id))

  function toggleSelect(id: string) {
    setSelectedIds(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  function toggleSelectPage() {
    if (allPageSelected) {
      setSelectedIds(prev => {
        const next = new Set(prev)
        pageIds.forEach(id => next.delete(id))
        return next
      })
    } else {
      setSelectedIds(prev => {
        const next = new Set(prev)
        pageIds.forEach(id => next.add(id))
        return next
      })
    }
  }

  const selectedTransactions = useMemo(() => {
    return transactions.filter(t => selectedIds.has(t.id))
  }, [transactions, selectedIds])

  const selectedIncome = selectedTransactions
    .filter(t => t.transactionType?.toLowerCase() === 'cashin')
    .reduce((s, t) => s + Number(t.amount || 0), 0)

  const selectedExpense = selectedTransactions
    .filter(t => t.transactionType?.toLowerCase() === 'cashout')
    .reduce((s, t) => s + Number(t.amount || 0), 0)

  const selectedNet = selectedIncome - selectedExpense

  function exportSelectedCsv() {
    const escapeCell = (value: unknown) => `"${String(value ?? '').replace(/"/g, '""')}"`
    const rows = [
      ['Date', 'Description', 'Category', 'Type', 'Amount', 'Currency', 'Cashbook', 'Transaction ID'],
      ...selectedTransactions.map(transaction => [
        transaction.transactionDate?.slice(0, 10) ?? '',
        transaction.remarks ?? '',
        getTransactionCategory(transaction).label,
        transaction.transactionType?.toLowerCase() === 'cashin' ? 'Income' : 'Expense',
        transaction.amount ?? 0,
        'PKR',
        cashbookName,
        transaction.id
      ])
    ]
    const csvContent = '\uFEFF' + rows.map(r => r.map(escapeCell).join(',')).join('\r\n')
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `spendwise-selected-${selectedTransactions.length}-transactions.csv`
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
    onToast(`Exported ${selectedTransactions.length} selected transactions`)
  }

  async function handleBulkDelete() {
    try {
      setBulkDeleting(true)
      const ids = Array.from(selectedIds)
      const results = await Promise.allSettled(
        ids.map(id => api(`/api/Transaction/${encodeURIComponent(id)}`, token, { method: 'DELETE' }))
      )
      const successCount = results.filter(r => r.status === 'fulfilled').length
      setSelectedIds(new Set())
      setShowBulkDeleteConfirm(false)
      await onRefresh()
      onToast(`Deleted ${successCount} transactions successfully`)
    } catch {
      onToast('Could not complete all deletions.')
    } finally {
      setBulkDeleting(false)
    }
  }

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

  const groups = useMemo(() => {
    return paginatedTransactions.reduce<{ label: string; transactions: Transaction[] }[]>((result, transaction) => {
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
  }, [paginatedTransactions])

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
        <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
          {onAddReceipt && (
            <button className="button secondary-action" onClick={onAddReceipt}>
              <Sparkles size={15} strokeWidth={1.8} /> Scan receipt
            </button>
          )}
          <button className="button primary" onClick={onAdd}>
            <Plus size={15} strokeWidth={1.8} /> Add transaction
          </button>
        </div>
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
        <select
          aria-label="Sort transactions"
          value={sortBy}
          onChange={e => setSortBy(e.target.value as 'date-desc' | 'date-asc' | 'amount-desc' | 'amount-asc')}
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
          <option value="date-desc">Newest first</option>
          <option value="date-asc">Oldest first</option>
          <option value="amount-desc">Amount: High to Low</option>
          <option value="amount-asc">Amount: Low to High</option>
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
        <button className="history-clear" onClick={() => { setQuery(''); setType('all'); setCategoryFilter('all'); setSortBy('date-desc'); setFromDate(''); setToDate(''); setDatePickerOpen(false) }}>Clear filters</button>
      </div>

      <article className="panel history-panel">
        <div className="history-table-head with-checkbox">
          <label className="checkbox-wrap" onClick={e => e.stopPropagation()} title="Select all on current page">
            <input
              type="checkbox"
              checked={allPageSelected}
              onChange={toggleSelectPage}
              aria-label="Select all transactions on page"
            />
          </label>
          <span>TRANSACTION</span>
          <span
            className="sortable-head"
            title="Click to sort by date"
            onClick={() => setSortBy(s => s === 'date-desc' ? 'date-asc' : 'date-desc')}
          >
            DATE {sortBy === 'date-desc' ? '↓' : sortBy === 'date-asc' ? '↑' : ''}
          </span>
          <span>TYPE</span>
          <span
            className="sortable-head"
            title="Click to sort by amount"
            onClick={() => setSortBy(s => s === 'amount-desc' ? 'amount-asc' : 'amount-desc')}
          >
            AMOUNT {sortBy === 'amount-desc' ? '↓' : sortBy === 'amount-asc' ? '↑' : ''}
          </span>
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
                <div
                  className={`history-row with-checkbox clickable-row ${selectedIds.has(transaction.id) ? 'selected-row' : ''}`}
                  key={transaction.id}
                  onClick={() => onOpenDetail?.(transaction)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={e => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault()
                      onOpenDetail?.(transaction)
                    }
                  }}
                >
                  <label className="checkbox-wrap" onClick={e => e.stopPropagation()} title="Select transaction">
                    <input
                      type="checkbox"
                      checked={selectedIds.has(transaction.id)}
                      onChange={() => toggleSelect(transaction.id)}
                      aria-label="Select transaction"
                    />
                  </label>
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
                          onClick={e => {
                            e.stopPropagation()
                            onViewAttachment(transaction)
                          }}
                        >
                          <Paperclip size={11} strokeWidth={2} /> Receipt
                        </button>
                      )}
                    </span>
                  </div>
                  <span>{date && !Number.isNaN(date.getTime()) ? date.toLocaleDateString() : '—'}</span>
                  <span><em className={'pill ' + (isIncome ? 'income-pill' : 'expense-pill')}>{isIncome ? 'Income' : 'Expense'}</em></span>
                  <strong className={isIncome ? 'income-amount' : ''}>{isIncome ? '+' : '−'} Rs. {Number(transaction.amount || 0).toLocaleString('en-PK')}</strong>
                  <div onClick={e => e.stopPropagation()}>
                    <TransactionRowActions token={token} transaction={transaction} onRefresh={onRefresh} onToast={onToast} />
                  </div>
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

        {filteredTransactions.length > 0 && (
          <div className="table-pagination">
            <div className="pagination-info">
              Showing <strong>{(page - 1) * pageSize + 1}</strong>–<strong>{Math.min(page * pageSize, filteredTransactions.length)}</strong> of <strong>{filteredTransactions.length}</strong>
            </div>
            <div className="pagination-controls">
              <label className="page-size-label">
                <span>Per page:</span>
                <select
                  value={pageSize}
                  onChange={e => {
                    setPageSize(Number(e.target.value))
                    setPage(1)
                  }}
                >
                  <option value={10}>10</option>
                  <option value={25}>25</option>
                  <option value={50}>50</option>
                  <option value={100}>100</option>
                </select>
              </label>
              <div className="page-nav">
                <button
                  type="button"
                  className="page-btn"
                  disabled={page <= 1}
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  title="Previous page"
                >
                  <ChevronLeft size={14} /> Prev
                </button>
                <span className="page-current">
                  {page} / {totalPages}
                </span>
                <button
                  type="button"
                  className="page-btn"
                  disabled={page >= totalPages}
                  onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                  title="Next page"
                >
                  Next <ChevronRight size={14} />
                </button>
              </div>
            </div>
          </div>
        )}
      </article>

      {selectedIds.size > 0 && (
        <div className="bulk-actions-floating-bar" role="toolbar" aria-label="Bulk actions">
          <div className="bulk-bar-info">
            <span className="bulk-badge">{selectedIds.size}</span>
            <strong>{selectedIds.size} {selectedIds.size === 1 ? 'selected' : 'selected'}</strong>
            <span className="bulk-totals">
              {selectedIncome > 0 && <span className="bulk-inc">+Rs. {selectedIncome.toLocaleString('en-PK')}</span>}
              {selectedExpense > 0 && <span className="bulk-exp">−Rs. {selectedExpense.toLocaleString('en-PK')}</span>}
            </span>
          </div>
          <div className="bulk-bar-actions">
            <button
              type="button"
              className="button secondary-action bulk-action-btn"
              onClick={exportSelectedCsv}
            >
              <Download size={13} strokeWidth={2} /> Export ({selectedIds.size})
            </button>
            <button
              type="button"
              className="button danger bulk-action-btn"
              onClick={() => setShowBulkDeleteConfirm(true)}
            >
              <Trash2 size={13} strokeWidth={2} /> Delete ({selectedIds.size})
            </button>
            <button
              type="button"
              className="bulk-deselect-btn"
              onClick={() => setSelectedIds(new Set())}
              title="Deselect all"
              aria-label="Deselect all"
            >
              <X size={15} />
            </button>
          </div>
        </div>
      )}

      {showBulkDeleteConfirm && (
        <div className="overlay" onClick={() => setShowBulkDeleteConfirm(false)}>
          <div className="dialog delete-dialog" onClick={e => e.stopPropagation()}>
            <header>
              <div>
                <div className="eyebrow" style={{ color: '#b64e45' }}>BULK REMOVAL</div>
                <h2>Delete {selectedIds.size} Transactions?</h2>
              </div>
              <button className="close" onClick={() => setShowBulkDeleteConfirm(false)}><X size={18} /></button>
            </header>
            <div className="dialog-content">
              <p className="muted" style={{ fontSize: 12, lineHeight: 1.6, margin: '0 0 14px' }}>
                Are you sure you want to permanently delete these <strong>{selectedIds.size}</strong> selected transactions from &ldquo;{cashbookName}&rdquo;?
                This action cannot be undone.
              </p>
              <div className="delete-confirm-box">
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11, marginBottom: 4 }}>
                  <span>Inflow: <strong style={{ color: '#16835c' }}>+Rs. {selectedIncome.toLocaleString('en-PK')}</strong></span>
                  <span>Outflow: <strong style={{ color: '#c05c43' }}>−Rs. {selectedExpense.toLocaleString('en-PK')}</strong></span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, fontWeight: 700, paddingTop: 6, borderTop: '1px solid #f0e1de' }}>
                  <span>Net impact:</span>
                  <span style={{ color: selectedNet >= 0 ? '#16835c' : '#c05c43' }}>
                    {selectedNet >= 0 ? '+' : '−'} Rs. {Math.abs(selectedNet).toLocaleString('en-PK')}
                  </span>
                </div>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 18 }}>
                <button
                  type="button"
                  className="button secondary-action"
                  onClick={() => setShowBulkDeleteConfirm(false)}
                  disabled={bulkDeleting}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  className="button danger"
                  onClick={handleBulkDelete}
                  disabled={bulkDeleting}
                >
                  {bulkDeleting ? 'Deleting…' : `Confirm Delete (${selectedIds.size})`}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
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
  const [showCashbooks, setShowCashbooks] = useState(false)
  const [showReceiptsVault, setShowReceiptsVault] = useState(false)
  const [overviewFilter, setOverviewFilter] = useState<'all' | 'income' | 'expense' | 'receipts'>('all')
  const [showCreateCashbook, setShowCreateCashbook] = useState(false)
  const [showCreateBusiness, setShowCreateBusiness] = useState(false)
  const [showNotifications, setShowNotifications] = useState(false)
  const [showProfile, setShowProfile] = useState(false)
  const [unreadInvitesCount, setUnreadInvitesCount] = useState(0)
  const [manageCashbook, setManageCashbook] = useState<{ book: Cashbook; mode: 'edit' | 'delete' } | null>(null)
  const [viewAttachment, setViewAttachment] = useState<Transaction | null>(null)
  const [viewTransactionDetail, setViewTransactionDetail] = useState<Transaction | null>(null)
  const [editingTransaction, setEditingTransaction] = useState<Transaction | null>(null)
  const [deletingTransaction, setDeletingTransaction] = useState<Transaction | null>(null)
  const [pendingUrlInviteToken, setPendingUrlInviteToken] = useState<string | null>(null)
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [search, setSearch] = useState('')
  const [overviewPeriod, setOverviewPeriod] = useState<'month' | 'year' | 'all'>('month')
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
        setMobileNavOpen(false)
        setShowReceipt(false)
        setShowManualTransaction(false)
        setShowCreateCashbook(false)
        setShowCreateBusiness(false)
        setShowNotifications(false)
        setShowProfile(false)
        setManageCashbook(null)
        setViewAttachment(null)
        setViewTransactionDetail(null)
        setEditingTransaction(null)
        setDeletingTransaction(null)
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
        name: item.name ?? (item as Business & { Name: string }).Name,
        category: item.category ?? (item as unknown as { Category?: string }).Category,
        description: item.description ?? (item as unknown as { Description?: string }).Description,
        fileUrl: item.fileUrl ?? (item as unknown as { FileUrl?: string }).FileUrl,
        fileName: item.fileName ?? (item as unknown as { FileName?: string }).FileName
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

  const overviewTransactions = useMemo(() => {
    const now = new Date()
    return transactions.filter(t => {
      if (overviewPeriod === 'all') return true
      if (!t.transactionDate) return false
      const d = new Date(t.transactionDate)
      if (Number.isNaN(d.getTime())) return false
      if (overviewPeriod === 'month') {
        return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth()
      }
      if (overviewPeriod === 'year') {
        return d.getFullYear() === now.getFullYear()
      }
      return true
    })
  }, [transactions, overviewPeriod])

  const visible = useMemo(
    () =>
      transactions
        .filter(t => `${t.remarks ?? ''} ${t.transactionType}`.toLowerCase().includes(search.toLowerCase()))
        .filter(t => {
          if (overviewFilter === 'income') return t.transactionType?.toLowerCase() === 'cashin'
          if (overviewFilter === 'expense') return t.transactionType?.toLowerCase() === 'cashout'
          if (overviewFilter === 'receipts') return Boolean(t.filePath || t.fileName)
          return true
        }),
    [transactions, search, overviewFilter]
  )
  const income = overviewTransactions.filter(t => t.transactionType?.toLowerCase() === 'cashin').reduce((s, t) => s + Number(t.amount || 0), 0)
  const expense = overviewTransactions.filter(t => t.transactionType?.toLowerCase() === 'cashout').reduce((s, t) => s + Number(t.amount || 0), 0)
  const balance = income - expense

  const heroChartData = useMemo(() => {
    const barsCount = 15
    if (!overviewTransactions.length) {
      const placeholderHeights = [30, 48, 38, 58, 42, 68, 52, 78, 56, 82, 62, 72, 52, 68, 48]
      return placeholderHeights.map((h, i) => ({
        key: `placeholder-${i}`,
        height: h,
        label: 'No transactions yet in this timeframe',
        incomeSum: 0,
        expenseSum: 0,
        net: 0,
        count: 0,
        isIncome: true,
        isEmpty: true
      }))
    }

    const sorted = [...overviewTransactions]
      .filter(t => t.transactionDate && !Number.isNaN(new Date(t.transactionDate).getTime()))
      .sort((a, b) => new Date(a.transactionDate!).getTime() - new Date(b.transactionDate!).getTime())

    if (!sorted.length) {
      return Array.from({ length: barsCount }).map((_, i) => ({
        key: `empty-${i}`,
        height: 25,
        label: 'No transaction dates',
        incomeSum: 0,
        expenseSum: 0,
        net: 0,
        count: 0,
        isIncome: true,
        isEmpty: true
      }))
    }

    const minTime = new Date(sorted[0].transactionDate!).getTime()
    const maxTime = Math.max(new Date(sorted[sorted.length - 1].transactionDate!).getTime(), minTime + 1000)
    const step = (maxTime - minTime) / barsCount || 1

    const buckets = Array.from({ length: barsCount }).map((_, i) => {
      const bucketStart = minTime + i * step
      const bucketEnd = bucketStart + step
      const bucketTxns = sorted.filter(t => {
        const time = new Date(t.transactionDate!).getTime()
        return i === barsCount - 1
          ? time >= bucketStart && time <= bucketEnd + 1000
          : time >= bucketStart && time < bucketEnd
      })

      const incomeSum = bucketTxns
        .filter(t => t.transactionType?.toLowerCase() === 'cashin')
        .reduce((s, t) => s + Number(t.amount || 0), 0)
      const expenseSum = bucketTxns
        .filter(t => t.transactionType?.toLowerCase() === 'cashout')
        .reduce((s, t) => s + Number(t.amount || 0), 0)
      const totalVolume = incomeSum + expenseSum
      const net = incomeSum - expenseSum

      const startDateStr = new Date(bucketStart).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
      const endDateStr = new Date(bucketEnd).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
      const label = startDateStr === endDateStr ? startDateStr : `${startDateStr} - ${endDateStr}`

      return {
        key: `bar-${i}`,
        label,
        totalVolume,
        incomeSum,
        expenseSum,
        net,
        count: bucketTxns.length,
        isIncome: net >= 0,
        isEmpty: bucketTxns.length === 0
      }
    })

    const maxVolume = Math.max(...buckets.map(b => b.totalVolume), 1)

    return buckets.map(b => {
      const height = b.totalVolume === 0 ? 12 : Math.max(16, Math.min(100, Math.round((b.totalVolume / maxVolume) * 100)))
      return {
        ...b,
        height
      }
    })
  }, [overviewTransactions])

  const receiptsCount = useMemo(() => transactions.filter(t => Boolean(t.filePath || t.fileName)).length, [transactions])

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
    setShowCashbooks(false)
    setShowReceiptsVault(false)
  }

  return (
    <div className="shell">
      {mobileNavOpen && (
        <div className="mobile-drawer-overlay" onClick={() => setMobileNavOpen(false)}>
          <div className="mobile-drawer" onClick={e => e.stopPropagation()}>
            <div className="mobile-drawer-header">
              <div className="brand">
                <span className="brand-symbol"><Wallet size={18} strokeWidth={1.9} /></span> spendwise<span>.</span>
              </div>
              <button
                className="mobile-drawer-close"
                onClick={() => setMobileNavOpen(false)}
                aria-label="Close menu"
              >
                <X size={19} strokeWidth={1.9} />
              </button>
            </div>

            <div className="mobile-drawer-body">
              <small className="side-label">WORKSPACE</small>
              <div className="workspace-wrapper">
                <label className="workspace">
                  <span className="workspace-icon">
                    {selectedBusiness?.fileUrl ? (
                      <img
                        src={selectedBusiness.fileUrl}
                        alt={selectedBusiness.name}
                        style={{ width: '100%', height: '100%', borderRadius: 6, objectFit: 'cover' }}
                      />
                    ) : (
                      (selectedBusiness?.name ?? 'S').slice(0, 1).toUpperCase()
                    )}
                  </span>
                  <select
                    value={businessId}
                    onChange={e => {
                      setBusinessId(e.target.value)
                      showOverview()
                      setMobileNavOpen(false)
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
                  onClick={() => {
                    setMobileNavOpen(false)
                    setShowCreateBusiness(true)
                  }}
                >
                  <Plus size={16} strokeWidth={2} />
                </button>
              </div>

              {book && (
                <div style={{ marginBottom: 16 }}>
                  <small className="side-label">ACTIVE CASHBOOK</small>
                  <div className="topbar-book-switcher" style={{ width: '100%', maxWidth: '100%' }}>
                    <BookOpen size={13} strokeWidth={2} />
                    <select
                      value={book.id}
                      onChange={e => {
                        const chosen = books.find(b => b.id === e.target.value)
                        if (chosen) {
                          setBook(chosen)
                          showToast(`Switched to "${chosen.cashBookName ?? chosen.name ?? 'Cashbook'}"`)
                        }
                      }}
                    >
                      {books.map(b => (
                        <option key={b.id} value={b.id}>
                          {b.cashBookName ?? b.name ?? 'Cashbook'}
                        </option>
                      ))}
                    </select>
                    <ChevronDown size={11} strokeWidth={2} className="topbar-book-arrow" />
                  </div>
                </div>
              )}

              <small className="side-label">MENU</small>
              <nav className="mobile-nav-list">
                <button
                  className={'nav ' + (!showAllTransactions && !showCashbooks && !showMembers && !showSettings && !showReports && !showReceiptsVault ? 'active' : '')}
                  onClick={() => {
                    showOverview()
                    setMobileNavOpen(false)
                  }}
                >
                  <span><LayoutDashboard size={17} strokeWidth={1.8} /></span> Overview
                </button>
                <button
                  className={'nav ' + (showAllTransactions ? 'active' : '')}
                  onClick={() => {
                    setShowAllTransactions(true)
                    setShowCashbooks(false)
                    setShowReceiptsVault(false)
                    setShowReports(false)
                    setShowMembers(false)
                    setShowSettings(false)
                    setMobileNavOpen(false)
                  }}
                >
                  <span><ReceiptText size={17} strokeWidth={1.8} /></span> Transactions
                  {transactions.length > 0 && <i>{transactions.length}</i>}
                </button>
                <button
                  className={'nav ' + (showCashbooks ? 'active' : '')}
                  onClick={() => {
                    setShowCashbooks(true)
                    setShowAllTransactions(false)
                    setShowReceiptsVault(false)
                    setShowReports(false)
                    setShowMembers(false)
                    setShowSettings(false)
                    setMobileNavOpen(false)
                  }}
                >
                  <span><BookOpen size={17} strokeWidth={1.8} /></span> Cashbooks
                  {books.length > 0 && <i>{books.length}</i>}
                </button>
                <button
                  className={'nav ' + (showReceiptsVault ? 'active' : '')}
                  onClick={() => {
                    setShowReceiptsVault(true)
                    setShowCashbooks(false)
                    setShowAllTransactions(false)
                    setShowReports(false)
                    setShowMembers(false)
                    setShowSettings(false)
                    setMobileNavOpen(false)
                  }}
                >
                  <span><Receipt size={17} strokeWidth={1.8} /></span> Receipts
                  {receiptsCount > 0 && <i>{receiptsCount}</i>}
                </button>
                <button
                  className={'nav ' + (showReports ? 'active' : '')}
                  onClick={() => {
                    setShowReports(true)
                    setShowCashbooks(false)
                    setShowReceiptsVault(false)
                    setShowAllTransactions(false)
                    setShowMembers(false)
                    setShowSettings(false)
                    setMobileNavOpen(false)
                  }}
                >
                  <span><ChartNoAxesCombined size={17} strokeWidth={1.8} /></span> Reports
                </button>
                <button
                  className={'nav ' + (showMembers ? 'active' : '')}
                  onClick={() => {
                    setShowMembers(true)
                    setShowCashbooks(false)
                    setShowReceiptsVault(false)
                    setShowReports(false)
                    setShowSettings(false)
                    setShowAllTransactions(false)
                    setMobileNavOpen(false)
                  }}
                >
                  <span><UsersRound size={17} strokeWidth={1.8} /></span> Members
                </button>
                <button
                  className={'nav ' + (showSettings ? 'active' : '')}
                  onClick={() => {
                    setShowSettings(true)
                    setShowCashbooks(false)
                    setShowReceiptsVault(false)
                    setShowReports(false)
                    setShowMembers(false)
                    setShowAllTransactions(false)
                    setMobileNavOpen(false)
                  }}
                >
                  <span><Settings size={17} strokeWidth={1.8} /></span> Settings
                </button>
              </nav>
            </div>

            <div className="mobile-drawer-footer">
              <div
                className="user-row"
                onClick={() => {
                  setMobileNavOpen(false)
                  setShowProfile(true)
                }}
                style={{ cursor: 'pointer' }}
                title="View profile & account details"
              >
                <span className="user-avatar">{fullName.slice(0, 1).toUpperCase()}</span>
                <span>
                  <strong>{fullName}</strong>
                  <small>{user.email ?? user.Email ?? 'My account'}</small>
                </span>
                <span
                  className="logout"
                  title="Sign out"
                  onClick={e => {
                    e.stopPropagation()
                    setMobileNavOpen(false)
                    onLogout()
                  }}
                >
                  <LogOut size={16} strokeWidth={1.8} />
                </span>
              </div>
            </div>
          </div>
        </div>
      )}
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-symbol"><Wallet size={18} strokeWidth={1.9} /></span> spendwise<span>.</span>
        </div>

        <small className="side-label">WORKSPACE</small>
        <div className="workspace-wrapper">
          <label className="workspace">
            <span className="workspace-icon">
              {selectedBusiness?.fileUrl ? (
                <img
                  src={selectedBusiness.fileUrl}
                  alt={selectedBusiness.name}
                  style={{ width: '100%', height: '100%', borderRadius: 6, objectFit: 'cover' }}
                />
              ) : (
                (selectedBusiness?.name ?? 'S').slice(0, 1).toUpperCase()
              )}
            </span>
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
            className={'nav ' + (!showAllTransactions && !showCashbooks && !showMembers && !showSettings && !showReports && !showReceiptsVault ? 'active' : '')}
            onClick={showOverview}
          >
            <span><LayoutDashboard size={17} strokeWidth={1.8} /></span> Overview
          </button>
          <button
            className={'nav ' + (showAllTransactions ? 'active' : '')}
            onClick={() => {
              setShowAllTransactions(true)
              setShowCashbooks(false)
              setShowReceiptsVault(false)
              setShowMembers(false)
              setShowSettings(false)
              setShowReports(false)
            }}
          >
            <span><ReceiptText size={17} strokeWidth={1.8} /></span> Transactions <i>{transactions.length}</i>
          </button>
          <button
            className={'nav ' + (showCashbooks ? 'active' : '')}
            onClick={() => {
              setShowCashbooks(true)
              setShowAllTransactions(false)
              setShowReceiptsVault(false)
              setShowMembers(false)
              setShowSettings(false)
              setShowReports(false)
            }}
          >
            <span><BookOpen size={17} strokeWidth={1.8} /></span> Cashbooks <i>{books.length}</i>
          </button>
          <button
            className={'nav ' + (showReceiptsVault ? 'active' : '')}
            onClick={() => {
              setShowReceiptsVault(true)
              setShowCashbooks(false)
              setShowAllTransactions(false)
              setShowMembers(false)
              setShowSettings(false)
              setShowReports(false)
            }}
          >
            <span><Receipt size={17} strokeWidth={1.8} /></span> Receipts <i>{receiptsCount}</i>
          </button>
          <button
            className={'nav ' + (showReports ? 'active' : '')}
            onClick={() => {
              setShowReports(true)
              setShowCashbooks(false)
              setShowReceiptsVault(false)
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
              setShowCashbooks(false)
              setShowReceiptsVault(false)
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
              setShowCashbooks(false)
              setShowReceiptsVault(false)
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
          <button
            className="mobile-menu-toggle"
            onClick={() => setMobileNavOpen(true)}
            aria-label="Open navigation menu"
          >
            <Menu size={19} strokeWidth={1.9} />
          </button>
          <div className="crumb">
            Workspace <span>/</span>{' '}
            <button className="crumb-button" onClick={showOverview}>
              {showSettings
                ? 'Settings'
                : showMembers
                ? 'Members'
                : showReports
                ? 'Reports'
                : showCashbooks
                ? 'Cashbooks'
                : showReceiptsVault
                ? 'Receipts Vault'
                : showAllTransactions
                ? 'Transactions'
                : 'Overview'}
            </button>
          </div>
          {book && (
            <div className="topbar-book-switcher" title="Active cashbook (click to switch)">
              <BookOpen size={13} strokeWidth={2} />
              <select
                value={book.id}
                onChange={e => {
                  const chosen = books.find(b => b.id === e.target.value)
                  if (chosen) {
                    setBook(chosen)
                    showToast(`Switched to "${chosen.cashBookName ?? chosen.name ?? 'Cashbook'}"`)
                  }
                }}
                aria-label="Active cashbook"
              >
                {books.map(b => (
                  <option key={b.id} value={b.id}>
                    {b.cashBookName ?? b.name ?? 'Cashbook'}
                  </option>
                ))}
              </select>
              <ChevronDown size={11} strokeWidth={2} className="topbar-book-arrow" />
            </div>
          )}
          <label className="search">
            <Search size={17} strokeWidth={1.8} />
            <input
              ref={searchInputRef}
              placeholder="Search transactions..."
              value={search}
              onChange={e => setSearch(e.target.value)}
              onKeyDown={e => {
                if (e.key === 'Enter') {
                  setShowAllTransactions(true)
                  setShowCashbooks(false)
                  setShowReceiptsVault(false)
                  setShowReports(false)
                  setShowMembers(false)
                  setShowSettings(false)
                }
              }}
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
              books={books}
              selectedBookId={book?.id}
              onSelectBook={b => setBook(b)}
              onBack={showOverview}
              onToast={showToast}
            />
          ) : showCashbooks ? (
            <CashbooksPage
              books={books}
              selectedBook={book}
              businessName={selectedBusiness?.name ?? 'Workspace'}
              activeBookTransactionsCount={transactions.length}
              onSelectBook={b => {
                setBook(b)
                showToast(`Switched to "${b.cashBookName ?? b.name ?? 'Cashbook'}"`)
              }}
              onOpenTransactions={b => {
                setBook(b)
                setShowCashbooks(false)
                setShowAllTransactions(true)
              }}
              onAddCashbook={() => setShowCreateCashbook(true)}
              onEditCashbook={b => setManageCashbook({ book: b, mode: 'edit' })}
              onDeleteCashbook={b => setManageCashbook({ book: b, mode: 'delete' })}
              onBack={showOverview}
              onToast={showToast}
            />
          ) : showReceiptsVault ? (
            <ReceiptsVaultPage
              transactions={transactions}
              token={token}
              cashbookName={book?.cashBookName ?? book?.name ?? 'Cashbook'}
              businessName={selectedBusiness?.name ?? 'Workspace'}
              books={books}
              selectedBookId={book?.id}
              onSelectBook={b => setBook(b)}
              onBack={showOverview}
              onUploadReceipt={() => setShowReceipt(true)}
              onViewAttachment={txn => setViewAttachment(txn)}
              onOpenDetail={txn => setViewTransactionDetail(txn)}
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
              onAdd={() => setShowManualTransaction(true)}
              onAddReceipt={() => setShowReceipt(true)}
              onRefresh={() => reloadTransactions()}
              onToast={showToast}
              onViewAttachment={txn => setViewAttachment(txn)}
              onOpenDetail={txn => setViewTransactionDetail(txn)}
              initialSearch={search}
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
                    <small>{overviewPeriod === 'month' ? 'THIS MONTH' : overviewPeriod === 'year' ? 'THIS YEAR' : 'ALL TIME'}</small>
                    <strong>Rs. {Math.abs(balance).toLocaleString('en-PK')}</strong>
                    <span><b>{balance >= 0 ? '↗' : '↘'}</b> {balance >= 0 ? 'Net surplus' : 'Net deficit'}</span>
                  </div>
                  <div className="chart-bars">
                    {heroChartData.map(bar => (
                      <i
                        key={bar.key}
                        style={{
                          height: `${bar.height}%`,
                          opacity: bar.isEmpty ? 0.35 : 1,
                          background: bar.isEmpty ? '#ffffff32' : bar.isIncome ? '#a5e492' : '#f7ab76'
                        }}
                        title={
                          bar.isEmpty
                            ? `${bar.label}`
                            : `${bar.label}\n${bar.count} transaction${bar.count === 1 ? '' : 's'}\nIncome: Rs. ${bar.incomeSum.toLocaleString('en-PK')}\nExpense: Rs. ${bar.expenseSum.toLocaleString('en-PK')}\nNet: Rs. ${bar.net.toLocaleString('en-PK')}`
                        }
                      />
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
                <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
                  <div className="type-chips" role="group" aria-label="Overview timeframe">
                    <button className={overviewPeriod === 'month' ? 'selected' : ''} onClick={() => setOverviewPeriod('month')}>
                      This month
                    </button>
                    <button className={overviewPeriod === 'year' ? 'selected' : ''} onClick={() => setOverviewPeriod('year')}>
                      This year
                    </button>
                    <button className={overviewPeriod === 'all' ? 'selected' : ''} onClick={() => setOverviewPeriod('all')}>
                      All time
                    </button>
                  </div>
                  <button className="period" onClick={() => setShowReports(true)}>
                    Full report →
                  </button>
                </div>
              </div>

              <section className="metrics">
                <article>
                  <div className="metric-symbol green"><Wallet size={17} strokeWidth={1.8} /></div>
                  <span className="trend">↗ 8.4%</span>
                  <small>TOTAL BALANCE</small>
                  <strong>Rs. {balance.toLocaleString('en-PK')}</strong>
                  <p>Across selected timeframe</p>
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
                  <span className="trend neutral">{overviewPeriod === 'month' ? 'THIS MONTH' : overviewPeriod === 'year' ? 'THIS YEAR' : 'ALL TIME'}</span>
                  <small>TRANSACTIONS</small>
                  <strong>{overviewTransactions.length}</strong>
                  <p>Activity in this timeframe</p>
                  <div className="sparkline purple-line" />
                </article>
              </section>

              {book && (
                <MonthlyBudgetCard
                  cashbookId={book.id}
                  cashbookName={book.cashBookName ?? book.name ?? 'Cashbook'}
                  transactions={transactions}
                  onToast={showToast}
                />
              )}

              <section className="bottom-grid">
                <article className="panel transactions" id="transactions">
                  <div className="panel-head">
                    <div>
                      <h2>Recent transactions</h2>
                      <p>{book?.cashBookName ?? book?.name ?? 'Select a cashbook to see activity'}</p>
                    </div>
                    <div className="panel-head-right">
                      <div className="overview-filter-tabs" role="tablist" aria-label="Transaction type filter">
                        <button
                          type="button"
                          className={`overview-filter-tab ${overviewFilter === 'all' ? 'active' : ''}`}
                          onClick={() => setOverviewFilter('all')}
                        >
                          All
                        </button>
                        <button
                          type="button"
                          className={`overview-filter-tab ${overviewFilter === 'income' ? 'active' : ''}`}
                          onClick={() => setOverviewFilter('income')}
                        >
                          Income
                        </button>
                        <button
                          type="button"
                          className={`overview-filter-tab ${overviewFilter === 'expense' ? 'active' : ''}`}
                          onClick={() => setOverviewFilter('expense')}
                        >
                          Expense
                        </button>
                        <button
                          type="button"
                          className={`overview-filter-tab ${overviewFilter === 'receipts' ? 'active' : ''}`}
                          onClick={() => setOverviewFilter('receipts')}
                        >
                          Receipts {receiptsCount > 0 ? `(${receiptsCount})` : ''}
                        </button>
                      </div>
                      <button className="text-button" onClick={() => setShowAllTransactions(true)}>
                        View all <ArrowRight size={14} strokeWidth={1.8} />
                      </button>
                    </div>
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
                        <div
                          className="table-row clickable-row"
                          key={t.id ?? `${t.remarks}-${i}`}
                          onClick={() => setViewTransactionDetail(t)}
                          role="button"
                          tabIndex={0}
                          onKeyDown={e => {
                            if (e.key === 'Enter' || e.key === ' ') {
                              e.preventDefault()
                              setViewTransactionDetail(t)
                            }
                          }}
                        >
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
                                  onClick={e => {
                                    e.stopPropagation()
                                    setViewAttachment(t)
                                  }}
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
                          <div onClick={e => e.stopPropagation()}>
                            <TransactionRowActions token={token} transaction={t} onRefresh={() => reloadTransactions()} onToast={showToast} />
                          </div>
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
                    <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                      <button
                        className="text-button"
                        style={{ fontSize: 11 }}
                        title="Open full cashbooks manager"
                        onClick={() => setShowCashbooks(true)}
                      >
                        Manage all →
                      </button>
                      <button className="add-small" aria-label="Create a cashbook" onClick={() => setShowCreateCashbook(true)}>
                        <Plus size={16} strokeWidth={1.8} />
                      </button>
                    </div>
                  </div>
                  <div className="book-list">
                    {books.map((b, i) => {
                      const isActive = book?.id === b.id
                      return (
                        <div className={`book-entry ${isActive ? 'is-active' : ''}`} key={b.id}>
                          <button
                            className={`book-row ${isActive ? 'chosen' : ''}`}
                            onClick={() => {
                              if (!isActive) {
                                setBook(b)
                                showToast(`Switched to "${b.cashBookName ?? b.name ?? 'Cashbook'}"`)
                              }
                            }}
                          >
                            <span className={`book-mark mark-${i % 3}`}><BookOpen size={16} strokeWidth={1.8} /></span>
                            <span>
                              <strong>{b.cashBookName ?? b.name ?? 'Cashbook'}</strong>
                              <small>
                                {isActive ? (
                                  <span className="active-book-indicator">
                                    <span className="dot" /> Active · {transactions.length} txns
                                  </span>
                                ) : (
                                  b.businessId ? 'Business cashbook' : 'Cashbook'
                                )}
                              </small>
                            </span>
                            <span className="row-arrow">
                              {isActive ? <Check size={14} strokeWidth={2.4} /> : '→'}
                            </span>
                          </button>
                          <button className="book-action" aria-label={`Rename ${b.cashBookName ?? b.name ?? 'cashbook'}`} title="Rename cashbook" onClick={() => setManageCashbook({ book: b, mode: 'edit' })}>
                            <Pencil size={14} strokeWidth={1.8} />
                          </button>
                          <button className="book-action delete-action" aria-label={`Delete ${b.cashBookName ?? b.name ?? 'cashbook'}`} title="Delete cashbook" onClick={() => setManageCashbook({ book: b, mode: 'delete' })}>
                            <Trash2 size={14} strokeWidth={1.8} />
                          </button>
                        </div>
                      )
                    })}
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
          onToast={showToast}
          onDone={() => {
            setShowReceipt(false)
            reloadTransactions(book.id)
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

      {viewTransactionDetail && (
        <TransactionDetailDialog
          transaction={viewTransactionDetail}
          cashbookName={book?.cashBookName ?? book?.name ?? 'Cashbook'}
          businessName={selectedBusiness?.name ?? 'Workspace'}
          onClose={() => setViewTransactionDetail(null)}
          onEdit={txn => {
            setViewTransactionDetail(null)
            setEditingTransaction(txn)
          }}
          onDelete={txn => {
            setViewTransactionDetail(null)
            setDeletingTransaction(txn)
          }}
          onViewAttachment={txn => setViewAttachment(txn)}
        />
      )}

      {editingTransaction && (
        <TransactionEditDialog
          token={token}
          transaction={editingTransaction}
          onClose={() => setEditingTransaction(null)}
          onSaved={async () => {
            await reloadTransactions()
            showToast('Transaction updated')
          }}
        />
      )}

      {deletingTransaction && (
        <TransactionDeleteDialog
          token={token}
          transaction={deletingTransaction}
          onClose={() => setDeletingTransaction(null)}
          onDeleted={async () => {
            await reloadTransactions()
            showToast('Transaction deleted')
          }}
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
