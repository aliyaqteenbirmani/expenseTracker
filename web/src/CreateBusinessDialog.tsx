import { useState, type FormEvent } from 'react'
import { BriefcaseBusiness, X } from 'lucide-react'

type Business = { id: string; name: string; category?: string; description?: string; fileUrl?: string; fileName?: string }

type Props = {
  token: string
  onClose: () => void
  onCreated: (business: Business) => void
}

export default function CreateBusinessDialog({ token, onClose, onCreated }: Props) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [logoFile, setLogoFile] = useState<File | null>(null)

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')

    const fields = new FormData(event.currentTarget)
    const form = new FormData()
    form.append('Name', String(fields.get('name')).trim())
    form.append('Category', String(fields.get('category')))
    form.append('Description', String(fields.get('description') ?? '').trim())
    if (logoFile) {
      form.append('File', logoFile)
    }

    try {
      const response = await fetch(`${apiBase}/api/Business/create`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
        body: form,
      })

      const data = await response.json().catch(() => ({}))
      if (!response.ok) {
        throw new Error(data.message || data.Message || `Failed to create business (${response.status})`)
      }

      const raw = data.data ?? data.Data ?? data
      const created: Business = {
        id: raw.id ?? raw.Id,
        name: raw.name ?? raw.Name ?? String(fields.get('name')).trim(),
        category: raw.category ?? raw.Category,
        description: raw.description ?? raw.Description,
        fileUrl: raw.fileUrl ?? raw.FileUrl,
        fileName: raw.fileName ?? raw.FileName,
      }

      if (!created.id) {
        throw new Error('Business was created, but the ID was missing in the server response.')
      }

      onCreated(created)
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not create workspace.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && !busy && onClose()}>
      <section className="dialog create-business-dialog">
        <header>
          <div>
            <div className="eyebrow">NEW WORKSPACE</div>
            <h2>Create a business</h2>
          </div>
          <button className="close" onClick={onClose} disabled={busy} aria-label="Close dialog">
            <X size={16} strokeWidth={1.8} />
          </button>
        </header>

        <form className="dialog-content setup-form" onSubmit={submit}>
          <p className="muted">
            Add a new workspace to track separate operations, projects, or entities with their own cashbooks.
          </p>

          <label>
            Business name
            <input name="name" autoFocus maxLength={150} required placeholder="e.g. Acme Studio" />
          </label>

          <label>
            Category
            <select name="category" required defaultValue="">
              <option value="" disabled>Choose a category</option>
              <option>Retail</option>
              <option>Food & beverage</option>
              <option>Services</option>
              <option>Freelance</option>
              <option>Healthcare</option>
              <option>Technology</option>
              <option>Personal</option>
              <option>Other</option>
            </select>
          </label>

          <label>
            Description <span className="muted">(optional)</span>
            <textarea name="description" maxLength={500} rows={3} placeholder="What is this workspace for?" />
          </label>

          <label>
            Workspace logo <span className="muted">(optional)</span>
            <div className="file-input-wrapper">
              <input
                type="file"
                accept="image/*"
                onChange={e => setLogoFile(e.target.files?.[0] ?? null)}
              />
              {logoFile && <small className="file-name-hint">{logoFile.name}</small>}
            </div>
          </label>

          {error && <div className="error-banner">{error}</div>}

          <button className="button primary full" disabled={busy}>
            <BriefcaseBusiness size={16} />
            {busy ? 'Creating workspace…' : 'Create workspace'} <span>→</span>
          </button>
        </form>
      </section>
    </div>
  )
}
