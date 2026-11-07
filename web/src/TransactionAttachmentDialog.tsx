import { useEffect, useState } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  Download,
  ExternalLink,
  ZoomIn,
  ZoomOut,
  RotateCw,
  RotateCcw,
  Paperclip,
  Loader2,
  AlertCircle,
} from 'lucide-react'

type TransactionAttachmentProps = {
  token: string
  transactionId: string
  fileName?: string
  filePath?: string
  remarks?: string
  onClose: () => void
}

export default function TransactionAttachmentDialog({
  token,
  transactionId,
  fileName,
  filePath: initialFilePath,
  remarks,
  onClose,
}: TransactionAttachmentProps) {
  const [filePath, setFilePath] = useState<string | null>(initialFilePath ?? null)
  const [actualFileName, setActualFileName] = useState<string>(fileName ?? 'Attachment')
  const [loading, setLoading] = useState(!initialFilePath)
  const [error, setError] = useState('')
  const [scale, setScale] = useState(1)
  const [rotation, setRotation] = useState(0)

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  useEffect(() => {
    if (initialFilePath) return

    async function fetchFile() {
      try {
        setLoading(true)
        const response = await fetch(
          `${apiBase}/api/Transaction/getfile?id=${encodeURIComponent(transactionId)}`,
          { headers: { Authorization: `Bearer ${token}` } }
        )
        const result = await response.json()
        if (response.ok && result?.data?.filePath) {
          setFilePath(result.data.filePath)
          if (result.data.fileName) setActualFileName(result.data.fileName)
        } else {
          setError(result?.message || 'Attachment not found for this transaction.')
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Could not load attachment.')
      } finally {
        setLoading(false)
      }
    }

    void fetchFile()
  }, [transactionId, token, initialFilePath, apiBase])

  const fullUrl = filePath
    ? filePath.startsWith('http')
      ? filePath
      : `${apiBase}${filePath.startsWith('/') ? '' : '/'}${filePath}`
    : null

  const isImage = fullUrl && /\.(jpg|jpeg|png|webp|gif|svg)$/i.test(fullUrl)

  const handleDownload = () => {
    if (!fullUrl) return
    const link = document.createElement('a')
    link.href = fullUrl
    link.target = '_blank'
    link.download = actualFileName
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
  }

  return (
    <ResponsiveDialog
      open={true}
      onOpenChange={(open) => !open && onClose()}
      className="sm:max-w-3xl"
      title={
        <div className="flex items-center justify-between w-full pr-6">
          <div className="flex items-center gap-2">
            <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Paperclip className="size-4" />
            </div>
            <div>
              <div className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
                Attachment Viewer
              </div>
              <div className="text-base font-semibold truncate max-w-sm">{actualFileName}</div>
            </div>
          </div>
          {fullUrl && (
            <div className="flex items-center gap-1.5">
              <Button
                variant="outline"
                size="icon"
                className="size-8"
                onClick={() => setScale((s) => Math.max(0.5, s - 0.25))}
                title="Zoom Out (-)"
              >
                <ZoomOut className="size-3.5" />
              </Button>
              <Badge variant="secondary" className="font-mono text-xs px-2 py-0.5">
                {Math.round(scale * 100)}%
              </Badge>
              <Button
                variant="outline"
                size="icon"
                className="size-8"
                onClick={() => setScale((s) => Math.min(3, s + 0.25))}
                title="Zoom In (+)"
              >
                <ZoomIn className="size-3.5" />
              </Button>
              <Button
                variant="outline"
                size="icon"
                className="size-8"
                onClick={() => setRotation((r) => (r + 90) % 360)}
                title="Rotate Clockwise"
              >
                <RotateCw className="size-3.5" />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                className="size-8"
                onClick={() => {
                  setScale(1)
                  setRotation(0)
                }}
                title="Reset Zoom & Rotation"
              >
                <RotateCcw className="size-3.5" />
              </Button>
              <Button size="sm" onClick={handleDownload} className="h-8 gap-1.5 text-xs ml-2">
                <Download className="size-3.5" />
                <span>Download</span>
              </Button>
            </div>
          )}
        </div>
      }
      description={remarks ? `Associated notes: ${remarks}` : 'Receipt inspection view.'}
    >
      <div className="space-y-4 pt-2">
        {loading && (
          <div className="flex flex-col items-center justify-center py-20 text-muted-foreground gap-2">
            <Loader2 className="size-6 animate-spin" />
            <span className="text-xs">Fetching high-resolution document...</span>
          </div>
        )}

        {error && (
          <Alert variant="destructive">
            <AlertCircle className="size-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {!loading && !error && fullUrl && (
          <div className="relative min-h-[360px] max-h-[560px] w-full overflow-hidden rounded-xl border border-border/80 bg-muted/20 flex items-center justify-center p-4">
            {isImage ? (
              <div className="overflow-auto max-h-full max-w-full flex items-center justify-center">
                <img
                  src={fullUrl}
                  alt={actualFileName}
                  className="max-h-[500px] max-w-full rounded-lg object-contain shadow-xs transition-transform duration-200"
                  style={{
                    transform: `scale(${scale}) rotate(${rotation}deg)`,
                  }}
                />
              </div>
            ) : (
              <div className="text-center space-y-3 p-8">
                <Paperclip className="size-12 mx-auto text-muted-foreground" />
                <p className="text-sm font-medium">{actualFileName}</p>
                <p className="text-xs text-muted-foreground">
                  This document type cannot be previewed inline.
                </p>
                <Button size="sm" onClick={handleDownload} className="gap-1.5">
                  <ExternalLink className="size-3.5" />
                  <span>Open or Download Document</span>
                </Button>
              </div>
            )}
          </div>
        )}

        <div className="flex justify-end pt-2 border-t">
          <Button variant="outline" size="sm" onClick={onClose} className="h-8 text-xs">
            Close
          </Button>
        </div>
      </div>
    </ResponsiveDialog>
  )
}
