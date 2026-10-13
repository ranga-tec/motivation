import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { BarChart3, CalendarRange, ClipboardCheck, FileDown, FileSpreadsheet, FileText, Globe2, MapPin, PackageCheck, Play, Ruler, Search, Stethoscope, Users } from 'lucide-react'
import { api } from '../api/client'
import type { ReportFilter, ReportResult } from '../api/types'
import { Alert, Card, EmptyState, ErrorPanel, Field, PageHeader, Skeleton, Spinner } from '../components/Ui'
import { useApi } from '../hooks/useApi'
import { errorMessage, saveFile } from '../lib/format'
import { useToast } from '../lib/toast'

type Kind = 'patients' | 'episodes' | 'assessments' | 'events'
const groups: { title: string; reports: { key: string; icon: typeof Users; kind: Kind }[] }[] = [
  { title: 'Patients', reports: [{ key: 'patient-registration', icon: Users, kind: 'patients' }, { key: 'foreign-patient', icon: Globe2, kind: 'patients' }, { key: 'active-records', icon: FileText, kind: 'episodes' }] },
  { title: 'Clinical', reports: [{ key: 'assessment', icon: Stethoscope, kind: 'assessments' }, { key: 'prosthetic-assessment', icon: Stethoscope, kind: 'assessments' }, { key: 'orthotic-assessment', icon: Stethoscope, kind: 'assessments' }, { key: 'fitting', icon: Ruler, kind: 'events' }, { key: 'delivery', icon: PackageCheck, kind: 'events' }, { key: 'follow-up', icon: ClipboardCheck, kind: 'events' }] },
  { title: 'Summaries', reports: [{ key: 'location-wise', icon: MapPin, kind: 'patients' }, { key: 'date-wise', icon: CalendarRange, kind: 'patients' }, { key: 'province-wise', icon: MapPin, kind: 'patients' }, { key: 'year-wise', icon: BarChart3, kind: 'patients' }] },
]
const all = groups.flatMap((group) => group.reports)
const thisYear = new Date().getFullYear()
const defaultFilter = (): ReportFilter => ({ dateFrom: `${thisYear}-01-01` })

