const value = (name: string) => (import.meta.env[name] as string | undefined)?.trim()

export const config = {
  apiUrl: (value('VITE_API_URL') ?? 'http://localhost:5015').replace(/\/$/, ''),
}
