import { Search, UserRoundPlus, Users } from 'lucide-react'
import { useState } from 'react'
import { api } from '../api/client'
import { EmptyState, ErrorPanel, LoadingRows, PageHeader } from '../components/Ui'
import { useApi } from '../hooks/useApi'

export function PatientsPage() {
  const [query, setQuery] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const result = useApi(() => api.patients({ search, page, pageSize: 20 }), `${search}-${page}`)
  const submit = (event: React.FormEvent) => { event.preventDefault(); setPage(1); setSearch(query.trim()) }
  return <>
    <PageHeader eyebrow="Patient administration" title="Patients" description="Find and manage patient records across all treatment centres." action={<button className="button primary"><UserRoundPlus size={18} /> Register patient</button>} />
    <section className="panel list-panel">
      <form className="filters" onSubmit={submit}><label className="search-field"><Search size={18} /><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search by name or patient number" aria-label="Search patients" /></label><button className="button secondary" type="submit">Search</button></form>
      {result.error ? <ErrorPanel message={result.error} retry={result.refresh} /> : result.loading ? <LoadingRows /> : !result.data?.items.length ? <EmptyState><Users size={34} /><h2>No patients found</h2><p>Try a different name or patient number.</p></EmptyState> : <>
        <div className="table-wrap"><table><thead><tr><th>Patient</th><th>Patient number</th><th>Centre</th><th>Category</th><th>Registered</th></tr></thead><tbody>{result.data.items.map((patient) => <tr key={patient.id}><td data-label="Patient"><div className="table-person"><div className="person-avatar soft">{patient.fullName.split(' ').map((x) => x[0]).slice(0,2).join('')}</div><div><strong>{patient.fullName}</strong><span>{patient.sex} · Born {new Date(patient.dateOfBirth).toLocaleDateString('en-GB')}</span></div></div></td><td data-label="Patient number"><span className="record-number">{patient.patientNumber}</span></td><td data-label="Centre">{patient.centerName}</td><td data-label="Category">{patient.category}</td><td data-label="Registered">{new Date(patient.registrationDate).toLocaleDateString('en-GB')}</td></tr>)}</tbody></table></div>
        <div className="pagination"><span>Showing {result.data.items.length} of {result.data.totalCount} patients</span><div><button className="button secondary compact" disabled={page === 1} onClick={() => setPage((x) => x - 1)}>Previous</button><span>Page {page} of {Math.max(1, result.data.totalPages)}</span><button className="button secondary compact" disabled={page >= result.data.totalPages} onClick={() => setPage((x) => x + 1)}>Next</button></div></div>
      </>}
    </section>
  </>
}
