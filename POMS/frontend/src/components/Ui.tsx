import type { PropsWithChildren, ReactNode } from 'react'

export function PageHeader({ eyebrow, title, description, action }: { eyebrow: string; title: string; description: string; action?: ReactNode }) {
  return <header className="page-header"><div><p className="eyebrow">{eyebrow}</p><h1>{title}</h1><p>{description}</p></div>{action}</header>
}
export function EmptyState({ children }: PropsWithChildren) { return <div className="empty-state">{children}</div> }
export function LoadingRows() { return <div className="loading-rows">{[1,2,3,4].map((item) => <div key={item} className="skeleton" />)}</div> }
export function ErrorPanel({ message, retry }: { message: string; retry: () => void }) { return <div className="error-panel"><div><strong>Unable to load this information</strong><span>{message}</span></div><button className="button secondary" onClick={retry}>Try again</button></div> }
export function Badge({ value }: { value: string }) { const key = value.toLowerCase().replaceAll(' ', ''); return <span className={`badge ${key}`}>{value.replace(/([a-z])([A-Z])/g, '$1 $2')}</span> }
