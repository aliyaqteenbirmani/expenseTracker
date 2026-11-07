import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import {
  Keyboard,
  LayoutDashboard,
  ReceiptText,
  BookOpen,
  UsersRound,
  Receipt,
  ChartNoAxesCombined,
  Settings,
  Search,
  Plus,
} from 'lucide-react'

type Props = {
  onClose: () => void
  onNavigate: (
    destination: 'overview' | 'transactions' | 'cashbooks' | 'receipts' | 'reports' | 'members' | 'settings'
  ) => void
  onAction: (action: 'add-transaction' | 'add-receipt' | 'focus-search') => void
}

export default function KeyboardShortcutsDialog({ onClose, onNavigate, onAction }: Props) {
  const isMac = typeof navigator !== 'undefined' && /Mac|iPod|iPhone|iPad/.test(navigator.platform)
  const modifierKey = isMac ? '⌘' : 'Ctrl'

  const navigationShortcuts = [
    { keys: ['G', 'O'], label: 'Go to Overview', icon: LayoutDashboard, dest: 'overview' as const },
    { keys: ['G', 'T'], label: 'Go to Transactions', icon: ReceiptText, dest: 'transactions' as const },
    { keys: ['G', 'C'], label: 'Go to Cashbooks', icon: BookOpen, dest: 'cashbooks' as const },
    { keys: ['G', 'V'], label: 'Go to Receipts Vault', icon: Receipt, dest: 'receipts' as const },
    { keys: ['G', 'R'], label: 'Go to Reports', icon: ChartNoAxesCombined, dest: 'reports' as const },
    { keys: ['G', 'M'], label: 'Go to Members', icon: UsersRound, dest: 'members' as const },
    { keys: ['G', 'S'], label: 'Go to Settings', icon: Settings, dest: 'settings' as const },
  ]

  const actionShortcuts = [
    {
      keys: [modifierKey, 'K'],
      label: 'Command palette & search',
      icon: Search,
      action: 'focus-search' as const,
    },
    {
      keys: [modifierKey, 'B'],
      label: 'Toggle sidebar open / collapsed',
      icon: LayoutDashboard,
    },
    {
      keys: ['N'],
      label: 'New Transaction entry',
      icon: Plus,
      action: 'add-transaction' as const,
    },
    {
      keys: ['R'],
      label: 'Scan / Upload Receipt',
      icon: Receipt,
      action: 'add-receipt' as const,
    },
  ]

  return (
    <ResponsiveDialog
      open={true}
      onOpenChange={(open) => !open && onClose()}
      title={
        <div className="flex items-center gap-2">
          <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Keyboard className="size-4" />
          </div>
          <div>
            <div className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              Productivity
            </div>
            <div className="text-base font-semibold">Keyboard Shortcuts</div>
          </div>
        </div>
      }
      description="Navigate across Spendwise and trigger rapid actions with single-stroke keys."
    >
      <div className="space-y-5 pt-2">
        {/* Navigation Section */}
        <div className="space-y-2">
          <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Navigation Shortcuts
          </h4>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            {navigationShortcuts.map((item) => {
              const Icon = item.icon
              return (
                <button
                  key={item.label}
                  type="button"
                  onClick={() => {
                    onClose()
                    onNavigate(item.dest)
                  }}
                  className="flex items-center justify-between p-2 rounded-lg border border-border/80 bg-muted/20 hover:bg-muted/50 transition-colors text-left cursor-pointer"
                >
                  <div className="flex items-center gap-2">
                    <Icon className="size-3.5 text-muted-foreground" />
                    <span className="text-xs font-medium text-foreground">{item.label}</span>
                  </div>
                  <div className="flex items-center gap-1">
                    {item.keys.map((k) => (
                      <kbd
                        key={k}
                        className="rounded border bg-muted px-1.5 py-0.5 font-mono text-[10px] font-medium text-muted-foreground"
                      >
                        {k}
                      </kbd>
                    ))}
                  </div>
                </button>
              )
            })}
          </div>
        </div>

        {/* Action Section */}
        <div className="space-y-2">
          <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Quick Actions
          </h4>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            {actionShortcuts.map((item) => {
              const Icon = item.icon
              return (
                <button
                  key={item.label}
                  type="button"
                  onClick={() => {
                    if (item.action) {
                      onClose()
                      onAction(item.action)
                    }
                  }}
                  className="flex items-center justify-between p-2 rounded-lg border border-border/80 bg-muted/20 hover:bg-muted/50 transition-colors text-left cursor-pointer"
                >
                  <div className="flex items-center gap-2">
                    <Icon className="size-3.5 text-muted-foreground" />
                    <span className="text-xs font-medium text-foreground">{item.label}</span>
                  </div>
                  <div className="flex items-center gap-1">
                    {item.keys.map((k) => (
                      <kbd
                        key={k}
                        className="rounded border bg-muted px-1.5 py-0.5 font-mono text-[10px] font-medium text-muted-foreground"
                      >
                        {k}
                      </kbd>
                    ))}
                  </div>
                </button>
              )
            })}
          </div>
        </div>

        <div className="flex justify-end pt-2 border-t">
          <Button variant="outline" size="sm" onClick={onClose} className="h-8 text-xs">
            Done
          </Button>
        </div>
      </div>
    </ResponsiveDialog>
  )
}
