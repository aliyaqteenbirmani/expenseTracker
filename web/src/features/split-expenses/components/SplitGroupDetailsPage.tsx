import { useState, useEffect, useCallback } from 'react'
import { motion } from 'motion/react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/app/EmptyState'
import { StatCard } from '@/components/app/StatCard'
import {
  MotionTabs,
  MotionTabsList,
  MotionTabsTrigger,
  MotionTabsContent,
} from '@/components/motion/motion-tabs'
import { useMotionSafe } from '@/hooks/use-motion-safe'
import {
  splitStaggerContainer,
  splitCardVariants,
} from './SplitPageTransition'
import { SplitGroupDetailsSkeleton } from './SplitGroupDetailsSkeleton'
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
import { splitExpensesApi } from '../splitExpensesApi'
import { AddExpenseDialog } from './AddExpenseDialog'
import { ExpenseDetailDialog } from './ExpenseDetailDialog'
import { SettleUpDialog } from './SettleUpDialog'
import { InviteMemberDialog } from './InviteMemberDialog'
import { toast } from 'sonner'
import { getMemberInitials } from './MemberSelector'
import { SplitNotificationBell } from './SplitNotificationBell'
import type {
  SplitGroupDetailDto,
  SplitExpenseListItemDto,
  SplitExpenseDetailDto,
  MemberBalanceDto,
  SettleUpOverviewDto,
  SplitSettlementDto,
  SplitGroupActivityDto,
  SplitGroupInvitationDto,
  SuggestedSettlementDto,
} from '../types'
import {
  ArrowLeft,
  Plus,
  Receipt,
  Users,
  Scale,
  HandCoins,
  History,
  Search,
  ArrowUpRight,
  ArrowDownLeft,
  CheckCircle2,
  Trash2,
  Mail,
  UserX,
  Crown,
  Send,
  Loader2,
} from 'lucide-react'
import { format } from 'date-fns'

interface SplitGroupDetailsPageProps {
  groupId: string
  currentUserId?: string
  token?: string
  onBack: () => void
  onGroupDeleted?: () => void
}

