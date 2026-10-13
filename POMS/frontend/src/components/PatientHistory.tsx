/* oxlint-disable react/only-export-components -- timeline helpers are shared with the record page */
import { Link } from 'react-router-dom'
import { ClipboardCheck, History, PackageCheck, Printer, Ruler, ShieldAlert, Stethoscope } from 'lucide-react'
import { api } from '../api/client'
import type { Assessment, Delivery, Episode, Fitting, FollowUp } from '../api/types'
import { useApi } from '../hooks/useApi'
import { errorMessage, formatDate, formatTime, humanize, saveFile } from '../lib/format'
import { useToast } from '../lib/toast'
import { Card, EmptyState, ErrorPanel, Skeleton } from './Ui'

export type TimelineEntry =
  | { kind: 'assessment'; date: string; item: Assessment; episode?: Episode }
  | { kind: 'fitting'; date: string; item: Fitting; episode?: Episode }
  | { kind: 'delivery'; date: string; item: Delivery; episode?: Episode }
  | { kind: 'followUp'; date: string; item: FollowUp; episode?: Episode }

const meta = {
  assessment: { icon: Stethoscope, tone: 'tone-violet', label: 'Assessment' },
  fitting: { icon: Ruler, tone: 'tone-amber', label: 'Fitting' },
  delivery: { icon: PackageCheck, tone: 'tone-green', label: 'Delivery' },
  followUp: { icon: ClipboardCheck, tone: 'tone-blue', label: 'Follow-up' },
}

export function usePrint() {
  const notify = useToast()
  return async (request: Promise<{ blob: Blob; fileName: string }>) => {
    try { saveFile(await request) } catch (error) { notify(errorMessage(error, 'The document could not be generated.'), 'error') }
  }
}

/** One row of the clinical timeline with its print actions. */
export function TimelineRow({ entry, onEdit, showRecord }: { entry: TimelineEntry; onEdit?: () => void; showRecord?: boolean }) {
  const print = usePrint()
  const { icon: Icon, tone, label } = meta[entry.kind]
  let title = label
  let sub = ''
  let notes: string | undefined
  let prints: { label: string; run: () => void }[] = []
  if (entry.kind === 'assessment') {
    const item = entry.item
    title = `${item.assessmentType} assessment · ${humanize(item.limbCategory)}`
    sub = `${item.mainProblemType} · ${item.side} · ${item.causeReasonType}${item.causeReasonOther ? ` (${item.causeReasonOther})` : ''}`
    notes = item.additionalInformation
    prints = [{ label: 'Assessment', run: () => void print(api.printAssessment(item.id)) }, { label: 'Prescription', run: () => void print(api.printPrescription(item.id)) }]
  } else if (entry.kind === 'fitting') {
    notes = entry.item.notes
  } else if (entry.kind === 'delivery') {
    sub = [entry.item.deviceName, formatTime(entry.item.deliveryTime)].filter(Boolean).join(' · ')
    notes = entry.item.notes
    prints = [{ label: 'Delivery note', run: () => void print(api.printDelivery(entry.item.id)) }]
  } else {
    sub = entry.item.startTime ? `${formatTime(entry.item.startTime)}–${formatTime(entry.item.endTime)}` : ''
    notes = entry.item.notes
    prints = [{ label: 'Follow-up note', run: () => void print(api.printFollowUp(entry.item.id)) }]
  }
  return <article className="timeline-item">
    <div style={{ display: 'flex', gap: 12, minWidth: 0 }}>
      <span className={`activity-icon ${tone}`}><Icon /></span>
      <div style={{ minWidth: 0 }}>
        <div className="title"><span>{title}</span><span className="muted date" style={{ fontWeight: 500 }}>· {formatDate(entry.date)}</span>{entry.item.isRestricted && <span className="restricted-tag"><ShieldAlert /> Restricted</span>}</div>
        {sub && <div className="sub">{sub}</div>}
        {entry.kind === 'assessment' && entry.item.prescriptions.length > 0 && <div className="rx-list">{entry.item.prescriptions.map((row) => <span key={row.id} className="rx"><b>{row.side}</b>{row.label}{row.subType ? ` · ${row.subType}` : ''}{row.otherText ? ` · ${row.otherText}` : ''}</span>)}</div>}
        {notes && <p>{notes}</p>}
        {showRecord && entry.episode && <div className="sub" style={{ marginTop: 6 }}><Link to={`/records/${entry.episode.id}`}>Record of {formatDate(entry.episode.recordDate)} · {entry.episode.centerName}</Link></div>}
      </div>
    </div>
    <div className="btn-group">
      {prints.map((item) => <button key={item.label} className="btn btn-ghost btn-sm" onClick={item.run}><Printer /> {item.label}</button>)}
      {onEdit && <button className="btn btn-secondary btn-sm" onClick={onEdit}>Edit</button>}
    </div>
  </article>
}

export function toTimeline(records: { assessments: Assessment[]; fittings: Fitting[]; deliveries: Delivery[]; followUps: FollowUp[] }, episode?: Episode): TimelineEntry[] {
  return [
    ...records.assessments.map((item) => ({ kind: 'assessment' as const, date: item.assessedOn, item, episode })),
    ...records.fittings.map((item) => ({ kind: 'fitting' as const, date: item.fittingDate, item, episode })),
    ...records.deliveries.map((item) => ({ kind: 'delivery' as const, date: item.deliveryDate, item, episode })),
    ...records.followUps.map((item) => ({ kind: 'followUp' as const, date: item.followUpDate, item, episode })),
  ].sort((a, b) => b.date.localeCompare(a.date))
}

/** Combined assessment, prescription, fitting, delivery and follow-up history across every record of a patient. */
export function PatientHistory({ episodes, loading, refreshKey }: { episodes: Episode[]; loading: boolean; refreshKey: number }) {
  const ids = episodes.map((episode) => episode.id).join(',')
  const history = useApi(async () => (await Promise.all(episodes.map(async (episode) => toTimeline(await api.clinicalRecords(episode.id), episode)))).flat().sort((a, b) => b.date.localeCompare(a.date)), `history-${ids}-${refreshKey}`, !loading)
  return <Card bodyless title="Clinical history" description="Assessments, prescriptions, fittings, deliveries and follow-ups across all records.">
    {loading || history.loading ? <Skeleton rows={4} tall /> : history.error ? <ErrorPanel message={history.error} retry={history.refresh} /> : !history.data?.length
      ? <EmptyState icon={<History />} title="No clinical history" description="Entries appear here once assessments, fittings, deliveries or follow-ups are recorded." />
      : <div className="timeline">{history.data.map((entry) => <TimelineRow key={`${entry.kind}-${entry.item.id}`} entry={entry} showRecord />)}</div>}
  </Card>
}
