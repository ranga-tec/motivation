/* oxlint-disable react/set-state-in-effect -- clears stale object URLs when the patient changes */
import { useEffect, useState } from 'react'
import { api } from '../api/client'

export function PatientAvatar({ patientId, name, hasPhoto, large = false }: { patientId: string; name: string; hasPhoto: boolean; large?: boolean }) {
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
  const initials = name.split(' ').filter(Boolean).map((part) => part[0]).slice(0, 2).join('').toUpperCase()
  return <div className={`patient-avatar ${large ? 'large' : ''}`}>{source ? <img src={source} alt={`${name} profile`} /> : <span>{initials}</span>}</div>
}
