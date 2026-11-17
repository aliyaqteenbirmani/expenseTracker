import { useState, useEffect, useCallback, useMemo } from 'react'
import { motion, AnimatePresence } from 'motion/react'
import { PageHeader } from '@/components/app/PageHeader'
import { StatCard } from '@/components/app/StatCard'
import { EmptyState } from '@/components/app/EmptyState'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { splitExpensesApi } from './splitExpensesApi'
import { SplitGroupCard } from './components/SplitGroupCard'
import { CreateGroupDialog } from './components/CreateGroupDialog'
import { SplitGroupDetailsPage } from './components/SplitGroupDetailsPage'
import { SplitNotificationBell } from './components/SplitNotificationBell'
import { SplitDashboardSkeleton } from './components/SplitDashboardSkeleton'
import {
  SplitPageTransition,
  splitStaggerContainer,
  splitCardVariants,
} from './components/SplitPageTransition'
import { useMotionSafe } from '@/hooks/use-motion-safe'
import { toast } from 'sonner'
import type { SplitGroupSummaryDto, SplitGroupInvitationDto, SplitNotificationDto } from './types'
import { splitLabels } from './splitLabels'
import { cn } from '@/lib/utils'
import {
  Plus,
  Scale,
  ArrowUpRight,
  ArrowDownLeft,
  Mail,
  Check,
  X,
  Layers,
} from 'lucide-react'

interface SplitExpensesPageProps {
  token?: string
  currentUserId?: string
  currentUserEmail?: string
  onToast?: (msg: string) => void
}

