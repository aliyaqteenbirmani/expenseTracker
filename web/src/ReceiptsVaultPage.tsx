import { useMemo, useState } from 'react'
import {
  ArrowDownLeft,
  ChevronDown,
  Download,
  ExternalLink,
  Eye,
  FileText,
  Filter,
  Plus,
  Receipt,
  Search,
  Sparkles
} from 'lucide-react'
import { getTransactionCategory, type Cashbook, type Transaction } from './App'

type Props = {
  transactions: Transaction[]
  token: string
  cashbookName: string
  businessName: string
  books?: Cashbook[]
  selectedBookId?: string
  onSelectBook?: (book: Cashbook) => void
  onBack: () => void
  onUploadReceipt: () => void
  onViewAttachment: (transaction: Transaction) => void
  onOpenDetail: (transaction: Transaction) => void
  onToast: (msg: string) => void
}

export default function ReceiptsVaultPage({
  transactions,
  cashbookName,
  businessName,
  books,
  selectedBookId,
  onSelectBook,
  onBack,
  onUploadReceipt,
  onViewAttachment,
  onOpenDetail,
  onToast
}: Props) {
  const [query, setQuery] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('all')
  const [sortBy, setSortBy] = useState<'date-desc' | 'date-asc' | 'amount-desc' | 'amount-asc'>('date-desc')

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  // Transactions that have an attachment
  const receiptTransactions = useMemo(() => {
    return transactions.filter(t => Boolean(t.filePath || t.fileName))
  }, [transactions])

  const filteredReceipts = useMemo(() => {
    return receiptTransactions
      .filter(t => {
        const text = (t.remarks ?? '').concat(' ', t.fileName ?? '').toLowerCase()
        const matchesQuery = !query.trim() || text.includes(query.trim().toLowerCase())
        const cat = getTransactionCategory(t)
        const matchesCategory = categoryFilter === 'all' || cat.label === categoryFilter
        return matchesQuery && matchesCategory
      })
      .sort((a, b) => {
        if (sortBy === 'date-desc') return (b.transactionDate ?? '').localeCompare(a.transactionDate ?? '')
        if (sortBy === 'date-asc') return (a.transactionDate ?? '').localeCompare(b.transactionDate ?? '')
        if (sortBy === 'amount-desc') return Number(b.amount || 0) - Number(a.amount || 0)
        if (sortBy === 'amount-asc') return Number(a.amount || 0) - Number(b.amount || 0)
        return 0
      })
  }, [receiptTransactions, query, categoryFilter, sortBy])

  const totalDocumentedSpend = useMemo(() => {
    return receiptTransactions.reduce((sum, t) => sum + Number(t.amount || 0), 0)
  }, [receiptTransactions])

  // Category summary counts
  const categoryCounts = useMemo(() => {
    const counts = new Map<string, number>()
    for (const t of receiptTransactions) {
      const cat = getTransactionCategory(t).label
      counts.set(cat, (counts.get(cat) ?? 0) + 1)
    }
    return counts
  }, [receiptTransactions])

  const categories = ['all', 'Food', 'Transport', 'Housing', 'Shopping', 'Health', 'Income', 'Other expense']

  function getFileUrl(filePath?: string) {
    if (!filePath) return null
    return filePath.startsWith('http') ? filePath : `${apiBase}${filePath.startsWith('/') ? '' : '/'}${filePath}`
  }

  function handleDownload(e: React.MouseEvent, t: Transaction) {
    e.stopPropagation()
    const url = getFileUrl(t.filePath)
    if (!url) {
      onViewAttachment(t)
      return
    }
    const a = document.createElement('a')
    a.href = url
    a.download = t.fileName || `receipt-${t.id}.jpg`
    a.target = '_blank'
    a.rel = 'noreferrer'
    document.body.appendChild(a)
    a.click()
    a.remove()
    onToast(`Downloading ${t.fileName || 'receipt'}`)
  }

  return (
    <section className="receipts-vault-page">
      {/* Top Header */}
      <div className="history-heading">
        <div>
          <button className="history-back" onClick={onBack}>
            <ArrowDownLeft size={14} strokeWidth={1.8} /> Back to overview
          </button>
          <div className="eyebrow">DOCUMENT REPOSITORY</div>
          <h1>Receipt &amp; Invoice Vault</h1>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
            {books && books.length > 1 ? (
              <select
                value={selectedBookId}
                onChange={e => {
                  const chosen = books.find(b => b.id === e.target.value)
                  if (chosen && onSelectBook) onSelectBook(chosen)
                }}
                className="vault-book-select"
              >
                {books.map(b => (
                  <option key={b.id} value={b.id}>
                    {b.cashBookName ?? b.name ?? 'Cashbook'}
                  </option>
                ))}
              </select>
            ) : (
              <span className="muted" style={{ fontSize: 11 }}>{cashbookName}</span>
            )}
            <span className="muted" style={{ fontSize: 11 }}>· {businessName}</span>
          </div>
        </div>

        <div className="reports-top-actions">
          <button
            type="button"
            className="button primary"
            onClick={onUploadReceipt}
            style={{ minHeight: 38, height: 38, padding: '0 15px', fontSize: 12 }}
          >
            <Plus size={15} strokeWidth={2} /> Scan / Upload Receipt
          </button>
        </div>
      </div>

      {/* 3 Metric Cards */}
      <div className="vault-stats-grid">
        <article className="vault-stat-card">
          <span className="vault-stat-icon green">
            <Receipt size={18} strokeWidth={2} />
          </span>
          <div>
            <small>TOTAL ARCHIVED RECEIPTS</small>
            <strong>{receiptTransactions.length}</strong>
            <span className="vault-stat-sub">Across active cashbook</span>
          </div>
        </article>

        <article className="vault-stat-card">
          <span className="vault-stat-icon blue">
            <Sparkles size={18} strokeWidth={2} />
          </span>
          <div>
            <small>DOCUMENTED SPEND VOLUME</small>
            <strong>Rs. {totalDocumentedSpend.toLocaleString('en-PK')}</strong>
            <span className="vault-stat-sub">Verified with attached proofs</span>
          </div>
        </article>

        <article className="vault-stat-card">
          <span className="vault-stat-icon orange">
            <Filter size={18} strokeWidth={2} />
          </span>
          <div>
            <small>CATEGORIES REPRESENTED</small>
            <strong>{categoryCounts.size}</strong>
            <span className="vault-stat-sub">Spending streams covered</span>
          </div>
        </article>
      </div>

      {/* Filter and Search Bar */}
      <div className="vault-filter-bar">
        <label className="vault-search">
          <Search size={15} strokeWidth={1.8} />
          <input
            placeholder="Search by remark, merchant, or filename..."
            value={query}
            onChange={e => setQuery(e.target.value)}
          />
          {query && (
            <button type="button" className="vault-search-clear" onClick={() => setQuery('')}>
              ✕
            </button>
          )}
        </label>

        <div className="vault-sort-select-wrap">
          <select
            value={sortBy}
            onChange={e => setSortBy(e.target.value as typeof sortBy)}
            aria-label="Sort receipts"
          >
            <option value="date-desc">Newest first</option>
            <option value="date-asc">Oldest first</option>
            <option value="amount-desc">Highest amount</option>
            <option value="amount-asc">Lowest amount</option>
          </select>
          <ChevronDown size={12} strokeWidth={2} className="vault-sort-arrow" />
        </div>
      </div>

      {/* Category Filter Pills */}
      <div className="vault-category-chips">
        {categories.map(cat => {
          const count = cat === 'all' ? receiptTransactions.length : (categoryCounts.get(cat) ?? 0)
          const isSelected = categoryFilter === cat
          return (
            <button
              key={cat}
              type="button"
              className={`vault-chip ${isSelected ? 'selected' : ''}`}
              onClick={() => setCategoryFilter(cat)}
            >
              <span>{cat === 'all' ? 'All Receipts' : cat}</span>
              <i>{count}</i>
            </button>
          )
        })}
      </div>

      {/* Receipts Grid */}
      {filteredReceipts.length > 0 ? (
        <div className="vault-grid">
          {filteredReceipts.map(t => {
            const isIncome = t.transactionType?.toLowerCase() === 'cashin'
            const cat = getTransactionCategory(t)
            const CatIcon = cat.Icon
            const fileUrl = getFileUrl(t.filePath)
            const isImage = fileUrl && /\.(jpg|jpeg|png|webp|gif|svg)$/i.test(fileUrl)
            const dateStr = t.transactionDate
              ? new Date(t.transactionDate).toLocaleDateString('en-US', {
                  month: 'short',
                  day: 'numeric',
                  year: 'numeric'
                })
              : 'Date missing'

            return (
              <div
                key={t.id}
                className="vault-card"
                onClick={() => onOpenDetail(t)}
                role="button"
                tabIndex={0}
                onKeyDown={e => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault()
                    onOpenDetail(t)
                  }
                }}
              >
                {/* Media Thumbnail Container */}
                <div className="vault-card-media">
                  {isImage ? (
                    <img
                      src={fileUrl}
                      alt={t.remarks || 'Receipt'}
                      className="vault-thumb-img"
                      loading="lazy"
                    />
                  ) : (
                    <div className="vault-doc-placeholder">
                      <FileText size={32} strokeWidth={1.5} color="#457659" />
                      <span className="doc-ext">
                        {t.fileName?.split('.').pop()?.toUpperCase() || 'DOC'}
                      </span>
                    </div>
                  )}

                  {/* Hover Quick Action Buttons */}
                  <div className="vault-media-actions" onClick={e => e.stopPropagation()}>
                    <button
                      type="button"
                      className="vault-media-btn"
                      title="View full receipt"
                      onClick={() => onViewAttachment(t)}
                    >
                      <Eye size={14} strokeWidth={2} />
                    </button>
                    <button
                      type="button"
                      className="vault-media-btn"
                      title="Download receipt"
                      onClick={e => handleDownload(e, t)}
                    >
                      <Download size={14} strokeWidth={2} />
                    </button>
                  </div>

                  <span className={`vault-amount-badge ${isIncome ? 'income' : 'expense'}`}>
                    {isIncome ? '+' : '−'} Rs. {Number(t.amount || 0).toLocaleString('en-PK')}
                  </span>
                </div>

                {/* Card Content Body */}
                <div className="vault-card-body">
                  <div className="vault-card-meta">
                    <span className="category-tag">
                      <CatIcon size={12} strokeWidth={2} style={{ marginRight: 3 }} />
                      {cat.label}
                    </span>
                    <small className="vault-date">{dateStr}</small>
                  </div>

                  <strong className="vault-title" title={t.remarks || 'Receipt'}>
                    {t.remarks || (isIncome ? 'Cash Inflow' : 'Cash Expense')}
                  </strong>

                  <div className="vault-card-foot">
                    <span className="vault-filename" title={t.fileName || 'Attached file'}>
                      {t.fileName || 'receipt.jpg'}
                    </span>
                    <span className="vault-inspect-hint">
                      Inspect <ExternalLink size={11} strokeWidth={2} />
                    </span>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      ) : (
        <div className="empty vault-empty">
          <span className="empty-receipt">
            <Receipt size={24} strokeWidth={1.8} />
          </span>
          <strong>{query || categoryFilter !== 'all' ? 'No matching receipts found' : 'No receipts archived yet'}</strong>
          <p>
            {query || categoryFilter !== 'all'
              ? 'Try adjusting your search terms or clearing the category filter.'
              : 'Scan or attach receipts when logging transactions to create a verifiable digital archive.'}
          </p>
          {!receiptTransactions.length && (
            <button type="button" className="button primary" onClick={onUploadReceipt} style={{ marginTop: 12 }}>
              <Plus size={15} strokeWidth={2} /> Scan your first receipt
            </button>
          )}
        </div>
      )}
    </section>
  )
}
