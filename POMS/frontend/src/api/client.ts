import { config } from '../config'
import type { ActivityItem, AdminCatalog, AdminCenter, AdminDevice, AdminItem, AdminUser, AdminUsers, Appointment, AppointmentOptions, Assessment, ClinicalOptions, CreateAppointmentRequest, CreatePatientRequest, DashboardMetrics, DashboardRecord, Delivery, DocumentOptions, Episode, EpisodeClinicalRecords, EpisodeOptions, Fitting, FollowUp, ListOptions, PagedResponse, PatientDetail, PatientEditDetails, PatientFilterOptions, PatientRegistrationOptions, PatientSummary, PrescriptionOption, RecordListOptions, ReportFilter, ReportOptions, ReportResult, SaveAssessmentRequest, SaveDeliveryRequest, SaveEpisodeRequest, SaveFittingRequest, SaveFollowUpRequest, StoredDocument } from './types'

export interface ApiProblem { title?: string; detail?: string; duplicateType?: string; existingPatientNumber?: string; existingPatientName?: string; errors?: Record<string, string[]> }
export class ApiError extends Error {
  status: number
  problem?: ApiProblem
  constructor(status: number, message: string, problem?: ApiProblem) { super(message); this.status = status; this.problem = problem }
}

const problemMessage = (status: number, problem?: ApiProblem) => problem?.detail
  ?? Object.values(problem?.errors ?? {})[0]?.[0]
  ?? problem?.title
  ?? (status === 403 ? 'You do not have permission for this action.' : status === 404 ? 'The requested record was not found.' : `Request failed (${status}).`)

let accessToken: (() => Promise<string | undefined>) | undefined
export const registerTokenProvider = (provider: () => Promise<string | undefined>) => { accessToken = provider }

const queryString = (options: object) => {
  const query = new URLSearchParams()
  Object.entries(options).forEach(([key, value]) => { if (value !== undefined && value !== null && value !== '') query.set(key, String(value)) })
  const text = query.toString()
  return text ? `?${text}` : ''
}
const apiTime = (value?: string) => value ? (/^\d{2}:\d{2}$/.test(value) ? `${value}:00` : value) : null

async function send(method: string, path: string, body?: unknown): Promise<Response> {
  const token = await accessToken?.()
  const headers: Record<string, string> = token ? { Authorization: `Bearer ${token}` } : {}
  let payload: BodyInit | undefined
  if (body instanceof FormData) payload = body
  else if (body !== undefined) { headers['Content-Type'] = 'application/json'; payload = JSON.stringify(body) }
  let response: Response
  try { response = await fetch(`${config.apiUrl}${path}`, { method, headers, body: payload }) }
  catch { throw new ApiError(0, 'The POMS server could not be reached. Check your connection and try again.') }
  if (!response.ok) {
    const problem = await response.json().catch(() => undefined) as ApiProblem | undefined
    throw new ApiError(response.status, problemMessage(response.status, problem), problem)
  }
  return response
}
const get = async <T>(path: string) => (await send('GET', path)).json() as Promise<T>
const post = async <T>(path: string, body: unknown) => (await send('POST', path, body)).json() as Promise<T>
const put = async <T>(path: string, body: unknown) => (await send('PUT', path, body)).json() as Promise<T>
const putVoid = async (path: string, body: unknown) => { await send('PUT', path, body) }
const remove = async (path: string) => { await send('DELETE', path) }
async function download(path: string): Promise<{ blob: Blob; fileName: string }> {
  const response = await send('GET', path)
  const disposition = response.headers.get('content-disposition') ?? ''
  const fileName = /filename\*?=(?:UTF-8'')?"?([^";]+)/i.exec(disposition)?.[1] ?? 'document'
  return { blob: await response.blob(), fileName: decodeURIComponent(fileName) }
}

