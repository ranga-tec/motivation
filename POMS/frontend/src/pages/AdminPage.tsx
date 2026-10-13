import { useMemo, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { KeyRound, LockKeyhole, Pencil, Search, ShieldCheck, UnlockKeyhole, UserPlus, Users, Wand2 } from 'lucide-react'
import { api } from '../api/client'
import type { AdminUser } from '../api/types'
import { useAuth } from '../auth/AuthProvider'
import { Avatar, Badge, Card, ConfirmModal, EmptyState, ErrorPanel, Field, FormModal, PageHeader, Skeleton } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { errorMessage, roleLabel } from '../lib/format'
import { useToast } from '../lib/toast'

const roleHelp: Record<string, string> = {
  ADMIN: 'Everything, including users and reference data',
  DATA_ENTRY: 'Registers patients and edits patient details',
  CLINICIAN: 'Records, assessments, fittings, deliveries, follow-ups',
  MANAGEMENT: 'Dashboard, patient folder (view) and reports',
  VIEWER: 'Reports only',
}
type Editor = Partial<AdminUser> & { password?: string }

export function AdminPage() {
  const { roles } = useAuth()
  const notify = useToast()
  const authorized = roles.includes('ADMIN')
  const data = useApi(() => api.adminUsers(), 'admin-users', authorized)
  const [query, setQuery] = useState('')
  const [role, setRole] = useState('')
  const [editor, setEditor] = useState<Editor>()
  const [password, setPassword] = useState<AdminUser>()
  const [locking, setLocking] = useState<AdminUser>()
  const users = useMemo(() => (data.data?.users ?? []).filter((user) => {
    const text = `${user.fullName} ${user.email} ${user.employeeNumber} ${user.designation}`.toLowerCase()
    return text.includes(query.trim().toLowerCase()) && (!role || user.roles.includes(role))
  }), [data.data, query, role])
  if (!authorized) return <Navigate to="/" replace />

  const toggleLock = async (user: AdminUser) => {
    try { await api.setAdminLock(user.id, !user.isLocked); notify(`${user.fullName} was ${user.isLocked ? 'unlocked' : 'locked'}.`); data.refresh() }
    catch (error) { notify(errorMessage(error, 'The account could not be updated.'), 'error') }
    finally { setLocking(undefined) }
  }

  return <>
    <PageHeader eyebrow="Administration" title="Users & access" description="Create staff accounts, assign roles, reset passwords and lock access." actions={<button className="btn btn-primary" onClick={() => setEditor({ roles: ['CLINICIAN'], canAccessRestrictedClinicalData: false })}><UserPlus /> Create user</button>} />
    <Card bodyless>
      <div className="card-toolbar">
        <label className="search-input"><Search /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search name, email, employee number" aria-label="Search users" /></label>
        <select className="select" style={{ width: 200 }} value={role} onChange={(event) => setRole(event.target.value)} aria-label="Role"><option value="">All roles</option>{data.data?.availableRoles.map((item) => <option key={item} value={item}>{roleLabel(item)}</option>)}</select>
      </div>
      {data.error ? <ErrorPanel message={data.error} retry={data.refresh} /> : data.loading ? <Skeleton rows={6} tall /> : !users.length
        ? <EmptyState icon={<Users />} title="No users found" description="Adjust the search or role filter." />
        : <div className="table-wrap"><table className="table responsive">
          <thead><tr><th>User</th><th>Employee no.</th><th>Designation</th><th>Roles</th><th>Status</th><th className="actions"><span className="sr-only">Actions</span></th></tr></thead>
          <tbody>{users.map((user) => <tr key={user.id}>
            <td className="primary-cell"><div className="cell-person"><Avatar name={user.fullName} /><div><strong>{user.fullName}{user.isCurrentUser && <span className="muted" style={{ fontWeight: 400 }}> (you)</span>}</strong><span>{user.email}</span></div></div></td>
            <td data-label="Employee no."><span className="mono">{user.employeeNumber}</span></td>
            <td data-label="Designation"><div>{user.designation}{user.department && <span className="cell-sub">{user.department}</span>}</div></td>
            <td data-label="Roles"><div className="chips" style={{ justifyContent: 'inherit' }}>{user.roles.map((item) => <span key={item} className="chip">{roleLabel(item)}</span>)}{user.canAccessRestrictedClinicalData && <span className="chip" title="Can open restricted entries"><ShieldCheck size={12} style={{ marginRight: 3 }} />Restricted</span>}</div></td>
            <td data-label="Status"><Badge value={user.isLocked ? 'Locked' : 'Active'} tone={user.isLocked ? 'tone-red' : 'tone-green'} /></td>
            <td className="actions"><div>
              <button className="icon-btn sm" title="Edit profile and roles" aria-label={`Edit ${user.fullName}`} onClick={() => setEditor(user)}><Pencil /></button>
              <button className="icon-btn sm" title="Reset password" aria-label={`Reset password for ${user.fullName}`} onClick={() => setPassword(user)}><KeyRound /></button>
              <button className={`icon-btn sm ${user.isLocked ? '' : 'danger'}`} title={user.isLocked ? 'Unlock' : 'Lock'} aria-label={`${user.isLocked ? 'Unlock' : 'Lock'} ${user.fullName}`} disabled={user.isCurrentUser} onClick={() => setLocking(user)}>{user.isLocked ? <UnlockKeyhole /> : <LockKeyhole />}</button>
            </div></td>
          </tr>)}</tbody>
        </table></div>}
      {data.data && <div className="card-footer"><span>{users.length} of {data.data.users.length} accounts</span></div>}
    </Card>
    {editor && <UserDialog value={editor} availableRoles={data.data?.availableRoles ?? []} close={() => setEditor(undefined)} saved={(message) => { setEditor(undefined); notify(message); data.refresh() }} />}
    {password && <PasswordDialog user={password} close={() => setPassword(undefined)} saved={() => { notify(`Password reset for ${password.fullName}.`); setPassword(undefined) }} />}
    {locking && <ConfirmModal danger={!locking.isLocked} title={locking.isLocked ? 'Unlock account?' : 'Lock account?'} message={locking.isLocked ? `${locking.fullName} will be able to sign in again.` : `${locking.fullName} will be signed out and unable to sign in until unlocked.`} confirmLabel={locking.isLocked ? 'Unlock' : 'Lock account'} onClose={() => setLocking(undefined)} onConfirm={() => toggleLock(locking)} />}
  </>
}

const generatePassword = () => {
  const sets = ['ABCDEFGHJKLMNPQRSTUVWXYZ', 'abcdefghijkmnopqrstuvwxyz', '23456789', '!@#$%&*?']
  const random = (text: string) => text[crypto.getRandomValues(new Uint32Array(1))[0] % text.length]
  const chars = [...sets.map(random), ...Array.from({ length: 10 }, () => random(sets.join('')))]
  return chars.sort(() => crypto.getRandomValues(new Uint8Array(1))[0] - 128).join('')
}

function PasswordInput({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  return <div style={{ display: 'flex', gap: 8 }}><input required minLength={8} type="text" autoComplete="new-password" value={value} onChange={(event) => onChange(event.target.value)} style={{ flex: 1 }} className="input mono" /><button type="button" className="btn btn-secondary" onClick={() => onChange(generatePassword())}><Wand2 /> Generate</button></div>
}

function UserDialog({ value, availableRoles, close, saved }: { value: Editor; availableRoles: string[]; close: () => void; saved: (message: string) => void }) {
  const [form, setForm] = useState<Editor>(value)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const creating = !value.id
  const set = (patch: Partial<Editor>) => setForm((current) => ({ ...current, ...patch }))
  const roles = form.roles ?? []
  const submit = async () => {
    if (!roles.length) { setError('Choose at least one role.'); return }
    setSubmitting(true); setError('')
    try {
      if (creating) { await api.createAdminUser({ ...form, roles }); saved(`Account created for ${form.fullName}. Share the initial password securely.`) }
      else {
        await api.updateAdminProfile(value.id!, form as Record<string, unknown>)
        if ([...roles].sort().join() !== [...(value.roles ?? [])].sort().join()) await api.updateAdminRoles(value.id!, roles)
        saved(`${form.fullName}'s account was updated.`)
      }
    } catch (failure) { setError(errorMessage(failure, 'The account could not be saved.')) }
    finally { setSubmitting(false) }
  }
  return <FormModal size="md" title={creating ? 'Create staff account' : 'Edit staff account'} description={creating ? 'The user signs in with this email and the initial password.' : value.email} onClose={close} onSubmit={submit} submitLabel={creating ? 'Create account' : 'Save changes'} submitting={submitting} error={error}>
    <div className="form-grid">
      {creating && <><Field label="Email" required><input required type="email" autoComplete="off" value={form.email ?? ''} onChange={(event) => set({ email: event.target.value })} /></Field>
        <Field label="Initial password" required hint="8+ characters with upper and lower case, a number and a symbol."><PasswordInput value={form.password ?? ''} onChange={(password) => set({ password })} /></Field></>}
      <Field label="Full name" required><input required maxLength={150} value={form.fullName ?? ''} onChange={(event) => set({ fullName: event.target.value })} /></Field>
      <Field label="Employee number" required><input required maxLength={50} value={form.employeeNumber ?? ''} onChange={(event) => set({ employeeNumber: event.target.value })} /></Field>
      <Field label="Designation" required><input required maxLength={100} value={form.designation ?? ''} onChange={(event) => set({ designation: event.target.value })} placeholder="e.g. Prosthetist" /></Field>
      <Field label="Department"><input maxLength={100} value={form.department ?? ''} onChange={(event) => set({ department: event.target.value })} /></Field>
      <Field label="Mobile number" required><input required type="tel" maxLength={30} value={form.mobileNumber ?? ''} onChange={(event) => set({ mobileNumber: event.target.value })} /></Field>
      <Field label="Work phone"><input type="tel" maxLength={30} value={form.workPhoneNumber ?? ''} onChange={(event) => set({ workPhoneNumber: event.target.value })} /></Field>
    </div>
    <div className="form-section">
      <div className="form-section-title"><div><h3>Roles</h3><p>Roles decide which screens and actions this person can use.</p></div></div>
      <div className="role-grid">{availableRoles.map((item) => <label key={item} className="check"><input type="checkbox" checked={roles.includes(item)} disabled={value.isCurrentUser && item === 'ADMIN'} onChange={(event) => set({ roles: event.target.checked ? [...roles, item] : roles.filter((roleName) => roleName !== item) })} /><span><strong>{roleLabel(item)}</strong><small>{roleHelp[item] ?? item}</small></span></label>)}</div>
      <label className="check" style={{ marginTop: 12 }}><input type="checkbox" checked={form.canAccessRestrictedClinicalData ?? false} onChange={(event) => set({ canAccessRestrictedClinicalData: event.target.checked })} /><span><strong>Restricted clinical data</strong><small>Can open records and files marked as restricted by other staff.</small></span></label>
    </div>
  </FormModal>
}

function PasswordDialog({ user, close, saved }: { user: AdminUser; close: () => void; saved: () => void }) {
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const submit = async () => { setSubmitting(true); setError(''); try { await api.resetAdminPassword(user.id, password); saved() } catch (failure) { setError(errorMessage(failure, 'The password could not be reset.')) } finally { setSubmitting(false) } }
  return <FormModal title="Reset password" description={`${user.fullName} · ${user.email}`} onClose={close} onSubmit={submit} submitLabel="Reset password" submitting={submitting} disabled={password.length < 8} error={error}>
    <Field label="New password" required hint="Share it with the user through a secure channel."><PasswordInput value={password} onChange={setPassword} /></Field>
  </FormModal>
}
