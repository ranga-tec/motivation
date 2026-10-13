import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ClipboardPlus, FolderOpen } from 'lucide-react'
import { api } from '../api/client'
import type { PatientSummary } from '../api/types'
import { useApi, useDebounced } from '../hooks/useApi'
import { formatDate } from '../lib/format'
import { Combobox } from './Combobox'
import { Badge, EmptyState, Field, Modal, Skeleton } from './Ui'

/** Type-ahead search over the patient folder. Shows the selected patient as a removable card. */
export function PatientSearch({ selected, onSelect, autoFocus }: { selected?: PatientSummary; onSelect: (patient?: PatientSummary) => void; autoFocus?: boolean }) {
  const [query, setQuery] = useState('')
  const term = useDebounced(query.trim(), 250)
  const results = useApi(() => api.patients({ search: term, pageSize: 8 }), `patient-search-${term}`, term.length >= 2)
  if (selected) return <div className="selected-patient"><div><strong>{selected.fullName}</strong><span>{selected.patientNumber} · {selected.centerName} · {selected.category}</span></div><button type="button" className="btn btn-ghost btn-sm" onClick={() => { onSelect(undefined); setQuery('') }}>Change</button></div>
  return <Combobox autoFocus={autoFocus} value={query} onChange={setQuery} required placeholder="Type a PNO, name, NIC or phone number" loading={results.loading && term.length >= 2}
    emptyText={term.length < 2 ? 'Type at least two characters' : 'No patients match this search'}
    options={term.length >= 2 ? (results.data?.items ?? []).map((patient) => ({ key: patient.id, label: `${patient.fullName}`, sub: `${patient.patientNumber} · ${patient.centerName} · born ${formatDate(patient.dateOfBirth)}` })) : []}
    onSelect={(option) => onSelect(results.data?.items.find((patient) => patient.id === option.key))} />
}

const titles = { record: 'New record / assessment', fitting: 'Add fitting', delivery: 'Add delivery', followUp: 'Add follow-up' }

/** Dashboard quick action: choose the patient, then the record the new entry belongs to. */
export function PatientPickerDialog({ action, close }: { action: 'record' | 'fitting' | 'delivery' | 'followUp'; close: () => void }) {
  const navigate = useNavigate()
  const [patient, setPatient] = useState<PatientSummary>()
  const episodes = useApi(() => api.episodes(patient!.id), `picker-episodes-${patient?.id}`, Boolean(patient) && action !== 'record')
  const go = (path: string) => { close(); navigate(path) }
  const continueRecord = () => patient && go(`/patients/${patient.id}?newRecord=1`)
  return <Modal title={titles[action]} description="Entries are always filed under the patient number." onClose={close} size="md"
    footer={<><button className="btn btn-secondary" onClick={close}>Cancel</button>{action === 'record' && <button className="btn btn-primary" disabled={!patient} onClick={continueRecord}><ClipboardPlus /> Continue</button>}</>}>
    <Field label="Patient" required><PatientSearch selected={patient} onSelect={setPatient} autoFocus /></Field>
    {patient && action !== 'record' && <div style={{ marginTop: 18 }}>
      <div className="field-label" style={{ marginBottom: 8 }}>Choose the patient record</div>
      {episodes.loading ? <Skeleton rows={2} tall /> : episodes.error ? <p className="field-error">{episodes.error}</p> : !episodes.data?.length
        ? <EmptyState compact icon={<FolderOpen />} title="No records yet" description="Create the first record for this patient, then add the entry." action={<button className="btn btn-primary btn-sm" onClick={continueRecord}><ClipboardPlus /> Create record</button>} />
        : <div className="list" style={{ border: '1px solid var(--border)', borderRadius: 10 }}>{[...episodes.data].sort((a, b) => Number(b.status === 'Active') - Number(a.status === 'Active')).map((episode) => <button key={episode.id} className="list-row" style={{ border: 0, borderBottom: '1px solid var(--border)', background: 'transparent', textAlign: 'left', width: '100%' }} onClick={() => go(`/records/${episode.id}?add=${action}`)}>
            <div className="grow"><strong>Record of {formatDate(episode.recordDate)} · {episode.centerName}</strong><span className="sub">{episode.assessmentCount} assessments · {episode.fittingCount} fittings · {episode.deliveryCount} deliveries · {episode.followUpCount} follow-ups</span></div>
            <Badge value={episode.status} />
          </button>)}</div>}
    </div>}
  </Modal>
}
