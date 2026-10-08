import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { BarChart3, CalendarDays, ChevronDown, ClipboardPlus, LayoutDashboard, Menu, Search, Settings, Users, X } from 'lucide-react'
import { useAuth } from '../auth/AuthProvider'

const navigation = [
  { to: '/', label: 'Overview', icon: LayoutDashboard, end: true },
  { to: '/patients', label: 'Patients', icon: Users },
  { to: '/appointments', label: 'Appointments', icon: CalendarDays },
  { to: '/reports', label: 'Reports', icon: BarChart3 },
  { to: '/admin', label: 'Administration', icon: Settings },
]
const titles: Record<string, string> = { '/': 'Overview', '/patients': 'Patients', '/patients/new': 'Register patient', '/appointments': 'Appointments', '/reports': 'Reports', '/admin': 'Administration' }

export function AppShell() {
  const [open, setOpen] = useState(false)
  const location = useLocation()
  const { user, roles: userRoles, signOut } = useAuth()
  const name = String(user?.profile.name ?? user?.profile.preferred_username ?? 'Staff user')
  const visibleNavigation = navigation.filter(item => item.to === '/reports'
    ? userRoles.some(role => ['VIEWER', 'MANAGEMENT', 'ADMIN'].includes(role))
    : item.to !== '/admin' || userRoles.includes('ADMIN'))
  return <div className="app-shell">
    {open && <button className="mobile-scrim" aria-label="Close navigation" onClick={() => setOpen(false)} />}
    <aside className={`sidebar ${open ? 'open' : ''}`}>
      <div className="brand"><div className="brand-mark">P</div><div><strong>POMS</strong><span>Clinical workspace</span></div><button className="icon-button mobile-only" onClick={() => setOpen(false)}><X /></button></div>
      <nav aria-label="Primary navigation">{visibleNavigation.map(({ to, label, icon: Icon, end }) => <NavLink key={to} to={to} end={end} onClick={() => setOpen(false)}><Icon size={19} /><span>{label}</span></NavLink>)}</nav>
      <div className="sidebar-support"><ClipboardPlus size={20} /><div><strong>Clinical support</strong><span>System assistance</span></div></div>
    </aside>
    <div className="workspace">
      <header className="topbar"><button className="icon-button menu-button" onClick={() => setOpen(true)} aria-label="Open navigation"><Menu /></button><div><span className="mobile-title">{titles[location.pathname] ?? 'POMS'}</span></div><div className="topbar-actions"><button className="icon-button search-button" aria-label="Search"><Search /></button><button className="profile-button" onClick={() => void signOut()} title="Sign out"><span>{name.split(' ').map((part) => part[0]).slice(0, 2).join('')}</span><div><strong>{name}</strong><small>Authenticated user</small></div><ChevronDown size={15} /></button></div></header>
      <main className="page"><Outlet /></main>
    </div>
  </div>
}
