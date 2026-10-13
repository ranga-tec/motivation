import { useMemo, useState } from 'react'
import { Navigate, NavLink, useParams } from 'react-router-dom'
import { ArchiveRestore, BookOpen, EyeOff, Pencil, Plus, Search, Trash2 } from 'lucide-react'
import { api } from '../api/client'
import type { AdminCatalog, AdminItem } from '../api/types'
import { useAuth } from '../auth/AuthProvider'
import { Badge, Card, ConfirmModal, EmptyState, ErrorPanel, Field, FormModal, PageHeader, Segmented, Skeleton } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { errorMessage } from '../lib/format'
import { useToast } from '../lib/toast'

type Editor = { id?: number; code?: string; name: string; parentId?: number; isActive: boolean; address?: string; phone?: string; requiresPatientNumberFlag?: boolean; patientNumberFlagCode?: string }
type Section = { key: string; label: string; singular: string; resource: string; group: string; coded?: boolean; parent?: 'provinces' | 'districts' | 'deviceTypes'; parentLabel?: string; mutableActive?: boolean; help: string }
const sections: Section[] = [
  { key: 'centres', label: 'Locations / centres', singular: 'location', resource: 'centers', group: 'Organisation', coded: true, parent: 'districts', parentLabel: 'District', mutableActive: true, help: 'Service centres. A patient-number flag (e.g. Colombo “C”) changes the PNO format.' },
  { key: 'provinces', label: 'Provinces', singular: 'province', resource: 'provinces', group: 'Geography', coded: true, help: 'Provinces used in addresses and reports.' },
  { key: 'districts', label: 'Districts', singular: 'district', resource: 'districts', group: 'Geography', coded: true, parent: 'provinces', parentLabel: 'Province', help: 'Districts, filtered by province during registration.' },
  { key: 'cities', label: 'Cities', singular: 'city', resource: 'cities', group: 'Geography', parent: 'districts', parentLabel: 'District', mutableActive: true, help: 'Cities, filtered by district during registration.' },
  { key: 'referral-sources', label: 'Referral sources', singular: 'referral source', resource: 'lookups/referral-sources', group: 'Registration', mutableActive: true, help: 'How patients got to know Exceed. A value starting with “Other” asks for details.' },
  { key: 'nationalities', label: 'Nationalities', singular: 'nationality', resource: 'lookups/nationalities', group: 'Registration', mutableActive: true, help: 'Offered for foreign patients.' },
  { key: 'main-problems', label: 'Main problem types', singular: 'main problem type', resource: 'lookups/main-problem-types', group: 'Clinical', mutableActive: true, help: 'Main problem options on assessments.' },
  { key: 'cause-reasons', label: 'Cause / pathology types', singular: 'cause / pathology type', resource: 'lookups/cause-reason-types', group: 'Clinical', mutableActive: true, help: 'Cause, reason or pathology options on assessments. “Other” asks for details.' },
  { key: 'device-types', label: 'Device types', singular: 'device type', resource: 'device-types', group: 'Clinical', coded: true, help: 'Groups for the device catalogue.' },
  { key: 'devices', label: 'Devices', singular: 'device', resource: 'devices', group: 'Clinical', coded: true, parent: 'deviceTypes', parentLabel: 'Device type', mutableActive: true, help: 'Devices recorded on deliveries.' },
]
const year = new Date().getFullYear()
const groupsOf = [...new Set(sections.map((section) => section.group))]

function itemsOf(catalog: AdminCatalog, section: Section): (AdminItem & Partial<Editor>)[] {
  if (section.key === 'centres') return catalog.centers.map((item) => ({ ...item, parentId: item.districtId, parentName: item.districtName }))
  if (section.key === 'devices') return catalog.devices.map((item) => ({ ...item, parentId: item.deviceTypeId, parentName: item.deviceTypeName }))
  if (section.resource.startsWith('lookups/')) return catalog.lookups[section.resource.split('/')[1]] ?? []
  if (section.key === 'device-types') return catalog.deviceTypes
  return catalog[section.key as 'provinces' | 'districts' | 'cities'] ?? []
}

