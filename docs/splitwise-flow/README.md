# Splitwise Flow & UI Overhaul Documentation

This directory contains the complete technical specification, UI wireframes, calculation architecture, and execution roadmap for transforming **Spendwise** (`expenseTracker`) into a seamless, Splitwise-grade experience.

## Documentation Index

| File | Description |
| :--- | :--- |
| **[00-splitwise-analysis-and-gap-audit.md](file:///c:/Users/LENOVO/source/repos/expenseTracker/docs/splitwise-flow/00-splitwise-analysis-and-gap-audit.md)** | Play Store Splitwise reverse engineering, UX principles, and thorough gap audit against our current implementation. |
| **[01-ui-redesign-and-wireframes.md](file:///c:/Users/LENOVO/source/repos/expenseTracker/docs/splitwise-flow/01-ui-redesign-and-wireframes.md)** | Visual wireframes and UI component specs for the Add Expense dialog, Split Method selector, Group Details hero banner, and 3-column micro-ledger feed. |
| **[02-split-calculation-engine-architecture.md](file:///c:/Users/LENOVO/source/repos/expenseTracker/docs/splitwise-flow/02-split-calculation-engine-architecture.md)** | Math and algorithms for all 5 split modes (Equal, Exact, %, Shares, Adjustments), deterministic penny rounding, multi-payer splits, and the Min-Cash-Flow debt simplification algorithm. |
| **[03-backend-domain-and-api-specs.md](file:///c:/Users/LENOVO/source/repos/expenseTracker/docs/splitwise-flow/03-backend-domain-and-api-specs.md)** | .NET 8 backend changes: new enums, DTOs, EF Core entity extensions, `ISplitCalculatorService` expansion, and API endpoints. |
| **[04-frontend-implementation-guide.md](file:///c:/Users/LENOVO/source/repos/expenseTracker/docs/splitwise-flow/04-frontend-implementation-guide.md)** | React 19 + TypeScript frontend guide: refactoring `SplitGroupDetailsPage.tsx`, building `SplitSentenceBar`, `SplitMethodModal`, `ExpenseFeedItem`, and custom hooks (`useSplitCalculator`). |
| **[05-step-by-step-execution-roadmap.md](file:///c:/Users/LENOVO/source/repos/expenseTracker/docs/splitwise-flow/05-step-by-step-execution-roadmap.md)** | Phased implementation roadmap from calculation engine to QA, with task lists, acceptance criteria, and verification checklists. |

---

## Key Highlights of the Redesign

1. **Natural Interactive Sentence Pattern**:
   - Replaces static forms with: `Paid by [You ▾] and split [Equally (=) ▾]`.
2. **Comprehensive Splitting Modes**:
   - Equal (`=`), Exact amounts (`123`), Percentages (`%`), and Proportional Shares (`👥`).
3. **Splitwise 3-Column Micro-Ledger Feed**:
   - Date + Category icon on left.
   - Title + Payer in center.
   - Clear personal financial impact on right (`you lent +Rs. X` in emerald green vs `you borrowed -Rs. Y` in coral red).
4. **Hero Balance Banner**:
   - Instant visual answer to *"Overall, you are owed..."* or *"Overall, you owe..."*.
5. **Debt Simplification**:
   - Greedy graph reduction to minimize total transfers between group members.
