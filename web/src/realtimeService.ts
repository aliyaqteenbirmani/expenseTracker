import * as signalR from '@microsoft/signalr'

export type RealtimeInvitationPayload = {
  type: string
  businessId: string
  businessName: string
  cashbookId?: string
  cashbookName?: string
  invitedByName?: string
  acceptedByName?: string
  acceptedUserId?: string
  remarks?: string
  message: string
}

export type RealtimeTransactionPayload = {
  type: string
  cashBookId: string
  cashbookName: string
  transaction: {
    id: string
    cashBookId: string
    amount: number
    transactionType: string
    remarks?: string
    transactionDate?: string
    fileName?: string
    filePath?: string
    createdBy?: string
    [key: string]: unknown
  }
  createdByName: string
  creatorUserId: string
  message: string
}

export type RealtimeTransactionUpdatePayload = {
  type: string
  cashBookId: string
  cashbookName: string
  transaction: {
    id: string
    cashBookId: string
    amount: number
    transactionType: string
    remarks?: string
    transactionDate?: string
    fileName?: string
    filePath?: string
    createdBy?: string
    [key: string]: unknown
  }
  modifiedByName: string
  modifierUserId: string
  message: string
}

export type RealtimeTransactionDeletePayload = {
  type: string
  cashBookId: string
  cashbookName: string
  transactionId: string
  deletedByName: string
  deleterUserId: string
  message: string
}

export type RealtimeHandlers = {
  onInvitationReceived?: (payload: RealtimeInvitationPayload) => void
  onInvitationAccepted?: (payload: RealtimeInvitationPayload) => void
  onWorkspacesChanged?: (payload: unknown) => void
  onCashbookCreated?: (payload: { businessId: string; cashbookId: string; cashbookName: string; createdByName: string; message: string }) => void
  onTransactionCreated?: (payload: RealtimeTransactionPayload) => void
  onTransactionUpdated?: (payload: RealtimeTransactionUpdatePayload) => void
  onTransactionDeleted?: (payload: RealtimeTransactionDeletePayload) => void
}

class RealtimeService {
  private connection: signalR.HubConnection | null = null
  private handlers: RealtimeHandlers = {}
  private currentCashbookId: string | null = null
  private currentBusinessId: string | null = null
  private isConnecting = false

  public init(token: string, apiBase: string, handlers: RealtimeHandlers) {
    this.handlers = handlers

    if (this.connection) {
      return
    }

    const hubUrl = `${apiBase}/hubs/app`

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    this.connection.on('ReceiveInvitation', (payload: RealtimeInvitationPayload) => {
      this.handlers.onInvitationReceived?.(payload)
    })

    this.connection.on('InvitationAccepted', (payload: RealtimeInvitationPayload) => {
      this.handlers.onInvitationAccepted?.(payload)
    })

    this.connection.on('WorkspacesChanged', (payload: unknown) => {
      this.handlers.onWorkspacesChanged?.(payload)
    })

    this.connection.on('CashbookCreated', (payload: { businessId: string; cashbookId: string; cashbookName: string; createdByName: string; message: string }) => {
      this.handlers.onCashbookCreated?.(payload)
    })

    this.connection.on('MemberJoined', (payload: unknown) => {
      this.handlers.onWorkspacesChanged?.(payload)
    })

    this.connection.on('TransactionCreated', (payload: RealtimeTransactionPayload) => {
      this.handlers.onTransactionCreated?.(payload)
    })

    this.connection.on('TransactionUpdated', (payload: RealtimeTransactionUpdatePayload) => {
      this.handlers.onTransactionUpdated?.(payload)
    })

    this.connection.on('TransactionDeleted', (payload: RealtimeTransactionDeletePayload) => {
      this.handlers.onTransactionDeleted?.(payload)
    })

    void this.startConnection()
  }

  public updateHandlers(handlers: RealtimeHandlers) {
    this.handlers = handlers
  }

  private async startConnection() {
    if (!this.connection || this.connection.state === signalR.HubConnectionState.Connected || this.isConnecting) {
      return
    }

    try {
      this.isConnecting = true
      await this.connection.start()
      this.isConnecting = false

      if (this.currentCashbookId) {
        await this.connection.invoke('JoinCashbook', this.currentCashbookId).catch(() => {})
      }
      if (this.currentBusinessId) {
        await this.connection.invoke('JoinBusiness', this.currentBusinessId).catch(() => {})
      }
    } catch {
      this.isConnecting = false
    }
  }

  public async setCashbook(cashbookId: string | null) {
    if (this.currentCashbookId === cashbookId) return

    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      if (this.currentCashbookId) {
        await this.connection.invoke('LeaveCashbook', this.currentCashbookId).catch(() => {})
      }
      if (cashbookId) {
        await this.connection.invoke('JoinCashbook', cashbookId).catch(() => {})
      }
    }

    this.currentCashbookId = cashbookId
  }

  public async setBusiness(businessId: string | null) {
    if (this.currentBusinessId === businessId) return

    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      if (this.currentBusinessId) {
        await this.connection.invoke('LeaveBusiness', this.currentBusinessId).catch(() => {})
      }
      if (businessId) {
        await this.connection.invoke('JoinBusiness', businessId).catch(() => {})
      }
    }

    this.currentBusinessId = businessId
  }

  public async disconnect() {
    if (this.connection) {
      try {
        await this.connection.stop()
      } catch {}
      this.connection = null
    }
    this.currentCashbookId = null
    this.currentBusinessId = null
    this.isConnecting = false
  }
}

export const realtimeService = new RealtimeService()
