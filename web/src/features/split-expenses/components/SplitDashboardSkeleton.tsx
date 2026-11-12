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
      <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between border-b pb-4">
        <div className="space-y-2">
          <Skeleton className="h-8 w-44 rounded-lg" />
          <Skeleton className="h-4 w-80 max-w-full rounded" />
        </div>
        <div className="flex items-center gap-2 mt-3 sm:mt-0">
          <Skeleton className="size-9 rounded-lg" />
          <Skeleton className="h-9 w-32 rounded-lg" />
        </div>
      </div>

      {/* KPI 4 Stat Cards Skeleton */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {[1, 2, 3, 4].map((i) => (
          <Card key={i} className="rounded-xl border border-border bg-card p-4 md:p-6 shadow-xs space-y-3">
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
          <Skeleton className="h-6 w-44 rounded" />
        </div>

        {/* Group Cards Grid Skeleton matching SplitGroupCard geometry */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <Card
              key={i}
              className="rounded-xl border border-border bg-card p-4 shadow-xs space-y-3"
            >
              {/* Top: Icon, title, subtitle */}
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-3">
                  <Skeleton className="size-11 rounded-xl" />
                  <div className="space-y-1.5">
                    <Skeleton className="h-5 w-32 rounded" />
                    <Skeleton className="h-3.5 w-24 rounded" />
                  </div>
                </div>
                <Skeleton className="h-5 w-14 rounded-full" />
              </div>

              {/* Subtitle / description skeleton */}
              <Skeleton className="h-3.5 w-48 rounded" />

              {/* Balance status footer */}
              <div className="border-t pt-3 mt-2 flex items-center justify-between">
                <div className="space-y-1">
                  <Skeleton className="h-2.5 w-16 rounded" />
                  <Skeleton className="h-4 w-28 rounded" />
                </div>
                <Skeleton className="h-4 w-20 rounded" />
              </div>
            </Card>
          ))}
        </div>
      </div>
    </div>
  )
}
