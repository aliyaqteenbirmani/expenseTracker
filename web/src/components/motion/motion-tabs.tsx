import * as React from "react"
import { motion } from "motion/react"
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs"
import { cn } from "@/lib/utils"
import { spring, duration, ease } from "@/lib/motion"

type Ctx = {
  value: string
  id: string
  dir: 1 | -1
  variant?: "pill" | "underline"
  order: React.MutableRefObject<string[]>
}

const TabsCtx = React.createContext<Ctx | null>(null)

const useTabsCtx = () => {
  const c = React.useContext(TabsCtx)
  if (!c) throw new Error("MotionTabs.* must be used inside <MotionTabs>")
  return c
}

export function MotionTabs({
  value,
  onValueChange,
  children,
  className,
  variant = "pill",
  ...props
}: Omit<React.ComponentProps<typeof Tabs>, "value" | "onValueChange"> & {
  value: string
  onValueChange?: (v: string) => void
  variant?: "pill" | "underline"
}) {
  const id = React.useId()
  const order = React.useRef<string[]>([])
  const prev = React.useRef(value)
  const dir: 1 | -1 =
    order.current.indexOf(value) >= order.current.indexOf(prev.current) ? 1 : -1

  React.useEffect(() => {
    prev.current = value
  }, [value])

  return (
    <TabsCtx.Provider value={{ value, id, dir, variant, order }}>
      <Tabs
        value={value}
        onValueChange={onValueChange}
        className={className}
        {...props}
      >
        {children}
      </Tabs>
    </TabsCtx.Provider>
  )
}

export const MotionTabsList = TabsList

export function MotionTabsTrigger({
  value,
  children,
  className,
  ...props
}: React.ComponentProps<typeof TabsTrigger>) {
  const ctx = useTabsCtx()
  if (!ctx.order.current.includes(value)) ctx.order.current.push(value)
  const active = ctx.value === value

  return (
    <TabsTrigger
      value={value}
      className={cn(
        "relative transition-colors duration-(--duration-fast)",
        "data-[state=active]:bg-transparent data-[state=active]:shadow-none dark:data-[state=active]:bg-transparent dark:data-[state=active]:border-transparent",
        className
      )}
      {...props}
    >
      {active && ctx.variant === "pill" && (
        <motion.span
          layoutId={`${ctx.id}-pill`}
          transition={spring.snappy}
          className="absolute inset-0 rounded-md bg-background shadow-xs dark:bg-input/40 dark:border dark:border-input"
        />
      )}
      {active && ctx.variant === "underline" && (
        <motion.span
          layoutId={`${ctx.id}-underline`}
          transition={spring.snappy}
          className="absolute inset-x-1 -bottom-px h-0.5 rounded-full bg-primary"
        />
      )}
      <span className="relative z-10 inline-flex items-center gap-1.5">{children}</span>
    </TabsTrigger>
  )
}

export function MotionTabsContent({
  value,
  children,
  className,
  ...props
}: React.ComponentProps<typeof TabsContent>) {
  const ctx = useTabsCtx()
  return (
    <TabsContent value={value} className={className} {...props}>
      <motion.div
        key={value}
        initial={{ opacity: 0, x: 6 * ctx.dir }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ duration: duration.base, ease: ease.out }}
      >
        {children}
      </motion.div>
    </TabsContent>
  )
}
