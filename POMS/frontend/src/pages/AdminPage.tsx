/* oxlint-disable react/set-state-in-effect -- initial API synchronization */
import { useEffect, useMemo, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { KeyRound, LockKeyhole, Pencil, Plus, UnlockKeyhole } from 'lucide-react'
import { ApiError, api } from '../api/client'
import type { AdminUser, AdminUsers } from '../api/types'
import { useAuth } from '../auth/AuthProvider'

type UserEditor = Partial<AdminUser> & { password?: string }
const message = (error: unknown) => error instanceof ApiError ? error.message : 'The change could not be saved.'

export function AdminPage() {
  const { roles } = useAuth()
  const authorized = roles.includes('ADMIN')
  const [data, setData] = useState<AdminUsers>()
  const [editor, setEditor] = useState<UserEditor>()
  const [passwordUser, setPasswordUser] = useState<AdminUser>()
  const [error, setError] = useState('')
  const load = async () => { try { setError(''); setData(await api.adminUsers()) } catch (failure) { setError(message(failure)) } }
  useEffect(() => { if (authorized) void load() }, [authorized])
  const save = async () => {
    if (!editor) return
    try {
      setError('')
      if (editor.id) await api.updateAdminProfile(editor.id, editor as Record<string, unknown>)
      else await api.createAdminUser({ ...editor, roles: editor.roles ?? ['VIEWER'] })
      setEditor(undefined); await load()
    } catch (failure) { setError(message(failure)) }
  }
  if (!authorized) return <Navigate to="/" replace />
  if (!data) return <div className="loading-state">Loading administration workspace...</div>
  return <>
    <header className="page-header"><div><p className="eyebrow">System administration</p><h1>Users and access</h1><p>Create staff accounts, assign roles, reset passwords, and control access.</p></div><button className="button primary" onClick={() => setEditor({ roles: ['VIEWER'], canAccessRestrictedClinicalData: false })}><Plus size={17} /> Create user</button></header>
    {error && <div className="form-alert error" role="alert">{error}</div>}
    <UsersPanel data={data} edit={setEditor} resetPassword={setPasswordUser} reload={load} setError={setError} />
    {editor && <UserDialog value={editor} roles={data.availableRoles} setValue={setEditor} save={save} />}
    {passwordUser && <PasswordDialog user={passwordUser} close={() => setPasswordUser(undefined)} setError={setError} />}
  </>
}

function UsersPanel({ data, edit, resetPassword, reload, setError }: { data: AdminUsers; edit: (value: UserEditor) => void; resetPassword: (value: AdminUser) => void; reload: () => Promise<void>; setError: (value: string) => void }) {
  const action = async (run: () => Promise<void>) => { try { setError(''); await run(); await reload() } catch (failure) { setError(message(failure)) } }
  return <article className="admin-card admin-users"><div className="section-heading"><div><h2>Staff accounts</h2><span>{data.users.length} accounts configured</span></div></div><div className="admin-list">{data.users.map((user) => <div className="admin-user-row" key={user.id}><div><strong>{user.fullName}</strong><span>{user.email} · {user.employeeNumber} · {user.designation}</span><div className="role-chips">{user.roles.map((role) => <small key={role}>{role}</small>)}{user.canAccessRestrictedClinicalData && <small>RESTRICTED DATA</small>}{user.isLocked && <small className="danger">LOCKED</small>}</div></div><div className="row-actions"><button className="secondary-button" onClick={() => edit(user)}><Pencil size={15} /> Profile</button><button className="secondary-button" onClick={() => resetPassword(user)}><KeyRound size={15} /> Password</button><button className="secondary-button" disabled={user.isCurrentUser} onClick={() => void action(() => api.setAdminLock(user.id, !user.isLocked))}>{user.isLocked ? <UnlockKeyhole size={15} /> : <LockKeyhole size={15} />}{user.isLocked ? 'Unlock' : 'Lock'}</button></div><div className="role-editor">{data.availableRoles.map((role) => <label key={role}><input type="checkbox" checked={user.roles.includes(role)} disabled={user.isCurrentUser && role === 'ADMIN'} onChange={(event) => { const next = event.target.checked ? [...user.roles, role] : user.roles.filter((value) => value !== role); if (next.length) void action(() => api.updateAdminRoles(user.id, next)) }} /> {role}</label>)}</div></div>)}</div></article>
}

function UserDialog({ value, roles, setValue, save }: { value: UserEditor; roles: string[]; setValue: (value?: UserEditor) => void; save: () => Promise<void> }) {
  const fields = useMemo(() => [['fullName', 'Full name'], ['employeeNumber', 'Employee number'], ['designation', 'Designation'], ['department', 'Department'], ['mobileNumber', 'Mobile number'], ['workPhoneNumber', 'Work phone']] as const, [])
  return <div className="dialog-backdrop"><section className="dialog-card wide" role="dialog" aria-modal="true"><div className="dialog-header"><div><p className="eyebrow">Staff access</p><h2>{value.id ? 'Edit staff profile' : 'Create staff account'}</h2></div><button className="icon-button" aria-label="Close" onClick={() => setValue(undefined)}>×</button></div><div className="form-grid two-column">{!value.id && <><label><span>Email *</span><input type="email" autoComplete="off" value={value.email ?? ''} onChange={(event) => setValue({ ...value, email: event.target.value })} /></label><label><span>Initial password *</span><input type="password" minLength={8} autoComplete="new-password" value={value.password ?? ''} onChange={(event) => setValue({ ...value, password: event.target.value })} /><small>At least 8 characters with upper/lowercase, number, and symbol.</small></label></>}{fields.map(([key, label]) => <label key={key}><span>{label}{['fullName', 'employeeNumber', 'designation', 'mobileNumber'].includes(key) ? ' *' : ''}</span><input value={value[key] ?? ''} onChange={(event) => setValue({ ...value, [key]: event.target.value })} /></label>)}<label className="checkbox-field"><input type="checkbox" checked={value.canAccessRestrictedClinicalData ?? false} onChange={(event) => setValue({ ...value, canAccessRestrictedClinicalData: event.target.checked })} /><span>Restricted clinical data</span></label>{!value.id && <fieldset className="role-select"><legend>Roles *</legend>{roles.map((role) => <label key={role}><input type="checkbox" checked={value.roles?.includes(role) ?? false} onChange={(event) => setValue({ ...value, roles: event.target.checked ? [...(value.roles ?? []), role] : value.roles?.filter((item) => item !== role) })} /> {role}</label>)}</fieldset>}</div><div className="dialog-actions"><button className="secondary-button" onClick={() => setValue(undefined)}>Cancel</button><button className="primary-button" onClick={() => void save()}>Save account</button></div></section></div>
}

function PasswordDialog({ user, close, setError }: { user: AdminUser; close: () => void; setError: (value: string) => void }) {
  const [password, setPassword] = useState('')
  const save = async () => { try { await api.resetAdminPassword(user.id, password); close() } catch (failure) { setError(message(failure)); close() } }
  return <div className="dialog-backdrop"><section className="dialog-card" role="dialog" aria-modal="true"><div className="dialog-header"><div><p className="eyebrow">Account security</p><h2>Reset password</h2><p>{user.email}</p></div><button className="icon-button" aria-label="Close" onClick={close}>×</button></div><div className="form-grid"><label className="form-field wide"><span>New password *</span><input type="password" minLength={8} autoComplete="new-password" value={password} onChange={(event) => setPassword(event.target.value)} /></label></div><div className="dialog-actions"><button className="secondary-button" onClick={close}>Cancel</button><button className="primary-button" disabled={password.length < 8} onClick={() => void save()}>Reset password</button></div></section></div>
}
