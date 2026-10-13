import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { FolderOpen, Search, UserPlus } from 'lucide-react'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthProvider'
import { PatientAvatar } from '../components/PatientAvatar'
import { Badge, Card, EmptyState, ErrorPanel, PageHeader, Pagination, Skeleton } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { access } from '../lib/access'
import { age, formatDate } from '../lib/format'

export function PatientsPage() {
  const { roles } = useAuth()
  const can = access(roles)
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const page = Number(params.get('page') ?? 1)
  const centerId = params.get('centre') ?? ''
  const provinceId = params.get('province') ?? ''
  const districtId = params.get('district') ?? ''
  const [query, setQuery] = useState(search)
  const [lastSearch, setLastSearch] = useState(search)
  if (search !== lastSearch) { setLastSearch(search); setQuery(search) }
  const options = useApi(() => api.patientFilterOptions(), 'patient-filter-options', can.readPatients)
  const result = useApi(() => api.patients({ search, page, pageSize: 20, centerId: Number(centerId) || undefined, provinceId: Number(provinceId) || undefined, districtId: Number(districtId) || undefined }), `patients-${search}-${page}-${centerId}-${provinceId}-${districtId}`, can.readPatients)
  const update = (patch: Record<string, string>) => {
    const next = new URLSearchParams(params)
    Object.entries(patch).forEach(([key, value]) => value ? next.set(key, value) : next.delete(key))
    if (!('page' in patch)) next.delete('page')
    setParams(next)
  }
  const submit = (event: FormEvent) => { event.preventDefault(); update({ q: query.trim() }) }
  const districts = options.data?.districts.filter((item) => !provinceId || item.parentId === Number(provinceId)) ?? []
  const filtered = Boolean(search || centerId || provinceId || districtId)

  return <>
    <PageHeader eyebrow="Patient folder" title="Patients" description="Every registration, record, document and printout is filed under the patient number (PNO)."
      actions={can.registerPatients && <Link to="/patients/new" className="btn btn-primary"><UserPlus /> Register patient</Link>} />
    <Card bodyless>
      <form className="card-toolbar" onSubmit={submit}>
        <label className="search-input"><Search /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="PNO, full name, name with initials, NIC, passport, licence or phone" aria-label="Search patients" /></label>
        <select className="select" style={{ width: 160 }} value={centerId} onChange={(event) => update({ centre: event.target.value })} aria-label="Location"><option value="">All locations</option>{options.data?.centers.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
        <select className="select" style={{ width: 170 }} value={provinceId} onChange={(event) => update({ province: event.target.value, district: '' })} aria-label="Province"><option value="">All provinces</option>{options.data?.provinces.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
        <select className="select" style={{ width: 160 }} value={districtId} onChange={(event) => update({ district: event.target.value })} aria-label="District"><option value="">All districts</option>{districts.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
        <button className="btn btn-secondary" type="submit"><Search /> Search</button>
        {filtered && <button className="btn btn-ghost" type="button" onClick={() => { setQuery(''); setParams({}) }}>Clear</button>}
      </form>
      {result.error ? <ErrorPanel message={result.error} retry={result.refresh} /> : result.loading ? <Skeleton rows={6} tall /> : !result.data?.items.length
        ? <EmptyState icon={<FolderOpen />} title={filtered ? 'No matching patients' : 'No patients registered yet'} description={filtered ? 'Try a different PNO, name, ID number or phone number, or clear the filters.' : 'Register the first patient to create a patient folder.'} action={!filtered && can.registerPatients ? <Link className="btn btn-primary btn-sm" to="/patients/new"><UserPlus /> Register patient</Link> : undefined} />
        : <>
          <div className="table-wrap"><table className="table responsive">
            <thead><tr><th>Patient</th><th>PNO</th><th>Location</th><th>Category</th><th>Age / sex</th><th>Registered</th></tr></thead>
            <tbody>{result.data.items.map((patient) => <tr key={patient.id} className="clickable" onClick={() => navigate(`/patients/${patient.id}`)}>
              <td className="primary-cell"><div className="cell-person"><PatientAvatar patientId={patient.id} name={patient.fullName} hasPhoto={patient.hasPhoto} /><div><Link to={`/patients/${patient.id}`} onClick={(event) => event.stopPropagation()} style={{ color: 'inherit' }}><strong>{patient.fullName}</strong></Link><span>{patient.nameWithInitials}</span></div></div></td>
              <td data-label="PNO"><span className="pno">{patient.patientNumber}</span></td>
              <td data-label="Location">{patient.centerName}</td>
              <td data-label="Category"><Badge value={patient.category} plain /></td>
              <td data-label="Age / sex" className="tabular">{age(patient.dateOfBirth)} y · {patient.sex}</td>
              <td data-label="Registered" className="tabular">{formatDate(patient.registrationDate)}</td>
            </tr>)}</tbody>
          </table></div>
          <div className="card-footer"><span>{result.data.totalCount} patient{result.data.totalCount === 1 ? '' : 's'}</span><Pagination page={page} totalPages={result.data.totalPages} onChange={(value) => update({ page: String(value) })} /></div>
        </>}
    </Card>
  </>
}
