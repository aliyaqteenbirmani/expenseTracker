# Spendwise — Agent Guide

Spendwise is a multi-workspace business cashbook app: React 19 + TypeScript + Vite, Tailwind v4, shadcn/ui, Motion, SignalR realtime, owner-controlled permissions.

## Always follow the project rules
Before any UI work, read and obey every file in `.agents/rules/`:
- `01-ui-stack.md` — component system, folder layout, design tokens
- `02-motion-system.md` — motion tokens, principles, anti-patterns
- `03-component-recipes.md` — which animated wrapper to use for each UI need
- `04-ui-review-checklist.md` — run before declaring any UI task done

## Non-negotiables
- Never change API calls, SignalR wiring, routes, or permission logic during UI work.
- Build every UI from shadcn/ui (`src/components/ui`) + project wrappers (`src/components/app`, `src/components/motion`). Never add another UI kit.
- All colors from theme tokens; all motion from `src/lib/motion.ts` + CSS tokens.
- Gate actions with `usePermissions()` + `<Gate>`.
- TypeScript strict; `tsc --noEmit`, lint, and build must pass.

## Keep memory current
When you add or rename a shared component, token, or pattern, update the matching file in `.agents/rules/` in the same change.
