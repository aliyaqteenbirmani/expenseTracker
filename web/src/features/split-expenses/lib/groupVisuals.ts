export interface GroupVisualResult {
  icon?: string
  initials?: string
  bgClass: string
}

/**
 * Resolves a light, colorful, personality-rich visual representation for a split group.
 * Replaces dark/heavy generic icons with delightful context-aware icons (e.g. 🍕 for Food Hunter)
 * or a clean two-letter monogram avatar on a soft tinted base.
 */
export function getGroupVisual(name?: string, customIcon?: string): GroupVisualResult {
  const trimmedName = (name || '').trim()
  const lower = trimmedName.toLowerCase()

  // 1. If user chose an intentional icon other than the generic 👥
  if (customIcon && customIcon !== '👥') {
    return {
      icon: customIcon,
      bgClass: 'bg-primary/10 border-primary/20 text-foreground',
    }
  }

  // 2. Keyword-based delightful matching
  // Food & Dining (e.g. "Food Hunter")
  if (
    lower.includes('food') ||
    lower.includes('hunt') ||
    lower.includes('eat') ||
    lower.includes('lunch') ||
    lower.includes('dinner') ||
    lower.includes('meal') ||
    lower.includes('cafe') ||
    lower.includes('pizza') ||
    lower.includes('burger')
  ) {
    return {
      icon: '🍕',
      bgClass: 'bg-amber-500/10 border-amber-500/20 text-amber-600 dark:text-amber-400',
    }
  }

  // Trips & Vacations
  if (
    lower.includes('trip') ||
    lower.includes('travel') ||
    lower.includes('tour') ||
    lower.includes('flight') ||
    lower.includes('vacation') ||
    lower.includes('holiday')
  ) {
    return {
      icon: '✈️',
      bgClass: 'bg-sky-500/10 border-sky-500/20 text-sky-600 dark:text-sky-400',
    }
  }

  // Home & Flatmates
  if (
    lower.includes('home') ||
    lower.includes('house') ||
    lower.includes('flat') ||
    lower.includes('room') ||
    lower.includes('rent') ||
    lower.includes('apartment')
  ) {
    return {
      icon: '🏠',
      bgClass: 'bg-emerald-500/10 border-emerald-500/20 text-emerald-600 dark:text-emerald-400',
    }
  }

  // Celebrations & Parties
  if (
    lower.includes('party') ||
    lower.includes('event') ||
    lower.includes('fest') ||
    lower.includes('fun') ||
    lower.includes('celebrat')
  ) {
    return {
      icon: '🎉',
      bgClass: 'bg-purple-500/10 border-purple-500/20 text-purple-600 dark:text-purple-400',
    }
  }

  // Transportation & Rides
  if (
    lower.includes('car') ||
    lower.includes('drive') ||
    lower.includes('ride') ||
    lower.includes('road')
  ) {
    return {
      icon: '🚗',
      bgClass: 'bg-indigo-500/10 border-indigo-500/20 text-indigo-600 dark:text-indigo-400',
    }
  }

  // 3. Fallback: Clean initials avatar
  const initials = trimmedName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0].toUpperCase())
    .join('') || 'SP'

  return {
    initials,
    bgClass: 'bg-primary/10 border-primary/20 text-primary font-bold',
  }
}
