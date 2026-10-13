import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Activity, CalendarCheck, CalendarPlus, ClipboardList, ClipboardPlus, FileUp, FolderOpen, PackageCheck, Ruler, Stethoscope, UserPlus, UserRoundCheck, Users } from 'lucide-react'
import { api } from '../api/client'
import type { ActivityItem } from '../api/types'
import { useAuth } from '../auth/AuthProvider'
import { CreateAppointmentDialog } from '../components/AppointmentDialogs'
import { PatientPickerDialog } from '../components/PatientPickerDialog'
import { Badge, Card, EmptyState, ErrorPanel, Skeleton } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { access } from '../lib/access'
import { formatLongDate, formatTime, greeting, humanize, localDate, relativeTime } from '../lib/format'
import { useToast } from '../lib/toast'

const activityStyle: Record<string, { icon: typeof Activity; tone: string }> = {
  PatientRegistered: { icon: UserPlus, tone: 'tone-blue' },
  AssessmentCreated: { icon: Stethoscope, tone: 'tone-violet' },
  Fitting: { icon: Ruler, tone: 'tone-amber' },
  Delivery: { icon: PackageCheck, tone: 'tone-green' },
  FollowUp: { icon: ClipboardList, tone: 'tone-green' },
  DocumentUploaded: { icon: FileUp, tone: 'tone-gray' },
}

type QuickAction = 'record' | 'fitting' | 'delivery' | 'followUp'

