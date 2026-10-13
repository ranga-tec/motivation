/* oxlint-disable react/only-export-components */
import { useEffect, useId, useLayoutEffect, useRef, useState, type KeyboardEvent, type PropsWithChildren, type ReactNode } from 'react'
import { AlertCircle, AlertTriangle, ChevronLeft, ChevronRight, Inbox, RotateCcw, X } from 'lucide-react'
import { humanize, initials } from '../lib/format'

export function PageHeader({ eyebrow, title, description, actions }: { eyebrow?: string; title: ReactNode; description?: ReactNode; actions?: ReactNode }) {
  return <header className="page-header">
    <div>{eyebrow && <p className="eyebrow">{eyebrow}</p>}<h1>{title}</h1>{description && <p className="description">{description}</p>}</div>
    {actions && <div className="page-actions">{actions}</div>}
  </header>
}

export function Card({ title, description, actions, count, children, className = '', bodyless }: PropsWithChildren<{ title?: ReactNode; description?: ReactNode; actions?: ReactNode; count?: number; className?: string; bodyless?: boolean }>) {
  return <section className={`card ${className}`}>
    {(title || actions) && <header className="card-header"><div>{title && <h2>{title}{count !== undefined && <span className="count">{count}</span>}</h2>}{description && <p>{description}</p>}</div>{actions && <div className="btn-group">{actions}</div>}</header>}
    {bodyless ? children : <div className="card-body">{children}</div>}
  </section>
}

const badgeTones: Record<string, string> = {
  active: 'tone-blue', scheduled: 'tone-blue', completed: 'tone-green', delivered: 'tone-green', cancelled: 'tone-red', locked: 'tone-red',
  inactive: 'tone-gray', local: 'tone-gray', foreign: 'tone-violet', prosthetic: 'tone-violet', orthotic: 'tone-amber',
  'follow-up': 'tone-green', fitting: 'tone-amber', assessed: 'tone-violet', 'awaiting assessment': 'tone-gray',
}
export function Badge({ value, tone, plain }: { value: string; tone?: string; plain?: boolean }) {
  const label = humanize(value)
  return <span className={`badge ${plain ? 'plain' : ''} ${tone ?? badgeTones[label.toLowerCase()] ?? 'tone-gray'}`}>{label}</span>
}

export function EmptyState({ icon, title, description, action, compact }: { icon?: ReactNode; title: string; description?: string; action?: ReactNode; compact?: boolean }) {
  return <div className={`empty ${compact ? 'compact' : ''}`}><div className="empty-icon">{icon ?? <Inbox />}</div><h3>{title}</h3>{description && <p>{description}</p>}{action}</div>
}

export function Skeleton({ rows = 4, tall }: { rows?: number; tall?: boolean }) {
  return <div className="skeleton-list" aria-busy="true" aria-label="Loading">{Array.from({ length: rows }, (_, index) => <div key={index} className={`skeleton ${tall ? 'tall' : ''}`} style={{ width: `${100 - (index % 3) * 12}%` }} />)}</div>
}

export function Alert({ tone = 'error', title, children, action }: PropsWithChildren<{ tone?: 'error' | 'warning' | 'info' | 'success'; title?: string; action?: ReactNode }>) {
  return <div className={`alert alert-${tone}`} role={tone === 'error' ? 'alert' : 'status'}>{tone === 'warning' ? <AlertTriangle /> : <AlertCircle />}<div>{title && <strong>{title}</strong>}{children}</div>{action}</div>
}

export function ErrorPanel({ message, retry }: { message: string; retry?: () => void }) {
  return <div style={{ padding: 16 }}><Alert title="Unable to load this information" action={retry && <button className="btn btn-secondary btn-sm" onClick={retry}><RotateCcw /> Retry</button>}>{message}</Alert></div>
}

export function Spinner() { return <span className="spinner" aria-hidden="true" /> }

export function Field({ label, required, error, hint, className = '', children }: PropsWithChildren<{ label: string; required?: boolean; error?: string; hint?: ReactNode; className?: string }>) {
  return <label className={`field ${error ? 'invalid' : ''} ${className}`}>
    <span className="field-label">{label}{required && <span className="req" aria-hidden="true">*</span>}</span>
    {children}
    {error ? <span className="field-error">{error}</span> : hint ? <span className="field-hint">{hint}</span> : null}
  </label>
}

export function Avatar({ name, src, size = '' }: { name: string; src?: string; size?: '' | 'sm' | 'lg' }) {
  return <div className={`avatar ${size}`}>{src ? <img src={src} alt="" /> : initials(name)}</div>
}

