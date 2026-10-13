import { useState } from 'react'
import { api } from '../api/client'
import type { Episode, SaveEpisodeRequest } from '../api/types'
import { useApi } from '../hooks/useApi'
import { errorMessage, localDate, localTime } from '../lib/format'
import { Field, FormModal, Segmented } from './Ui'

export function EpisodeDialog({ patientId, patientName, centerId, episode, close, saved }: { patientId: string; patientName: string; centerId: number; episode?: Episode; close: () => void; saved: (message: string) => void }) {
  const options = useApi(() => api.episodeOptions(), 'episode-options')
  const [form, setForm] = useState<SaveEpisodeRequest>(() => ({
    patientId, centerId: episode?.centerId ?? centerId, status: episode?.status ?? 'Active', recordDate: episode?.recordDate ?? localDate(),
    recordTime: episode?.recordTime?.slice(0, 5) ?? localTime(), remarks: episode?.remarks ?? '', isRestricted: episode?.isRestricted ?? false,
  }))
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const set = (patch: Partial<SaveEpisodeRequest>) => setForm((current) => ({ ...current, ...patch }))
  const activeCentre = options.data?.centers.some((center) => center.id === form.centerId)
  const submit = async () => {
    if (!activeCentre) { setError('Select an active treatment centre.'); return }
    setSubmitting(true); setError('')
    try {
      if (episode) await api.updateEpisode(episode.id, form); else await api.createEpisode(form)
      saved(`Patient record ${episode ? 'updated' : 'created'}.`)
    } catch (failure) { setError(errorMessage(failure, 'The patient record could not be saved.')) }
    finally { setSubmitting(false) }
  }
  return <FormModal title={episode ? 'Edit patient record' : 'New patient record'} description={`${patientName} · one prosthetic or orthotic service case`} onClose={close} onSubmit={submit} submitLabel={episode ? 'Save changes' : 'Create record'} submitting={submitting} disabled={options.loading} error={error}>
    <div className="form-grid">
      <Field label="Location" required><select required value={activeCentre ? form.centerId : ''} onChange={(event) => set({ centerId: Number(event.target.value) })}><option value="">Select centre</option>{options.data?.centers.map((center) => <option key={center.id} value={center.id}>{center.name}</option>)}</select></Field>
      <Field label="Status" required><Segmented label="Status" value={form.status} onChange={(status) => set({ status })} options={(options.data?.statuses ?? ['Active', 'Completed', 'Cancelled']).map((value) => ({ value, label: value }))} /></Field>
      <Field label="Record date" required><input required type="date" value={form.recordDate} onChange={(event) => set({ recordDate: event.target.value })} /></Field>
      <Field label="Record time" required><input required type="time" value={form.recordTime} onChange={(event) => set({ recordTime: event.target.value })} /></Field>
      <Field label="Remarks" className="span-all"><textarea maxLength={1000} rows={3} value={form.remarks} onChange={(event) => set({ remarks: event.target.value })} placeholder="e.g. Left trans-tibial prosthesis — first fitting" /></Field>
      <label className="check span-all"><input type="checkbox" checked={form.isRestricted} onChange={(event) => set({ isRestricted: event.target.checked })} /><span><strong>Restricted record</strong><small>Only the author and staff with restricted-data access can open it.</small></span></label>
    </div>
  </FormModal>
}
