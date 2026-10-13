import { useEffect, useMemo, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, ArrowRight, Camera, Check, CheckCircle2, ClipboardPlus, Pencil, Plus, Save, Trash2, UserPlus } from 'lucide-react'
import { api, ApiError } from '../api/client'
import type { CreatePatientRequest, PatientDetail, PatientRegistrationOptions } from '../api/types'
import { useAuth } from '../auth/AuthProvider'
import { AssigneeInput } from '../components/AppointmentDialogs'
import { Alert, ErrorPanel, Field, PageHeader, Skeleton, Spinner } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { formatDate, humanize, localDate } from '../lib/format'
import { useToast } from '../lib/toast'

const latestDob = () => { const date = new Date(); date.setDate(date.getDate() - 3); return localDate(date) }
const blank = (): CreatePatientRequest => ({
  fullName: '', nameWithInitials: '', dateOfBirth: '', sex: '', category: 'Local', identificationType: 'NIC', identificationNumber: '',
  address1: '', provinceId: 0, districtId: 0, cityOther: '', centerId: 0, registrationDate: localDate(),
  assignedClinicianEntry: '', guardianName: '', guardianRelationship: '', confirmPossibleDuplicate: false, contacts: [{ telephoneNumber: '', dateConfirmed: localDate() }],
})
const relationships = ['Mother', 'Father', 'Spouse', 'Brother', 'Sister', 'Son', 'Daughter', 'Guardian', 'Caregiver', 'Friend', 'Self', 'Other']
const steps = [
  { key: 'identity', title: 'Identity', hint: 'Name, ID and location' },
  { key: 'contact', title: 'Contact', hint: 'Address and phones' },
  { key: 'guardian', title: 'Guardian / carer', hint: 'Support person' },
  { key: 'registration', title: 'Registration', hint: 'Referral and staff' },
  { key: 'review', title: 'Review', hint: 'Confirm and save' },
]
// Maps server validation keys to the wizard step that owns the field.
const fieldStep: Record<string, number> = {
  fullname: 0, namewithinitials: 0, dateofbirth: 0, sex: 0, category: 0, nationality: 0, identificationtype: 0, identificationnumber: 0, centerid: 0, employment: 0,
  address1: 1, address2: 1, provinceid: 1, districtid: 1, cityid: 1, cityother: 1, email: 1, contacts: 1,
  guardianname: 2, guardianrelationship: 2, guardianphone: 2, guardianmobile: 2, guardianaddress: 2,
  registrationdate: 3, assignedclinicianentry: 3, referralsourceid: 3, referralsourceother: 3, traveltimedistance: 3, remarks: 3,
}

export function PatientRegistrationPage() {
  const { id } = useParams()
  const editing = Boolean(id)
  const options = useApi(() => api.registrationOptions(), 'registration-options')
  const existing = useApi(() => api.patientForEdit(id!), `patient-edit-${id}`, editing)
  if (options.error || existing.error) return <><Header editing={editing} id={id} /><div className="card"><ErrorPanel message={options.error || existing.error} retry={() => { options.refresh(); existing.refresh() }} /></div></>
  if (options.loading || existing.loading || !options.data || (editing && !existing.data)) return <><Header editing={editing} id={id} /><div className="card"><Skeleton rows={8} tall /></div></>
  // The edit endpoint stores the clinician's full name; the server matches a selected account by its display text.
  const assignee = existing.data?.assignedClinicianUserId ? options.data.assignees.find((item) => item.userId === existing.data!.assignedClinicianUserId) : undefined
  const initial = existing.data ? {
    ...existing.data, confirmPossibleDuplicate: false,
    assignedClinicianEntry: assignee?.displayName ?? existing.data.assignedClinicianEntry,
    assignedClinicianUserId: assignee?.userId,
    contacts: existing.data.contacts.length ? existing.data.contacts : [{ telephoneNumber: '' }],
  } : blank()
  return <RegistrationWizard key={id ?? 'new'} id={id} initial={initial} options={options.data} />
}