function toCsv(result: ReportResult) {
  const escape = (value: string) => /[",\n]/.test(value) ? `"${value.replaceAll('"', '""')}"` : value
  return [result.headers, ...result.rows].map((row) => row.map((cell) => escape(cell ?? '')).join(',')).join('\r\n')
}

export function ReportsPage() {
  const notify = useToast()
  const [params, setParams] = useSearchParams()
  const key = all.some((item) => item.key === params.get('report')) ? params.get('report')! : 'patient-registration'
  const kind = all.find((item) => item.key === key)!.kind
  const options = useApi(() => api.reportOptions(), 'report-options')
  const [filter, setFilter] = useState<ReportFilter>(defaultFilter)
  const [applied, setApplied] = useState<ReportFilter>(defaultFilter)
  const [quick, setQuick] = useState('')
  const [exporting, setExporting] = useState(false)
  const result = useApi(() => api.report(key, applied), `report-${key}-${JSON.stringify(applied)}`)
  const title = options.data?.reports.find((item) => item.key === key)?.title ?? 'Report'
  const set = (patch: Partial<ReportFilter>) => setFilter((current) => ({ ...current, ...patch }))
  const invalidRange = Boolean(filter.dateFrom && filter.dateTo && filter.dateFrom > filter.dateTo)
  // oxlint-disable-next-line react/set-state-in-effect -- clear the in-table search when switching reports
  useEffect(() => setQuick(''), [key])

  const rows = useMemo(() => {
    const text = quick.trim().toLowerCase()
    return (result.data?.rows ?? []).filter((row) => !text || row.some((cell) => cell?.toLowerCase().includes(text)))
  }, [result.data, quick])
  const summary = result.data?.headers.length === 2 && /count|registrations/i.test(result.data.headers[1])
  const max = summary ? Math.max(1, ...rows.map((row) => Number(row[1]) || 0)) : 1

  const pdf = async () => { setExporting(true); try { saveFile(await api.reportPdf(key, applied)) } catch (error) { notify(errorMessage(error, 'The PDF could not be generated.'), 'error') } finally { setExporting(false) } }
  const csv = () => { if (result.data) saveFile({ blob: new Blob([`﻿${toCsv({ ...result.data, rows })}`], { type: 'text/csv;charset=utf-8' }), fileName: `${key}.csv` }) }

  return <>
    <PageHeader eyebrow="Reports" title={title} description="Filter, review and export operational reports. Exports use the filters currently applied."
      actions={<><button className="btn btn-secondary" disabled={!result.data?.rows.length} onClick={csv}><FileSpreadsheet /> CSV</button><button className="btn btn-primary" disabled={!result.data || exporting} onClick={() => void pdf()}>{exporting ? <Spinner /> : <FileDown />} Export PDF</button></>} />
    <div className="reports-layout">
      <nav className="card report-nav" aria-label="Reports">
        <div className="report-list">{groups.map((group) => <div key={group.title} style={{ display: 'contents' }}><h3>{group.title}</h3>{group.reports.map(({ key: item, icon: Icon }) => <button key={item} className={`report-link ${item === key ? 'active' : ''}`} onClick={() => setParams({ report: item })}><Icon />{options.data?.reports.find((report) => report.key === item)?.title.replace(/ Report$/, '') ?? item}</button>)}</div>)}</div>
      </nav>
      <Card bodyless>
        <form className="filter-grid" onSubmit={(event) => { event.preventDefault(); if (!invalidRange) setApplied({ ...filter }) }}>
          <Field label="From"><input type="date" value={filter.dateFrom ?? ''} onChange={(event) => set({ dateFrom: event.target.value || undefined })} /></Field>
          <Field label="To" error={invalidRange ? 'Must be on or after the start date' : undefined}><input type="date" value={filter.dateTo ?? ''} onChange={(event) => set({ dateTo: event.target.value || undefined })} /></Field>
          <Field label="Location"><select value={filter.centerId ?? ''} onChange={(event) => set({ centerId: event.target.value ? Number(event.target.value) : undefined })}><option value="">All locations</option>{options.data?.centers.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
          <Field label="Patient number (PNO)"><input value={filter.patientNumber ?? ''} onChange={(event) => set({ patientNumber: event.target.value || undefined })} placeholder="Any" /></Field>
          {kind !== 'events' && <Field label="Province"><select value={filter.provinceId ?? ''} onChange={(event) => set({ provinceId: event.target.value ? Number(event.target.value) : undefined })}><option value="">All provinces</option>{options.data?.provinces.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>}
          {(kind === 'patients' || kind === 'episodes') && key !== 'foreign-patient' && <Field label="Local / foreign"><select value={filter.isForeign === undefined ? '' : String(filter.isForeign)} onChange={(event) => set({ isForeign: event.target.value === '' ? undefined : event.target.value === 'true' })}><option value="">All patients</option><option value="false">Local</option><option value="true">Foreign</option></select></Field>}
          {key === 'assessment' && <Field label="Assessment type"><select value={filter.assessmentType ?? ''} onChange={(event) => set({ assessmentType: event.target.value || undefined })}><option value="">Prosthetic and orthotic</option><option value="Prosthetic">Prosthetic</option><option value="Orthotic">Orthotic</option></select></Field>}
          {kind === 'assessments' && <Field label="Prosthetic / orthotic type"><select value={filter.limbCategory ?? ''} onChange={(event) => set({ limbCategory: event.target.value || undefined })}><option value="">All types</option><option value="UpperLimb">Upper limb</option><option value="LowerLimb">Lower limb</option>{key !== 'prosthetic-assessment' && <option value="Spinal">Spinal</option>}</select></Field>}
          {kind !== 'events' && <Field label="Year"><input type="number" min={2000} max={thisYear + 1} value={filter.year ?? ''} onChange={(event) => set({ year: event.target.value ? Number(event.target.value) : undefined })} placeholder="Any year" /></Field>}
          <div className="filter-actions"><button type="button" className="btn btn-ghost" onClick={() => { setFilter(defaultFilter()); setApplied(defaultFilter()) }}>Reset</button><button type="submit" className="btn btn-primary" disabled={invalidRange}><Play /> Run report</button></div>
        </form>
        <div className="card-header">
          <div><h2>Results</h2><p>{result.loading ? 'Running…' : result.data ? `${rows.length}${quick ? ` of ${result.data.totalCount}` : ''} row${rows.length === 1 ? '' : 's'}` : ''}</p></div>
          {!!result.data?.rows.length && <label className="search-input" style={{ maxWidth: 260 }}><Search /><input value={quick} onChange={(event) => setQuick(event.target.value)} placeholder="Find in results" aria-label="Find in results" /></label>}
        </div>
        {options.error && <div style={{ padding: 16 }}><Alert>{options.error}</Alert></div>}
        {result.error ? <ErrorPanel message={result.error} retry={result.refresh} /> : result.loading ? <Skeleton rows={6} /> : !rows.length
          ? <EmptyState icon={<BarChart3 />} title="No rows for these filters" description="Widen the date range or clear some filters, then run the report again." />
          : <div className="table-wrap"><table className="table responsive">
            <thead><tr>{result.data!.headers.map((header) => <th key={header}>{header}</th>)}</tr></thead>
            <tbody>{rows.map((row, index) => <tr key={index}>{row.map((cell, column) => <td key={column} data-label={result.data!.headers[column]} className={column === 0 ? 'primary-cell' : ''}>
              {summary && column === 1 ? <div className="bar-cell"><div className="bar"><div style={{ width: `${(Number(cell) / max) * 100}%` }} /></div><strong className="tabular">{cell}</strong></div>
                : result.data!.headers[column] === 'PNO' ? <span className="pno">{cell}</span> : cell || <span className="muted">—</span>}
            </td>)}</tr>)}</tbody>
          </table></div>}
      </Card>
    </div>
  </>
}