export function SplitGroupDetailsPage({
  groupId,
  currentUserId,
  token,
  onBack,
  onGroupDeleted,
}: SplitGroupDetailsPageProps) {
  const { reduce, fadeOnly } = useMotionSafe()
  const [activeTab, setActiveTab] = useState('overview')
  const [loading, setLoading] = useState(true)
  const [group, setGroup] = useState<SplitGroupDetailDto | null>(null)

  // Sub-data states
  const [expenses, setExpenses] = useState<SplitExpenseListItemDto[]>([])
  const [loadingExpenses, setLoadingExpenses] = useState(false)
  const [expenseSearch, setExpenseSearch] = useState('')

  const [balances, setBalances] = useState<MemberBalanceDto[]>([])
  const [settleOverview, setSettleOverview] = useState<SettleUpOverviewDto | null>(null)
  const [settlements, setSettlements] = useState<SplitSettlementDto[]>([])
  const [activities, setActivities] = useState<SplitGroupActivityDto[]>([])
  const [invitations, setInvitations] = useState<SplitGroupInvitationDto[]>([])

  // Dialog controls
  const [addExpenseOpen, setAddExpenseOpen] = useState(false)
  const [selectedExpense, setSelectedExpense] = useState<SplitExpenseDetailDto | null>(null)
  const [settleUpOpen, setSettleUpOpen] = useState(false)
  const [activeSuggestion, setActiveSuggestion] = useState<SuggestedSettlementDto | null>(null)
  const [inviteOpen, setInviteOpen] = useState(false)
  const [deleteGroupOpen, setDeleteGroupOpen] = useState(false)
  const [deletingGroup, setDeletingGroup] = useState(false)

  // Member removal state
  const [memberToRemove, setMemberToRemove] = useState<string | null>(null)
  const [removingMember, setRemovingMember] = useState(false)

  const isOwner = group?.ownerId === currentUserId

  const loadGroupDetails = useCallback(async () => {
    try {
      setLoading(true)
      const data = await splitExpensesApi.getGroupDetails(groupId, token)
      setGroup(data)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to load group details'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }, [groupId, token])

  const loadExpenses = useCallback(async () => {
    try {
      setLoadingExpenses(true)
      const res = await splitExpensesApi.getExpenses(groupId, { search: expenseSearch }, token)
      setExpenses(res.items || [])
    } catch {
      // Handled silently
    } finally {
      setLoadingExpenses(false)
    }
  }, [groupId, expenseSearch, token])

  const loadBalancesAndSettlements = useCallback(async () => {
    try {
      const [balList, settleData, historyList] = await Promise.all([
        splitExpensesApi.getGroupBalances(groupId, token),
        splitExpensesApi.getSettleUpSuggestions(groupId, token),
        splitExpensesApi.getGroupSettlements(groupId, token),
      ])
      setBalances(balList || [])
      setSettleOverview(settleData || null)
      setSettlements(historyList || [])
    } catch {
      // Handled silently
    }
  }, [groupId, token])

  const loadActivities = useCallback(async () => {
    try {
      const res = await splitExpensesApi.getGroupActivities(groupId, { pageSize: 50 }, token)
      setActivities(res.items || [])
    } catch {
      // Handled silently
    }
  }, [groupId, token])

  const loadInvitations = useCallback(async () => {
    try {
      const data = await splitExpensesApi.getGroupInvitations(groupId, token)
      setInvitations(data || [])
    } catch {
      // Handled silently
    }
  }, [groupId, token])

  // Initial load
  useEffect(() => {
    loadGroupDetails()
    loadExpenses()
    loadBalancesAndSettlements()
    loadActivities()
    loadInvitations()
  }, [loadGroupDetails, loadExpenses, loadBalancesAndSettlements, loadActivities, loadInvitations])

  // Refetch when switching tabs
  const handleTabChange = (tab: string) => {
    setActiveTab(tab)
    if (tab === 'expenses') loadExpenses()
    if (tab === 'balances' || tab === 'settle') loadBalancesAndSettlements()
    if (tab === 'activity') loadActivities()
    if (tab === 'members') loadInvitations()
  }

  // Open expense details
  const handleOpenExpense = async (expenseId: string) => {
    try {
      const detail = await splitExpensesApi.getExpenseDetails(groupId, expenseId, token)
      setSelectedExpense(detail)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to load expense details'
      toast.error(msg)
    }
  }

  // Member removal
  const handleConfirmRemoveMember = async () => {
    if (!memberToRemove) return
    try {
      setRemovingMember(true)
      await splitExpensesApi.removeMember(groupId, memberToRemove, token)
      toast.success('Member removed from group')
      setMemberToRemove(null)
      loadGroupDetails()
      loadBalancesAndSettlements()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to remove member'
      toast.error(msg)
    } finally {
      setRemovingMember(false)
    }
  }

  // Invitation revocation
  const handleRevokeInvitation = async (invitationId: string) => {
    try {
      await splitExpensesApi.revokeInvitation(groupId, invitationId, token)
      toast.success('Invitation revoked')
      loadInvitations()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to revoke invitation'
      toast.error(msg)
    }
  }

  // Invitation resending
  const [resendingInviteId, setResendingInviteId] = useState<string | null>(null)
  const handleResendInvitation = async (invitationId: string) => {
    try {
      setResendingInviteId(invitationId)
      await splitExpensesApi.resendInvitation(groupId, invitationId, token)
      toast.success('Invitation resent successfully')
      loadInvitations()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to resend invitation'
      toast.error(msg)
    } finally {
      setResendingInviteId(null)
    }
  }

  // Group deletion
  const handleDeleteGroup = async () => {
    try {
      setDeletingGroup(true)
      await splitExpensesApi.deleteGroup(groupId, token)
      toast.success('Group deleted successfully')
      setDeleteGroupOpen(false)
      onGroupDeleted ? onGroupDeleted() : onBack()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to delete group'
      toast.error(msg)
    } finally {
      setDeletingGroup(false)
    }
  }

  if (loading && !group) {
    return <SplitGroupDetailsSkeleton />
  }

  if (!group) {
    return (
      <EmptyState
        title="Group Not Found"
        description="The requested split group could not be found or you do not have permission to view it."
        action={
          <Button onClick={onBack} variant="outline" className="gap-2">
            <ArrowLeft className="size-4" />
            Back to Split Expenses
          </Button>
        }
      />
    )
  }

  const myBalance = group.currentUserBalance
  const isSettled = Math.abs(myBalance) < 0.005
  const isOwed = myBalance > 0.005
  const owes = myBalance < -0.005

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b pb-4">
        <div className="flex items-center gap-3">
          <Button
            variant="ghost"
            size="icon"
            onClick={onBack}
            className="size-9 rounded-lg"
            aria-label="Back to dashboard"
          >
            <ArrowLeft className="size-4" />
          </Button>
          <div className="flex size-11 items-center justify-center rounded-xl bg-muted/60 text-2xl shadow-xs border">
            {group.icon || '👥'}
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl md:text-2xl font-bold tracking-tight text-foreground">
                {group.name}
              </h1>
              {isOwner && (
                <Badge variant="outline" className="text-[10px] px-1.5 py-0 h-5 border-primary/30 text-primary gap-1">
                  <Crown className="size-2.5" />
                  Owner
                </Badge>
              )}
            </div>
            <p className="text-xs text-muted-foreground">
              {group.memberCount} members · Owner: {group.ownerName}
              {group.description && ` · ${group.description}`}
            </p>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-wrap items-center gap-2">
          <SplitNotificationBell token={token} onInvitationProcessed={loadGroupDetails} />
          <Button
            onClick={() => setAddExpenseOpen(true)}
            size="sm"
            className="gap-1.5 shadow-xs"
          >
            <Plus className="size-4" />
            Add Expense
          </Button>
          <Button
            onClick={() => {
              setActiveSuggestion(null)
              setSettleUpOpen(true)
            }}
            variant="outline"
            size="sm"
            className="gap-1.5 shadow-xs"
          >
            <HandCoins className="size-4" />
            Settle Up
          </Button>
          {isOwner && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setDeleteGroupOpen(true)}
              className="text-destructive hover:bg-destructive/10"
              aria-label="Delete group"
            >
              <Trash2 className="size-4" />
            </Button>
          )}
        </div>
      </div>

      {/* Navigation Tabs */}
      <MotionTabs value={activeTab} onValueChange={handleTabChange} variant="underline">
        <MotionTabsList className="w-full justify-start border-b rounded-none p-0 bg-transparent gap-4">
          <MotionTabsTrigger value="overview" className="pb-2.5">
            Overview
          </MotionTabsTrigger>
          <MotionTabsTrigger value="expenses" className="pb-2.5">
            Expenses ({expenses.length})
          </MotionTabsTrigger>
          <MotionTabsTrigger value="balances" className="pb-2.5">
            Balances ({balances.length})
          </MotionTabsTrigger>
          <MotionTabsTrigger value="settle" className="pb-2.5">
            Settle Up
          </MotionTabsTrigger>
          <MotionTabsTrigger value="members" className="pb-2.5">
            Members ({group.members?.length || 0})
          </MotionTabsTrigger>
          <MotionTabsTrigger value="activity" className="pb-2.5">
            Activity
          </MotionTabsTrigger>
        </MotionTabsList>

        {/* 1. OVERVIEW TAB */}
        <MotionTabsContent value="overview" className="space-y-6 pt-4">
          <motion.div
            variants={reduce ? undefined : splitStaggerContainer}
            initial="hidden"
            animate="show"
            className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4"
          >
            <motion.div variants={reduce ? fadeOnly : splitCardVariants}>
              <StatCard
                label="Total Group Expenses"
                value={group.totalExpenses}
                formatFn={(val) => `Rs. ${val.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`}
                icon={Receipt}
                trend="up"
                delta="Recorded ledger"
              />
            </motion.div>
            <motion.div variants={reduce ? fadeOnly : splitCardVariants}>
              <StatCard
                label="Your Net Balance"
                value={myBalance}
                formatFn={(val) =>
                  isSettled
                    ? 'Settled up'
                    : isOwed
                    ? `+Rs. ${Math.abs(val).toFixed(2)}`
                    : `-Rs. ${Math.abs(val).toFixed(2)}`
                }
                icon={Scale}
                trend={isOwed ? 'up' : owes ? 'down' : 'neutral'}
                delta={isOwed ? 'You get back' : owes ? 'You owe' : 'All clear'}
              />
            </motion.div>
            <motion.div variants={reduce ? fadeOnly : splitCardVariants}>
              <StatCard
                label="Total Settled"
                value={settleOverview?.totalOutstandingDebt || 0}
                formatFn={(val) => `Rs. ${val.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`}
                icon={HandCoins}
                trend="neutral"
                delta="Outstanding debt"
              />
            </motion.div>
            <motion.div variants={reduce ? fadeOnly : splitCardVariants}>
              <StatCard
                label="Group Members"
                value={group.memberCount}
                formatFn={(val) => `${val}`}
                icon={Users}
                trend="neutral"
                delta={`Owner: ${group.ownerName}`}
              />
            </motion.div>
          </motion.div>

          {/* Quick Settle Suggestions Banner */}
          {settleOverview?.suggestedTransactions && settleOverview.suggestedTransactions.length > 0 && (
            <Card className="rounded-xl border border-primary/20 bg-primary/5 p-4">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <div className="space-y-0.5">
                  <h4 className="font-semibold text-sm text-foreground flex items-center gap-1.5">
                    <HandCoins className="size-4 text-primary" />
                    Simplified Debt Suggestions
                  </h4>
                  <p className="text-xs text-muted-foreground">
                    Direct settlements calculated to minimize the number of payments.
                  </p>
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => handleTabChange('settle')}
                  className="shrink-0"
                >
                  View All Settlements
                </Button>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2.5 mt-3">
                {settleOverview.suggestedTransactions.slice(0, 3).map((item, idx) => (
                  <div
                    key={idx}
                    className="flex items-center justify-between p-2.5 rounded-lg border bg-background text-xs"
                  >
                    <div className="truncate mr-2">
                      <span className="font-semibold">{item.fromUserName}</span> pays{' '}
                      <span className="font-semibold">{item.toUserName}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="font-mono font-bold text-foreground">
                        Rs. {item.amount.toFixed(2)}
                      </span>
                      <Button
                        size="sm"
                        variant="ghost"
                        className="h-6 px-2 text-[10px]"
                        onClick={() => {
                          setActiveSuggestion(item)
                          setSettleUpOpen(true)
                        }}
                      >
                        Pay
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          )}

          {/* Recent Expenses List */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h3 className="text-base font-semibold text-foreground">Recent Expenses</h3>
              <Button variant="link" size="sm" onClick={() => handleTabChange('expenses')}>
                View all ({expenses.length})
              </Button>
            </div>

            {expenses.length === 0 ? (
              <EmptyState
                title="No Expenses Yet"
                description="Add the first expense for this group to start sharing costs equally."
                action={
                  <Button onClick={() => setAddExpenseOpen(true)} size="sm">
                    Add Expense
                  </Button>
                }
              />
            ) : (
              <div className="rounded-xl border divide-y overflow-hidden bg-card">
                {expenses.slice(0, 5).map((exp) => (
                  <div
                    key={exp.id}
                    onClick={() => handleOpenExpense(exp.id)}
                    className="flex items-center justify-between p-3.5 hover:bg-muted/40 cursor-pointer transition-colors"
                  >
                    <div className="flex items-center gap-3 truncate">
                      <div className="flex size-9 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                        <Receipt className="size-4" />
                      </div>
                      <div className="truncate">
                        <div className="font-semibold text-sm truncate">{exp.title}</div>
                        <div className="text-xs text-muted-foreground">
                          Paid by {exp.payerName} · {format(new Date(exp.expenseDate), 'MMM d')}
                        </div>
                      </div>
                    </div>
                    <div className="text-right">
                      <div className="font-mono font-semibold text-sm">
                        Rs. {exp.amount.toFixed(2)}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Your share: Rs. {exp.currentUserShare.toFixed(2)}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </MotionTabsContent>

        {/* 2. EXPENSES TAB */}
        <MotionTabsContent value="expenses" className="space-y-4 pt-4">
          <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
            <div className="relative flex-1 max-w-sm">
              <Search className="size-4 absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search expenses by title..."
                value={expenseSearch}
                onChange={(e) => setExpenseSearch(e.target.value)}
                className="pl-9"
              />
            </div>
            <Button onClick={() => setAddExpenseOpen(true)} size="sm" className="gap-1.5">
              <Plus className="size-4" />
              Add Expense
            </Button>
          </div>

          {loadingExpenses ? (
            <div className="space-y-2">
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
            </div>
          ) : expenses.length === 0 ? (
            <EmptyState
              title="No Expenses Found"
              description="No expenses match your search query."
              action={
                <Button onClick={() => setAddExpenseOpen(true)} size="sm">
                  Add Expense
                </Button>
              }
            />
          ) : (
            <motion.div
              variants={reduce ? undefined : splitStaggerContainer}
              initial="hidden"
              animate="show"
              className="rounded-xl border divide-y overflow-hidden bg-card"
            >
              {expenses.map((exp) => (
                <motion.div
                  key={exp.id}
                  variants={reduce ? fadeOnly : splitCardVariants}
                  onClick={() => handleOpenExpense(exp.id)}
                  className="flex items-center justify-between p-3.5 hover:bg-muted/40 cursor-pointer transition-colors"
                >
                  <div className="flex items-center gap-3 truncate">
                    <div className="flex size-9 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                      <Receipt className="size-4" />
                    </div>
                    <div className="truncate">
                      <div className="font-semibold text-sm truncate flex items-center gap-2">
                        {exp.title}
                        {exp.category && (
                          <Badge variant="secondary" className="font-normal text-[10px] h-4">
                            {exp.category}
                          </Badge>
                        )}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Paid by {exp.payerName} {exp.payerId === currentUserId && '(You)'} ·{' '}
                        {format(new Date(exp.expenseDate), 'MMM d, yyyy')} · {exp.participantCount} shared
                      </div>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="font-mono font-semibold text-sm">
                      Rs. {exp.amount.toFixed(2)}
                    </div>
                    <div className="text-xs text-muted-foreground font-mono">
                      Your share: Rs. {exp.currentUserShare.toFixed(2)}
                    </div>
                  </div>
                </motion.div>
              ))}
            </motion.div>
          )}
        </MotionTabsContent>

        {/* 3. BALANCES TAB */}
        <MotionTabsContent value="balances" className="space-y-4 pt-4">
          <div className="rounded-xl border bg-card p-4">
            <h3 className="text-base font-semibold mb-1">Member Balances</h3>
            <p className="text-xs text-muted-foreground mb-4">
              Net balance calculates total paid minus total share, plus net settlement payments.
            </p>

            <motion.div
              variants={reduce ? undefined : splitStaggerContainer}
              initial="hidden"
              animate="show"
              className="space-y-3"
            >
              {balances.map((m) => {
                const bal = m.netBalance
                const isZero = Math.abs(bal) < 0.005
                const isPositive = bal > 0.005
                const isNegative = bal < -0.005

                return (
                  <motion.div
                    key={m.userId}
                    variants={reduce ? fadeOnly : splitCardVariants}
                    className="flex flex-col sm:flex-row sm:items-center justify-between p-3.5 rounded-lg border bg-muted/20 gap-3"
                  >
                    <div className="flex items-center gap-3">
                      <div className="flex size-9 items-center justify-center rounded-full bg-primary/10 text-primary font-semibold text-xs">
                        {getMemberInitials(m.fullName || m.email)}
                      </div>
                      <div>
                        <div className="font-semibold text-sm flex items-center gap-2">
                          {m.fullName}
                          {m.userId === currentUserId && (
                            <Badge variant="outline" className="text-[10px] h-4">
                              You
                            </Badge>
                          )}
                          {!m.isActive && (
                            <Badge variant="secondary" className="text-[10px] h-4">
                              Inactive
                            </Badge>
                          )}
                        </div>
                        <div className="text-xs text-muted-foreground">
                          Paid: Rs. {m.totalPaid.toFixed(2)} · Share: Rs. {m.totalShare.toFixed(2)}
                        </div>
                      </div>
                    </div>

                    <div className="text-right sm:text-right flex sm:flex-col items-center sm:items-end justify-between sm:justify-start">
                      <span className="text-[10px] uppercase tracking-wider text-muted-foreground sm:block">
                        Net Status
                      </span>
                      {isZero && (
                        <span className="text-xs font-medium text-muted-foreground flex items-center gap-1">
                          <CheckCircle2 className="size-3.5" />
                          Settled up
                        </span>
                      )}
                      {isPositive && (
                        <span className="text-sm font-bold font-mono text-inflow flex items-center gap-1">
                          <ArrowUpRight className="size-4" />
                          Gets back Rs. {Math.abs(bal).toFixed(2)}
                        </span>
                      )}
                      {isNegative && (
                        <span className="text-sm font-bold font-mono text-outflow flex items-center gap-1">
                          <ArrowDownLeft className="size-4" />
                          Owes Rs. {Math.abs(bal).toFixed(2)}
                        </span>
                      )}
                    </div>
                  </motion.div>
                )
              })}
            </motion.div>
          </div>
        </MotionTabsContent>

        {/* 4. SETTLE UP TAB */}
        <MotionTabsContent value="settle" className="space-y-6 pt-4">
          {/* Suggested payments */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-base font-semibold">Suggested Payments</h3>
                <p className="text-xs text-muted-foreground">
                  Debt simplification engine calculates the fewest transactions to settle all debts.
                </p>
              </div>
              <Button
                onClick={() => {
                  setActiveSuggestion(null)
                  setSettleUpOpen(true)
                }}
                size="sm"
                className="gap-1.5"
              >
                <Plus className="size-4" />
                Record Custom Payment
              </Button>
            </div>

            {settleOverview?.suggestedTransactions.length === 0 ? (
              <div className="rounded-xl border bg-card p-6 text-center space-y-2">
                <CheckCircle2 className="size-8 text-inflow mx-auto" />
                <h4 className="font-semibold text-sm">Everyone is settled up!</h4>
                <p className="text-xs text-muted-foreground">
                  There are no outstanding debts in this group.
                </p>
              </div>
            ) : (
              <motion.div
                variants={reduce ? undefined : splitStaggerContainer}
                initial="hidden"
                animate="show"
                className="grid grid-cols-1 sm:grid-cols-2 gap-3"
              >
                {settleOverview?.suggestedTransactions.map((item, idx) => (
                  <motion.div key={idx} variants={reduce ? fadeOnly : splitCardVariants}>
                    <Card className="rounded-xl p-3.5 flex items-center justify-between">
                      <div>
                        <div className="text-xs text-muted-foreground">Payment {idx + 1}</div>
                        <div className="text-sm font-semibold">
                          {item.fromUserName} → {item.toUserName}
                        </div>
                        <div className="text-sm font-mono font-bold text-foreground mt-0.5">
                          Rs. {item.amount.toFixed(2)}
                        </div>
                      </div>
                      <Button
                        size="sm"
                        onClick={() => {
                          setActiveSuggestion(item)
                          setSettleUpOpen(true)
                        }}
                      >
                        Record Payment
                      </Button>
                    </Card>
                  </motion.div>
                ))}
              </motion.div>
            )}
          </div>

          {/* Past Settlement History */}
          <div className="space-y-3">
            <h3 className="text-base font-semibold">Settlement Payment History</h3>
            {settlements.length === 0 ? (
              <p className="text-xs text-muted-foreground">No payments recorded yet.</p>
            ) : (
              <div className="rounded-xl border divide-y overflow-hidden bg-card">
                {settlements.map((st) => (
                  <div key={st.id} className="flex items-center justify-between p-3.5 text-sm">
                    <div className="flex items-center gap-3 truncate">
                      <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
                        <HandCoins className="size-4" />
                      </div>
                      <div className="truncate">
                        <div className="font-semibold truncate">
                          {st.fromUserName} paid {st.toUserName}
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {format(new Date(st.settlementDate), 'MMM d, yyyy')}
                          {st.note && ` · ${st.note}`}
                        </div>
                      </div>
                    </div>
                    <div className="font-mono font-bold text-inflow">
                      Rs. {st.amount.toFixed(2)}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </MotionTabsContent>

        {/* 5. MEMBERS TAB */}
        <MotionTabsContent value="members" className="space-y-6 pt-4">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-base font-semibold">Active Members</h3>
              <p className="text-xs text-muted-foreground">Members who can view and share expenses.</p>
            </div>
            {isOwner && (
              <Button onClick={() => setInviteOpen(true)} size="sm" className="gap-1.5">
                <Plus className="size-4" />
                Invite Member
              </Button>
            )}
          </div>

          <motion.div
            variants={reduce ? undefined : splitStaggerContainer}
            initial="hidden"
            animate="show"
            className="rounded-xl border divide-y overflow-hidden bg-card"
          >
            {group.members?.map((m) => (
              <motion.div
                key={m.userId}
                variants={reduce ? fadeOnly : splitCardVariants}
                className="flex items-center justify-between p-3.5"
              >
                <div className="flex items-center gap-3">
                  <div className="flex size-9 items-center justify-center rounded-full bg-primary/10 text-primary font-semibold text-xs">
                    {getMemberInitials(m.fullName || m.email)}
                  </div>
                  <div>
                    <div className="font-semibold text-sm flex items-center gap-2">
                      {m.fullName}
                      {m.userId === currentUserId && (
                        <Badge variant="outline" className="text-[10px] h-4">
                          You
                        </Badge>
                      )}
                      {m.role === 'Owner' && (
                        <Badge variant="secondary" className="text-[10px] h-4 gap-1">
                          <Crown className="size-2.5" />
                          Owner
                        </Badge>
                      )}
                    </div>
                    <div className="text-xs text-muted-foreground">{m.email}</div>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  <div className="text-right">
                    <span className="text-xs font-mono font-medium block">
                      Balance: Rs. {m.balance.toFixed(2)}
                    </span>
                  </div>
                  {isOwner && m.role !== 'Owner' && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="size-8 text-destructive hover:bg-destructive/10"
                      onClick={() => setMemberToRemove(m.userId)}
                      aria-label="Remove member"
                    >
                      <UserX className="size-4" />
                    </Button>
                  )}
                </div>
              </motion.div>
            ))}
          </motion.div>

          {/* Pending Invitations Section */}
          {invitations.length > 0 && (
            <div className="space-y-3">
              <h4 className="text-sm font-semibold text-foreground">Pending Invitations</h4>
              <div className="rounded-xl border divide-y overflow-hidden bg-card">
                {invitations.map((inv) => (
                  <div key={inv.id} className="flex items-center justify-between p-3 text-sm">
                    <div className="flex items-center gap-2.5">
                      <Mail className="size-4 text-muted-foreground" />
                      <div>
                        <div className="font-medium text-xs">{inv.invitedEmail}</div>
                        <div className="text-[10px] text-muted-foreground">
                          Invited by {inv.invitedByName}
                          {inv.resendCount && inv.resendCount > 0 ? ` · Resent ${inv.resendCount}x` : ''}
                        </div>
                      </div>
                    </div>
                    {isOwner && (
                      <div className="flex items-center gap-1.5 shrink-0">
                        <Button
                          variant="outline"
                          size="sm"
                          className="h-7 text-xs gap-1"
                          disabled={resendingInviteId === inv.id}
                          onClick={() => handleResendInvitation(inv.id)}
                        >
                          {resendingInviteId === inv.id ? (
                            <Loader2 className="size-3 animate-spin" />
                          ) : (
                            <Send className="size-3" />
                          )}
                          Resend
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-7 text-xs text-destructive hover:bg-destructive/10"
                          onClick={() => handleRevokeInvitation(inv.id)}
                        >
                          Revoke
                        </Button>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </MotionTabsContent>

        {/* 6. ACTIVITY TAB */}
        <MotionTabsContent value="activity" className="space-y-4 pt-4">
          <div className="rounded-xl border bg-card p-4">
            <h3 className="text-base font-semibold mb-3">Group Activity History</h3>

            {activities.length === 0 ? (
              <p className="text-xs text-muted-foreground">No activities recorded yet.</p>
            ) : (
              <motion.div
                variants={reduce ? undefined : splitStaggerContainer}
                initial="hidden"
                animate="show"
                className="relative pl-6 space-y-4 before:absolute before:left-2.5 before:top-2 before:bottom-2 before:w-px before:bg-border"
              >
                {activities.map((act) => (
                  <motion.div
                    key={act.id}
                    variants={reduce ? fadeOnly : splitCardVariants}
                    className="relative flex items-start gap-3"
                  >
                    <div className="absolute -left-6 mt-1 flex size-5 items-center justify-center rounded-full bg-primary/10 text-primary border border-background">
                      <History className="size-2.5" />
                    </div>
                    <div className="space-y-0.5 text-xs">
                      <p className="font-medium text-foreground">{act.description}</p>
                      <p className="text-[10px] text-muted-foreground">
                        {format(new Date(act.createdAt), 'MMM d, yyyy · h:mm a')}
                      </p>
                    </div>
                  </motion.div>
                ))}
              </motion.div>
            )}
          </div>
        </MotionTabsContent>
      </MotionTabs>

      {/* Add Expense Dialog */}
      <AddExpenseDialog
        open={addExpenseOpen}
        onOpenChange={setAddExpenseOpen}
        groupId={groupId}
        members={group.members || []}
        currentUserId={currentUserId}
        token={token}
        onExpenseAdded={() => {
          loadGroupDetails()
          loadExpenses()
          loadBalancesAndSettlements()
          loadActivities()
        }}
      />

      {/* Expense Detail Dialog */}
      <ExpenseDetailDialog
        open={!!selectedExpense}
        onOpenChange={(v) => !v && setSelectedExpense(null)}
        expense={selectedExpense}
        currentUserId={currentUserId}
        isGroupOwner={isOwner}
        token={token}
        onExpenseDeleted={() => {
          loadGroupDetails()
          loadExpenses()
          loadBalancesAndSettlements()
          loadActivities()
        }}
      />

      {/* Settle Up Dialog */}
      <SettleUpDialog
        open={settleUpOpen}
        onOpenChange={setSettleUpOpen}
        groupId={groupId}
        members={group.members || []}
        currentUserId={currentUserId}
        initialSuggestion={activeSuggestion}
        token={token}
        onSettlementRecorded={() => {
          loadGroupDetails()
          loadBalancesAndSettlements()
          loadActivities()
        }}
      />

      {/* Invite Member Dialog */}
      <InviteMemberDialog
        open={inviteOpen}
        onOpenChange={setInviteOpen}
        groupId={groupId}
        token={token}
        onMemberInvited={() => {
          loadInvitations()
          loadActivities()
        }}
      />

      {/* Remove Member Confirmation Alert */}
      <AlertDialog open={!!memberToRemove} onOpenChange={(v) => !v && setMemberToRemove(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove member from group?</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to remove this member? Members with outstanding balances cannot be removed until settled.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={removingMember}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmRemoveMember}
              disabled={removingMember}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {removingMember ? 'Removing...' : 'Remove Member'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Delete Group Confirmation Alert */}
      <AlertDialog open={deleteGroupOpen} onOpenChange={setDeleteGroupOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this split group?</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{group.name}"? This action is permanent. Groups with active expenses cannot be deleted.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deletingGroup}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteGroup}
              disabled={deletingGroup}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {deletingGroup ? 'Deleting...' : 'Delete Group'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
