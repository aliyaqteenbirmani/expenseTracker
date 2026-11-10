import { useState } from 'react'
import { SidebarTrigger } from '@/components/ui/sidebar'
import { Separator } from '@/components/ui/separator'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from '@/components/ui/command'
import {
  HoverCard,
  HoverCardContent,
  HoverCardTrigger,
} from '@/components/ui/hover-card'
import { Button } from '@/components/ui/button'
import { RoleBadge } from '@/components/app/RoleBadge'
import { PermissionChip } from '@/components/app/PermissionChip'
import { NotificationsPopover } from '@/components/notifications/NotificationsPopover'
import { useTheme } from '@/providers/ThemeProvider'
import {
  BookOpen,
  ChevronsUpDown,
  Check,
  Plus,
  Search,
  Sun,
  Moon,
  ShieldCheck,
  Crown,
} from 'lucide-react'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import type { Cashbook } from '@/App'
import type { AppBusiness, AppUser } from './AppSidebar'

interface SiteHeaderProps {
  business: AppBusiness | null
  cashbooks: Cashbook[]
  activeCashbook: Cashbook | null
  onSelectCashbook: (cb: Cashbook) => void
  onAddCashbook: () => void
  currentView: string
  onNavigate: (view: string) => void
  isOwner: boolean
  canCreateCashbook: boolean
  canCreateTransaction: boolean
  canEditTransaction: boolean
  canDeleteTransaction: boolean
  token: string
  unreadInvitesCount?: number
  onInvitationProcessed?: (target?: { businessId?: string; cashbookId?: string }) => void
  onToast: (msg: string) => void
  onOpenCommand: () => void
  refreshTrigger?: number
  user?: AppUser | null
  onOpenProfile?: () => void
}

const pageTitles: Record<string, string> = {
  overview: 'Overview',
  transactions: 'Transactions',
  cashbooks: 'Cashbooks',
  members: 'Members & Access',
  receipts: 'Receipts Vault',
  reports: 'Reports',
  settings: 'Settings',
}

