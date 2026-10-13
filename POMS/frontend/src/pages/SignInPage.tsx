import { useState, type FormEvent } from 'react'
import { Navigate } from 'react-router-dom'
import { ClipboardList, Eye, EyeOff, FileText, LockKeyhole, MapPin, ShieldCheck } from 'lucide-react'
import { useAuth } from '../auth/AuthProvider'
import { Alert, Field, Spinner } from '../components/Ui'

const year = new Date().getFullYear()

export function SignInPage() {
  const { user, signIn } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  if (user) return <Navigate to="/" replace />
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (submitting) return
    setSubmitting(true); setError('')
    try { await signIn(email.trim(), password) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Sign-in could not be completed.') }
    finally { setSubmitting(false) }
  }
  return <main className="signin">
    <aside className="signin-aside">
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}><div className="brand-mark">EL</div><div><strong style={{ fontSize: 16 }}>Exceed Lanka</strong><div style={{ color: '#9fb6dc', fontSize: 12.5 }}>Prosthetic &amp; Orthotic Services</div></div></div>
        <h1>Prosthetic &amp; Orthotic Data Management System</h1>
        <p>One patient folder for registration, assessments, prescriptions, fittings, deliveries and follow-ups — tracked under the patient number.</p>
        <div className="signin-points">
          <div><ClipboardList /> Complete clinical pathway from assessment to follow-up</div>
          <div><MapPin /> Ragama, Colombo and future service centres</div>
          <div><FileText /> Printable forms and thirteen operational reports</div>
        </div>
      </div>
      <small>© {year} Exceed Lanka · POMS</small>
    </aside>
    <section className="signin-main">
      <div className="signin-card">
        <h2>Sign in</h2>
        <p>Use your POMS staff account to continue.</p>
        <form onSubmit={(event) => void submit(event)}>
          {error && <Alert>{error}</Alert>}
          <Field label="Email address"><input type="email" autoComplete="username" required autoFocus value={email} onChange={(event) => setEmail(event.target.value)} placeholder="name@exceedlanka.org" /></Field>
          <Field label="Password">
            <div className="password-field"><input type={showPassword ? 'text' : 'password'} autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} style={{ width: '100%' }} />
              <button type="button" className="icon-btn sm" onClick={() => setShowPassword((value) => !value)} aria-label={showPassword ? 'Hide password' : 'Show password'}>{showPassword ? <EyeOff /> : <Eye />}</button></div>
          </Field>
          <button className="btn btn-primary btn-block" type="submit" disabled={submitting} style={{ height: 44 }}>{submitting ? <Spinner /> : <LockKeyhole />} {submitting ? 'Signing in…' : 'Sign in'}</button>
        </form>
        <div className="signin-foot"><ShieldCheck /> Protected health information. Authorised staff only.</div>
      </div>
    </section>
  </main>
}
