/* oxlint-disable react/set-state-in-effect -- route-driven API synchronization */
import { useEffect, useMemo, useState } from 'react'
import { Navigate, NavLink, useParams } from 'react-router-dom'
import { Archive, Pencil, Plus, Search, Trash2 } from 'lucide-react'
import { ApiError, api } from '../api/client'
import type { AdminCatalog, AdminItem } from '../api/types'
import { useAuth } from '../auth/AuthProvider'

type Editor = { resource: string; id?: number; title: string; code?: string; name: string; parentId?: number; isActive: boolean; address?: string; phone?: string; requiresPatientNumberFlag?: boolean; patientNumberFlagCode?: string }
type SectionConfig = { key: string; label: string; resource: string; coded?: boolean; parent?: 'provinces' | 'districts' | 'deviceTypes'; mutableActive?: boolean }
const sections: SectionConfig[] = [
  { key: 'provinces', label: 'Provinces', resource: 'provinces', coded: true },
  { key: 'districts', label: 'Districts', resource: 'districts', coded: true, parent: 'provinces' },
  { key: 'cities', label: 'Cities', resource: 'cities', parent: 'districts', mutableActive: true },
  { key: 'centres', label: 'Service centres', resource: 'centers', coded: true, parent: 'districts', mutableActive: true },
  { key: 'referral-sources', label: 'Referral sources', resource: 'lookups/referral-sources', mutableActive: true },
  { key: 'nationalities', label: 'Nationalities', resource: 'lookups/nationalities', mutableActive: true },
  { key: 'main-problems', label: 'Main problem types', resource: 'lookups/main-problem-types', mutableActive: true },
  { key: 'cause-reasons', label: 'Cause / reason types', resource: 'lookups/cause-reason-types', mutableActive: true },
  { key: 'device-types', label: 'Device types', resource: 'device-types', coded: true },
  { key: 'devices', label: 'Devices', resource: 'devices', coded: true, parent: 'deviceTypes', mutableActive: true },
]
const message = (error: unknown) => error instanceof ApiError ? error.message : 'The reference item could not be saved.'

export function ReferencePage() {
  const { roles } = useAuth()
  const { section = 'cities' } = useParams()
  const config = sections.find((item) => item.key === section)
  const [catalog, setCatalog] = useState<AdminCatalog>()
  const [editor, setEditor] = useState<Editor>()
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState<'all' | 'active' | 'inactive'>('all')
  const [error, setError] = useState('')
  const authorized = roles.includes('ADMIN')
  const load = async () => { try { setError(''); setCatalog(await api.adminCatalog()) } catch (failure) { setError(message(failure)) } }
  useEffect(() => { if (authorized) void load() }, [authorized])
  const items = useMemo(() => catalog && config ? getItems(catalog, config) : [], [catalog, config])
  const filtered = useMemo(() => items.filter((item) => {
    const text = `${item.name} ${item.code ?? ''} ${item.parentName ?? ''}`.toLowerCase()
    return text.includes(query.trim().toLowerCase()) && (status === 'all' || item.isActive === (status === 'active'))
  }), [items, query, status])
  if (!authorized) return <Navigate to="/" replace />
  if (!config) return <Navigate to="/references/cities" replace />
  if (!catalog) return <div className="loading-state">Loading reference data...</div>
  const parents = config.parent ? catalog[config.parent] as AdminItem[] : []
  const open = (item?: AdminItem) => { const details = item as AdminItem & Partial<Editor>; setEditor({ resource: config.resource, id: item?.id, title: `${item ? 'Edit' : 'Add'} ${config.label.toLowerCase()}`, code: item?.code, name: item?.name ?? '', parentId: item?.parentId ?? parents[0]?.id, isActive: item?.isActive ?? true, address: details.address, phone: details.phone, requiresPatientNumberFlag: details.requiresPatientNumberFlag, patientNumberFlagCode: details.patientNumberFlagCode }) }
  const save = async () => {
    if (!editor?.name.trim()) { setError('Name is required.'); return }
    try {
      const body: Record<string, unknown> = { name: editor.name, code: editor.code ?? '', isActive: editor.isActive, parentId: editor.parentId }
      if (editor.resource === 'centers') Object.assign(body, { districtId: editor.parentId, address: editor.address, phone: editor.phone, requiresPatientNumberFlag: editor.requiresPatientNumberFlag, patientNumberFlagCode: editor.patientNumberFlagCode })
      if (editor.resource === 'devices') body.deviceTypeId = editor.parentId
      await api.saveAdminItem(editor.resource, body, editor.id); setEditor(undefined); await load()
    } catch (failure) { setError(message(failure)) }
  }
  const setActive = async (item: AdminItem, active: boolean) => {
    const details = item as AdminItem & Partial<Editor>
    try {
      const body: Record<string, unknown> = { name: item.name, code: item.code ?? '', isActive: active, parentId: item.parentId }
      if (config.resource === 'centers') Object.assign(body, { districtId: item.parentId, address: details.address, phone: details.phone, requiresPatientNumberFlag: details.requiresPatientNumberFlag, patientNumberFlagCode: details.patientNumberFlagCode })
      if (config.resource === 'devices') body.deviceTypeId = item.parentId
      await api.saveAdminItem(config.resource, body, item.id); await load()
    } catch (failure) { setError(message(failure)) }
  }
  const remove = async (item: AdminItem) => {
    if (!window.confirm(`Delete “${item.name}”? Values already used by records cannot be deleted.`)) return
    try { setError(''); await api.deleteAdminItem(config.resource, item.id); await load() }
    catch (failure) { setError(message(failure)) }
  }
  return <>
    <header className="page-header"><div><p className="eyebrow">Reference data</p><h1>{config.label}</h1><p>Search, add, edit, and safely deactivate values used throughout POMS.</p></div><button className="button primary" onClick={() => open()}><Plus size={17} /> Add {config.label.toLowerCase()}</button></header>
    <nav className="reference-nav" aria-label="Reference sections">{sections.map((item) => <NavLink key={item.key} to={`/references/${item.key}`}>{item.label}</NavLink>)}</nav>
    {error && <div className="form-alert error" role="alert">{error}</div>}
    <section className="panel reference-panel"><div className="reference-toolbar"><label className="search-field"><Search size={18} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder={`Search ${config.label.toLowerCase()}`} /></label>{config.mutableActive && <select value={status} onChange={(event) => setStatus(event.target.value as typeof status)}><option value="all">All statuses</option><option value="active">Active</option><option value="inactive">Inactive</option></select>}<span>{filtered.length} of {items.length}</span></div><div className="reference-table-wrap"><table><thead><tr><th>Name</th>{config.coded && <th>Code</th>}{config.parent && <th>Parent</th>}<th>Status</th><th>Actions</th></tr></thead><tbody>{filtered.map((item) => <tr key={item.id}><td><strong>{item.name}</strong></td>{config.coded && <td>{item.code}</td>}{config.parent && <td>{item.parentName}</td>}<td><span className={`status-pill ${item.isActive ? 'active' : 'inactive'}`}>{item.isActive ? 'Active' : 'Inactive'}</span></td><td><div className="row-actions"><button aria-label={`Edit ${item.name}`} onClick={() => open(item)}><Pencil /></button>{config.mutableActive && <button className={item.isActive ? 'danger' : ''} aria-label={`${item.isActive ? 'Deactivate' : 'Activate'} ${item.name}`} onClick={() => void setActive(item, !item.isActive)}><Archive /></button>}<button className="danger" aria-label={`Delete ${item.name}`} onClick={() => void remove(item)}><Trash2 /></button></div></td></tr>)}</tbody></table>{filtered.length === 0 && <div className="empty-reference">No matching reference values.</div>}</div></section>
    {editor && <ItemDialog editor={editor} parents={parents} setEditor={setEditor} save={save} />}
  </>
}

