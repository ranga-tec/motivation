import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type FormEvent, type PropsWithChildren } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { LockKeyhole } from 'lucide-react'
import { registerTokenProvider } from '../api/client'
import { config } from '../config'

/* oxlint-disable react/only-export-components */

interface AuthUser { id: string; email: string; name: string }
interface AuthTokens { tokenType: string; accessToken: string; expiresAt: number; refreshToken: string }
interface TokenResponse { tokenType: string; accessToken: string; expiresIn: number; refreshToken: string }
interface SessionResponse { userId: string; email: string; displayName: string; roles: string[] }
interface AuthState {
  user?: AuthUser
  roles: string[]
  loading: boolean
  accessError: string
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => void
}

const tokenStorageKey = 'poms.auth.tokens'
const AuthContext = createContext<AuthState | undefined>(undefined)

const readTokens = (): AuthTokens | undefined => {
  try {
    const value = window.sessionStorage.getItem(tokenStorageKey)
    return value ? JSON.parse(value) as AuthTokens : undefined
  } catch { return undefined }
}

const toTokens = (response: TokenResponse): AuthTokens => ({
  tokenType: response.tokenType,
  accessToken: response.accessToken,
  expiresAt: Date.now() + response.expiresIn * 1000,
  refreshToken: response.refreshToken,
})

export function AuthProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<AuthUser>()
  const [roles, setRoles] = useState<string[]>([])
  const [accessError, setAccessError] = useState('')
  const [loading, setLoading] = useState(true)
  const tokens = useRef<AuthTokens | undefined>(readTokens())
  const refreshInProgress = useRef<Promise<string | undefined> | undefined>(undefined)

  const storeTokens = useCallback((value?: AuthTokens) => {
    tokens.current = value
    if (value) window.sessionStorage.setItem(tokenStorageKey, JSON.stringify(value))
    else window.sessionStorage.removeItem(tokenStorageKey)
  }, [])

  const refresh = useCallback(async (): Promise<string | undefined> => {
    if (!tokens.current?.refreshToken) return undefined
    const response = await fetch(`${config.apiUrl}/auth/refresh`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: tokens.current.refreshToken }),
    })
    if (!response.ok) { storeTokens(); return undefined }
    const next = toTokens(await response.json() as TokenResponse)
    storeTokens(next)
    return next.accessToken
  }, [storeTokens])

  const accessToken = useCallback(async () => {
    if (tokens.current && tokens.current.expiresAt > Date.now() + 30_000) return tokens.current.accessToken
    refreshInProgress.current ??= refresh().finally(() => { refreshInProgress.current = undefined })
    return refreshInProgress.current
  }, [refresh])

  const clearSession = useCallback(() => {
    storeTokens(); setUser(undefined); setRoles([]); setAccessError('')
  }, [storeTokens])

  const synchronize = useCallback(async () => {
    const token = await accessToken()
    if (!token) { clearSession(); return }
    const response = await fetch(`${config.apiUrl}/api/v1/session`, { headers: { Authorization: `Bearer ${token}` } })
    if (!response.ok) {
      clearSession()
      throw new Error(response.status === 403 ? 'Your account is not authorised for POMS.' : 'The POMS session could not be loaded.')
    }
    const session = await response.json() as SessionResponse
    setUser({ id: session.userId, email: session.email, name: session.displayName || session.email })
    setRoles(session.roles); setAccessError('')
  }, [accessToken, clearSession])

  useEffect(() => {
    registerTokenProvider(accessToken)
    void synchronize().catch((error: Error) => setAccessError(error.message)).finally(() => setLoading(false))
  }, [accessToken, synchronize])

  const signIn = useCallback(async (email: string, password: string) => {
    const response = await fetch(`${config.apiUrl}/auth/login?useCookies=false`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }),
    })
    if (!response.ok) throw new Error(response.status === 401 ? 'Incorrect email or password.' : 'Sign-in could not be completed.')
    storeTokens(toTokens(await response.json() as TokenResponse))
    await synchronize()
  }, [storeTokens, synchronize])

  const value = useMemo<AuthState>(() => ({ user, roles, loading, accessError, signIn, signOut: clearSession }), [user, roles, loading, accessError, signIn, clearSession])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => { const value = useContext(AuthContext); if (!value) throw new Error('AuthProvider is missing'); return value }

export function ProtectedRoute() {
  const { user, loading, accessError, signOut } = useAuth()
  if (loading) return <div className="full-page-state"><span className="spinner" /> Restoring secure session…</div>
  if (accessError) return <div className="full-page-state"><div className="error-panel" role="alert">{accessError}<button className="button secondary" onClick={signOut}>Sign out</button></div></div>
  if (!user) return <Navigate to="/signin" replace />
  return <Outlet />
}

export function SignInPage() {
  const { user, signIn } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  if (user) return <Navigate to="/" replace />
  const submit = async (event: FormEvent) => {
    event.preventDefault(); if (submitting) return
    setSubmitting(true); setError('')
    try { await signIn(email.trim(), password) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Sign-in could not be completed.') }
    finally { setSubmitting(false) }
  }
  return <main className="signin-page"><section className="signin-card">
    <div className="brand-mark large">P</div><p className="eyebrow">POMS clinical workspace</p>
    <h1>Secure staff sign in</h1><p>Enter your authorised POMS staff account.</p>
    <form onSubmit={(event) => void submit(event)}>
      <label className="form-field"><span>Email address</span><input type="email" autoComplete="username" required value={email} onChange={(event) => setEmail(event.target.value)} /></label>
      <label className="form-field"><span>Password</span><input type="password" autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} /></label>
      {error && <div className="error-panel" role="alert">{error}</div>}
      <button className="button primary full" type="submit" disabled={submitting}><LockKeyhole size={18} /> {submitting ? 'Signing in…' : 'Sign in securely'}</button>
    </form>
    <small>Protected healthcare information. Authorised personnel only.</small>
  </section></main>
}
