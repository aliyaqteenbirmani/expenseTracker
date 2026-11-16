# Splitwise Play Store App Analysis & Gap Audit

## 1. Executive Summary

This document performs an in-depth reverse engineering and UX/functional audit of the **Splitwise** app (as available on Google Play Store and iOS App Store) and compares it with our current **Spendwise** (`expenseTracker`) project.

Splitwise's worldwide success is driven by two key factors:
1. **Zero-friction Expense Entry**: Adding an expense takes less than 5 seconds using natural-language patterns ("Paid by **you** and split **equally**") and smart category heuristics.
2. **Crystal-Clear Personal Ledger**: Users do not care about raw accounting logs; they care about **"Who owes whom?"** and **"How much do I get back or need to pay?"**. Every screen immediately answers these two questions in bold green (lent/owed) and orange/red (borrowed/owe).

Currently, Spendwise has basic equal-split functionality and an overwhelming, dense UI. This document identifies every gap and outlines the transformation needed.

---

## 2. Play Store Splitwise: Core UX Architecture

### 2.1 Navigation & Information Hierarchy

```
┌────────────────────────────────────────────────────────┐
│                     SPLITWISE APP                      │
├──────────────┬──────────────┬─────────────┬────────────┤
│    GROUPS    │   FRIENDS    │  ACTIVITY   │  ACCOUNT   │
└──────────────┴──────────────┴─────────────┴────────────┘
```

1. **Groups Tab**:
   - Shows overall balance summary at top: *"Overall, you are owed $150.00"* or *"Overall, you owe $45.00"*.
   - List of all groups with customizable group avatars/icons.
   - Each group card displays:
     - Group name (e.g., "Trip to Northern Areas", "Flatmates 402").
     - User's specific net balance inside that group in green (+Rs. 4,200) or red (-Rs. 1,500) or gray ("settled up").
2. **Friends Tab**:
   - Direct 1-on-1 balances independent of groups, or aggregated across multiple shared groups.
3. **Activity Feed Tab**:
   - Universal chronological timeline of events (Expense added, updated, deleted, comments, settlements confirmed).
4. **Account Tab**:
   - User profile, currency preferences, passcodes, simplified debts toggle.
5. **Universal Floating Action Button (+)**:
   - Available on primary screens to trigger "Add an expense" instantly.

---

## 3. Splitwise "Add Expense" Flow: Step-by-Step Breakdown

In Splitwise, the "Add Expense" dialog is **the most refined flow** in the entire application.

```
┌─────────────────────────────────────────────────────────────────┐
│ [✕ Cancel]                  Add Expense                [✓ Save] │
├─────────────────────────────────────────────────────────────────┤
│ With you and: [Select Group or Friends ▾]                      │
├─────────────────────────────────────────────────────────────────┤
│    ┌───┐                                                        │
│    │🍽️ │  Enter description (e.g. "Dinner at Monal")            │
│    └───┘                                                        │
│    Rs. [ 0.00                     ]  <-- Large typography input│
├─────────────────────────────────────────────────────────────────┤
│   Paid by [ you ▾ ] and split [ equally ▾ ]                     │
├─────────────────────────────────────────────────────────────────┤
│   [📅 Today, Nov 16]   [📷 Add image/notes]                    │
└─────────────────────────────────────────────────────────────────┘
```

### Step 1: Context & Auto-Categorization
- Group or Friends pre-selected if initiated from inside a group; searchable selector if initiated from dashboard.
- As the user types the description (e.g. *"Uber"*, *"Dinner"*, *"Groceries"*, *"Electricity"*), an intelligent icon matcher automatically selects the category icon (Transport, Food, Groceries, Utilities).

### Step 2: High-Focus Amount Input
- Giant, clean numeral input with active currency prefix (`Rs.` or `$`).
- Direct numpad on mobile; auto-focus with numeric formatting on web/desktop.

### Step 3: Natural Sentence Interactive Bar
Splitwise does not present a complex form with 20 fields. It presents an interactive sentence:
> **"Paid by [you ▾] and split [equally ▾]"**

Both pills are clickable and open dedicated, specialized sheets:

#### Payer Sheet (`Paid by [...]`):
- **Single Payer**:
  - `You` (default)
  - Any group member (`Ali`, `Usman`, `Bilal`)
- **Multiple People Paid**:
  - Allows entering exact amounts paid by different people (e.g., Ali paid Rs. 3,000 and Bilal paid Rs. 2,000 for a Rs. 5,000 bill).

#### Split Method Sheet (`Split [...]`):
Splitwise provides **5 distinct split modes**:
1. **Equally (`=`)**:
   - Checkbox next to each member.
   - Shows live calculation: *"Rs. 1,000/person (3 people)"*.
2. **Exact Amounts (`123`)**:
   - Numerical input next to each member.
   - Live counter: *"Rs. 3,200 of Rs. 5,000 (Rs. 1,800 left to allocate)"*.
   - Save button disabled until remaining is exactly 0.00.
