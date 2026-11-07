import { format, formatDistanceToNow, isToday, isYesterday } from 'date-fns'

export function formatMoney(amount: number, currency: string = 'PKR'): string {
  const absAmount = Math.abs(amount)
  try {
    const formatted = new Intl.NumberFormat(undefined, {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(absAmount)

    const prefix = currency === 'PKR' ? '₨' : currency === 'USD' ? '$' : currency === 'EUR' ? '€' : currency === 'GBP' ? '£' : `${currency} `
    return `${prefix}${formatted}`
  } catch {
    return `${currency} ${absAmount.toFixed(2)}`
  }
}

export function formatDate(dateInput: string | Date | null | undefined): string {
  if (!dateInput) return ''
  const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput
  if (isNaN(date.getTime())) return ''
  return format(date, 'MMM d, yyyy')
}

export function formatDateTime(dateInput: string | Date | null | undefined): string {
  if (!dateInput) return ''
  const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput
  if (isNaN(date.getTime())) return ''
  return format(date, 'MMM d, yyyy h:mm a')
}

export function formatRelativeDate(dateInput: string | Date | null | undefined): string {
  if (!dateInput) return ''
  const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput
  if (isNaN(date.getTime())) return ''
  if (isToday(date)) return 'Today'
  if (isYesterday(date)) return 'Yesterday'
  return formatDistanceToNow(date, { addSuffix: true })
}
