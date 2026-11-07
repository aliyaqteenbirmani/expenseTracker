import { useMemo, useState } from 'react'
import { PageHeader } from '@/components/app/PageHeader'
import { StatCard } from '@/components/app/StatCard'
import { Card, CardContent, CardFooter, CardHeader } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Gate } from '@/components/app/Gate'
import { EmptyState } from '@/components/app/EmptyState'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import {
  BookOpen,
  ArrowLeft,
  Plus,
  Search,
  MoreHorizontal,
  Pencil,
  Trash2,
  Check,
  ReceiptText,
  Building2,
  ArrowRight,
} from 'lucide-react'
import type { Cashbook } from '@/App'
import { cn } from '@/lib/utils'

interface CashbooksPageProps {
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

export function CashbooksPage({
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
  onToast,
}: CashbooksPageProps) {
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<'all' | 'active' | 'other'>('all')

  const filteredBooks = useMemo(() => {
    return books.filter((b) => {
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
    <div className="space-y-6 pb-12">
      <PageHeader
        title="Cashbooks"
        description={`Manage, switch and audit books for ${businessName}.`}
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
          <Gate
            allow={canCreate}
            reason="Cashbook creation restricted — ask the owner for access"
          >
            <Button size="sm" onClick={onAddCashbook} className="gap-1.5 h-8 text-xs">
              <Plus className="size-3.5" />
              <span>New Cashbook</span>
            </Button>
          </Gate>
        }
      />

      {/* KPI Strip */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <StatCard
          label="Total Cashbooks"
          value={books.length}
          icon={BookOpen}
          footer="Active within workspace"
        />
        <StatCard
          label="Active Cashbook"
          value={1}
          formatFn={() => selectedBook?.cashBookName ?? selectedBook?.name ?? 'None'}
          footer="Current selected context"
        />
        <StatCard
          label="Active Book Entries"
          value={activeBookTransactionsCount}
          icon={ReceiptText}
          footer="Transactions in active book"
        />
      </div>

      {/* Toolbar */}
      <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative flex-1 max-w-xs">
          <Search className="absolute left-2.5 top-2.5 size-3.5 text-muted-foreground" />
          <Input
            placeholder="Search cashbooks..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-8 h-8 text-xs"
          />
        </div>

        <ToggleGroup
          value={[filter]}
          onValueChange={(vals) => {
            const chosen = Array.isArray(vals) ? vals[0] : vals
            if (chosen) setFilter(chosen as 'all' | 'active' | 'other')
          }}
          className="border border-border/80 rounded-lg p-0.5"
        >
          <ToggleGroupItem value="all" className="text-xs h-7 px-2.5">
            All ({books.length})
          </ToggleGroupItem>
          <ToggleGroupItem value="active" className="text-xs h-7 px-2.5">
            Active
          </ToggleGroupItem>
          <ToggleGroupItem value="other" className="text-xs h-7 px-2.5">
            Other
          </ToggleGroupItem>
        </ToggleGroup>
      </div>

      {/* Cashbooks Cards Grid */}
      {filteredBooks.length === 0 ? (
        <EmptyState
          icon={BookOpen}
          title="No cashbooks found"
          description={
            search
              ? 'No cashbooks match your search filter.'
              : 'Create your first cashbook to start managing expenses and income.'
          }
          action={
            canCreate ? (
              <Button size="sm" onClick={onAddCashbook} className="gap-1.5">
                <Plus className="size-3.5" />
                <span>Create Cashbook</span>
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {filteredBooks.map((b) => {
            const isActive = selectedBook?.id === b.id
            const bookTitle = b.cashBookName ?? b.name ?? 'Cashbook'

            return (
              <Card
                key={b.id}
                className={cn(
                  'relative flex flex-col justify-between p-5 border-border/80 shadow-xs transition-all duration-200 hover:shadow-md hover:-translate-y-0.5',
                  isActive && 'ring-2 ring-primary/40 bg-gradient-to-t from-primary/5 to-card'
                )}
              >
                <CardHeader className="p-0 pb-3 flex flex-row items-start justify-between space-y-0">
                  <div className="flex items-center gap-3">
                    <div
                      className={cn(
                        'flex size-10 items-center justify-center rounded-xl border',
                        isActive
                          ? 'border-primary/30 bg-primary/10 text-primary'
                          : 'border-border/80 bg-muted/60 text-muted-foreground'
                      )}
                    >
                      <BookOpen className="size-5 stroke-[1.75]" />
                    </div>
                    <div>
                      <h3 className="font-semibold text-base text-foreground tracking-tight">
                        {bookTitle}
                      </h3>
                      <p className="text-xs text-muted-foreground flex items-center gap-1">
                        <Building2 className="size-3" />
                        <span>{businessName}</span>
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-1.5">
                    {isActive ? (
                      <Badge className="bg-primary text-primary-foreground text-[10px] gap-1 px-2 py-0.5">
                        <span className="size-1.5 rounded-full bg-emerald-400 animate-pulse" />
                        Active
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="text-[10px] text-muted-foreground">
                        Standard
                      </Badge>
                    )}

                    <DropdownMenu>
                      <DropdownMenuTrigger
                        render={(props) => (
                          <Button
                            {...props}
                            variant="ghost"
                            size="icon"
                            className="size-7"
                            aria-label="Cashbook options"
                          >
                            <MoreHorizontal className="size-4" />
                          </Button>
                        )}
                      />
                      <DropdownMenuContent align="end" className="w-44">
                        {!isActive && (
                          <DropdownMenuItem
                            onClick={() => {
                              onSelectBook(b)
                              onToast(`Switched active cashbook to "${bookTitle}"`)
                            }}
                          >
                            <Check className="size-4 mr-2" />
                            <span>Set as active</span>
                          </DropdownMenuItem>
                        )}
                        <DropdownMenuItem onClick={() => onOpenTransactions(b)}>
                          <ReceiptText className="size-4 mr-2" />
                          <span>View transactions</span>
                        </DropdownMenuItem>
                        {canEdit && (
                          <DropdownMenuItem onClick={() => onEditCashbook(b)}>
                            <Pencil className="size-4 mr-2" />
                            <span>Rename cashbook</span>
                          </DropdownMenuItem>
                        )}
                        {canDelete && (
                          <>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem
                              className="text-destructive focus:text-destructive"
                              onClick={() => onDeleteCashbook(b)}
                            >
                              <Trash2 className="size-4 mr-2" />
                              <span>Delete cashbook</span>
                            </DropdownMenuItem>
                          </>
                        )}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                </CardHeader>

                <CardContent className="p-0 py-2">
                  <div className="rounded-lg bg-muted/40 p-3 flex items-center justify-between text-xs">
                    <span className="text-muted-foreground">Recorded entries:</span>
                    <span className="font-mono font-medium">
                      {isActive ? activeBookTransactionsCount : '—'}
                    </span>
                  </div>
                </CardContent>

                <CardFooter className="p-0 pt-3 border-t flex items-center justify-between">
                  {!isActive ? (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-8 text-xs"
                      onClick={() => {
                        onSelectBook(b)
                        onToast(`Switched active cashbook to "${bookTitle}"`)
                      }}
                    >
                      Make Active
                    </Button>
                  ) : (
                    <span className="text-xs text-muted-foreground font-medium">
                      Currently Selected
                    </span>
                  )}
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 text-xs gap-1 hover:text-foreground"
                    onClick={() => onOpenTransactions(b)}
                  >
                    <span>Transactions</span>
                    <ArrowRight className="size-3.5" />
                  </Button>
                </CardFooter>
              </Card>
            )
          })}
        </div>
      )}
    </div>
  )
}
