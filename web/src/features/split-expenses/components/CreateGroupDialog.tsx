import { useState } from 'react'
import { ResponsiveDialog } from '@/components/app/ResponsiveDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { splitExpensesApi } from '../splitExpensesApi'
import { toast } from 'sonner'
import type { SplitGroupSummaryDto } from '../types'

interface CreateGroupDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  token?: string
  onGroupCreated: (newGroup: SplitGroupSummaryDto) => void
}

const PRESET_ICONS = ['👥', '🏖️', '🏠', '🍔', '🚗', '✈️', '💼', '🎉', '☕', '🛒']

export function CreateGroupDialog({
  open,
  onOpenChange,
  token,
  onGroupCreated,
}: CreateGroupDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selectedIcon, setSelectedIcon] = useState('👥')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!name.trim()) {
      toast.error('Group name is required')
      return
    }

    try {
      setLoading(true)
      const res = await splitExpensesApi.createGroup(
        {
          name: name.trim(),
          description: description.trim() || undefined,
          icon: selectedIcon,
        },
        token
      )
      toast.success('Split group created successfully')
      setName('')
      setDescription('')
      setSelectedIcon('👥')
      onOpenChange(false)
      onGroupCreated(res)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to create group'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Create Split Group"
      description="Create a new expense-sharing group for your trip, house, or project."
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="group-name">Group Name *</Label>
          <Input
            id="group-name"
            placeholder="e.g. Trip to Murree, Flat 4B, Friday Dinner"
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={loading}
            maxLength={150}
            required
            autoFocus
          />
        </div>

        <div className="space-y-2">
          <Label>Group Icon</Label>
          <div className="flex flex-wrap gap-2">
            {PRESET_ICONS.map((icon) => (
              <button
                type="button"
                key={icon}
                onClick={() => setSelectedIcon(icon)}
                className={`flex size-10 items-center justify-center rounded-lg border text-lg transition-all ${
                  selectedIcon === icon
                    ? 'border-primary bg-primary/10 shadow-xs scale-105'
                    : 'border-border hover:bg-muted/50'
                }`}
              >
                {icon}
              </button>
            ))}
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="group-desc">Description (Optional)</Label>
          <Textarea
            id="group-desc"
            placeholder="What is this group for?"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            disabled={loading}
            maxLength={500}
            rows={3}
          />
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={loading}
          >
            Cancel
          </Button>
          <Button type="submit" loading={loading}>
            Create Group
          </Button>
        </div>
      </form>
    </ResponsiveDialog>
  )
}
