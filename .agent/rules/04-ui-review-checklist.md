# UI Review Checklist — run before finishing any UI task

- [ ] Built from shadcn/ui + project wrappers; no new UI library; new shadcn parts added via CLI.
- [ ] Colors only from tokens; no hex; light + dark both checked.
- [ ] Timing only from motion tokens; no `transition-all`; only allowed properties animated.
- [ ] Correct recipe from `03-component-recipes.md` used (tabs, toggles, checkbox, dialogs, numbers, rows…).
- [ ] Enter ease-out / exit ease-in and faster; nothing on UI chrome > 400ms; interactions interruptible.
- [ ] Async: delayed shimmer skeleton, empty state, loading/success/error feedback, no layout shift.
- [ ] Permission gating via `usePermissions()` + `<Gate>`; 403 handled.
- [ ] Keyboard + focus rings work; icon buttons labelled; realtime announced via aria-live.
- [ ] `prefers-reduced-motion` verified (DevTools → Rendering → emulate).
- [ ] 60fps at 4× CPU throttle for the touched interactions.
- [ ] Responsive at 375 / 768 / 1280 / 1920; no horizontal scroll.
- [ ] `tsc --noEmit`, lint, build pass.
- [ ] Rules in `.agents/rules/` updated if a shared component/token/pattern changed.
