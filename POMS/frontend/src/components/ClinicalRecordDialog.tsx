import { useState } from 'react'
import { api } from '../api/client'
import type { Assessment, Delivery, Fitting, FollowUp, SaveAssessmentRequest } from '../api/types'
import { useApi } from '../hooks/useApi'
import { errorMessage, humanize, localDate } from '../lib/format'
import { Field, FormModal, Segmented } from './Ui'

export type ClinicalKind = 'assessment' | 'fitting' | 'delivery' | 'followUp'
export type ClinicalItem = Assessment | Fitting | Delivery | FollowUp
const labels: Record<ClinicalKind, string> = { assessment: 'assessment', fitting: 'fitting', delivery: 'delivery', followUp: 'follow-up' }

export function ClinicalRecordDialog({ kind, episodeId, patientName, item, close, saved }: { kind: ClinicalKind; episodeId: string; patientName: string; item?: ClinicalItem; close: () => void; saved: (message: string) => void }) {
  return kind === 'assessment'
    ? <AssessmentForm episodeId={episodeId} patientName={patientName} item={item as Assessment | undefined} close={close} saved={saved} />
    : <EntryForm kind={kind} episodeId={episodeId} patientName={patientName} item={item as Fitting | Delivery | FollowUp | undefined} close={close} saved={saved} />
}

function Restricted({ checked, set }: { checked: boolean; set: (value: boolean) => void }) {
  return <label className="check span-all"><input type="checkbox" checked={checked} onChange={(event) => set(event.target.checked)} /><span><strong>Restricted entry</strong><small>Only the author and staff with restricted-data access can open it.</small></span></label>
}

