import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { ThemeProvider } from '@/providers/ThemeProvider'
import { TooltipProvider } from '@/components/ui/tooltip'
import { MotionConfig } from 'motion/react'
import { AnimatedPage } from '@/components/motion/animated-outlet'
import { RouteProgressBar } from '@/components/motion/route-progress-bar'
import { Toaster } from '@/components/ui/sonner'
import { toast } from 'sonner'
import { SidebarProvider, SidebarInset } from '@/components/ui/sidebar'
import { AppSidebar, type AppBusiness, type AppUser } from '@/components/layout/AppSidebar'
import { SiteHeader } from '@/components/layout/SiteHeader'
import { CommandMenu } from '@/components/command/CommandMenu'
import { DashboardPage } from '@/features/dashboard/DashboardPage'
import { TransactionsPage } from '@/features/transactions/TransactionsPage'
import { CashbooksPage } from '@/features/cashbooks/CashbooksPage'
import { MembersPage } from '@/features/members/MembersPage'
import { ReceiptsVaultPage } from '@/features/receipts/ReceiptsVaultPage'
import { ReportsPage } from '@/features/reports/ReportsPage'
import { BusinessSettings } from '@/features/settings/BusinessSettings'
import { SplitExpensesPage } from '@/features/split-expenses/SplitExpensesPage'
import { AuthPage } from '@/features/auth/AuthPage'
import { CreateCashbookDialog } from '@/features/cashbooks/CreateCashbookDialog'
import { ManageCashbookDialog } from '@/features/cashbooks/ManageCashbookDialog'
import { ManualTransactionDialog } from '@/features/transactions/ManualTransactionDialog'
import { ReceiptDialog } from '@/features/transactions/ReceiptDialog'
import { TransactionEditDialog } from '@/features/transactions/TransactionEditDialog'
import { TransactionDetailSheet } from '@/features/transactions/TransactionDetailSheet'
import CreateBusinessDialog from './CreateBusinessDialog'
import UserProfileDialog from './UserProfileDialog'
import KeyboardShortcutsDialog from './KeyboardShortcutsDialog'
import TransactionAttachmentDialog from './TransactionAttachmentDialog'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { ErrorBoundary } from '@/components/app/ErrorBoundary'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { realtimeService } from './realtimeService'
import {
  BriefcaseBusiness,
  Fuel,
  HeartPulse,
  House,
  ReceiptText,
  ShoppingCart,
  Mail,
  Loader2,
  AlertCircle,
  type LucideIcon,
} from 'lucide-react'

const API = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

export type Envelope<T> = { data?: T; Data?: T; message?: string; Message?: string }

