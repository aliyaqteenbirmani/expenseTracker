import { api } from '@/App'
import type {
  SplitGroupSummaryDto,
  SplitGroupDetailDto,
  SplitGroupMemberDto,
  CreateSplitGroupDto,
  CreateSplitGroupResultDto,
  SplitUserSearchResultDto,
  UpdateSplitGroupDto,
  InviteSplitGroupMemberDto,
  BatchInviteMembersDto,
  BatchInviteResultDto,
  SplitGroupInvitationDto,
  PagedExpenseResultDto,
  SplitExpenseDetailDto,
  CreateSplitExpenseDto,
  UpdateSplitExpenseDto,
  MemberBalanceDto,
  GroupFinancialSummaryDto,
  SplitSettlementDto,
  SettleUpOverviewDto,
  RecordSettlementDto,
  PagedGroupActivityResultDto,
  PagedSplitNotificationResultDto,
  SplitNotificationUnreadCountDto,
} from './types'

export const splitExpensesApi = {
  // --- Group APIs ---
  getMyGroups: (token?: string) =>
    api<SplitGroupSummaryDto[]>('/api/splitgroup', token),

  getGroupDetails: (groupId: string, token?: string) =>
    api<SplitGroupDetailDto>(`/api/splitgroup/${groupId}`, token),

  getGroupMembers: (groupId: string, token?: string) =>
    api<SplitGroupMemberDto[]>(`/api/splitgroup/${groupId}/members`, token),

  createGroup: (dto: CreateSplitGroupDto, token?: string) =>
    api<CreateSplitGroupResultDto>('/api/splitgroup', token, {
      method: 'POST',
      body: JSON.stringify(dto),
    }),

  searchUsers: (query: string, token?: string) =>
    api<SplitUserSearchResultDto[]>(`/api/splitgroup/search-users?query=${encodeURIComponent(query)}`, token),

  updateGroup: (groupId: string, dto: UpdateSplitGroupDto, token?: string) =>
    api<boolean>(`/api/splitgroup/${groupId}`, token, {
      method: 'PUT',
      body: JSON.stringify(dto),
    }),

  deleteGroup: (groupId: string, token?: string) =>
    api<boolean>(`/api/splitgroup/${groupId}`, token, {
      method: 'DELETE',
    }),

  // --- Member & Invitation APIs ---
  inviteMember: (groupId: string, dto: InviteSplitGroupMemberDto, token?: string) => {
    const email = dto.email?.trim() || dto.invitedEmail?.trim()
    const userId = dto.userId || dto.invitedUserId
    return api<SplitGroupInvitationDto>(`/api/splitgroup/${groupId}/invite`, token, {
      method: 'POST',
      body: JSON.stringify({
        email,
        invitedEmail: email,
        userId,
        invitedUserId: userId,
      }),
    })
  },

  batchInviteMembers: (groupId: string, dto: BatchInviteMembersDto, token?: string) =>
    api<BatchInviteResultDto>(`/api/splitgroup/${groupId}/invitations/batch`, token, {
      method: 'POST',
      body: JSON.stringify(dto),
    }),

  resendInvitation: (groupId: string, invitationId: string, token?: string) =>
    api<SplitGroupInvitationDto>(`/api/splitgroup/${groupId}/invitations/${invitationId}/resend`, token, {
      method: 'POST',
    }),

  getInvitationByToken: (tokenString: string) =>
    api<SplitGroupInvitationDto>(`/api/splitgroup/invitations/token/${encodeURIComponent(tokenString)}`),

  getGroupInvitations: (groupId: string, token?: string, includeHistory?: boolean) =>
    api<SplitGroupInvitationDto[]>(
      `/api/splitgroup/${groupId}/invitations${includeHistory ? '?includeHistory=true' : ''}`,
      token
    ),

  getMyInvitations: (token?: string) =>
    api<SplitGroupInvitationDto[]>('/api/splitgroup/invitations/me', token),

  revokeInvitation: (groupId: string, invitationId: string, token?: string) =>
    api<boolean>(`/api/splitgroup/${groupId}/invitations/${invitationId}/revoke`, token, {
      method: 'POST',
    }),

  acceptInvitation: (invitationId: string, token?: string) =>
    api<boolean>(`/api/splitgroup/invitations/${invitationId}/accept`, token, {
      method: 'POST',
    }),

  rejectInvitation: (invitationId: string, token?: string) =>
    api<boolean>(`/api/splitgroup/invitations/${invitationId}/reject`, token, {
      method: 'POST',
    }),

  removeMember: (groupId: string, userId: string, token?: string) =>
    api<boolean>(`/api/splitgroup/${groupId}/members/${userId}`, token, {
      method: 'DELETE',
    }),

  // --- Split Notifications APIs ---
  getMyNotifications: (pageNumber = 1, pageSize = 20, token?: string) =>
    api<PagedSplitNotificationResultDto>(`/api/splitnotification?pageNumber=${pageNumber}&pageSize=${pageSize}`, token),

  getNotificationUnreadCount: (token?: string) =>
    api<SplitNotificationUnreadCountDto>('/api/splitnotification/unread-count', token),

  markNotificationAsRead: (notificationId: string, token?: string) =>
    api<boolean>(`/api/splitnotification/${notificationId}/read`, token, {
      method: 'PUT',
    }),

  markAllNotificationsAsRead: (token?: string) =>
    api<number>('/api/splitnotification/read-all', token, {
      method: 'PUT',
    }),

  // --- Expense APIs ---
  getExpenses: (
    groupId: string,
    params?: { search?: string; category?: string; pageNumber?: number; pageSize?: number },
    token?: string
  ) => {
    const query = new URLSearchParams()
    if (params?.search) query.set('search', params.search)
    if (params?.category) query.set('category', params.category)
    if (params?.pageNumber) query.set('pageNumber', params.pageNumber.toString())
    if (params?.pageSize) query.set('pageSize', params.pageSize.toString())
    const qs = query.toString() ? `?${query.toString()}` : ''
    return api<PagedExpenseResultDto>(`/api/splitgroup/${groupId}/expenses${qs}`, token)
  },

  getExpenseDetails: (groupId: string, expenseId: string, token?: string) =>
    api<SplitExpenseDetailDto>(`/api/splitgroup/${groupId}/expenses/${expenseId}`, token),

  addExpense: (groupId: string, dto: CreateSplitExpenseDto, token?: string) =>
    api<SplitExpenseDetailDto>(`/api/splitgroup/${groupId}/expenses`, token, {
      method: 'POST',
      body: JSON.stringify(dto),
    }),

  updateExpense: (groupId: string, expenseId: string, dto: UpdateSplitExpenseDto, token?: string) =>
    api<SplitExpenseDetailDto>(`/api/splitgroup/${groupId}/expenses/${expenseId}`, token, {
      method: 'PUT',
      body: JSON.stringify(dto),
    }),

  deleteExpense: (groupId: string, expenseId: string, token?: string) =>
    api<boolean>(`/api/splitgroup/${groupId}/expenses/${expenseId}`, token, {
      method: 'DELETE',
    }),

  // --- Balance APIs ---
  getGroupBalances: (groupId: string, token?: string) =>
    api<MemberBalanceDto[]>(`/api/splitgroup/${groupId}/balances`, token),

  getMyBalance: (groupId: string, token?: string) =>
    api<MemberBalanceDto>(`/api/splitgroup/${groupId}/balances/me`, token),

  getMemberBalance: (groupId: string, userId: string, token?: string) =>
    api<MemberBalanceDto>(`/api/splitgroup/${groupId}/balances/${userId}`, token),

  getFinancialSummary: (groupId: string, token?: string) =>
    api<GroupFinancialSummaryDto>(`/api/splitgroup/${groupId}/financial-summary`, token),

  // --- Settlement APIs ---
  getGroupSettlements: (groupId: string, token?: string) =>
    api<SplitSettlementDto[]>(`/api/splitgroup/${groupId}/settlements`, token),

  getSettleUpSuggestions: (groupId: string, token?: string) =>
    api<SettleUpOverviewDto>(`/api/splitgroup/${groupId}/settlements/suggestions`, token),

  recordSettlement: (groupId: string, dto: RecordSettlementDto, token?: string) =>
    api<SplitSettlementDto>(`/api/splitgroup/${groupId}/settlements`, token, {
      method: 'POST',
      body: JSON.stringify(dto),
    }),

  deleteSettlement: (groupId: string, settlementId: string, token?: string) =>
    api<boolean>(`/api/splitgroup/${groupId}/settlements/${settlementId}`, token, {
      method: 'DELETE',
    }),

  // --- Activity APIs ---
  getGroupActivities: (
    groupId: string,
    params?: { activityType?: string; pageNumber?: number; pageSize?: number },
    token?: string
  ) => {
    const query = new URLSearchParams()
    if (params?.activityType) query.set('activityType', params.activityType)
    if (params?.pageNumber) query.set('pageNumber', params.pageNumber.toString())
    if (params?.pageSize) query.set('pageSize', params.pageSize.toString())
    const qs = query.toString() ? `?${query.toString()}` : ''
    return api<PagedGroupActivityResultDto>(`/api/splitgroup/${groupId}/activities${qs}`, token)
  },
}
