# Component Recipes — use these, don't reinvent

| Need | Use | Motion behaviour |
|---|---|---|
| Tabs (any) | `MotionTabs`, `MotionTabsList`, `MotionTabsTrigger`, `MotionTabsContent` (`components/motion/motion-tabs.tsx`); `variant="underline"` for page-level | layoutId pill/underline `spring.snappy`; content fade + 6px directional slide 200ms |
| Segmented control / single toggle | `MotionToggleGroup` (`components/motion/motion-toggle-group.tsx`) | sliding pill; Cash In/Out pill tints inflow/outflow |
| Checkbox | `ui/checkbox` (custom) | CSS stroke-draw check 200ms, indeterminate line, `active:scale-90` |
| Radio | `ui/radio-group` (custom) | dot scale 0→1 ease-spring |
| Switch | `ui/switch` (custom) | thumb ease-spring, stretches on press |
| Button | `ui/button` + `loading` prop | `active:scale-[0.97]`; loading keeps width; success morph via `Swap` |
| Icon/text change | `Swap` (`components/motion/swap.tsx`) | popLayout, y ±6, blur 2px, 140/100ms |
| Hover/selection highlight in lists | `HoverGlide` (`components/motion/hover-glide.tsx`) | single layoutId highlight follows hover, `spring.snappy` |
| Dropdown/select/popover/hover-card | shadcn, tuned | origin-aware zoom 0.97 + 4px slide, 200/140ms |
| Tooltip | shadcn, provider delay 350 / skip 150 | 120/80ms fade |
| Modal | `ResponsiveDialog` | zoom 0.96 + slide 8px, 240/140ms; body `layout` resize |
| Destructive confirm | `AlertDialog` | shake on outside click |
| Detail panel | `Sheet` | 280/200ms slide; inner stagger |
| Expand/collapse | Accordion / Collapsible | Radix height vars 220/160ms + chevron rotate |
| Toasts | Sonner, `toast.promise` | in-place state morph; undo countdown bar |
| Money/KPI/counts | `AnimatedNumber` (`components/motion/animated-number.tsx`) | 450ms digit roll via @number-flow/react |
| Table rows | Data Table + `rowPresence` | first-load stagger ≤ 12; realtime insert/flash/collapse; "N new" pill if scrolled |
| Bulk actions | floating bar | slide up `spring.smooth` |
| Loading | shimmer `Skeleton` + `useDelayedFlag(150)` | crossfade to content, no shift |
| Route change | `AnimatedOutlet` + `RouteProgressBar` (`components/motion/route-progress-bar.tsx`) | `pageTransition`, top-level segment only |
| Split module transitions | `SplitPageTransition` (`components/motion/split-page-transition.tsx`) | fade + 6px slide/0.99 scale 280/140ms (`duration.slow`/`fast`); `useMotionSafe` reduced-motion clamp; `splitStaggerContainer` + `splitCardVariants` for grids |
| Active item between siblings | `layoutId` (`nav-active`, `active-cashbook`) | `spring.snappy` |
| Grid ↔ list | Motion `layout` | `spring.smooth` |
| Form error | field `animate-shake` + layout message | focus first invalid |
| Success | `SuccessCheck` (`components/motion/success-check.tsx`) | circle + check stroke draw |
| Charts | shadcn Chart | animate first render only; Swap center label |
| Theme toggle | `switchTheme()` | View Transition circular reveal, fallback instant |

When you create a new reusable animated component, add a row here in the same change.
