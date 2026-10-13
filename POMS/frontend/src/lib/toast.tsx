/* oxlint-disable react/only-export-components */
import { createContext, useCallback, useContext, useMemo, useState, type PropsWithChildren } from 'react'
import { AlertCircle, CheckCircle2, Info, X } from 'lucide-react'

type Tone = 'success' | 'error' | 'info'
interface Toast { id: number; tone: Tone; message: string }
type Notify = (message: string, tone?: Tone) => void

const ToastContext = createContext<Notify | undefined>(undefined)
const icons = { success: CheckCircle2, error: AlertCircle, info: Info }

export function ToastProvider({ children }: PropsWithChildren) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const dismiss = useCallback((id: number) => setToasts((current) => current.filter((toast) => toast.id !== id)), [])
  const notify = useCallback<Notify>((message, tone = 'success') => {
    const id = Date.now() + Math.random()
    setToasts((current) => [...current.slice(-3), { id, tone, message }])
    setTimeout(() => dismiss(id), tone === 'error' ? 7000 : 4500)
  }, [dismiss])
  const value = useMemo(() => notify, [notify])
  return <ToastContext.Provider value={value}>
    {children}
    <div className="toasts" aria-live="polite">
      {toasts.map((toast) => { const Icon = icons[toast.tone]; return <div key={toast.id} className={`toast ${toast.tone}`} role={toast.tone === 'error' ? 'alert' : 'status'}><Icon /><p>{toast.message}</p><button className="icon-btn sm" onClick={() => dismiss(toast.id)} aria-label="Dismiss"><X /></button></div> })}
    </div>
  </ToastContext.Provider>
}

export const useToast = () => { const value = useContext(ToastContext); if (!value) throw new Error('ToastProvider is missing'); return value }