function AssessmentForm({ episodeId, patientName, item, close, saved }: { episodeId: string; patientName: string; item?: Assessment; close: () => void; saved: (message: string) => void }) {
  const options = useApi(() => api.clinicalOptions(), 'clinical-options')
  const [form, setForm] = useState<SaveAssessmentRequest>(() => ({
    episodeId, assessmentType: item?.assessmentType ?? 'Prosthetic', limbCategory: item?.limbCategory ?? 'LowerLimb', assessedOn: item?.assessedOn ?? localDate(),
    startTime: item?.startTime?.slice(0, 5) ?? '09:00', endTime: item?.endTime?.slice(0, 5) ?? '10:00', mainProblemTypeId: item?.mainProblemTypeId ?? 0, side: item?.side ?? 'Left',
    causeReasonTypeId: item?.causeReasonTypeId ?? 0, causeReasonOther: item?.causeReasonOther ?? '', additionalInformation: item?.additionalInformation ?? '', isRestricted: item?.isRestricted ?? false,
    prescriptions: item?.prescriptions.map((row) => ({ side: row.side, code: row.code, subType: row.subType, otherText: row.otherText })) ?? [],
  }))
  const prescriptions = useApi(() => api.prescriptionOptions(form.assessmentType, form.limbCategory), `prescriptions-${form.assessmentType}-${form.limbCategory}`)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const set = (patch: Partial<SaveAssessmentRequest>) => setForm((current) => ({ ...current, ...patch }))
  const sides = form.side === 'Bilateral' ? ['Left', 'Right'] : [form.side]
  const limbs = (options.data?.limbCategories ?? ['UpperLimb', 'LowerLimb', 'Spinal']).filter((value) => form.assessmentType !== 'Prosthetic' || value !== 'Spinal')
  const cause = options.data?.causeReasonTypes.find((value) => value.id === form.causeReasonTypeId)
  const showCauseOther = Boolean(cause?.name.toLowerCase().startsWith('other') || form.causeReasonOther)
  const setPrescription = (side: string, patch: { code?: string; subType?: string; otherText?: string }) => {
    const existing = form.prescriptions.find((row) => row.side === side) ?? { side, code: '' }
    const next = { ...existing, ...patch }
    if (patch.code !== undefined) { next.subType = undefined; next.otherText = patch.code === 'OTHER' ? existing.otherText : undefined }
    set({ prescriptions: [...form.prescriptions.filter((row) => row.side !== side), next] })
  }
  const submit = async () => {
    if (form.endTime <= form.startTime) { setError('The end time must be later than the start time.'); return }
    setSubmitting(true); setError('')
    const request = { ...form, prescriptions: form.prescriptions.filter((row) => sides.includes(row.side)) }
    try {
      if (item) await api.updateAssessment(item.id, request); else await api.createAssessment(request)
      saved(`Assessment ${item ? 'updated' : 'saved'}.`)
    } catch (failure) { setError(errorMessage(failure, 'The assessment could not be saved.')) }
    finally { setSubmitting(false) }
  }
  return <FormModal size="lg" title={item ? 'Edit assessment' : 'New assessment'} description={patientName} onClose={close} onSubmit={submit} submitLabel={item ? 'Save assessment' : 'Save assessment'} submitting={submitting} disabled={options.loading} error={error}>
    <div className="form-section">
      <div className="form-section-title"><h3>Assessment</h3></div>
      <div className="form-grid cols-3">
        <Field label="Assessment type" required className="span-all"><div className="radio-cards">{['Prosthetic', 'Orthotic'].map((value) => <label key={value} className="radio-card"><input type="radio" name="assessmentType" checked={form.assessmentType === value} onChange={() => set({ assessmentType: value, limbCategory: value === 'Prosthetic' && form.limbCategory === 'Spinal' ? 'LowerLimb' : form.limbCategory, prescriptions: [] })} />{value} assessment</label>)}</div></Field>
        <Field label={form.assessmentType === 'Prosthetic' ? 'Type of prosthetic assessment' : 'Type of orthosis'} required className="span-all"><Segmented label="Limb category" value={form.limbCategory} onChange={(value) => set({ limbCategory: value, prescriptions: [] })} options={limbs.map((value) => ({ value, label: value === 'Spinal' ? 'Spinal orthosis' : humanize(value) }))} /></Field>
        <Field label="Date of assessment" required><input required type="date" value={form.assessedOn} onChange={(event) => set({ assessedOn: event.target.value })} /></Field>
        <Field label="Start time" required><input required type="time" value={form.startTime} onChange={(event) => set({ startTime: event.target.value })} /></Field>
        <Field label="End time" required><input required type="time" value={form.endTime} onChange={(event) => set({ endTime: event.target.value })} /></Field>
      </div>
    </div>
    <div className="form-section">
      <div className="form-section-title"><h3>Clinical findings</h3></div>
      <div className="form-grid">
        <Field label="Main problem type" required><select required value={form.mainProblemTypeId || ''} onChange={(event) => set({ mainProblemTypeId: Number(event.target.value) })}><option value="">Select main problem</option>{options.data?.mainProblemTypes.map((value) => <option key={value.id} value={value.id}>{value.name}</option>)}</select></Field>
        <Field label="Side" required><Segmented label="Side" value={form.side} onChange={(side) => set({ side })} options={(options.data?.sides ?? ['Left', 'Right', 'Bilateral']).map((value) => ({ value, label: value }))} /></Field>
        <Field label="Cause / reason / pathology" required><select required value={form.causeReasonTypeId || ''} onChange={(event) => set({ causeReasonTypeId: Number(event.target.value) })}><option value="">Select cause</option>{options.data?.causeReasonTypes.map((value) => <option key={value.id} value={value.id}>{value.name}</option>)}</select></Field>
        {showCauseOther ? <Field label="Cause details" required={cause?.name.toLowerCase().startsWith('other')}><input required={cause?.name.toLowerCase().startsWith('other')} maxLength={200} value={form.causeReasonOther} onChange={(event) => set({ causeReasonOther: event.target.value })} /></Field> : <div />}
        <Field label="Additional assessment information" className="span-all"><textarea rows={3} maxLength={2000} value={form.additionalInformation} onChange={(event) => set({ additionalInformation: event.target.value })} /></Field>
      </div>
    </div>
    <div className="form-section">
      <div className="form-section-title"><div><h3>Prescription</h3><p>{form.side === 'Bilateral' ? 'Bilateral — choose a prescription for each side. They can differ.' : 'Choose from the standard prescription list.'}</p></div></div>
      <div className="prescriptions">{sides.map((side) => {
        const row = form.prescriptions.find((value) => value.side === side) ?? { side, code: '' }
        const selected = prescriptions.data?.find((value) => value.code === row.code)
        return <div key={side} className="prescription-box">
          <h3>{form.side === 'Bilateral' ? `${side} side` : `${side} side prescription`}</h3>
          <div style={{ display: 'grid', gap: 12 }}>
            <Field label="Prescription" required><select required value={row.code} disabled={prescriptions.loading} onChange={(event) => setPrescription(side, { code: event.target.value })}><option value="">{prescriptions.loading ? 'Loading…' : 'Select prescription'}</option>{prescriptions.data?.map((value) => <option key={value.code} value={value.code}>{value.label}</option>)}</select></Field>
            {selected && selected.subTypes.length > 0 && <Field label="Sub-type"><select value={row.subType ?? ''} onChange={(event) => setPrescription(side, { subType: event.target.value || undefined })}><option value="">Not specified</option>{selected.subTypes.map((value) => <option key={value}>{value}</option>)}</select></Field>}
            {row.code === 'OTHER' && <Field label="Specify other type" required><input required maxLength={200} value={row.otherText ?? ''} onChange={(event) => setPrescription(side, { otherText: event.target.value })} /></Field>}
          </div>
        </div>
      })}</div>
    </div>
    <div className="form-section"><div className="form-grid"><Restricted checked={form.isRestricted} set={(isRestricted) => set({ isRestricted })} /></div></div>
  </FormModal>
}

