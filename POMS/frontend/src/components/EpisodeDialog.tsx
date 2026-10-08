import { X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { Episode, SaveEpisodeRequest } from '../api/types'
import { useApi } from '../hooks/useApi'

const now = new Date()
const initial = (patientId: string, centerId: number, episode?: Episode): SaveEpisodeRequest => ({
  patientId,
  centerId: episode?.centerId ?? centerId,
  status: episode?.status ?? 'Active',
  recordDate: episode?.recordDate ?? now.toISOString().slice(0, 10),
  recordTime: episode?.recordTime?.slice(0, 5) ?? now.toTimeString().slice(0, 5),
  remarks: episode?.remarks ?? '',
  isRestricted: episode?.isRestricted ?? false,
})

export function EpisodeDialog({ patientId, patientName, centerId, episode, close, saved }: { patientId: string; patientName: string; centerId: number; episode?: Episode; close: () => void; saved: (message: string) => void }) {
  const options = useApi(() => api.episodeOptions(), 'episode-options')
  const [form, setForm] = useState(() => initial(patientId, centerId, episode))
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const submit = async (event: FormEvent) => {
    event.preventDefault(); if (submitting) return; setSubmitting(true); setError('')
    try {
      if (episode) await api.updateEpisode(episode.id, form)
      else await api.createEpisode(form)
      saved(`Patient record ${episode ? 'updated' : 'created'} successfully.`)
    } catch (failure) { setError(failure instanceof ApiError ? failure.message : 'The patient record could not be saved.') }
    finally { setSubmitting(false) }
  }
  return <div className="dialog-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) close() }}><section className="dialog" role="dialog" aria-modal="true" aria-labelledby="episode-dialog-title"><header><div><h2 id="episode-dialog-title">{episode ? 'Edit' : 'New'} patient record</h2><p>{patientName}</p></div><button type="button" className="icon-button" onClick={close} aria-label="Close"><X /></button></header><div className="dialog-body"><form onSubmit={(event) => void submit(event)}>{error && <div className="inline-error">{error}</div>}<div className="dialog-grid"><label className="form-field"><span>Centre *</span><select required value={form.centerId} onChange={(event) => setForm({ ...form, centerId: Number(event.target.value) })}><option value="">Select centre</option>{options.data?.centers.map((center) => <option key={center.id} value={center.id}>{center.name}</option>)}</select></label><label className="form-field"><span>Status *</span><select value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}>{options.data?.statuses.map((status) => <option key={status}>{status}</option>)}</select></label><label className="form-field"><span>Record date *</span><input required type="date" value={form.recordDate} onChange={(event) => setForm({ ...form, recordDate: event.target.value })} /></label><label className="form-field"><span>Record time *</span><input required type="time" value={form.recordTime} onChange={(event) => setForm({ ...form, recordTime: event.target.value })} /></label><label className="form-field wide"><span>Remarks</span><textarea maxLength={1000} rows={4} value={form.remarks} onChange={(event) => setForm({ ...form, remarks: event.target.value })} /></label><label className="checkbox-field wide"><input type="checkbox" checked={form.isRestricted} onChange={(event) => setForm({ ...form, isRestricted: event.target.checked })} /><span><strong>Restricted clinical record</strong><small>Only authorized users and the creator can access this record.</small></span></label></div><div className="dialog-actions"><button type="button" className="button secondary" onClick={close}>Close</button><button type="submit" className="button primary" disabled={submitting || options.loading}>{submitting ? 'Saving…' : episode ? 'Save changes' : 'Create record'}</button></div></form></div></section></div>
}