export function SplitExpensesPage({
  token,
  currentUserId,
  currentUserEmail,
}: SplitExpensesPageProps) {
  const { reduce, fadeOnly } = useMotionSafe()
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null)
  const [initialExpenseId, setInitialExpenseId] = useState<string | null>(null)
  const [initialTab, setInitialTab] = useState<string | null>(null)
  const [groups, setGroups] = useState<SplitGroupSummaryDto[]>([])
  const [myInvitations, setMyInvitations] = useState<SplitGroupInvitationDto[]>([])
  const [loading, setLoading] = useState(true)
  const [createGroupOpen, setCreateGroupOpen] = useState(false)
  const [respondingInviteId, setRespondingInviteId] = useState<string | null>(null)

  const handleNotificationClick = (notif: SplitNotificationDto) => {
    if (notif.groupId) {
      setSelectedGroupId(notif.groupId)
      if (notif.type === 'ExpenseDeleted') {
        toast.info('This expense has been deleted.')
        setInitialExpenseId(null)
        setInitialTab('expenses')
      } else if (notif.type.includes('Expense') && notif.referenceId) {
        setInitialExpenseId(notif.referenceId)
        setInitialTab('expenses')
      } else if (notif.type.includes('Settlement')) {
        setInitialExpenseId(null)
        setInitialTab('settle')
      } else if (notif.type.includes('Invitation') || notif.type.includes('Member')) {
        setInitialExpenseId(null)
        setInitialTab('members')
      } else {
        setInitialExpenseId(null)
        setInitialTab('overview')
      }
    }
  }

  const loadData = useCallback(async () => {
    try {
      setLoading(true)
      const [groupsData, invitesData] = await Promise.all([
        splitExpensesApi.getMyGroups(token),
        splitExpensesApi.getMyInvitations(token),
      ])
      setGroups(groupsData || [])
      setMyInvitations(invitesData || [])
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to load split groups'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }, [token])

  useEffect(() => {
    loadData()
  }, [loadData])

  // Aggregate financial metrics across all groups
  const metrics = useMemo(() => {
    let totalOwedToMe = 0
    let totalIOwe = 0

    for (const g of groups) {
      if (g.currentUserBalance > 0.005) {
        totalOwedToMe += g.currentUserBalance
      } else if (g.currentUserBalance < -0.005) {
        totalIOwe += Math.abs(g.currentUserBalance)
      }
    }

    const net = totalOwedToMe - totalIOwe
    return { totalOwedToMe, totalIOwe, net }
  }, [groups])

  // Accept / Reject Invitation handlers
  const handleAcceptInvite = async (invitationId: string) => {
    try {
      setRespondingInviteId(invitationId)
      await splitExpensesApi.acceptInvitation(invitationId, token)
      toast.success('Joined group successfully!')
      loadData()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to accept invitation'
      toast.error(msg)
    } finally {
      setRespondingInviteId(null)
    }
  }

  const handleRejectInvite = async (invitationId: string) => {
    try {
      setRespondingInviteId(invitationId)
      await splitExpensesApi.rejectInvitation(invitationId, token)
      toast.success('Invitation rejected')
      loadData()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to reject invitation'
      toast.error(msg)
    } finally {
      setRespondingInviteId(null)
    }
  }

  return (
    <AnimatePresence mode="wait" initial={false}>
      {selectedGroupId ? (
        <SplitPageTransition key={`group-details-${selectedGroupId}`}>
          <SplitGroupDetailsPage
            groupId={selectedGroupId}
            currentUserId={currentUserId}
            token={token}
            initialExpenseId={initialExpenseId}
            initialTab={initialTab}
            onBack={() => {
              setSelectedGroupId(null)
              setInitialExpenseId(null)
              setInitialTab(null)
              loadData()
            }}
            onGroupDeleted={() => {
              setSelectedGroupId(null)
              setInitialExpenseId(null)
              setInitialTab(null)
              loadData()
            }}
          />
        </SplitPageTransition>
      ) : (
        <SplitPageTransition key="split-dashboard">
          {loading && groups.length === 0 ? (
            <SplitDashboardSkeleton />
          ) : (
            <div className="space-y-6">
              {/* Header */}
              <PageHeader
                className="border-b pb-4"
                title="Split Expenses"
                description="Share expenses with friends, flatmates, and travel groups. Automatically divide costs and simplify debt."
                actions={
                  <div className="flex items-center gap-2 w-full sm:w-auto shrink-0 justify-end">
                    <SplitNotificationBell
                      token={token}
                      onInvitationProcessed={loadData}
                      onSettlementProcessed={loadData}
                      onNotificationClick={handleNotificationClick}
                    />
                    <Button
                      onClick={() => setCreateGroupOpen(true)}
                      className="gap-2 bg-emerald-600 hover:bg-emerald-700 text-white font-medium shadow-xs hover:shadow transition-[color,background-color,box-shadow,transform] duration-150 active:scale-[0.98] flex-1 sm:flex-initial"
                    >
                      <Plus className="size-4" />
                      Create Group
                    </Button>
                  </div>
                }
              />

              {/* KPI Overview Cards with staggered animation */}
              <motion.div
                variants={reduce ? undefined : splitStaggerContainer}
                initial="hidden"
                animate="show"
                className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 items-stretch"
              >
                <motion.div variants={reduce ? fadeOnly : splitCardVariants} className="h-full">
                  <StatCard
                    className={cn(
                      "h-full transition-all duration-200",
                      metrics.totalOwedToMe < 0.005
                        ? "border-border/70 bg-card/60 opacity-80 hover:opacity-100"
                        : "border-inflow/20 bg-gradient-to-t from-inflow-muted/30 to-card"
                    )}
                    label={splitLabels.dashboard.youWillGet.title}
                    labelClassName={
                      metrics.totalOwedToMe < 0.005
                        ? "text-muted-foreground/90 font-semibold"
                        : "text-inflow font-semibold"
                    }
                    value={metrics.totalOwedToMe}
                    currencySymbol="Rs."
                    formatFn={(val) => val.toLocaleString('en-PK', { minimumFractionDigits: 2 })}
                    icon={ArrowUpRight}
                    iconContainerClassName={
                      metrics.totalOwedToMe < 0.005
                        ? "bg-muted/60 text-muted-foreground"
                        : "bg-inflow/10 text-inflow"
                    }
                    iconClassName={
                      metrics.totalOwedToMe < 0.005
                        ? "text-muted-foreground"
                        : "text-inflow"
                    }
                    valueColor={
                      metrics.totalOwedToMe < 0.005
                        ? "text-muted-foreground"
                        : "text-inflow"
                    }
                    trend={metrics.totalOwedToMe > 0.005 ? "up" : "neutral"}
                    delta={splitLabels.dashboard.youWillGet.subtext}
                    badgePosition="below"
                  />
                </motion.div>
                <motion.div variants={reduce ? fadeOnly : splitCardVariants} className="h-full">
                  <StatCard
                    className={cn(
                      "h-full transition-all duration-200",
                      metrics.totalIOwe > 0.005
                        ? "border-outflow/30 bg-gradient-to-t from-outflow-muted/40 to-card shadow-xs ring-1 ring-outflow/20"
                        : "border-outflow/20 bg-gradient-to-t from-outflow-muted/30 to-card"
                    )}
                    label={splitLabels.dashboard.youNeedToPay.title}
                    labelClassName="text-outflow font-semibold"
                    value={metrics.totalIOwe}
                    currencySymbol="Rs."
                    formatFn={(val) => val.toLocaleString('en-PK', { minimumFractionDigits: 2 })}
                    icon={ArrowDownLeft}
                    iconContainerClassName="bg-outflow/10 text-outflow"
                    iconClassName="text-outflow"
                    valueColor="text-outflow"
                    trend="down"
                    delta={splitLabels.dashboard.youNeedToPay.subtext}
                    badgePosition="below"
                  />
                </motion.div>
                <motion.div variants={reduce ? fadeOnly : splitCardVariants} className="h-full">
                  <StatCard
                    className="h-full"
                    label={splitLabels.dashboard.overallBalance.title}
                    labelClassName="text-muted-foreground/90 font-semibold"
                    value={Math.abs(metrics.net)}
                    sign={metrics.net > 0.005 ? "+" : metrics.net < -0.005 ? "-" : undefined}
                    currencySymbol="Rs."
                    formatFn={(val) => val.toLocaleString('en-PK', { minimumFractionDigits: 2 })}
                    icon={Scale}
                    valueColor={
                      metrics.net > 0.005
                        ? 'text-inflow'
                        : metrics.net < -0.005
                        ? 'text-outflow'
                        : 'text-muted-foreground'
                    }
                    trend={metrics.net > 0.005 ? 'up' : metrics.net < -0.005 ? 'down' : 'neutral'}
                  />
                </motion.div>
                <motion.div variants={reduce ? fadeOnly : splitCardVariants} className="h-full">
                  <StatCard
                    className="h-full"
                    label={splitLabels.dashboard.myGroups.title}
                    labelClassName="text-muted-foreground/90 font-semibold"
                    value={groups.length}
                    formatFn={(val) => `${val}`}
                    icon={Layers}
                    trend="neutral"
                  />
                </motion.div>
              </motion.div>

              {/* Pending Invitations Banner */}
              {myInvitations.length > 0 && (
                <motion.div
                  variants={reduce ? fadeOnly : splitCardVariants}
                  initial="hidden"
                  animate="show"
                >
                  <Card className="rounded-xl border border-primary/20 bg-primary/5 p-4 shadow-xs">
                    <div className="flex items-center gap-2 mb-3">
                      <Mail className="size-4 text-primary" />
                      <h3 className="font-semibold text-sm">
                        Group Invitations ({myInvitations.length})
                      </h3>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      {myInvitations.map((inv) => (
                        <div
                          key={inv.id}
                          className="flex flex-col sm:flex-row sm:items-center justify-between p-3.5 rounded-lg border bg-background text-sm gap-3"
                        >
                          <div className="truncate min-w-0">
                            <p className="text-sm text-foreground truncate" title={`${inv.groupName} by ${inv.invitedByName}`}>
                              You&apos;ve been invited to <span className="font-semibold">{inv.groupName}</span> by{' '}
                              <span className="font-medium">{inv.invitedByName}</span>
                            </p>
                          </div>
                          <div className="flex items-center gap-2 shrink-0 self-end sm:self-auto">
                            <Button
                              size="sm"
                              className="h-8 gap-1 text-xs shadow-xs"
                              disabled={respondingInviteId === inv.id}
                              onClick={() => handleAcceptInvite(inv.id)}
                            >
                              <Check className="size-3.5" />
                              Accept
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              className="h-8 gap-1 text-xs text-destructive hover:bg-destructive/10"
                              disabled={respondingInviteId === inv.id}
                              onClick={() => handleRejectInvite(inv.id)}
                            >
                              <X className="size-3.5" />
                              Decline
                            </Button>
                          </div>
                        </div>
                      ))}
                    </div>
                  </Card>
                </motion.div>
              )}

              {/* Groups Grid */}
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <h2 className="text-base sm:text-lg font-semibold tracking-tight text-foreground">
                    My Groups ({groups.length})
                  </h2>
                </div>

                {groups.length === 0 ? (
                  <EmptyState
                    icon={Layers}
                    title="No Split Groups Yet"
                    description="Create your first group to start sharing expenses"
                    action={
                      <Button onClick={() => setCreateGroupOpen(true)} className="gap-2 shadow-xs">
                        <Plus className="size-4" />
                        Create Group
                      </Button>
                    }
                  />
                ) : (
                  <motion.div
                    variants={reduce ? undefined : splitStaggerContainer}
                    initial="hidden"
                    animate="show"
                    className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-stretch"
                  >
                    {groups.map((group) => (
                      <motion.div
                        key={group.id}
                        variants={reduce ? fadeOnly : splitCardVariants}
                        className="h-full"
                      >
                        <SplitGroupCard
                          group={group}
                          onClick={() => setSelectedGroupId(group.id)}
                        />
                      </motion.div>
                    ))}
                  </motion.div>
                )}
              </div>

              {/* Create Group Dialog */}
              <CreateGroupDialog
                open={createGroupOpen}
                onOpenChange={setCreateGroupOpen}
                token={token}
                currentUserId={currentUserId}
                currentUserEmail={currentUserEmail}
                onGroupCreated={(newGroup) => {
                  setGroups((prev) => [newGroup, ...prev])
                  setSelectedGroupId(newGroup.id)
                }}
              />
            </div>
          )}
        </SplitPageTransition>
      )}
    </AnimatePresence>
  )
}
