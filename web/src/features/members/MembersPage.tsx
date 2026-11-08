import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { PageHeader } from '@/components/app/PageHeader'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { MotionTabs, MotionTabsList, MotionTabsTrigger } from '@/components/motion/motion-tabs'
import { MotionToggleGroup } from '@/components/motion/motion-toggle-group'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetFooter,
} from '@/components/ui/sheet'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { RoleBadge } from '@/components/app/RoleBadge'
import { PermissionChip, type PermissionKind } from '@/components/app/PermissionChip'
import { EmptyState } from '@/components/app/EmptyState'
import {
  Users,
  UserPlus,
  ShieldCheck,
  ShieldAlert,
  ArrowLeft,
  Search,
  CheckCircle2,
  Lock,
  Copy,
  Check,
  Trash2,
  SlidersHorizontal,
  Mail,
  Loader2,
  BookOpen,
  Building2,
} from 'lucide-react'
import type { Cashbook } from '@/App'
import { formatRelativeDate } from '@/lib/format'
import { cn } from '@/lib/utils'

export type BusinessInfo = { id: string; name: string }
export type Member = {
  userId: string
  fullName: string
  email: string
  status: string
  permissions: string[]
}
export type Invitation = {
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
  { id: 1, code: 'BUSINESS_VIEW', label: 'View business', kind: 'view' as PermissionKind, description: 'Access workspace overview & dashboards' },
  { id: 2, code: 'CASHBOOK_LIST_VIEW', label: 'View cashbooks', kind: 'view' as PermissionKind, description: 'Browse all cashbooks' },
  { id: 3, code: 'BUSINESS_UPDATE', label: 'Edit business', kind: 'edit' as PermissionKind, description: 'Update business name and settings' },
  { id: 4, code: 'CASHBOOK_CREATE', label: 'Create cashbooks', kind: 'add' as PermissionKind, description: 'Create and initialize cashbooks' },
  { id: 5, code: 'CASHBOOK_UPDATE', label: 'Edit cashbooks', kind: 'edit' as PermissionKind, description: 'Rename and reconfigure cashbooks' },
  { id: 6, code: 'CASHBOOK_DELETE', label: 'Delete cashbooks', kind: 'delete' as PermissionKind, description: 'Archive or permanently delete cashbooks' },
]

const cashbookPermissions = [
  { id: 1, code: 'CASHBOOK_VIEW', label: 'View cashbook', kind: 'view' as PermissionKind, description: 'See cashbook in workspace list' },
  { id: 2, code: 'TRANSACTION_VIEW', label: 'View transactions', kind: 'view' as PermissionKind, description: 'Inspect entries, amounts, attachments' },
  { id: 3, code: 'TRANSACTION_CREATE', label: 'Add transactions', kind: 'add' as PermissionKind, description: 'Record cash in/out and receipts' },
  { id: 4, code: 'TRANSACTION_UPDATE', label: 'Edit transactions', kind: 'edit' as PermissionKind, description: 'Modify remarks and dates' },
  { id: 5, code: 'TRANSACTION_DELETE', label: 'Delete transactions', kind: 'delete' as PermissionKind, description: 'Permanently remove transactions' },
]

async function request<T>(path: string, token: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${token}`)
  if (options.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'}${path}`, { ...options, headers })
  const body = (await response.json().catch(() => ({}))) as Envelope<T>
  if (!response.ok) throw new Error(body.message ?? body.Message ?? `Request failed (${response.status})`)
  return (body.data ?? body.Data ?? body) as T
}

interface MembersPageProps {
  token: string
  business: BusinessInfo
  books: Cashbook[]
  isOwner?: boolean
  currentUserId?: string
  onBack: () => void
  onToast: (msg: string) => void
}

