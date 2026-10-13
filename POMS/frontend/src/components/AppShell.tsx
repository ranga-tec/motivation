import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { BarChart3, BookOpen, CalendarDays, ChevronDown, ClipboardList, FolderOpen, LayoutDashboard, LogOut, Menu, Moon, Plus, Search, Settings, Sun, UserPlus, Users, X } from 'lucide-react'
import { useAuth } from '../auth/AuthProvider'
import { access } from '../lib/access'
import { initials, roleLabel } from '../lib/format'
import { useTheme } from '../lib/theme'

const year = new Date().getFullYear()

export function AppShell() {
  const [open, setOpen] = useState(false)
  const location = useLocation()
  const { user, roles } = useAuth()
  const can = access(roles)
  // oxlint-disable-next-line react/set-state-in-effect -- close the mobile drawer after navigation
  useEffect(() => setOpen(false), [location.pathname])

  const workspace = [
    { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true, show: true },
    { to: '/patients', label: 'Patient Folder', icon: FolderOpen, show: can.readPatients },
    { to: '/records', label: 'Clinical Records', icon: ClipboardList, show: can.readPatients },
    { to: '/appointments', label: 'Appointments', icon: CalendarDays, show: true },
    { to: '/reports', label: 'Reports', icon: BarChart3, show: can.reports },
  ]
  const administration = [
    { to: '/admin/users', label: 'Users & access', icon: Users },
    { to: '/admin/reference', label: 'Reference data', icon: BookOpen },
  ]
  const name = user?.name ?? 'Staff user'

  return <div className="shell">
    {open && <button className="scrim" aria-label="Close navigation" onClick={() => setOpen(false)} />}
    <aside className={`sidebar ${open ? 'open' : ''}`} aria-label="Main navigation">
      <div className="sidebar-brand">
        <div className="brand-mark">EL</div>
        <div className="brand-text"><strong>Exceed Lanka</strong><span>P&amp;O Data Management</span></div>
        <button className="icon-btn sidebar-close" onClick={() => setOpen(false)} aria-label="Close navigation"><X /></button>
      </div>
      <nav className="sidebar-scroll">
        <div className="nav-group"><div className="nav-heading">Workspace</div>
          {workspace.filter((item) => item.show).map(({ to, label, icon: Icon, end }) => <NavLink key={to} to={to} end={end} className="nav-link"><Icon /><span>{label}</span></NavLink>)}
        </div>
        {can.isAdmin && <div className="nav-group"><div className="nav-heading">Administration</div>
          {administration.map(({ to, label, icon: Icon }) => <NavLink key={to} to={to} className="nav-link"><Icon /><span>{label}</span></NavLink>)}
        </div>}
        <div className="nav-group"><div className="nav-heading">Preferences</div>
          <NavLink to="/settings" className="nav-link"><Settings /><span>Settings</span></NavLink>
        </div>
      </nav>
      <div className="sidebar-footer">
        <div className="sidebar-user"><div className="avatar sm">{initials(name)}</div><div><strong>{name}</strong><span>{roles.map(roleLabel).join(', ') || 'Staff'}</span></div></div>
      </div>
    </aside>

    <div className="main">
      <header className="topbar">
        <button className="icon-btn menu-toggle" onClick={() => setOpen(true)} aria-label="Open navigation"><Menu /></button>
        {can.readPatients ? <GlobalSearch /> : <div className="topbar-spacer" />}
        <div className="topbar-actions">
          {can.readPatients && <NewMenu canRegister={can.registerPatients} />}
          <ThemeToggle />
          <UserMenu />
        </div>
      </header>
      <main className="page" id="main"><Outlet /></main>
      <footer className="app-footer"><span>© {year} Exceed Lanka · Prosthetic &amp; Orthotic Data Management System</span><span>POMS · Authorised use only</span></footer>
    </div>
  </div>
}

function GlobalSearch() {
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const input = useRef<HTMLInputElement>(null)
  useEffect(() => {
    const shortcut = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement
      if (event.key === '/' && !['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)) { event.preventDefault(); input.current?.focus() }
    }
    document.addEventListener('keydown', shortcut)
    return () => document.removeEventListener('keydown', shortcut)
  }, [])
  const submit = (event: FormEvent) => { event.preventDefault(); navigate(`/patients?q=${encodeURIComponent(query.trim())}`); setQuery(''); input.current?.blur() }
  return <form className="topbar-search" role="search" onSubmit={submit}>
    <Search /><input ref={input} value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search PNO, name, NIC, passport or phone" aria-label="Search patients" /><kbd>/</kbd>
  </form>
}

function useDismiss(open: boolean, close: () => void) {
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!open) return
    const click = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) close() }
    const key = (event: KeyboardEvent) => { if (event.key === 'Escape') close() }
    document.addEventListener('mousedown', click); document.addEventListener('keydown', key)
    return () => { document.removeEventListener('mousedown', click); document.removeEventListener('keydown', key) }
  }, [open, close])
  return ref
}

function NewMenu({ canRegister }: { canRegister: boolean }) {
  const [open, setOpen] = useState(false)
  const ref = useDismiss(open, () => setOpen(false))
  return <div className="menu-wrap" ref={ref}>
    <button className="btn btn-primary btn-sm" onClick={() => setOpen((value) => !value)} aria-expanded={open}><Plus /> <span className="hide-mobile">New</span></button>
    {open && <div className="menu" role="menu" onClick={() => setOpen(false)}>
      {canRegister && <Link className="menu-item" role="menuitem" to="/patients/new"><UserPlus /> Register patient</Link>}
      <Link className="menu-item" role="menuitem" to="/appointments?new=1"><CalendarDays /> New appointment</Link>
    </div>}
  </div>
}

function ThemeToggle() {
  const { resolved, setTheme } = useTheme()
  const dark = resolved === 'dark'
  return <button className="icon-btn hide-mobile" onClick={() => setTheme(dark ? 'light' : 'dark')} aria-label={dark ? 'Switch to light theme' : 'Switch to dark theme'} title={dark ? 'Light theme' : 'Dark theme'}>{dark ? <Sun /> : <Moon />}</button>
}

function UserMenu() {
  const { user, roles, signOut } = useAuth()
  const [open, setOpen] = useState(false)
  const ref = useDismiss(open, () => setOpen(false))
  const name = user?.name ?? 'Staff user'
  return <div className="menu-wrap" ref={ref}>
    <button className="btn btn-ghost btn-sm" style={{ paddingLeft: 4 }} onClick={() => setOpen((value) => !value)} aria-expanded={open} aria-label="Account menu"><div className="avatar sm">{initials(name)}</div><ChevronDown /></button>
    {open && <div className="menu" role="menu">
      <div className="menu-header"><strong>{name}</strong><span>{user?.email}</span><span>{roles.map(roleLabel).join(' · ')}</span></div>
      <Link className="menu-item" role="menuitem" to="/settings" onClick={() => setOpen(false)}><Settings /> Settings &amp; appearance</Link>
      <div className="menu-sep" />
      <button className="menu-item danger" role="menuitem" onClick={signOut}><LogOut /> Sign out</button>
    </div>}
  </div>
}
