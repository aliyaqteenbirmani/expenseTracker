import { useEffect, useState } from 'react'
import { Download, ExternalLink, FileText, X } from 'lucide-react'

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

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  useEffect(() => {
    if (initialFilePath) return

    async function fetchFile() {
      try {
        setLoading(true)
        const response = await fetch(`${apiBase}/api/Transaction/getfile?id=${encodeURIComponent(transactionId)}`, {
          headers: { Authorization: `Bearer ${token}` }
        })
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
  const isPdf = fullUrl && /\.pdf$/i.test(fullUrl)

  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && onClose()}>
      <section className="dialog attachment-dialog">
        <header>
          <div>
            <div className="eyebrow">TRANSACTION RECEIPT</div>
            <h2>{remarks || 'Attached Receipt'}</h2>
          </div>
          <button className="close" onClick={onClose} aria-label="Close dialog">
            <X size={16} strokeWidth={1.8} />
          </button>
        </header>

        <div className="attachment-content">
          {loading && (
            <div className="attachment-loading">
              <div className="skeleton-block" style={{ width: 120, height: 120, borderRadius: 12, margin: '20px auto' }} />
              <p className="muted">Loading attachment…</p>
            </div>
          )}

          {error && <div className="error-banner">{error}</div>}

          {!loading && !error && fullUrl && (
            <div className="attachment-view">
              {isImage && (
                <div className="attachment-image-box">
                  <img src={fullUrl} alt={remarks || 'Transaction receipt'} className="attachment-img-preview" />
                </div>
              )}

              {isPdf && (
                <div className="attachment-pdf-box">
                  <iframe src={fullUrl} title="PDF Receipt Preview" className="attachment-pdf-frame" />
                </div>
              )}

              {!isImage && !isPdf && (
                <div className="attachment-file-generic">
                  <FileText size={48} strokeWidth={1.4} />
                  <strong>{actualFileName}</strong>
                  <p className="muted">This document cannot be previewed directly in the browser.</p>
                </div>
              )}

              <div className="attachment-actions">
                <a
                  href={fullUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="button secondary-action"
                  download={actualFileName}
                >
                  <Download size={14} /> Download receipt
                </a>
                <a
                  href={fullUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="button secondary-action"
                >
                  <ExternalLink size={14} /> Open in new tab
                </a>
              </div>
            </div>
          )}
        </div>
      </section>
    </div>
  )
}
