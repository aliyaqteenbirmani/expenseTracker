import { Card } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

/**
 * Layout-matched skeleton loader for Split Expenses dashboard.
 * Matches 1:1 with the final layout (Header, 4 StatCards, and Group cards grid)
 * preventing layout shift and eliminating abrupt content snapping.
 */
export function SplitDashboardSkeleton() {
  return (
    <div className="space-y-6 animate-in fade-in-50 duration-200">
      {/* Page Header Skeleton */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between border-b pb-4">
        <div className="space-y-1.5">
          <Skeleton className="h-8 w-44 rounded-lg" />
          <Skeleton className="h-4 w-80 max-w-full rounded" />
        </div>
        <div className="flex items-center gap-2 w-full sm:w-auto justify-end">
          <Skeleton className="size-9 rounded-lg shrink-0" />
          <Skeleton className="h-9 w-32 rounded-lg flex-1 sm:flex-initial" />
        </div>
      </div>

      {/* KPI 4 Stat Cards Skeleton */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 items-stretch">
        {[1, 2, 3, 4].map((i) => (
          <Card key={i} className="rounded-xl border border-border/80 bg-card p-6 shadow-xs flex flex-col justify-between h-full space-y-3">
            <div className="flex items-center justify-between">
              <Skeleton className="h-4 w-28 rounded" />
              <Skeleton className="size-8 rounded-lg" />
            </div>
            <Skeleton className="h-7 w-36 rounded" />
            <Skeleton className="h-3.5 w-24 rounded" />
          </Card>
        ))}
      </div>

      {/* Groups Section Header */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Skeleton className="h-6 w-36 rounded" />
        </div>

        {/* Group Cards Grid Skeleton matching SplitGroupCard geometry */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-stretch">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <Card
              key={i}
              className="rounded-xl border border-border/80 bg-card p-5 shadow-xs flex flex-col justify-between h-full space-y-4"
            >
              {/* Top: Icon, title, subtitle */}
              <div className="space-y-2.5">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-3">
                    <Skeleton className="size-11 rounded-xl shrink-0" />
                    <div className="space-y-1.5">
                      <Skeleton className="h-5 w-32 rounded" />
                      <Skeleton className="h-3.5 w-24 rounded" />
                    </div>
                  </div>
                  <Skeleton className="h-5 w-14 rounded-full shrink-0" />
                </div>
                <Skeleton className="h-3.5 w-48 rounded" />
              </div>

              {/* Balance status box skeleton */}
              <div className="rounded-lg bg-muted/30 p-2.5 border border-border/40 space-y-1.5">
                <Skeleton className="h-2.5 w-16 rounded" />
                <Skeleton className="h-4 w-28 rounded" />
              </div>

              {/* Footer: Spending + View group */}
              <div className="border-t border-border/60 pt-3 flex items-center justify-between">
                <Skeleton className="h-3.5 w-28 rounded" />
                <Skeleton className="h-3.5 w-20 rounded" />
              </div>
            </Card>
          ))}
        </div>
      </div>
    </div>
  )
}
