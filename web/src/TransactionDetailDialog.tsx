import { useState } from 'react'
import {
  Calendar,
  Check,
  Clock,
  Copy,
  ExternalLink,
  Layers,
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
  onClose: () => void
  onEdit: (transaction: Transaction) => void
  onDelete: (transaction: Transaction) => void
  onViewAttachment: (transaction: Transaction) => void
}

export default function TransactionDetailDialog({
  transaction,
  cashbookName = 'Cashbook',
  businessName = 'Workspace',
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
            <div className="txn-detail-amount-wrap">
              <span className="txn-detail-sign">{isIncome ? '+' : '−'}</span>
              <span className="txn-detail-currency">Rs.</span>
              <span className="txn-detail-amount">
                {Number(transaction.amount || 0).toLocaleString('en-PK')}
              </span>
            </div>

            <div className="txn-detail-badges">
              <span className={`pill ${isIncome ? 'income-pill' : 'expense-pill'}`}>
                {isIncome ? 'Income' : 'Expense'}
              </span>
              <span className="category-tag">
                <CategoryIcon size={12} strokeWidth={2} style={{ marginRight: 4 }} />
                {category.label}
              </span>
            </div>
          </div>

          {/* Core Metadata Grid */}
          <div className="txn-meta-grid">
            <div className="txn-meta-item">
              <div className="txn-meta-icon"><Calendar size={15} strokeWidth={1.8} /></div>
              <div>
                <small>DATE & TIME</small>
                <strong>{formattedDate}</strong>
                {formattedTime && (
                  <span className="txn-meta-sub">
                    <Clock size={11} strokeWidth={1.8} style={{ display: 'inline', marginRight: 3 }} />
                    {formattedTime} {relativeTime ? `(${relativeTime})` : ''}
                  </span>
                )}
              </div>
            </div>

            <div className="txn-meta-item">
              <div className="txn-meta-icon"><User size={15} strokeWidth={1.8} /></div>
              <div>
                <small>RECORDED BY</small>
                <strong>{transaction.createdBy || 'Unknown'}</strong>
                <span className="txn-meta-sub">User who added this transaction</span>
              </div>
            </div>

            <div className="txn-meta-item">
              <div className="txn-meta-icon"><Wallet size={15} strokeWidth={1.8} /></div>
              <div>
                <small>CASHBOOK</small>
                <strong>{cashbookName}</strong>
                <span className="txn-meta-sub">In workspace: {businessName}</span>
              </div>
            </div>

            <div className="txn-meta-item">
              <div className="txn-meta-icon"><Layers size={15} strokeWidth={1.8} /></div>
              <div>
                <small>TRANSACTION ID</small>
                <div className="txn-id-row">
                  <code>{transaction.id || 'N/A'}</code>
                  {transaction.id && (
                    <button
                      type="button"
                      className="txn-copy-btn"
                      title="Copy transaction ID"
                      onClick={() => copyText(transaction.id, 'id')}
                    >
                      {copiedField === 'id' ? <Check size={12} color="#18835c" /> : <Copy size={12} />}
                    </button>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Remarks / Description Card */}
          <div className="txn-remarks-card">
            <div className="txn-remarks-header">
              <small>NOTE / REMARKS</small>
              {transaction.remarks && (
                <button
                  type="button"
                  className="txn-copy-btn"
                  title="Copy note"
                  onClick={() => copyText(transaction.remarks ?? '', 'remarks')}
                >
                  {copiedField === 'remarks' ? (
                    <span style={{ color: '#18835c', fontSize: 11, display: 'inline-flex', alignItems: 'center', gap: 3 }}>
                      <Check size={12} /> Copied
                    </span>
                  ) : (
                    <span style={{ fontSize: 11, display: 'inline-flex', alignItems: 'center', gap: 3 }}>
                      <Copy size={12} /> Copy
                    </span>
                  )}
                </button>
              )}
            </div>
            <p className="txn-remarks-text">
              {transaction.remarks || <em className="muted">No description or remarks provided.</em>}
            </p>
          </div>

          {/* Receipt Attachment Card */}
          <div className="txn-attachment-card">
            <div className="txn-attachment-header">
              <span className="txn-attachment-title">
                <Paperclip size={15} strokeWidth={1.8} />
                <strong>Receipt Attachment</strong>
              </span>
              {hasAttachment && (
                <button
                  type="button"
                  className="button secondary-action"
                  style={{ minHeight: 30, height: 30, padding: '0 10px', fontSize: 11 }}
                  onClick={() => onViewAttachment(transaction)}
                >
                  <ExternalLink size={12} strokeWidth={1.8} /> View full receipt
                </button>
              )}
            </div>

            {hasAttachment ? (
              <div className="txn-attachment-info">
                <div className="txn-attachment-pill">
                  <span className="file-badge">FILE</span>
                  <span className="file-name">{transaction.fileName || 'Attached Receipt'}</span>
                </div>
                <p className="txn-attachment-hint">
                  Click &ldquo;View full receipt&rdquo; to preview, zoom, or download the original copy.
                </p>
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
            <div style={{ display: 'flex', gap: 8 }}>
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
