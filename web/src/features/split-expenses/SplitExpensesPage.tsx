import { useState, useEffect, useCallback, useMemo } from 'react'
import { PageHeader } from '@/components/app/PageHeader'
import { StatCard } from '@/components/app/StatCard'
import { EmptyState } from '@/components/app/EmptyState'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { splitExpensesApi } from './splitExpensesApi'
import { SplitGroupCard } from './components/SplitGroupCard'
import { CreateGroupDialog } from './components/CreateGroupDialog'
import { SplitGroupDetailsPage } from './components/SplitGroupDetailsPage'
import { toast } from 'sonner'
import type { SplitGroupSummaryDto, SplitGroupInvitationDto } from './types'
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
  onToast?: (msg: string) => void
}

export function SplitExpensesPage({
  token,
  currentUserId,
}: SplitExpensesPageProps) {
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null)
  const [groups, setGroups] = useState<SplitGroupSummaryDto[]>([])
  const [myInvitations, setMyInvitations] = useState<SplitGroupInvitationDto[]>([])
  const [loading, setLoading] = useState(true)
  const [createGroupOpen, setCreateGroupOpen] = useState(false)
  const [respondingInviteId, setRespondingInviteId] = useState<string | null>(null)

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

  // If a group is selected, show its detail page
  if (selectedGroupId) {
    return (
      <SplitGroupDetailsPage
        groupId={selectedGroupId}
        currentUserId={currentUserId}
        token={token}
        onBack={() => {
          setSelectedGroupId(null)
          loadData()
        }}
        onGroupDeleted={() => {
          setSelectedGroupId(null)
          loadData()
        }}
      />
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title="Split Expenses"
        description="Share expenses with friends, flatmates, and travel groups. Automatically divide costs and simplify debt."
        actions={
          <Button onClick={() => setCreateGroupOpen(true)} className="gap-2 shadow-xs">
            <Plus className="size-4" />
            Create Group
          </Button>
        }
      />

      {/* KPI Overview Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          label="Total You Are Owed"
          value={metrics.totalOwedToMe}
          formatFn={(val) => `Rs. ${val.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`}
          icon={ArrowUpRight}
          trend={metrics.totalOwedToMe > 0 ? 'up' : 'neutral'}
          delta="People owe you"
        />
        <StatCard
          label="Total You Owe"
          value={metrics.totalIOwe}
          formatFn={(val) => `Rs. ${val.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`}
          icon={ArrowDownLeft}
          trend={metrics.totalIOwe > 0 ? 'down' : 'neutral'}
          delta="You owe others"
        />
        <StatCard
          label="Net Balance"
          value={metrics.net}
          formatFn={(val) =>
            Math.abs(val) < 0.005
              ? 'Settled up'
              : val > 0
              ? `+Rs. ${val.toLocaleString('en-PK', { minimumFractionDigits: 2 })}`
              : `-Rs. ${Math.abs(val).toLocaleString('en-PK', { minimumFractionDigits: 2 })}`
          }
          icon={Scale}
          trend={metrics.net > 0 ? 'up' : metrics.net < 0 ? 'down' : 'neutral'}
          delta={metrics.net > 0 ? 'Overall positive' : metrics.net < 0 ? 'Overall debt' : 'Zero balance'}
        />
        <StatCard
          label="Active Groups"
          value={groups.length}
          formatFn={(val) => `${val}`}
          icon={Layers}
          trend="neutral"
          delta={`${groups.length} groups`}
        />
      </div>

      {/* Pending Invitations Banner */}
      {myInvitations.length > 0 && (
        <Card className="rounded-xl border border-primary/30 bg-primary/5 p-4 shadow-xs">
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
                className="flex items-center justify-between p-3 rounded-lg border bg-background text-sm"
              >
                <div className="truncate mr-2">
                  <div className="font-semibold truncate">{inv.groupName}</div>
                  <div className="text-xs text-muted-foreground truncate">
                    Invited by {inv.invitedByName}
                  </div>
                </div>
                <div className="flex items-center gap-1.5 shrink-0">
                  <Button
                    size="sm"
                    className="h-8 gap-1 text-xs"
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
      )}

      {/* Groups Grid */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            My Expense Groups ({groups.length})
          </h2>
        </div>

        {loading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <Skeleton className="h-40 rounded-xl" />
            <Skeleton className="h-40 rounded-xl" />
            <Skeleton className="h-40 rounded-xl" />
          </div>
        ) : groups.length === 0 ? (
          <EmptyState
            title="No Split Groups Yet"
            description="Create your first expense-sharing group or ask a group owner to invite you."
            action={
              <Button onClick={() => setCreateGroupOpen(true)} className="gap-2">
                <Plus className="size-4" />
                Create Group
              </Button>
            }
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {groups.map((group) => (
              <SplitGroupCard
                key={group.id}
                group={group}
                onClick={() => setSelectedGroupId(group.id)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Create Group Dialog */}
      <CreateGroupDialog
        open={createGroupOpen}
        onOpenChange={setCreateGroupOpen}
        token={token}
        onGroupCreated={(newGroup) => {
          setGroups((prev) => [newGroup, ...prev])
          setSelectedGroupId(newGroup.id)
        }}
      />
    </div>
  )
}