export function DashboardPage() {
  const { user, roles } = useAuth()
  const can = access(roles)
  const navigate = useNavigate()
  const notify = useToast()
  const today = localDate()
  const [appointmentOpen, setAppointmentOpen] = useState(false)
  const [picker, setPicker] = useState<QuickAction>()
  const metrics = useApi(() => api.dashboardMetrics(), 'dashboard-metrics')
  const activity = useApi(() => api.dashboardActivity(), 'dashboard-activity')
  const schedule = useApi(() => api.appointments({ dateFrom: today, dateTo: today, status: 'Scheduled', pageSize: 50 }), `dashboard-schedule-${today}`)
  // PRD 5.3 and 14.3: deliveries and follow-ups are never shown as pending items.
  const pending = schedule.data?.items.filter((item) => item.type !== 'Delivery' && item.type !== 'FollowUp') ?? []
  const firstName = user?.name.split(/[\s@]/)[0] ?? ''
  const m = metrics.data

  const stats = [
    { label: 'Total patients', value: m?.totalPatients, foot: 'All registered patients', icon: Users, tone: 'tone-blue', to: can.readPatients ? '/patients' : undefined },
    { label: 'Active records', value: m?.activeRecords, foot: 'Active treatment episodes', icon: FolderOpen, tone: 'tone-violet', to: can.readPatients ? '/records?status=Active' : undefined },
    { label: 'Active patients', value: m?.activePatients, foot: 'Patients with an active record', icon: UserRoundCheck, tone: 'tone-green', to: can.readPatients ? '/records?status=Active' : undefined },
    { label: "Today's activities", value: m?.todayActivities, foot: m ? `${m.todayBreakdown.assessments} assessments · ${m.todayBreakdown.fittings} fittings · ${m.todayBreakdown.deliveries} deliveries` : 'Assessments, fittings and deliveries', icon: Activity, tone: 'tone-amber', to: '/appointments' },
  ]

  const quickActions = [
    { label: 'Register new patient', hint: 'Local or foreign', icon: UserPlus, tone: 'tone-blue', show: can.registerPatients, run: () => navigate('/patients/new') },
    { label: 'New record / assessment', hint: 'Under an existing PNO', icon: ClipboardPlus, tone: 'tone-violet', show: can.clinical, run: () => setPicker('record') },
    { label: 'New appointment', hint: 'Assessment, fitting, delivery', icon: CalendarPlus, tone: 'tone-blue', show: can.readPatients, run: () => setAppointmentOpen(true) },
    { label: 'Add fitting', hint: 'Record a fitting date', icon: Ruler, tone: 'tone-amber', show: can.clinical, run: () => setPicker('fitting') },
    { label: 'Add delivery', hint: 'Record a delivery', icon: PackageCheck, tone: 'tone-green', show: can.clinical, run: () => setPicker('delivery') },
    { label: 'Add follow-up', hint: 'Record a follow-up', icon: ClipboardList, tone: 'tone-green', show: can.clinical, run: () => setPicker('followUp') },
  ].filter((action) => action.show)

  return <>
    <div className="welcome">
      <div>
        <p className="eyebrow">{formatLongDate(today)}</p>
        <h1>Exceed Lanka Prosthetic &amp; Orthotic Data Management System</h1>
        <p>{greeting()}{firstName ? `, ${firstName}` : ''}. Here is today's clinical activity across all centres.</p>
      </div>
    </div>

    {metrics.error && <ErrorPanel message={metrics.error} retry={metrics.refresh} />}
    <section className="stats" aria-label="Summary">
      {stats.map(({ label, value, foot, icon: Icon, tone, to }) => {
        const body = <><div className="stat-top"><span className="stat-label">{label}</span><span className={`stat-icon ${tone}`}><Icon /></span></div><div className="stat-value">{metrics.loading ? '—' : value ?? '—'}</div><div className="stat-foot">{foot}</div></>
        return to ? <Link key={label} to={to} className="stat">{body}</Link> : <article key={label} className="stat">{body}</article>
      })}
    </section>

    <section className="quick-actions" aria-label="Quick actions">
      {quickActions.map(({ label, hint, icon: Icon, tone, run }) => <button key={label} className="quick-action" onClick={run}><span className={`stat-icon ${tone}`}><Icon /></span><span>{label}<small style={{ display: 'block', marginTop: 2 }}>{hint}</small></span></button>)}
    </section>

    <div className="dashboard-grid">
      <Card bodyless title="Recent activities" description="Latest work recorded under patient numbers" actions={can.readPatients && <Link className="btn btn-ghost btn-sm" to="/records">View records</Link>}>
        {activity.error ? <ErrorPanel message={activity.error} retry={activity.refresh} /> : activity.loading ? <Skeleton rows={6} tall /> : !activity.data?.length
          ? <EmptyState icon={<Activity />} title="No activity yet" description="Registrations, assessments, fittings, deliveries and uploads will appear here." />
          : <div className="list">{activity.data.map((item, index) => <ActivityRow key={`${item.type}-${item.occurredAt}-${index}`} item={item} linkable={can.readPatients} clinical={can.clinical} />)}</div>}
      </Card>

      <Card bodyless title="Today's schedule" description="Pending assessments and fittings" actions={<Link className="btn btn-ghost btn-sm" to="/appointments">Open calendar</Link>}>
        {schedule.error ? <ErrorPanel message={schedule.error} retry={schedule.refresh} /> : schedule.loading ? <Skeleton rows={4} tall /> : !pending.length
          ? <EmptyState icon={<CalendarCheck />} title="Nothing pending today" description="Scheduled assessments and fittings for today will be listed here." action={can.readPatients ? <button className="btn btn-secondary btn-sm" onClick={() => setAppointmentOpen(true)}><CalendarPlus /> New appointment</button> : undefined} />
          : <div className="list">{pending.map((item) => <div key={item.id} className="list-row">
              <span className="time-pill">{formatTime(item.appointmentTime) || 'TBC'}</span>
              <div className="grow"><strong>{item.patientName}</strong><span className="sub">{item.patientNumber} · {item.assignedClinicianName ?? 'Unassigned'}</span></div>
              <Badge value={humanize(item.type)} plain tone={item.type === 'Assessment' ? 'tone-violet' : 'tone-amber'} />
            </div>)}</div>}
      </Card>
    </div>

    {appointmentOpen && <CreateAppointmentDialog close={() => setAppointmentOpen(false)} saved={(item) => { setAppointmentOpen(false); notify(`Appointment booked for ${item.patientName}.`); schedule.refresh(); metrics.refresh() }} />}
    {picker && <PatientPickerDialog action={picker} close={() => setPicker(undefined)} />}
  </>
}

function ActivityRow({ item, linkable, clinical }: { item: ActivityItem; linkable: boolean; clinical: boolean }) {
  const style = activityStyle[item.type] ?? activityStyle.DocumentUploaded
  const Icon = style.icon
  const content = <>
    <span className={`activity-icon ${style.tone}`}><Icon /></span>
    <div className="grow"><strong>{item.description}</strong><span className="sub">{item.patientNumber} · {item.patientName} · {item.location}</span></div>
    <div className="activity-meta"><div>{relativeTime(item.occurredAt)}</div>{item.user && <div>{item.user.split('@')[0]}</div>}</div>
  </>
  const target = item.episodeId && clinical ? `/records/${item.episodeId}` : `/patients/${item.patientId}`
  return linkable ? <Link className="list-row" to={target}>{content}</Link> : <div className="list-row">{content}</div>
}
