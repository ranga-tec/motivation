import { config } from '../config'
import { createDemoPatient, demoApi, demoAppointmentActions, demoEpisodeActions, demoRegistrationOptions } from './demoData'
import type { Appointment, AppointmentOptions, CreateAppointmentRequest, CreatePatientRequest, Episode, EpisodeOptions, ListOptions, PagedResponse, PatientDetail, PatientRegistrationOptions, PatientSummary, SaveEpisodeRequest } from './types'

export interface ApiProblem { title?: string; detail?: string; duplicateType?: string; existingPatientNumber?: string; existingPatientName?: string; errors?: Record<string, string[]> }
export class ApiError extends Error {
  status: number
  problem?: ApiProblem
  constructor(status: number, message: string, problem?: ApiProblem) { super(message); this.status = status; this.problem = problem }
}
let accessToken: (() => Promise<string | undefined>) | undefined
export const registerTokenProvider = (provider: () => Promise<string | undefined>) => { accessToken = provider }
const queryString = (options: ListOptions) => { const query = new URLSearchParams(); Object.entries(options).forEach(([key, value]) => { if (value !== undefined && value !== '') query.set(key, String(value)) }); return query.toString() }
async function get<T>(path: string): Promise<T> {
  const token = await accessToken?.()
  const response = await fetch(`${config.apiUrl}${path}`, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
  if (!response.ok) { const problem = await response.json().catch(() => undefined) as ApiProblem | undefined; throw new ApiError(response.status, problem?.detail ?? problem?.title ?? `Request failed (${response.status})`, problem) }
  return response.json() as Promise<T>
}
async function post<T>(path: string, body: unknown): Promise<T> {
  const token = await accessToken?.()
  const response = await fetch(`${config.apiUrl}${path}`, { method: 'POST', headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: JSON.stringify(body) })
  if (!response.ok) { const problem = await response.json().catch(() => undefined) as ApiProblem | undefined; throw new ApiError(response.status, problem?.detail ?? problem?.title ?? `Request failed (${response.status})`, problem) }
  return response.json() as Promise<T>
}
async function put<T>(path: string, body: unknown): Promise<T> {
  const token = await accessToken?.()
  const response = await fetch(`${config.apiUrl}${path}`, { method: 'PUT', headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: JSON.stringify(body) })
  if (!response.ok) { const problem = await response.json().catch(() => undefined) as ApiProblem | undefined; throw new ApiError(response.status, problem?.detail ?? problem?.title ?? `Request failed (${response.status})`, problem) }
  return response.json() as Promise<T>
}
export const api = {
  patients: async (options: ListOptions = {}): Promise<PagedResponse<PatientSummary>> => config.demoMode ? demoApi.patients(options.search) : get(`/api/v1/patients?${queryString(options)}`),
  patient: async (id: string): Promise<PatientDetail> => config.demoMode ? demoApi.patient(id) : get(`/api/v1/patients/${id}`),
  appointments: async (options: ListOptions = {}): Promise<PagedResponse<Appointment>> => config.demoMode ? demoApi.appointments() : get(`/api/v1/appointments?${queryString(options)}`),
  registrationOptions: async (): Promise<PatientRegistrationOptions> => config.demoMode ? demoRegistrationOptions : get('/api/v1/patients/registration-options'),
  createPatient: async (request: CreatePatientRequest): Promise<PatientDetail> => config.demoMode ? createDemoPatient(request) : post('/api/v1/patients', request),
  appointmentOptions: async (): Promise<AppointmentOptions> => config.demoMode ? { assignees: demoRegistrationOptions.assignees } : get('/api/v1/appointments/options'),
  createAppointment: async (request: CreateAppointmentRequest): Promise<Appointment> => config.demoMode ? demoAppointmentActions.create(request) : post('/api/v1/appointments', request),
  completeAppointment: async (id: string): Promise<Appointment> => config.demoMode ? demoAppointmentActions.complete(id) : post(`/api/v1/appointments/${id}/complete`, {}),
  cancelAppointment: async (id: string, reason: string): Promise<Appointment> => config.demoMode ? demoAppointmentActions.cancel(id) : post(`/api/v1/appointments/${id}/cancel`, { reason }),
  rescheduleAppointment: async (id: string, appointmentDate: string, appointmentTime: string | undefined, reason: string): Promise<Appointment> => config.demoMode ? demoAppointmentActions.reschedule(id, appointmentDate, appointmentTime) : post(`/api/v1/appointments/${id}/reschedule`, { appointmentDate, appointmentTime: appointmentTime || null, reason }),
  episodes: async (patientId: string): Promise<Episode[]> => config.demoMode ? demoApi.episodes(patientId) : get(`/api/v1/episodes?patientId=${encodeURIComponent(patientId)}`),
  episodeOptions: async (): Promise<EpisodeOptions> => config.demoMode ? { centers: demoRegistrationOptions.centers, statuses: ['Active', 'Completed', 'Cancelled'] } : get('/api/v1/episodes/options'),
  createEpisode: async (request: SaveEpisodeRequest): Promise<Episode> => config.demoMode ? demoEpisodeActions.create(request) : post('/api/v1/episodes', request),
  updateEpisode: async (id: string, request: SaveEpisodeRequest): Promise<Episode> => config.demoMode ? demoEpisodeActions.update(id, request) : put(`/api/v1/episodes/${id}`, request),
}