export const api = {
  // Patients
  patients: (options: ListOptions & { centerId?: number; provinceId?: number; districtId?: number } = {}) => get<PagedResponse<PatientSummary>>(`/api/v1/patients${queryString(options)}`),
  patient: (id: string) => get<PatientDetail>(`/api/v1/patients/${id}`),
  patientForEdit: (id: string) => get<PatientEditDetails>(`/api/v1/patients/${id}/edit`),
  patientFilterOptions: () => get<PatientFilterOptions>('/api/v1/patients/filter-options'),
  registrationOptions: () => get<PatientRegistrationOptions>('/api/v1/patients/registration-options'),
  createPatient: (request: CreatePatientRequest) => post<PatientDetail>('/api/v1/patients', request),
  updatePatient: (id: string, request: CreatePatientRequest) => put<PatientDetail>(`/api/v1/patients/${id}`, request),

  // Appointments
  appointments: (options: ListOptions = {}) => get<PagedResponse<Appointment>>(`/api/v1/appointments${queryString(options)}`),
  appointmentOptions: () => get<AppointmentOptions>('/api/v1/appointments/options'),
  createAppointment: (request: CreateAppointmentRequest) => post<Appointment>('/api/v1/appointments', { ...request, appointmentTime: apiTime(request.appointmentTime) }),
  completeAppointment: (id: string) => post<Appointment>(`/api/v1/appointments/${id}/complete`, {}),
  cancelAppointment: (id: string, reason: string) => post<Appointment>(`/api/v1/appointments/${id}/cancel`, { reason }),
  rescheduleAppointment: (id: string, appointmentDate: string, appointmentTime: string | undefined, reason: string) => post<Appointment>(`/api/v1/appointments/${id}/reschedule`, { appointmentDate, appointmentTime: apiTime(appointmentTime), reason }),

  // Patient records (episodes)
  episodes: (patientId: string) => get<Episode[]>(`/api/v1/episodes?patientId=${encodeURIComponent(patientId)}`),
  episode: (id: string) => get<Episode>(`/api/v1/episodes/${id}`),
  episodeOptions: () => get<EpisodeOptions>('/api/v1/episodes/options'),
  createEpisode: (request: SaveEpisodeRequest) => post<Episode>('/api/v1/episodes', { ...request, recordTime: apiTime(request.recordTime) }),
  updateEpisode: (id: string, request: SaveEpisodeRequest) => put<Episode>(`/api/v1/episodes/${id}`, { ...request, recordTime: apiTime(request.recordTime) }),

  // Clinical records
  clinicalOptions: () => get<ClinicalOptions>('/api/v1/clinical-records/options'),
  prescriptionOptions: (assessmentType: string, limbCategory: string) => get<PrescriptionOption[]>(`/api/v1/clinical-records/prescription-options${queryString({ assessmentType, limbCategory })}`),
  clinicalRecords: (episodeId: string) => get<EpisodeClinicalRecords>(`/api/v1/clinical-records/episodes/${episodeId}`),
  createAssessment: (request: SaveAssessmentRequest) => post<Assessment>('/api/v1/clinical-records/assessments', { ...request, startTime: apiTime(request.startTime), endTime: apiTime(request.endTime) }),
  updateAssessment: (id: string, request: SaveAssessmentRequest) => put<Assessment>(`/api/v1/clinical-records/assessments/${id}`, { ...request, startTime: apiTime(request.startTime), endTime: apiTime(request.endTime) }),
  createFitting: (request: SaveFittingRequest) => post<Fitting>('/api/v1/clinical-records/fittings', request),
  updateFitting: (id: string, request: SaveFittingRequest) => put<Fitting>(`/api/v1/clinical-records/fittings/${id}`, request),
  createDelivery: (request: SaveDeliveryRequest) => post<Delivery>('/api/v1/clinical-records/deliveries', { ...request, deliveryTime: apiTime(request.deliveryTime) }),
  updateDelivery: (id: string, request: SaveDeliveryRequest) => put<Delivery>(`/api/v1/clinical-records/deliveries/${id}`, { ...request, deliveryTime: apiTime(request.deliveryTime) }),
  createFollowUp: (request: SaveFollowUpRequest) => post<FollowUp>('/api/v1/clinical-records/follow-ups', { ...request, startTime: apiTime(request.startTime), endTime: apiTime(request.endTime) }),
  updateFollowUp: (id: string, request: SaveFollowUpRequest) => put<FollowUp>(`/api/v1/clinical-records/follow-ups/${id}`, { ...request, startTime: apiTime(request.startTime), endTime: apiTime(request.endTime) }),

  // Documents and printing
  documentOptions: () => get<DocumentOptions>('/api/v1/documents/options'),
  documents: (scope: 'patient' | 'episode', ownerId: string) => get<StoredDocument[]>(`/api/v1/documents?${scope}Id=${encodeURIComponent(ownerId)}`),
  uploadDocument: (scope: 'patient' | 'episode', ownerId: string, documentType: string, notes: string, restricted: boolean, file: File) => {
    const body = new FormData()
    body.set(`${scope}Id`, ownerId); body.set('documentType', documentType); body.set('notes', notes); body.set('isRestricted', String(restricted)); body.set('file', file)
    return send('POST', '/api/v1/documents', body).then((response) => response.json() as Promise<StoredDocument>)
  },
  deleteDocument: (document: StoredDocument) => remove(`/api/v1/documents/${document.id}?scope=${document.scope}`),
  downloadDocument: (document: StoredDocument) => download(`/api/v1/documents/${document.id}?scope=${document.scope}`),
  patientPhoto: async (patientId: string) => (await download(`/api/v1/documents/patient-photo/${patientId}`)).blob,
  printRegistration: (patientId: string) => download(`/api/v1/print/patients/${patientId}/registration`),
  printAssessment: (id: string) => download(`/api/v1/print/assessments/${id}`),
  printPrescription: (id: string) => download(`/api/v1/print/assessments/${id}/prescription`),
  printDelivery: (id: string) => download(`/api/v1/print/deliveries/${id}`),
  printFollowUp: (id: string) => download(`/api/v1/print/follow-ups/${id}`),

  // Administration
  adminCatalog: () => get<AdminCatalog>('/api/v1/admin/catalog'),
  adminUsers: () => get<AdminUsers>('/api/v1/admin/users'),
  saveAdminItem: (resource: string, item: Record<string, unknown>, id?: number) => id ? put<AdminItem | AdminCenter | AdminDevice>(`/api/v1/admin/${resource}/${id}`, item) : post<AdminItem | AdminCenter | AdminDevice>(`/api/v1/admin/${resource}`, item),
  deleteAdminItem: (resource: string, id: number) => remove(`/api/v1/admin/${resource}/${id}`),
  createAdminUser: (request: Record<string, unknown>) => post<AdminUser>('/api/v1/admin/users', request),
  updateAdminProfile: (id: string, request: Record<string, unknown>) => put<AdminUser>(`/api/v1/admin/users/${id}/profile`, request),
  updateAdminRoles: (id: string, roles: string[]) => putVoid(`/api/v1/admin/users/${id}/roles`, { roles }),
  setAdminLock: (id: string, locked: boolean) => putVoid(`/api/v1/admin/users/${id}/lock`, { locked }),
  resetAdminPassword: (id: string, password: string) => putVoid(`/api/v1/admin/users/${id}/password`, { password }),

  // Dashboard and reports
  dashboardMetrics: () => get<DashboardMetrics>('/api/v1/dashboard'),
  dashboardActivity: () => get<ActivityItem[]>('/api/v1/dashboard/activity'),
  records: (options: RecordListOptions = {}) => get<PagedResponse<DashboardRecord>>(`/api/v1/dashboard/records${queryString(options)}`),
  reportOptions: () => get<ReportOptions>('/api/v1/reports/options'),
  report: (key: string, filter: ReportFilter) => get<ReportResult>(`/api/v1/reports/${key}${queryString(filter)}`),
  reportPdf: (key: string, filter: ReportFilter) => download(`/api/v1/reports/${key}/pdf${queryString(filter)}`),
}
