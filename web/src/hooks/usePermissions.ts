export interface UserWorkspaceAccess {
  isOwner: boolean
  businessPermissions: string[]
  cashbookPermissions: string[]
}

export interface PermissionsState {
  isOwner: boolean
  canView: boolean
  canAdd: boolean
  canEdit: boolean
  canDelete: boolean
  canCreateTransaction: boolean
  canEditTransaction: boolean
  canDeleteTransaction: boolean
  canCreateCashbook: boolean
  canEditCashbook: boolean
  canDeleteCashbook: boolean
  businessPermissions: string[]
  cashbookPermissions: string[]
}

export function computePermissions(access: UserWorkspaceAccess): PermissionsState {
  const isOwner = access.isOwner
  const bizPerms = access.businessPermissions || []
  const cbPerms = access.cashbookPermissions || []

  const canCreateTx = isOwner || cbPerms.includes('TRANSACTION_CREATE')
  const canEditTx = isOwner || cbPerms.includes('TRANSACTION_UPDATE')
  const canDeleteTx = isOwner || cbPerms.includes('TRANSACTION_DELETE')
  const canViewTx = isOwner || cbPerms.includes('TRANSACTION_VIEW') || canCreateTx || canEditTx || canDeleteTx

  const canCreateCb = isOwner || bizPerms.includes('CASHBOOK_CREATE')
  const canEditCb = isOwner || bizPerms.includes('CASHBOOK_UPDATE')
  const canDeleteCb = isOwner || bizPerms.includes('CASHBOOK_DELETE')

  return {
    isOwner,
    canView: canViewTx,
    canAdd: canCreateTx,
    canEdit: canEditTx,
    canDelete: canDeleteTx,
    canCreateTransaction: canCreateTx,
    canEditTransaction: canEditTx,
    canDeleteTransaction: canDeleteTx,
    canCreateCashbook: canCreateCb,
    canEditCashbook: canEditCb,
    canDeleteCashbook: canDeleteCb,
    businessPermissions: bizPerms,
    cashbookPermissions: cbPerms,
  }
}
