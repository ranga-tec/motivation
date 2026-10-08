import { config } from '../config'
import type { AdminCatalog, AdminCenter, AdminDevice, AdminItem, AdminUsers, AdminUser, Appointment, AppointmentOptions, Assessment, ClinicalOptions, CreateAppointmentRequest, CreatePatientRequest, DashboardMetrics, Delivery, DocumentOptions, Episode, EpisodeClinicalRecords, EpisodeOptions, Fitting, FollowUp, ListOptions, PagedResponse, PatientDetail, PatientRegistrationOptions, PatientSummary, PrescriptionOption, ReportFilter, ReportOptions, ReportResult, SaveAssessmentRequest, SaveDeliveryRequest, SaveEpisodeRequest, SaveFittingRequest, SaveFollowUpRequest, StoredDocument } from './types'

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
async function putVoid(path: string, body: unknown): Promise<void> { const token = await accessToken?.(); const response = await fetch(`${config.apiUrl}${path}`, { method: 'PUT', headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: JSON.stringify(body) }); if (!response.ok) { const problem = await response.json().catch(() => undefined) as ApiProblem | undefined; throw new ApiError(response.status, problem?.detail ?? problem?.title ?? `Request failed (${response.status})`, problem) } }
async function upload<T>(path: string, body: FormData): Promise<T> {
  const token = await accessToken?.(); const response = await fetch(`${config.apiUrl}${path}`, { method: 'POST', headers: token ? { Authorization: `Bearer ${token}` } : {}, body })
  if (!response.ok) { const problem = await response.json().catch(() => undefined) as ApiProblem | undefined; throw new ApiError(response.status, problem?.detail ?? problem?.title ?? `Request failed (${response.status})`, problem) }
  return response.json() as Promise<T>
}
async function remove(path: string): Promise<void> { const token = await accessToken?.(); const response = await fetch(`${config.apiUrl}${path}`, { method: 'DELETE', headers: token ? { Authorization: `Bearer ${token}` } : {} }); if (!response.ok) throw new ApiError(response.status, `Request failed (${response.status})`) }
async function download(path: string): Promise<{ blob: Blob; fileName: string }> { const token = await accessToken?.(); const response = await fetch(`${config.apiUrl}${path}`, { headers: token ? { Authorization: `Bearer ${token}` } : {} }); if (!response.ok) throw new ApiError(response.status, `Request failed (${response.status})`); const disposition = response.headers.get('content-disposition') ?? ''; const fileName = /filename\*?=(?:UTF-8'')?"?([^";]+)/i.exec(disposition)?.[1] ?? 'document'; return { blob: await response.blob(), fileName: decodeURIComponent(fileName) } }
export const api = {
  patients: async (options: ListOptions = {}): Promise<PagedResponse<PatientSummary>> => get(`/api/v1/patients?${queryString(options)}`),
  patient: async (id: string): Promise<PatientDetail> => get(`/api/v1/patients/${id}`),
  appointments: async (options: ListOptions = {}): Promise<PagedResponse<Appointment>> => get(`/api/v1/appointments?${queryString(options)}`),
  registrationOptions: async (): Promise<PatientRegistrationOptions> => get('/api/v1/patients/registration-options'),
  createPatient: async (request: CreatePatientRequest): Promise<PatientDetail> => post('/api/v1/patients', request),
  appointmentOptions: async (): Promise<AppointmentOptions> => get('/api/v1/appointments/options'),
  createAppointment: async (request: CreateAppointmentRequest): Promise<Appointment> => post('/api/v1/appointments', request),
  completeAppointment: async (id: string): Promise<Appointment> => post(`/api/v1/appointments/${id}/complete`, {}),
  cancelAppointment: async (id: string, reason: string): Promise<Appointment> => post(`/api/v1/appointments/${id}/cancel`, { reason }),
  rescheduleAppointment: async (id: string, appointmentDate: string, appointmentTime: string | undefined, reason: string): Promise<Appointment> => post(`/api/v1/appointments/${id}/reschedule`, { appointmentDate, appointmentTime: appointmentTime || null, reason }),
  episodes: async (patientId: string): Promise<Episode[]> => get(`/api/v1/episodes?patientId=${encodeURIComponent(patientId)}`),
  episode: async (id: string): Promise<Episode> => get(`/api/v1/episodes/${id}`),
  episodeOptions: async (): Promise<EpisodeOptions> => get('/api/v1/episodes/options'),
  createEpisode: async (request: SaveEpisodeRequest): Promise<Episode> => post('/api/v1/episodes', request),
  updateEpisode: async (id: string, request: SaveEpisodeRequest): Promise<Episode> => put(`/api/v1/episodes/${id}`, request),
  clinicalOptions: async (): Promise<ClinicalOptions> => get('/api/v1/clinical-records/options'),
  prescriptionOptions: async (assessmentType: string, limbCategory: string): Promise<PrescriptionOption[]> => get(`/api/v1/clinical-records/prescription-options?assessmentType=${assessmentType}&limbCategory=${limbCategory}`),
  clinicalRecords: async (episodeId: string): Promise<EpisodeClinicalRecords> => get(`/api/v1/clinical-records/episodes/${episodeId}`),
  createAssessment: async (request: SaveAssessmentRequest): Promise<Assessment> => post('/api/v1/clinical-records/assessments', request),
  updateAssessment: async (id: string, request: SaveAssessmentRequest): Promise<Assessment> => put(`/api/v1/clinical-records/assessments/${id}`, request),
  createFitting: async (request: SaveFittingRequest): Promise<Fitting> => post('/api/v1/clinical-records/fittings', request),
  updateFitting: async (id: string, request: SaveFittingRequest): Promise<Fitting> => put(`/api/v1/clinical-records/fittings/${id}`, request),
  createDelivery: async (request: SaveDeliveryRequest): Promise<Delivery> => post('/api/v1/clinical-records/deliveries', request),
  updateDelivery: async (id: string, request: SaveDeliveryRequest): Promise<Delivery> => put(`/api/v1/clinical-records/deliveries/${id}`, request),
  createFollowUp: async (request: SaveFollowUpRequest): Promise<FollowUp> => post('/api/v1/clinical-records/follow-ups', request),
  updateFollowUp: async (id: string, request: SaveFollowUpRequest): Promise<FollowUp> => put(`/api/v1/clinical-records/follow-ups/${id}`, request),
  documentOptions: async (): Promise<DocumentOptions> => get('/api/v1/documents/options'),
  documents: async (scope: 'patient' | 'episode', ownerId: string): Promise<StoredDocument[]> => get(`/api/v1/documents?${scope}Id=${encodeURIComponent(ownerId)}`),
  uploadDocument: async (scope: 'patient' | 'episode', ownerId: string, documentType: string, notes: string, restricted: boolean, file: File): Promise<StoredDocument> => { const body = new FormData(); body.set(`${scope}Id`, ownerId); body.set('documentType', documentType); body.set('notes', notes); body.set('isRestricted', String(restricted)); body.set('file', file); return upload('/api/v1/documents', body) },
  deleteDocument: async (document: StoredDocument): Promise<void> => remove(`/api/v1/documents/${document.id}?scope=${document.scope}`),
  downloadDocument: async (document: StoredDocument): Promise<{ blob: Blob; fileName: string }> => download(`/api/v1/documents/${document.id}?scope=${document.scope}`),
  printRegistration: async (patientId: string) => download(`/api/v1/print/patients/${patientId}/registration`),
  printAssessment: async (id: string) => download(`/api/v1/print/assessments/${id}`),
  printPrescription: async (id: string) => download(`/api/v1/print/assessments/${id}/prescription`),
  printDelivery: async (id: string) => download(`/api/v1/print/deliveries/${id}`),
  printFollowUp: async (id: string) => download(`/api/v1/print/follow-ups/${id}`),
  adminCatalog: async (): Promise<AdminCatalog> => get('/api/v1/admin/catalog'),
  adminUsers: async (): Promise<AdminUsers> => get('/api/v1/admin/users'),
  saveAdminItem: async (resource: string, item: Record<string, unknown>, id?: number): Promise<AdminItem | AdminCenter | AdminDevice> => id ? put(`/api/v1/admin/${resource}/${id}`, item) : post(`/api/v1/admin/${resource}`, item),
  createAdminUser: async (request: Record<string, unknown>): Promise<AdminUser> => post('/api/v1/admin/users', request),
  updateAdminProfile: async (id: string, request: Record<string, unknown>): Promise<AdminUser> => put(`/api/v1/admin/users/${id}/profile`, request),
  updateAdminRoles: async (id: string, roles: string[]): Promise<void> => putVoid(`/api/v1/admin/users/${id}/roles`, { roles }),
  setAdminLock: async (id: string, locked: boolean): Promise<void> => putVoid(`/api/v1/admin/users/${id}/lock`, { locked }),
  dashboardMetrics: async (): Promise<DashboardMetrics> => get('/api/v1/dashboard'),
  reportOptions: async (): Promise<ReportOptions> => get('/api/v1/reports/options'),
  report: async (key: string, filter: ReportFilter): Promise<ReportResult> => get(`/api/v1/reports/${key}?${queryString(filter)}`),
  reportPdf: async (key: string, filter: ReportFilter) => download(`/api/v1/reports/${key}/pdf?${queryString(filter)}`),
}
