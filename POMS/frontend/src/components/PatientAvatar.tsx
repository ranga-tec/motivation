/* oxlint-disable react/set-state-in-effect -- clears stale object URLs when the patient changes */
import { useEffect, useState } from 'react'
import { api } from '../api/client'
import { initials } from '../lib/format'

/** Patient photo fetched with the bearer token; falls back to initials. */
export function PatientAvatar({ patientId, name, hasPhoto, size = '', onOpen }: { patientId: string; name: string; hasPhoto: boolean; size?: '' | 'sm' | 'lg'; onOpen?: (url: string) => void }) {
  const [source, setSource] = useState<string>()
  useEffect(() => {
    if (!hasPhoto) { setSource(undefined); return }
    let active = true
    let objectUrl = ''
    void api.patientPhoto(patientId).then((blob) => {
      if (!active) return
      objectUrl = URL.createObjectURL(blob)
      setSource(objectUrl)
    }).catch(() => { if (active) setSource(undefined) })
    return () => { active = false; if (objectUrl) URL.revokeObjectURL(objectUrl) }
  }, [patientId, hasPhoto])
  const content = source ? <img src={source} alt={`${name}`} /> : initials(name)
  if (source && onOpen) return <button type="button" className={`avatar ${size}`} style={{ border: 0, padding: 0, cursor: 'zoom-in' }} onClick={() => onOpen(source)} aria-label="Enlarge photo">{content}</button>
  return <div className={`avatar ${size}`}>{content}</div>
}
