import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { CalendarDays, CalendarPlus, Check, ChevronLeft, ChevronRight, Clock3, X } from 'lucide-react'
import { api } from '../api/client'
import type { Appointment } from '../api/types'
import { AppointmentActionDialog, CreateAppointmentDialog } from '../components/AppointmentDialogs'
import { Avatar, Badge, Card, ConfirmModal, EmptyState, ErrorPanel, PageHeader, Segmented, Skeleton } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { addDays, errorMessage, formatDate, formatLongDate, formatTime, humanize, localDate } from '../lib/format'
import { useToast } from '../lib/toast'
import { useAuth } from '../auth/AuthProvider'
import { access } from '../lib/access'

type Action = { kind: 'cancel' | 'reschedule'; appointment: Appointment }
const typeTone: Record<string, string> = { Assessment: 'tone-violet', Fitting: 'tone-amber', Delivery: 'tone-green', FollowUp: 'tone-green', GaitTraining: 'tone-blue' }

export function AppointmentsPage() {
  const notify = useToast()
  const canBook = access(useAuth().roles).readPatients
  const [params, setParams] = useSearchParams()
  const [date, setDate] = useState(localDate())
  const [view, setView] = useState<'day' | 'week'>('day')
  const [status, setStatus] = useState('')
  const [type, setType] = useState('')
  const [action, setAction] = useState<Action>()
  const [completing, setCompleting] = useState<Appointment>()
  const creating = canBook && params.get('new') === '1'
  const setCreating = (open: boolean) => setParams(open ? { new: '1' } : {}, { replace: true })
  const dateTo = view === 'day' ? date : addDays(date, 6)
  const result = useApi(() => api.appointments({ dateFrom: date, dateTo, status, pageSize: 100 }), `appointments-${date}-${dateTo}-${status}`)
  const items = (result.data?.items ?? []).filter((item) => !type || item.type === type)
    .sort((a, b) => `${a.appointmentDate}${a.appointmentTime ?? '99'}`.localeCompare(`${b.appointmentDate}${b.appointmentTime ?? '99'}`))
  const today = localDate()

  const complete = async (item: Appointment) => {
    try { await api.completeAppointment(item.id); notify(`${item.patientName}'s ${humanize(item.type).toLowerCase()} was marked complete.`); result.refresh() }
    catch (error) { notify(errorMessage(error, 'The appointment could not be completed.'), 'error') }
    finally { setCompleting(undefined) }
  }

  return <>
    <PageHeader eyebrow="Schedule" title="Appointments" description="Assessment, fitting and delivery visits across all centres." actions={canBook && <button className="btn btn-primary" onClick={() => setCreating(true)}><CalendarPlus /> New appointment</button>} />
    <Card bodyless>
      <div className="card-toolbar">
        <div className="btn-group" style={{ gap: 4 }}>
          <button className="icon-btn" onClick={() => setDate(addDays(date, view === 'day' ? -1 : -7))} aria-label="Previous"><ChevronLeft /></button>
          <input className="input" type="date" value={date} onChange={(event) => event.target.value && setDate(event.target.value)} style={{ width: 160 }} aria-label="Date" />
          <button className="icon-btn" onClick={() => setDate(addDays(date, view === 'day' ? 1 : 7))} aria-label="Next"><ChevronRight /></button>
          <button className="btn btn-secondary btn-sm" disabled={date === today} onClick={() => setDate(today)}>Today</button>
        </div>
        <Segmented label="Range" value={view} onChange={setView} options={[{ value: 'day', label: 'Day' }, { value: 'week', label: '7 days' }]} />
        <select className="select" style={{ width: 160 }} value={type} onChange={(event) => setType(event.target.value)} aria-label="Visit type"><option value="">All visit types</option><option value="Assessment">Assessment</option><option value="Fitting">Fitting</option><option value="Delivery">Delivery</option></select>
        <select className="select" style={{ width: 150 }} value={status} onChange={(event) => setStatus(event.target.value)} aria-label="Status"><option value="">All statuses</option><option>Scheduled</option><option>Completed</option><option>Cancelled</option></select>
      </div>
      <div className="card-header" style={{ background: 'transparent' }}>
        <div><h2>{view === 'day' ? formatLongDate(date) : `${formatDate(date)} – ${formatDate(dateTo)}`}</h2><p>{result.loading ? 'Loading…' : `${items.length} appointment${items.length === 1 ? '' : 's'}`}</p></div>
      </div>
      {result.error ? <ErrorPanel message={result.error} retry={result.refresh} /> : result.loading ? <Skeleton rows={5} tall /> : !items.length
        ? <EmptyState icon={<CalendarDays />} title="No appointments" description="Nothing is booked for this period with the selected filters." action={canBook ? <button className="btn btn-secondary btn-sm" onClick={() => setCreating(true)}><CalendarPlus /> Book an appointment</button> : undefined} />
        : <div className="table-wrap"><table className="table responsive">
          <thead><tr>{view === 'week' && <th>Date</th>}<th>Time</th><th>Patient</th><th>Visit</th><th>Handled by</th><th>Status</th><th className="actions"><span className="sr-only">Actions</span></th></tr></thead>
          <tbody>{items.map((item) => <tr key={item.id}>
            {view === 'week' && <td data-label="Date" className="tabular">{formatDate(item.appointmentDate)}</td>}
            <td data-label="Time"><strong className="tabular">{formatTime(item.appointmentTime) || 'TBC'}</strong></td>
            <td className="primary-cell"><div className="cell-person"><Avatar name={item.patientName} /><div><strong>{item.patientName}</strong><span>{item.patientNumber}</span></div></div></td>
            <td data-label="Visit"><Badge value={humanize(item.type)} plain tone={typeTone[item.type]} /></td>
            <td data-label="Handled by">{item.assignedClinicianName ?? <span className="muted">Unassigned</span>}</td>
            <td data-label="Status"><Badge value={item.status} /></td>
            <td className="actions">{item.status === 'Scheduled' && <div>
              <button className="icon-btn sm" title="Mark complete" aria-label="Mark complete" onClick={() => setCompleting(item)}><Check /></button>
              <button className="icon-btn sm" title="Reschedule" aria-label="Reschedule" onClick={() => setAction({ kind: 'reschedule', appointment: item })}><Clock3 /></button>
              <button className="icon-btn sm danger" title="Cancel" aria-label="Cancel appointment" onClick={() => setAction({ kind: 'cancel', appointment: item })}><X /></button>
            </div>}</td>
          </tr>)}</tbody>
        </table></div>}
    </Card>
    {creating && <CreateAppointmentDialog initialDate={date >= today ? date : today} close={() => setCreating(false)} saved={(item) => { setCreating(false); setView('day'); setDate(item.appointmentDate); notify(`Appointment booked for ${item.patientName}.`); result.refresh() }} />}
    {action && <AppointmentActionDialog value={action} close={() => setAction(undefined)} saved={(message) => { setAction(undefined); notify(message); result.refresh() }} />}
    {completing && <ConfirmModal danger={false} title="Mark appointment complete?" message={`${completing.patientName} · ${humanize(completing.type)} on ${formatDate(completing.appointmentDate)}.`} confirmLabel="Mark complete" onClose={() => setCompleting(undefined)} onConfirm={() => complete(completing)} />}
  </>
}
