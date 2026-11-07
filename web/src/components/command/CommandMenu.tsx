import { useState } from 'react'
import {
  CommandDialog,
  CommandInput,
  CommandList,
  CommandEmpty,
  CommandGroup,
  CommandItem,
  CommandSeparator,
} from '@/components/ui/command'
import {
  LayoutDashboard,
  BookOpen,
  Users,
  ReceiptText,
  ChartColumn,
  Plus,
  ArrowDownLeft,
  ArrowUpRight,
  UserPlus,
  Keyboard,
  Sun,
  Moon,
  Laptop,
  Check,
} from 'lucide-react'
import { useTheme } from '@/providers/ThemeProvider'
import type { Cashbook, Transaction } from '@/App'
import { formatMoney } from '@/lib/format'

interface CommandMenuProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onNavigate: (view: string) => void
  books: Cashbook[]
  activeBookId?: string
  onSelectBook: (book: Cashbook) => void
  onAddTransaction?: (type: 'cashin' | 'cashout') => void
  onAddCashbook?: () => void
  onInviteMember?: () => void
  onOpenShortcuts?: () => void
  transactions?: Transaction[]
  onSelectTransaction?: (txn: Transaction) => void
  canCreateTransaction?: boolean
  canCreateCashbook?: boolean
  isOwner?: boolean
}

export function CommandMenu({
  open,
  onOpenChange,
  onNavigate,
  books,
  activeBookId,
  onSelectBook,
  onAddTransaction,
  onAddCashbook,
  onInviteMember,
  onOpenShortcuts,
  transactions = [],
  onSelectTransaction,
  canCreateTransaction = true,
  canCreateCashbook = true,
  isOwner = true,
}: CommandMenuProps) {
  const { setTheme } = useTheme()
  const [search, setSearch] = useState('')

  const handleSelect = (callback: () => void) => {
    onOpenChange(false)
    callback()
  }

  const matchingTransactions = search.trim()
    ? transactions
        .filter((t) =>
          (t.remarks ?? '').toLowerCase().includes(search.toLowerCase())
        )
        .slice(0, 5)
    : []

  return (
    <CommandDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Command Palette"
      description="Search actions, pages, cashbooks and transactions"
    >
      <CommandInput
        placeholder="Type a command or search..."
        value={search}
        onValueChange={setSearch}
      />
      <CommandList>
        <CommandEmpty>No results found.</CommandEmpty>

        {matchingTransactions.length > 0 && (
          <CommandGroup heading="Transactions">
            {matchingTransactions.map((t) => (
              <CommandItem
                key={t.id}
                onSelect={() => handleSelect(() => onSelectTransaction?.(t))}
                className="flex items-center justify-between"
              >
                <div className="flex items-center gap-2 truncate">
                  {t.transactionType?.toLowerCase() === 'cashin' ? (
                    <ArrowDownLeft className="size-4 text-inflow shrink-0" />
                  ) : (
                    <ArrowUpRight className="size-4 text-outflow shrink-0" />
                  )}
                  <span className="truncate">{t.remarks || 'Untitled entry'}</span>
                </div>
                <span className="font-mono text-xs text-muted-foreground shrink-0">
                  {formatMoney(t.amount)}
                </span>
              </CommandItem>
            ))}
          </CommandGroup>
        )}

        <CommandGroup heading="Navigation">
          <CommandItem onSelect={() => handleSelect(() => onNavigate('overview'))}>
            <LayoutDashboard className="size-4" />
            <span>Dashboard / Overview</span>
          </CommandItem>
          <CommandItem onSelect={() => handleSelect(() => onNavigate('transactions'))}>
            <ReceiptText className="size-4" />
            <span>Transactions</span>
          </CommandItem>
          <CommandItem onSelect={() => handleSelect(() => onNavigate('cashbooks'))}>
            <BookOpen className="size-4" />
            <span>Cashbooks</span>
          </CommandItem>
          <CommandItem onSelect={() => handleSelect(() => onNavigate('members'))}>
            <Users className="size-4" />
            <span>Members & Access</span>
          </CommandItem>
          <CommandItem onSelect={() => handleSelect(() => onNavigate('receipts'))}>
            <ReceiptText className="size-4" />
            <span>Receipts Vault</span>
          </CommandItem>
          <CommandItem onSelect={() => handleSelect(() => onNavigate('reports'))}>
            <ChartColumn className="size-4" />
            <span>Financial Reports</span>
          </CommandItem>
        </CommandGroup>

        <CommandSeparator />

        <CommandGroup heading="Actions">
          {canCreateTransaction && (
            <>
              <CommandItem onSelect={() => handleSelect(() => onAddTransaction?.('cashin'))}>
                <ArrowDownLeft className="size-4 text-inflow" />
                <span>Add Cash In</span>
              </CommandItem>
              <CommandItem onSelect={() => handleSelect(() => onAddTransaction?.('cashout'))}>
                <ArrowUpRight className="size-4 text-outflow" />
                <span>Add Cash Out</span>
              </CommandItem>
            </>
          )}
          {canCreateCashbook && (
            <CommandItem onSelect={() => handleSelect(() => onAddCashbook?.())}>
              <Plus className="size-4" />
              <span>New Cashbook</span>
            </CommandItem>
          )}
          {isOwner && (
            <CommandItem onSelect={() => handleSelect(() => onInviteMember?.())}>
              <UserPlus className="size-4" />
              <span>Invite Teammate</span>
            </CommandItem>
          )}
        </CommandGroup>

        {books.length > 0 && (
          <>
            <CommandSeparator />
            <CommandGroup heading="Cashbooks">
              {books.map((b) => (
                <CommandItem
                  key={b.id}
                  onSelect={() => handleSelect(() => onSelectBook(b))}
                  className="flex items-center justify-between"
                >
                  <div className="flex items-center gap-2">
                    <BookOpen className="size-4 text-muted-foreground" />
                    <span>{b.cashBookName ?? b.name ?? 'Cashbook'}</span>
                  </div>
                  {b.id === activeBookId && <Check className="size-4 text-primary" />}
                </CommandItem>
              ))}
            </CommandGroup>
          </>
        )}

        <CommandSeparator />

        <CommandGroup heading="Preferences">
          <CommandItem onSelect={() => handleSelect(() => onOpenShortcuts?.())}>
            <Keyboard className="size-4" />
            <span>Keyboard Shortcuts</span>
          </CommandItem>
          <CommandItem onSelect={() => handleSelect(() => setTheme('light'))}>
            <Sun className="size-4" />
            <span>Light Theme</span>
          </CommandItem>
          <CommandItem onSelect={() => handleSelect(() => setTheme('dark'))}>
            <Moon className="size-4" />
            <span>Dark Theme</span>
          </CommandItem>
          <CommandItem onSelect={() => handleSelect(() => setTheme('system'))}>
            <Laptop className="size-4" />
            <span>System Theme</span>
          </CommandItem>
        </CommandGroup>
      </CommandList>
    </CommandDialog>
  )
}
