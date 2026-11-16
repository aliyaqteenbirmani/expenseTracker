import { Button } from '@/components/ui/button'
import { ChevronDown } from 'lucide-react'

interface SplitSentenceBarProps {
  payerName: string
  splitMethodLabel: string
  onOpenPayerSelect: () => void
  onOpenSplitMethod: () => void
  disabled?: boolean
}

export function SplitSentenceBar({
  payerName,
  splitMethodLabel,
  onOpenPayerSelect,
  onOpenSplitMethod,
  disabled = false,
}: SplitSentenceBarProps) {
  return (
    <div className="flex flex-wrap items-center justify-center gap-2 py-3 px-4 rounded-xl bg-muted/40 border border-border/70 text-sm">
      <span className="text-muted-foreground font-medium">Paid by</span>
      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={disabled}
        onClick={onOpenPayerSelect}
        className="h-8 px-3 font-semibold text-primary border-primary/30 bg-background hover:bg-primary/5 active:scale-95 transition-transform"
      >
        <span className="truncate max-w-[140px]">{payerName}</span>
        <ChevronDown className="ml-1.5 h-3.5 w-3.5 opacity-60 shrink-0" />
      </Button>

      <span className="text-muted-foreground font-medium">and split</span>
      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={disabled}
        onClick={onOpenSplitMethod}
        className="h-8 px-3 font-semibold text-primary border-primary/30 bg-background hover:bg-primary/5 active:scale-95 transition-transform"
      >
        <span>{splitMethodLabel}</span>
        <ChevronDown className="ml-1.5 h-3.5 w-3.5 opacity-60 shrink-0" />
      </Button>
    </div>
  )
}
