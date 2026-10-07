import { config } from '../config'
import { demoApi } from './demoData'
import type { Appointment, ListOptions, PagedResponse, PatientSummary } from './types'

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) { super(message); this.status = status }
}
let accessToken: (() => Promise<string | undefined>) | undefined
export const registerTokenProvider = (provider: () => Promise<string | undefined>) => { accessToken = provider }
const queryString = (options: ListOptions) => { const query = new URLSearchParams(); Object.entries(options).forEach(([key, value]) => { if (value !== undefined && value !== '') query.set(key, String(value)) }); return query.toString() }
async function get<T>(path: string): Promise<T> {
  const token = await accessToken?.()
  const response = await fetch(`${config.apiUrl}${path}`, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
  if (!response.ok) { const problem = await response.json().catch(() => undefined); throw new ApiError(response.status, problem?.detail ?? problem?.title ?? `Request failed (${response.status})`) }
  return response.json() as Promise<T>
}
export const api = {
  patients: async (options: ListOptions = {}): Promise<PagedResponse<PatientSummary>> => config.demoMode ? demoApi.patients(options.search) : get(`/api/v1/patients?${queryString(options)}`),
  appointments: async (options: ListOptions = {}): Promise<PagedResponse<Appointment>> => config.demoMode ? demoApi.appointments() : get(`/api/v1/appointments?${queryString(options)}`),
}
