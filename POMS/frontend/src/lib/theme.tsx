/* oxlint-disable react/only-export-components */
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type PropsWithChildren } from 'react'

export type ThemeChoice = 'light' | 'dark' | 'classic' | 'system'
export type Density = 'comfortable' | 'compact'

interface ThemeState { theme: ThemeChoice; resolved: 'light' | 'dark' | 'classic'; density: Density; setTheme: (value: ThemeChoice) => void; setDensity: (value: Density) => void }

const themeKey = 'poms.theme'
const densityKey = 'poms.density'
const ThemeContext = createContext<ThemeState | undefined>(undefined)

const read = <T extends string>(key: string, allowed: readonly T[], fallback: T): T => {
  try { const value = window.localStorage.getItem(key) as T | null; return value && allowed.includes(value) ? value : fallback } catch { return fallback }
}
const write = (key: string, value: string) => { try { window.localStorage.setItem(key, value) } catch { /* storage unavailable */ } }
const prefersDark = () => window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false

export function ThemeProvider({ children }: PropsWithChildren) {
  const [theme, setThemeState] = useState<ThemeChoice>(() => read(themeKey, ['light', 'dark', 'classic', 'system'] as const, 'light'))
  const [density, setDensityState] = useState<Density>(() => read(densityKey, ['comfortable', 'compact'] as const, 'comfortable'))
  const [systemDark, setSystemDark] = useState(prefersDark)

  useEffect(() => {
    const query = window.matchMedia?.('(prefers-color-scheme: dark)')
    if (!query) return
    const listener = (event: MediaQueryListEvent) => setSystemDark(event.matches)
    query.addEventListener('change', listener)
    return () => query.removeEventListener('change', listener)
  }, [])

  const resolved = theme === 'system' ? (systemDark ? 'dark' : 'light') : theme
  useEffect(() => { document.documentElement.dataset.theme = resolved }, [resolved])
  useEffect(() => { document.documentElement.dataset.density = density }, [density])

  const setTheme = useCallback((value: ThemeChoice) => { setThemeState(value); write(themeKey, value) }, [])
  const setDensity = useCallback((value: Density) => { setDensityState(value); write(densityKey, value) }, [])
  const value = useMemo(() => ({ theme, resolved, density, setTheme, setDensity }), [theme, resolved, density, setTheme, setDensity])
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export const useTheme = () => { const value = useContext(ThemeContext); if (!value) throw new Error('ThemeProvider is missing'); return value }
