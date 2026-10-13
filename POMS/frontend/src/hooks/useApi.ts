import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react'

/* oxlint-disable react/set-state-in-effect */

/**
 * Loads data and reloads whenever `dependencyKey` changes. Responses that arrive after a newer
 * request started are ignored, so fast filter changes never show stale results.
 */
export function useApi<T>(load: () => Promise<T>, dependencyKey: string, enabled = true) {
  const [data, setData] = useState<T>()
  const [loading, setLoading] = useState(enabled)
  const [error, setError] = useState('')
  const loader = useRef(load)
  useLayoutEffect(() => { loader.current = load })
  const sequence = useRef(0)
  const refresh = useCallback(() => {
    if (!enabled) { setLoading(false); return }
    const current = ++sequence.current
    setLoading(true); setError('')
    loader.current()
      .then((value) => { if (current === sequence.current) setData(value) })
      .catch((reason: Error) => { if (current === sequence.current) setError(reason.message || 'Request failed.') })
      .finally(() => { if (current === sequence.current) setLoading(false) })
  }, [dependencyKey, enabled]) // eslint-disable-line react-hooks/exhaustive-deps
  useEffect(refresh, [refresh])
  return { data, loading, error, refresh, setData }
}

export function useDebounced<T>(value: T, delay = 300) {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => { const timer = setTimeout(() => setDebounced(value), delay); return () => clearTimeout(timer) }, [value, delay])
  return debounced
}