function Header({ editing, id }: { editing: boolean; id?: string }) {
  return <>
    <Link to={editing ? `/patients/${id}` : '/patients'} className="back-link"><ArrowLeft size={16} /> {editing ? 'Back to patient folder' : 'Patient folder'}</Link>
    <PageHeader eyebrow={editing ? 'Patient folder' : 'New patient'} title={editing ? 'Edit patient details' : 'Register patient'} description={editing ? 'Changes keep the existing patient number.' : 'The patient number (PNO) is generated automatically when you save.'} />
  </>
}

function RegistrationWizard({ id, initial, options }: { id?: string; initial: CreatePatientRequest; options: PatientRegistrationOptions }) {
  const editing = Boolean(id)
  const { user } = useAuth()
  const notify = useToast()
  const navigate = useNavigate()
  const formRef = useRef<HTMLFormElement>(null)
  const [step, setStep] = useState(0)
  const [visited, setVisited] = useState(editing ? steps.length - 1 : 0)
  const [form, setForm] = useState<CreatePatientRequest>(initial)
  const [photo, setPhoto] = useState<File>()
  const [photoPreview, setPhotoPreview] = useState('')
  const [zoom, setZoom] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [duplicate, setDuplicate] = useState<{ number?: string; name?: string }>()
  const [created, setCreated] = useState<{ patient: PatientDetail; warning?: string }>()
  useEffect(() => () => { if (photoPreview) URL.revokeObjectURL(photoPreview) }, [photoPreview])

  const districts = useMemo(() => options.districts.filter((item) => item.parentId === form.provinceId), [options.districts, form.provinceId])
  const cities = useMemo(() => options.cities.filter((item) => item.parentId === form.districtId), [options.cities, form.districtId])
  const referral = options.referralSources.find((item) => item.id === form.referralSourceId)
  const centre = options.centers.find((item) => item.id === form.centerId)
  const set = (patch: Partial<CreatePatientRequest>) => {
    setForm((current) => ({ ...current, ...patch }))
    setFieldErrors((current) => { const next = { ...current }; Object.keys(patch).forEach((key) => delete next[key.toLowerCase()]); return next })
  }
  const setContact = (index: number, patch: Partial<CreatePatientRequest['contacts'][number]>) => set({ contacts: form.contacts.map((contact, position) => position === index ? { ...contact, ...patch } : contact) })

  // Focus the first real input whenever the step changes (client request: tab order starts on a field, not the heading).
  useEffect(() => {
    const first = formRef.current?.querySelector<HTMLElement>('.wizard-panel input:not([type=file]):not([disabled]), .wizard-panel select:not([disabled]), .wizard-panel textarea')
    first?.focus({ preventScroll: true })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }, [step])

  const goTo = (target: number) => {
    if (target > step && !formRef.current?.reportValidity()) return
    const next = Math.max(0, Math.min(target, steps.length - 1))
    if (next > visited + 1 && next > step) return
    setStep(next); setVisited((value) => Math.max(value, next))
  }
  const submit = async (confirmDuplicate = false) => {
    setSubmitting(true); setError(''); setFieldErrors({}); setDuplicate(undefined)
    const request: CreatePatientRequest = {
      ...form,
      confirmPossibleDuplicate: confirmDuplicate,
      identificationNumber: form.identificationType === 'NotApplicable' ? '' : form.identificationNumber,
      nationality: form.category === 'Foreign' ? form.nationality : form.nationality || undefined,
      referralSourceOther: referral?.name.toLowerCase().startsWith('other') ? form.referralSourceOther : undefined,
      contacts: form.contacts.filter((contact) => contact.telephoneNumber.trim()).map((contact) => ({ ...contact, dateConfirmed: contact.dateConfirmed || undefined, personChecked: contact.personChecked || undefined })),
    }
    try {
      if (editing) {
        await api.updatePatient(id!, request)
        notify(`${request.fullName}'s details were updated.`)
        navigate(`/patients/${id}`)
        return
      }
      const patient = await api.createPatient(request)
      let warning: string | undefined
      if (photo) {
        try { await api.uploadDocument('patient', patient.id, 'PatientPhoto', 'Registration profile photo', false, photo) }
        catch { warning = 'The patient was registered, but the photo could not be uploaded. Add it from the patient folder.' }
      }
      setCreated({ patient, warning })
    } catch (reason) {
      if (reason instanceof ApiError && reason.problem?.duplicateType === 'possible') { setDuplicate({ number: reason.problem.existingPatientNumber, name: reason.problem.existingPatientName }); return }
      const message = reason instanceof Error ? reason.message : 'Registration could not be completed.'
      setError(message)
      if (reason instanceof ApiError) {
        const errors: Record<string, string> = {}
        Object.entries(reason.problem?.errors ?? {}).forEach(([key, values]) => { errors[key.toLowerCase().split('[')[0].replace(/^request\./, '')] = values[0] })
        setFieldErrors(errors)
        const firstStep = Math.min(...Object.keys(errors).map((key) => fieldStep[key] ?? 4))
        if (Number.isFinite(firstStep) && firstStep < 4) setStep(firstStep)
      }
    } finally { setSubmitting(false) }
  }
  const onSubmit = (event: FormEvent) => { event.preventDefault(); if (step < steps.length - 1) goTo(step + 1); else void submit() }
  const onKeyDown = (event: KeyboardEvent) => {
    if (event.altKey && event.key === 'ArrowRight') { event.preventDefault(); goTo(step + 1) }
    if (event.altKey && event.key === 'ArrowLeft') { event.preventDefault(); goTo(step - 1) }
  }

  if (created) return <div className="card success-panel">
    <div className="success-icon"><CheckCircle2 /></div>
    <p className="eyebrow">Registration complete</p>
    <h1>{created.patient.fullName}</h1>
    <div className="pno-big">{created.patient.patientNumber}</div>
    <p className="muted">Patient folder created at {created.patient.centerName}. Use this patient number on every record.</p>
    {created.warning && <div style={{ marginTop: 16, textAlign: 'left' }}><Alert tone="warning">{created.warning}</Alert></div>}
    <div className="btn-group">
      <Link className="btn btn-primary" to={`/patients/${created.patient.id}`}>Open patient folder</Link>
      <Link className="btn btn-secondary" to={`/patients/${created.patient.id}?newRecord=1`}><ClipboardPlus /> New record</Link>
      <button className="btn btn-ghost" onClick={() => { setCreated(undefined); setForm(blank()); setPhoto(undefined); setPhotoPreview(''); setStep(0); setVisited(0) }}><UserPlus /> Register another</button>
    </div>
  </div>

  const review = (title: string, target: number, rows: [string, string | undefined | null][]) => <section className="review-block">
    <header><h3>{title}</h3><button type="button" className="btn btn-ghost btn-sm" onClick={() => setStep(target)}><Pencil /> Edit</button></header>
    <dl className="dl">{rows.map(([label, value]) => <div key={label}><dt>{label}</dt><dd className={value ? '' : 'empty-value'}>{value || 'Not provided'}</dd></div>)}</dl>
  </section>

  return <>
    <Header editing={editing} id={id} />
    <div className="wizard">
      <nav className="card wizard-steps" aria-label="Registration steps">
        {steps.map((item, index) => <button key={item.key} type="button" className={`wizard-step ${index === step ? 'active' : index <= visited && index < step ? 'done' : ''}`} aria-current={index === step ? 'step' : undefined} onClick={() => goTo(index)} disabled={index > visited + 1}>
          <span className="num">{index < step && index <= visited ? <Check /> : index + 1}</span><span><strong>{item.title}</strong><small>{item.hint}</small></span>
        </button>)}
      </nav>
      <form ref={formRef} className="card wizard-body" onSubmit={onSubmit} onKeyDown={onKeyDown} noValidate={false}>
        <div className="wizard-progress"><div style={{ width: `${((step + 1) / steps.length) * 100}%` }} /></div>
        <header className="card-header"><div><h2>{steps[step].title}</h2><p>Step {step + 1} of {steps.length} · Fields marked <span style={{ color: 'var(--danger)' }}>*</span> are required</p></div></header>
        <div className="card-body wizard-panel">
          {error && <Alert title={editing ? 'Changes were not saved' : 'Registration was not saved'}>{error}</Alert>}
          {duplicate && <Alert tone="warning" title="Possible existing patient" action={<button type="button" className="btn btn-secondary btn-sm" disabled={submitting} onClick={() => void submit(true)}>Checked — save anyway</button>}>
            {duplicate.number} · {duplicate.name} has the same name and date of birth. Open the existing folder to check before continuing.</Alert>}

          {step === 0 && <div className="form-grid">
            {!editing && <div className="span-all photo-picker">
              <div className="avatar lg">{photoPreview ? <img src={photoPreview} alt="Selected patient" /> : <Camera />}</div>
              <div><strong>Patient photo</strong><span>Optional. A clear head-and-shoulders photo, JPG or PNG up to 5 MB.</span>
                <label className="btn btn-secondary btn-sm"><Camera /> {photo ? 'Change photo' : 'Choose photo'}<input className="sr-only" type="file" accept="image/jpeg,image/png" onChange={(event) => {
                  const selected = event.target.files?.[0]
                  if (selected && selected.size > 5 * 1024 * 1024) { notify('The photo must be 5 MB or smaller.', 'error'); return }
                  if (photoPreview) URL.revokeObjectURL(photoPreview)
                  setPhoto(selected); setPhotoPreview(selected ? URL.createObjectURL(selected) : '')
                }} /></label>
                {photo && <button type="button" className="btn btn-ghost btn-sm" onClick={() => { setPhoto(undefined); setPhotoPreview('') }}>Remove</button>}
              </div>
            </div>}
            <Field label="Centre / location" required error={fieldErrors.centerid} hint={centre ? 'Determines the patient number format.' : undefined}>
              <select required value={form.centerId || ''} onChange={(event) => set({ centerId: Number(event.target.value) })}><option value="">Select location</option>{options.centers.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
            </Field>
            <Field label="Patient category" required>
              <div className="radio-cards">{['Local', 'Foreign'].map((value) => <label key={value} className="radio-card"><input type="radio" name="category" value={value} checked={form.category === value} onChange={() => set({ category: value, identificationType: value === 'Foreign' && form.identificationType === 'NIC' ? 'Passport' : form.identificationType })} />{value} patient</label>)}</div>
            </Field>
            <Field label="Full name" required error={fieldErrors.fullname} className="span-2"><input required maxLength={200} autoComplete="off" value={form.fullName} onChange={(event) => set({ fullName: event.target.value })} /></Field>
            <Field label="Name with initials" required error={fieldErrors.namewithinitials}><input required maxLength={100} value={form.nameWithInitials} onChange={(event) => set({ nameWithInitials: event.target.value })} placeholder="e.g. A. B. Perera" /></Field>
            <Field label="Date of birth" required error={fieldErrors.dateofbirth}><input required type="date" max={latestDob()} value={form.dateOfBirth} onChange={(event) => set({ dateOfBirth: event.target.value })} /></Field>
            <Field label="Gender" required error={fieldErrors.sex}><select required value={form.sex} onChange={(event) => set({ sex: event.target.value })}><option value="">Select gender</option><option>Male</option><option>Female</option><option>Other</option></select></Field>
            {form.category === 'Foreign'
              ? <Field label="Nationality" required error={fieldErrors.nationality}>{options.nationalities.length
                  ? <select required value={form.nationality ?? ''} onChange={(event) => set({ nationality: event.target.value })}><option value="">Select nationality</option>{options.nationalities.map((item) => <option key={item.id}>{item.name}</option>)}{form.nationality && !options.nationalities.some((item) => item.name === form.nationality) && <option>{form.nationality}</option>}</select>
                  : <input required value={form.nationality ?? ''} onChange={(event) => set({ nationality: event.target.value })} />}</Field>
              : <Field label="Employment" error={fieldErrors.employment}><input maxLength={200} value={form.employment ?? ''} onChange={(event) => set({ employment: event.target.value })} /></Field>}
            <Field label="Identification type" required><select value={form.identificationType} onChange={(event) => set({ identificationType: event.target.value, identificationNumber: event.target.value === 'NotApplicable' ? '' : form.identificationNumber })}><option value="NIC">NIC</option><option value="Passport">Passport</option><option value="DrivingLicense">Driving licence</option><option value="NotApplicable">N/A — no identification</option></select></Field>
            <Field label="Identification number" required={form.identificationType !== 'NotApplicable'} error={fieldErrors.identificationnumber} hint="Checked against existing patients for duplicates."><input required={form.identificationType !== 'NotApplicable'} disabled={form.identificationType === 'NotApplicable'} maxLength={50} value={form.identificationNumber ?? ''} onChange={(event) => set({ identificationNumber: event.target.value })} placeholder={form.identificationType === 'NotApplicable' ? 'Not applicable' : ''} /></Field>
            {form.category === 'Foreign' && <Field label="Employment" error={fieldErrors.employment}><input maxLength={200} value={form.employment ?? ''} onChange={(event) => set({ employment: event.target.value })} /></Field>}
          </div>}

          {step === 1 && <>
            <div className="form-grid">
              <Field label="Address line 1" required error={fieldErrors.address1} className="span-2"><input required maxLength={300} value={form.address1} onChange={(event) => set({ address1: event.target.value })} /></Field>
              <Field label="Address line 2" className="span-2"><input maxLength={300} value={form.address2 ?? ''} onChange={(event) => set({ address2: event.target.value })} /></Field>
              <Field label="Province" required error={fieldErrors.provinceid}><select required value={form.provinceId || ''} onChange={(event) => set({ provinceId: Number(event.target.value), districtId: 0, cityId: undefined })}><option value="">Select province</option>{options.provinces.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
              <Field label="District" required error={fieldErrors.districtid}><select required disabled={!form.provinceId} value={form.districtId || ''} onChange={(event) => set({ districtId: Number(event.target.value), cityId: undefined })}><option value="">{form.provinceId ? 'Select district' : 'Select a province first'}</option>{districts.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
              <Field label="City" required={!form.cityOther} error={fieldErrors.cityid}><select disabled={!form.districtId} value={form.cityId ?? ''} onChange={(event) => set({ cityId: event.target.value ? Number(event.target.value) : undefined, cityOther: event.target.value ? '' : form.cityOther })}><option value="">{form.districtId ? 'Not listed — type below' : 'Select a district first'}</option>{cities.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
              {!form.cityId && <Field label="City (if not listed)" required error={fieldErrors.cityother}><input required maxLength={100} value={form.cityOther ?? ''} onChange={(event) => set({ cityOther: event.target.value })} /></Field>}
              <Field label="Email" error={fieldErrors.email} hint="Enter N/A if the patient has no email." className={form.cityId ? '' : 'span-2'}><input type="text" inputMode="email" maxLength={256} placeholder="name@example.com or N/A" value={form.email ?? ''} onChange={(event) => set({ email: event.target.value })} /></Field>
            </div>
            <div className="form-section" style={{ marginTop: 24 }}>
              <div className="form-section-title"><div><h3>Telephone numbers</h3><p>Record when each number was confirmed and who checked it.</p></div><button type="button" className="btn btn-secondary btn-sm" onClick={() => set({ contacts: [...form.contacts, { telephoneNumber: '', dateConfirmed: localDate() }] })}><Plus /> Add number</button></div>
              {fieldErrors.contacts && <p className="field-error" style={{ marginBottom: 10 }}>{fieldErrors.contacts}</p>}
              {form.contacts.map((contact, index) => <div key={index} className="phone-row">
                <Field label={index === 0 ? 'Telephone number' : `Telephone number ${index + 1}`} required={index === 0}><input type="tel" required={index === 0} maxLength={30} value={contact.telephoneNumber} onChange={(event) => setContact(index, { telephoneNumber: event.target.value })} placeholder="07X XXX XXXX" /></Field>
                <Field label="Date confirmed"><input type="date" max={localDate()} value={contact.dateConfirmed ?? ''} onChange={(event) => setContact(index, { dateConfirmed: event.target.value })} /></Field>
                <Field label="Person checked"><input maxLength={100} value={contact.personChecked ?? ''} onChange={(event) => setContact(index, { personChecked: event.target.value })} placeholder={user?.name} /></Field>
                <button type="button" className="icon-btn danger" disabled={form.contacts.length === 1} onClick={() => set({ contacts: form.contacts.filter((_, position) => position !== index) })} aria-label="Remove number"><Trash2 /></button>
              </div>)}
            </div>
          </>}

          {step === 2 && <div className="form-grid">
            <Field label="Guardian / carer name" required error={fieldErrors.guardianname} className="span-2"><input required maxLength={100} value={form.guardianName} onChange={(event) => set({ guardianName: event.target.value })} /></Field>
            <Field label="Relationship" required error={fieldErrors.guardianrelationship}><select required value={relationships.includes(form.guardianRelationship) || !form.guardianRelationship ? form.guardianRelationship : 'Other'} onChange={(event) => set({ guardianRelationship: event.target.value })}><option value="">Select relationship</option>{relationships.map((item) => <option key={item}>{item}</option>)}</select></Field>
            <Field label="Mobile" error={fieldErrors.guardianmobile}><input type="tel" maxLength={30} value={form.guardianMobile ?? ''} onChange={(event) => set({ guardianMobile: event.target.value })} /></Field>
            <Field label="Phone" error={fieldErrors.guardianphone}><input type="tel" maxLength={30} value={form.guardianPhone ?? ''} onChange={(event) => set({ guardianPhone: event.target.value })} /></Field>
            <Field label="Address" error={fieldErrors.guardianaddress}><input maxLength={300} value={form.guardianAddress ?? ''} onChange={(event) => set({ guardianAddress: event.target.value })} placeholder="Leave blank if same as patient" /></Field>
          </div>}

          {step === 3 && <div className="form-grid">
            <Field label="Date of registration" required error={fieldErrors.registrationdate}><input required type="date" max={localDate()} value={form.registrationDate} onChange={(event) => set({ registrationDate: event.target.value })} /></Field>
            <Field label="Registration processed by" hint="Recorded automatically from your sign-in."><input disabled value={user?.name ?? ''} /></Field>
            <Field label="Handled by (Prosthetist / Orthotist)" required error={fieldErrors.assignedclinicianentry} className="span-2"><AssigneeInput value={form.assignedClinicianEntry} assignees={options.assignees} onChange={(entry, userId) => set({ assignedClinicianEntry: entry, assignedClinicianUserId: userId })} /></Field>
            <Field label="Referral source" hint="How the patient got to know Exceed." error={fieldErrors.referralsourceid}><select value={form.referralSourceId ?? ''} onChange={(event) => set({ referralSourceId: event.target.value ? Number(event.target.value) : undefined })}><option value="">Not recorded</option>{options.referralSources.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
            {referral?.name.toLowerCase().startsWith('other')
              ? <Field label="Other referral source" required error={fieldErrors.referralsourceother}><input required maxLength={150} value={form.referralSourceOther ?? ''} onChange={(event) => set({ referralSourceOther: event.target.value })} /></Field>
              : <Field label="Time / distance to travel to centre" error={fieldErrors.traveltimedistance}><input maxLength={100} placeholder="e.g. 2 hours / 45 km" value={form.travelTimeDistance ?? ''} onChange={(event) => set({ travelTimeDistance: event.target.value })} /></Field>}
            <Field label="Referred by (person)"><input maxLength={150} value={form.referralPersonName ?? ''} onChange={(event) => set({ referralPersonName: event.target.value })} /></Field>
            <Field label="Referrer contact number"><input type="tel" maxLength={30} value={form.referralPersonContactNumber ?? ''} onChange={(event) => set({ referralPersonContactNumber: event.target.value })} /></Field>
            {referral?.name.toLowerCase().startsWith('other') && <Field label="Time / distance to travel to centre" className="span-2"><input maxLength={100} placeholder="e.g. 2 hours / 45 km" value={form.travelTimeDistance ?? ''} onChange={(event) => set({ travelTimeDistance: event.target.value })} /></Field>}
            <Field label="Remarks" className="span-all"><textarea rows={3} maxLength={1000} value={form.remarks ?? ''} onChange={(event) => set({ remarks: event.target.value })} /></Field>
          </div>}

          {step === 4 && <>
            {!editing && photoPreview && <div className="photo-picker" style={{ marginBottom: 14 }}><button type="button" className="avatar lg" style={{ border: 0, padding: 0, cursor: 'zoom-in' }} onClick={() => setZoom(true)} aria-label="Enlarge photo"><img src={photoPreview} alt="Patient" /></button><div><strong>{form.fullName}</strong><span>Click the photo to enlarge.</span></div></div>}
            <div className="review-grid">
              {review('Identity', 0, [['Location', centre?.name], ['Category', `${form.category} patient`], ['Full name', form.fullName], ['Name with initials', form.nameWithInitials], ['Date of birth', form.dateOfBirth && formatDate(form.dateOfBirth)], ['Gender', form.sex], ['Nationality', form.nationality], ['Identification', `${humanize(form.identificationType)}${form.identificationNumber ? ` · ${form.identificationNumber}` : ''}`], ['Employment', form.employment]])}
              {review('Contact', 1, [['Address', [form.address1, form.address2].filter(Boolean).join(', ')], ['Province', options.provinces.find((item) => item.id === form.provinceId)?.name], ['District', options.districts.find((item) => item.id === form.districtId)?.name], ['City', options.cities.find((item) => item.id === form.cityId)?.name ?? form.cityOther], ['Email', form.email], ['Telephone', form.contacts.filter((contact) => contact.telephoneNumber).map((contact) => contact.telephoneNumber).join(', ')]])}
              {review('Guardian / carer', 2, [['Name', form.guardianName], ['Relationship', form.guardianRelationship], ['Mobile', form.guardianMobile], ['Phone', form.guardianPhone], ['Address', form.guardianAddress]])}
              {review('Registration', 3, [['Registration date', formatDate(form.registrationDate)], ['Processed by', user?.name], ['Handled by', form.assignedClinicianEntry], ['Referral source', referral ? (form.referralSourceOther ? `${referral.name} — ${form.referralSourceOther}` : referral.name) : undefined], ['Travel time / distance', form.travelTimeDistance], ['Remarks', form.remarks]])}
            </div>
          </>}
        </div>
        <footer className="wizard-footer">
          <span className="hint">Enter: continue · Alt + ←/→: move between steps</span>
          {step > 0 && <button type="button" className="btn btn-secondary" onClick={() => setStep(step - 1)}><ArrowLeft /> Back</button>}
          {step < steps.length - 1
            ? <button type="submit" className="btn btn-primary">Continue <ArrowRight /></button>
            : <button type="submit" className="btn btn-primary" disabled={submitting}>{submitting ? <Spinner /> : <Save />}{submitting ? 'Saving…' : editing ? 'Save changes' : 'Register patient'}</button>}
        </footer>
      </form>
    </div>
    {zoom && photoPreview && <button className="lightbox" onClick={() => setZoom(false)} onKeyDown={(event) => event.key === 'Escape' && setZoom(false)} autoFocus aria-label="Close photo"><img src={photoPreview} alt="Patient" /></button>}
  </>
}