function getItems(catalog: AdminCatalog, config: SectionConfig): AdminItem[] {
  if (config.key === 'centres') return catalog.centers.map((item) => ({ ...item, parentId: item.districtId, parentName: item.districtName }))
  if (config.key === 'devices') return catalog.devices.map((item) => ({ ...item, parentId: item.deviceTypeId, parentName: item.deviceTypeName }))
  if (config.resource.startsWith('lookups/')) return catalog.lookups[config.resource.split('/')[1]] ?? []
  if (config.key === 'device-types') return catalog.deviceTypes
  if (config.key === 'provinces') return catalog.provinces
  if (config.key === 'districts') return catalog.districts
  if (config.key === 'cities') return catalog.cities
  return []
}

function ItemDialog({ editor, parents, setEditor, save }: { editor: Editor; parents: AdminItem[]; setEditor: (value?: Editor) => void; save: () => Promise<void> }) {
  return <div className="dialog-backdrop"><section className="dialog-card" role="dialog" aria-modal="true"><div className="dialog-header"><div><p className="eyebrow">Reference data</p><h2>{editor.title}</h2></div><button className="icon-button" aria-label="Close" onClick={() => setEditor(undefined)}>×</button></div><div className="form-grid">{!editor.resource.startsWith('lookups/') && editor.resource !== 'cities' && <label><span>Code *</span><input value={editor.code ?? ''} onChange={(event) => setEditor({ ...editor, code: event.target.value })} /></label>}<label><span>Name *</span><input autoFocus value={editor.name} onChange={(event) => setEditor({ ...editor, name: event.target.value })} /></label>{parents.length > 0 && <label><span>Parent *</span><select value={editor.parentId} onChange={(event) => setEditor({ ...editor, parentId: Number(event.target.value) })}>{parents.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>}{editor.resource === 'centers' && <><label><span>Address</span><input value={editor.address ?? ''} onChange={(event) => setEditor({ ...editor, address: event.target.value })} /></label><label><span>Phone</span><input value={editor.phone ?? ''} onChange={(event) => setEditor({ ...editor, phone: event.target.value })} /></label><label className="checkbox-field"><input type="checkbox" checked={editor.requiresPatientNumberFlag ?? false} onChange={(event) => setEditor({ ...editor, requiresPatientNumberFlag: event.target.checked })} /><span>Patient number flag required</span></label>{editor.requiresPatientNumberFlag && <label><span>Flag code *</span><input value={editor.patientNumberFlagCode ?? ''} onChange={(event) => setEditor({ ...editor, patientNumberFlagCode: event.target.value })} /></label>}</>}<label className="checkbox-field"><input type="checkbox" checked={editor.isActive} onChange={(event) => setEditor({ ...editor, isActive: event.target.checked })} /><span>Active</span></label></div><div className="dialog-actions"><button className="secondary-button" onClick={() => setEditor(undefined)}>Cancel</button><button className="primary-button" onClick={() => void save()}>Save</button></div></section></div>
}
