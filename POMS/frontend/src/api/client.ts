import { config } from '../config'
import { createDemoPatient, demoApi, demoAppointmentActions, demoClinicalActions, demoClinicalOptions, demoEpisodeActions, demoRegistrationOptions } from './demoData'
import type { Appointment, AppointmentOptions, Assessment, ClinicalOptions, CreateAppointmentRequest, CreatePatientRequest, Delivery, Episode, EpisodeClinicalRecords, EpisodeOptions, Fitting, FollowUp, ListOptions, PagedResponse, PatientDetail, PatientRegistrationOptions, PatientSummary, PrescriptionOption, SaveAssessmentRequest, SaveDeliveryRequest, SaveEpisodeRequest, SaveFittingRequest, SaveFollowUpRequest } from './types'

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
  episode: async (id: string): Promise<Episode> => config.demoMode ? demoApi.episode(id) : get(`/api/v1/episodes/${id}`),
  episodeOptions: async (): Promise<EpisodeOptions> => config.demoMode ? { centers: demoRegistrationOptions.centers, statuses: ['Active', 'Completed', 'Cancelled'] } : get('/api/v1/episodes/options'),
  createEpisode: async (request: SaveEpisodeRequest): Promise<Episode> => config.demoMode ? demoEpisodeActions.create(request) : post('/api/v1/episodes', request),
  updateEpisode: async (id: string, request: SaveEpisodeRequest): Promise<Episode> => config.demoMode ? demoEpisodeActions.update(id, request) : put(`/api/v1/episodes/${id}`, request),
  clinicalOptions: async (): Promise<ClinicalOptions> => config.demoMode ? demoClinicalOptions : get('/api/v1/clinical-records/options'),
  prescriptionOptions: async (assessmentType: string, limbCategory: string): Promise<PrescriptionOption[]> => config.demoMode ? [{ code: 'OTHER', label: 'Other', subTypes: [] }] : get(`/api/v1/clinical-records/prescription-options?assessmentType=${assessmentType}&limbCategory=${limbCategory}`),
  clinicalRecords: async (episodeId: string): Promise<EpisodeClinicalRecords> => config.demoMode ? demoClinicalActions.records(episodeId) : get(`/api/v1/clinical-records/episodes/${episodeId}`),
  createAssessment: async (request: SaveAssessmentRequest): Promise<Assessment> => config.demoMode ? demoClinicalActions.saveAssessment(request) : post('/api/v1/clinical-records/assessments', request),
  updateAssessment: async (id: string, request: SaveAssessmentRequest): Promise<Assessment> => config.demoMode ? demoClinicalActions.saveAssessment(request, id) : put(`/api/v1/clinical-records/assessments/${id}`, request),
  createFitting: async (request: SaveFittingRequest): Promise<Fitting> => config.demoMode ? demoClinicalActions.saveFitting(request) : post('/api/v1/clinical-records/fittings', request),
  updateFitting: async (id: string, request: SaveFittingRequest): Promise<Fitting> => config.demoMode ? demoClinicalActions.saveFitting(request, id) : put(`/api/v1/clinical-records/fittings/${id}`, request),
  createDelivery: async (request: SaveDeliveryRequest): Promise<Delivery> => config.demoMode ? demoClinicalActions.saveDelivery(request) : post('/api/v1/clinical-records/deliveries', request),
  updateDelivery: async (id: string, request: SaveDeliveryRequest): Promise<Delivery> => config.demoMode ? demoClinicalActions.saveDelivery(request, id) : put(`/api/v1/clinical-records/deliveries/${id}`, request),
  createFollowUp: async (request: SaveFollowUpRequest): Promise<FollowUp> => config.demoMode ? demoClinicalActions.saveFollowUp(request) : post('/api/v1/clinical-records/follow-ups', request),
  updateFollowUp: async (id: string, request: SaveFollowUpRequest): Promise<FollowUp> => config.demoMode ? demoClinicalActions.saveFollowUp(request, id) : put(`/api/v1/clinical-records/follow-ups/${id}`, request),
}
