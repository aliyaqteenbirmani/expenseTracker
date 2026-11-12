import { Card } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

/**
 * Layout-matched skeleton loader for Split Group details view.
 * Mirrors group header, action buttons, tab bar, and Overview StatCards
 * to ensure smooth transitions without layout shift.
 */
export function SplitGroupDetailsSkeleton() {
  return (
    <div className="space-y-6 animate-in fade-in-50 duration-200">
      {/* Group Top Header Skeleton */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b pb-4">
        <div className="flex items-center gap-3">
          <Skeleton className="size-9 rounded-lg" />
          <Skeleton className="size-11 rounded-xl" />
          <div className="space-y-1.5">
            <Skeleton className="h-7 w-48 rounded" />
            <Skeleton className="h-4 w-36 rounded" />
          </div>
        </div>

        {/* Action Buttons Skeleton */}
        <div className="flex items-center gap-2">
          <Skeleton className="size-9 rounded-lg" />
          <Skeleton className="h-9 w-28 rounded-lg" />
          <Skeleton className="h-9 w-24 rounded-lg" />
        </div>
      </div>

      {/* Tabs Bar Skeleton */}
      <div className="flex items-center gap-4 border-b pb-2">
        <Skeleton className="h-5 w-20 rounded" />
        <Skeleton className="h-5 w-24 rounded" />
        <Skeleton className="h-5 w-20 rounded" />
        <Skeleton className="h-5 w-20 rounded" />
        <Skeleton className="h-5 w-20 rounded" />
        <Skeleton className="h-5 w-18 rounded" />
      </div>

      {/* Overview 4 Stat Cards Skeleton */}
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

      {/* Summary Cards Skeleton */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Card className="rounded-xl border border-border bg-card p-4 shadow-xs space-y-3">
          <Skeleton className="h-5 w-36 rounded" />
          <Skeleton className="h-16 w-full rounded-lg" />
        </Card>
        <Card className="rounded-xl border border-border bg-card p-4 shadow-xs space-y-3">
          <Skeleton className="h-5 w-36 rounded" />
          <Skeleton className="h-16 w-full rounded-lg" />
        </Card>
      </div>
    </div>
  )
}
