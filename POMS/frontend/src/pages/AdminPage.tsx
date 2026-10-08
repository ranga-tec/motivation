/* oxlint-disable react(set-state-in-effect) -- initial API synchronization */
import { useEffect, useMemo, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { LockKeyhole, MapPin, Pencil, Plus, Settings, Stethoscope, UnlockKeyhole, Users } from 'lucide-react'
import { ApiError, api } from '../api/client'
import type { AdminCatalog, AdminItem, AdminUser } from '../api/types'
import { useAuth } from '../auth/AuthProvider'

type Tab = 'locations' | 'lookups' | 'devices' | 'users'
type Editor = { resource: string; id?: number; title: string; code?: string; name: string; parentId?: number; isActive: boolean; address?: string; phone?: string; requiresPatientNumberFlag?: boolean; patientNumberFlagCode?: string }
const lookupLabels: Record<string, string> = { 'referral-sources': 'Referral sources', nationalities: 'Nationalities', 'main-problem-types': 'Main problem types', 'cause-reason-types': 'Cause / reason types' }
const message = (error: unknown) => error instanceof ApiError ? error.message : 'The change could not be saved.'

export function AdminPage() {
  const { roles } = useAuth()
  const authorized = roles.includes('ADMIN')
  const [tab, setTab] = useState<Tab>('locations')
  const [catalog, setCatalog] = useState<AdminCatalog>()
  const [usersData, setUsersData] = useState<{ availableRoles: string[]; users: AdminUser[] }>()
  const [editor, setEditor] = useState<Editor>()
  const [userEditor, setUserEditor] = useState<Partial<AdminUser>>()
  const [error, setError] = useState('')
  const load = async () => { try { setError(''); const [nextCatalog, nextUsers] = await Promise.all([api.adminCatalog(), api.adminUsers()]); setCatalog(nextCatalog); setUsersData(nextUsers) } catch (e) { setError(message(e)) } }
  useEffect(() => {
    if (!authorized) return
    Promise.all([api.adminCatalog(), api.adminUsers()])
      .then(([nextCatalog, nextUsers]) => { setCatalog(nextCatalog); setUsersData(nextUsers) })
      .catch(e => setError(message(e)))
  }, [authorized])
  const saveItem = async () => { if (!editor?.name.trim()) return setError('Name is required.'); try { setError(''); const body: Record<string, unknown> = { name: editor.name, code: editor.code ?? '', isActive: editor.isActive, parentId: editor.parentId }; if (editor.resource === 'centers') Object.assign(body, { districtId: editor.parentId, address: editor.address, phone: editor.phone, requiresPatientNumberFlag: editor.requiresPatientNumberFlag, patientNumberFlagCode: editor.patientNumberFlagCode }); if (editor.resource === 'devices') body.deviceTypeId = editor.parentId; await api.saveAdminItem(editor.resource, body, editor.id); setEditor(undefined); await load() } catch (e) { setError(message(e)) } }
  const saveUser = async () => { if (!userEditor) return; try { setError(''); if (userEditor.id) await api.updateAdminProfile(userEditor.id, userEditor as Record<string, unknown>); else await api.createAdminUser({ ...userEditor, roles: userEditor.roles ?? ['VIEWER'] }); setUserEditor(undefined); await load() } catch (e) { setError(message(e)) } }
  if (!authorized) return <Navigate to="/" replace />
  if (!catalog || !usersData) return <div className="loading-state">Loading administration workspace...</div>
  return <>
    <header className="page-header"><div><p className="eyebrow">System administration</p><h1>Administration</h1><p>Manage reference data, service locations, devices, and staff access.</p></div></header>
    {error && <div className="form-alert error" role="alert">{error}</div>}
    <div className="admin-tabs" role="tablist">
      <TabButton active={tab === 'locations'} icon={<MapPin />} label="Locations" onClick={() => setTab('locations')} />
      <TabButton active={tab === 'lookups'} icon={<Stethoscope />} label="Clinical options" onClick={() => setTab('lookups')} />
      <TabButton active={tab === 'devices'} icon={<Settings />} label="Devices" onClick={() => setTab('devices')} />
      <TabButton active={tab === 'users'} icon={<Users />} label="Staff access" onClick={() => setTab('users')} />
    </div>
    {tab === 'locations' && <Locations catalog={catalog} edit={setEditor} />}
    {tab === 'lookups' && <Lookups catalog={catalog} edit={setEditor} />}
    {tab === 'devices' && <Devices catalog={catalog} edit={setEditor} />}
    {tab === 'users' && <UsersPanel data={usersData} edit={setUserEditor} reload={load} setError={setError} />}
    {editor && <ItemDialog editor={editor} catalog={catalog} setEditor={setEditor} save={saveItem} />}
    {userEditor && <UserDialog value={userEditor} roles={usersData.availableRoles} setValue={setUserEditor} save={saveUser} />}
  </>
}

function TabButton({ active, icon, label, onClick }: { active: boolean; icon: React.ReactNode; label: string; onClick: () => void }) { return <button role="tab" aria-selected={active} className={active ? 'active' : ''} onClick={onClick}>{icon}{label}</button> }
function Section({ title, resource, items, edit, coded = false, parents }: { title: string; resource: string; items: AdminItem[]; edit: (value: Editor) => void; coded?: boolean; parents?: AdminItem[] }) {
  const open = (item?: AdminItem) => { const detailed = item as AdminItem & Partial<Editor>; edit({ resource, id: item?.id, title: `${item ? 'Edit' : 'Add'} ${title.toLowerCase()}`, code: item?.code, name: item?.name ?? '', parentId: item?.parentId ?? parents?.[0]?.id, isActive: item?.isActive ?? true, address: detailed.address, phone: detailed.phone, requiresPatientNumberFlag: detailed.requiresPatientNumberFlag, patientNumberFlagCode: detailed.patientNumberFlagCode }) }
  return <article className="admin-card"><div className="section-heading"><div><h2>{title}</h2><span>{items.length} configured</span></div><button className="secondary-button" onClick={() => open()}><Plus size={16} /> Add</button></div><div className="admin-list">{items.map((item) => <div className="admin-row" key={item.id}><div><strong>{item.name}</strong><span>{[coded && item.code, item.parentName, item.isActive ? 'Active' : 'Inactive'].filter(Boolean).join(' · ')}</span></div><button className="icon-button" aria-label={`Edit ${item.name}`} onClick={() => open(item)}><Pencil size={17} /></button></div>)}</div></article>
}
function Locations({ catalog, edit }: { catalog: AdminCatalog; edit: (value: Editor) => void }) { return <div className="admin-grid"><Section title="Provinces" resource="provinces" items={catalog.provinces} edit={edit} coded /><Section title="Districts" resource="districts" items={catalog.districts} edit={edit} coded parents={catalog.provinces} /><Section title="Cities" resource="cities" items={catalog.cities} edit={edit} parents={catalog.districts} /><Section title="Service centres" resource="centers" items={catalog.centers.map(x => ({ ...x, parentId: x.districtId, parentName: x.districtName }))} edit={edit} coded parents={catalog.districts} /></div> }
function Lookups({ catalog, edit }: { catalog: AdminCatalog; edit: (value: Editor) => void }) { return <div className="admin-grid">{Object.entries(lookupLabels).map(([key, title]) => <Section key={key} title={title} resource={`lookups/${key}`} items={catalog.lookups[key] ?? []} edit={edit} />)}</div> }
function Devices({ catalog, edit }: { catalog: AdminCatalog; edit: (value: Editor) => void }) { return <div className="admin-grid"><Section title="Device types" resource="device-types" items={catalog.deviceTypes} edit={edit} coded /><Section title="Devices" resource="devices" items={catalog.devices.map(x => ({ ...x, parentId: x.deviceTypeId, parentName: x.deviceTypeName }))} edit={edit} coded parents={catalog.deviceTypes} /></div> }

function UsersPanel({ data, edit, reload, setError }: { data: { availableRoles: string[]; users: AdminUser[] }; edit: (value: Partial<AdminUser>) => void; reload: () => Promise<void>; setError: (value: string) => void }) {
  const action = async (run: () => Promise<void>) => { try { setError(''); await run(); await reload() } catch (e) { setError(message(e)) } }
  return <article className="admin-card admin-users"><div className="section-heading"><div><h2>Staff access</h2><span>{data.users.length} authorised identities · credentials are managed by your identity provider</span></div><button className="primary-button" onClick={() => edit({ roles: ['VIEWER'], canAccessRestrictedClinicalData: false })}><Plus size={16} /> Authorise staff member</button></div><div className="admin-list">{data.users.map(user => <div className="admin-user-row" key={user.id}><div><strong>{user.fullName}</strong><span>{user.email} · {user.employeeNumber} · {user.designation}</span><div className="role-chips">{user.roles.map(role => <small key={role}>{role}</small>)}{user.canAccessRestrictedClinicalData && <small>RESTRICTED DATA</small>}{user.isLocked && <small className="danger">LOCKED</small>}</div></div><div className="row-actions"><button className="secondary-button" onClick={() => edit(user)}><Pencil size={15} /> Profile</button><button className="secondary-button" disabled={user.isCurrentUser} onClick={() => void action(() => api.setAdminLock(user.id, !user.isLocked))}>{user.isLocked ? <UnlockKeyhole size={15} /> : <LockKeyhole size={15} />}{user.isLocked ? 'Unlock' : 'Lock'}</button></div><div className="role-editor">{data.availableRoles.map(role => <label key={role}><input type="checkbox" checked={user.roles.includes(role)} disabled={user.isCurrentUser && role === 'ADMIN'} onChange={(event) => { const next = event.target.checked ? [...user.roles, role] : user.roles.filter(x => x !== role); if (next.length) void action(() => api.updateAdminRoles(user.id, next)) }} /> {role}</label>)}</div></div>)}</div></article>
}

function ItemDialog({ editor, catalog, setEditor, save }: { editor: Editor; catalog: AdminCatalog; setEditor: (value?: Editor) => void; save: () => Promise<void> }) {
  const parents = editor.resource === 'districts' ? catalog.provinces : editor.resource === 'cities' || editor.resource === 'centers' ? catalog.districts : editor.resource === 'devices' ? catalog.deviceTypes : []
  return <div className="dialog-backdrop" role="presentation"><section className="dialog-card" role="dialog" aria-modal="true" aria-labelledby="item-title"><div className="dialog-header"><div><p className="eyebrow">Reference data</p><h2 id="item-title">{editor.title}</h2></div><button className="icon-button" aria-label="Close" onClick={() => setEditor(undefined)}>×</button></div><div className="form-grid">{!editor.resource.startsWith('lookups/') && editor.resource !== 'cities' && <label><span>Code *</span><input value={editor.code ?? ''} onChange={e => setEditor({ ...editor, code: e.target.value })} /></label>}<label><span>Name *</span><input autoFocus value={editor.name} onChange={e => setEditor({ ...editor, name: e.target.value })} /></label>{parents.length > 0 && <label><span>{editor.resource === 'devices' ? 'Device type' : editor.resource === 'districts' ? 'Province' : 'District'} *</span><select value={editor.parentId} onChange={e => setEditor({ ...editor, parentId: Number(e.target.value) })}>{parents.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>}{editor.resource === 'centers' && <><label><span>Address</span><input value={editor.address ?? ''} onChange={e => setEditor({ ...editor, address: e.target.value })} /></label><label><span>Phone</span><input value={editor.phone ?? ''} onChange={e => setEditor({ ...editor, phone: e.target.value })} /></label><label className="checkbox-field"><input type="checkbox" checked={editor.requiresPatientNumberFlag ?? false} onChange={e => setEditor({ ...editor, requiresPatientNumberFlag: e.target.checked })} /><span>Patient number flag required</span></label>{editor.requiresPatientNumberFlag && <label><span>Flag code *</span><input value={editor.patientNumberFlagCode ?? ''} onChange={e => setEditor({ ...editor, patientNumberFlagCode: e.target.value })} /></label>}</>}<label className="checkbox-field"><input type="checkbox" checked={editor.isActive} onChange={e => setEditor({ ...editor, isActive: e.target.checked })} /><span>Active</span></label></div><div className="dialog-actions"><button className="secondary-button" onClick={() => setEditor(undefined)}>Cancel</button><button className="primary-button" onClick={() => void save()}>Save</button></div></section></div>
}

function UserDialog({ value, roles, setValue, save }: { value: Partial<AdminUser>; roles: string[]; setValue: (value?: Partial<AdminUser>) => void; save: () => Promise<void> }) {
  const fields = useMemo(() => [['fullName', 'Full name'], ['employeeNumber', 'Employee number'], ['designation', 'Designation'], ['department', 'Department'], ['mobileNumber', 'Mobile number'], ['workPhoneNumber', 'Work phone']] as const, [])
  return <div className="dialog-backdrop"><section className="dialog-card wide" role="dialog" aria-modal="true"><div className="dialog-header"><div><p className="eyebrow">Staff access</p><h2>{value.id ? 'Edit staff profile' : 'Authorise staff member'}</h2>{!value.id && <p>The email must exactly match the account in your identity provider.</p>}</div><button className="icon-button" aria-label="Close" onClick={() => setValue(undefined)}>×</button></div><div className="form-grid two-column">{!value.id && <label><span>Identity-provider email *</span><input type="email" value={value.email ?? ''} onChange={e => setValue({ ...value, email: e.target.value })} /></label>}{fields.map(([key, label]) => <label key={key}><span>{label}{['fullName', 'employeeNumber', 'designation', 'mobileNumber'].includes(key) ? ' *' : ''}</span><input value={value[key] ?? ''} onChange={e => setValue({ ...value, [key]: e.target.value })} /></label>)}<label className="checkbox-field"><input type="checkbox" checked={value.canAccessRestrictedClinicalData ?? false} onChange={e => setValue({ ...value, canAccessRestrictedClinicalData: e.target.checked })} /><span>Restricted clinical data</span></label>{!value.id && <fieldset className="role-select"><legend>Roles *</legend>{roles.map(role => <label key={role}><input type="checkbox" checked={value.roles?.includes(role) ?? false} onChange={e => setValue({ ...value, roles: e.target.checked ? [...(value.roles ?? []), role] : value.roles?.filter(x => x !== role) })} /> {role}</label>)}</fieldset>}</div><div className="dialog-actions"><button className="secondary-button" onClick={() => setValue(undefined)}>Cancel</button><button className="primary-button" onClick={() => void save()}>Save access</button></div></section></div>
}
