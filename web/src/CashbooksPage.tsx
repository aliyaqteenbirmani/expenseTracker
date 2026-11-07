import { useMemo, useState } from 'react'
import {
  ArrowLeft,
  ArrowRight,
  BookOpen,
  LayoutGrid,
  Pencil,
  Plus,
  ReceiptText,
  Search,
  Trash2,
  Wallet
} from 'lucide-react'
import type { Cashbook } from './App'

type Props = {
  books: Cashbook[]
  selectedBook: Cashbook | null
  businessName: string
  activeBookTransactionsCount: number
  canCreate?: boolean
  canEdit?: boolean
  canDelete?: boolean
  onSelectBook: (book: Cashbook) => void
  onOpenTransactions: (book: Cashbook) => void
  onAddCashbook: () => void
  onEditCashbook: (book: Cashbook) => void
  onDeleteCashbook: (book: Cashbook) => void
  onBack: () => void
  onToast: (message: string) => void
}

export default function CashbooksPage({
  books,
  selectedBook,
  businessName,
  activeBookTransactionsCount,
  canCreate = true,
  canEdit = true,
  canDelete = true,
  onSelectBook,
  onOpenTransactions,
  onAddCashbook,
  onEditCashbook,
  onDeleteCashbook,
  onBack,
  onToast
}: Props) {
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<'all' | 'active' | 'other'>('all')

  const filteredBooks = useMemo(() => {
    return books.filter(b => {
      const name = (b.cashBookName ?? b.name ?? '').toLowerCase()
      const matchesSearch = !search.trim() || name.includes(search.trim().toLowerCase())
      const isActive = selectedBook?.id === b.id

      if (!matchesSearch) return false
      if (filter === 'active') return isActive
      if (filter === 'other') return !isActive
      return true
    })
  }, [books, search, filter, selectedBook?.id])

  return (
    <section className="cashbooks-page">
      {/* Heading Header */}
      <div className="history-heading">
        <div>
          <button className="history-back" onClick={onBack}>
            <ArrowLeft size={14} strokeWidth={2} /> Back to overview
          </button>
          <div className="eyebrow">WORKSPACE CASHBOOKS</div>
          <h1>Cashbooks Management</h1>
          <p className="muted">
            Manage and switch between books for <strong>{businessName}</strong>.
          </p>
        </div>
        {canCreate && (
          <button className="button primary" onClick={onAddCashbook}>
            <Plus size={16} strokeWidth={2} /> Create cashbook
          </button>
        )}
      </div>

      {/* Summary KPI Cards */}
      <div className="cashbooks-kpi-grid">
        <div className="cashbook-kpi-card">
          <div className="kpi-icon icon-books">
            <LayoutGrid size={18} strokeWidth={1.8} />
          </div>
          <div className="kpi-info">
            <small>TOTAL CASHBOOKS</small>
            <strong>{books.length}</strong>
          </div>
        </div>

        <div className="cashbook-kpi-card">
          <div className="kpi-icon icon-active">
            <BookOpen size={18} strokeWidth={1.8} />
          </div>
          <div className="kpi-info">
            <small>CURRENT ACTIVE BOOK</small>
            <strong title={selectedBook?.cashBookName ?? selectedBook?.name ?? 'None'}>
              {selectedBook?.cashBookName ?? selectedBook?.name ?? 'None selected'}
            </strong>
          </div>
        </div>

        <div className="cashbook-kpi-card">
          <div className="kpi-icon icon-txns">
            <ReceiptText size={18} strokeWidth={1.8} />
          </div>
          <div className="kpi-info">
            <small>ENTRIES IN ACTIVE BOOK</small>
            <strong>{activeBookTransactionsCount}</strong>
          </div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="cashbooks-toolbar">
        <div className="cashbooks-search-box">
          <Search size={15} strokeWidth={1.8} />
          <input
            type="text"
            placeholder="Search cashbooks by name…"
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>

        <div className="cashbooks-filter-pills">
          <button
            type="button"
            className={filter === 'all' ? 'active' : ''}
            onClick={() => setFilter('all')}
          >
            All ({books.length})
          </button>
          <button
            type="button"
            className={filter === 'active' ? 'active' : ''}
            onClick={() => setFilter('active')}
          >
            Active
          </button>
          <button
            type="button"
            className={filter === 'other' ? 'active' : ''}
            onClick={() => setFilter('other')}
          >
            Other books
          </button>
        </div>
      </div>

      {/* Cashbooks Grid */}
      {filteredBooks.length > 0 ? (
        <div className="cashbooks-cards-grid">
          {filteredBooks.map((b, i) => {
            const isActive = selectedBook?.id === b.id
            const bookName = b.cashBookName ?? b.name ?? 'Cashbook'

            return (
              <div key={b.id} className={`cashbook-card ${isActive ? 'is-active' : ''}`}>
                <div className="cashbook-card-header">
                  <div className={`book-mark mark-${i % 3}`}>
                    <BookOpen size={18} strokeWidth={1.8} />
                  </div>
                  {isActive ? (
                    <span className="cashbook-active-pill">
                      <span className="dot" /> Active
                    </span>
                  ) : (
                    <span className="cashbook-idle-pill">Standard</span>
                  )}
                </div>

                <div className="cashbook-card-body">
                  <h3>{bookName}</h3>
                  <span className="cashbook-meta">
                    <Wallet size={12} strokeWidth={1.8} /> Business cashbook
                  </span>
                </div>

                <div className="cashbook-card-footer">
                  <div className="cashbook-card-primary-action">
                    {isActive ? (
                      <button
                        type="button"
                        className="button primary small full"
                        onClick={() => onOpenTransactions(b)}
                      >
                        Transactions <ArrowRight size={13} strokeWidth={2} />
                      </button>
                    ) : (
                      <button
                        type="button"
                        className="button secondary-action small full"
                        onClick={() => {
                          onSelectBook(b)
                          onToast(`Activated cashbook "${bookName}"`)
                        }}
                      >
                        Set as Active
                      </button>
                    )}
                  </div>

                  {(canEdit || canDelete) && (
                    <div className="cashbook-card-icon-actions">
                      {canEdit && (
                        <button
                          type="button"
                          className="book-action"
                          title="Rename cashbook"
                          aria-label={`Rename ${bookName}`}
                          onClick={() => onEditCashbook(b)}
                        >
                          <Pencil size={14} strokeWidth={1.8} />
                        </button>
                      )}
                      {canDelete && (
                        <button
                          type="button"
                          className="book-action delete-action"
                          title="Delete cashbook"
                          aria-label={`Delete ${bookName}`}
                          onClick={() => onDeleteCashbook(b)}
                        >
                          <Trash2 size={14} strokeWidth={1.8} />
                        </button>
                      )}
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      ) : (
        <div className="cashbooks-empty-state">
          <BookOpen size={36} strokeWidth={1.4} style={{ color: '#8ca194', marginBottom: 12 }} />
          <h3>{search ? 'No matching cashbooks' : 'No cashbooks found'}</h3>
          <p className="muted">
            {search
              ? `No cashbook matched your search "${search}". Try clearing the search or typing a different keyword.`
              : 'Get started by creating your first cashbook to record money coming in and going out.'}
          </p>
          {search ? (
            <button className="button secondary-action" onClick={() => setSearch('')}>
              Clear search
            </button>
          ) : (
            canCreate && (
              <button className="button primary" onClick={onAddCashbook}>
                <Plus size={15} strokeWidth={2} /> Create your first cashbook
              </button>
            )
          )}
        </div>
      )}
    </section>
  )
}
