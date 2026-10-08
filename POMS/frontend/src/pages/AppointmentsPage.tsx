import { CalendarDays, Check, Clock3, Plus, X } from 'lucide-react'
import { useState } from 'react'
import { api } from '../api/client'
import type { Appointment } from '../api/types'
import { Badge, EmptyState, ErrorPanel, LoadingRows, PageHeader } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { AppointmentActionDialog, CreateAppointmentDialog } from '../components/AppointmentDialogs'

const today = new Date().toISOString().slice(0, 10)
type Action = { kind: 'cancel' | 'reschedule'; appointment: Appointment }

export function AppointmentsPage() {
  const [date, setDate] = useState(today)
  const [status, setStatus] = useState('')
  const [creating, setCreating] = useState(false)
  const [action, setAction] = useState<Action>()
  const [notice, setNotice] = useState('')
  const result = useApi(() => api.appointments({ dateFrom: date, dateTo: date, status, pageSize: 50 }), `${date}-${status}`)

  const complete = async (item: Appointment) => {
    setNotice('')
    try { await api.completeAppointment(item.id); setNotice(`${item.patientName}'s appointment was completed.`); result.refresh() }
    catch (error) { setNotice(error instanceof Error ? error.message : 'The appointment could not be completed.') }
  }

  return <>
    <PageHeader eyebrow="Clinical schedule" title="Appointments" description="Review daily activity and keep patient visits moving." action={<button className="button primary" onClick={() => setCreating(true)}><Plus size={18} /> New appointment</button>} />
    {notice && <div className="notice-panel" role="status">{notice}</div>}
    <section className="panel list-panel">
      <div className="filters"><label><span>Date</span><input type="date" value={date} onChange={(e) => setDate(e.target.value)} /></label><label><span>Status</span><select value={status} onChange={(e) => setStatus(e.target.value)}><option value="">All statuses</option><option>Scheduled</option><option>Completed</option><option>Cancelled</option></select></label></div>
      {result.error ? <ErrorPanel message={result.error} retry={result.refresh} /> : result.loading ? <LoadingRows /> : !result.data?.items.length ? <EmptyState><CalendarDays size={34} /><h2>No appointments</h2><p>There are no appointments matching this date and status.</p></EmptyState> : <div className="table-wrap"><table><thead><tr><th>Time</th><th>Patient</th><th>Visit type</th><th>Clinician</th><th>Status</th><th>Actions</th></tr></thead><tbody>{result.data.items.map((item) => <tr key={item.id}><td data-label="Time"><strong>{item.appointmentTime?.slice(0,5) ?? 'TBC'}</strong></td><td data-label="Patient"><div className="table-person"><div className="person-avatar">{item.patientName.split(' ').map((part) => part[0]).slice(0,2).join('')}</div><div><strong>{item.patientName}</strong><span>{item.patientNumber}</span></div></div></td><td data-label="Visit type">{item.type.replace(/([a-z])([A-Z])/g, '$1 $2')}</td><td data-label="Clinician">{item.assignedClinicianName ?? 'Unassigned'}</td><td data-label="Status"><Badge value={item.status} /></td><td data-label="Actions">{item.status === 'Scheduled' && <div className="row-actions"><button title="Complete" onClick={() => void complete(item)}><Check /></button><button title="Reschedule" onClick={() => setAction({ kind: 'reschedule', appointment: item })}><Clock3 /></button><button title="Cancel" className="danger" onClick={() => setAction({ kind: 'cancel', appointment: item })}><X /></button></div>}</td></tr>)}</tbody></table></div>}
    </section>
    {creating && <CreateAppointmentDialog close={() => setCreating(false)} saved={(item) => { setCreating(false); setDate(item.appointmentDate); setNotice(`Appointment created for ${item.patientName}.`); result.refresh() }} />}
    {action && <AppointmentActionDialog value={action} close={() => setAction(undefined)} saved={(message) => { setAction(undefined); setNotice(message); result.refresh() }} />}
  </>
}