export function SiteHeader({
  business,
  cashbooks,
  activeCashbook,
  onSelectCashbook,
  onAddCashbook,
  currentView,
  onNavigate,
  isOwner,
  canCreateCashbook,
  canCreateTransaction,
  canEditTransaction,
  canDeleteTransaction,
  token,
  onInvitationProcessed,
  onToast,
  onOpenCommand,
  refreshTrigger,
  user,
  onOpenProfile,
}: SiteHeaderProps) {
  const { theme, switchTheme } = useTheme()
  const [comboboxOpen, setComboboxOpen] = useState(false)

  const activePageTitle = pageTitles[currentView] || 'Overview'
  const fullName = user ? `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() || user.email || 'User' : 'User'
  const initials = fullName.slice(0, 2).toUpperCase()

  return (
    <header className="sticky top-0 z-30 flex h-14 shrink-0 items-center gap-2 border-b border-border/80 bg-background/95 px-4 backdrop-blur-md">
      {/* 1. Sidebar Trigger + Separator */}
      <SidebarTrigger className="-ml-1 cursor-pointer" />
      <Separator orientation="vertical" className="mr-2 h-4" />

      {/* 2. Breadcrumbs */}
      <Breadcrumb className="hidden sm:flex">
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink
              onClick={() => onNavigate('overview')}
              className="cursor-pointer hover:text-foreground text-xs"
            >
              {business?.name || 'Workspace'}
            </BreadcrumbLink>
          </BreadcrumbItem>
          {activeCashbook && (
            <>
              <BreadcrumbSeparator />
              <BreadcrumbItem>
                <BreadcrumbLink
                  onClick={() => onNavigate('cashbooks')}
                  className="cursor-pointer hover:text-foreground text-xs"
                >
                  {activeCashbook.cashBookName ?? activeCashbook.name ?? 'Cashbook'}
                </BreadcrumbLink>
              </BreadcrumbItem>
            </>
          )}
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage className="font-semibold text-xs text-foreground">
              {activePageTitle}
            </BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      {/* 3. Cashbook Selector (combobox) */}
      {cashbooks.length > 0 && (
        <Popover open={comboboxOpen} onOpenChange={setComboboxOpen}>
          <PopoverTrigger
            render={(props) => (
              <Button
                {...props}
                variant="outline"
                size="sm"
                className="h-8 gap-1.5 px-2.5 text-xs font-normal border-border/80 shadow-none hover:bg-muted/60"
              >
                <BookOpen className="size-3.5 text-muted-foreground" />
                <span className="max-w-28 sm:max-w-36 truncate font-medium">
                  {activeCashbook?.cashBookName ?? activeCashbook?.name ?? 'Select Cashbook'}
                </span>
                <ChevronsUpDown className="size-3 opacity-50 ml-0.5" />
              </Button>
            )}
          />
          <PopoverContent className="w-56 p-0" align="start">
            <Command>
              <CommandInput placeholder="Search cashbook..." />
              <CommandList>
                <CommandEmpty>No cashbook found.</CommandEmpty>
                <CommandGroup heading="Cashbooks">
                  {cashbooks.map((cb) => (
                    <CommandItem
                      key={cb.id}
                      onSelect={() => {
                        onSelectCashbook(cb)
                        setComboboxOpen(false)
                      }}
                      className="flex items-center justify-between text-xs cursor-pointer"
                    >
                      <span className="truncate flex items-center gap-1.5">
                        <span className="truncate">{cb.cashBookName ?? cb.name ?? 'Cashbook'}</span>
                        {cb.isOwner === false && (
                          <span className="rounded bg-amber-500/10 px-1 py-0.5 text-[9px] font-medium text-amber-600 dark:text-amber-400 shrink-0">
                            Shared
                          </span>
                        )}
                      </span>
                      {cb.id === activeCashbook?.id && (
                        <Check className="size-3.5 text-primary" />
                      )}
                    </CommandItem>
                  ))}
                </CommandGroup>
                {canCreateCashbook && (
                  <>
                    <CommandSeparator />
                    <CommandGroup>
                      <CommandItem
                        onSelect={() => {
                          setComboboxOpen(false)
                          onAddCashbook()
                        }}
                        className="text-xs cursor-pointer"
                      >
                        <Plus className="size-3.5 mr-1" />
                        <span>New cashbook</span>
                      </CommandItem>
                    </CommandGroup>
                  </>
                )}
              </CommandList>
            </Command>
          </PopoverContent>
        </Popover>
      )}

      {/* 4. Role Badge with HoverCard permissions summary */}
      <HoverCard>
        <HoverCardTrigger className="cursor-help inline-flex items-center">
          <RoleBadge role={isOwner ? 'owner' : 'member'} size="sm" />
        </HoverCardTrigger>
        <HoverCardContent side="bottom" align="start" className="w-72 p-3 space-y-2">
          <div className="flex items-center gap-2">
            {isOwner ? (
              <Crown className="size-4 text-amber-500" />
            ) : (
              <ShieldCheck className="size-4 text-blue-500" />
            )}
            <div>
              <p className="text-xs font-semibold">
                {isOwner ? 'Cashbook Owner' : 'Invited Member'}
              </p>
              <p className="text-[11px] text-muted-foreground">
                {isOwner
                  ? 'Full administrative control over this cashbook & members'
                  : 'Owner-controlled permissions assigned to you'}
              </p>
            </div>
          </div>
          <Separator />
          <div className="space-y-1">
            <p className="text-[11px] font-medium text-muted-foreground">Your permissions:</p>
            <div className="flex flex-wrap gap-1">
              <PermissionChip kind="view" size="sm" />
              {canCreateTransaction && <PermissionChip kind="add" size="sm" />}
              {canEditTransaction && <PermissionChip kind="edit" size="sm" />}
              {canDeleteTransaction && <PermissionChip kind="delete" size="sm" />}
            </div>
          </div>
        </HoverCardContent>
      </HoverCard>

      {/* Spacer */}
      <div className="flex-1" />

      {/* 5. Search Trigger (Ctrl+K) */}
      <Button
        variant="outline"
        size="sm"
        onClick={onOpenCommand}
        className="h-8 gap-2 px-3 text-xs text-muted-foreground border-border/80 shadow-none hover:bg-muted/60"
      >
        <Search className="size-3.5" />
        <span className="hidden md:inline">Search transactions...</span>
        <kbd className="pointer-events-none ml-1 hidden rounded border bg-muted px-1.5 font-mono text-[10px] font-medium opacity-100 sm:inline-block">
          ⌘K
        </kbd>
      </Button>

      {/* 6. Notifications Popover */}
      <NotificationsPopover
        token={token}
        onInvitationProcessed={onInvitationProcessed}
        onToast={onToast}
        refreshTrigger={refreshTrigger}
      />

      {/* 7. Theme Toggle with View Transitions circular reveal */}
      <Button
        variant="ghost"
        size="icon"
        className="size-8 cursor-pointer"
        aria-label="Toggle theme"
        onClick={(e) => switchTheme(theme === 'dark' ? 'light' : 'dark', e)}
      >
        <Sun className="size-4 rotate-0 scale-100 transition-[transform,opacity] duration-(--duration-fast) ease-out dark:-rotate-90 dark:scale-0" />
        <Moon className="absolute size-4 rotate-90 scale-0 transition-[transform,opacity] duration-(--duration-fast) ease-out dark:rotate-0 dark:scale-100" />
      </Button>

      {/* 8. User Profile Button */}
      {onOpenProfile && (
        <Button
          variant="ghost"
          size="icon"
          className="size-8 rounded-lg cursor-pointer ml-1"
          aria-label="Account profile"
          onClick={onOpenProfile}
          title={`Profile: ${fullName}`}
        >
          <Avatar className="size-7 rounded-lg">
            <AvatarFallback className="rounded-lg bg-primary/10 text-primary font-semibold text-xs">
              {initials}
            </AvatarFallback>
          </Avatar>
        </Button>
      )}
    </header>
  )
}
