import { useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Check, ClipboardCheck, PackageCheck, Pencil, Plus, Ruler, ShieldAlert, Stethoscope } from 'lucide-react'
import { api } from '../api/client'
import type { EpisodeClinicalRecords } from '../api/types'
import { ClinicalRecordDialog, type ClinicalItem, type ClinicalKind } from '../components/ClinicalRecordDialog'
import { DocumentPanel } from '../components/DocumentPanel'
import { EpisodeDialog } from '../components/EpisodeDialog'
import { TimelineRow, toTimeline } from '../components/PatientHistory'
import { Badge, Card, EmptyState, ErrorPanel, PageHeader, Skeleton } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { formatDate } from '../lib/format'
import { useToast } from '../lib/toast'

type DialogState = { kind: ClinicalKind; item?: ClinicalItem }
const sections: { kind: ClinicalKind; key: keyof EpisodeClinicalRecords; title: string; icon: typeof Stethoscope; empty: string }[] = [
  { kind: 'assessment', key: 'assessments', title: 'Assessments & prescriptions', icon: Stethoscope, empty: 'No assessment recorded yet.' },
  { kind: 'fitting', key: 'fittings', title: 'Fittings', icon: Ruler, empty: 'No fitting dates recorded.' },
  { kind: 'delivery', key: 'deliveries', title: 'Deliveries', icon: PackageCheck, empty: 'No delivery recorded.' },
  { kind: 'followUp', key: 'followUps', title: 'Follow-ups', icon: ClipboardCheck, empty: 'No follow-ups recorded.' },
]
const kinds: ClinicalKind[] = ['assessment', 'fitting', 'delivery', 'followUp']

export function ClinicalRecordsPage() {
  const { episodeId = '' } = useParams()
  const notify = useToast()
  const [params, setParams] = useSearchParams()
  const requested = params.get('add') as ClinicalKind | null
  const [dialog, setDialog] = useState<DialogState | undefined>(requested && kinds.includes(requested) ? { kind: requested } : undefined)
  const [editingEpisode, setEditingEpisode] = useState(false)
  const [refreshKey, setRefreshKey] = useState(0)
  const episode = useApi(() => api.episode(episodeId), `episode-${episodeId}-${refreshKey}`)
  const records = useApi(() => api.clinicalRecords(episodeId), `clinical-${episodeId}-${refreshKey}`)
  const saved = (message: string) => { setDialog(undefined); setEditingEpisode(false); notify(message); setRefreshKey((value) => value + 1) }
  const closeDialog = () => { setDialog(undefined); if (requested) setParams({}, { replace: true }) }

  if (episode.loading && !episode.data) return <div className="card"><Skeleton rows={6} tall /></div>
  if (episode.error || !episode.data) return <div className="card"><ErrorPanel message={episode.error || 'Patient record not found.'} retry={episode.refresh} /></div>
  const value = episode.data
  const data = records.data
  const stages = [
    { label: 'Assessment', count: data?.assessments.length ?? value.assessmentCount, icon: Stethoscope },
    { label: 'Fitting', count: data?.fittings.length ?? value.fittingCount, icon: Ruler },
    { label: 'Delivery', count: data?.deliveries.length ?? value.deliveryCount, icon: PackageCheck },
    { label: 'Follow-up', count: data?.followUps.length ?? value.followUpCount, icon: ClipboardCheck },
  ]
  const reached = stages.reduce((last, stage, index) => stage.count > 0 ? index : last, -1)

  return <>
    <Link to={`/patients/${value.patientId}?tab=records`} className="back-link"><ArrowLeft size={16} /> {value.patientName}</Link>
    <PageHeader eyebrow={`${value.patientNumber} · ${value.centerName}`}
      title={<span style={{ display: 'inline-flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>Record of {formatDate(value.recordDate)} <Badge value={value.status} />{value.isRestricted && <span className="restricted-tag"><ShieldAlert /> Restricted</span>}</span>}
      description={value.remarks || `Clinical pathway for ${value.patientName}.`}
      actions={<><button className="btn btn-secondary" onClick={() => setEditingEpisode(true)}><Pencil /> Edit record</button><button className="btn btn-primary" onClick={() => setDialog({ kind: 'assessment' })}><Stethoscope /> New assessment</button></>} />

    <section className="card" style={{ marginBottom: 'var(--gap)' }} aria-label="Treatment stage">
      <div className="stage-track">{stages.map((stage, index) => { const Icon = stage.icon; const state = index < reached ? 'done' : index === reached ? 'current' : ''; return <div key={stage.label} className={`stage ${state}`}><span className="stage-dot">{index < reached ? <Check /> : <Icon />}</span><div><strong>{stage.label}</strong><small>{stage.count ? `${stage.count} recorded` : 'Pending'}</small></div></div> })}</div>
    </section>

    {records.error ? <div className="card"><ErrorPanel message={records.error} retry={records.refresh} /></div> : <div className="grid grid-2">
      {sections.map(({ kind, key, title, icon: Icon, empty }) => {
        const items = data ? toTimeline({ assessments: [], fittings: [], deliveries: [], followUps: [], [key]: data[key] }) : []
        return <Card key={kind} bodyless title={title} count={data?.[key].length} actions={<button className="btn btn-secondary btn-sm" onClick={() => setDialog({ kind })}><Plus /> Add</button>}>
          {records.loading ? <Skeleton rows={2} tall /> : !items.length ? <EmptyState compact icon={<Icon />} title={empty} /> : <div className="timeline">{items.map((entry) => <TimelineRow key={entry.item.id} entry={entry} onEdit={() => setDialog({ kind, item: entry.item })} />)}</div>}
        </Card>
      })}
    </div>}

    <div className="section-gap"><DocumentPanel scope="episode" ownerId={value.id} /></div>
    {dialog && <ClinicalRecordDialog kind={dialog.kind} episodeId={value.id} patientName={`${value.patientName} · ${value.patientNumber}`} item={dialog.item} close={closeDialog} saved={(message) => { saved(message); if (requested) setParams({}, { replace: true }) }} />}
    {editingEpisode && <EpisodeDialog patientId={value.patientId} patientName={value.patientName} centerId={value.centerId} episode={value} close={() => setEditingEpisode(false)} saved={saved} />}
  </>
}
