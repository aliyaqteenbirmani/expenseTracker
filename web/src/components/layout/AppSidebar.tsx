import { motion } from 'motion/react'
import { spring } from '@/lib/motion'
import { AnimatedNumber } from '@/components/motion/animated-number'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from '@/components/ui/sidebar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuShortcut,
  DropdownMenuTrigger,
  DropdownMenuSub,
  DropdownMenuSubTrigger,
  DropdownMenuSubContent,
} from '@/components/ui/dropdown-menu'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import {
  LayoutDashboard,
  ReceiptText,
  BookOpen,
  Users,
  ChartColumn,
  Settings,
  ChevronsUpDown,
  Plus,
  LogOut,
  User,
  Keyboard,
  Sun,
  Moon,
  Laptop,
  Check,
  Building2,
} from 'lucide-react'
import { useTheme } from '@/providers/ThemeProvider'

export interface AppBusiness {
  id: string
  name: string
  category?: string
  description?: string
  fileUrl?: string
  isOwner?: boolean
}

export interface AppUser {
  id?: string
  firstName?: string
  lastName?: string
  email?: string
}

interface AppSidebarProps {
  businesses: AppBusiness[]
  selectedBusiness: AppBusiness | null
  onSelectBusiness: (biz: AppBusiness) => void
  onCreateBusiness: () => void
  currentView: string
  onNavigate: (view: string) => void
  user: AppUser
  onLogout: () => void
  onOpenProfile: () => void
  onOpenShortcuts: () => void
  unreadInvitesCount?: number
}

