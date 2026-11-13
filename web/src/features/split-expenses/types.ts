export interface SplitGroupSummaryDto {
  id: string
  name: string
  description?: string
  icon?: string
  ownerId: string
  ownerName: string
  memberCount: number
  totalExpenses: number
  currentUserBalance: number
  currentUserRole: string
  createdAt: string
}

export interface SplitGroupMemberDto {
  id: string
  userId: string
  fullName: string
  email: string
  role: string
  joinedAt: string
  isActive: boolean
  balance: number
}

export interface SplitGroupDetailDto {
  id: string
  name: string
  description?: string
  icon?: string
  ownerId: string
  ownerName: string
  ownerEmail: string
  memberCount: number
  totalExpenses: number
  currentUserBalance: number
  currentUserRole: string
  isActive: boolean
  createdAt: string
  members: SplitGroupMemberDto[]
}

export interface SplitExpenseShareItemDto {
  userId: string
  userName: string
  userEmail: string
  shareAmount: number
  isPayer: boolean
}

export interface SplitExpenseListItemDto {
  id: string
  groupId: string
  title: string
  amount: number
  payerId: string
  payerName: string
  expenseDate: string
  category?: string
  participantCount: number
  currentUserShare: number
  createdBy: string
  createdByName: string
  createdAt: string
}

export interface SplitExpenseDetailDto {
  id: string
  groupId: string
  title: string
  description?: string
  note?: string
  amount: number
  category?: string
  expenseDate: string
  payerId: string
  payerName: string
  payerEmail: string
  receiptUrl?: string
  splitType: string
  createdBy: string
  createdByName: string
  createdAt: string
  updatedAt: string
  shares: SplitExpenseShareItemDto[]
}

export interface PagedExpenseResultDto {
  items: SplitExpenseListItemDto[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface MemberBalanceDto {
  userId: string
  fullName: string
  email: string
  totalPaid: number
  totalShare: number
  settlementsPaid: number
  settlementsReceived: number
  pendingPaid?: number
  pendingReceived?: number
  netBalance: number
  role: string
  isActive: boolean
}

export interface GroupFinancialSummaryDto {
  groupId: string
  groupName: string
  totalExpensesAmount: number
  totalSettlementsAmount: number
  totalActiveDebt: number
  membersCount: number
  currentUserNetBalance: number
  currentUserPendingPaid?: number
  currentUserPendingReceived?: number
  memberBalances: MemberBalanceDto[]
}

export type SplitSettlementStatus =
  | 'PendingReceiver'
  | 'PendingOwner'
  | 'Confirmed'
  | 'DeclinedByReceiver'
  | 'DeclinedByOwner'
  | 'Cancelled'

export interface SplitSettlementDto {
  id: string
  groupId: string
  groupName: string
  fromUserId: string
  fromUserName: string
  fromUserEmail: string
  toUserId: string
  toUserName: string
  toUserEmail: string
  amount: number
  settlementDate: string
  note?: string
  status: SplitSettlementStatus
  createdBy: string
  createdByName: string
  receiverActionBy?: string
  receiverActionByName?: string
  receiverActionAt?: string
  ownerActionBy?: string
  ownerActionByName?: string
  ownerActionAt?: string
  declineReason?: string
  cancelledAt?: string
  cancelledBy?: string
  cancelledByName?: string
  createdAt: string
  updatedAt: string
  canConfirm?: boolean
  canDecline?: boolean
  canCancel?: boolean
  canReverse?: boolean
}

export interface SuggestedSettlementDto {
  fromUserId: string
  fromUserName: string
  fromUserEmail: string
  toUserId: string
  toUserName: string
  toUserEmail: string
  amount: number
}

export interface SettleUpOverviewDto {
  groupId: string
  groupName: string
  totalOutstandingDebt: number
  debtors: MemberBalanceDto[]
  creditors: MemberBalanceDto[]
  suggestedTransactions: SuggestedSettlementDto[]
}

export interface SplitGroupActivityDto {
  id: string
  groupId: string
  userId: string
  userName: string
  userEmail?: string
  activityType: string
  referenceId?: string
  description: string
  createdAt: string
}

export interface PagedGroupActivityResultDto {
  items: SplitGroupActivityDto[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface SplitGroupInvitationDto {
  id: string
  groupId: string
  groupName: string
  invitedUserId?: string
  invitedUserName?: string
  invitedEmail: string
  invitedBy: string
  invitedByName: string
  status: 'Pending' | 'Accepted' | 'Rejected' | 'Expired' | 'Revoked'
  expiresAt: string
  createdAt: string
  respondedAt?: string
  lastSentAt?: string
  resendCount?: number
  invitationToken?: string
}

export interface BatchInviteMembersDto {
  invitees: CreateGroupInviteeInputDto[]
}

export interface BatchInviteResultDto {
  results: CreateGroupInviteeResultDto[]
  totalCount: number
  successCount: number
  failureCount: number
}

export interface CreateGroupInviteeInputDto {
  userId?: string
  email?: string
  name?: string
}

export interface CreateGroupInviteeResultDto {
  identifier: string
  status: 'Invited' | 'AlreadyPending' | 'AlreadyMember' | 'Self' | 'Skipped' | 'Failed' | string
  message: string
  invitationId?: string
}

export interface CreateSplitGroupResultDto extends SplitGroupSummaryDto {
  invitations?: CreateGroupInviteeResultDto[]
}

export interface SplitUserSearchResultDto {
  userId: string
  fullName: string
  email: string
}

export interface CreateSplitGroupDto {
  name: string
  description?: string
  icon?: string
  invitees?: CreateGroupInviteeInputDto[]
}

export interface UpdateSplitGroupDto {
  name: string
  description?: string
  icon?: string
}

export interface CreateSplitExpenseDto {
  title: string
  amount: number
  description?: string
  note?: string
  category?: string
  expenseDate?: string
  payerId?: string
  receiptUrl?: string
  participantUserIds: string[]
}

export interface UpdateSplitExpenseDto {
  title: string
  amount: number
  description?: string
  note?: string
  category?: string
  expenseDate?: string
  payerId: string
  receiptUrl?: string
  participantUserIds: string[]
}

export interface RecordSettlementDto {
  fromUserId: string
  toUserId: string
  amount: number
  settlementDate?: string
  note?: string
}

export interface InviteSplitGroupMemberDto {
  email?: string
  invitedEmail?: string
  userId?: string
  invitedUserId?: string
}

export interface SplitNotificationDto {
  id: string
  userId: string
  type: 'GroupInvitationReceived' | 'InvitationAccepted' | 'InvitationRejected' | string
  groupId?: string
  groupName?: string
  referenceId?: string
  title: string
  message: string
  isRead: boolean
  createdAt: string
  readAt?: string
}

export interface SplitNotificationUnreadCountDto {
  unreadCount: number
}

export interface PagedSplitNotificationResultDto {
  items: SplitNotificationDto[]
  totalCount: number
  unreadCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}
