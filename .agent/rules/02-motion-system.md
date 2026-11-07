# Motion System

Motion explains change, gives feedback, and makes the app feel fast. Enterprise tone: quick, quiet, precise. Never decorative.

## Tokens (only source of timing)
CSS (`src/index.css`): `--duration-instant 80ms`, `--duration-fast 140ms`, `--duration-base 200ms`, `--duration-slow 280ms`, `--duration-slower 400ms`; easing `--ease-out (0.22,1,0.36,1)`, `--ease-out-expo (0.16,1,0.3,1)`, `--ease-in (0.55,0,1,0.45)`, `--ease-in-out (0.65,0,0.35,1)`, `--ease-spring (0.34,1.36,0.64,1)`. Tailwind: `ease-out`, `ease-spring`, `duration-(--duration-fast)`, `animate-shimmer`, `animate-shake`, `animate-row-flash`.
JS (`src/lib/motion.ts`): `duration`, `ease`, `spring.snappy` (pills/indicators), `spring.smooth` (layout/resize/reorder), `spring.bouncy` (toggles, small pops), variants `fadeUp`, `fadeScale`, `stagger()`, `pageTransition`, `rowPresence`.
Never write inline duration/easing/spring numbers in components.

## Timing guide
| Interaction | Duration | Easing |
|---|---|---|
| Hover, press, color, focus ring | 80–140ms | ease-out |
| Checkbox/radio/switch | 140–200ms | ease-out / ease-spring |
| Tooltip | 120ms in / 80ms out | ease-out |
| Dropdown, popover, select | 200ms in / 140ms out | ease-out-expo / ease-in |
| Dialog | 240ms in / 140ms out | ease-out-expo / ease-in |
| Sheet | 280ms in / 200ms out | ease-out-expo / ease-in |
| Page transition | 280ms in / 140ms out | ease-out / ease-in |
| Number roll | 450ms | ease-out |
| Chart first render | 600ms, once | ease-out |

## Principles
1. Feedback within one frame; CSS transitions for state (interruptible); keyframes only for enter/exit.
2. Enter ease-out, exit ease-in and ~65% of enter duration.
3. Small distances: 4–8px slides, scale from 0.96–0.98, never from 0.
4. Overlays grow from their trigger: `origin-(--radix-*-transform-origin)`.
5. Shared indicators (tabs, toggle groups, nav, active card) slide with `layoutId` + `spring.snappy`.
6. Stagger only on first load, ≤ 12 items, 25–40ms.
7. No re-animation on filter/sort/typing — crossfade ≤ 120ms instead.
8. Never block input or wait for animation to accept a click.

## Allowed properties
transform, opacity, filter (blur ≤ 2px), clip-path, color/background-color/border-color/box-shadow. Height only via Radix `--radix-*-content-height`, Motion `layout`, or `height: auto` presence. Always list properties explicitly: `transition-[color,background-color,border-color,box-shadow,opacity,transform]`.

## Anti-patterns (reject in review)
`transition-all`; animating width/height/top/left/margin directly; > 400ms on UI chrome; bouncy springs on overlays/large surfaces; infinite animations except shimmer/spinner; re-animating charts/tables on re-render; per-frame setState; flashy effects (confetti, 3D tilt, glow, spin).

## Reduced motion
`<MotionConfig reducedMotion="user">` at root; global CSS clamp in `index.css`; `useMotionSafe()` for custom variants. Under reduce: no translate/scale/rotate/blur, fades ≤ 100ms, numbers/charts snap.

## Performance
60fps at 4× CPU throttle. `LazyMotion` + `m` (domAnimation; domMax only where layout/layoutId needed). No static `will-change`. Memoize table columns and chart data.

## Inspiration sources (restyle + retime before use)
beUI (beui.dev, same stack, install via shadcn CLI `@beui/*`), Transitions.dev (interaction patterns), Beautiful UI (glide hovers, row sweeps, shimmer), Rare UI (visual only).