function EntryForm({ kind, episodeId, patientName, item, close, saved }: { kind: Exclude<ClinicalKind, 'assessment'>; episodeId: string; patientName: string; item?: Fitting | Delivery | FollowUp; close: () => void; saved: (message: string) => void }) {
  const options = useApi(() => api.clinicalOptions(), 'clinical-options', kind === 'delivery')
  const initialDate = item ? ('fittingDate' in item ? item.fittingDate : 'deliveryDate' in item ? item.deliveryDate : item.followUpDate) : localDate()
  const [date, setDate] = useState(initialDate)
  const [start, setStart] = useState(item && 'startTime' in item ? item.startTime?.slice(0, 5) ?? '09:00' : '09:00')
  const [end, setEnd] = useState(item && 'endTime' in item ? item.endTime?.slice(0, 5) ?? '10:00' : '10:00')
  const [deliveryTime, setDeliveryTime] = useState(item && 'deliveryTime' in item ? item.deliveryTime?.slice(0, 5) ?? '09:00' : '09:00')
  const [deviceId, setDeviceId] = useState(item && 'deviceId' in item ? item.deviceId ?? 0 : 0)
  const [notes, setNotes] = useState(item?.notes ?? '')
  const [restricted, setRestricted] = useState(item?.isRestricted ?? false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const label = labels[kind]
  const title = label[0].toUpperCase() + label.slice(1)
  const submit = async () => {
    if (kind === 'followUp' && end <= start) { setError('The end time must be later than the start time.'); return }
    setSubmitting(true); setError('')
    try {
      if (kind === 'fitting') { const request = { episodeId, fittingDate: date, notes, isRestricted: restricted }; if (item) await api.updateFitting(item.id, request); else await api.createFitting(request) }
      else if (kind === 'delivery') { const request = { episodeId, deliveryDate: date, deliveryTime, notes, deviceId: deviceId || undefined, isRestricted: restricted }; if (item) await api.updateDelivery(item.id, request); else await api.createDelivery(request) }
      else { const request = { episodeId, followUpDate: date, startTime: start, endTime: end, notes, isRestricted: restricted }; if (item) await api.updateFollowUp(item.id, request); else await api.createFollowUp(request) }
      saved(`${title} ${item ? 'updated' : 'saved'}.`)
    } catch (failure) { setError(errorMessage(failure, `The ${label} could not be saved.`)) }
    finally { setSubmitting(false) }
  }
  return <FormModal title={`${item ? 'Edit' : 'Add'} ${label}`} description={patientName} onClose={close} onSubmit={submit} submitLabel={item ? 'Save changes' : `Save ${label}`} submitting={submitting} error={error}>
    <div className="form-grid">
      <Field label={`${title} date`} required className={kind === 'fitting' ? 'span-all' : ''}><input required type="date" value={date} onChange={(event) => setDate(event.target.value)} /></Field>
      {kind === 'delivery' && <Field label="Delivery time" required><input required type="time" value={deliveryTime} onChange={(event) => setDeliveryTime(event.target.value)} /></Field>}
      {kind === 'followUp' && <><div /><Field label="Start time" required><input required type="time" value={start} onChange={(event) => setStart(event.target.value)} /></Field><Field label="End time" required><input required type="time" value={end} onChange={(event) => setEnd(event.target.value)} /></Field></>}
      {kind === 'delivery' && <Field label="Device delivered" className="span-all"><select value={deviceId || ''} onChange={(event) => setDeviceId(Number(event.target.value))}><option value="">Not specified</option>{options.data?.devices.map((value) => <option key={value.id} value={value.id}>{value.name}</option>)}</select></Field>}
      <Field label={kind === 'delivery' ? 'Notes / remarks' : 'Notes'} className="span-all"><textarea rows={4} maxLength={2000} value={notes} onChange={(event) => setNotes(event.target.value)} placeholder={kind === 'fitting' ? 'What was done at this fitting?' : undefined} /></Field>
      <Restricted checked={restricted} set={setRestricted} />
    </div>
  </FormModal>
}
