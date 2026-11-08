import React, { Component, type ReactNode } from 'react'
import { AlertTriangle, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Unhandled UI Error caught by ErrorBoundary:', error, errorInfo)
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex items-center justify-center p-6 bg-background text-foreground">
          <div className="max-w-md w-full rounded-2xl border border-border/80 bg-card p-6 shadow-xl space-y-4 text-center">
            <div className="mx-auto flex size-12 items-center justify-center rounded-xl bg-destructive/10 text-destructive">
              <AlertTriangle className="size-6" />
            </div>
            <div className="space-y-1">
              <h2 className="text-lg font-semibold tracking-tight">Something went wrong</h2>
              <p className="text-xs text-muted-foreground">
                An unexpected UI error occurred. You can reload the page or try again.
              </p>
            </div>
            {this.state.error?.message && (
              <div className="rounded-lg bg-muted/50 p-2.5 text-left text-xs font-mono text-muted-foreground overflow-x-auto max-h-24">
                {this.state.error.message}
              </div>
            )}
            <div className="pt-2 flex justify-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => this.setState({ hasError: false, error: null })}
              >
                Try Again
              </Button>
              <Button
                size="sm"
                className="gap-1.5"
                onClick={() => window.location.reload()}
              >
                <RefreshCw className="size-3.5" />
                <span>Reload App</span>
              </Button>
            </div>
          </div>
        </div>
      )
    }

    return this.props.children
  }
}
