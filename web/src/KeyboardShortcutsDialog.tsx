import {
  BookOpen,
  ChartNoAxesCombined,
  Keyboard,
  LayoutDashboard,
  Plus,
  Receipt,
  ReceiptText,
  Search,
  Settings,
  Sparkles,
  UsersRound,
  X
} from 'lucide-react'

type Props = {
  onClose: () => void
  onNavigate: (destination: 'overview' | 'transactions' | 'cashbooks' | 'receipts' | 'reports' | 'members' | 'settings') => void
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
      label: 'Quick Search transactions',
      icon: Search,
      action: 'focus-search' as const
    },
    {
      keys: ['N'],
      label: 'New Transaction entry',
      icon: Plus,
      action: 'add-transaction' as const
    },
    {
      keys: ['R'],
      label: 'Scan / Upload Receipt',
      icon: Receipt,
      action: 'add-receipt' as const
    },
  ]

  const globalShortcuts = [
    {
      keys: ['?'],
      label: 'Toggle Keyboard Shortcuts',
      desc: 'Or press ' + modifierKey + ' + /'
    },
    {
      keys: ['Esc'],
      label: 'Close Active Modal / Drawer',
      desc: 'Dismisses open dialogs'
    }
  ]

  return (
    <div className="overlay" onClick={onClose} role="dialog" aria-modal="true">
      <div className="dialog shortcuts-dialog" onClick={e => e.stopPropagation()}>
        <header>
          <div>
            <div className="eyebrow">POWER USER GUIDE</div>
            <h2>
              <Keyboard size={18} strokeWidth={2} style={{ display: 'inline', verticalAlign: -2, marginRight: 6 }} />
              Keyboard Shortcuts
            </h2>
          </div>
          <button className="close" onClick={onClose} aria-label="Close shortcuts dialog">
            <X size={18} strokeWidth={1.8} />
          </button>
        </header>

        <div className="dialog-content shortcuts-content">
          <p className="shortcuts-intro muted">
            Navigate Spendwise and perform financial actions instantly without lifting your hands off the keyboard.
          </p>

          <div className="shortcuts-section">
            <h4 className="shortcuts-heading">QUICK ACTIONS</h4>
            <div className="shortcuts-list">
              {actionShortcuts.map(item => {
                const Icon = item.icon
                return (
                  <div
                    key={item.label}
                    className="shortcut-row interactive"
                    onClick={() => {
                      onClose()
                      onAction(item.action)
                    }}
                    role="button"
                    tabIndex={0}
                  >
                    <div className="shortcut-meta">
                      <span className="shortcut-icon"><Icon size={14} strokeWidth={1.8} /></span>
                      <span className="shortcut-label">{item.label}</span>
                    </div>
                    <div className="shortcut-keys">
                      {item.keys.map((k, i) => (
                        <span key={i}>
                          <kbd className="shortcut-kbd">{k}</kbd>
                          {i < item.keys.length - 1 && <span className="key-plus">+</span>}
                        </span>
                      ))}
                    </div>
                  </div>
                )
              })}
            </div>
          </div>

          <div className="shortcuts-section">
            <h4 className="shortcuts-heading">NAVIGATION (PRESS G THEN KEY)</h4>
            <div className="shortcuts-grid">
              {navigationShortcuts.map(item => {
                const Icon = item.icon
                return (
                  <div
                    key={item.label}
                    className="shortcut-row interactive"
                    onClick={() => {
                      onClose()
                      onNavigate(item.dest)
                    }}
                    role="button"
                    tabIndex={0}
                  >
                    <div className="shortcut-meta">
                      <span className="shortcut-icon"><Icon size={14} strokeWidth={1.8} /></span>
                      <span className="shortcut-label">{item.label}</span>
                    </div>
                    <div className="shortcut-keys">
                      <kbd className="shortcut-kbd">{item.keys[0]}</kbd>
                      <span className="key-then">then</span>
                      <kbd className="shortcut-kbd">{item.keys[1]}</kbd>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>

          <div className="shortcuts-section">
            <h4 className="shortcuts-heading">GENERAL</h4>
            <div className="shortcuts-list">
              {globalShortcuts.map(item => (
                <div key={item.label} className="shortcut-row">
                  <div className="shortcut-meta">
                    <span className="shortcut-label">{item.label}</span>
                    {item.desc && <small className="muted" style={{ marginLeft: 8, fontSize: 11 }}>{item.desc}</small>}
                  </div>
                  <div className="shortcut-keys">
                    {item.keys.map((k, i) => (
                      <kbd key={i} className="shortcut-kbd">{k}</kbd>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="shortcuts-footer-tip">
            <Sparkles size={14} strokeWidth={1.8} />
            <span>Pro tip: Press <code>?</code> anywhere outside of a text input to open this cheatsheet.</span>
          </div>
        </div>
      </div>
    </div>
  )
}
