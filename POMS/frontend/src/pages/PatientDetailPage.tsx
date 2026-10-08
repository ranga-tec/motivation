import { ArrowLeft, ClipboardPlus, FileText, Pencil, ShieldAlert } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Episode } from '../api/types'
import { EpisodeDialog } from '../components/EpisodeDialog'
import { Badge, EmptyState, ErrorPanel, LoadingRows, PageHeader } from '../components/Ui'
import { useApi } from '../hooks/useApi'

export function PatientDetailPage() {
  const { id = '' } = useParams()
  const [refreshKey, setRefreshKey] = useState(0)
  const [editing, setEditing] = useState<Episode | null | undefined>(undefined)
  const [notice, setNotice] = useState('')
  const patient = useApi(() => api.patient(id), `patient-${id}`)
  const records = useApi(() => api.episodes(id), `episodes-${id}-${refreshKey}`)
  const saved = (message: string) => { setEditing(undefined); setNotice(message); setRefreshKey((value) => value + 1) }
  if (patient.loading) return <LoadingRows />
  if (patient.error || !patient.data) return <ErrorPanel message={patient.error || 'Patient not found.'} retry={patient.refresh} />
  const value = patient.data
  return <>
    <Link to="/patients" className="back-link"><ArrowLeft size={16} /> Patients</Link>
    <PageHeader eyebrow={value.patientNumber} title={value.fullName} description={`${value.sex} · Born ${new Date(value.dateOfBirth).toLocaleDateString('en-GB')} · ${value.centerName}`} action={<button className="button primary" onClick={() => setEditing(null)}><ClipboardPlus size={18} /> New patient record</button>} />
    {notice && <div className="notice success" role="status">{notice}</div>}
    <section className="patient-summary-grid"><article className="panel summary-card"><h2>Patient information</h2><dl><div><dt>Name with initials</dt><dd>{value.nameWithInitials}</dd></div><div><dt>Identification</dt><dd>{value.identificationType} · {value.identificationNumber || 'N/A'}</dd></div><div><dt>Address</dt><dd>{[value.address1, value.address2, value.city || value.cityOther, value.district, value.province].filter(Boolean).join(', ')}</dd></div><div><dt>Assigned clinician</dt><dd>{value.assignedClinicianName || 'Not assigned'}</dd></div><div><dt>Contact</dt><dd>{value.contacts.map((contact) => contact.telephoneNumber).join(', ') || 'Not provided'}</dd></div></dl></article><article className="panel summary-card"><h2>Registration</h2><dl><div><dt>Centre</dt><dd>{value.centerName}</dd></div><div><dt>Category</dt><dd>{value.category}</dd></div><div><dt>Registered</dt><dd>{new Date(value.registrationDate).toLocaleDateString('en-GB')}</dd></div><div><dt>Email</dt><dd>{value.email || 'Not provided'}</dd></div></dl></article></section>
    <section className="panel list-panel"><div className="section-heading"><div><p className="eyebrow">Clinical workspace</p><h2>Patient records</h2></div></div>{records.error ? <ErrorPanel message={records.error} retry={records.refresh} /> : records.loading ? <LoadingRows /> : !records.data?.length ? <EmptyState><FileText size={34} /><h2>No patient records</h2><p>Create the first record to begin the clinical pathway.</p></EmptyState> : <div className="record-cards">{records.data.map((record) => <article key={record.id} className="record-card"><div><div className="record-card-title"><strong>{new Date(record.recordDate).toLocaleDateString('en-GB')}</strong><Badge value={record.status} />{record.isRestricted && <span className="restricted-label"><ShieldAlert size={14} /> Restricted</span>}</div><p>{record.centerName}{record.recordTime ? ` · ${record.recordTime.slice(0, 5)}` : ''}</p><span>{record.remarks || 'No remarks recorded.'}</span></div><div className="record-counts"><span>{record.assessmentCount} assessments</span><span>{record.fittingCount} fittings</span><span>{record.deliveryCount} deliveries</span><span>{record.followUpCount} follow-ups</span><span>{record.documentCount} documents</span></div><button className="button secondary compact" onClick={() => setEditing(record)}><Pencil size={15} /> Edit</button></article>)}</div>}</section>
    {editing !== undefined && <EpisodeDialog patientId={value.id} patientName={value.fullName} centerId={value.centerId} episode={editing ?? undefined} close={() => setEditing(undefined)} saved={saved} />}
  </>
}