export type User = {
  id?: string
  Id?: string
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

export type Business = {
  id: string
  name: string
  category?: string
  description?: string
  fileUrl?: string
  fileName?: string
  isOwner?: boolean
}

export type UserWorkspaceAccess = {
  isOwner: boolean
  businessPermissions: string[]
  cashbookPermissions: string[]
}

export type Cashbook = {
  id: string
  cashBookName?: string
  name?: string
  businessId: string
  isOwner?: boolean
  role?: string
  ownerName?: string
}

export type Transaction = {
  id: string
  amount: number
  transactionType: string
  remarks?: string
  transactionDate?: string
  fileName?: string
  filePath?: string
  createdBy?: string
  createdOn?: string
}

export type TransactionCategory = { label: string; Icon: LucideIcon }

export function getTransactionCategory(transaction: Transaction): TransactionCategory {
  const text = (transaction.remarks ?? '').toLowerCase()
  if (transaction.transactionType?.toLowerCase() === 'cashin') {
    return { label: 'Income', Icon: BriefcaseBusiness }
  }
  if (/rent|house|home|electric|utility|water bill/.test(text)) {
    return { label: 'Housing', Icon: House }
  }
  if (/fuel|petrol|gas station|uber|taxi|ride|bus/.test(text)) {
    return { label: 'Transport', Icon: Fuel }
  }
  if (/food|grocery|restaurant|cafe|meal/.test(text)) {
    return { label: 'Food', Icon: ShoppingCart }
  }
  if (/medical|health|doctor|pharmacy|medicine/.test(text)) {
    return { label: 'Health', Icon: HeartPulse }
  }
  if (/shop|clothes|store|purchase/.test(text)) {
    return { label: 'Shopping', Icon: ShoppingCart }
  }
  return { label: 'Other expense', Icon: ReceiptText }
}

function unwrap<T>(body: Envelope<T> | T): T {
  if (body && typeof body === 'object' && ('data' in body || 'Data' in body)) {
    const candidate = (body as Envelope<T>).data ?? (body as Envelope<T>).Data
    return (candidate ?? body) as T
  }
  return body as T
}

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

let isRefreshing = false
let refreshSubscribers: Array<(token: string) => void> = []

function onTokenRefreshed(token: string) {
  refreshSubscribers.forEach((callback) => callback(token))
  refreshSubscribers = []
}

function addRefreshSubscriber(callback: (token: string) => void) {
  refreshSubscribers.push(callback)
}

export async function api<T>(path: string, token?: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (options.body && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${API}${path}`, { ...options, headers })

  if (response.status === 401 && token) {
    const rawStored = sessionStorage.getItem('spendwise-user')
    if (rawStored) {
      try {
        const storedUser = JSON.parse(rawStored) as User
        const currentRefreshToken = storedUser.refreshToken ?? storedUser.RefreshToken

        if (currentRefreshToken) {
          if (!isRefreshing) {
            isRefreshing = true
            try {
              const refreshResponse = await fetch(`${API}/api/Auth/refresh-token`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: currentRefreshToken }),
              })
              const refreshBody = (await refreshResponse.json().catch(() => ({}))) as Envelope<User>
              if (refreshResponse.ok) {
                const refreshed = unwrap(refreshBody)
                const newAccess = refreshed.accessToken ?? refreshed.AccessToken
                const newRefresh = refreshed.refreshToken ?? refreshed.RefreshToken
                if (newAccess) {
                  storedUser.accessToken = newAccess
                  storedUser.AccessToken = newAccess
                  if (newRefresh) {
                    storedUser.refreshToken = newRefresh
                    storedUser.RefreshToken = newRefresh
                  }
                  sessionStorage.setItem('spendwise-user', JSON.stringify(storedUser))
                  onTokenRefreshed(newAccess)
                  isRefreshing = false

                  headers.set('Authorization', `Bearer ${newAccess}`)
                  const retryResponse = await fetch(`${API}${path}`, { ...options, headers })
                  const retryBody = (await retryResponse.json().catch(() => ({}))) as Envelope<T>
                  if (!retryResponse.ok) {
                    throw new ApiError(
                      retryBody.message ?? retryBody.Message ?? `Request failed (${retryResponse.status})`,
                      retryResponse.status
                    )
                  }
                  return unwrap(retryBody)
                }
              }
            } finally {
              isRefreshing = false
            }
          } else {
            return new Promise<T>((resolve, reject) => {
              addRefreshSubscriber((newToken) => {
                headers.set('Authorization', `Bearer ${newToken}`)
                fetch(`${API}${path}`, { ...options, headers })
                  .then(async (res) => {
                    const b = (await res.json().catch(() => ({}))) as Envelope<T>
                    if (!res.ok) {
                      reject(
                        new ApiError(
                          b.message ?? b.Message ?? `Request failed (${res.status})`,
                          res.status
                        )
                      )
                    } else {
                      resolve(unwrap(b))
                    }
                  })
                  .catch(reject)
              })
            })
          }
        }
      } catch {
        // Fall through
      }
    }
  }

  const body = (await response.json().catch(() => ({}))) as Envelope<T> & { message?: string; Message?: string }
  if (!response.ok) {
    throw new ApiError(
      body.message ?? body.Message ?? `Request failed (${response.status})`,
      response.status
    )
  }
  return unwrap(body)
}

function PendingInviteUrlDialog({
  inviteToken,
  token,
  onClose,
  onAccepted,
  onToast,
}: {
  inviteToken: string
  token: string
  onClose: () => void
  onAccepted: (target?: { businessId?: string; cashbookId?: string }) => void
  onToast: (msg: string) => void
}) {
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [inviteData, setInviteData] = useState<{
    id?: string
    businessName?: string
    cashbookName?: string
    invitedByName?: string
    businessId?: string
    BusinessId?: string
    cashBookId?: string
    CashBookId?: string
  } | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    async function fetchByToken() {
      try {
        setLoading(true)
        const response = await fetch(
          `${API}/api/Invitations/by-token?token=${encodeURIComponent(inviteToken)}`,
          { headers: { Authorization: `Bearer ${token}` } }
        )
        const result = (await response.json().catch(() => ({}))) as Envelope<any>
        if (response.ok) {
          setInviteData(result.data ?? result.Data ?? result)
        } else {
          setError(result?.message || 'Could not validate this invitation link.')
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Error validating invite link.')
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
      const bId = inviteData.businessId || inviteData.BusinessId
      const cbId = inviteData.cashBookId || inviteData.CashBookId
      onAccepted(bId ? { businessId: bId, cashbookId: cbId } : undefined)
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not accept invitation.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <ResponsiveDialog
      open={true}
      onOpenChange={(open) => !open && !busy && onClose()}
      title={
        <div className="flex items-center gap-2">
          <Mail className="size-4 text-primary" />
          <span>You&apos;ve been invited!</span>
        </div>
      }
      description="You received a collaborative invitation link."
    >
      <div className="space-y-4 pt-2">
        {loading && (
          <div className="flex items-center gap-2 text-xs text-muted-foreground py-4">
            <Loader2 className="size-4 animate-spin" />
            <span>Validating invitation credentials...</span>
          </div>
        )}

        {error && (
          <Alert variant="destructive" className="py-2 text-xs">
            <AlertCircle className="size-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {!loading && !error && inviteData && (
          <div className="space-y-4">
            <p className="text-sm">
              <strong>{inviteData.invitedByName || 'A teammate'}</strong> invited you to join{' '}
              <strong>{inviteData.businessName}</strong>
              {inviteData.cashbookName ? ` (${inviteData.cashbookName})` : ''}.
            </p>
            <div className="flex justify-end gap-2 pt-2 border-t">
              <Button variant="ghost" onClick={onClose} disabled={busy} className="h-8 text-xs">
                Cancel
              </Button>
              <Button onClick={accept} disabled={busy} className="h-8 text-xs">
                {busy ? <Loader2 className="size-3.5 animate-spin mr-1.5" /> : null}
                <span>Accept Invitation</span>
              </Button>
            </div>
          </div>
        )}
      </div>
    </ResponsiveDialog>
  )
}

function DashboardApp({ user, onLogout }: { user: User; onLogout: () => void }) {
  const token = user.accessToken ?? user.AccessToken ?? ''
  const [businesses, setBusinesses] = useState<Business[]>([])
  const [businessId, setBusinessId] = useState('')
  const [books, setBooks] = useState<Cashbook[]>([])
  const [book, setBook] = useState<Cashbook | null>(null)
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [currentView, setCurrentView] = useState<string>('overview')

  // Modals & Sheets
  const [showReceipt, setShowReceipt] = useState(false)
  const [manualTxnInitialType, setManualTxnInitialType] = useState<'cashin' | 'cashout'>('cashout')
  const [showManualTransaction, setShowManualTransaction] = useState(false)
  const [showCreateCashbook, setShowCreateCashbook] = useState(false)
  const [showCreateBusiness, setShowCreateBusiness] = useState(false)
  const [showProfile, setShowProfile] = useState(false)
  const [showShortcuts, setShowShortcuts] = useState(false)
  const [commandOpen, setCommandOpen] = useState(false)
  const [notificationsRefreshTrigger, setNotificationsRefreshTrigger] = useState(0)
  const [unreadInvitesCount, setUnreadInvitesCount] = useState(0)
  const [manageCashbook, setManageCashbook] = useState<{ book: Cashbook; mode: 'edit' | 'delete' } | null>(null)
  const [viewAttachment, setViewAttachment] = useState<Transaction | null>(null)
  const [viewTransactionDetail, setViewTransactionDetail] = useState<Transaction | null>(null)
  const [editingTransaction, setEditingTransaction] = useState<Transaction | null>(null)
  const [pendingUrlInviteToken, setPendingUrlInviteToken] = useState<string | null>(null)
  const [loadingTransactions, setLoadingTransactions] = useState(false)

  const [userAccess, setUserAccess] = useState<UserWorkspaceAccess>({
    isOwner: true,
    businessPermissions: [],
    cashbookPermissions: [],
  })

  const showToast = useCallback((msg: string) => {
    toast(msg)
  }, [])

  // Permissions calculation
  useEffect(() => {
    if (!businessId) {
      setUserAccess({ isOwner: true, businessPermissions: [], cashbookPermissions: [] })
      return
    }
    const cbQuery = book?.id ? `?cashbookId=${encodeURIComponent(book.id)}` : ''
    api<UserWorkspaceAccess>(
      `/api/MemberManagement/businesses/${encodeURIComponent(businessId)}/access${cbQuery}`,
      token
    )
      .then((res) => {
        if (res) setUserAccess(res)
      })
      .catch(() => {
        const currentBiz = businesses.find((b) => b.id === businessId)
        setUserAccess({
          isOwner: currentBiz?.isOwner ?? true,
          businessPermissions: [],
          cashbookPermissions: [],
        })
      })
  }, [businessId, book?.id, token, businesses])

  const isOwner = userAccess.isOwner
  const canCreateCashbook = isOwner || userAccess.businessPermissions.includes('CASHBOOK_CREATE')
  const canEditCashbook = isOwner || userAccess.businessPermissions.includes('CASHBOOK_UPDATE')
  const canDeleteCashbook = isOwner || userAccess.businessPermissions.includes('CASHBOOK_DELETE')
  const canCreateTransaction = isOwner || userAccess.cashbookPermissions.includes('TRANSACTION_CREATE')
  const canEditTransaction = isOwner || userAccess.cashbookPermissions.includes('TRANSACTION_UPDATE')
  const canDeleteTransaction = isOwner || userAccess.cashbookPermissions.includes('TRANSACTION_DELETE')

  const fetchUnreadInvites = useCallback(async () => {
    try {
      const data = await api<any[]>('/api/Invitations/mypendingInvitations', token)
      setUnreadInvitesCount(Array.isArray(data) ? data.length : 0)
    } catch {
      // Ignore background badge fetch errors
    }
  }, [token])

  const reloadTransactions = useCallback(
    async (bookIdToLoad?: string) => {
      const activeId = bookIdToLoad ?? book?.id
      if (!activeId) return
      setLoadingTransactions(true)
      try {
        const list = await api<Transaction[]>(
          `/api/Transaction/getall?CashBookId=${encodeURIComponent(activeId)}`,
          token
        )
        const sorted = (list ?? [])
          .map((item) => ({
            ...item,
            id: item.id ?? (item as unknown as { Id?: string }).Id ?? '',
            amount: Number(item.amount ?? (item as unknown as { Amount?: number }).Amount ?? 0),
            transactionType:
              item.transactionType ??
              (item as unknown as { TransactionType?: string }).TransactionType ??
              'CashOut',
            remarks: item.remarks ?? (item as unknown as { Remarks?: string }).Remarks ?? '',
            transactionDate:
              item.transactionDate ?? (item as unknown as { TransactionDate?: string }).TransactionDate,
            createdBy: item.createdBy ?? (item as unknown as { CreatedBy?: string }).CreatedBy,
            fileName: item.fileName ?? (item as unknown as { FileName?: string }).FileName,
            filePath: item.filePath ?? (item as unknown as { FilePath?: string }).FilePath,
            createdOn: item.createdOn ?? (item as unknown as { CreatedOn?: string }).CreatedOn,
          }))
          .sort((a, b) => (b.transactionDate ?? '').localeCompare(a.transactionDate ?? ''))
        setTransactions(sorted)
      } catch (e) {
        showToast(e instanceof Error ? e.message : 'Could not refresh transactions.')
      } finally {
        setLoadingTransactions(false)
      }
    },
    [book?.id, token, showToast]
  )

  const reloadCashbooks = useCallback(
    async (targetBusinessId?: string, preferredCashbookId?: string) => {
      const bId = targetBusinessId !== undefined ? targetBusinessId : businessId
      try {
        const query = bId ? `?id=${encodeURIComponent(bId)}` : ''
        const list = await api<Cashbook[]>(
          `/api/CashBook/getall${query}`,
          token
        )
        const normalized = (list ?? []).map((item) => ({
          ...item,
          id: item.id ?? (item as Cashbook & { Id: string }).Id,
          cashBookName:
            item.cashBookName ??
            (item as Cashbook & { CashBookName?: string }).CashBookName ??
            item.name ??
            (item as Cashbook & { Name?: string }).Name ??
            'Cashbook',
          businessId: item.businessId ?? (item as Cashbook & { BusinessId: string }).BusinessId ?? bId,
          isOwner: item.isOwner ?? true,
          role: item.role ?? (item.isOwner === false ? 'Member' : 'Owner'),
          ownerName: item.ownerName,
        }))
        setBooks(normalized)
        const selected =
          (preferredCashbookId && normalized.find((b) => b.id === preferredCashbookId)) ||
          normalized.find((b) => b.id === book?.id) ||
          normalized[0] ||
          null
        setBook(selected)
        if (selected) {
          void reloadTransactions(selected.id)
        } else {
          setTransactions([])
        }
      } catch (e) {
        showToast(e instanceof Error ? e.message : 'Could not load cashbooks.')
      }
    },
    [businessId, book?.id, token, reloadTransactions, showToast]
  )

  const reloadBusinesses = useCallback(
    async (preferredBusinessId?: string, preferredCashbookId?: string) => {
      try {
        const list = await api<Business[]>('/api/Business/all', token)
        const normalized = (list ?? []).map((item) => ({
          ...item,
          id: item.id ?? (item as Business & { Id: string }).Id,
          name:
            item.name ??
            (item as Business & { Name?: string }).Name ??
            (item as Business & { businessName?: string }).businessName ??
            'Workspace',
        }))
        setBusinesses(normalized)
        const active =
          (preferredBusinessId && normalized.find((b) => b.id === preferredBusinessId)) ||
          normalized[0] ||
          null
        if (active) {
          setBusinessId(active.id)
          await reloadCashbooks(active.id, preferredCashbookId)
        } else {
          setBusinessId('')
          await reloadCashbooks('', preferredCashbookId)
        }
      } catch (e) {
        showToast(e instanceof Error ? e.message : 'Could not load businesses.')
      }
    },
    [token, reloadCashbooks, showToast]
  )

  // SignalR Realtime Hub
  const bookRef = useRef(book)
  bookRef.current = book
  const businessIdRef = useRef(businessId)
  businessIdRef.current = businessId

  useEffect(() => {
    if (!token) return

    realtimeService.init(token, API, {
      onInvitationReceived: (payload) => {
        void fetchUnreadInvites()
        setNotificationsRefreshTrigger((prev) => prev + 1)
        showToast(payload.message || `${payload.invitedByName} sent you an invitation!`)
      },
      onInvitationAccepted: (payload) => {
        showToast(payload.message || `${payload.acceptedByName} accepted your invitation!`)
        void reloadBusinesses()
        void reloadCashbooks()
      },
      onWorkspacesChanged: () => {
        void reloadBusinesses()
        void reloadCashbooks()
        void fetchUnreadInvites()
      },
      onCashbookCreated: (payload) => {
        if (payload.businessId === businessIdRef.current) {
          void reloadCashbooks()
          showToast(
            payload.message || `${payload.createdByName} created cashbook '${payload.cashbookName}'`
          )
        }
      },
      onTransactionCreated: (payload) => {
        if (payload.cashBookId === bookRef.current?.id) {
          void reloadTransactions(payload.cashBookId)
          const currentId = user.id ?? (user as unknown as { Id?: string }).Id
          if (payload.creatorUserId !== currentId) {
            showToast(payload.message || `${payload.createdByName} added a transaction.`)
          }
        }
      },
      onTransactionUpdated: (payload) => {
        if (payload.cashBookId === bookRef.current?.id) {
          void reloadTransactions(payload.cashBookId)
          const currentId = user.id ?? (user as unknown as { Id?: string }).Id
          if (payload.modifierUserId !== currentId) {
            showToast(payload.message || `${payload.modifiedByName} updated a transaction.`)
          }
        }
      },
      onTransactionDeleted: (payload) => {
        if (payload.cashBookId === bookRef.current?.id) {
          void reloadTransactions(payload.cashBookId)
          const currentId = user.id ?? (user as unknown as { Id?: string }).Id
          if (payload.deleterUserId !== currentId) {
            showToast(payload.message || `${payload.deletedByName} deleted a transaction.`)
          }
        }
      },
    })
  }, [token, fetchUnreadInvites, user, reloadBusinesses, reloadCashbooks, reloadTransactions, showToast])

  useEffect(() => {
    void realtimeService.setCashbook(book?.id ?? null)
  }, [book?.id])

  useEffect(() => {
    void realtimeService.setBusiness(businessId || null)
  }, [businessId])

  // Initial mount
  useEffect(() => {
    const inviteParam = sessionStorage.getItem('spendwise-invite-token')
    if (inviteParam) {
      setPendingUrlInviteToken(inviteParam)
      sessionStorage.removeItem('spendwise-invite-token')
    }
    void reloadBusinesses()
    void fetchUnreadInvites()
  }, [reloadBusinesses, fetchUnreadInvites])

  // Global hotkeys
  useEffect(() => {
    let gotoPrefix = false
    let gotoTimer: number | null = null

    function handleKeyDown(e: KeyboardEvent) {
      const activeEl = document.activeElement
      const isInputActive =
        activeEl &&
        (activeEl.tagName === 'INPUT' ||
          activeEl.tagName === 'TEXTAREA' ||
          activeEl.tagName === 'SELECT' ||
          (activeEl as HTMLElement).isContentEditable)

      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        setCommandOpen((prev) => !prev)
        return
      }

      if ((e.metaKey || e.ctrlKey) && e.key === '/') {
        e.preventDefault()
        setShowShortcuts((prev) => !prev)
        return
      }

      if (isInputActive) return

      if (e.key === 'Escape') {
        setShowShortcuts(false)
        setShowProfile(false)
        setCommandOpen(false)
        return
      }

      if (e.key.toLowerCase() === 'n' && !gotoPrefix) {
        if (book && canCreateTransaction) {
          setShowManualTransaction(true)
        }
        return
      }

      if (e.key.toLowerCase() === 'r' && !gotoPrefix) {
        if (book && canCreateTransaction) {
          setShowReceipt(true)
        }
        return
      }

      if (e.key.toLowerCase() === 'g') {
        gotoPrefix = true
        if (gotoTimer) clearTimeout(gotoTimer)
        gotoTimer = window.setTimeout(() => {
          gotoPrefix = false
        }, 1200)
        return
      }

      if (gotoPrefix) {
        const target = e.key.toLowerCase()
        gotoPrefix = false
        if (target === 'o' || target === 'd') setCurrentView('overview')
        else if (target === 't') setCurrentView('transactions')
        else if (target === 'c') setCurrentView('cashbooks')
        else if (target === 'm') setCurrentView('members')
        else if (target === 'v') setCurrentView('receipts')
        else if (target === 'r') setCurrentView('reports')
        else if (target === 's') setCurrentView('settings')
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => {
      window.removeEventListener('keydown', handleKeyDown)
      if (gotoTimer) clearTimeout(gotoTimer)
    }
  }, [book, canCreateTransaction])

  const selectedBusiness = useMemo(() => {
    return businesses.find((b) => b.id === businessId) || null
  }, [businesses, businessId])

  const appUser: AppUser = {
    id: user.id ?? (user as unknown as { Id?: string }).Id,
    firstName: user.firstName ?? (user as unknown as { FirstName?: string }).FirstName,
    lastName: user.lastName ?? (user as unknown as { LastName?: string }).LastName,
    email: user.email ?? (user as unknown as { Email?: string }).Email,
  }

  const appBusinesses: AppBusiness[] = businesses.map((b) => ({
    id: b.id,
    name: b.name || (b as unknown as { businessName?: string }).businessName || (b as unknown as { Name?: string }).Name || 'Workspace',
    category: b.category,
    description: b.description,
    fileUrl: b.fileUrl,
    isOwner: b.isOwner,
  }))

  const handleDeleteTransaction = async (txn: Transaction) => {
    try {
      await api(`/api/Transaction/${encodeURIComponent(txn.id)}`, token, {
        method: 'DELETE',
      })
      showToast('Transaction permanently deleted')
      await reloadTransactions()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Could not delete transaction.')
    }
  }

  return (
    <SidebarProvider>
      <RouteProgressBar loading={loadingTransactions} />
      <AppSidebar
        businesses={appBusinesses}
        selectedBusiness={selectedBusiness}
        onSelectBusiness={(biz) => {
          setBusinessId(biz.id)
          void reloadCashbooks(biz.id)
        }}
        onCreateBusiness={() => setShowCreateBusiness(true)}
        currentView={currentView}
        onNavigate={setCurrentView}
        user={appUser}
        onLogout={onLogout}
        onOpenProfile={() => setShowProfile(true)}
        onOpenShortcuts={() => setShowShortcuts(true)}
        unreadInvitesCount={unreadInvitesCount}
        token={token}
      />

      <SidebarInset>
        <SiteHeader
          business={selectedBusiness}
          cashbooks={books}
          activeCashbook={book}
          onSelectCashbook={(cb) => {
            setBook(cb)
            void reloadTransactions(cb.id)
            showToast(`Switched active cashbook to "${cb.cashBookName ?? cb.name ?? 'Cashbook'}"`)
          }}
          onAddCashbook={() => setShowCreateCashbook(true)}
          currentView={currentView}
          onNavigate={setCurrentView}
          isOwner={isOwner}
          canCreateCashbook={canCreateCashbook}
          canCreateTransaction={canCreateTransaction}
          canEditTransaction={canEditTransaction}
          canDeleteTransaction={canDeleteTransaction}
          token={token}
          unreadInvitesCount={unreadInvitesCount}
          onInvitationProcessed={(target) => {
            void reloadBusinesses(target?.businessId, target?.cashbookId)
            void fetchUnreadInvites()
          }}
          onToast={showToast}
          onOpenCommand={() => setCommandOpen(true)}
          refreshTrigger={notificationsRefreshTrigger}
          user={appUser}
          onOpenProfile={() => setShowProfile(true)}
        />

        <main className="flex-1 p-4 md:p-6 overflow-y-auto">
          <AnimatedPage currentKey={currentView}>
            {currentView === 'overview' && (
              <DashboardPage
                cashbook={book}
                transactions={transactions}
                loading={loadingTransactions}
                canCreateTransaction={canCreateTransaction}
                canEditTransaction={canEditTransaction}
                canDeleteTransaction={canDeleteTransaction}
                isOwner={isOwner}
                onAddTransaction={(type) => {
                  setManualTxnInitialType(type || 'cashout')
                  setShowManualTransaction(true)
                }}
                onAddReceipt={() => setShowReceipt(true)}
                onEditTransaction={(txn) => setEditingTransaction(txn)}
                onDeleteTransaction={handleDeleteTransaction}
                onSelectTransaction={(txn) => setViewTransactionDetail(txn)}
                onViewAttachment={(txn) => setViewAttachment(txn)}
                onViewAllTransactions={() => setCurrentView('transactions')}
                onToast={showToast}
              />
            )}

            {currentView === 'transactions' && (
              <TransactionsPage
                transactions={transactions}
                token={token}
                cashbookName={book?.cashBookName ?? book?.name ?? 'Cashbook'}
                books={books}
                selectedBookId={book?.id}
                canCreate={canCreateTransaction}
                canEdit={canEditTransaction}
                canDelete={canDeleteTransaction}
                onSelectBook={(cb) => {
                  setBook(cb)
                  void reloadTransactions(cb.id)
                }}
                onBack={() => setCurrentView('overview')}
                onAdd={() => setShowManualTransaction(true)}
                onAddReceipt={() => setShowReceipt(true)}
                onRefresh={() => reloadTransactions()}
                onToast={showToast}
                onViewAttachment={(txn) => setViewAttachment(txn)}
                onOpenDetail={(txn) => setViewTransactionDetail(txn)}
                onEditTransaction={(txn) => setEditingTransaction(txn)}
                onDeleteTransaction={handleDeleteTransaction}
              />
            )}

            {currentView === 'cashbooks' && (
              <CashbooksPage
                books={books}
                selectedBook={book}
                businessName={selectedBusiness?.name ?? 'Workspace'}
                activeBookTransactionsCount={transactions.length}
                canCreate={canCreateCashbook}
                canEdit={canEditCashbook}
                canDelete={canDeleteCashbook}
                onSelectBook={(cb) => {
                  setBook(cb)
                  void reloadTransactions(cb.id)
                }}
                onOpenTransactions={(cb) => {
                  setBook(cb)
                  void reloadTransactions(cb.id)
                  setCurrentView('transactions')
                }}
                onAddCashbook={() => setShowCreateCashbook(true)}
                onEditCashbook={(cb) => setManageCashbook({ book: cb, mode: 'edit' })}
                onDeleteCashbook={(cb) => setManageCashbook({ book: cb, mode: 'delete' })}
                onBack={() => setCurrentView('overview')}
                onToast={showToast}
              />
            )}

            {currentView === 'members' && (
              <MembersPage
                token={token}
                business={{ id: businessId, name: selectedBusiness?.name ?? 'Workspace' }}
                books={books}
                isOwner={isOwner}
                currentUserId={user.id ?? (user as unknown as { Id?: string }).Id}
                onBack={() => setCurrentView('overview')}
                onToast={showToast}
              />
            )}

            {currentView === 'receipts' && (
              <ReceiptsVaultPage
                transactions={transactions}
                token={token}
                cashbookName={book?.cashBookName ?? book?.name ?? 'Cashbook'}
                businessName={selectedBusiness?.name ?? 'Workspace'}
                books={books}
                selectedBookId={book?.id}
                onSelectBook={(cb) => {
                  setBook(cb)
                  void reloadTransactions(cb.id)
                }}
                onBack={() => setCurrentView('overview')}
                onUploadReceipt={() => setShowReceipt(true)}
                onViewAttachment={(txn) => setViewAttachment(txn)}
                onOpenDetail={(txn) => setViewTransactionDetail(txn)}
                onToast={showToast}
              />
            )}

            {currentView === 'reports' && (
              <ReportsPage
                transactions={transactions}
                cashbookName={book?.cashBookName ?? book?.name ?? 'Cashbook'}
                businessName={selectedBusiness?.name ?? 'Workspace'}
                books={books}
                selectedBookId={book?.id}
                onSelectBook={(cb) => {
                  setBook(cb)
                  void reloadTransactions(cb.id)
                }}
                onBack={() => setCurrentView('overview')}
                onToast={showToast}
              />
            )}

            {currentView === 'settings' && selectedBusiness && (
              <BusinessSettings
                token={token}
                business={selectedBusiness}
                onBack={() => setCurrentView('overview')}
                onUpdated={(updated) => {
                  setBusinesses((items) =>
                    items.map((item) => (item.id === updated.id ? updated : item))
                  )
                }}
                onDeleted={() => {
                  void reloadBusinesses()
                  setCurrentView('overview')
                }}
                onToast={showToast}
                api={api}
              />
            )}

            {currentView === 'split-expenses' && user && (
              <SplitExpensesPage
                token={token}
                currentUserId={user.id ?? (user as unknown as { Id?: string }).Id}
                onToast={showToast}
              />
            )}
          </AnimatedPage>
        </main>
      </SidebarInset>

      {/* Global Command Palette */}
      <CommandMenu
        open={commandOpen}
        onOpenChange={setCommandOpen}
        onNavigate={setCurrentView}
        books={books}
        activeBookId={book?.id}
        onSelectBook={(cb) => {
          setBook(cb)
          void reloadTransactions(cb.id)
        }}
        onAddTransaction={(type) => {
          setManualTxnInitialType(type)
          setShowManualTransaction(true)
        }}
        onAddCashbook={() => setShowCreateCashbook(true)}
        onInviteMember={() => setCurrentView('members')}
        onOpenShortcuts={() => setShowShortcuts(true)}
        onOpenProfile={() => setShowProfile(true)}
        transactions={transactions}
        onSelectTransaction={(txn) => setViewTransactionDetail(txn)}
        canCreateTransaction={canCreateTransaction}
        canCreateCashbook={canCreateCashbook}
        isOwner={isOwner}
      />

      {/* Modals & Dialogs */}
      {showManualTransaction && book && (
        <ManualTransactionDialog
          token={token}
          book={book}
          initialType={manualTxnInitialType}
          onClose={() => setShowManualTransaction(false)}
          onDone={async () => reloadTransactions(book.id)}
          api={api}
        />
      )}

      {showReceipt && book && (
        <ReceiptDialog
          token={token}
          book={book}
          onClose={() => setShowReceipt(false)}
          onDone={() => {
            setShowReceipt(false)
            void reloadTransactions(book.id)
          }}
          onToast={showToast}
          api={api}
        />
      )}

      {editingTransaction && (
        <TransactionEditDialog
          token={token}
          transaction={editingTransaction}
          onClose={() => setEditingTransaction(null)}
          onSaved={async () => {
            await reloadTransactions(book?.id)
            showToast('Transaction updated')
          }}
          api={api}
        />
      )}

      {showCreateCashbook && businessId && (
        <CreateCashbookDialog
          token={token}
          businessId={businessId}
          onClose={() => setShowCreateCashbook(false)}
          onCreated={async () => reloadCashbooks()}
          api={api}
        />
      )}

      {manageCashbook && (
        <ManageCashbookDialog
          token={token}
          book={manageCashbook.book}
          mode={manageCashbook.mode}
          onClose={() => setManageCashbook(null)}
          onSaved={async () => reloadCashbooks()}
          api={api}
        />
      )}

      {showCreateBusiness && (
        <CreateBusinessDialog
          token={token}
          onClose={() => setShowCreateBusiness(false)}
          onCreated={(created) => {
            void reloadBusinesses(created.id)
            showToast(`Workspace "${created.name}" created!`)
          }}
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

      {showShortcuts && (
        <KeyboardShortcutsDialog
          onClose={() => setShowShortcuts(false)}
          onNavigate={setCurrentView}
          onAction={(action) => {
            if (action === 'add-transaction' && book) setShowManualTransaction(true)
            if (action === 'add-receipt' && book) setShowReceipt(true)
            if (action === 'focus-search') setCommandOpen(true)
          }}
        />
      )}

      {viewTransactionDetail && (
        <TransactionDetailSheet
          open={Boolean(viewTransactionDetail)}
          onOpenChange={(open) => !open && setViewTransactionDetail(null)}
          transaction={viewTransactionDetail}
          cashbookName={book?.cashBookName ?? book?.name ?? 'Cashbook'}
          businessName={selectedBusiness?.name ?? 'Workspace'}
          canEdit={canEditTransaction}
          canDelete={canDeleteTransaction}
          onEdit={(txn) => {
            setViewTransactionDetail(null)
            setEditingTransaction(txn)
          }}
          onDelete={(txn) => {
            setViewTransactionDetail(null)
            void handleDeleteTransaction(txn)
          }}
          onViewAttachment={(txn) => setViewAttachment(txn)}
          onToast={showToast}
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

      {pendingUrlInviteToken && (
        <PendingInviteUrlDialog
          inviteToken={pendingUrlInviteToken}
          token={token}
          onClose={() => setPendingUrlInviteToken(null)}
          onAccepted={(target) => {
            void reloadBusinesses(target?.businessId, target?.cashbookId)
            void fetchUnreadInvites()
          }}
          onToast={showToast}
        />
      )}
    </SidebarProvider>
  )
}

export default function App() {
  const [user, setUser] = useState<User | null>(() => {
    const raw = sessionStorage.getItem('spendwise-user')
    if (!raw) return null
    try {
      return JSON.parse(raw) as User
    } catch {
      return null
    }
  })

  function handleLogout() {
    sessionStorage.removeItem('spendwise-user')
    sessionStorage.removeItem('spendwise-invite-token')
    void realtimeService.disconnect()
    setUser(null)
  }

  return (
    <ThemeProvider defaultTheme="system" storageKey="spendwise-theme">
      <MotionConfig reducedMotion="user">
        <TooltipProvider>
          <ErrorBoundary>
            <div className="min-h-screen bg-background text-foreground antialiased">
              {user ? (
                <DashboardApp user={user} onLogout={handleLogout} />
              ) : (
                <AuthPage onLogin={setUser} api={api} />
              )}
            </div>
            <Toaster
              position="bottom-right"
              richColors
              closeButton
              expand={false}
              visibleToasts={4}
              gap={8}
              duration={4000}
            />
          </ErrorBoundary>
        </TooltipProvider>
      </MotionConfig>
    </ThemeProvider>
  )
}