3. **Percentages (`%`)**:
   - Percentage input per member.
   - Live counter: *"80% of 100% (20% remaining)"*. Auto-converts to currency amounts.
4. **By Shares / Parts (`2:1:1`)**:
   - Useful for couples or families sharing rooms (e.g., Ali = 2 shares, Usman = 1 share, Bilal = 1 share).
   - Shows fractional division automatically.
5. **Adjustment (`+/-`)**:
   - Equal base split with adjustments (e.g., "Everyone splits equally, but Hamza pays Rs. 500 extra for his dessert").

---

## 4. Group Details & Feed Flow in Splitwise

### 4.1 Header & Overview
- Group Name & Cover Photo/Avatar.
- Prominent Net Balance Banner:
  - *"Overall, you are owed Rs. 4,500"* (Highlighted Emerald Green).
  - *"Overall, you owe Rs. 1,200"* (Highlighted Coral/Rose).
  - *"You are all settled up in this group"* (Clean Muted Gray).
- Three Core Action Buttons:
  - **[Settle up]**: Instant settlement drawer.
  - **[Balances]**: View who owes what to whom.
  - **[Charts / Totals]**: Total group spend and category breakdown.

### 4.2 Group Feed (The Transaction Stream)
Transactions are grouped chronologically by **Month & Year** (e.g. `NOVEMBER 2026`).

Each item is a 3-column micro-ledger card:
- **Column 1 (Date & Category Icon)**:
  - Clean pastel category pill (e.g. Utensils icon for Food, Shopping Bag for Groceries).
  - Date stamp (`Nov 15`).
- **Column 2 (Description & Payer)**:
  - Title: **Dinner at Monal** (bold).
  - Subtitle: `Ali paid Rs. 3,000` (muted secondary text).
- **Column 3 (Personal Impact — Right-Aligned)**:
  - If you paid and lent:
    - Label: `you lent` (text-xs uppercase or muted).
    - Amount: `+Rs. 1,500` (bold emerald green).
  - If someone else paid and you owe:
    - Label: `you borrowed` (text-xs uppercase or muted).
    - Amount: `-Rs. 750` (bold orange/red).
  - If you were not part of the split:
    - Label: `not involved` (muted gray).
- Settlements appear in the feed with a distinctive **Handshake / Settlement** card:
  - `Usman paid Ali Rs. 1,000` (with a green checkmark badge).

---

## 5. Current Spendwise vs. Splitwise: Detailed Gap Audit

| Feature / Area | Splitwise (Gold Standard) | Spendwise (Current State) | Severity |
| :--- | :--- | :--- | :--- |
| **Split Modes** | 5 modes: Equal, Exact amounts, Percentages, Shares/parts, Adjustments | Only 1 mode: Equal split across checked members | 🔴 Critical |
| **Add Expense UX** | Intuitive sentence bar (`Paid by X and split Y`), large currency input, quick save | Heavy standard form dialog with separate dropdowns and inputs | 🔴 Critical |
| **Payer Selection** | Single payer or multiple payers with split payments | Single dropdown only; multiple payers unsupported | 🟠 High |
| **Feed Readability** | 3-column personal ledger (`you lent +Rs. X`, `you borrowed -Rs. Y`), monthly groupings | Generic transaction cards lacking instant personal ledger clarity | 🔴 Critical |
| **Balance Visibility** | Prominent colored top banner: "You are owed X" or "You owe Y" | Buried inside stat cards and tabs; lacks instant recognition | 🟠 High |
| **Settle Up Flow** | 1-tap quick settle sheet with pre-calculated pairwise debt | Nested inside separate tab with multi-step dialogs | 🟠 High |
| **Debt Simplification** | Graph simplification algorithm reduces redundant payment loops | Manual individual pairwise settlements only | 🟡 Medium |
| **Code Architecture** | Modular components, isolated sheets, dedicated state machine | Monolithic 1,930-line `SplitGroupDetailsPage.tsx` file | 🔴 Critical (Maintainability) |

---

## 6. Target Transformation Goals

1. **Adopt Splitwise's Signature Add-Expense Flow**:
   - Replace the rigid dialog with the interactive sentence pattern (`Paid by [you] and split [equally]`).
   - Implement the full suite of Split Methods: **Equal**, **Exact Amounts**, **Percentages**, and **Shares**.
2. **Transform the Group Page into an Intuitive Personal Ledger**:
   - Re-skin the expense feed to feature Month Dividers, Category Badges, and explicit personal ledger indicators (`you lent`, `you borrowed`, `not involved`).
3. **Elevate Group Financial Summary**:
   - Prominent, clean hero banner displaying the user's net position in high-contrast tokens.
4. **Refactor Codebase to Maintainable Modularity**:
   - Break `SplitGroupDetailsPage.tsx` down into focused, animated shadcn-compliant components.
