import { useMemo, useState } from 'react'
import { PageHeader } from '@/components/app/PageHeader'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { EmptyState } from '@/components/app/EmptyState'
import {
  ArrowLeft,
  Search,
  Plus,
  Paperclip,
  Download,
  Eye,
  ExternalLink,
  BookOpen,
} from 'lucide-react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { getTransactionCategory, type Cashbook, type Transaction } from '@/App'
import { formatMoney, formatDate } from '@/lib/format'
import { cn } from '@/lib/utils'

interface ReceiptsVaultPageProps {
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

export function ReceiptsVaultPage({
  transactions,
  token,
  cashbookName,
  businessName,
  books,
  selectedBookId,
  onSelectBook,
  onBack,
  onUploadReceipt,
  onViewAttachment,
  onOpenDetail,
  onToast,
}: ReceiptsVaultPageProps) {
  const [query, setQuery] = useState('')
  const [sortBy, setSortBy] = useState<'date-desc' | 'amount-desc'>('date-desc')

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  const receiptTransactions = useMemo(() => {
    return transactions.filter((t) => Boolean(t.filePath || t.fileName))
  }, [transactions])

  const filteredReceipts = useMemo(() => {
    return receiptTransactions
      .filter((t) => {
        const text = (t.remarks ?? '').concat(' ', t.fileName ?? '').toLowerCase()
        return !query.trim() || text.includes(query.trim().toLowerCase())
      })
      .sort((a, b) => {
        if (sortBy === 'amount-desc') return Number(b.amount || 0) - Number(a.amount || 0)
        const dateA = a.transactionDate ? new Date(a.transactionDate).getTime() : 0
        const dateB = b.transactionDate ? new Date(b.transactionDate).getTime() : 0
        return dateB - dateA
      })
  }, [receiptTransactions, query, sortBy])

  const totalReceiptsAmount = useMemo(() => {
    return filteredReceipts.reduce((sum, t) => sum + Number(t.amount || 0), 0)
  }, [filteredReceipts])

  const downloadFile = (e: React.MouseEvent, t: Transaction) => {
    e.stopPropagation()
    const url = `${apiBase}/api/transactions/${t.id}/attachment`
    fetch(url, { headers: { Authorization: `Bearer ${token}` } })
      .then((r) => r.blob())
      .then((blob) => {
        const blobUrl = window.URL.createObjectURL(blob)
        const link = document.createElement('a')
        link.href = blobUrl
        link.download = t.fileName || `receipt_${t.id}.jpg`
        link.click()
        window.URL.revokeObjectURL(blobUrl)
        onToast('Receipt downloaded')
      })
      .catch(() => onToast('Could not download file.'))
  }

  return (
    <div className="space-y-6 pb-12">
      <PageHeader
        title="Receipts Vault"
        description={`Audit and inspect all scanned attachments for ${cashbookName} (${businessName}).`}
        breadcrumb={
          <Button
            variant="ghost"
            size="sm"
            onClick={onBack}
            className="-ml-2 h-7 gap-1 text-xs text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="size-3.5" />
            <span>Back to Overview</span>
          </Button>
        }
        actions={
          <div className="flex flex-wrap items-center gap-2">
            {books && books.length > 1 && onSelectBook && (
              <Select
                value={selectedBookId}
                onValueChange={(val) => {
                  const b = books.find((x) => x.id === val)
                  if (b) onSelectBook(b)
                }}
              >
                <SelectTrigger className="h-8 text-xs w-44">
                  <BookOpen className="size-3.5 mr-1 text-muted-foreground" />
                  <SelectValue placeholder="Select cashbook" />
                </SelectTrigger>
                <SelectContent>
                  {books.map((b) => (
                    <SelectItem key={b.id} value={b.id} className="text-xs">
                      {b.cashBookName ?? b.name ?? 'Cashbook'}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}

            <Button size="sm" className="h-8 text-xs gap-1.5" onClick={onUploadReceipt}>
              <Plus className="size-3.5" />
              <span>Upload Receipt</span>
            </Button>
          </div>
        }
      />

      {/* Toolbar & Filter */}
      <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative flex-1 max-w-xs">
          <Search className="absolute left-2.5 top-2.5 size-3.5 text-muted-foreground" />
          <Input
            placeholder="Search receipts by notes or filename..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="pl-8 h-8 text-xs"
          />
        </div>

        <div className="flex items-center gap-2">
          <Select value={sortBy} onValueChange={(v) => setSortBy(v as any)}>
            <SelectTrigger className="h-8 text-xs w-36">
              <SelectValue placeholder="Sort order" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="date-desc" className="text-xs">
                Newest Date
              </SelectItem>
              <SelectItem value="amount-desc" className="text-xs">
                Highest Amount
              </SelectItem>
            </SelectContent>
          </Select>

          <Badge variant="outline" className="h-8 px-2.5 text-xs font-mono">
            {filteredReceipts.length} receipts · {formatMoney(totalReceiptsAmount)}
          </Badge>
        </div>
      </div>

      {/* Receipts Grid */}
      {filteredReceipts.length === 0 ? (
        <EmptyState
          icon={Paperclip}
          title="No receipts found"
          description={
            query
              ? 'No receipts match your search term.'
              : 'Add receipts with your transactions to store an audit-ready digital paper trail.'
          }
          action={
            <Button size="sm" onClick={onUploadReceipt} className="gap-1.5">
              <Plus className="size-3.5" />
              <span>Upload First Receipt</span>
            </Button>
          }
        />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-4">
          {filteredReceipts.map((t) => {
            const cat = getTransactionCategory(t)
            const isIncome = t.transactionType?.toLowerCase() === 'cashin'

            return (
              <Card
                key={t.id}
                className="group relative overflow-hidden border-border/80 shadow-xs transition-all duration-200 hover:shadow-md hover:-translate-y-0.5 cursor-pointer"
                onClick={() => onViewAttachment(t)}
              >
                {/* Thumbnail Header */}
                <div className="relative aspect-4/3 w-full bg-muted/60 overflow-hidden flex items-center justify-center border-b">
                  <div className="flex flex-col items-center justify-center text-muted-foreground gap-1.5">
                    <Paperclip className="size-8 stroke-[1.5]" />
                    <span className="text-[11px] font-mono max-w-[160px] truncate px-2">
                      {t.fileName || 'Receipt scan'}
                    </span>
                  </div>

                  {/* Quick Action Overlay on hover */}
                  <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
                    <Button
                      size="sm"
                      variant="secondary"
                      className="h-8 text-xs gap-1"
                      onClick={(e) => {
                        e.stopPropagation()
                        onViewAttachment(t)
                      }}
                    >
                      <Eye className="size-3.5" />
                      <span>View</span>
                    </Button>
                    <Button
                      size="sm"
                      variant="secondary"
                      className="h-8 size-8 p-0"
                      onClick={(e) => downloadFile(e, t)}
                      title="Download receipt"
                    >
                      <Download className="size-3.5" />
                    </Button>
                  </div>

                  <Badge
                    variant="outline"
                    className={cn(
                      'absolute top-2.5 right-2.5 font-mono text-[10px] bg-background/90 backdrop-blur-xs font-semibold',
                      isIncome ? 'text-inflow border-inflow/30' : 'text-outflow border-outflow/30'
                    )}
                  >
                    {isIncome ? '+ ' : '− '}
                    {formatMoney(Number(t.amount || 0))}
                  </Badge>
                </div>

                {/* Card Content Details */}
                <CardContent className="p-3.5 space-y-2">
                  <div className="flex items-center justify-between text-xs">
                    <Badge variant="secondary" className="text-[10px] px-1.5 py-0 font-normal">
                      {cat.label}
                    </Badge>
                    <span className="text-[11px] text-muted-foreground">
                      {formatDate(t.transactionDate)}
                    </span>
                  </div>

                  <p className="font-medium text-xs text-foreground truncate" title={t.remarks}>
                    {t.remarks || 'Untitled entry'}
                  </p>

                  <div className="flex items-center justify-between pt-1 border-t text-[11px] text-muted-foreground">
                    <span className="truncate">By {t.createdBy || 'Unknown'}</span>
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation()
                        onOpenDetail(t)
                      }}
                      className="inline-flex items-center gap-0.5 text-primary hover:underline"
                    >
                      <span>Entry</span>
                      <ExternalLink className="size-2.5" />
                    </button>
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      )}
    </div>
  )
}
