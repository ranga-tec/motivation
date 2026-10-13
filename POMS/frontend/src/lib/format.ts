const pad = (value: number) => String(value).padStart(2, '0')

/** Local calendar date as yyyy-MM-dd. `toISOString` would give the UTC date, which is wrong before 05:30 in Sri Lanka. */
export const localDate = (date = new Date()) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
export const localTime = (date = new Date()) => `${pad(date.getHours())}:${pad(date.getMinutes())}`
export const addDays = (iso: string, days: number) => { const date = new Date(`${iso}T00:00:00`); date.setDate(date.getDate() + days); return localDate(date) }

const parse = (value: string) => new Date(/^\d{4}-\d{2}-\d{2}$/.test(value) ? `${value}T00:00:00` : value)

export const formatDate = (value?: string | null) => value ? parse(value).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'
export const formatLongDate = (value: string) => parse(value).toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
export const formatDateTime = (value?: string | null) => value ? parse(value).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '—'
export const formatTime = (value?: string | null) => value ? value.slice(0, 5) : ''

export function relativeTime(value: string) {
  const seconds = Math.round((Date.now() - parse(value).getTime()) / 1000)
  if (seconds < 60) return 'Just now'
  const minutes = Math.round(seconds / 60)
  if (minutes < 60) return `${minutes} min ago`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `${hours} h ago`
  const days = Math.round(hours / 24)
  if (days < 7) return `${days} d ago`
  return formatDate(value)
}

export function age(dateOfBirth: string) {
  const birth = parse(dateOfBirth)
  const now = new Date()
  let years = now.getFullYear() - birth.getFullYear()
  if (now.getMonth() < birth.getMonth() || (now.getMonth() === birth.getMonth() && now.getDate() < birth.getDate())) years -= 1
  return years
}

/** Turns enum names such as `LowerLimb` or `DATA_ENTRY` into readable labels. */
export const humanize = (value?: string | null) => {
  if (!value) return ''
  if (value === 'NotApplicable') return 'N/A'
  if (value === 'FollowUp') return 'Follow-up'
  if (value === 'DrivingLicense') return 'Driving licence'
  if (/^[A-Z]+_[A-Z_]+$/.test(value)) return value.toLowerCase().split('_').map((part) => part[0].toUpperCase() + part.slice(1)).join(' ')
  return value.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export const roleLabel = (role: string) => ({ ADMIN: 'Administrator', CLINICIAN: 'Clinical user', DATA_ENTRY: 'Registration user', MANAGEMENT: 'Management user', VIEWER: 'Report user' } as Record<string, string>)[role] ?? humanize(role)

export const initials = (name: string) => name.split(/[\s.]+/).filter(Boolean).map((part) => part[0]).slice(0, 2).join('').toUpperCase() || '?'

export function saveFile({ blob, fileName }: { blob: Blob; fileName: string }) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export function greeting(date = new Date()) {
  const hour = date.getHours()
  return hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening'
}

export const errorMessage = (error: unknown, fallback: string) => error instanceof Error && error.message ? error.message : fallback
