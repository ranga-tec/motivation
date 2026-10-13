import { CheckCircle2, LogOut } from 'lucide-react'
import { useAuth } from '../auth/AuthProvider'
import { Card, PageHeader, Segmented } from '../components/Ui'
import { roleLabel } from '../lib/format'
import { useTheme, type ThemeChoice } from '../lib/theme'

const themes: { value: ThemeChoice; label: string; description: string }[] = [
  { value: 'light', label: 'Light', description: 'Clean and bright' },
  { value: 'dark', label: 'Dark', description: 'Easy on the eyes' },
  { value: 'classic', label: 'Classic', description: 'Original navy sidebar' },
  { value: 'system', label: 'System', description: 'Follow this device' },
]

export function SettingsPage() {
  const { theme, setTheme, density, setDensity } = useTheme()
  const { user, roles, signOut } = useAuth()
  return <>
    <PageHeader eyebrow="Preferences" title="Settings" description="Appearance settings are saved on this device." />
    <div className="stack">
      <Card title="Theme" description="Choose how POMS looks. The Classic theme keeps the original navy design.">
        <div className="theme-options" role="radiogroup" aria-label="Theme">
          {themes.map((item) => <button key={item.value} type="button" role="radio" aria-checked={theme === item.value} aria-pressed={theme === item.value} className="theme-option" onClick={() => setTheme(item.value)}>
            <div className={`theme-preview preview-${item.value}`}><div className="p-side"><i /><i /><i /><i /></div><div className="p-main"><i /><div className="p-card" /><div className="p-card" /></div></div>
            <div className="label"><span>{item.label}<small style={{ display: 'block', fontWeight: 400, color: 'var(--muted)', fontSize: 12 }}>{item.description}</small></span>{theme === item.value && <CheckCircle2 />}</div>
          </button>)}
        </div>
      </Card>
      <Card bodyless title="Layout">
        <div className="setting-row"><div><h3>Density</h3><p>Compact fits more rows on screen; comfortable is easier to read and tap.</p></div><Segmented label="Density" value={density} onChange={setDensity} options={[{ value: 'comfortable', label: 'Comfortable' }, { value: 'compact', label: 'Compact' }]} /></div>
      </Card>
      <Card bodyless title="Account">
        <div className="setting-row"><div><h3>{user?.name}</h3><p>{user?.email}</p></div><div className="chips">{roles.map((role) => <span key={role} className="chip">{roleLabel(role)}</span>)}</div></div>
        <div className="setting-row"><div><h3>Sign out</h3><p>Ends your session on this browser. Password changes are handled by an administrator.</p></div><button className="btn btn-secondary" onClick={signOut}><LogOut /> Sign out</button></div>
      </Card>
    </div>
  </>
}
