import { useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { ArrowLeft, CalendarPlus, ClipboardCheck, ClipboardList, ClipboardPlus, FileText, FolderOpen, History, IdCard, MapPin, PackageCheck, Pencil, Phone, Printer, Ruler, ShieldAlert, Stethoscope, User } from 'lucide-react'
import { api } from '../api/client'
import type { Episode, PatientDetail, PatientSummary } from '../api/types'
import { useAuth } from '../auth/AuthProvider'
import { CreateAppointmentDialog } from '../components/AppointmentDialogs'
import { ClinicalRecordDialog, type ClinicalKind } from '../components/ClinicalRecordDialog'
import { DocumentPanel } from '../components/DocumentPanel'
import { EpisodeDialog } from '../components/EpisodeDialog'
import { PatientAvatar } from '../components/PatientAvatar'
import { PatientHistory } from '../components/PatientHistory'
import { Badge, Card, EmptyState, ErrorPanel, Skeleton } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { access } from '../lib/access'
import { age, errorMessage, formatDate, humanize, saveFile } from '../lib/format'
import { useToast } from '../lib/toast'

type Tab = 'overview' | 'records' | 'history' | 'documents' | 'print'

export function PatientDetailPage() {
  const { id = '' } = useParams()
  const { roles } = useAuth()
  const can = access(roles)
  const notify = useToast()
  const [params, setParams] = useSearchParams()
  const tab = (params.get('tab') as Tab) || 'overview'
  const [episodeDialog, setEpisodeDialog] = useState<Episode | null | undefined>(params.get('newRecord') === '1' && can.clinical ? null : undefined)
  const [clinical, setClinical] = useState<{ kind: ClinicalKind; episode: Episode }>()
  const [booking, setBooking] = useState(false)
  const [photo, setPhoto] = useState<string>()
  const [refreshKey, setRefreshKey] = useState(0)
  const patient = useApi(() => api.patient(id), `patient-${id}`)
  const records = useApi(() => api.episodes(id), `episodes-${id}-${refreshKey}`, can.clinical)
  const setTab = (value: Tab) => setParams(value === 'overview' ? {} : { tab: value }, { replace: true })

  if (patient.loading && !patient.data) return <div className="card"><Skeleton rows={6} tall /></div>
  if (patient.error || !patient.data) return <div className="card"><ErrorPanel message={patient.error || 'Patient not found.'} retry={patient.refresh} /></div>
  const value = patient.data
  const summary: PatientSummary = { ...value }
  const tabs: { key: Tab; label: string; icon: typeof User; count?: number; show: boolean }[] = [
    { key: 'overview', label: 'Overview', icon: User, show: true },
    { key: 'records', label: 'Clinical records', icon: FolderOpen, count: records.data?.length, show: can.clinical },
    { key: 'history', label: 'Clinical history', icon: History, show: can.clinical },
    { key: 'documents', label: 'Documents', icon: FileText, show: true },
    { key: 'print', label: 'Print forms', icon: Printer, show: true },
  ]
  const print = async () => { try { saveFile(await api.printRegistration(id)) } catch (error) { notify(errorMessage(error, 'The form could not be generated.'), 'error') } }
  const afterSave = (message: string) => { setEpisodeDialog(undefined); setClinical(undefined); notify(message); setRefreshKey((key) => key + 1); if (params.get('newRecord')) setParams({ tab: 'records' }, { replace: true }) }

  return <>
    <Link to="/patients" className="back-link"><ArrowLeft size={16} /> Patient folder</Link>
    <section className="card" style={{ marginBottom: 'var(--gap)' }}>
      <div className="profile-card">
        <PatientAvatar patientId={value.id} name={value.fullName} hasPhoto={value.hasPhoto} size="lg" onOpen={setPhoto} />
        <div className="profile-main">
          <div className="profile-title"><span className="pno">{value.patientNumber}</span><Badge value={value.category} plain tone={value.category === 'Foreign' ? 'tone-violet' : 'tone-gray'} /></div>
          <h1>{value.fullName}</h1>
          <div className="profile-meta">
            <span><User />{value.nameWithInitials} · {age(value.dateOfBirth)} y · {value.sex}</span>
            <span><IdCard />{humanize(value.identificationType)}{value.identificationNumber ? ` ${value.identificationNumber}` : ''}</span>
            <span><MapPin />{value.centerName}</span>
            {value.contacts[0] && <span><Phone />{value.contacts[0].telephoneNumber}</span>}
          </div>
        </div>
        <div className="btn-group">
          {can.registerPatients && <Link className="btn btn-secondary" to={`/patients/${id}/edit`}><Pencil /> Edit details</Link>}
          <button className="btn btn-secondary" onClick={() => setBooking(true)}><CalendarPlus /> Appointment</button>
          {can.clinical && <button className="btn btn-primary" onClick={() => setEpisodeDialog(null)}><ClipboardPlus /> New record</button>}
        </div>
      </div>
    </section>

    <nav className="tabs" role="tablist">{tabs.filter((item) => item.show).map(({ key, label, icon: Icon, count }) => <button key={key} role="tab" className="tab" aria-selected={tab === key} onClick={() => setTab(key)}><Icon />{label}{count !== undefined && <span className="count">{count}</span>}</button>)}</nav>

    {tab === 'overview' && <Overview value={value} />}

    {tab === 'records' && can.clinical && <Card bodyless title="Clinical records" description="Each record is one prosthetic or orthotic service case under this patient number." actions={<button className="btn btn-primary btn-sm" onClick={() => setEpisodeDialog(null)}><ClipboardPlus /> New record</button>}>
      {records.error ? <ErrorPanel message={records.error} retry={records.refresh} /> : records.loading ? <Skeleton rows={3} tall /> : !records.data?.length
        ? <EmptyState icon={<FolderOpen />} title="No clinical records yet" description="Create a record to begin the assessment, fitting and delivery pathway." action={<button className="btn btn-primary btn-sm" onClick={() => setEpisodeDialog(null)}><ClipboardPlus /> Create first record</button>} />
        : records.data.map((record) => <article key={record.id} className="record-card">
          <div>
            <div className="record-card-title"><strong>Record of {formatDate(record.recordDate)}</strong><Badge value={record.status} />{record.isRestricted && <span className="restricted-tag"><ShieldAlert /> Restricted</span>}</div>
            <p>{record.centerName}{record.remarks ? ` · ${record.remarks}` : ''}</p>
            <div className="record-stats"><span><Stethoscope />{record.assessmentCount} assessments</span><span><Ruler />{record.fittingCount} fittings</span><span><PackageCheck />{record.deliveryCount} deliveries</span><span><ClipboardCheck />{record.followUpCount} follow-ups</span><span><FileText />{record.documentCount} files</span></div>
          </div>
          <div className="btn-group">
            {record.status === 'Active' && <>
              <button className="btn btn-ghost btn-sm" onClick={() => setClinical({ kind: 'fitting', episode: record })}><Ruler /> Fitting</button>
              <button className="btn btn-ghost btn-sm" onClick={() => setClinical({ kind: 'delivery', episode: record })}><PackageCheck /> Delivery</button>
              <button className="btn btn-ghost btn-sm" onClick={() => setClinical({ kind: 'followUp', episode: record })}><ClipboardCheck /> Follow-up</button>
            </>}
            <button className="btn btn-ghost btn-sm" onClick={() => setEpisodeDialog(record)}><Pencil /> Edit</button>
            <Link className="btn btn-secondary btn-sm" to={`/records/${record.id}`}><ClipboardList /> Open record</Link>
          </div>
        </article>)}
    </Card>}

    {tab === 'history' && can.clinical && <PatientHistory episodes={records.data ?? []} loading={records.loading} refreshKey={refreshKey} />}
    {tab === 'documents' && <DocumentPanel scope="patient" ownerId={value.id} changed={patient.refresh} />}
    {tab === 'print' && <Card title="Printable forms" description="Generated from the data held in POMS. Assessment, prescription, delivery and follow-up forms are printed from each clinical record.">
      <div className="print-grid">
        <button className="print-item" onClick={() => void print()}><span className="file-icon"><Printer /></span><div><strong>Patient registration form</strong><span>Exceed Lanka Form 101 layout · PDF</span></div></button>
        {can.clinical && <button className="print-item" onClick={() => setTab('history')}><span className="file-icon"><History /></span><div><strong>Clinical forms</strong><span>Assessment, prescription, delivery note, follow-up note</span></div></button>}
      </div>
    </Card>}

    {episodeDialog !== undefined && <EpisodeDialog patientId={value.id} patientName={value.fullName} centerId={value.centerId} episode={episodeDialog ?? undefined} close={() => { setEpisodeDialog(undefined); if (params.get('newRecord')) setParams({}, { replace: true }) }} saved={afterSave} />}
    {clinical && <ClinicalRecordDialog kind={clinical.kind} episodeId={clinical.episode.id} patientName={`${value.fullName} · ${value.patientNumber}`} close={() => setClinical(undefined)} saved={afterSave} />}
    {booking && <CreateAppointmentDialog patient={summary} close={() => setBooking(false)} saved={(item) => { setBooking(false); notify(`Appointment booked for ${formatDate(item.appointmentDate)}.`) }} />}
    {photo && <button className="lightbox" onClick={() => setPhoto(undefined)} autoFocus aria-label="Close photo"><img src={photo} alt={value.fullName} /></button>}
  </>
}

function Overview({ value }: { value: PatientDetail }) {
  const item = (label: string, content?: string | null, wide = false) => <div className={wide ? 'span-all' : ''}><dt>{label}</dt><dd className={content ? '' : 'empty-value'}>{content || 'Not recorded'}</dd></div>
  return <div className="grid grid-2">
    <Card title="Personal & identification">
      <dl className="dl">
        {item('Full name', value.fullName, true)}
        {item('Name with initials', value.nameWithInitials)}
        {item('Date of birth', `${formatDate(value.dateOfBirth)} (${age(value.dateOfBirth)} years)`)}
        {item('Gender', value.sex)}
        {item('Category', `${value.category} patient`)}
        {item('Identification', `${humanize(value.identificationType)}${value.identificationNumber ? ` · ${value.identificationNumber}` : ''}`)}
        {item('Nationality', value.nationality)}
        {item('Employment', value.employment)}
      </dl>
    </Card>
    <Card title="Contact & address">
      <dl className="dl">
        {item('Address', [value.address1, value.address2].filter(Boolean).join(', '), true)}
        {item('City', value.city || value.cityOther)}
        {item('District', value.district)}
        {item('Province', value.province)}
        {item('Email', value.email)}
      </dl>
      <div className="contact-list" style={{ marginTop: 16 }}>
        {value.contacts.length ? value.contacts.map((contact) => <div key={contact.id} className="contact-item"><span className="file-icon"><Phone /></span><div><strong>{contact.telephoneNumber}</strong><span>{contact.dateConfirmed ? `Confirmed ${formatDate(contact.dateConfirmed)}` : 'Not confirmed'}{contact.personChecked ? ` · by ${contact.personChecked}` : ''}</span></div></div>) : <p className="muted">No telephone numbers recorded.</p>}
      </div>
    </Card>
    <Card title="Guardian / carer">
      <dl className="dl">
        {item('Name', value.guardianName)}
        {item('Relationship', value.guardianRelationship)}
        {item('Mobile', value.guardianMobile)}
        {item('Phone', value.guardianPhone)}
        {item('Address', value.guardianAddress, true)}
      </dl>
    </Card>
    <Card title="Registration">
      <dl className="dl">
        {item('Patient number', value.patientNumber)}
        {item('Location', value.centerName)}
        {item('Date of registration', formatDate(value.registrationDate))}
        {item('Processed by', value.registrationProcessedBy)}
        {item('Handled by', value.assignedClinicianName)}
        {item('Referral source', value.referralSource ? `${value.referralSource}${value.referralSourceOther ? ` — ${value.referralSourceOther}` : ''}` : undefined)}
        {item('Referred by', [value.referralPersonName, value.referralPersonContactNumber].filter(Boolean).join(' · '))}
        {item('Travel time / distance', value.travelTimeDistance)}
        {item('Remarks', value.remarks, true)}
      </dl>
    </Card>
  </div>
}
