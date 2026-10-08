import { ArrowLeft, ClipboardCheck, PackageCheck, Ruler, ShieldAlert, Stethoscope } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Assessment, Delivery, Fitting, FollowUp } from '../api/types'
import { ClinicalRecordDialog, type ClinicalItem, type ClinicalKind } from '../components/ClinicalRecordDialog'
import { Badge, EmptyState, ErrorPanel, LoadingRows, PageHeader } from '../components/Ui'
import { useApi } from '../hooks/useApi'

type DialogState = { kind: ClinicalKind; item?: ClinicalItem }
export function ClinicalRecordsPage() {
  const { episodeId = '' } = useParams()
  const [dialog, setDialog] = useState<DialogState>()
  const [refreshKey, setRefreshKey] = useState(0)
  const [notice, setNotice] = useState('')
  const episode = useApi(() => api.episode(episodeId), `episode-${episodeId}`)
  const records = useApi(() => api.clinicalRecords(episodeId), `clinical-${episodeId}-${refreshKey}`)
  const saved = (message: string) => { setDialog(undefined); setNotice(message); setRefreshKey((value) => value + 1) }
  if (episode.loading) return <LoadingRows />
  if (episode.error || !episode.data) return <ErrorPanel message={episode.error || 'Patient record not found.'} retry={episode.refresh} />
  const value = episode.data
  return <>
    <Link to={`/patients/${value.patientId}`} className="back-link"><ArrowLeft size={16} /> {value.patientName}</Link>
    <PageHeader eyebrow={`Patient record · ${new Date(value.recordDate).toLocaleDateString('en-GB')}`} title="Clinical record" description={`${value.patientNumber} · ${value.centerName}`} action={<Badge value={value.status} />} />
    {notice && <div className="notice success" role="status">{notice}</div>}
    <div className="clinical-actions"><button className="button primary" onClick={() => setDialog({ kind: 'assessment' })}><Stethoscope size={17} /> Assessment</button><button className="button secondary" onClick={() => setDialog({ kind: 'fitting' })}><Ruler size={17} /> Fitting</button><button className="button secondary" onClick={() => setDialog({ kind: 'delivery' })}><PackageCheck size={17} /> Delivery</button><button className="button secondary" onClick={() => setDialog({ kind: 'followUp' })}><ClipboardCheck size={17} /> Follow-up</button></div>
    {records.error ? <ErrorPanel message={records.error} retry={records.refresh} /> : records.loading ? <LoadingRows /> : records.data && <div className="clinical-sections"><RecordSection title="Assessments" empty="No assessments recorded." items={records.data.assessments} kind="assessment" open={(item) => setDialog({ kind: 'assessment', item })} render={(item: Assessment) => <><strong>{item.assessmentType} · {item.limbCategory.replace(/([a-z])([A-Z])/g, '$1 $2')}</strong><span>{item.mainProblemType} · {item.side}</span><small>{item.prescriptions.map((row) => `${row.side}: ${row.label}`).join(' · ')}</small></>} /><RecordSection title="Fittings" empty="No fittings recorded." items={records.data.fittings} kind="fitting" open={(item) => setDialog({ kind: 'fitting', item })} render={(item: Fitting) => <><strong>Fitting · {new Date(item.fittingDate).toLocaleDateString('en-GB')}</strong><span>{item.notes || 'No notes recorded.'}</span></>} /><RecordSection title="Deliveries" empty="No deliveries recorded." items={records.data.deliveries} kind="delivery" open={(item) => setDialog({ kind: 'delivery', item })} render={(item: Delivery) => <><strong>Delivery · {new Date(item.deliveryDate).toLocaleDateString('en-GB')}</strong><span>{item.deviceName || 'No device selected'}{item.deliveryTime ? ` · ${item.deliveryTime.slice(0, 5)}` : ''}</span><small>{item.notes}</small></>} /><RecordSection title="Follow-ups" empty="No follow-ups recorded." items={records.data.followUps} kind="followUp" open={(item) => setDialog({ kind: 'followUp', item })} render={(item: FollowUp) => <><strong>Follow-up · {new Date(item.followUpDate).toLocaleDateString('en-GB')}</strong><span>{item.startTime?.slice(0, 5)}–{item.endTime?.slice(0, 5)}</span><small>{item.notes}</small></>} /></div>}
    {dialog && <ClinicalRecordDialog kind={dialog.kind} episodeId={value.id} patientName={value.patientName} item={dialog.item} close={() => setDialog(undefined)} saved={saved} />}
  </>
}

function RecordSection<T extends ClinicalItem>({ title, empty, items, open, render }: { title: string; empty: string; items: T[]; kind: ClinicalKind; open: (item: T) => void; render: (item: T) => React.ReactNode }) { return <section className="panel clinical-section"><header><h2>{title}</h2><span>{items.length}</span></header>{items.length === 0 ? <EmptyState><p>{empty}</p></EmptyState> : <div>{items.map((item) => <article key={item.id}><div className="clinical-entry">{render(item)}{item.isRestricted && <em><ShieldAlert size={13} /> Restricted</em>}</div><button className="button secondary compact" onClick={() => open(item)}>Edit</button></article>)}</div>}</section> }
