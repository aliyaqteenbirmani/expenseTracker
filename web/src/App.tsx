import { useEffect, useMemo, useState, type FormEvent } from 'react'
import './App.css'

const API = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

type Envelope<T> = { data?: T; Data?: T; message?: string; Message?: string }
type User = { firstName?: string; FirstName?: string; lastName?: string; LastName?: string; email?: string; Email?: string; accessToken?: string; AccessToken?: string; refreshToken?: string; RefreshToken?: string }
type Business = { id: string; name: string; category?: string }
type Cashbook = { id: string; cashBookName?: string; name?: string; businessId: string }
type Transaction = { id?: string; amount: number; transactionType: string; remarks?: string; transactionDate?: string }
type ReceiptDraft = { draftId: string; status: string; recognizedText?: string; suggestedMerchant?: string; suggestedAmount?: number; suggestedTransactionDate?: string; suggestedTransactionType?: string; suggestedCategory?: string; parsingConfidence?: number }

class ApiError extends Error { readonly status: number; constructor(message: string, status: number) { super(message); this.status = status } }

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
  const body = await response.json().catch(() => ({})) as Envelope<T> & { message?: string; Message?: string }
  if (!response.ok) throw new ApiError(body.message ?? body.Message ?? `Request failed (${response.status})`, response.status)
  return unwrap(body)
}

