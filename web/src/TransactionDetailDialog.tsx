import { useState } from 'react'
import {
  Calendar,
  Check,
  Clock,
  Copy,
  ExternalLink,
  Layers,
  Lock,
  Paperclip,
  Pencil,
    Trash2,
  User,
  Wallet,
  X
} from 'lucide-react'
import { getTransactionCategory, type Transaction } from './App'

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
  onViewAttachment
}: Props) {
  const [copiedField, setCopiedField] = useState<string | null>(null)

  const isIncome = transaction.transactionType?.toLowerCase() === 'cashin'
  const category = getTransactionCategory(transaction)
  const CategoryIcon = category.Icon
  const hasAttachment = Boolean(transaction.filePath || transaction.fileName)

  const dateObj = transaction.transactionDate ? new Date(transaction.transactionDate) : null
  const isValidDate = dateObj && !Number.isNaN(dateObj.getTime())

  const formattedDate = isValidDate
    ? dateObj.toLocaleDateString('en-US', {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      })
    : 'Date not recorded'

  const formattedTime = isValidDate
    ? dateObj.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit'
      })
    : ''

  const relativeTime = isValidDate
    ? (() => {
        const now = new Date()
        const diffMs = now.getTime() - dateObj.getTime()
        const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))
        if (diffDays === 0) return 'Today'
        if (diffDays === 1) return 'Yesterday'
        if (diffDays > 1 && diffDays < 30) return `${diffDays} days ago`
        return ''
      })()
    : ''

  function copyText(text: string, field: string) {
    if (!text) return
    void navigator.clipboard.writeText(text)
    setCopiedField(field)
    setTimeout(() => setCopiedField(null), 2000)
  }

  return (
    <div className="overlay" onClick={onClose} role="dialog" aria-modal="true">
      <div
        className="dialog transaction-detail-dialog"
        onClick={e => e.stopPropagation()}
      >
        <header>
          <div>
            <div className="eyebrow">TRANSACTION DETAILS</div>
            <h2>{transaction.remarks || (isIncome ? 'Cash Inflow' : 'Cash Expense')}</h2>
          </div>
          <button className="close" onClick={onClose} aria-label="Close dialog">
            <X size={18} strokeWidth={1.8} />
          </button>
        </header>

        <div className="dialog-content transaction-detail-content">
          {/* Prominent Amount & Category Banner */}
          <div className={`txn-detail-hero ${isIncome ? 'income' : 'expense'}`}>
            <div className="txn-detail-hero-icon">
              <CategoryIcon size={24} strokeWidth={1.8} />
            </div>
            <div className="txn-detail-hero-info">
              <span className="txn-detail-type-badge">
                {isIncome ? 'Cash In (Income / Refund)' : 'Cash Out (Expense)'}
              </span>
              <h1 className="txn-detail-amount">
                {isIncome ? '+' : '-'}Rs. {(transaction.amount ?? 0).toLocaleString('en-PK')}
              </h1>
              <div className="txn-detail-category-label">
                Category: <strong>{category.label}</strong>
              </div>
            </div>
          </div>

          {/* Metadata Cards Grid */}
          <div className="txn-detail-grid">
            <div className="txn-meta-card">
              <span className="txn-meta-card-icon">
                <Calendar size={16} strokeWidth={1.8} />
              </span>
              <div className="txn-meta-card-content">
                <label>Transaction Date</label>
                <strong>{formattedDate}</strong>
                {formattedTime && (
                  <small className="muted">
                    <Clock size={11} strokeWidth={1.6} style={{ display: 'inline', verticalAlign: -1, marginRight: 3 }} />
                    {formattedTime} {relativeTime ? `(${relativeTime})` : ''}
                  </small>
                )}
              </div>
            </div>

            <div className="txn-meta-card">
              <span className="txn-meta-card-icon">
                <Wallet size={16} strokeWidth={1.8} />
              </span>
              <div className="txn-meta-card-content">
                <label>Cashbook & Workspace</label>
                <strong>{cashbookName}</strong>
                <small className="muted">{businessName}</small>
              </div>
            </div>

            <div className="txn-meta-card txn-meta-card-author">
              <span className="txn-meta-card-icon">
                <User size={16} strokeWidth={1.8} />
              </span>
              <div className="txn-meta-card-content">
                <label>Recorded By</label>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  <strong>{transaction.createdBy || 'Workspace Member'}</strong>
                  <span className="txn-creator-badge-inline">Author</span>
                </div>
                <small className="muted">Logged in database record</small>
              </div>
            </div>

            <div className="txn-meta-card">
              <span className="txn-meta-card-icon">
                <Layers size={16} strokeWidth={1.8} />
              </span>
              <div className="txn-meta-card-content">
                <label>Transaction ID</label>
                <div className="txn-id-row">
                  <code>{transaction.id?.slice(0, 8)}…{transaction.id?.slice(-4)}</code>
                  <button
                    type="button"
                    className="txn-copy-id-btn"
                    title="Copy Full ID"
                    onClick={() => copyText(transaction.id, 'id')}
                  >
                    {copiedField === 'id' ? <Check size={12} /> : <Copy size={12} />}
                  </button>
                </div>
                <small className="muted">Reference ID</small>
              </div>
            </div>
          </div>

          {/* Remarks & Notes */}
          <div className="txn-notes-section">
            <label className="txn-section-label">Notes & Description</label>
            <div className="txn-notes-box">
              {transaction.remarks ? (
                <p>{transaction.remarks}</p>
              ) : (
                <p className="muted" style={{ fontStyle: 'italic' }}>
                  No remarks or note entered for this transaction.
                </p>
              )}
            </div>
          </div>

          {/* Receipt Attachment Section */}
          <div className="txn-attachment-section">
            <label className="txn-section-label">
              <Paperclip size={13} strokeWidth={1.8} /> Receipt / Bill Attachment
            </label>
            {hasAttachment ? (
              <div className="txn-attachment-card">
                <div className="txn-attachment-info">
                  <span className="attachment-file-pill">
                    <Paperclip size={13} />
                    {transaction.fileName || 'Attached Receipt'}
                  </span>
                  <p className="muted" style={{ margin: '4px 0 0', fontSize: 11 }}>
                    Uploaded receipt image or invoice proof.
                  </p>
                </div>
                <button
                  type="button"
                  className="button secondary-action"
                  onClick={() => {
                    onClose()
                    onViewAttachment(transaction)
                  }}
                >
                  <ExternalLink size={14} strokeWidth={1.8} /> View Receipt
                </button>
              </div>
            ) : (
              <div className="txn-no-attachment">
                <p className="muted" style={{ margin: 0, fontSize: 11 }}>
                  No receipt or invoice was attached to this transaction.
                </p>
              </div>
            )}
          </div>

          {/* Footer Actions */}
          <div className="txn-detail-actions">
            {canDelete ? (
              <button
                type="button"
                className="button danger-outline"
                onClick={() => {
                  onClose()
                  onDelete(transaction)
                }}
              >
                <Trash2 size={14} strokeWidth={1.8} /> Delete
              </button>
            ) : (
              <span className="txn-view-only-tag" title="Owner has restricted transaction deletion for this cashbook">
                <Lock size={12} strokeWidth={1.8} /> Delete restricted
              </span>
            )}
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              {canEdit ? (
                <button
                  type="button"
                  className="button secondary-action"
                  onClick={() => {
                    onClose()
                    onEdit(transaction)
                  }}
                >
                  <Pencil size={14} strokeWidth={1.8} /> Edit
                </button>
              ) : (
                <span className="txn-view-only-tag" title="Owner has restricted transaction editing for this cashbook">
                  <Lock size={12} strokeWidth={1.8} /> Edit restricted
                </span>
              )}
              <button type="button" className="button primary" onClick={onClose}>
                Done
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