export function ReferencePage() {
  const { roles } = useAuth()
  const notify = useToast()
  const { section: key = 'centres' } = useParams()
  const section = sections.find((item) => item.key === key)
  const authorized = roles.includes('ADMIN')
  const catalog = useApi(() => api.adminCatalog(), 'admin-catalog', authorized)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState<'all' | 'active' | 'inactive'>('all')
  const [editor, setEditor] = useState<Editor>()
  const [deleting, setDeleting] = useState<AdminItem>()
  const items = useMemo(() => catalog.data && section ? itemsOf(catalog.data, section) : [], [catalog.data, section])
  const filtered = useMemo(() => items.filter((item) => `${item.name} ${item.code ?? ''} ${item.parentName ?? ''}`.toLowerCase().includes(query.trim().toLowerCase()) && (status === 'all' || item.isActive === (status === 'active'))), [items, query, status])
  if (!authorized) return <Navigate to="/" replace />
  if (!section) return <Navigate to="/admin/reference/centres" replace />
  const parents = section.parent && catalog.data ? catalog.data[section.parent] as AdminItem[] : []

  const body = (value: Editor) => {
    const request: Record<string, unknown> = { name: value.name.trim(), code: value.code?.trim() || value.name.trim().replace(/[^A-Za-z0-9]+/g, '-').slice(0, 20).toUpperCase(), isActive: value.isActive, parentId: value.parentId }
    if (section.resource === 'centers') Object.assign(request, { districtId: value.parentId, address: value.address, phone: value.phone, requiresPatientNumberFlag: value.requiresPatientNumberFlag ?? false, patientNumberFlagCode: value.requiresPatientNumberFlag ? value.patientNumberFlagCode : undefined })
    if (section.resource === 'devices') request.deviceTypeId = value.parentId
    if (section.resource === 'districts') request.provinceId = value.parentId
    if (section.resource === 'cities') request.districtId = value.parentId
    return request
  }
  const setActive = async (item: AdminItem & Partial<Editor>, active: boolean) => {
    try { await api.saveAdminItem(section.resource, body({ ...item, isActive: active }), item.id); notify(`${item.name} ${active ? 'reactivated' : 'deactivated'}.`); catalog.refresh() }
    catch (error) { notify(errorMessage(error, 'The value could not be updated.'), 'error') }
  }
  const remove = async (item: AdminItem) => {
    try { await api.deleteAdminItem(section.resource, item.id); notify(`${item.name} deleted.`); catalog.refresh() }
    catch (error) { notify(errorMessage(error, 'Values already used by records cannot be deleted. Deactivate it instead.'), 'error') }
    finally { setDeleting(undefined) }
  }

  return <>
    <PageHeader eyebrow="Administration · Reference data" title={section.label} description={section.help} actions={<button className="btn btn-primary" onClick={() => setEditor({ name: '', isActive: true, parentId: parents[0]?.id })}><Plus /> Add {section.singular}</button>} />
    <div className="split-layout">
      <nav className="card side-nav" aria-label="Reference data">
        <div className="side-nav-list">{groupsOf.map((group) => <div key={group} style={{ display: 'contents' }}><h3>{group}</h3>{sections.filter((item) => item.group === group).map((item) => <NavLink key={item.key} to={`/admin/reference/${item.key}`}>{item.label}{catalog.data && <span>{itemsOf(catalog.data, item).length}</span>}</NavLink>)}</div>)}</div>
      </nav>
      <Card bodyless>
        <div className="card-toolbar">
          <label className="search-input"><Search /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder={`Search ${section.label.toLowerCase()}`} aria-label="Search" /></label>
          {section.mutableActive && <Segmented label="Status" value={status} onChange={setStatus} options={[{ value: 'all', label: 'All' }, { value: 'active', label: 'Active' }, { value: 'inactive', label: 'Inactive' }]} />}
        </div>
        {catalog.error ? <ErrorPanel message={catalog.error} retry={catalog.refresh} /> : catalog.loading ? <Skeleton rows={8} /> : !filtered.length
          ? <EmptyState icon={<BookOpen />} title="No matching values" description={items.length ? 'Try another search or status.' : `Add the first ${section.singular}.`} />
          : <div className="table-wrap"><table className="table responsive">
            <thead><tr><th>Name</th>{section.coded && <th>Code</th>}{section.parent && <th>{section.parentLabel}</th>}{section.key === 'centres' && <th>PNO flag</th>}<th>Status</th><th className="actions"><span className="sr-only">Actions</span></th></tr></thead>
            <tbody>{filtered.map((item) => <tr key={item.id}>
              <td className="primary-cell"><strong>{item.name}</strong>{section.key === 'centres' && item.address && <span className="cell-sub">{item.address}</span>}</td>
              {section.coded && <td data-label="Code"><span className="mono">{item.code}</span></td>}
              {section.parent && <td data-label={section.parentLabel}>{item.parentName}</td>}
              {section.key === 'centres' && <td data-label="PNO flag">{item.requiresPatientNumberFlag ? <span className="pno">{item.patientNumberFlagCode}/</span> : <span className="muted">General</span>}</td>}
              <td data-label="Status"><Badge value={item.isActive ? 'Active' : 'Inactive'} tone={item.isActive ? 'tone-green' : 'tone-gray'} /></td>
              <td className="actions"><div>
                <button className="icon-btn sm" title="Edit" aria-label={`Edit ${item.name}`} onClick={() => setEditor({ ...item, parentId: item.parentId })}><Pencil /></button>
                {section.mutableActive && <button className="icon-btn sm" title={item.isActive ? 'Deactivate' : 'Reactivate'} aria-label={`${item.isActive ? 'Deactivate' : 'Reactivate'} ${item.name}`} onClick={() => void setActive(item, !item.isActive)}>{item.isActive ? <EyeOff /> : <ArchiveRestore />}</button>}
                <button className="icon-btn sm danger" title="Delete" aria-label={`Delete ${item.name}`} onClick={() => setDeleting(item)}><Trash2 /></button>
              </div></td>
            </tr>)}</tbody>
          </table></div>}
        {catalog.data && <div className="card-footer"><span>{filtered.length} of {items.length}</span></div>}
      </Card>
    </div>
    {editor && <ItemDialog section={section} parents={parents} value={editor} close={() => setEditor(undefined)} save={async (value) => { await api.saveAdminItem(section.resource, body(value), value.id); notify(`${value.name} saved.`); setEditor(undefined); catalog.refresh() }} />}
    {deleting && <ConfirmModal title={`Delete ${section.singular}?`} message={<>“{deleting.name}” will be removed. Values already used by patients or records cannot be deleted — deactivate them instead.</>} confirmLabel="Delete" onClose={() => setDeleting(undefined)} onConfirm={() => remove(deleting)} />}
  </>
}

