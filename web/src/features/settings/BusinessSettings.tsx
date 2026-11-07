import { useState, type FormEvent } from 'react'
import { PageHeader } from '@/components/app/PageHeader'
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from '@/components/ui/card'
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
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  ArrowLeft,
  Building2,
  Trash2,
  Save,
  Loader2,
  AlertCircle,
} from 'lucide-react'
import type { Business } from '@/App'

interface BusinessSettingsProps {
  token: string
  business: Business
  onBack: () => void
  onUpdated: (business: Business) => void
  onDeleted: () => void
  onToast: (msg: string) => void
  api: <T>(path: string, token?: string, options?: RequestInit) => Promise<T>
}

export function BusinessSettings({
  token,
  business,
  onBack,
  onUpdated,
  onDeleted,
  onToast,
  api,
}: BusinessSettingsProps) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [logoPreview, setLogoPreview] = useState<string | null>(business.fileUrl ?? null)
  const [category, setCategory] = useState(business.category ?? 'Other')

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    const fields = new FormData(event.currentTarget)
    const form = new FormData()
    form.append('Id', business.id)
    form.append('Name', String(fields.get('name')).trim())
    form.append('Category', category)
    form.append('Description', String(fields.get('description') ?? '').trim())
    if (logoFile) {
      form.append('File', logoFile)
    }
    try {
      const updated = await api<Business>('/api/Business', token, { method: 'PATCH', body: form })
      onUpdated({
        ...business,
        ...updated,
        id: updated.id ?? business.id,
        name: updated.name ?? String(fields.get('name')).trim(),
        category: updated.category ?? category,
        description: updated.description ?? String(fields.get('description') ?? '').trim(),
        fileUrl: updated.fileUrl ?? logoPreview ?? business.fileUrl,
        fileName: updated.fileName ?? business.fileName,
      })
      onToast('Business settings saved')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save business settings.')
    } finally {
      setBusy(false)
    }
  }

  async function remove() {
    setBusy(true)
    setError('')
    try {
      await api(`/api/Business/${encodeURIComponent(business.id)}`, token, { method: 'DELETE' })
      onToast('Business deleted')
      onDeleted()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not delete this business.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="space-y-6 pb-12">
      <PageHeader
        title="Business Settings"
        description="Update identity, category and workspace branding."
        breadcrumb={
          <Button
            variant="ghost"
            size="sm"
            onClick={onBack}
            className="-ml-2 h-7 gap-1 text-xs text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="size-3.5" />
            <span>Back to Overview</span>
          </Button>
        }
      />

      <div className="max-w-2xl space-y-6">
        <Card className="border-border/80 shadow-xs">
          <CardHeader className="p-6 pb-4">
            <CardTitle className="text-base font-semibold">Workspace Profile</CardTitle>
            <CardDescription className="text-xs text-muted-foreground">
              These details identify this workspace across cashbooks and member invites.
            </CardDescription>
          </CardHeader>
          <form onSubmit={save}>
            <CardContent className="p-6 pt-0 space-y-4">
              {/* Logo Editor */}
              <div className="flex items-center gap-4">
                <div className="flex size-14 items-center justify-center rounded-xl border border-border/80 bg-muted/40 overflow-hidden shrink-0">
                  {logoPreview ? (
                    <img src={logoPreview} alt="Logo" className="size-full object-cover" />
                  ) : (
                    <Building2 className="size-6 text-muted-foreground" />
                  )}
                </div>
                <div className="space-y-1">
                  <Label htmlFor="logoUpload" className="text-xs font-medium">
                    Upload Logo
                  </Label>
                  <Input
                    id="logoUpload"
                    type="file"
                    accept="image/*"
                    onChange={(e) => {
                      const f = e.target.files?.[0] ?? null
                      setLogoFile(f)
                      if (f) setLogoPreview(URL.createObjectURL(f))
                    }}
                    className="h-8 text-xs file:mr-2 file:h-6 file:rounded file:border-0 file:bg-primary/10 file:px-2 file:text-xs file:font-semibold file:text-primary cursor-pointer"
                  />
                </div>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="name" className="text-xs font-medium">
                  Business Name
                </Label>
                <Input
                  id="name"
                  name="name"
                  defaultValue={business.name}
                  required
                  maxLength={150}
                  className="h-9 text-xs"
                />
              </div>

              <div className="space-y-1.5">
                <Label className="text-xs font-medium">Category</Label>
                <Select value={category} onValueChange={(val) => val && setCategory(val)}>
                  <SelectTrigger className="h-9 text-xs">
                    <SelectValue placeholder="Select category" />
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
                <Label htmlFor="description" className="text-xs font-medium">
                  Description
                </Label>
                <Textarea
                  id="description"
                  name="description"
                  defaultValue={business.description ?? ''}
                  rows={3}
                  className="text-xs"
                />
              </div>

              {error && (
                <Alert variant="destructive" className="py-2 text-xs">
                  <AlertCircle className="size-4" />
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}
            </CardContent>
            <CardFooter className="flex justify-end gap-2 p-6 pt-0 border-t mt-4 pt-4">
              <Button type="button" variant="ghost" onClick={onBack} disabled={busy} className="h-8 text-xs">
                Cancel
              </Button>
              <Button type="submit" disabled={busy} className="h-8 text-xs gap-1.5">
                {busy ? <Loader2 className="size-3.5 animate-spin" /> : <Save className="size-3.5" />}
                <span>Save Changes</span>
              </Button>
            </CardFooter>
          </form>
        </Card>

        {/* Danger Zone */}
        <Card className="border-destructive/30 bg-destructive/5 shadow-xs">
          <CardHeader className="p-6 pb-2">
            <CardTitle className="text-base font-semibold text-destructive flex items-center gap-2">
              <Trash2 className="size-4" />
              <span>Danger Zone</span>
            </CardTitle>
            <CardDescription className="text-xs text-muted-foreground">
              Permanently delete this workspace and all linked cashbooks and transactions.
            </CardDescription>
          </CardHeader>
          <CardFooter className="p-6 pt-3 flex justify-end">
            <Button
              variant="destructive"
              size="sm"
              onClick={() => setConfirmDelete(true)}
              className="h-8 text-xs gap-1.5"
            >
              <Trash2 className="size-3.5" />
              <span>Delete Workspace</span>
            </Button>
          </CardFooter>
        </Card>
      </div>

      {/* Delete Confirmation */}
      <AlertDialog open={confirmDelete} onOpenChange={setConfirmDelete}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="text-destructive">Delete Workspace?</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to permanently delete <strong>&ldquo;{business.name}&rdquo;</strong>?
              This operation cannot be undone. All cashbooks and transaction histories will be destroyed.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={busy}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              disabled={busy}
              onClick={remove}
            >
              {busy ? <Loader2 className="size-3.5 animate-spin mr-1.5" /> : null}
              <span>Delete Workspace</span>
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
