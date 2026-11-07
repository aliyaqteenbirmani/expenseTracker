import { TransactionDetailSheet } from '@/features/transactions/TransactionDetailSheet'
import type { Transaction } from './App'

type Props = {
  transaction: Transaction
  cashbookName?: string
  businessName?: string
  canEdit?: boolean
  canDelete?: boolean
  onClose: () => void
  onEdit: (transaction: Transaction) => void
  onDelete: (transaction: Transaction) => void
  onViewAttachment: (transaction: Transaction) => void
}

export default function TransactionDetailDialog({
  transaction,
  cashbookName = 'Cashbook',
  businessName = 'Workspace',
  canEdit = true,
  canDelete = true,
  onClose,
  onEdit,
  onDelete,
  onViewAttachment,
}: Props) {
  return (
    <TransactionDetailSheet
      open={true}
      onOpenChange={(open) => !open && onClose()}
      transaction={transaction}
      cashbookName={cashbookName}
      businessName={businessName}
      canEdit={canEdit}
      canDelete={canDelete}
      onEdit={onEdit}
      onDelete={onDelete}
      onViewAttachment={onViewAttachment}
      onToast={() => {}}
    />
  )
}