/** Accessible modal: Escape closes, focus moves to the first field, and page scroll is locked. */
export function Modal({ title, description, onClose, size = '', footer, children, busy }: PropsWithChildren<{ title: string; description?: ReactNode; onClose: () => void; size?: '' | 'md' | 'lg'; footer?: ReactNode; busy?: boolean }>) {
  const ref = useRef<HTMLElement>(null)
  const titleId = useId()
  const close = useRef(onClose)
  useLayoutEffect(() => { close.current = onClose })
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null
    const overflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const first = ref.current?.querySelector<HTMLElement>('.modal-body input:not([type=hidden]):not([disabled]), .modal-body select:not([disabled]), .modal-body textarea:not([disabled])')
    ;(first ?? ref.current?.querySelector<HTMLElement>('button'))?.focus()
    return () => { document.body.style.overflow = overflow; previous?.focus?.() }
  }, [])
  const keyDown = (event: KeyboardEvent) => { if (event.key === 'Escape' && !busy) { event.stopPropagation(); close.current() } }
  return <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget && !busy) onClose() }} onKeyDown={keyDown}>
    <section ref={ref} className={`modal ${size}`} role="dialog" aria-modal="true" aria-labelledby={titleId}>
      <header className="modal-header"><div><h2 id={titleId}>{title}</h2>{description && <p>{description}</p>}</div><button type="button" className="icon-btn" onClick={onClose} disabled={busy} aria-label="Close"><X /></button></header>
      <div className="modal-body">{children}</div>
      {footer && <footer className="modal-footer">{footer}</footer>}
    </section>
  </div>
}

/** Modal whose body is a form; the footer submit button submits it. */
export function FormModal({ title, description, onClose, onSubmit, submitLabel, submitting, disabled, danger, size, error, children, extraFooter }: PropsWithChildren<{ title: string; description?: ReactNode; onClose: () => void; onSubmit: () => void | Promise<void>; submitLabel: string; submitting?: boolean; disabled?: boolean; danger?: boolean; size?: '' | 'md' | 'lg'; error?: string; extraFooter?: ReactNode }>) {
  const formId = useId()
  return <Modal title={title} description={description} onClose={onClose} size={size} busy={submitting} footer={<>{extraFooter}<span className="spacer" /><button type="button" className="btn btn-secondary" onClick={onClose} disabled={submitting}>Cancel</button><button type="submit" form={formId} className={`btn ${danger ? 'btn-danger' : 'btn-primary'}`} disabled={submitting || disabled}>{submitting && <Spinner />}{submitting ? 'Saving…' : submitLabel}</button></>}>
    <form id={formId} onSubmit={(event) => { event.preventDefault(); if (!submitting) void onSubmit() }} noValidate={false}>
      {error && <Alert>{error}</Alert>}
      {children}
    </form>
  </Modal>
}

export function ConfirmModal({ title, message, confirmLabel, onConfirm, onClose, danger = true }: { title: string; message: ReactNode; confirmLabel: string; onConfirm: () => Promise<void> | void; onClose: () => void; danger?: boolean }) {
  const [busy, setBusy] = useState(false)
  const run = async () => { setBusy(true); try { await onConfirm() } finally { setBusy(false) } }
  return <Modal title={title} onClose={onClose} busy={busy} footer={<><button className="btn btn-secondary" onClick={onClose} disabled={busy}>Cancel</button><button className={`btn ${danger ? 'btn-danger' : 'btn-primary'}`} onClick={() => void run()} disabled={busy}>{busy && <Spinner />}{confirmLabel}</button></>}>
    <p style={{ color: 'var(--text-2)' }}>{message}</p>
  </Modal>
}

export function Pagination({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (page: number) => void }) {
  if (totalPages <= 1) return null
  return <div className="pagination"><button className="icon-btn sm" disabled={page <= 1} onClick={() => onChange(page - 1)} aria-label="Previous page"><ChevronLeft /></button><span>Page {page} of {totalPages}</span><button className="icon-btn sm" disabled={page >= totalPages} onClick={() => onChange(page + 1)} aria-label="Next page"><ChevronRight /></button></div>
}

export function Segmented<T extends string>({ value, options, onChange, label }: { value: T; options: { value: T; label: string }[]; onChange: (value: T) => void; label: string }) {
  return <div className="segmented" role="group" aria-label={label}>{options.map((option) => <button key={option.value} type="button" aria-pressed={value === option.value} onClick={() => onChange(option.value)}>{option.label}</button>)}</div>
}
