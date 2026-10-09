import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { X } from 'lucide-react'
import { api, ApiError } from '../api/client'
import type { CreatePatientRequest, PatientDetail } from '../api/types'
import { LoadingRows } from './Ui'
import { useApi } from '../hooks/useApi'

export function PatientEditDialog({ patientId, close, saved }: { patientId: string; close: () => void; saved: (patient: PatientDetail) => void }) {
  const details = useApi(() => api.patientForEdit(patientId), `patient-edit-${patientId}`)
  const options = useApi(() => api.registrationOptions(), 'patient-edit-options')
  const [form, setForm] = useState<CreatePatientRequest>()
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  // oxlint-disable-next-line react/set-state-in-effect -- API data initializes the editable draft.
  useEffect(() => { if (details.data) setForm({ ...details.data, confirmPossibleDuplicate: false, contacts: details.data.contacts.length ? details.data.contacts : [{ telephoneNumber: '' }] }) }, [details.data])
  const districts = useMemo(() => options.data?.districts.filter(x => x.parentId === form?.provinceId) ?? [], [options.data, form?.provinceId])
  const cities = useMemo(() => options.data?.cities.filter(x => x.parentId === form?.districtId) ?? [], [options.data, form?.districtId])
  const update = <K extends keyof CreatePatientRequest>(key: K, value: CreatePatientRequest[K]) => setForm(current => current ? { ...current, [key]: value } : current)
  const submit = async (event: FormEvent) => { event.preventDefault(); if (!form) return; setSubmitting(true); setError(''); try { saved(await api.updatePatient(patientId, form)) } catch (failure) { setError(failure instanceof ApiError ? failure.message : 'The patient record could not be updated.') } finally { setSubmitting(false) } }
  return <div className="dialog-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) close() }}><section className="dialog patient-edit-dialog" role="dialog" aria-modal="true" aria-labelledby="patient-edit-title"><header><div><h2 id="patient-edit-title">Edit patient</h2><p>Update registration and contact information without changing the patient number.</p></div><button className="icon-button" onClick={close} aria-label="Close"><X /></button></header><div className="dialog-body">
    {details.error || options.error ? <div className="inline-error">{details.error || options.error}</div> : !form || options.loading ? <LoadingRows /> : <form onSubmit={event => void submit(event)}>
      {error && <div className="inline-error" role="alert">{error}</div>}
      <div className="dialog-grid patient-edit-grid">
        <Field label="Full name *"><input required maxLength={200} value={form.fullName} onChange={e => update('fullName', e.target.value)} /></Field>
        <Field label="Name with initials *"><input required maxLength={100} value={form.nameWithInitials} onChange={e => update('nameWithInitials', e.target.value)} /></Field>
        <Field label="Date of birth *"><input required type="date" value={form.dateOfBirth} onChange={e => update('dateOfBirth', e.target.value)} /></Field>
        <Field label="Gender *"><select value={form.sex} onChange={e => update('sex', e.target.value)}><option>Male</option><option>Female</option><option>Other</option></select></Field>
        <Field label="Category *"><select value={form.category} onChange={e => update('category', e.target.value)}><option>Local</option><option>Foreign</option></select></Field>
        {form.category === 'Foreign' && <Field label="Nationality *"><input required value={form.nationality ?? ''} onChange={e => update('nationality', e.target.value)} /></Field>}
        <Field label="Identification type *"><select value={form.identificationType} onChange={e => update('identificationType', e.target.value)}><option value="NIC">NIC</option><option value="DrivingLicense">Driving licence</option><option value="Passport">Passport</option><option value="NotApplicable">N/A</option></select></Field>
        {form.identificationType !== 'NotApplicable' && <Field label="Identification number *"><input required value={form.identificationNumber ?? ''} onChange={e => update('identificationNumber', e.target.value)} /></Field>}
        <Field label="Employment"><input value={form.employment ?? ''} onChange={e => update('employment', e.target.value)} /></Field>
        <Field label="Address line 1 *" wide><input required value={form.address1} onChange={e => update('address1', e.target.value)} /></Field>
        <Field label="Address line 2" wide><input value={form.address2 ?? ''} onChange={e => update('address2', e.target.value)} /></Field>
        <Field label="Province *"><select required value={form.provinceId} onChange={e => { update('provinceId', Number(e.target.value)); update('districtId', 0); update('cityId', undefined) }}>{options.data?.provinces.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></Field>
        <Field label="District *"><select required value={form.districtId} onChange={e => { update('districtId', Number(e.target.value)); update('cityId', undefined) }}>{districts.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></Field>
        <Field label="City"><select value={form.cityId ?? ''} onChange={e => update('cityId', e.target.value ? Number(e.target.value) : undefined)}><option value="">Not listed</option>{cities.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></Field>
        {!form.cityId && <Field label="City (if not listed) *"><input required value={form.cityOther ?? ''} onChange={e => update('cityOther', e.target.value)} /></Field>}
        <Field label="Telephone number *"><input required type="tel" value={form.contacts[0]?.telephoneNumber ?? ''} onChange={e => update('contacts', [{ ...form.contacts[0], telephoneNumber: e.target.value }])} /></Field>
        <Field label="Email"><input value={form.email ?? ''} onChange={e => update('email', e.target.value)} /></Field>
        <Field label="Guardian / carer name *"><input required value={form.guardianName} onChange={e => update('guardianName', e.target.value)} /></Field>
        <Field label="Relationship *"><input required value={form.guardianRelationship} onChange={e => update('guardianRelationship', e.target.value)} /></Field>
        <Field label="Guardian mobile"><input type="tel" value={form.guardianMobile ?? ''} onChange={e => update('guardianMobile', e.target.value)} /></Field>
        <Field label="Guardian address"><input value={form.guardianAddress ?? ''} onChange={e => update('guardianAddress', e.target.value)} /></Field>
        <Field label="Treatment centre *"><select required value={form.centerId} onChange={e => update('centerId', Number(e.target.value))}>{options.data?.centers.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></Field>
        <Field label="Registration date *"><input required type="date" value={form.registrationDate} onChange={e => update('registrationDate', e.target.value)} /></Field>
        <Field label="Handled by *" wide><input required list="patient-edit-assignees" value={form.assignedClinicianEntry} onChange={e => { const selected = options.data?.assignees.find(x => x.displayName === e.target.value); update('assignedClinicianEntry', e.target.value); update('assignedClinicianUserId', selected?.userId) }} /><datalist id="patient-edit-assignees">{options.data?.assignees.map(x => <option key={x.userId} value={x.displayName} />)}</datalist></Field>
        <Field label="Remarks" wide><textarea rows={3} value={form.remarks ?? ''} onChange={e => update('remarks', e.target.value)} /></Field>
      </div><div className="dialog-actions"><button type="button" className="button secondary" onClick={close}>Close</button><button type="submit" className="button primary" disabled={submitting}>{submitting ? 'Saving...' : 'Save patient'}</button></div>
    </form>}
  </div></section></div>
}

function Field({ label, wide, children }: { label: string; wide?: boolean; children: React.ReactNode }) { return <label className={`form-field ${wide ? 'wide' : ''}`}><span>{label}</span>{children}</label> }
