import { useCallback, useEffect, useState } from 'react'

/* oxlint-disable react/set-state-in-effect */

export function useApi<T>(load: () => Promise<T>, dependencyKey: string) {
  const [data, setData] = useState<T>()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const refresh = useCallback(() => { setLoading(true); setError(''); load().then(setData).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false)) }, [dependencyKey]) // eslint-disable-line react-hooks/exhaustive-deps
  useEffect(refresh, [refresh])
  return { data, loading, error, refresh }
}
