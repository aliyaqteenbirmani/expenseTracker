# UI Stack & Component Conventions

## Stack
React 19, TypeScript strict, Vite, Tailwind CSS v4 (`@tailwindcss/vite`, tokens in `src/index.css` via `@theme inline`), shadcn/ui (new-york, CSS variables), Radix (via shadcn only), lucide-react icons, Motion (`motion/react`), tw-animate-css, @tanstack/react-table, shadcn Chart (Recharts), react-hook-form + zod, Sonner, cmdk, vaul, date-fns, @number-flow/react.

## Where things live
- `src/components/ui/` — shadcn generated. Add new ones ONLY with `npx shadcn@latest add <name>`. Edits limited to classes/variants/motion tuning.
- `src/components/motion/` — animated wrappers: MotionTabs, MotionToggleGroup, HoverGlide, Swap, AnimatedNumber, AnimatedOutlet, SuccessCheck, RowPresence.
- `src/components/app/` — product primitives: PageHeader, StatCard, MoneyText, PermissionChip, RoleBadge, Gate, EmptyState, ResponsiveDialog.
- `src/components/layout/` — AppSidebar, SiteHeader, NavUser, WorkspaceSwitcher, CashbookSwitcher.
- `src/features/<domain>/` — pages and feature components.
- `src/lib/motion.ts` (motion tokens), `src/lib/format.ts` (money/date), `src/lib/utils.ts` (`cn`).
- `src/hooks/` — usePermissions, useMotionSafe, useDelayedFlag, useHotkeys, useMediaQuery.

## Choosing components (decision order)
1. Existing project wrapper in `components/app` or `components/motion`.
2. shadcn/ui component or official block.
3. beUI (`npx shadcn@latest add @beui/<name>`) — install into `components/motion`, then refactor to project tokens.
4. Build new on top of shadcn + Motion, then document it in `03-component-recipes.md`.
Never: MUI, Chakra, Ant, Mantine, DaisyUI, Headless UI, raw Radix where a shadcn wrapper exists.

## Design tokens
- Colors: shadcn tokens + `inflow`, `outflow`, `warning`, `owner`, `perm-view|add|edit|delete` (each with `-muted`). No hex in components.
- Radius `--radius: 0.625rem`: cards `rounded-xl`, controls `rounded-md`, pills `rounded-full`.
- Spacing 4px grid: page `px-4 lg:px-6`, sections `gap-4 md:gap-6`, cards `p-6` (dense `p-4`).
- Depth: `border` + `shadow-xs` on cards; `shadow-lg` only on floating layers.

## Typography
Page title `text-2xl font-semibold tracking-tight`; card title `text-base font-semibold`; body `text-sm`; meta `text-xs text-muted-foreground`; KPI `text-3xl font-semibold tabular-nums`; money `font-mono tabular-nums text-right` via `<MoneyText>` (inflow `+` `text-inflow`, outflow `−` `text-outflow`).

## Patterns
- Dialogs → `<ResponsiveDialog>` (Dialog ≥ md, Drawer < md). Detail views → Sheet.
- Forms → react-hook-form + zod + shadcn Field/Form; submit `<Button loading>`.
- Feedback → Sonner (`toast.promise` for mutations). Never `alert()`.
- Destructive → AlertDialog.
- Async regions → layout-matched shimmer Skeleton (after 150ms) + EmptyState.
- Tables → shadcn Data Table pattern; virtualize > 200 rows.
- Permissions → `usePermissions()` + `<Gate>`: primary CTAs disabled with Lock + Tooltip; row/menu actions hidden; "Read only" badge when none; self-edit hidden; on 403 toast + refetch permissions.

## Accessibility
Icon buttons need `aria-label` + Tooltip; keep visible focus rings; never color-only signals; `aria-live="polite"` for realtime updates; WCAG 2.2 AA contrast in light and dark.
