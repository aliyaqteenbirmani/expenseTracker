import { useState, type FormEvent } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Building2, Loader2, Upload, AlertCircle } from 'lucide-react'

type Business = {
  id: string
  name: string
  category?: string
  description?: string
  fileUrl?: string
  fileName?: string
}

type Props = {
  token: string
  onClose: () => void
  onCreated: (business: Business) => void
}

export default function CreateBusinessDialog({ token, onClose, onCreated }: Props) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [category, setCategory] = useState('')
  const [logoFile, setLogoFile] = useState<File | null>(null)

  const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7195'

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')

    const fields = new FormData(event.currentTarget)
    const form = new FormData()
    form.append('Name', String(fields.get('name')).trim())
    form.append('Category', category || 'Other')
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
    <ResponsiveDialog
      open={true}
      onOpenChange={(open) => !open && !busy && onClose()}
      title={
        <div className="flex items-center gap-2">
          <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Building2 className="size-4" />
          </div>
          <div>
            <div className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              New Workspace
            </div>
            <div className="text-base font-semibold">Create a Business</div>
          </div>
        </div>
      }
      description="Add a new business workspace to track separate cashbooks, expenses and collaborators."
    >
      <form onSubmit={submit} className="space-y-4 pt-2">
        <div className="space-y-1.5">
          <Label htmlFor="bizName" className="text-xs font-medium">
            Business Name
          </Label>
          <Input
            id="bizName"
            name="name"
            autoFocus
            maxLength={150}
            required
            placeholder="e.g. Acme Studio"
            className="h-9 text-xs"
          />
        </div>

        <div className="space-y-1.5">
          <Label className="text-xs font-medium">Category</Label>
          <Select value={category} onValueChange={(val) => val && setCategory(val)}>
            <SelectTrigger className="h-9 text-xs">
              <SelectValue placeholder="Select business category" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Retail">Retail</SelectItem>
              <SelectItem value="Food & beverage">Food & beverage</SelectItem>
              <SelectItem value="Services">Services</SelectItem>
              <SelectItem value="Freelance">Freelance</SelectItem>
              <SelectItem value="Healthcare">Healthcare</SelectItem>
              <SelectItem value="Technology">Technology</SelectItem>
              <SelectItem value="Personal">Personal</SelectItem>
              <SelectItem value="Other">Other</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="bizDesc" className="text-xs font-medium">
            Description <span className="text-muted-foreground font-normal">(optional)</span>
          </Label>
          <Textarea
            id="bizDesc"
            name="description"
            maxLength={500}
            rows={2}
            placeholder="What is this workspace for?"
            className="text-xs"
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="bizLogo" className="text-xs font-medium">
            Workspace Logo <span className="text-muted-foreground font-normal">(optional)</span>
          </Label>
          <div className="flex items-center gap-2">
            <Input
              id="bizLogo"
              type="file"
              accept="image/*"
              onChange={(e) => setLogoFile(e.target.files?.[0] ?? null)}
              className="text-xs file:mr-2 file:h-7 file:rounded-md file:border-0 file:bg-primary/10 file:px-2 file:text-xs file:font-semibold file:text-primary cursor-pointer"
            />
          </div>
          {logoFile && (
            <p className="text-[11px] text-muted-foreground truncate">{logoFile.name}</p>
          )}
        </div>

        {error && (
          <Alert variant="destructive" className="py-2 text-xs">
            <AlertCircle className="size-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <div className="flex justify-end gap-2 pt-2 border-t">
          <Button type="button" variant="ghost" onClick={onClose} disabled={busy} className="h-9 text-xs">
            Cancel
          </Button>
          <Button type="submit" disabled={busy} className="h-9 text-xs gap-1.5">
            {busy ? <Loader2 className="size-3.5 animate-spin" /> : <Upload className="size-3.5" />}
            <span>{busy ? 'Creating workspace…' : 'Create workspace'}</span>
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
