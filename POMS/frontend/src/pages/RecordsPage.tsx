import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { ClipboardList, Search } from 'lucide-react'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthProvider'
import { Avatar, Badge, Card, EmptyState, ErrorPanel, PageHeader, Pagination, Segmented, Skeleton } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { access } from '../lib/access'
import { formatDate, humanize } from '../lib/format'

const statuses = [{ value: 'Active', label: 'Active' }, { value: 'Completed', label: 'Completed' }, { value: 'Cancelled', label: 'Cancelled' }, { value: 'all', label: 'All' }]

export function RecordsPage() {
  const { roles } = useAuth()
  const can = access(roles)
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? 'Active'
  const search = params.get('q') ?? ''
  const centre = params.get('centre') ?? ''
  const page = Number(params.get('page') ?? 1)
  const [query, setQuery] = useState(search)
  const centres = useApi(() => api.patientFilterOptions(), 'patient-filter-options')
  const result = useApi(() => api.records({ status: status === 'all' ? undefined : status, search, centerId: Number(centre) || undefined, page, pageSize: 20 }), `records-${status}-${search}-${centre}-${page}`)
  const update = (patch: Record<string, string>) => { const next = new URLSearchParams(params); Object.entries(patch).forEach(([key, value]) => value ? next.set(key, value) : next.delete(key)); if (!('page' in patch)) next.delete('page'); setParams(next) }
  const submit = (event: FormEvent) => { event.preventDefault(); update({ q: query.trim() }) }
  const open = (episodeId: string, patientId: string) => navigate(can.clinical ? `/records/${episodeId}` : `/patients/${patientId}`)

  return <>
    <PageHeader eyebrow="Clinical records" title="Patient records" description="Prosthetic and orthotic treatment records (episodes), tracked under each patient number." />
    <Card bodyless>
      <form className="card-toolbar" onSubmit={submit}>
        <Segmented label="Record status" value={status} onChange={(value) => update({ status: value === 'Active' ? '' : value })} options={statuses} />
        <label className="search-input"><Search /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search PNO or patient name" aria-label="Search records" /></label>
        <select className="select" style={{ width: 170 }} value={centre} onChange={(event) => update({ centre: event.target.value })} aria-label="Location"><option value="">All locations</option>{centres.data?.centers.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
      </form>
      {result.error ? <ErrorPanel message={result.error} retry={result.refresh} /> : result.loading ? <Skeleton rows={6} tall /> : !result.data?.items.length
        ? <EmptyState icon={<ClipboardList />} title="No records found" description="Records are created from a patient's folder. Adjust the filters to see other statuses." />
        : <>
          <div className="table-wrap"><table className="table responsive">
            <thead><tr><th>Patient</th><th>PNO</th><th>Location</th><th>Assessment type</th><th>Prosthesis / orthosis</th><th>Last activity</th><th>Stage / status</th></tr></thead>
            <tbody>{result.data.items.map((record) => <tr key={record.episodeId} className="clickable" onClick={() => open(record.episodeId, record.patientId)}>
              <td className="primary-cell"><div className="cell-person"><Avatar name={record.patientName} /><div><Link to={`/patients/${record.patientId}`} onClick={(event) => event.stopPropagation()} style={{ color: 'inherit' }}><strong>{record.patientName}</strong></Link></div></div></td>
              <td data-label="PNO"><span className="pno">{record.patientNumber}</span></td>
              <td data-label="Location">{record.location}</td>
              <td data-label="Assessment type">{record.assessmentType ? <Badge value={record.assessmentType} plain /> : <span className="muted">—</span>}</td>
              <td data-label="Prosthesis / orthosis"><div>{record.prescription ?? (record.limbCategory ? humanize(record.limbCategory) : <span className="muted">—</span>)}{record.prescription && record.limbCategory && <span className="cell-sub">{humanize(record.limbCategory)}</span>}</div></td>
              <td data-label="Last activity" className="tabular">{formatDate(record.lastActivity)}</td>
              <td data-label="Stage / status"><div className="chips"><Badge value={record.stage} plain />{record.status !== 'Active' && <Badge value={record.status} />}</div></td>
            </tr>)}</tbody>
          </table></div>
          <div className="card-footer"><span>{result.data.totalCount} record{result.data.totalCount === 1 ? '' : 's'}</span><Pagination page={page} totalPages={result.data.totalPages} onChange={(value) => update({ page: String(value) })} /></div>
        </>}
    </Card>
  </>
}
