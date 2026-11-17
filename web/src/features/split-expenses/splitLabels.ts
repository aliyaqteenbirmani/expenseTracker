/**
 * Centralized, jargon-free labels and text constants for Split Expenses.
 * Follows UI Step 1: Plain language, consistent everywhere, no accounting jargon.
 */

export function pluralize(count: number, singular: string, plural = `${singular}s`): string {
  return `${count} ${count === 1 ? singular : plural}`
}

export function pluralGroups(count: number): string {
  return pluralize(count, 'group')
}

export function pluralMembers(count: number): string {
  return pluralize(count, 'member')
}

export function pluralExpenses(count: number): string {
  return pluralize(count, 'expense')
}

export const splitLabels = {
  // 1. Dashboard KPI cards
  dashboard: {
    youWillGet: {
      title: 'You Will Get',
      subtext: 'from others',
    },
    youNeedToPay: {
      title: 'You Need to Pay',
      subtext: 'to others',
    },
    overallBalance: {
      title: 'Your Overall Balance',
      subtext: 'Across all groups',
      settled: 'Rs. 0.00',
      owed: (formattedAmount: string) => `+${formattedAmount}`,
      owe: (formattedAmount: string) => `-${formattedAmount}`,
    },
    myGroups: {
      title: 'Active Groups',
      count: (count: number) => pluralGroups(count),
    },
  },

  // 2. Group Overview cards
  overview: {
    groupSpending: {
      title: 'Group Spending',
      subtext: 'All expenses',
    },
    yourBalance: {
      title: 'Your Balance',
      settled: 'All settled up',
      youAreOwed: 'You are owed',
      youOwe: 'You owe',
      waitingConfirmation: (formattedAmount: string) => `${formattedAmount} waiting for confirmation`,
    },
    paymentsSettled: {
      title: 'Payments Settled',
      subtext: 'Confirmed',
    },
    members: {
      title: 'Members',
      owner: (ownerName: string) => `Owner: ${ownerName}`,
      count: (count: number) => pluralMembers(count),
    },
  },

  // 3. Navigation Tabs
  tabs: {
    overview: 'Overview',
    expenses: (count: number) => `Expenses (${count})`,
    whoOwesWhom: 'Who Owes Whom', // count removed as required
    settleUp: 'Settle Up',
    members: (count: number) => `Members (${count})`,
    activity: 'Activity',
  },

  // 4. Header buttons & Actions
  actions: {
    addExpense: 'Add Expense',
    recordPayment: 'Record Payment', // renamed from "Settle Up" to avoid duplicate with tab
    recordCustomPayment: 'Record Custom Payment',
    createGroup: 'Create Group',
  },

  // 5. Suggested Payments
  suggestedPayments: {
    title: 'Suggested Payments',
    subtext: "The fewest payments needed to clear everyone's balance.",
    allSettledTitle: 'Everyone is settled up!',
    allSettledSubtext: 'There are no debts to settle in this group.',
  },

  // 6. Expense rows
  expense: {
    yourShare: (formattedAmount: string) => `Your share: ${formattedAmount}`,
    notIncluded: 'Not included',
  },

  // 7. Who Owes Whom (Balances tab)
  whoOwesWhom: {
    title: 'Member Balances',
    helperText: 'Positive means they should get money back. Negative means they owe money.',
    status: 'Status',
    settledUp: 'Settled up',
    youAreOwed: (formattedAmount: string) => `You are owed ${formattedAmount}`,
    youOwe: (formattedAmount: string) => `You owe ${formattedAmount}`,
    memberIsOwed: (name: string, formattedAmount: string) => `${name} is owed ${formattedAmount}`,
    memberOwes: (name: string, formattedAmount: string) => `${name} owes ${formattedAmount}`,
  },

  // 8. Settlement statuses shown to users
  settlementStatus: {
    waitingFor: (name: string) => `Waiting for ${name}`,
    waitingForOwner: 'Waiting for group owner',
    confirmed: 'Confirmed',
    declined: 'Declined',
    cancelled: 'Cancelled',
  },
} as const