export function MembersPage({
  token,
  business,
  books,
  isOwner = false,
  currentUserId,
  onBack,
  onToast,
}: MembersPageProps) {
  const [scope, setScope] = useState<'cashbook' | 'business'>('cashbook')
  const [cashbookId, setCashbookId] = useState(books[0]?.id ?? '')
  const [members, setMembers] = useState<Member[]>([])
  const [, setPending] = useState<Invitation[]>([])
  const [sent, setSent] = useState<Invitation[]>([])
  const [sentStatus, setSentStatus] = useState<string>('All')
  const [search, setSearch] = useState('')
  const [email, setEmail] = useState('')
  const [permissions, setPermissions] = useState<number[]>([1, 2, 3])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [confirmRemove, setConfirmRemove] = useState<Member | null>(null)
  const [editingMember, setEditingMember] = useState<Member | null>(null)
  const [editorSelected, setEditorSelected] = useState<number[]>([])
  const [savingPermissions, setSavingPermissions] = useState(false)
  const [invited, setInvited] = useState(false)
  const [copiedLink, setCopiedLink] = useState<string | null>(null)

  const activeCashbook = books.find((b) => b.id === cashbookId) || books[0]
  const currentOptions = scope === 'business' ? businessPermissions : cashbookPermissions

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
    try {
      const results = await Promise.allSettled([
        request<Member[]>(memberUrl, token),
        request<Invitation[]>('/api/Invitations/mypendingInvitations', token),
        isOwner
          ? request<Invitation[]>(
              `/api/Invitations/mysentInvitations${sentStatus && sentStatus !== 'All' ? `?status=${encodeURIComponent(sentStatus)}` : ''}`,
              token
            )
          : Promise.resolve([]),
      ])

      if (results[0].status === 'fulfilled') setMembers(results[0].value ?? [])
      else {
        setMembers([])
        setError(results[0].reason instanceof Error ? results[0].reason.message : 'Could not load members.')
      }

      if (results[1].status === 'fulfilled') setPending(results[1].value ?? [])
      if (results[2].status === 'fulfilled') setSent(results[2].value ?? [])
    } finally {
      setLoading(false)
    }
  }, [memberUrl, token, scope, cashbookId, isOwner, sentStatus])

  useEffect(() => {
    void reload()
  }, [reload])

  // Presets logic
  const handlePreset = (type: 'full' | 'add_edit' | 'add_only' | 'view_only') => {
    if (scope === 'cashbook') {
      if (type === 'full') setPermissions([1, 2, 3, 4, 5])
      else if (type === 'add_edit') setPermissions([1, 2, 3, 4])
      else if (type === 'add_only') setPermissions([1, 2, 3])
      else setPermissions([1, 2])
    } else {
      if (type === 'full') setPermissions([1, 2, 3, 4, 5, 6])
      else if (type === 'add_edit') setPermissions([1, 2, 4, 5])
      else if (type === 'add_only') setPermissions([1, 2, 4])
      else setPermissions([1, 2])
    }
  }

  // Handle invitation submission
  const handleInvite = async (e: FormEvent) => {
    e.preventDefault()
    if (!email.trim()) return
    setBusy(true)
    setError('')
    try {
      await request('/api/Invitations/invite', token, {
        method: 'POST',
        body: JSON.stringify({
          businessId: business.id,
          cashbookId: scope === 'cashbook' ? cashbookId : null,
          email: email.trim(),
          permissions,
        }),
      })
      setEmail('')
      setInvited(true)
      setTimeout(() => setInvited(false), 2000)
      onToast(`Invitation successfully sent to ${email}`)
      await reload()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send invitation.')
    } finally {
      setBusy(false)
    }
  }

  // Handle member removal
  const handleRemoveMember = async () => {
    if (!confirmRemove) return
    setBusy(true)
    const removeUrl =
      scope === 'business'
        ? `/api/MemberManagement/businesses/${business.id}/members/${confirmRemove.userId}`
        : `/api/MemberManagement/businesses/${business.id}/cashbooks/${cashbookId}/members/${confirmRemove.userId}`
    try {
      await request(removeUrl, token, { method: 'DELETE' })
      onToast(`Removed ${confirmRemove.fullName} from ${scope}`)
      setConfirmRemove(null)
      await reload()
    } catch (err) {
      onToast(err instanceof Error ? err.message : 'Could not remove member.')
    } finally {
      setBusy(false)
    }
  }

  // Save updated permissions from sheet
  const handleSaveMemberPermissions = async () => {
    if (!editingMember) return
    setSavingPermissions(true)
    const updateUrl =
      scope === 'business'
        ? `/api/MemberManagement/businesses/${business.id}/members/${editingMember.userId}/permissions`
        : `/api/MemberManagement/businesses/${business.id}/cashbooks/${cashbookId}/members/${editingMember.userId}/permissions`
    try {
      await request(updateUrl, token, {
        method: 'PUT',
        body: JSON.stringify({ permissions: editorSelected }),
      })
      onToast(`Permissions updated for ${editingMember.fullName}`)
      setEditingMember(null)
      await reload()
    } catch (err) {
      onToast(err instanceof Error ? err.message : 'Could not update permissions.')
    } finally {
      setSavingPermissions(false)
    }
  }

  const filteredMembers = useMemo(() => {
    return members.filter((m) => {
      if (!search.trim()) return true
      const q = search.toLowerCase()
      return (
        m.fullName.toLowerCase().includes(q) ||
        m.email.toLowerCase().includes(q)
      )
    })
  }, [members, search])

  return (
    <div className="space-y-6 pb-12">
      {/* 1. Page Header with Scope Tabs */}
      <PageHeader
        title="Members & Access"
        description="Owner-controlled role assignments, invitations and granular operation permissions."
        breadcrumb={
          <Button
            variant="ghost"
            size="sm"
            onClick={onBack}
            className="-ml-2 h-7 gap-1 text-xs text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="size-3.5" />
            <span>Back to Overview</span>
          </Button>
        }
        actions={
          <div className="flex items-center gap-3">
            {scope === 'cashbook' && books.length > 1 && (
              <Select value={cashbookId} onValueChange={(val) => val && setCashbookId(val)}>
                <SelectTrigger className="h-8 text-xs w-44">
                  <BookOpen className="size-3.5 mr-1 text-muted-foreground" />
                  <SelectValue placeholder="Select cashbook" />
                </SelectTrigger>
                <SelectContent>
                  {books.map((b) => (
                    <SelectItem key={b.id} value={b.id} className="text-xs">
                      {b.cashBookName ?? b.name ?? 'Cashbook'}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}

            <MotionTabs
              value={scope}
              onValueChange={(v) => setScope(v as 'cashbook' | 'business')}
            >
              <MotionTabsList className="h-8">
                <MotionTabsTrigger value="cashbook" className="text-xs px-3">
                  Cashbook Scope
                </MotionTabsTrigger>
                <MotionTabsTrigger value="business" className="text-xs px-3">
                  Entire Business
                </MotionTabsTrigger>
              </MotionTabsList>
            </MotionTabs>
          </div>
        }
      />

      {error && (
        <Alert variant="destructive">
          <ShieldAlert className="size-4" />
          <AlertTitle>Access Error</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* 2. Two-column Layout on lg */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Left Column (col-span-8): Directory & Invitations */}
        <div className="lg:col-span-8 space-y-6">
          {/* Member Directory Card */}
          <Card className="border-border/80 shadow-xs">
            <CardHeader className="p-4 sm:p-6 pb-3 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between space-y-0">
              <div>
                <CardTitle className="text-base font-semibold">Active Members</CardTitle>
                <CardDescription className="text-xs text-muted-foreground">
                  Collaborators with assigned permissions for{' '}
                  <strong className="text-foreground">
                    {scope === 'cashbook'
                      ? activeCashbook?.cashBookName ?? activeCashbook?.name ?? 'Cashbook'
                      : business.name}
                  </strong>
                </CardDescription>
              </div>
              <div className="relative w-full sm:w-56">
                <Search className="absolute left-2.5 top-2.5 size-3.5 text-muted-foreground" />
                <Input
                  placeholder="Search members..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="pl-8 h-8 text-xs"
                />
              </div>
            </CardHeader>

            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow className="border-b hover:bg-transparent">
                    <TableHead className="text-xs font-semibold">Member</TableHead>
                    <TableHead className="text-xs font-semibold">Role</TableHead>
                    <TableHead className="text-xs font-semibold">Permissions</TableHead>
                    <TableHead className="text-right text-xs font-semibold">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {loading ? (
                    Array.from({ length: 3 }).map((_, i) => (
                      <TableRow key={i} className="h-14">
                        <TableCell colSpan={4} className="text-center py-4">
                          <Loader2 className="size-4 animate-spin mx-auto text-muted-foreground" />
                        </TableCell>
                      </TableRow>
                    ))
                  ) : filteredMembers.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={4} className="h-32 text-center p-0">
                        <EmptyState
                          icon={Users}
                          title="No members found"
                          description="Invite team members to collaborate on this cashbook."
                          className="border-0 shadow-none"
                        />
                      </TableCell>
                    </TableRow>
                  ) : (
                    filteredMembers.map((m) => {
                      const isCurrentUser = m.userId === currentUserId
                      const initials = m.fullName.slice(0, 2).toUpperCase()

                      return (
                        <TableRow key={m.userId} className="h-14 hover:bg-muted/40">
                          <TableCell className="py-2.5">
                            <div className="flex items-center gap-2.5">
                              <Avatar className="size-8">
                                <AvatarFallback className="text-xs bg-primary/10 text-primary font-semibold">
                                  {initials}
                                </AvatarFallback>
                              </Avatar>
                              <div>
                                <div className="flex items-center gap-1.5">
                                  <span className="font-semibold text-xs text-foreground">
                                    {m.fullName}
                                  </span>
                                  {isCurrentUser && (
                                    <Badge variant="secondary" className="px-1 py-0 text-[10px]">
                                      You
                                    </Badge>
                                  )}
                                </div>
                                <p className="text-[11px] text-muted-foreground">{m.email}</p>
                              </div>
                            </div>
                          </TableCell>

                          <TableCell className="py-2.5">
                            <RoleBadge role={m.status === 'Owner' ? 'owner' : 'member'} size="sm" />
                          </TableCell>

                          <TableCell className="py-2.5">
                            <div className="flex flex-wrap gap-1 max-w-xs">
                              {m.permissions?.length ? (
                                m.permissions.map((p) => {
                                  const kind: PermissionKind = p.includes('CREATE')
                                    ? 'add'
                                    : p.includes('UPDATE')
                                    ? 'edit'
                                    : p.includes('DELETE')
                                    ? 'delete'
                                    : 'view'
                                  return (
                                    <PermissionChip key={p} kind={kind} size="sm" />
                                  )
                                })
                              ) : (
                                <span className="text-[11px] text-muted-foreground italic">
                                  Default access
                                </span>
                              )}
                            </div>
                          </TableCell>

                          <TableCell className="py-2.5 text-right">
                            {isOwner && !isCurrentUser ? (
                              <div className="flex items-center justify-end gap-1">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="h-7 text-xs px-2 gap-1"
                                  onClick={() => {
                                    setEditingMember(m)
                                    setEditorSelected(
                                      currentOptions
                                        .filter(
                                          (opt) =>
                                            m.permissions?.includes(opt.code) ||
                                            m.permissions?.includes(String(opt.id))
                                        )
                                        .map((opt) => opt.id)
                                    )
                                  }}
                                >
                                  <SlidersHorizontal className="size-3" />
                                  <span>Edit</span>
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  className="size-7 text-destructive hover:bg-destructive/10 hover:text-destructive"
                                  onClick={() => setConfirmRemove(m)}
                                  title="Remove member"
                                >
                                  <Trash2 className="size-3.5" />
                                </Button>
                              </div>
                            ) : (
                              <Badge
                                variant="outline"
                                className="text-[10px] text-muted-foreground border-border/80"
                              >
                                {isCurrentUser ? 'Your account' : 'Owner'}
                              </Badge>
                            )}
                          </TableCell>
                        </TableRow>
                      )
                    })
                  )}
                </TableBody>
              </Table>
            </CardContent>
          </Card>

          {/* Capabilities Matrix Card (for Invited Members) */}
          {!isOwner && (
            <Card className="border-border/80 shadow-xs">
              <CardHeader className="p-4 sm:p-6 pb-3">
                <div className="flex items-center gap-2">
                  <ShieldCheck className="size-4 text-primary" />
                  <CardTitle className="text-base font-semibold">Your Cashbook Access</CardTitle>
                </div>
                <CardDescription className="text-xs text-muted-foreground">
                  Capabilities verified and authorized by the cashbook owner.
                </CardDescription>
              </CardHeader>
              <CardContent className="p-4 sm:p-6 pt-0 space-y-2">
                {cashbookPermissions.map((item) => {
                  const allowed =
                    members
                      .find((m) => m.userId === currentUserId)
                      ?.permissions?.includes(item.code) ?? false

                  return (
                    <div
                      key={item.id}
                      className="flex items-center justify-between p-3 rounded-lg border border-border/80 bg-muted/20"
                    >
                      <div className="space-y-0.5">
                        <p className="text-xs font-semibold">{item.label}</p>
                        <p className="text-[11px] text-muted-foreground">{item.description}</p>
                      </div>
                      {allowed ? (
                        <Badge
                          variant="outline"
                          className="border-inflow/30 bg-inflow-muted/50 text-inflow gap-1 text-xs"
                        >
                          <CheckCircle2 className="size-3" />
                          <span>Allowed</span>
                        </Badge>
                      ) : (
                        <Badge
                          variant="outline"
                          className="text-muted-foreground border-border/80 gap-1 text-xs"
                        >
                          <Lock className="size-3" />
                          <span>Restricted</span>
                        </Badge>
                      )}
                    </div>
                  )
                })}
                <div className="rounded-lg border border-border/80 bg-muted/40 p-3 mt-3 text-xs text-muted-foreground">
                  <strong>Need higher privileges?</strong> Ask the cashbook owner to elevate your
                  permissions from their Members & Access console.
                </div>
              </CardContent>
            </Card>
          )}

          {/* Invitations Sent (Owner Only) */}
          {isOwner && (
            <Card className="border-border/80 shadow-xs">
              <CardHeader className="p-4 sm:p-6 pb-3 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between space-y-0">
                <div>
                  <CardTitle className="text-base font-semibold">Sent Invitations</CardTitle>
                  <CardDescription className="text-xs text-muted-foreground">
                    Pending and historical collaboration invitations sent by you.
                  </CardDescription>
                </div>
                <MotionToggleGroup
                  value={sentStatus}
                  onValueChange={setSentStatus}
                  items={[
                    { value: 'All', label: 'All' },
                    { value: 'Pending', label: 'Pending' },
                    { value: 'Accepted', label: 'Accepted' },
                  ]}
                  size="sm"
                />
              </CardHeader>
              <CardContent className="p-0">
                <Table>
                  <TableHeader>
                    <TableRow className="border-b hover:bg-transparent">
                      <TableHead className="text-xs font-semibold">Invited Email</TableHead>
                      <TableHead className="text-xs font-semibold">Scope</TableHead>
                      <TableHead className="text-xs font-semibold">Status</TableHead>
                      <TableHead className="text-xs font-semibold">Sent Date</TableHead>
                      <TableHead className="text-right text-xs font-semibold">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {sent.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={5} className="h-24 text-center text-xs text-muted-foreground">
                          No invitations found for status &ldquo;{sentStatus}&rdquo;.
                        </TableCell>
                      </TableRow>
                    ) : (
                      sent.map((inv) => (
                        <TableRow key={inv.id} className="h-12 hover:bg-muted/40 text-xs">
                          <TableCell className="font-medium">{inv.invitedEmail}</TableCell>
                          <TableCell className="text-muted-foreground">
                            {inv.cashbookName ? `Cashbook: ${inv.cashbookName}` : 'Entire Business'}
                          </TableCell>
                          <TableCell>
                            <Badge
                              variant="outline"
                              className={cn(
                                'text-[10px] px-1.5 py-0 font-medium',
                                inv.status.toLowerCase() === 'accepted'
                                  ? 'border-inflow/30 bg-inflow-muted/50 text-inflow'
                                  : inv.status.toLowerCase() === 'pending'
                                  ? 'border-amber-500/30 bg-amber-500/10 text-amber-600'
                                  : 'text-muted-foreground'
                              )}
                            >
                              {inv.status}
                            </Badge>
                          </TableCell>
                          <TableCell className="text-muted-foreground">
                            {formatRelativeDate(inv.createdOn)}
                          </TableCell>
                          <TableCell className="text-right">
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-7 text-xs px-2 gap-1"
                              onClick={() => {
                                navigator.clipboard.writeText(
                                  `${window.location.origin}/login?invite=${inv.id}`
                                )
                                setCopiedLink(inv.id)
                                onToast('Invitation link copied!')
                                setTimeout(() => setCopiedLink(null), 2000)
                              }}
                            >
                              {copiedLink === inv.id ? (
                                <Check className="size-3 text-inflow" />
                              ) : (
                                <Copy className="size-3" />
                              )}
                              <span>Copy Link</span>
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Right Column (col-span-4): Invite Panel (Sticky) */}
        <div className="lg:col-span-4 lg:sticky lg:top-20 space-y-4">
          {isOwner ? (
            <Card className="border-border/80 shadow-xs">
              <CardHeader className="p-5 pb-3">
                <div className="flex items-center gap-2">
                  <div className="flex size-7 items-center justify-center rounded-lg bg-primary/10 text-primary">
                    <UserPlus className="size-4" />
                  </div>
                  <CardTitle className="text-base font-semibold">Invite Teammate</CardTitle>
                </div>
                <CardDescription className="text-xs text-muted-foreground">
                  Grant granular operation access to collaborator.
                </CardDescription>
              </CardHeader>

              <CardContent className="p-5 pt-0 space-y-4">
                <form onSubmit={handleInvite} className="space-y-4">
                  {/* Email */}
                  <div className="space-y-1.5">
                    <Label htmlFor="inviteEmail" className="text-xs font-medium">
                      Teammate Email Address
                    </Label>
                    <div className="relative">
                      <Mail className="absolute left-2.5 top-2.5 size-3.5 text-muted-foreground" />
                      <Input
                        id="inviteEmail"
                        type="email"
                        required
                        placeholder="colleague@company.com"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        className="pl-8 h-8 text-xs"
                      />
                    </div>
                  </div>

                  {/* Scope info */}
                  <div className="rounded-lg border border-border/80 bg-muted/30 p-2.5 text-xs space-y-1">
                    <span className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
                      Target Scope
                    </span>
                    <p className="font-semibold text-foreground flex items-center gap-1.5">
                      {scope === 'cashbook' ? (
                        <>
                          <BookOpen className="size-3.5 text-primary" />
                          <span>{activeCashbook?.cashBookName ?? activeCashbook?.name ?? 'Cashbook'}</span>
                        </>
                      ) : (
                        <>
                          <Building2 className="size-3.5 text-primary" />
                          <span>Entire {business.name}</span>
                        </>
                      )}
                    </p>
                  </div>

                  {/* Presets */}
                  <div className="space-y-1.5">
                    <Label className="text-xs font-medium">Permission Presets</Label>
                    <div className="grid grid-cols-2 gap-1.5">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-7 text-xs font-normal"
                        onClick={() => handlePreset('full')}
                      >
                        Full Access
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-7 text-xs font-normal"
                        onClick={() => handlePreset('add_edit')}
                      >
                        Add & Edit
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-7 text-xs font-normal"
                        onClick={() => handlePreset('add_only')}
                      >
                        Add Only
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-7 text-xs font-normal"
                        onClick={() => handlePreset('view_only')}
                      >
                        View Only
                      </Button>
                    </div>
                  </div>

                  {/* Permission checklist */}
                  <div className="space-y-2 border-t pt-3">
                    <Label className="text-xs font-medium">Granular Permissions</Label>
                    <div className="space-y-2">
                      {currentOptions.map((opt) => {
                        const checked = permissions.includes(opt.id)
                        return (
                          <label
                            key={opt.id}
                            className={cn(
                              'flex items-start gap-2.5 p-2 rounded-lg border border-border/80 text-xs cursor-pointer transition-colors hover:bg-muted/40',
                              checked && 'bg-primary/5 border-primary/30'
                            )}
                          >
                            <Checkbox
                              checked={checked}
                              onCheckedChange={(val) => {
                                setPermissions((prev) =>
                                  val ? [...prev, opt.id] : prev.filter((x) => x !== opt.id)
                                );
                              }}
                              className="mt-0.5"
                            />
                            <div className="space-y-0.5 flex-1">
                              <div className="flex items-center justify-between">
                                <span className="font-medium text-foreground">{opt.label}</span>
                                <PermissionChip kind={opt.kind} size="sm" />
                              </div>
                              <p className="text-[11px] text-muted-foreground">{opt.description}</p>
                            </div>
                          </label>
                        )
                      })}
                    </div>
                  </div>

                  {/* Security Alert Notice */}
                  <Alert className="py-2.5 px-3 bg-muted/40 border-border/80">
                    <ShieldCheck className="size-3.5 text-primary" />
                    <AlertTitle className="text-xs font-semibold">Owner-Controlled Permissions</AlertTitle>
                    <AlertDescription className="text-[11px] text-muted-foreground leading-relaxed">
                      Only the Cashbook Owner can assign or revoke permissions. Members cannot elevate their privileges.
                    </AlertDescription>
                  </Alert>

                  <Button type="submit" disabled={busy || !email.trim()} loading={busy} success={invited} successLabel="Invitation Sent!" className="w-full h-8 text-xs">
                    <UserPlus className="size-3.5 mr-1.5" />
                    <span>Send Invitation</span>
                  </Button>
                </form>
              </CardContent>
            </Card>
          ) : (
            <Card className="border-border/80 shadow-xs">
              <CardHeader className="p-5">
                <CardTitle className="text-base font-semibold">Permission Security</CardTitle>
                <CardDescription className="text-xs text-muted-foreground leading-relaxed">
                  You are viewing this workspace as an invited collaborator. Your actions are governed by permissions assigned by the workspace owner.
                </CardDescription>
              </CardHeader>
            </Card>
          )}
        </div>
      </div>

      {/* Permission Edit Sheet (Owner only) */}
      <Sheet open={Boolean(editingMember)} onOpenChange={(open) => !open && setEditingMember(null)}>
        <SheetContent className="sm:max-w-md w-full overflow-y-auto">
          <SheetHeader className="pb-4">
            <SheetTitle className="text-base font-semibold">
              Edit Permissions: {editingMember?.fullName}
            </SheetTitle>
          </SheetHeader>

          <div className="space-y-4 py-2">
            <p className="text-xs text-muted-foreground">
              Configure operations that <strong className="text-foreground">{editingMember?.email}</strong> is allowed to perform.
            </p>

            {/* Quick Presets inside editor */}
            <div className="grid grid-cols-2 gap-1.5">
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-7 text-xs font-normal"
                onClick={() => setEditorSelected(scope === 'cashbook' ? [1, 2, 3, 4, 5] : [1, 2, 3, 4, 5, 6])}
              >
                Full Access
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-7 text-xs font-normal"
                onClick={() => setEditorSelected(scope === 'cashbook' ? [1, 2, 3, 4] : [1, 2, 4, 5])}
              >
                Add & Edit
              </Button>
            </div>

            <div className="space-y-2 pt-2 border-t">
              {currentOptions.map((opt) => {
                const isChecked = editorSelected.includes(opt.id)
                return (
                  <label
                    key={opt.id}
                    className={cn(
                      'flex items-start gap-2.5 p-3 rounded-lg border border-border/80 text-xs cursor-pointer transition-colors hover:bg-muted/40',
                      isChecked && 'bg-primary/5 border-primary/30'
                    )}
                  >
                    <Checkbox
                      checked={isChecked}
                      onCheckedChange={(val) => {
                        setEditorSelected((prev) =>
                          val ? [...prev, opt.id] : prev.filter((x) => x !== opt.id)
                        )
                      }}
                      className="mt-0.5"
                    />
                    <div className="space-y-0.5 flex-1">
                      <div className="flex items-center justify-between">
                        <span className="font-medium text-foreground">{opt.label}</span>
                        <PermissionChip kind={opt.kind} size="sm" />
                      </div>
                      <p className="text-[11px] text-muted-foreground">{opt.description}</p>
                    </div>
                  </label>
                )
              })}
            </div>
          </div>

          <SheetFooter className="pt-4 border-t">
            <Button variant="ghost" onClick={() => setEditingMember(null)} disabled={savingPermissions}>
              Cancel
            </Button>
            <Button onClick={handleSaveMemberPermissions} disabled={savingPermissions}>
              {savingPermissions ? <Loader2 className="size-3.5 animate-spin mr-1.5" /> : null}
              Save Changes
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      {/* Remove Member Confirmation Dialog */}
      <AlertDialog
        open={Boolean(confirmRemove)}
        onOpenChange={(open) => !open && setConfirmRemove(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove Member Access?</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to revoke access for{' '}
              <strong>{confirmRemove?.fullName}</strong> ({confirmRemove?.email})? They will no longer be able to view or manage entries in this {scope}.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={handleRemoveMember}
            >
              Revoke Access
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
