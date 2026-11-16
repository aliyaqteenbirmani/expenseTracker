import { useState, useMemo, useCallback } from 'react'
import type { SplitGroupMemberDto, ParticipantSplitRequestDto } from '../types'
import {
  calculateEqualSplit,
  validateExactSplit,
  calculatePercentageSplit,
  calculatePartsSplit,
} from '../lib/calculationEngine'

export type SplitMethod = 'Equal' | 'Exact' | 'Percentage' | 'Shares'

export function useSplitCalculator(
  members: SplitGroupMemberDto[],
  totalAmount: number,
  initialMethod: SplitMethod = 'Equal'
) {
  const activeMembers = useMemo(() => members.filter((m) => m.isActive), [members])

  const [method, setMethod] = useState<SplitMethod>(initialMethod)

  // 1. Equal Split State
  const [equalSelectedIds, setEqualSelectedIds] = useState<string[]>(() =>
    activeMembers.map((m) => m.userId)
  )

  // 2. Exact Split State: userId -> amount string
  const [exactValues, setExactValues] = useState<Record<string, string>>({})

  // 3. Percentage Split State: userId -> percentage string
  const [percentageValues, setPercentageValues] = useState<Record<string, string>>({})

  // 4. Shares / Parts State: userId -> parts count (>= 1)
  const [shareParts, setShareParts] = useState<Record<string, number>>(() =>
    Object.fromEntries(activeMembers.map((m) => [m.userId, 1]))
  )

  // Update defaults when active members change
  const resetToMembers = useCallback(
    (newMembers: SplitGroupMemberDto[], preserveAmount?: number) => {
      const active = newMembers.filter((m) => m.isActive)
      setEqualSelectedIds(active.map((m) => m.userId))
      setShareParts(Object.fromEntries(active.map((m) => [m.userId, 1])))

      // If amount provided, pre-fill equal percentages and exact fractions
      if (preserveAmount && preserveAmount > 0 && active.length > 0) {
        const equalAmt = (preserveAmount / active.length).toFixed(2)
        const equalPct = (100 / active.length).toFixed(2)
        setExactValues(Object.fromEntries(active.map((m) => [m.userId, equalAmt])))
        setPercentageValues(Object.fromEntries(active.map((m) => [m.userId, equalPct])))
      } else {
        setExactValues({})
        setPercentageValues({})
      }
    },
    []
  )

  // Toggle single member in equal split
  const toggleEqualParticipant = useCallback((userId: string) => {
    setEqualSelectedIds((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    )
  }, [])

  const selectAllEqual = useCallback(() => {
    setEqualSelectedIds(activeMembers.map((m) => m.userId))
  }, [activeMembers])

  const deselectAllEqual = useCallback(() => {
    setEqualSelectedIds([])
  }, [])

  // Set exact amount for user
  const setExactValue = useCallback((userId: string, val: string) => {
    setExactValues((prev) => ({ ...prev, [userId]: val }))
  }, [])

  // Set percentage for user
  const setPercentageValue = useCallback((userId: string, val: string) => {
    setPercentageValues((prev) => ({ ...prev, [userId]: val }))
  }, [])

  // Parts modification
  const setSharePart = useCallback((userId: string, parts: number) => {
    setShareParts((prev) => ({ ...prev, [userId]: Math.max(0, parts) }))
  }, [])

  const incrementSharePart = useCallback((userId: string) => {
    setShareParts((prev) => ({ ...prev, [userId]: (prev[userId] ?? 0) + 1 }))
  }, [])

  const decrementSharePart = useCallback((userId: string) => {
    setShareParts((prev) => ({ ...prev, [userId]: Math.max(0, (prev[userId] ?? 1) - 1) }))
  }, [])

  // Live Computed Shares
  const calculatedShares = useMemo<Record<string, number>>(() => {
    if (totalAmount <= 0 || activeMembers.length === 0) return {}

    switch (method) {
      case 'Equal':
        return calculateEqualSplit(totalAmount, equalSelectedIds)

      case 'Exact': {
        const entries = activeMembers.map((m) => ({
          userId: m.userId,
          amount: parseFloat(exactValues[m.userId] || '0') || 0,
        }))
        const res: Record<string, number> = {}
        for (const e of entries) {
          if (e.amount > 0) res[e.userId] = e.amount
        }
        return res
      }

      case 'Percentage': {
        const entries = activeMembers.map((m) => ({
          userId: m.userId,
          percentage: parseFloat(percentageValues[m.userId] || '0') || 0,
        }))
        return calculatePercentageSplit(totalAmount, entries)
      }

      case 'Shares': {
        const entries = activeMembers.map((m) => ({
          userId: m.userId,
          parts: shareParts[m.userId] ?? 1,
        }))
        return calculatePartsSplit(totalAmount, entries)
      }
    }
  }, [method, totalAmount, activeMembers, equalSelectedIds, exactValues, percentageValues, shareParts])

  // Validation
  const validation = useMemo(() => {
    if (totalAmount <= 0) {
      return { isValid: false, message: 'Enter a valid amount greater than 0' }
    }

    if (method === 'Equal') {
      if (equalSelectedIds.length === 0) {
        return { isValid: false, message: 'Select at least one member to split with' }
      }
      const count = equalSelectedIds.length
      const perHead = (totalAmount / count).toFixed(2)
      return {
        isValid: true,
        message: `Rs. ${perHead}/person (${count} members included)`,
      }
    }

    if (method === 'Exact') {
      const entries = activeMembers.map((m) => ({
        userId: m.userId,
        amount: parseFloat(exactValues[m.userId] || '0') || 0,
      }))
      const check = validateExactSplit(totalAmount, entries)
      if (!check.isValid) {
        const remaining = check.remainingAmount
        return {
          isValid: false,
          remainingAmount: remaining,
          message:
            remaining > 0
              ? `Rs. ${remaining.toFixed(2)} left to allocate`
              : `Rs. ${Math.abs(remaining).toFixed(2)} over total amount`,
        }
      }
      return { isValid: true, message: 'All funds allocated exactly (Rs. 0.00 left)' }
    }

    if (method === 'Percentage') {
      const totalPct = activeMembers.reduce(
        (sum, m) => sum + (parseFloat(percentageValues[m.userId] || '0') || 0),
        0
      )
      const diff = Math.round((100 - totalPct) * 100) / 100
      if (Math.abs(diff) > 0.05) {
        return {
          isValid: false,
          remainingPercentage: diff,
          message: diff > 0 ? `${diff.toFixed(1)}% remaining` : `${Math.abs(diff).toFixed(1)}% over 100%`,
        }
      }
      return { isValid: true, message: '100% of expense allocated' }
    }

    if (method === 'Shares') {
      const totalParts = activeMembers.reduce((s, m) => s + (shareParts[m.userId] ?? 0), 0)
      if (totalParts <= 0) {
        return { isValid: false, message: 'At least one member must have shares' }
      }
      return { isValid: true, message: `Split into ${totalParts} total parts` }
    }

    return { isValid: true, message: '' }
  }, [method, totalAmount, activeMembers, equalSelectedIds, exactValues, percentageValues, shareParts])

  // Get Payload for API
  const getSplitsPayload = useCallback((): {
    splitType: string
    participantSplits: ParticipantSplitRequestDto[]
    participantUserIds: string[]
  } => {
    switch (method) {
      case 'Exact': {
        const splits = activeMembers
          .map((m) => ({
            userId: m.userId,
            exactAmount: parseFloat(exactValues[m.userId] || '0') || 0,
          }))
          .filter((s) => (s.exactAmount || 0) > 0)

        return {
          splitType: 'Exact',
          participantSplits: splits,
          participantUserIds: splits.map((s) => s.userId),
        }
      }

      case 'Percentage': {
        const splits = activeMembers
          .map((m) => ({
            userId: m.userId,
            percentage: parseFloat(percentageValues[m.userId] || '0') || 0,
          }))
          .filter((s) => (s.percentage || 0) > 0)

        return {
          splitType: 'Percentage',
          participantSplits: splits,
          participantUserIds: splits.map((s) => s.userId),
        }
      }

      case 'Shares': {
        const splits = activeMembers
          .map((m) => ({
            userId: m.userId,
            shareParts: shareParts[m.userId] ?? 1,
          }))
          .filter((s) => (s.shareParts || 0) > 0)

        return {
          splitType: 'Shares',
          participantSplits: splits,
          participantUserIds: splits.map((s) => s.userId),
        }
      }

      case 'Equal':
      default: {
        return {
          splitType: 'Equal',
          participantSplits: equalSelectedIds.map((userId) => ({ userId })),
          participantUserIds: equalSelectedIds,
        }
      }
    }
  }, [method, activeMembers, exactValues, percentageValues, shareParts, equalSelectedIds])

  return {
    method,
    setMethod,
    equalSelectedIds,
    setEqualSelectedIds,
    toggleEqualParticipant,
    selectAllEqual,
    deselectAllEqual,
    exactValues,
    setExactValue,
    setExactValues,
    percentageValues,
    setPercentageValue,
    shareParts,
    setSharePart,
    incrementSharePart,
    decrementSharePart,
    calculatedShares,
    validation,
    resetToMembers,
    getSplitsPayload,
  }
}
