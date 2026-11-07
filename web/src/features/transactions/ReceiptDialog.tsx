import { useEffect, useState, type FormEvent } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  Upload,
  Sparkles,
  Loader2,
  AlertCircle,
  Receipt,
  ArrowRight,
} from 'lucide-react'
import type { Cashbook } from '@/App'
import { cn } from '@/lib/utils'

type ReceiptScanStep = 'idle' | 'uploading' | 'analyzing' | 'suggesting' | 'review'

type ReceiptDraft = {
  draftId: string
  fileUrl?: string
  status?: string
  suggestedMerchant?: string
  suggestedAmount?: number
  suggestedDate?: string
  suggestedCategory?: string
  suggestedTransactionType?: string
  recognizedText?: string
  parsingConfidence?: number
}

const QUICK_RECEIPT_CATEGORIES = [
  'Food & Dining',
  'Transport',
  'Supplies',
  'Utilities',
  'Software',
  'Marketing',
  'Other',
]

interface ReceiptDialogProps {
  token: string
  book: Cashbook
  onClose: () => void
  onDone: () => void
  onToast?: (msg: string) => void
  api: <T>(path: string, token?: string, options?: RequestInit) => Promise<T>
}

export function ReceiptDialog({
  token,
  book,
  onClose,
  onDone,
  onToast,
  api,
}: ReceiptDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [draft, setDraft] = useState<ReceiptDraft | null>(null)
  const [step, setStep] = useState<ReceiptScanStep>('idle')
  const [stepMessage, setStepMessage] = useState('')
  const [error, setError] = useState('')
  const [isDragging, setIsDragging] = useState(false)
  const [selectedCategory, setSelectedCategory] = useState('')
  const [copiedText, setCopiedText] = useState(false)

  useEffect(() => {
    if (file && file.type.startsWith('image/')) {
      const url = URL.createObjectURL(file)
      setPreviewUrl(url)
      return () => URL.revokeObjectURL(url)
    }
    setPreviewUrl(null)
  }, [file])

  useEffect(() => {
    if (draft?.suggestedCategory) {
      setSelectedCategory(draft.suggestedCategory)
    }
  }, [draft?.suggestedCategory])

  async function runScanPipeline(targetFile: File) {
    setError('')
    setFile(targetFile)

    // 1. Upload draft
    setStep('uploading')
    setStepMessage('Uploading receipt document...')
    let uploadedDraft: ReceiptDraft
    try {
      const form = new FormData()
      form.append('CashBookId', book.id)
      form.append('File', targetFile)
      uploadedDraft = await api<ReceiptDraft>('/api/Transaction/receipts/drafts', token, {
        method: 'POST',
        body: form,
      })
      setDraft(uploadedDraft)
    } catch (e) {
      setStep('idle')
      setError(e instanceof Error ? e.message : 'Upload failed.')
      return
    }

    // 2. OCR text extraction
    setStep('analyzing')
    setStepMessage('Analyzing receipt with OCR...')
    try {
      await api(`/api/Transaction/receipts/drafts/${uploadedDraft.draftId}/analyze`, token, {
        method: 'POST',
      })
    } catch (e) {
      setStep('idle')
      setError(e instanceof Error ? e.message : 'Receipt OCR analysis failed.')
      return
    }

    // 3. Suggestion parsing
    setStep('suggesting')
    setStepMessage('Detecting merchant, amount & date...')
    try {
      await api(`/api/Transaction/receipts/drafts/${uploadedDraft.draftId}/suggest`, token, {
        method: 'POST',
      })
      const latest = await api<ReceiptDraft>(
        `/api/Transaction/receipts/drafts/${uploadedDraft.draftId}`,
        token
      )
      setDraft(latest)
      setStep('review')
    } catch (e) {
      try {
        const latest = await api<ReceiptDraft>(
          `/api/Transaction/receipts/drafts/${uploadedDraft.draftId}`,
          token
        )
        setDraft(latest)
        setStep('review')
        setError('Partial suggestions detected. Please review and fill required fields.')
      } catch {
        setStep('idle')
        setError(e instanceof Error ? e.message : 'Could not generate receipt suggestions.')
      }
    }
  }

  function handleFileSelect(e: React.ChangeEvent<HTMLInputElement>) {
    const f = e.target.files?.[0]
    if (f) runScanPipeline(f)
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setIsDragging(false)
    const f = e.dataTransfer.files?.[0]
    if (f) runScanPipeline(f)
  }

  async function confirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!draft) return
    const data = new FormData(event.currentTarget)
    setStep('suggesting')
    setStepMessage('Saving transaction...')
    setError('')
    try {
      await api(`/api/Transaction/receipts/drafts/${draft.draftId}/confirm`, token, {
        method: 'POST',
        body: JSON.stringify({
          amount: Number(data.get('amount')),
          transactionType: data.get('type'),
          transactionDate: new Date(String(data.get('date'))).toISOString(),
          merchant: data.get('merchant'),
          remarks: data.get('notes'),
          category: selectedCategory || data.get('category'),
        }),
      })
      onToast?.('Transaction created from receipt!')
      onDone()
    } catch (e) {
      setStep('review')
      setError(e instanceof Error ? e.message : 'Could not create transaction.')
    }
  }

  function copyOcrText() {
    if (draft?.recognizedText) {
      navigator.clipboard.writeText(draft.recognizedText)
      setCopiedText(true)
      setTimeout(() => setCopiedText(false), 2000)
    }
  }

  const confidence = Math.round((draft?.parsingConfidence ?? 0) * 100)
  const isBusy = step === 'uploading' || step === 'analyzing' || (step === 'suggesting' && !draft?.status)

  const progressPercent =
    step === 'uploading'
      ? 33
      : step === 'analyzing'
      ? 66
      : step === 'suggesting'
      ? 92
      : step === 'review'
      ? 100
      : 0

  return (
    <ResponsiveDialog
      open={true}
      onOpenChange={(open) => !open && !isBusy && onClose()}
      className="sm:max-w-xl"
      title={
        <div className="flex items-center gap-2">
          <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Sparkles className="size-4" />
          </div>
          <div>
            <div className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              Smart Scan
            </div>
            <div className="text-base font-semibold">Scan & Add Receipt</div>
          </div>
        </div>
      }
      description="Drop your receipt image. Spendwise will extract merchant, date, amount and details."
    >
      <div className="space-y-4 pt-2">
        {step === 'idle' && !draft && (
          <div className="space-y-4">
            <label
              className={cn(
                'flex flex-col items-center justify-center p-8 rounded-xl border-2 border-dashed border-border/80 bg-muted/20 cursor-pointer transition-colors hover:bg-muted/40',
                isDragging && 'border-primary bg-primary/5'
              )}
              onDragOver={(e) => {
                e.preventDefault()
                setIsDragging(true)
              }}
              onDragLeave={() => setIsDragging(false)}
              onDrop={handleDrop}
            >
              {previewUrl ? (
                <div className="max-h-28 overflow-hidden rounded-lg mb-2">
                  <img src={previewUrl} alt="Receipt preview" className="max-h-28 object-contain" />
                </div>
              ) : (
                <div className="flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary mb-3">
                  <Upload className="size-6 stroke-[1.75]" />
                </div>
              )}
              <p className="text-sm font-semibold text-foreground text-center">
                {file?.name ?? 'Choose or drag & drop a receipt file'}
              </p>
              <p className="text-xs text-muted-foreground mt-1">
                JPG, PNG, or PDF up to 10 MB
              </p>
              <input
                type="file"
                className="hidden"
                accept="image/jpeg,image/png,application/pdf,.jpg,.jpeg,.png,.pdf"
                onChange={handleFileSelect}
              />
            </label>
          </div>
        )}

        {isBusy && (
          <div className="space-y-4 py-8 text-center">
            <Loader2 className="size-8 animate-spin mx-auto text-primary" />
            <div className="space-y-1">
              <p className="text-sm font-semibold">{stepMessage}</p>
              <p className="text-xs text-muted-foreground">Extracting ledger attributes with AI</p>
            </div>
            <Progress value={progressPercent} className="h-1.5 max-w-xs mx-auto" />
          </div>
        )}

        {step === 'review' && draft && (
          <form onSubmit={confirm} className="space-y-4">
            <div className="flex items-center justify-between p-3 rounded-lg border border-border/80 bg-muted/30 text-xs">
              <div className="flex items-center gap-2">
                <Receipt className="size-4 text-primary" />
                <span className="font-medium">OCR Confidence</span>
              </div>
              <Badge variant="outline" className="font-mono text-xs">
                {confidence}% accuracy
              </Badge>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label htmlFor="rcptAmount" className="text-xs">Amount (PKR)</Label>
                <Input
                  id="rcptAmount"
                  name="amount"
                  type="number"
                  step="0.01"
                  required
                  defaultValue={draft.suggestedAmount ?? ''}
                  className="h-8 text-xs font-mono font-semibold"
                />
              </div>

              <div className="space-y-1">
                <Label htmlFor="rcptDate" className="text-xs">Date</Label>
                <Input
                  id="rcptDate"
                  name="date"
                  type="date"
                  required
                  defaultValue={draft.suggestedDate ? draft.suggestedDate.split('T')[0] : ''}
                  className="h-8 text-xs"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label htmlFor="rcptMerchant" className="text-xs">Merchant</Label>
                <Input
                  id="rcptMerchant"
                  name="merchant"
                  defaultValue={draft.suggestedMerchant ?? ''}
                  placeholder="e.g. Shell, Carrefour"
                  className="h-8 text-xs"
                />
              </div>

              <div className="space-y-1">
                <Label htmlFor="rcptType" className="text-xs">Type</Label>
                <select
                  id="rcptType"
                  name="type"
                  defaultValue={draft.suggestedTransactionType ?? 'CashOut'}
                  className="h-8 w-full rounded-md border border-input bg-background px-2 text-xs"
                >
                  <option value="CashOut">Expense (Outflow)</option>
                  <option value="CashIn">Income (Inflow)</option>
                </select>
              </div>
            </div>

            {/* Quick Category Chips */}
            <div className="space-y-1.5">
              <Label className="text-xs">Category</Label>
              <div className="flex flex-wrap gap-1">
                {QUICK_RECEIPT_CATEGORIES.map((cat) => (
                  <Button
                    key={cat}
                    type="button"
                    variant={selectedCategory === cat ? 'default' : 'outline'}
                    size="sm"
                    className="h-6 text-[11px] px-2"
                    onClick={() => setSelectedCategory(cat)}
                  >
                    {cat}
                  </Button>
                ))}
              </div>
            </div>

            <div className="space-y-1">
              <Label htmlFor="rcptNotes" className="text-xs">Notes / Remarks</Label>
              <Input
                id="rcptNotes"
                name="notes"
                defaultValue={draft.suggestedMerchant ? `Receipt from ${draft.suggestedMerchant}` : ''}
                placeholder="Optional memo"
                className="h-8 text-xs"
              />
            </div>

            {draft.recognizedText && (
              <details className="rounded-lg border border-border/80 p-2 text-xs">
                <summary className="cursor-pointer font-medium text-muted-foreground flex items-center justify-between">
                  <span>Recognized OCR raw text</span>
                  <button
                    type="button"
                    onClick={(e) => {
                      e.preventDefault()
                      copyOcrText()
                    }}
                    className="text-primary hover:underline"
                  >
                    {copiedText ? '✓ Copied' : 'Copy'}
                  </button>
                </summary>
                <pre className="mt-2 text-[11px] text-muted-foreground max-h-24 overflow-y-auto whitespace-pre-wrap">
                  {draft.recognizedText}
                </pre>
              </details>
            )}

            <div className="flex justify-end gap-2 pt-2 border-t">
              <Button type="button" variant="ghost" onClick={onClose} className="h-8 text-xs">
                Cancel
              </Button>
              <Button type="submit" className="h-8 text-xs gap-1.5">
                <span>Confirm & Record</span>
                <ArrowRight className="size-3.5" />
              </Button>
            </div>
          </form>
        )}

        {error && (
          <Alert variant="destructive" className="py-2 text-xs">
            <AlertCircle className="size-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
      </div>
    </ResponsiveDialog>
  )
}