function ItemDialog({ section, parents, value, close, save }: { section: Section; parents: AdminItem[]; value: Editor; close: () => void; save: (value: Editor) => Promise<void> }) {
  const [form, setForm] = useState(value)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const set = (patch: Partial<Editor>) => setForm((current) => ({ ...current, ...patch }))
  const submit = async () => { setSubmitting(true); setError(''); try { await save(form) } catch (failure) { setError(errorMessage(failure, 'The value could not be saved.')) } finally { setSubmitting(false) } }
  return <FormModal title={`${value.id ? 'Edit' : 'Add'} ${section.singular}`} onClose={close} onSubmit={submit} submitLabel="Save" submitting={submitting} error={error}>
    <div className="form-grid">
      <Field label="Name" required className={section.coded ? '' : 'span-all'}><input required maxLength={150} value={form.name} onChange={(event) => set({ name: event.target.value })} /></Field>
      {section.coded && <Field label="Code" required><input required maxLength={20} className="mono" value={form.code ?? ''} onChange={(event) => set({ code: event.target.value.toUpperCase() })} /></Field>}
      {section.parent && <Field label={section.parentLabel ?? 'Parent'} required className="span-all"><select required value={form.parentId ?? ''} onChange={(event) => set({ parentId: Number(event.target.value) })}><option value="">Select</option>{parents.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>}
      {section.resource === 'centers' && <>
        <Field label="Address" className="span-all"><input maxLength={300} value={form.address ?? ''} onChange={(event) => set({ address: event.target.value })} /></Field>
        <Field label="Phone"><input type="tel" maxLength={30} value={form.phone ?? ''} onChange={(event) => set({ phone: event.target.value })} /></Field>
        <div />
        <label className="check span-all"><input type="checkbox" checked={form.requiresPatientNumberFlag ?? false} onChange={(event) => set({ requiresPatientNumberFlag: event.target.checked })} /><span><strong>Use a patient-number flag</strong><small>Patients registered here receive numbers like <span className="mono">{form.patientNumberFlagCode || 'C'}/{year}/0001</span>.</small></span></label>
        {form.requiresPatientNumberFlag && <Field label="Flag code" required><input required maxLength={5} className="mono" value={form.patientNumberFlagCode ?? ''} onChange={(event) => set({ patientNumberFlagCode: event.target.value.toUpperCase() })} /></Field>}
      </>}
      <label className="check span-all"><input type="checkbox" checked={form.isActive} onChange={(event) => set({ isActive: event.target.checked })} /><span><strong>Active</strong><small>Inactive values stay on existing records but are not offered for new entries.</small></span></label>
    </div>
  </FormModal>
}
