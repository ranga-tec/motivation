import { useRef, useState, type DragEvent } from 'react'
import { Download, FileText, ShieldAlert, Trash2, Upload, UploadCloud, X } from 'lucide-react'
import { api } from '../api/client'
import type { StoredDocument } from '../api/types'
import { useApi } from '../hooks/useApi'
import { errorMessage, formatDateTime, humanize, saveFile } from '../lib/format'
import { useToast } from '../lib/toast'
import { Alert, Card, ConfirmModal, EmptyState, ErrorPanel, Field, Skeleton, Spinner } from './Ui'

const size = (bytes?: number) => !bytes ? '' : bytes > 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`

export function DocumentPanel({ scope, ownerId, changed }: { scope: 'patient' | 'episode'; ownerId: string; changed?: () => void }) {
  const notify = useToast()
  const [refreshKey, setRefreshKey] = useState(0)
  const documents = useApi(() => api.documents(scope, ownerId), `documents-${scope}-${ownerId}-${refreshKey}`)
  const options = useApi(() => api.documentOptions(), 'document-options')
  const [type, setType] = useState('')
  const [notes, setNotes] = useState('')
  const [restricted, setRestricted] = useState(false)
  const [file, setFile] = useState<File>()
  const [dragging, setDragging] = useState(false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [deleting, setDeleting] = useState<StoredDocument>()
  const input = useRef<HTMLInputElement>(null)
  const types = (options.data?.documentTypes ?? []).filter((value) => value !== 'PatientPhoto' || scope === 'patient')
  const selectedType = type || (types.includes('Other') ? 'Other' : types[0] ?? '')
  const maxMb = options.data?.maxFileSizeMb ?? 10

  const choose = (selected?: File) => {
    setError('')
    if (!selected) return
    if (selected.size > maxMb * 1024 * 1024) { setError(`The file is larger than ${maxMb} MB.`); return }
    setFile(selected)
  }
  const drop = (event: DragEvent) => { event.preventDefault(); setDragging(false); choose(event.dataTransfer.files?.[0]) }
  const reset = () => { setFile(undefined); setNotes(''); setRestricted(false); if (input.current) input.current.value = '' }
  const upload = async () => {
    if (!file || submitting) return
    setSubmitting(true); setError('')
    try { await api.uploadDocument(scope, ownerId, selectedType, notes, restricted, file); notify(`${file.name} uploaded.`); reset(); setRefreshKey((value) => value + 1); changed?.() }
    catch (failure) { setError(errorMessage(failure, 'The document could not be uploaded.')) }
    finally { setSubmitting(false) }
  }
  const download = async (item: StoredDocument) => { try { saveFile(await api.downloadDocument(item)) } catch (failure) { notify(errorMessage(failure, 'The document could not be downloaded.'), 'error') } }
  const remove = async (item: StoredDocument) => {
    try { await api.deleteDocument(item); notify(`${item.fileName} deleted.`); setRefreshKey((value) => value + 1); changed?.() }
    catch (failure) { notify(errorMessage(failure, 'The document could not be deleted.'), 'error') }
    finally { setDeleting(undefined) }
  }

  return <Card bodyless title="Documents" count={documents.data?.length} description={scope === 'patient' ? 'NIC / passport copies, scanned forms, medical and supporting documents.' : 'Assessment photos, prescriptions and delivery confirmations for this record.'}>
    <div className="card-body" style={{ borderBottom: '1px solid var(--border)', background: 'var(--surface-2)' }}>
      {error && <Alert>{error}</Alert>}
      <div className="upload-grid">
        <div className={`dropzone ${dragging ? 'dragging' : ''}`} role="button" tabIndex={0} onClick={() => input.current?.click()} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); input.current?.click() } }}
          onDragOver={(event) => { event.preventDefault(); setDragging(true) }} onDragLeave={() => setDragging(false)} onDrop={drop}>
          <span className="file-icon">{file ? <FileText /> : <UploadCloud />}</span>
          <div><strong>{file ? file.name : 'Choose a file or drag it here'}</strong><span>{file ? size(file.size) : `${(options.data?.allowedExtensions ?? ['.pdf', '.jpg', '.png']).join(', ')} · up to ${maxMb} MB`}</span></div>
          {file && <button type="button" className="icon-btn sm" onClick={(event) => { event.stopPropagation(); reset() }} aria-label="Remove file"><X /></button>}
          <input ref={input} className="sr-only" type="file" tabIndex={-1} accept={options.data?.allowedExtensions.join(',')} onChange={(event) => choose(event.target.files?.[0])} />
        </div>
        <Field label="Document type" required><select value={selectedType} onChange={(event) => setType(event.target.value)}>{types.map((value) => <option key={value} value={value}>{humanize(value)}</option>)}</select></Field>
        <Field label="Notes"><input value={notes} maxLength={1000} onChange={(event) => setNotes(event.target.value)} placeholder="Optional" /></Field>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, marginTop: 14, flexWrap: 'wrap' }}>
        <label className="check-inline"><input type="checkbox" checked={restricted} onChange={(event) => setRestricted(event.target.checked)} /> Restricted — only authorised staff can open it</label>
        <button className="btn btn-primary" disabled={!file || submitting} onClick={() => void upload()}>{submitting ? <Spinner /> : <Upload />}{submitting ? 'Uploading…' : 'Upload document'}</button>
      </div>
    </div>
    {documents.error ? <ErrorPanel message={documents.error} retry={documents.refresh} /> : documents.loading ? <Skeleton rows={3} tall /> : !documents.data?.length
      ? <EmptyState compact icon={<FileText />} title="No documents yet" description="Uploaded files are stored securely under the patient number." />
      : <div className="table-wrap"><table className="table responsive">
        <thead><tr><th>File</th><th>Type</th><th>Uploaded</th><th className="actions"><span className="sr-only">Actions</span></th></tr></thead>
        <tbody>{documents.data.map((item) => <tr key={item.id}>
          <td className="primary-cell"><div className="cell-person" style={{ minWidth: 0 }}><span className="file-icon"><FileText /></span><div style={{ minWidth: 0 }}><strong style={{ overflowWrap: 'anywhere' }}>{item.fileName}</strong><span>{[size(item.fileSize), item.notes].filter(Boolean).join(' · ') || '—'}</span></div></div></td>
          <td data-label="Type"><span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}>{humanize(item.documentType)}{item.isRestricted && <span className="restricted-tag"><ShieldAlert /> Restricted</span>}</span></td>
          <td data-label="Uploaded"><div><span className="tabular">{formatDateTime(item.uploadedAt)}</span><span className="cell-sub">{item.uploadedBy}</span></div></td>
          <td className="actions"><div><button className="icon-btn sm" onClick={() => void download(item)} aria-label={`Download ${item.fileName}`} title="Download"><Download /></button><button className="icon-btn sm danger" onClick={() => setDeleting(item)} aria-label={`Delete ${item.fileName}`} title="Delete"><Trash2 /></button></div></td>
        </tr>)}</tbody>
      </table></div>}
    {deleting && <ConfirmModal title="Delete document?" message={<>“{deleting.fileName}” will be permanently removed from this {scope === 'patient' ? 'patient folder' : 'record'}.</>} confirmLabel="Delete document" onClose={() => setDeleting(undefined)} onConfirm={() => remove(deleting)} />}
  </Card>
}
