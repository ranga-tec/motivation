import { createContext, useContext, useEffect, useMemo, useState, type PropsWithChildren } from 'react'
import { Navigate, Outlet, useNavigate } from 'react-router-dom'
import { UserManager, WebStorageStateStore, type User } from 'oidc-client-ts'
import { LockKeyhole } from 'lucide-react'
import { registerTokenProvider } from '../api/client'
import { config, isOidcConfigured } from '../config'

/* oxlint-disable react/only-export-components */

interface AuthState { user?: User; roles: string[]; loading: boolean; accessError: string; signIn: () => Promise<void>; signOut: () => Promise<void> }
const AuthContext = createContext<AuthState | undefined>(undefined)

const manager = isOidcConfigured ? new UserManager({
  authority: config.oidc.authority!, client_id: config.oidc.clientId!, scope: config.oidc.scope,
  redirect_uri: config.oidc.redirectUri, post_logout_redirect_uri: config.oidc.postLogoutRedirectUri,
  response_type: 'code', userStore: new WebStorageStateStore({ store: window.sessionStorage }),
  automaticSilentRenew: true, loadUserInfo: true,
}) : undefined

export function AuthProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<User>()
  const [roles, setRoles] = useState<string[]>([])
  const [accessError, setAccessError] = useState('')
  const [loading, setLoading] = useState(Boolean(manager))
  const synchronize = async (value?: User | null) => {
    const active = value && !value.expired ? value : undefined
    setUser(active); setRoles([]); setAccessError('')
    if (!active) return
    const response = await fetch(`${config.apiUrl}/api/v1/session`, { headers: { Authorization: `Bearer ${active.access_token}` } })
    if (!response.ok) { setAccessError(response.status === 403 ? 'Your identity is not authorised for POMS.' : 'The POMS session could not be loaded.'); return }
    const session = await response.json() as { roles: string[] }
    setRoles(session.roles)
  }
  useEffect(() => {
    if (!manager) return
    manager.getUser().then(synchronize).catch(() => setAccessError('The secure session could not be restored.')).finally(() => setLoading(false))
    const loaded = (value: User) => { setLoading(true); void synchronize(value).finally(() => setLoading(false)) }
    const unloaded = () => { setUser(undefined); setRoles([]); setAccessError('') }
    manager.events.addUserLoaded(loaded); manager.events.addUserUnloaded(unloaded)
    return () => { manager.events.removeUserLoaded(loaded); manager.events.removeUserUnloaded(unloaded) }
  }, [])
  useEffect(() => registerTokenProvider(async () => (await manager?.getUser())?.access_token), [])
  const value = useMemo<AuthState>(() => ({ user, roles, loading, accessError, signIn: () => manager?.signinRedirect() ?? Promise.resolve(), signOut: () => manager?.signoutRedirect() ?? Promise.resolve() }), [user, roles, loading, accessError])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => { const value = useContext(AuthContext); if (!value) throw new Error('AuthProvider is missing'); return value }

export function ProtectedRoute() {
  const { user, loading, accessError, signOut } = useAuth()
  if (loading) return <div className="full-page-state"><span className="spinner" /> Restoring secure session…</div>
  if (accessError) return <div className="full-page-state"><div className="error-panel" role="alert">{accessError}<button className="button secondary" onClick={() => void signOut()}>Sign out</button></div></div>
  if (!user) return <Navigate to="/signin" replace />
  return <Outlet />
}

export function SignInPage() {
  const { user, signIn } = useAuth()
  const [error, setError] = useState('')
  if (user) return <Navigate to="/" replace />
  return <main className="signin-page"><section className="signin-card">
    <div className="brand-mark large">P</div><p className="eyebrow">POMS clinical workspace</p>
    <h1>Secure staff sign in</h1><p>Access patient records, appointments, and operational tools through your organisation's identity provider.</p>
    {isOidcConfigured ? <button className="button primary full" onClick={() => { setError(''); void signIn().catch(() => setError('The identity provider is unavailable. Contact your system administrator.')) }}><LockKeyhole size={18} /> Continue securely</button> : <div className="config-warning"><strong>Identity provider not configured</strong><span>Add the VITE_OIDC_AUTHORITY and VITE_OIDC_CLIENT_ID deployment variables.</span></div>}
    {error && <div className="error-panel" role="alert">{error}</div>}
    <small>Protected healthcare information. Authorised personnel only.</small>
  </section></main>
}

export function AuthCallback() {
  const navigate = useNavigate()
  const [error, setError] = useState(manager ? '' : 'Identity provider is not configured.')
  useEffect(() => { if (!manager) return; manager.signinRedirectCallback().then(() => navigate('/', { replace: true })).catch((reason: Error) => setError(reason.message)) }, [navigate])
  return <div className="full-page-state">{error ? <div className="error-panel">Sign-in failed: {error}</div> : <><span className="spinner" /> Completing secure sign-in…</>}</div>
}
