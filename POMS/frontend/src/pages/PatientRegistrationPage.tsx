import { AlertTriangle, ArrowLeft, CheckCircle2, Save } from 'lucide-react'
import { useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { CreatePatientRequest, PatientDetail } from '../api/types'
import { ErrorPanel, LoadingRows, PageHeader } from '../components/Ui'
import { useApi } from '../hooks/useApi'

const today = new Date().toISOString().slice(0, 10)
const latestDob = new Date(Date.now() - 3 * 86400000).toISOString().slice(0, 10)
const initialForm: CreatePatientRequest = {
  fullName: '', nameWithInitials: '', dateOfBirth: '', sex: '', category: 'Local', identificationType: 'NIC', identificationNumber: '',
  address1: '', provinceId: 0, districtId: 0, cityOther: '', centerId: 0, registrationDate: today,
  assignedClinicianEntry: '', guardianName: '', guardianRelationship: '', confirmPossibleDuplicate: false, contacts: [{ telephoneNumber: '' }],
}

export function PatientRegistrationPage() {
  const options = useApi(() => api.registrationOptions(), 'patient-registration-options')
  const [form, setForm] = useState<CreatePatientRequest>(initialForm)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [duplicate, setDuplicate] = useState<{ number?: string; name?: string }>()
  const [created, setCreated] = useState<PatientDetail>()
  const districts = useMemo(() => options.data?.districts.filter((item) => item.parentId === form.provinceId) ?? [], [options.data, form.provinceId])
  const cities = useMemo(() => options.data?.cities.filter((item) => item.parentId === form.districtId) ?? [], [options.data, form.districtId])

  const update = <K extends keyof CreatePatientRequest>(key: K, value: CreatePatientRequest[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setFieldErrors((current) => { const next = { ...current }; delete next[key.toLowerCase()]; return next })
  }
  const submit = async (event?: FormEvent, confirmDuplicate = false) => {
    event?.preventDefault(); setSubmitting(true); setError(''); setFieldErrors({}); setDuplicate(undefined)
    try { setCreated(await api.createPatient({ ...form, confirmPossibleDuplicate: confirmDuplicate })) }
    catch (reason) {
      if (reason instanceof ApiError) {
        if (reason.problem?.duplicateType === 'possible') setDuplicate({ number: reason.problem.existingPatientNumber, name: reason.problem.existingPatientName })
        else {
          setError(reason.message)
          const errors: Record<string, string> = {}
          Object.entries(reason.problem?.errors ?? {}).forEach(([key, values]) => { errors[key.toLowerCase()] = values[0] })
          setFieldErrors(errors)
        }
      } else setError('Registration could not be completed. Check your connection and try again.')
    } finally { setSubmitting(false) }
  }

  if (created) return <div className="registration-result"><CheckCircle2 /><p className="eyebrow">Registration complete</p><h1>{created.fullName}</h1><div className="patient-number-result">{created.patientNumber}</div><p>The patient record is ready for clinical activity.</p><div><Link className="button primary" to="/patients">Return to patients</Link><button className="button secondary" onClick={() => { setCreated(undefined); setForm(initialForm) }}>Register another patient</button></div></div>
  return <>
    <PageHeader eyebrow="Patient administration" title="Register patient" description="Create one verified patient record. Fields marked * are required." action={<Link className="button secondary" to="/patients"><ArrowLeft size={17} /> Back to patients</Link>} />
    {options.error ? <ErrorPanel message={options.error} retry={options.refresh} /> : options.loading ? <section className="panel"><LoadingRows /></section> :
    <form className="registration-form" onSubmit={(event) => void submit(event)}>
      {error && <div className="error-panel" role="alert"><div><strong>Registration was not saved</strong><span>{error}</span></div></div>}
      {duplicate && <div className="duplicate-panel" role="alert"><AlertTriangle /><div><strong>Possible matching patient</strong><p>{duplicate.number} — {duplicate.name} has the same name and date of birth. Check the existing record before continuing.</p><button type="button" className="button secondary" disabled={submitting} onClick={() => void submit(undefined, true)}>Details checked — register anyway</button></div></div>}

      <FormSection number="01" title="Patient identity" description="Details used to identify the patient and prevent duplicate records.">
        <Field label="Full name" required error={fieldErrors.fullname} wide><input required maxLength={200} autoComplete="name" value={form.fullName} onChange={(e) => update('fullName', e.target.value)} /></Field>
        <Field label="Name with initials" required error={fieldErrors.namewithinitials}><input required maxLength={100} value={form.nameWithInitials} onChange={(e) => update('nameWithInitials', e.target.value)} /></Field>
        <Field label="Date of birth" required error={fieldErrors.dateofbirth}><input required type="date" max={latestDob} value={form.dateOfBirth} onChange={(e) => update('dateOfBirth', e.target.value)} /></Field>
        <Field label="Gender" required><select required value={form.sex} onChange={(e) => update('sex', e.target.value)}><option value="">Select gender</option><option>Male</option><option>Female</option><option>Other</option></select></Field>
        <Field label="Patient category" required><select value={form.category} onChange={(e) => update('category', e.target.value)}><option>Local</option><option>Foreign</option></select></Field>
        {form.category === 'Foreign' && <Field label="Nationality" required error={fieldErrors.nationality}><input required value={form.nationality ?? ''} onChange={(e) => update('nationality', e.target.value)} /></Field>}
        <Field label="Identification type" required><select value={form.identificationType} onChange={(e) => update('identificationType', e.target.value)}><option value="NIC">NIC</option><option value="DrivingLicense">Driving licence</option><option value="Passport">Passport</option><option value="NotApplicable">N/A</option></select></Field>
        {form.identificationType !== 'NotApplicable' && <Field label="Identification number" required error={fieldErrors.identificationnumber}><input required maxLength={50} value={form.identificationNumber ?? ''} onChange={(e) => update('identificationNumber', e.target.value)} /></Field>}
        <Field label="Employment"><input value={form.employment ?? ''} onChange={(e) => update('employment', e.target.value)} /></Field>
      </FormSection>

      <FormSection number="02" title="Contact and location" description="Where the patient lives and the safest way to make contact.">
        <Field label="Address line 1" required wide error={fieldErrors.address1}><input required value={form.address1} onChange={(e) => update('address1', e.target.value)} /></Field>
        <Field label="Address line 2" wide><input value={form.address2 ?? ''} onChange={(e) => update('address2', e.target.value)} /></Field>
        <Field label="Province" required><select required value={form.provinceId || ''} onChange={(e) => { update('provinceId', Number(e.target.value)); update('districtId', 0); update('cityId', undefined) }}><option value="">Select province</option>{options.data?.provinces.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
        <Field label="District" required error={fieldErrors.districtid}><select required disabled={!form.provinceId} value={form.districtId || ''} onChange={(e) => { update('districtId', Number(e.target.value)); update('cityId', undefined) }}><option value="">Select district</option>{districts.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
        <Field label="City"><select disabled={!form.districtId} value={form.cityId ?? ''} onChange={(e) => update('cityId', e.target.value ? Number(e.target.value) : undefined)}><option value="">Not listed</option>{cities.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
        {!form.cityId && <Field label="City (if not listed)" required error={fieldErrors.cityother}><input required value={form.cityOther ?? ''} onChange={(e) => update('cityOther', e.target.value)} /></Field>}
        <Field label="Telephone number" required><input required type="tel" value={form.contacts[0]?.telephoneNumber ?? ''} onChange={(e) => update('contacts', [{ telephoneNumber: e.target.value }])} /></Field>
        <Field label="Email"><input type="text" placeholder="Email address or N/A" value={form.email ?? ''} onChange={(e) => update('email', e.target.value)} /></Field>
      </FormSection>

      <FormSection number="03" title="Guardian or carer" description="Primary support person for communication and care decisions.">
        <Field label="Guardian / carer name" required wide error={fieldErrors.guardianname}><input required value={form.guardianName} onChange={(e) => update('guardianName', e.target.value)} /></Field>
        <Field label="Relationship" required><select required value={form.guardianRelationship} onChange={(e) => update('guardianRelationship', e.target.value)}><option value="">Select relationship</option>{['Mother','Father','Spouse','Brother','Sister','Son','Daughter','Caregiver','Friend','Other'].map((item) => <option key={item}>{item}</option>)}</select></Field>
        <Field label="Mobile number"><input type="tel" value={form.guardianMobile ?? ''} onChange={(e) => update('guardianMobile', e.target.value)} /></Field>
        <Field label="Guardian address" wide><input value={form.guardianAddress ?? ''} onChange={(e) => update('guardianAddress', e.target.value)} /></Field>
      </FormSection>

      <FormSection number="04" title="Registration assignment" description="Route this record to the correct centre and clinician.">
        <Field label="Treatment centre" required error={fieldErrors.centerid}><select required value={form.centerId || ''} onChange={(e) => update('centerId', Number(e.target.value))}><option value="">Select centre</option>{options.data?.centers.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
        <Field label="Registration date" required><input required type="date" max={today} value={form.registrationDate} onChange={(e) => update('registrationDate', e.target.value)} /></Field>
        <Field label="Handled by" required wide error={fieldErrors.assignedclinicianentry}><input required list="assignees" placeholder="Select staff or type a Prosthetist / Orthotist name" value={form.assignedClinicianEntry} onChange={(e) => { const selected = options.data?.assignees.find((item) => item.displayName === e.target.value); update('assignedClinicianEntry', e.target.value); update('assignedClinicianUserId', selected?.userId) }} /><datalist id="assignees">{options.data?.assignees.map((item) => <option key={item.userId} value={item.displayName} />)}</datalist></Field>
        <Field label="Remarks" wide><textarea rows={3} value={form.remarks ?? ''} onChange={(e) => update('remarks', e.target.value)} /></Field>
      </FormSection>

      <div className="form-actions"><Link className="button secondary" to="/patients">Cancel</Link><button className="button primary" disabled={submitting} type="submit">{submitting ? <span className="spinner light" /> : <Save size={17} />}{submitting ? 'Registering…' : 'Register patient'}</button></div>
    </form>}
  </>
}

function FormSection({ number, title, description, children }: { number: string; title: string; description: string; children: React.ReactNode }) {
  return <section className="panel form-section"><header><span>{number}</span><div><h2>{title}</h2><p>{description}</p></div></header><div className="form-grid">{children}</div></section>
}
function Field({ label, required, error, wide, children }: { label: string; required?: boolean; error?: string; wide?: boolean; children: React.ReactNode }) {
  return <label className={`form-field ${wide ? 'wide' : ''}`}><span>{label}{required && <b aria-hidden="true"> *</b>}</span>{children}{error && <small className="field-error">{error}</small>}</label>
}