export function AppSidebar({
  businesses,
  selectedBusiness,
  onSelectBusiness,
  onCreateBusiness,
  currentView,
  onNavigate,
  user,
  onLogout,
  onOpenProfile,
  onOpenShortcuts,
  unreadInvitesCount = 0,
}: AppSidebarProps) {
  const { setTheme } = useTheme()
  const fullName = `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() || user.email || 'User'
  const initials = fullName.slice(0, 2).toUpperCase()

  const navItems = [
    { id: 'overview', label: 'Overview', icon: LayoutDashboard },
    { id: 'transactions', label: 'Transactions', icon: ReceiptText },
    { id: 'cashbooks', label: 'Cashbooks', icon: BookOpen },
    {
      id: 'members',
      label: 'Members & Access',
      icon: Users,
      badge: unreadInvitesCount > 0 ? unreadInvitesCount : undefined,
    },
    { id: 'receipts', label: 'Receipts Vault', icon: ReceiptText },
    { id: 'reports', label: 'Reports', icon: ChartColumn },
    { id: 'settings', label: 'Settings', icon: Settings },
  ]

  return (
    <Sidebar variant="inset" collapsible="icon">
      {/* 1. Header: Workspace Switcher (sidebar-07 pattern) */}
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger
                render={(props) => (
                  <SidebarMenuButton
                    {...props}
                    size="lg"
                    className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
                  >
                    <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                      {selectedBusiness?.fileUrl ? (
                        <img
                          src={selectedBusiness.fileUrl}
                          alt={selectedBusiness.name}
                          className="size-full rounded-lg object-cover"
                        />
                      ) : (
                        <Building2 className="size-4" />
                      )}
                    </div>
                    <div className="grid flex-1 text-left text-sm leading-tight">
                      <span className="truncate font-semibold">
                        {selectedBusiness?.name || 'My Workspace'}
                      </span>
                      <span className="truncate text-xs text-muted-foreground">
                        {selectedBusiness?.isOwner ? 'Owner' : 'Member'} · Free
                      </span>
                    </div>
                    <ChevronsUpDown className="ml-auto size-4" />
                  </SidebarMenuButton>
                )}
              />
              <DropdownMenuContent
                className="w-(--radix-dropdown-menu-trigger-width) min-w-56 rounded-lg"
                align="start"
                side="bottom"
                sideOffset={4}
              >
                <DropdownMenuLabel className="text-xs text-muted-foreground">
                  Workspaces
                </DropdownMenuLabel>
                {businesses.map((biz, index) => (
                  <DropdownMenuItem
                    key={biz.id}
                    onClick={() => onSelectBusiness(biz)}
                    className="gap-2 p-2"
                  >
                    <div className="flex size-6 items-center justify-center rounded-sm border bg-background text-xs font-semibold">
                      {biz.name.slice(0, 1).toUpperCase()}
                    </div>
                    <span className="flex-1 truncate">{biz.name}</span>
                    {biz.id === selectedBusiness?.id && <Check className="size-4 text-primary" />}
                    {index < 9 && <DropdownMenuShortcut>⌘{index + 1}</DropdownMenuShortcut>}
                  </DropdownMenuItem>
                ))}
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={onCreateBusiness} className="gap-2 p-2 cursor-pointer">
                  <div className="flex size-6 items-center justify-center rounded-md border border-dashed bg-background">
                    <Plus className="size-4" />
                  </div>
                  <span className="font-medium text-muted-foreground">Create business</span>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>

      {/* 2. Content: Workspace Navigation */}
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Workspace</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {navItems.map((item) => {
                const Icon = item.icon
                const isActive = currentView === item.id
                return (
                  <SidebarMenuItem key={item.id}>
                    <SidebarMenuButton
                      isActive={isActive}
                      tooltip={item.label}
                      onClick={() => onNavigate(item.id)}
                      className="relative cursor-pointer transition-colors duration-(--duration-fast)"
                    >
                      {isActive && (
                        <motion.span
                          layoutId="nav-active"
                          transition={spring.snappy}
                          className="absolute inset-0 rounded-md bg-sidebar-accent -z-10"
                        />
                      )}
                      <Icon className="size-4" />
                      <span>{item.label}</span>
                    </SidebarMenuButton>
                    {item.badge !== undefined && (
                      <SidebarMenuBadge>
                        <AnimatedNumber value={item.badge} />
                      </SidebarMenuBadge>
                    )}
                  </SidebarMenuItem>
                )
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      {/* 3. Footer: User Menu (sidebar-07 NavUser pattern) */}
      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger
                render={(props) => (
                  <SidebarMenuButton
                    {...props}
                    size="lg"
                    className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
                  >
                    <Avatar className="size-8 rounded-lg">
                      <AvatarImage src="" alt={fullName} />
                      <AvatarFallback className="rounded-lg bg-primary/10 text-primary font-semibold text-xs">
                        {initials}
                      </AvatarFallback>
                    </Avatar>
                    <div className="grid flex-1 text-left text-sm leading-tight">
                      <span className="truncate font-semibold">{fullName}</span>
                      <span className="truncate text-xs text-muted-foreground">
                        {user.email || 'My account'}
                      </span>
                    </div>
                    <ChevronsUpDown className="ml-auto size-4" />
                  </SidebarMenuButton>
                )}
              />
              <DropdownMenuContent
                className="w-(--radix-dropdown-menu-trigger-width) min-w-56 rounded-lg"
                side="bottom"
                align="end"
                sideOffset={4}
              >
                <DropdownMenuLabel className="p-0 font-normal">
                  <div className="flex items-center gap-2 px-1 py-1.5 text-left text-sm">
                    <Avatar className="size-8 rounded-lg">
                      <AvatarFallback className="rounded-lg bg-primary/10 text-primary font-semibold text-xs">
                        {initials}
                      </AvatarFallback>
                    </Avatar>
                    <div className="grid flex-1 text-left text-sm leading-tight">
                      <span className="truncate font-semibold">{fullName}</span>
                      <span className="truncate text-xs text-muted-foreground">
                        {user.email}
                      </span>
                    </div>
                  </div>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={onOpenProfile} className="cursor-pointer">
                  <User className="size-4" />
                  <span>Account & Profile</span>
                </DropdownMenuItem>
                <DropdownMenuItem onClick={onOpenShortcuts} className="cursor-pointer">
                  <Keyboard className="size-4" />
                  <span>Keyboard Shortcuts</span>
                  <DropdownMenuShortcut>⌘/</DropdownMenuShortcut>
                </DropdownMenuItem>
                <DropdownMenuSub>
                  <DropdownMenuSubTrigger>
                    <Sun className="size-4 rotate-0 scale-100 transition-[transform,opacity] duration-(--duration-fast) ease-out dark:-rotate-90 dark:scale-0" />
                    <Moon className="absolute size-4 rotate-90 scale-0 transition-[transform,opacity] duration-(--duration-fast) ease-out dark:rotate-0 dark:scale-100" />
                    <span>Theme</span>
                  </DropdownMenuSubTrigger>
                  <DropdownMenuSubContent>
                    <DropdownMenuItem onClick={() => setTheme('light')}>
                      <Sun className="size-4 mr-2" />
                      <span>Light</span>
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => setTheme('dark')}>
                      <Moon className="size-4 mr-2" />
                      <span>Dark</span>
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => setTheme('system')}>
                      <Laptop className="size-4 mr-2" />
                      <span>System</span>
                    </DropdownMenuItem>
                  </DropdownMenuSubContent>
                </DropdownMenuSub>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={onLogout} className="cursor-pointer text-destructive focus:text-destructive">
                  <LogOut className="size-4" />
                  <span>Sign out</span>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>

      <SidebarRail />
    </Sidebar>
  )
}
