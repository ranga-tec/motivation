import { Search, X } from 'lucide-react'
import { useState, type FormEvent, type ReactNode } from 'react'
import { api, ApiError } from '../api/client'
import type { Appointment, CreateAppointmentRequest, PatientSummary } from '../api/types'
import { useApi } from '../hooks/useApi'

const today = new Date().toISOString().slice(0, 10)
const blank: CreateAppointmentRequest = { patientId: '', type: 'Assessment', appointmentDate: today, appointmentTime: '', assignedClinicianEntry: '' }

export function CreateAppointmentDialog({ close, saved }: { close: () => void; saved: (item: Appointment) => void }) {
  const options = useApi(() => api.appointmentOptions(), 'appointment-options')
  const [form, setForm] = useState(blank)
  const [query, setQuery] = useState('')
  const [patients, setPatients] = useState<PatientSummary[]>([])
  const [selected, setSelected] = useState<PatientSummary>()
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const search = async () => {
    if (query.trim().length < 2) return
    setPatients((await api.patients({ search: query.trim(), pageSize: 8 })).items)
  }
  const submit = async (event: FormEvent) => {
    event.preventDefault(); setSubmitting(true); setError('')
    try { saved(await api.createAppointment(form)) }
    catch (failure) { setError(failure instanceof ApiError ? failure.message : 'The appointment could not be created.') }
    finally { setSubmitting(false) }
  }

  return <Dialog title="New appointment" description="Schedule a patient with the appropriate clinician." close={close}><form onSubmit={(event) => void submit(event)}>
    {error && <div className="inline-error">{error}</div>}
    <label className="form-field wide"><span>Patient *</span>{selected ? <div className="selected-record"><div><strong>{selected.fullName}</strong><span>{selected.patientNumber}</span></div><button type="button" onClick={() => { setSelected(undefined); setForm({ ...form, patientId: '' }) }}>Change</button></div> : <><div className="record-search"><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Name or patient number" /><button type="button" onClick={() => void search()} aria-label="Search patients"><Search /></button></div>{patients.length > 0 && <div className="search-results">{patients.map((patient) => <button type="button" key={patient.id} onClick={() => { setSelected(patient); setForm({ ...form, patientId: patient.id }); setPatients([]) }}><strong>{patient.fullName}</strong><span>{patient.patientNumber} · {patient.centerName}</span></button>)}</div>}</>}</label>
    <div className="dialog-grid"><label className="form-field"><span>Visit type *</span><select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })}><option>Assessment</option><option>Fitting</option><option>Delivery</option><option value="FollowUp">Follow-up</option><option value="GaitTraining">Gait training</option></select></label><label className="form-field"><span>Date *</span><input required type="date" value={form.appointmentDate} onChange={(e) => setForm({ ...form, appointmentDate: e.target.value })} /></label><label className="form-field"><span>Time</span><input type="time" value={form.appointmentTime} onChange={(e) => setForm({ ...form, appointmentTime: e.target.value })} /></label><label className="form-field"><span>Handled by *</span><input required list="appointment-assignees" value={form.assignedClinicianEntry} onChange={(e) => { const assignee = options.data?.assignees.find((item) => item.displayName === e.target.value); setForm({ ...form, assignedClinicianEntry: e.target.value, assignedClinicianUserId: assignee?.userId }) }} /><datalist id="appointment-assignees">{options.data?.assignees.map((item) => <option key={item.userId} value={item.displayName} />)}</datalist></label><label className="form-field wide"><span>Notes</span><textarea rows={3} value={form.notes ?? ''} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></label></div>
    <DialogActions close={close} submitting={submitting} label="Create appointment" disabled={!form.patientId || options.loading} />
  </form></Dialog>
}

export function AppointmentActionDialog({ value, close, saved }: { value: { kind: 'cancel' | 'reschedule'; appointment: Appointment }; close: () => void; saved: (message: string) => void }) {
  const rescheduling = value.kind === 'reschedule'
  const [date, setDate] = useState(value.appointment.appointmentDate)
  const [time, setTime] = useState(value.appointment.appointmentTime?.slice(0,5) ?? '')
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const submit = async (event: FormEvent) => {
    event.preventDefault(); setSubmitting(true); setError('')
    try {
      if (rescheduling) await api.rescheduleAppointment(value.appointment.id, date, time || undefined, reason)
      else await api.cancelAppointment(value.appointment.id, reason)
      saved(`${value.appointment.patientName}'s appointment was ${rescheduling ? 'rescheduled' : 'cancelled'}.`)
    } catch (failure) { setError(failure instanceof Error ? failure.message : 'The appointment could not be updated.') }
    finally { setSubmitting(false) }
  }
  return <Dialog title={`${rescheduling ? 'Reschedule' : 'Cancel'} appointment`} description={`${value.appointment.patientName} · ${value.appointment.patientNumber}`} close={close}><form onSubmit={(event) => void submit(event)}>{error && <div className="inline-error">{error}</div>}{rescheduling && <div className="dialog-grid"><label className="form-field"><span>New date *</span><input required type="date" value={date} onChange={(e) => setDate(e.target.value)} /></label><label className="form-field"><span>New time</span><input type="time" value={time} onChange={(e) => setTime(e.target.value)} /></label></div>}<label className="form-field wide"><span>{rescheduling ? 'Reason for rescheduling' : 'Cancellation reason'} *</span><textarea required maxLength={500} rows={4} value={reason} onChange={(e) => setReason(e.target.value)} /></label><DialogActions close={close} submitting={submitting} label={rescheduling ? 'Save new schedule' : 'Cancel appointment'} danger={!rescheduling} /></form></Dialog>
}

function Dialog({ title, description, close, children }: { title: string; description: string; close: () => void; children: ReactNode }) { return <div className="dialog-backdrop" onMouseDown={(e) => { if (e.target === e.currentTarget) close() }}><section className="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title"><header><div><h2 id="dialog-title">{title}</h2><p>{description}</p></div><button className="icon-button" onClick={close} aria-label="Close"><X /></button></header><div className="dialog-body">{children}</div></section></div> }
function DialogActions({ close, submitting, label, disabled, danger }: { close: () => void; submitting: boolean; label: string; disabled?: boolean; danger?: boolean }) { return <div className="dialog-actions"><button type="button" className="button secondary" onClick={close}>Close</button><button disabled={submitting || disabled} className={`button ${danger ? 'danger-button' : 'primary'}`} type="submit">{submitting ? 'Saving…' : label}</button></div> }
