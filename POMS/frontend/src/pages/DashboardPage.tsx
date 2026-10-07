import { CalendarCheck, Clock3, UserRoundPlus, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { Badge, ErrorPanel, LoadingRows, PageHeader } from '../components/Ui'
import { useApi } from '../hooks/useApi'

const today = new Date().toISOString().slice(0, 10)
export function DashboardPage() {
  const patients = useApi(() => api.patients({ pageSize: 5 }), 'dashboard-patients')
  const appointments = useApi(() => api.appointments({ dateFrom: today, dateTo: today, pageSize: 5 }), `dashboard-appointments-${today}`)
  const retry = () => { patients.refresh(); appointments.refresh() }
  return <>
    <PageHeader eyebrow="Wednesday, 7 October" title="Good morning" description="Here is what needs attention across your clinical services today." action={<Link className="button primary" to="/patients/new"><UserRoundPlus size={18} /> Register patient</Link>} />
    <section className="metrics" aria-label="Daily summary">
      <article><span className="metric-icon blue"><CalendarCheck /></span><div><span>Today's appointments</span><strong>{appointments.data?.totalCount ?? '—'}</strong><small>Across all centres</small></div></article>
      <article><span className="metric-icon teal"><Clock3 /></span><div><span>Awaiting consultation</span><strong>{appointments.data?.items.filter((x) => x.status !== 'Completed').length ?? '—'}</strong><small>Requires attention</small></div></article>
      <article><span className="metric-icon violet"><Users /></span><div><span>Registered patients</span><strong>{patients.data?.totalCount ?? '—'}</strong><small>Current search scope</small></div></article>
    </section>
    {(patients.error || appointments.error) && <ErrorPanel message={patients.error || appointments.error} retry={retry} />}
    <div className="dashboard-grid">
      <section className="panel"><div className="panel-heading"><div><h2>Today's appointments</h2><p>Upcoming clinical activity</p></div><Link to="/appointments">View schedule</Link></div>
        {appointments.loading ? <LoadingRows /> : <div className="appointment-list">{appointments.data?.items.map((item) => <article key={item.id}><time>{item.appointmentTime?.slice(0,5) ?? 'TBC'}</time><div className="person-avatar">{item.patientName.split(' ').map((x) => x[0]).slice(0,2).join('')}</div><div><strong>{item.patientName}</strong><span>{item.patientNumber} · {item.type.replace(/([a-z])([A-Z])/g, '$1 $2')}</span></div><Badge value={item.status} /></article>)}</div>}
      </section>
      <section className="panel"><div className="panel-heading"><div><h2>Recently registered</h2><p>Latest patient records</p></div><Link to="/patients">View patients</Link></div>
        {patients.loading ? <LoadingRows /> : <div className="recent-list">{patients.data?.items.map((patient) => <article key={patient.id}><div className="person-avatar soft">{patient.nameWithInitials[0]}</div><div><strong>{patient.fullName}</strong><span>{patient.patientNumber} · {patient.centerName}</span></div><time>{new Date(patient.registrationDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })}</time></article>)}</div>}
      </section>
    </div>
  </>
}
