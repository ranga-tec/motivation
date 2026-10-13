import { useState } from 'react'
import { api } from '../api/client'
import type { Appointment, AssigneeOption, CreateAppointmentRequest, PatientSummary } from '../api/types'
import { useAuth } from '../auth/AuthProvider'
import { useApi } from '../hooks/useApi'
import { access } from '../lib/access'
import { errorMessage, formatDate, localDate } from '../lib/format'
import { Combobox } from './Combobox'
import { PatientSearch } from './PatientPickerDialog'
import { Field, FormModal, Segmented } from './Ui'

// PRD 5.4.3: appointments cover assessment, fitting and delivery. Follow-ups live in clinical records.
const visitTypes = [{ value: 'Assessment', label: 'Assessment' }, { value: 'Fitting', label: 'Fitting' }, { value: 'Delivery', label: 'Delivery' }]

export function AssigneeInput({ value, assignees, onChange }: { value: string; assignees: AssigneeOption[]; onChange: (entry: string, userId?: string) => void }) {
  const text = value.trim().toLowerCase()
  const options = assignees.filter((item) => !text || item.displayName.toLowerCase().includes(text) || item.fullName.toLowerCase().includes(text))
    .sort((a, b) => Number(b.isPreferred) - Number(a.isPreferred)).slice(0, 12)
    .map((item) => ({ key: item.userId, label: item.displayName, sub: item.isPreferred ? 'Prosthetist / Orthotist' : undefined }))
  return <Combobox required value={value} placeholder="Choose staff or type a name" options={options} emptyText="No staff match — the typed name will be used"
    onChange={(entry) => onChange(entry, assignees.find((item) => item.displayName === entry)?.userId)}
    onSelect={(option) => onChange(option.label, option.key)} />
}

export function CreateAppointmentDialog({ close, saved, initialDate, patient: initialPatient }: { close: () => void; saved: (item: Appointment) => void; initialDate?: string; patient?: PatientSummary }) {
  const { roles } = useAuth()
  const can = access(roles)
  const options = useApi(() => api.appointmentOptions(), 'appointment-options')
  const [patient, setPatient] = useState<PatientSummary | undefined>(initialPatient)
  const [form, setForm] = useState<CreateAppointmentRequest>({ patientId: initialPatient?.id ?? '', type: 'Assessment', appointmentDate: initialDate ?? localDate(), appointmentTime: '', assignedClinicianEntry: '' })
  const episodes = useApi(() => api.episodes(patient!.id), `appointment-episodes-${patient?.id}`, Boolean(patient) && can.clinical)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const set = (patch: Partial<CreateAppointmentRequest>) => setForm((current) => ({ ...current, ...patch }))
  const submit = async () => {
    if (!form.patientId) { setError('Select a patient.'); return }
    setSubmitting(true); setError('')
    try { saved(await api.createAppointment({ ...form, episodeId: form.episodeId || undefined })) }
    catch (failure) { setError(errorMessage(failure, 'The appointment could not be created.')) }
    finally { setSubmitting(false) }
  }
  return <FormModal title="New appointment" description="Book an assessment, fitting or delivery visit." onClose={close} onSubmit={submit} submitLabel="Book appointment" submitting={submitting} disabled={!form.patientId || options.loading} error={error} size="md">
    <div className="form-grid">
      <Field label="Patient" required className="span-all"><PatientSearch autoFocus={!initialPatient} selected={patient} onSelect={(value) => { setPatient(value); set({ patientId: value?.id ?? '', episodeId: undefined }) }} /></Field>
      {patient && can.clinical && <Field label="Patient record" className="span-all" hint="Optional. Links the visit to a treatment record.">
        <select value={form.episodeId ?? ''} onChange={(event) => set({ episodeId: event.target.value || undefined })} disabled={episodes.loading}>
          <option value="">{episodes.loading ? 'Loading records…' : 'Not linked to a record'}</option>
          {episodes.data?.map((episode) => <option key={episode.id} value={episode.id}>{formatDate(episode.recordDate)} · {episode.centerName} · {episode.status} — {episode.assessmentCount} assessments, {episode.fittingCount} fittings, {episode.deliveryCount} deliveries</option>)}
        </select>
      </Field>}
      <Field label="Visit type" required className="span-all"><Segmented label="Visit type" value={form.type} options={visitTypes} onChange={(type) => set({ type })} /></Field>
      <Field label="Date" required><input required type="date" value={form.appointmentDate} onChange={(event) => set({ appointmentDate: event.target.value })} /></Field>
      <Field label="Time" hint="Leave blank if not fixed"><input type="time" value={form.appointmentTime} onChange={(event) => set({ appointmentTime: event.target.value })} /></Field>
      <Field label="Handled by" required className="span-all"><AssigneeInput value={form.assignedClinicianEntry} assignees={options.data?.assignees ?? []} onChange={(entry, userId) => set({ assignedClinicianEntry: entry, assignedClinicianUserId: userId })} /></Field>
      <Field label="Notes" className="span-all"><textarea rows={3} maxLength={2000} value={form.notes ?? ''} onChange={(event) => set({ notes: event.target.value })} /></Field>
    </div>
  </FormModal>
}

export function AppointmentActionDialog({ value, close, saved }: { value: { kind: 'cancel' | 'reschedule'; appointment: Appointment }; close: () => void; saved: (message: string) => void }) {
  const rescheduling = value.kind === 'reschedule'
  const [date, setDate] = useState(value.appointment.appointmentDate)
  const [time, setTime] = useState(value.appointment.appointmentTime?.slice(0, 5) ?? '')
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const submit = async () => {
    setSubmitting(true); setError('')
    try {
      if (rescheduling) await api.rescheduleAppointment(value.appointment.id, date, time || undefined, reason)
      else await api.cancelAppointment(value.appointment.id, reason)
      saved(`${value.appointment.patientName}'s appointment was ${rescheduling ? 'rescheduled' : 'cancelled'}.`)
    } catch (failure) { setError(errorMessage(failure, 'The appointment could not be updated.')) }
    finally { setSubmitting(false) }
  }
  return <FormModal title={rescheduling ? 'Reschedule appointment' : 'Cancel appointment'} description={`${value.appointment.patientName} · ${value.appointment.patientNumber}`} onClose={close} onSubmit={submit} submitLabel={rescheduling ? 'Save new time' : 'Cancel appointment'} danger={!rescheduling} submitting={submitting} error={error}>
    <div className="form-grid">
      {rescheduling && <><Field label="New date" required><input required type="date" value={date} onChange={(event) => setDate(event.target.value)} /></Field><Field label="New time"><input type="time" value={time} onChange={(event) => setTime(event.target.value)} /></Field></>}
      <Field label={rescheduling ? 'Reason for rescheduling' : 'Reason for cancellation'} required className="span-all"><textarea required maxLength={500} rows={3} value={reason} onChange={(event) => setReason(event.target.value)} /></Field>
    </div>
  </FormModal>
}
