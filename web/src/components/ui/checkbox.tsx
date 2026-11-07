"use client"

import { Checkbox as CheckboxPrimitive } from "@base-ui/react/checkbox"
import { cn } from "@/lib/utils"

function Checkbox({ className, ...props }: CheckboxPrimitive.Root.Props) {
  return (
    <CheckboxPrimitive.Root
      data-slot="checkbox"
      className={cn(
        "peer group relative flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-input outline-none",
        "transition-[background-color,border-color,box-shadow,transform] duration-(--duration-fast) ease-out",
        "active:scale-90 data-checked:bg-primary data-checked:border-primary data-checked:text-primary-foreground",
        "data-indeterminate:bg-primary data-indeterminate:border-primary data-indeterminate:text-primary-foreground",
        "data-[state=checked]:bg-primary data-[state=checked]:border-primary data-[state=checked]:text-primary-foreground",
        "data-[state=indeterminate]:bg-primary data-[state=indeterminate]:border-primary data-[state=indeterminate]:text-primary-foreground",
        "focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50",
        "aria-invalid:border-destructive aria-invalid:ring-destructive/20",
        className
      )}
      {...props}
    >
      <CheckboxPrimitive.Indicator keepMounted className="grid place-content-center text-current">
        <svg
          viewBox="0 0 24 24"
          className="size-3.5"
          fill="none"
          stroke="currentColor"
          strokeWidth={3.25}
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden
        >
          {/* check */}
          <path
            d="M4.5 12.5l4.75 4.75L19.5 7"
            pathLength={1}
            className="[stroke-dasharray:1] [stroke-dashoffset:1] transition-[stroke-dashoffset] duration-(--duration-base) ease-out group-data-checked:[stroke-dashoffset:0] group-data-[state=checked]:[stroke-dashoffset:0] group-data-checked:delay-[40ms]"
          />
          {/* indeterminate */}
          <path
            d="M6 12h12"
            pathLength={1}
            className="[stroke-dasharray:1] [stroke-dashoffset:1] transition-[stroke-dashoffset] duration-(--duration-fast) ease-out group-data-indeterminate:[stroke-dashoffset:0] group-data-[state=indeterminate]:[stroke-dashoffset:0]"
          />
        </svg>
      </CheckboxPrimitive.Indicator>
    </CheckboxPrimitive.Root>
  )
}

export { Checkbox }
