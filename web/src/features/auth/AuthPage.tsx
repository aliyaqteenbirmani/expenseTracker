import { useState, useEffect, type FormEvent } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Wallet,
  Sparkles,
  ArrowRight,
  Mail,
  Lock,
  CheckCircle2,
  AlertCircle,
  Loader2,
} from 'lucide-react'
import type { User } from '@/App'

interface AuthPageProps {
  onLogin: (user: User) => void
  api: <T>(path: string, token?: string, options?: RequestInit) => Promise<T>
}

export function AuthPage({ onLogin, api }: AuthPageProps) {
  const [mode, setMode] = useState<'login' | 'register' | 'forgot' | 'reset'>('login')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [gender, setGender] = useState('')
  const [forgotEmail, setForgotEmail] = useState('')
  const [resetToken, setResetToken] = useState('')
  const [resetEmail, setResetEmail] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const inviteParam = params.get('inviteToken')
    const resetParam = params.get('resetToken')
    const tokenParam = params.get('token')
    const emailParam = params.get('email')

    if (inviteParam) {
      sessionStorage.setItem('spendwise-invite-token', inviteParam)
      setNotice(
        "You've been invited to collaborate! Please sign in or create an account to accept your invitation."
      )
    } else if (resetParam || (tokenParam && emailParam)) {
      setResetToken(resetParam || tokenParam || '')
      if (emailParam) setResetEmail(emailParam)
      setMode('reset')
    } else if (tokenParam) {
      sessionStorage.setItem('spendwise-invite-token', tokenParam)
      setNotice(
        "You've been invited to collaborate! Please sign in or create an account to accept your invitation."
      )
    }
  }, [])

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      const user = await api<User>('/api/Auth/Login', undefined, {
        method: 'POST',
        body: JSON.stringify({ userName: username, password }),
      })
      if (!(user.accessToken ?? user.AccessToken)) {
        throw new Error('Login response did not include an access token.')
      }
      sessionStorage.setItem('spendwise-user', JSON.stringify(user))
      onLogin(user)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to sign in.')
    } finally {
      setBusy(false)
    }
  }

  async function register(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError('')
    setNotice('')
    const fields = new FormData(event.currentTarget)
    try {
      await api('/api/Auth/Registration', undefined, {
        method: 'POST',
        body: JSON.stringify({
          firstName: fields.get('firstName'),
          lastName: fields.get('lastName'),
          email: fields.get('email'),
          phone: fields.get('phone'),
          gender: gender || 'Other',
          password: fields.get('password'),
        }),
      })
      setUsername(String(fields.get('email')))
      setPassword('')
      setMode('login')
      setNotice('Account created successfully! Sign in with your email and password.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to create your account.')
    } finally {
      setBusy(false)
    }
  }

  async function handleForgotPassword(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await api(`/api/Auth/forgot-password?email=${encodeURIComponent(forgotEmail)}`, undefined, {
        method: 'POST',
      })
      setNotice('If that email exists in our system, recovery instructions have been sent.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not process password recovery.')
    } finally {
      setBusy(false)
    }
  }

  async function handleResetPassword(event: FormEvent) {
    event.preventDefault()
    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await api('/api/Auth/reset-password', undefined, {
        method: 'POST',
        body: JSON.stringify({
          email: resetEmail,
          token: resetToken,
          newPassword,
        }),
      })
      setNotice('Password reset successfully! You can now log in.')
      setMode('login')
      setUsername(resetEmail)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not reset password.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="flex min-h-screen w-full items-center justify-center p-4 bg-muted/30">
      <div className="w-full max-w-md space-y-6">
        {/* Brand Header */}
        <div className="flex flex-col items-center space-y-2 text-center">
          <div className="flex size-12 items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-md ring-4 ring-primary/10">
            <Wallet className="size-6 stroke-[2]" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground flex items-center gap-1.5">
            <span>Spendwise</span>
            <Sparkles className="size-4 text-amber-500 fill-amber-500" />
          </h1>
          <p className="text-xs text-muted-foreground">
            Enterprise Financial Ledger & Collaborative Cashbooks
          </p>
        </div>

        {/* Auth Card */}
        <Card className="border-border/80 shadow-md">
          <CardHeader className="space-y-1 pb-4">
            <CardTitle className="text-lg font-semibold tracking-tight">
              {mode === 'register'
                ? 'Create your account'
                : mode === 'forgot'
                ? 'Reset your password'
                : mode === 'reset'
                ? 'Choose a new password'
                : 'Sign in to your account'}
            </CardTitle>
            <CardDescription className="text-xs">
              {mode === 'register'
                ? 'Set up your account to start managing cashbooks and expenses.'
                : mode === 'forgot'
                ? 'Enter your email to receive recovery instructions.'
                : mode === 'reset'
                ? 'Enter your reset token and new secure password.'
                : 'Enter your credentials to access your workspaces.'}
            </CardDescription>
          </CardHeader>

          <CardContent className="space-y-4">
            {notice && (
              <Alert className="border-inflow/30 bg-inflow-muted/50 text-inflow text-xs py-2.5">
                <CheckCircle2 className="size-4" />
                <AlertDescription>{notice}</AlertDescription>
              </Alert>
            )}

            {error && (
              <Alert variant="destructive" className="text-xs py-2.5">
                <AlertCircle className="size-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            {/* Login Form */}
            {mode === 'login' && (
              <form onSubmit={submit} className="space-y-3">
                <div className="space-y-1.5">
                  <Label htmlFor="loginEmail" className="text-xs">
                    Email address
                  </Label>
                  <div className="relative">
                    <Mail className="absolute left-2.5 top-2.5 size-3.5 text-muted-foreground" />
                    <Input
                      id="loginEmail"
                      type="email"
                      autoComplete="username"
                      required
                      placeholder="you@example.com"
                      value={username}
                      onChange={(e) => setUsername(e.target.value)}
                      className="pl-8 h-9 text-xs"
                    />
                  </div>
                </div>

                <div className="space-y-1.5">
                  <div className="flex items-center justify-between">
                    <Label htmlFor="loginPass" className="text-xs">
                      Password
                    </Label>
                    <button
                      type="button"
                      onClick={() => {
                        setMode('forgot')
                        setError('')
                        setNotice('')
                      }}
                      className="text-[11px] text-primary hover:underline"
                    >
                      Forgot password?
                    </button>
                  </div>
                  <div className="relative">
                    <Lock className="absolute left-2.5 top-2.5 size-3.5 text-muted-foreground" />
                    <Input
                      id="loginPass"
                      type="password"
                      autoComplete="current-password"
                      required
                      placeholder="••••••••"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      className="pl-8 h-9 text-xs"
                    />
                  </div>
                </div>

                <Button type="submit" disabled={busy} className="w-full h-9 text-xs gap-1.5 mt-2">
                  {busy ? <Loader2 className="size-3.5 animate-spin" /> : null}
                  <span>{busy ? 'Signing in…' : 'Sign in'}</span>
                  {!busy && <ArrowRight className="size-3.5" />}
                </Button>
              </form>
            )}

            {/* Register Form */}
            {mode === 'register' && (
              <form onSubmit={register} className="space-y-3">
                <div className="grid grid-cols-2 gap-2">
                  <div className="space-y-1">
                    <Label htmlFor="regFirst" className="text-xs">
                      First Name
                    </Label>
                    <Input
                      id="regFirst"
                      name="firstName"
                      required
                      maxLength={50}
                      placeholder="Jane"
                      className="h-8 text-xs"
                    />
                  </div>
                  <div className="space-y-1">
                    <Label htmlFor="regLast" className="text-xs">
                      Last Name
                    </Label>
                    <Input
                      id="regLast"
                      name="lastName"
                      maxLength={50}
                      placeholder="Doe"
                      className="h-8 text-xs"
                    />
                  </div>
                </div>

                <div className="space-y-1">
                  <Label htmlFor="regEmail" className="text-xs">
                    Email address
                  </Label>
                  <Input
                    id="regEmail"
                    name="email"
                    type="email"
                    required
                    placeholder="you@example.com"
                    className="h-8 text-xs"
                  />
                </div>

                <div className="grid grid-cols-2 gap-2">
                  <div className="space-y-1">
                    <Label htmlFor="regPhone" className="text-xs">
                      Phone
                    </Label>
                    <Input
                      id="regPhone"
                      name="phone"
                      type="tel"
                      required
                      placeholder="+92 300 1234567"
                      className="h-8 text-xs"
                    />
                  </div>
                  <div className="space-y-1">
                    <Label className="text-xs">Gender</Label>
                    <Select value={gender} onValueChange={(val) => val && setGender(val)}>
                      <SelectTrigger className="h-8 text-xs">
                        <SelectValue placeholder="Gender" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Male">Male</SelectItem>
                        <SelectItem value="Female">Female</SelectItem>
                        <SelectItem value="Other">Other</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="space-y-1">
                  <Label htmlFor="regPass" className="text-xs">
                    Password
                  </Label>
                  <Input
                    id="regPass"
                    name="password"
                    type="password"
                    required
                    minLength={8}
                    placeholder="Minimum 8 characters"
                    className="h-8 text-xs"
                  />
                </div>

                <Button type="submit" disabled={busy} className="w-full h-9 text-xs gap-1.5 mt-2">
                  {busy ? <Loader2 className="size-3.5 animate-spin" /> : null}
                  <span>{busy ? 'Creating account…' : 'Create account'}</span>
                  {!busy && <ArrowRight className="size-3.5" />}
                </Button>
              </form>
            )}

            {/* Forgot Form */}
            {mode === 'forgot' && (
              <form onSubmit={handleForgotPassword} className="space-y-3">
                <div className="space-y-1.5">
                  <Label htmlFor="fEmail" className="text-xs">
                    Email address
                  </Label>
                  <Input
                    id="fEmail"
                    type="email"
                    required
                    placeholder="Enter registered email"
                    value={forgotEmail}
                    onChange={(e) => setForgotEmail(e.target.value)}
                    className="h-9 text-xs"
                  />
                </div>
                <Button type="submit" disabled={busy} className="w-full h-9 text-xs gap-1.5">
                  {busy ? <Loader2 className="size-3.5 animate-spin" /> : <Mail className="size-3.5" />}
                  <span>{busy ? 'Sending link…' : 'Send recovery link'}</span>
                </Button>
              </form>
            )}

            {/* Reset Form */}
            {mode === 'reset' && (
              <form onSubmit={handleResetPassword} className="space-y-3">
                <div className="space-y-1">
                  <Label className="text-xs">Account Email</Label>
                  <Input
                    type="email"
                    required
                    value={resetEmail}
                    onChange={(e) => setResetEmail(e.target.value)}
                    className="h-8 text-xs"
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Reset Token</Label>
                  <Input
                    type="text"
                    required
                    value={resetToken}
                    onChange={(e) => setResetToken(e.target.value)}
                    className="h-8 text-xs"
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">New Password</Label>
                  <Input
                    type="password"
                    required
                    minLength={8}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="h-8 text-xs"
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Confirm New Password</Label>
                  <Input
                    type="password"
                    required
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    className="h-8 text-xs"
                  />
                </div>
                <Button type="submit" disabled={busy} className="w-full h-9 text-xs gap-1.5">
                  {busy ? <Loader2 className="size-3.5 animate-spin" /> : null}
                  <span>Reset Password</span>
                </Button>
              </form>
            )}
          </CardContent>

          <CardFooter className="flex justify-center border-t py-3 text-xs text-muted-foreground">
            {mode === 'login' ? (
              <div>
                Don&apos;t have an account?{' '}
                <button
                  type="button"
                  onClick={() => {
                    setMode('register')
                    setError('')
                    setNotice('')
                  }}
                  className="font-semibold text-primary hover:underline ml-1"
                >
                  Create one now
                </button>
              </div>
            ) : (
              <div>
                Already have an account?{' '}
                <button
                  type="button"
                  onClick={() => {
                    setMode('login')
                    setError('')
                    setNotice('')
                  }}
                  className="font-semibold text-primary hover:underline ml-1"
                >
                  Sign in
                </button>
              </div>
            )}
          </CardFooter>
        </Card>
      </div>
    </div>
  )
}
