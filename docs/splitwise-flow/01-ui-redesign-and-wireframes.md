# Splitwise UI/UX Redesign & Wireframes

## 1. Overview & Design Principles

This document defines the user interface redesign for **Spendwise Split Expenses** inspired by the Splitwise mobile application, tailored to our project stack:
- **Framework**: React 19 + TypeScript strict + Vite
- **Styling**: Tailwind CSS v4 (`@theme inline`) + shadcn/ui tokens
- **Motion**: `motion/react` + project motion tokens (`duration.normal`, `ease.out`)
- **Tokens**: `inflow` (green for lent/owed to you), `outflow` (red/orange for borrowed/you owe), tabular numerals for financial precision.

---

## 2. Redesigned Add Expense Modal / Drawer

The redesign replaces the standard long-form modal with Splitwise’s focused, interactive sentence workflow.

### 2.1 Wireframe: Add Expense View

```
┌──────────────────────────────────────────────────────────────────┐
│  [✕ Cancel]                    Add Expense              [✓ Save] │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│   Group: [ Northern Tour 2026 ▾ ]                                │
│                                                                  │
│   ┌──────┐  Title                                                │
│   │  🍕  │ [ Dinner at Monal                          ]          │
│   └──────┘                                                       │
│                                                                  │
│   Amount                                                         │
│   ┌──────────────────────────────────────────────────────────┐   │
│   │ Rs.   3,600.00                                           │   │
│   └──────────────────────────────────────────────────────────┘   │
│   (Large 3xl tabular-nums font, clean border, auto-focused)      │
│                                                                  │
│   ┌──────────────────────────────────────────────────────────┐   │
│   │ Paid by  [ You ▾ ]  and split  [ Equally (=) ▾ ]         │   │
│   └──────────────────────────────────────────────────────────┘   │
│   (Interactive Sentence Bar - clicking pills opens modal/sheet)  │
│                                                                  │
│   ┌──────────────────────────────────────────────────────────┐   │
│   │ ℹ️ Rs. 1,200.00 / person  (3 members included)           │   │
│   └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│   ── Optional Details ─────────────────────────────────────────  │
│   📅 Date: [ Today, Nov 16, 2026 ]                               │
│   🏷️ Category: [ Food & Dining ▾ ]                               │
│   📝 Notes / Receipt: [ Add note or attach receipt... ]          │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 2.2 Interactive Sentence Bar Anatomy
```tsx
<div className="flex flex-wrap items-center justify-center gap-1.5 py-3 px-4 rounded-xl bg-muted/40 border border-border/60 text-sm">
  <span className="text-muted-foreground">Paid by</span>
  <Button
    variant="outline"
    size="sm"
    className="h-8 font-semibold text-primary border-primary/30 bg-background hover:bg-primary/5"
    onClick={() => setPayerModalOpen(true)}
  >
    {payerDisplayLabel} <ChevronDown className="ml-1 h-3.5 w-3.5 opacity-60" />
  </Button>
  <span className="text-muted-foreground">and split</span>
  <Button
    variant="outline"
    size="sm"
    className="h-8 font-semibold text-primary border-primary/30 bg-background hover:bg-primary/5"
    onClick={() => setSplitMethodModalOpen(true)}
  >
    {splitMethodDisplayLabel} <ChevronDown className="ml-1 h-3.5 w-3.5 opacity-60" />
  </Button>