function Login({ onLogin }: { onLogin: (user: User) => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [registering, setRegistering] = useState(false)
  const [notice, setNotice] = useState('')
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError('')
    try {
      const user = await api<User>('/api/Auth/Login', undefined, { method: 'POST', body: JSON.stringify({ userName: username, password }) })
      if (!(user.accessToken ?? user.AccessToken)) throw new Error('Login response did not include an access token.')
      sessionStorage.setItem('spendwise-user', JSON.stringify(user)); onLogin(user)
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to sign in.') }
    finally { setBusy(false) }
  }
  async function register(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(''); setNotice('')
    const fields = new FormData(event.currentTarget)
    try {
      await api('/api/Auth/Registration', undefined, { method: 'POST', body: JSON.stringify({ firstName: fields.get('firstName'), lastName: fields.get('lastName'), email: fields.get('email'), phone: fields.get('phone'), gender: fields.get('gender'), password: fields.get('password') }) })
      setUsername(String(fields.get('email'))); setPassword(''); setRegistering(false); setNotice('Account created. Sign in with your email and password.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to create your account.') }
    finally { setBusy(false) }
  }
  return <main className="login-screen"><section className="login-art"><div className="art-brand"><span className="brand-symbol">↗</span> spendwise<span>.</span></div><div className="art-copy"><div className="eyebrow">MONEY, MADE CLEAR</div><h1>Your finances,<br/>in a better place.</h1><p>One calm space to see where your money goes and what matters next.</p></div><div className="art-card"><span>YOUR BALANCE</span><strong>Rs. 248,560</strong><div className="art-chart"><i/><i/><i/><i/><i/><i/><i/><i/><i/><i/><i/><i/></div><small><b>↗ 12.8%</b> &nbsp; this month</small></div><span className="art-note note-one">✦ &nbsp; Spend with intention</span><span className="art-note note-two">↗ &nbsp; Your goals, in sight</span><div className="login-copyright">© 2026 Spendwise · Personal finance, made simple</div></section><section className="login-panel"><div className="login-content"><div className="mobile-brand"><span className="brand-symbol">↗</span> spendwise<span>.</span></div><div className="eyebrow">{registering ? 'GET STARTED' : 'WELCOME BACK'}</div><h2>{registering ? 'Create your account' : 'Sign in to your account'}</h2><p className="muted">{registering ? 'Set up your account to start tracking.' : 'Pick up right where you left off.'}</p>{notice && <div className="success-banner">{notice}</div>}{registering ? <form className="login-form" onSubmit={register}><label>First name<input name="firstName" autoComplete="given-name" maxLength={50} required/></label><label>Last name<input name="lastName" autoComplete="family-name" maxLength={50}/></label><label>Email address<input name="email" type="email" autoComplete="email" required/></label><label>Phone number<input name="phone" type="tel" minLength={10} maxLength={15} pattern="[0-9+() -]{10,15}" required/></label><label>Gender<select name="gender" defaultValue="" required><option value="" disabled>Select gender</option><option>Male</option><option>Female</option><option>Other</option></select></label><label>Password<input name="password" type="password" autoComplete="new-password" minLength={8} maxLength={100} required/></label>{error && <div className="error-banner">{error}</div>}<button className="button primary full" disabled={busy}>{busy ? 'Creating account…' : 'Create account'} <span>→</span></button></form> : <form className="login-form" onSubmit={submit}><label>Email address<input type="email" autoComplete="username" required placeholder="you@example.com" value={username} onChange={e => setUsername(e.target.value)}/></label><label>Password<input type="password" autoComplete="current-password" required placeholder="Enter your password" value={password} onChange={e => setPassword(e.target.value)}/></label><div className="form-between"><label className="check-label"><input type="checkbox"/> Remember me</label><span className="muted">Secure sign in</span></div>{error && <div className="error-banner">{error}</div>}<button className="button primary full" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'} <span>→</span></button></form>}<button className="switch-auth" onClick={() => { setRegistering(!registering); setError(''); setNotice('') }}>{registering ? 'Already have an account? Sign in' : 'New to Spendwise? Create an account'}</button>{!registering && <div className="secure-note"><span>✓</span> Sign-in is kept for this browser session</div>}</div><div className="login-foot">Need help? <a href="mailto:support@spendwise.app">Contact support</a></div></section></main>
}

function GettingStarted({ token, onComplete }: { token: string; onComplete: (business: Business, book: Cashbook) => void }) {
  const [step, setStep] = useState(1)
  const [business, setBusiness] = useState<Business | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  async function createBusiness(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError('')
    const fields = new FormData(event.currentTarget)
    const form = new FormData(); form.append('Name', String(fields.get('name'))); form.append('Category', String(fields.get('category'))); form.append('Description', String(fields.get('description') ?? ''))
    try {
      const result = await api<Business>('/api/Business/create', token, { method: 'POST', body: form })
      const created = { ...result, id: result.id ?? (result as Business & { Id: string }).Id, name: result.name ?? (result as Business & { Name: string }).Name }
      if (!created.id) throw new Error('Business was created, but the API did not return its ID. Refresh the page and try adding a cashbook.')
      setBusiness(created); setStep(2)
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not create the business.') }
    finally { setBusy(false) }
  }
  async function createCashbook(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!business) return
    setBusy(true); setError('')
    const fields = new FormData(event.currentTarget); const name = String(fields.get('cashBookName'))
    try {
      await api('/api/CashBook', token, { method: 'POST', body: JSON.stringify({ cashBookName: name, businessId: business.id }) })
      const result = await api<Cashbook[]>(`/api/CashBook/getall?id=${encodeURIComponent(business.id)}`, token)
      const books = (result ?? []).map(item => ({ ...item, id: item.id ?? (item as Cashbook & { Id: string }).Id, cashBookName: item.cashBookName ?? (item as Cashbook & { CashBookName: string }).CashBookName, businessId: item.businessId ?? (item as Cashbook & { BusinessId: string }).BusinessId ?? business.id }))
      const created = books.find(item => item.cashBookName === name) ?? books[0]
      if (!created?.id) throw new Error('Cashbook was created, but the API did not return it. Refresh the page to continue.')
      onComplete(business, created)
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not create the cashbook.') }
    finally { setBusy(false) }
  }
  return <main className="setup-page"><section className="setup-card"><div className="brand"><span className="brand-symbol">↗</span> spendwise<span>.</span></div><div className="eyebrow">YOUR WORKSPACE · STEP {step} OF 2</div><h1>{step === 1 ? 'Let’s set up your business' : 'Create your first cashbook'}</h1><p className="muted">{step === 1 ? 'Add the workspace where you’ll organize your finances.' : `Cashbooks keep transactions organized for ${business?.name ?? 'your business'}.`}</p><div className="setup-progress"><i className={step >= 1 ? 'complete' : ''}/><i className={step >= 2 ? 'complete' : ''}/></div>{step === 1 ? <form className="setup-form" onSubmit={createBusiness}><label>Business name<input name="name" maxLength={150} required placeholder="e.g. Ali’s Store"/></label><label>Category<select name="category" required defaultValue=""><option value="" disabled>Choose a category</option><option>Retail</option><option>Food & beverage</option><option>Services</option><option>Freelance</option><option>Personal</option><option>Other</option></select></label><label>Description <span className="muted">(optional)</span><textarea name="description" maxLength={500} rows={3} placeholder="What is this workspace for?"/></label><button className="button primary full" disabled={busy}>{busy ? 'Creating business…' : 'Continue'} <span>→</span></button></form> : <form className="setup-form" onSubmit={createCashbook}><label>Cashbook name<input name="cashBookName" maxLength={200} required placeholder="e.g. Daily expenses"/></label><div className="setup-tip"><span>▣</span><p>You can create more cashbooks later to track different accounts or activities.</p></div><button className="button primary full" disabled={busy}>{busy ? 'Creating cashbook…' : 'Finish setup'} <span>→</span></button><button type="button" className="setup-back" onClick={() => setStep(1)} disabled={busy}>← Back to business details</button></form>}{error && <div className="error-banner">{error}</div>}</section></main>
}

function CreateCashbookDialog({ token, businessId, onClose, onCreated }: { token: string; businessId: string; onClose: () => void; onCreated: (name: string) => Promise<void> }) {
  const [busy, setBusy] = useState(false)
  const [created, setCreated] = useState(false)
  const [error, setError] = useState('')
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError('')
    const name = String(new FormData(event.currentTarget).get('cashBookName')).trim()
    try {
      if (!created) {
        await api('/api/CashBook', token, { method: 'POST', body: JSON.stringify({ cashBookName: name, businessId }) })
        setCreated(true)
      }
      await onCreated(name)
      onClose()
    } catch (e) { setError(created ? `Cashbook created, but the list could not refresh. Retry to load it. ${e instanceof Error ? e.message : ''}` : e instanceof Error ? e.message : 'Could not create the cashbook.') }
    finally { setBusy(false) }
  }
  return <div className="overlay" onMouseDown={e => e.target === e.currentTarget && !busy && onClose()}><section className="dialog create-cashbook-dialog"><header><div><div className="eyebrow">ORGANIZE YOUR MONEY</div><h2>{created ? 'Cashbook created' : 'Create a cashbook'}</h2></div><button className="close" onClick={onClose} disabled={busy}>×</button></header><form className="dialog-content setup-form" onSubmit={submit}><p className="muted">{created ? 'The cashbook is saved. Retry loading your cashbooks to select it.' : 'Create a cashbook for this business. You can switch between cashbooks from your workspace.'}</p>{!created && <label>Cashbook name<input name="cashBookName" autoFocus maxLength={200} required placeholder="e.g. Daily expenses"/></label>}{error && <div className="error-banner">{error}</div>}<button className="button primary full" disabled={busy}>{busy ? 'Please wait…' : created ? 'Retry loading cashbooks' : 'Create cashbook'} <span>→</span></button></form></section></div>
}

function ReceiptDialog({ token, book, onClose, onDone }: { token: string; book: Cashbook; onClose: () => void; onDone: () => void }) {
  const [file, setFile] = useState<File | null>(null)
  const [draft, setDraft] = useState<ReceiptDraft | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  async function upload() {
    if (!file) return
    const form = new FormData(); form.append('CashBookId', book.id); form.append('File', file)
    setBusy(true); setError('')
    try { setDraft(await api<ReceiptDraft>('/api/Transaction/receipts/drafts', token, { method: 'POST', body: form })) }
    catch (e) { setError(e instanceof Error ? e.message : 'Upload failed.') }
    finally { setBusy(false) }
  }
  async function analyze() {
    if (!draft) return
    setBusy(true); setError('')
    try {
      await api(`/api/Transaction/receipts/drafts/${draft.draftId}/analyze`, token, { method: 'POST' })
      await api(`/api/Transaction/receipts/drafts/${draft.draftId}/suggest`, token, { method: 'POST' })
      setDraft(await api<ReceiptDraft>(`/api/Transaction/receipts/drafts/${draft.draftId}`, token))
    } catch (e) { setError(e instanceof Error ? e.message : 'Receipt analysis failed.') }
    finally { setBusy(false) }
  }
  async function confirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!draft) return
    const data = new FormData(event.currentTarget); setBusy(true); setError('')
    try {
      await api(`/api/Transaction/receipts/drafts/${draft.draftId}/confirm`, token, { method: 'POST', body: JSON.stringify({ amount: Number(data.get('amount')), transactionType: data.get('type'), transactionDate: new Date(String(data.get('date'))).toISOString(), merchant: data.get('merchant'), remarks: data.get('notes'), category: data.get('category') }) })
      onDone()
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not create transaction.') }
    finally { setBusy(false) }
  }
  return <div className="overlay" onMouseDown={e => e.target === e.currentTarget && onClose()}><section className="dialog"><header><div><div className="eyebrow">RECEIPT ASSIST</div><h2>Add from a receipt</h2></div><button className="close" onClick={onClose}>×</button></header><div className="dialog-content">{!draft && <><p className="muted">Upload a JPG, PNG, or PDF. Review the extracted details before saving.</p><label className="dropzone"><span className="upload-symbol">↑</span><strong>{file?.name ?? 'Choose a receipt file'}</strong><small>{file ? `${(file.size / 1024 / 1024).toFixed(2)} MB` : 'JPG, PNG, or PDF · up to 10 MB'}</small><input type="file" accept="image/jpeg,image/png,application/pdf,.jpg,.jpeg,.png,.pdf" onChange={e => setFile(e.target.files?.[0] ?? null)}/></label><button className="button primary full" disabled={!file || busy} onClick={upload}>{busy ? 'Uploading…' : 'Upload receipt'} <span>→</span></button></>}{draft && draft.status !== 'ReadyForReview' && <div className="processing"><div className="processing-icon">▤</div><h3>Receipt uploaded</h3><p className="muted">It won’t be added as a transaction until you review and confirm it.</p><button className="button primary full" disabled={busy} onClick={analyze}>{busy ? 'Reading receipt…' : 'Analyze receipt'} <span>→</span></button></div>}{draft?.status === 'ReadyForReview' && <form className="review-form" onSubmit={confirm}><div className="review-alert"><span>✦</span><div><strong>Draft ready for review</strong><small>Check each field before creating the transaction.</small></div></div><div className="confidence"><span>Extraction confidence</span><b>{Math.round((draft.parsingConfidence ?? 0) * 100)}%</b></div><div className="fields"><label>Merchant<input name="merchant" defaultValue={draft.suggestedMerchant ?? ''} placeholder="Merchant name"/></label><label>Amount<input name="amount" type="number" min="0.01" step="0.01" required defaultValue={draft.suggestedAmount ?? ''}/></label><label>Date<input name="date" type="date" required defaultValue={draft.suggestedTransactionDate?.slice(0, 10) ?? new Date().toISOString().slice(0, 10)}/></label><label>Type<select name="type" defaultValue={draft.suggestedTransactionType ?? 'CashOut'}><option value="CashOut">Cash out</option><option value="CashIn">Cash in</option></select></label><label>Category<input name="category" defaultValue={draft.suggestedCategory ?? ''} placeholder="Category suggestion"/></label><label>Notes<input name="notes" placeholder="Optional note"/></label></div><details><summary>Recognized receipt text</summary><pre>{draft.recognizedText}</pre></details><button className="button primary full" disabled={busy}>{busy ? 'Saving transaction…' : 'Confirm and add transaction'} <span>→</span></button></form>}{error && <div className="error-banner">{error}</div>}</div></section></div>
}

function Dashboard({ user, onLogout }: { user: User; onLogout: () => void }) {
  const token = user.accessToken ?? user.AccessToken ?? ''
  const [businesses, setBusinesses] = useState<Business[]>([])
  const [businessId, setBusinessId] = useState('')
  const [books, setBooks] = useState<Cashbook[]>([])
  const [book, setBook] = useState<Cashbook | null>(null)
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [showReceipt, setShowReceipt] = useState(false)
  const [showCreateCashbook, setShowCreateCashbook] = useState(false)
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [needsSetup, setNeedsSetup] = useState(false)
  const fullName = `${user.firstName ?? user.FirstName ?? ''} ${user.lastName ?? user.LastName ?? ''}`.trim() || user.email || user.Email || 'there'
  useEffect(() => { api<Business[]>('/api/Business/all', token).then(list => { const normalized = (list ?? []).map(item => ({ ...item, id: item.id ?? (item as Business & { Id: string }).Id, name: item.name ?? (item as Business & { Name: string }).Name })); setBusinesses(normalized); if (normalized.length) setBusinessId(normalized[0].id); else setNeedsSetup(true) }).catch(e => { if (e instanceof ApiError && e.status === 404) setNeedsSetup(true); else setError(e instanceof Error ? e.message : 'Could not load businesses.') }).finally(() => setLoading(false)) }, [])
  useEffect(() => { if (!businessId) { setBooks([]); setBook(null); return } api<Cashbook[]>(`/api/CashBook/getall?id=${encodeURIComponent(businessId)}`, token).then(list => { const fixed = (list ?? []).map(item => ({ ...item, id: item.id ?? (item as unknown as { Id: string }).Id, cashBookName: item.cashBookName ?? (item as unknown as { CashBookName: string }).CashBookName })); setBooks(fixed); setBook(current => fixed.find(item => item.id === current?.id) ?? fixed[0] ?? null) }).catch(e => setError(e instanceof Error ? e.message : 'Could not load cashbooks.')) }, [businessId])
  useEffect(() => { if (!book?.id) { setTransactions([]); return } api<Transaction[]>(`/api/Transaction/getall?CashBookId=${encodeURIComponent(book.id)}`, token).then(setTransactions).catch(e => setError(e instanceof Error ? e.message : 'Could not load transactions.')) }, [book?.id])
  const visible = useMemo(() => transactions.filter(t => `${t.remarks ?? ''} ${t.transactionType}`.toLowerCase().includes(search.toLowerCase())), [transactions, search])
  const income = transactions.filter(t => t.transactionType?.toLowerCase() === 'cashin').reduce((s, t) => s + Number(t.amount || 0), 0)
  const expense = transactions.filter(t => t.transactionType?.toLowerCase() === 'cashout').reduce((s, t) => s + Number(t.amount || 0), 0)
  const balance = income - expense
  async function reloadCashbooks(selectName?: string) {
    if (!businessId) return
    const list = await api<Cashbook[]>(`/api/CashBook/getall?id=${encodeURIComponent(businessId)}`, token)
    const fixed = (list ?? []).map(item => ({ ...item, id: item.id ?? (item as unknown as { Id: string }).Id, cashBookName: item.cashBookName ?? (item as unknown as { CashBookName: string }).CashBookName, businessId: item.businessId ?? (item as unknown as { BusinessId: string }).BusinessId ?? businessId }))
    setBooks(fixed)
    setBook(fixed.find(item => item.cashBookName === selectName) ?? fixed.find(item => item.id === book?.id) ?? fixed[0] ?? null)
  }
  if (loading) return <main className="setup-page"><div className="setup-loading"><span className="brand-symbol">↗</span><p>Loading your workspace…</p></div></main>
  if (needsSetup) return <GettingStarted token={token} onComplete={(createdBusiness, createdBook) => { setBusinesses([createdBusiness]); setBusinessId(createdBusiness.id); setBooks([createdBook]); setBook(createdBook); setNeedsSetup(false) }}/>
  return <div className="shell"><aside className="sidebar"><div className="brand"><span className="brand-symbol">↗</span> spendwise<span>.</span></div><small className="side-label">WORKSPACE</small><label className="workspace"><span className="workspace-icon">{(businesses.find(b => b.id === businessId)?.name ?? 'S').slice(0, 1).toUpperCase()}</span><select value={businessId} onChange={e => setBusinessId(e.target.value)}>{businesses.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}</select><span>⌄</span></label><small className="side-label">MENU</small><nav><button className="nav active"><span>◫</span> Overview</button><button className="nav" onClick={() => document.getElementById('cashbooks')?.scrollIntoView({ behavior: 'smooth' })}><span>▣</span> Cashbooks <i>{books.length}</i></button><button className="nav" onClick={() => setError('Reports are coming soon.')}><span>⌁</span> Reports</button><button className="nav" onClick={() => setError('Member management is coming soon.')}><span>♧</span> Members</button><div className="nav-rule"/><button className="nav" onClick={() => setError('Settings are coming soon.')}><span>⚙</span> Settings</button></nav><div className="sidebar-bottom"><div className="support-card"><span>✦</span><strong>Need a hand?</strong><p>Tips to make the most of Spendwise.</p><a href="mailto:support@spendwise.app">Visit help center →</a></div><button className="user-row" onClick={onLogout}><span className="user-avatar">{fullName.slice(0, 1).toUpperCase()}</span><span><strong>{fullName}</strong><small>{user.email ?? user.Email ?? 'My account'}</small></span><span className="logout">⇥</span></button></div></aside>
  <main className="main"><header className="topbar"><div className="crumb">Workspace <span>/</span> <strong>Overview</strong></div><label className="search"><span>⌕</span><input placeholder="Search transactions..." value={search} onChange={e => setSearch(e.target.value)}/><kbd>⌘ K</kbd></label><button className="notification">♧<i/></button><span className="top-user">{fullName.slice(0, 1).toUpperCase()}</span></header><div className="content"><div className="heading"><div><div className="eyebrow">{new Intl.DateTimeFormat('en', { weekday: 'long', month: 'long', day: 'numeric' }).format(new Date()).toUpperCase()}</div><h1>Good morning, {fullName.split(' ')[0]} <span>✦</span></h1><p className="muted">Here’s what’s happening with your money today.</p></div><button className="button primary" disabled={!book} onClick={() => setShowReceipt(true)}>＋ Add receipt</button></div>{error && <div className="error-banner">{error}<button onClick={() => setError('')}>Dismiss</button></div>}
  <section className="hero"><div><div className="hero-kicker"><i/> YOUR MONEY AT A GLANCE</div><h2>A clearer view of<br/>your financial day.</h2><p>Every transaction tells a story. Keep yours moving in the right direction.</p><button onClick={() => document.getElementById('transactions')?.scrollIntoView({ behavior: 'smooth' })}>Explore your activity <span>→</span></button></div><div className="hero-chart"><div className="hero-total"><small>THIS MONTH</small><strong>Rs. {Math.abs(balance).toLocaleString('en-PK')}</strong><span><b>↗ 8.4%</b> vs last month</span></div><div className="chart-bars">{[34, 52, 42, 66, 48, 72, 57, 87, 65, 96, 72, 82, 60, 91, 74].map((h, i) => <i key={i} style={{ height: `${h}%` }}/>)}</div></div><span className="hero-spark">✳</span></section>
  <div className="section-heading"><div><h2>Your overview</h2><p>A snapshot of your selected cashbook.</p></div><button className="period">This month　⌄</button></div><section className="metrics"><article><div className="metric-symbol green">▣</div><span className="trend">↗ 8.4%</span><small>TOTAL BALANCE</small><strong>Rs. {balance.toLocaleString('en-PK')}</strong><p>Across selected cashbook</p><div className="sparkline green-line"/></article><article><div className="metric-symbol blue">↗</div><span className="trend">↗ 4.2%</span><small>TOTAL INCOME</small><strong>Rs. {income.toLocaleString('en-PK')}</strong><p>Money in this period</p><div className="sparkline blue-line"/></article><article><div className="metric-symbol orange">↘</div><span className="trend down">↘ 2.1%</span><small>TOTAL EXPENSES</small><strong>Rs. {expense.toLocaleString('en-PK')}</strong><p>Money out this period</p><div className="sparkline orange-line"/></article><article><div className="metric-symbol purple">▤</div><span className="trend neutral">THIS MONTH</span><small>TRANSACTIONS</small><strong>{transactions.length}</strong><p>Activity in this cashbook</p><div className="sparkline purple-line"/></article></section>
  <section className="bottom-grid"><article className="panel transactions" id="transactions"><div className="panel-head"><div><h2>Recent transactions</h2><p>{book?.cashBookName ?? book?.name ?? 'Select a cashbook to see activity'}</p></div><button className="text-button">View all →</button></div><div className="table-head"><span>TRANSACTION</span><span>DATE</span><span>TYPE</span><span>AMOUNT</span></div>{loading ? <div className="empty">Loading activity…</div> : visible.length ? visible.slice(0, 7).map((t, i) => <div className="table-row" key={t.id ?? `${t.remarks}-${i}`}><div className="table-transaction"><span className={`txn-icon txn-${i % 4}`}>{t.transactionType?.toLowerCase() === 'cashin' ? '↗' : '▤'}</span><span><strong>{t.remarks || (t.transactionType === 'CashIn' ? 'Cash received' : 'Cash expense')}</strong><small>{book?.cashBookName ?? book?.name ?? 'Cashbook'}</small></span></div><span>{t.transactionDate ? new Date(t.transactionDate).toLocaleDateString() : '—'}</span><span><em className={t.transactionType === 'CashIn' ? 'pill income-pill' : 'pill expense-pill'}>{t.transactionType === 'CashIn' ? 'Income' : 'Expense'}</em></span><strong className={t.transactionType === 'CashIn' ? 'income-amount' : ''}>{t.transactionType === 'CashIn' ? '+' : '−'} Rs. {Number(t.amount).toLocaleString('en-PK')}</strong></div>) : <div className="empty"><span className="empty-receipt">▤</span><strong>No transactions yet</strong><p>Add a transaction or scan a receipt to get started.</p><button onClick={() => setShowReceipt(true)}>Add your first receipt →</button></div>}</article><article className="panel cashbook-panel" id="cashbooks"><div className="panel-head"><div><h2>Your cashbooks</h2><p>Choose a book to focus your view.</p></div><button className="add-small" aria-label="Create a cashbook" onClick={() => setShowCreateCashbook(true)}>＋</button></div><div className="book-list">{books.map((b, i) => <button className={`book-row ${book?.id === b.id ? 'chosen' : ''}`} key={b.id} onClick={() => setBook(b)}><span className={`book-mark mark-${i % 3}`}>{i % 2 ? '⌁' : '▣'}</span><span><strong>{b.cashBookName ?? b.name ?? 'Cashbook'}</strong><small>{b.businessId ? 'Business cashbook' : 'Cashbook'}</small></span><span className="row-arrow">→</span></button>)}{!books.length && <div className="book-empty">{loading ? 'Loading cashbooks…' : 'No cashbooks found for this business.'}</div>}</div><button className="create-book" onClick={() => setShowCreateCashbook(true)}>＋ Create a cashbook</button><div className="insight-card"><span>✦</span><div><small>SPENDING INSIGHT</small><strong>Stay in the know</strong><p>Review activity to spot your spending patterns.</p></div></div></article></section><footer>Made for a clearer money story <span>✦</span><a href={`${API}/swagger`} target="_blank" rel="noreferrer">Help & support</a></footer></div></main>{showReceipt && book && <ReceiptDialog token={token} book={book} onClose={() => setShowReceipt(false)} onDone={() => { setShowReceipt(false); api<Transaction[]>(`/api/Transaction/getall?CashBookId=${encodeURIComponent(book.id)}`, token).then(setTransactions).catch(e => setError(e instanceof Error ? e.message : 'Could not refresh transactions.')) }}/ >}{showCreateCashbook && businessId && <CreateCashbookDialog token={token} businessId={businessId} onClose={() => setShowCreateCashbook(false)} onCreated={reloadCashbooks}/>}</div>
}

function App() {
  const [user, setUser] = useState<User | null>(() => { try { return JSON.parse(sessionStorage.getItem('spendwise-user') ?? 'null') as User | null } catch { return null } })
  function logout() { sessionStorage.removeItem('spendwise-user'); setUser(null) }
  return user && (user.accessToken || user.AccessToken) ? <Dashboard user={user} onLogout={logout}/> : <Login onLogin={setUser}/>
}

export default App