</div>
```

---

## 3. Split Method Selector Modal / Sheet

When clicking the **"split [Equally (=) ▾]"** pill, an interactive dialog opens containing 4 distinct calculation tabs:

### 3.1 Tab 1: Equally (`=`)
```
┌──────────────────────────────────────────────────────────────────┐
│ Split Method                                            [✕ Close]│
├──────────────────────────────────────────────────────────────────┤
│  [ = Equally ]   [ 123 Exact ]   [ % Percent ]   [ 👥 Shares ]   │
├──────────────────────────────────────────────────────────────────┤
│  Select who was involved in this expense:                        │
│                                                                  │
│  [✓] Select All       [ ] Deselect All                           │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ [✓]  (Avatar) You (Ali)                     Rs. 1,200.00   │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ [✓]  (Avatar) Usman Khan                    Rs. 1,200.00   │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ [✓]  (Avatar) Bilal Ahmed                   Rs. 1,200.00   │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ [ ]  (Avatar) Hamza Tariq                          Rs. 0   │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  Summary: Split between 3 people (Rs. 1,200.00 / person)         │
│  [ Confirm Split ]                                               │
└──────────────────────────────────────────────────────────────────┘
```

### 3.2 Tab 2: Exact Amounts (`123`)
```
┌──────────────────────────────────────────────────────────────────┐
│  [ = Equally ]   [ 123 Exact ]   [ % Percent ]   [ 👥 Shares ]   │
├──────────────────────────────────────────────────────────────────┤
│  Specify the exact amount paid for each person:                  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ (Avatar) You (Ali)            Rs. [ 2,000.00             ] │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ (Avatar) Usman Khan           Rs. [ 1,000.00             ] │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ (Avatar) Bilal Ahmed          Rs. [   600.00             ] │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  Total: Rs. 3,600.00 of Rs. 3,600.00                             │
│  Status: ✅ Rs. 0.00 remaining (Balanced)                        │
│  [ Confirm Split ] (Enabled only when remaining == 0)            │
└──────────────────────────────────────────────────────────────────┘
```

### 3.3 Tab 3: Percentages (`%`)
```
┌──────────────────────────────────────────────────────────────────┐
│  [ = Equally ]   [ 123 Exact ]   [ % Percent ]   [ 👥 Shares ]   │
├──────────────────────────────────────────────────────────────────┤
│  Specify the percentage share for each person:                   │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ (Avatar) You (Ali)            [ 50 ] %    -> Rs. 1,800.00  │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ (Avatar) Usman Khan           [ 25 ] %    -> Rs.   900.00  │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ (Avatar) Bilal Ahmed          [ 25 ] %    -> Rs.   900.00  │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  Total: 100% of 100%                                             │
│  Status: ✅ 0% remaining                                         │
│  [ Confirm Split ]                                               │
└──────────────────────────────────────────────────────────────────┘
```

### 3.4 Tab 4: Shares / Parts (`👥`)
```
┌──────────────────────────────────────────────────────────────────┐
│  [ = Equally ]   [ 123 Exact ]   [ % Percent ]   [ 👥 Shares ]   │
├──────────────────────────────────────────────────────────────────┤
│  Assign proportional shares (e.g. 2 for couple, 1 for single):   │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ (Avatar) You (Family)         [ - ]  2  [ + ] -> 2/4 shares │  │
│  │                               (Rs. 1,800.00)               │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ (Avatar) Usman Khan           [ - ]  1  [ + ] -> 1/4 share  │  │
│  │                               (Rs.   900.00)               │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ (Avatar) Bilal Ahmed          [ - ]  1  [ + ] -> 1/4 share  │  │
│  │                               (Rs.   900.00)               │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  Total shares: 4 parts (Rs. 900.00 per share)                    │
│  [ Confirm Split ]                                               │
└──────────────────────────────────────────────────────────────────┘
```

---

## 4. Redesigned Group Details Page & Personal Ledger

The group view is redesigned to match Splitwise's high-efficiency ledger layout.

### 4.1 Wireframe: Group View & Transaction Feed

```
┌──────────────────────────────────────────────────────────────────┐
│ [← Groups]      Northern Tour 2026               [⚙ Settings]    │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ HERO NET BALANCE BANNER                                    │  │
│  │ Overall, you are owed                                      │  │
│  │ +Rs. 4,500.00                                              │  │
│  │ (inflow green badge • 4 active members)                    │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ [🤝 Settle Up]         [📊 Balances]       [📈 Totals]     │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ── NOVEMBER 2026 ───────────────────────────────────────────    │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ 📅 Nov 16   🍕 Dinner at Monal             you lent        │  │
│  │             You paid Rs. 3,600             +Rs. 2,400.00   │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ 📅 Nov 15   ⛽ Fuel at M2 Service          you borrowed    │  │
│  │             Usman paid Rs. 4,000           -Rs. 1,000.00   │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ 📅 Nov 14   🤝 Settlement                  Settled         │  │
│  │             Bilal paid you Rs. 1,500       ✓ Received      │  │
│  ├────────────────────────────────────────────────────────────┤  │
│  │ 📅 Nov 13   🏨 Hotel Room Upgrade          not involved    │  │
│  │             Hamza paid Rs. 5,000           Rs. 0.00        │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  [ + Add Expense Floating / Fixed CTA Button ]                   │
└──────────────────────────────────────────────────────────────────┘
```

### 4.2 Splitwise Feed Item Component Anatomy

```tsx
<div className="flex items-center justify-between p-3.5 hover:bg-muted/40 rounded-xl transition-colors cursor-pointer border border-transparent hover:border-border/60">
  {/* Left: Category Icon + Date */}
  <div className="flex items-center gap-3">
    <div className="flex flex-col items-center justify-center w-10 text-center">
      <span className="text-[10px] font-medium uppercase text-muted-foreground">NOV</span>
      <span className="text-base font-bold leading-none">16</span>
    </div>
    <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary">
      <CategoryIcon category={expense.category} className="w-5 h-5" />
    </div>
    <div>
      <h4 className="font-semibold text-sm leading-tight text-foreground">{expense.title}</h4>
      <p className="text-xs text-muted-foreground mt-0.5">
        {isUserPayer ? 'You paid ' : `${expense.payerName} paid `}
        <span className="font-medium text-foreground">{formatMoney(expense.amount)}</span>
      </p>
    </div>
  </div>

  {/* Right: Personal Financial Impact */}
  <div className="text-right">
    <span className="text-[11px] block uppercase font-medium tracking-wider text-muted-foreground">
      {ledgerState.label} {/* 'you lent' | 'you borrowed' | 'not involved' */}
    </span>
    <span
      className={cn(
        "font-mono font-semibold text-sm tabular-nums",
        ledgerState.type === 'lent' && "text-inflow",
        ledgerState.type === 'borrowed' && "text-outflow",
        ledgerState.type === 'none' && "text-muted-foreground"
      )}
    >
      {ledgerState.formattedAmount}
    </span>
  </div>
</div>
```

---

## 5. Responsive Adaptations (Mobile vs. Desktop)

1. **Mobile (< 768px)**:
   - Full-screen Drawer (`vaul` via `<ResponsiveDialog>`) for Add Expense and Split Method Selector.
   - Fixed Bottom Action Bar with prominent `+ Add Expense` button.
   - Sticky Hero Net Balance Banner so user never loses track of their financial standing.
2. **Desktop (≥ 768px)**:
   - Centered Responsive Dialog with keyboard shortcuts (`Enter` to submit, `Esc` to cancel, `1-4` to switch tabs).
   - Split layout: Left column = Group Activity Feed; Right column = Member Balances & Quick Settle summary.
