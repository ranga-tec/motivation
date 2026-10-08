export interface PagedResponse<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }
export interface PatientSummary { id: string; patientNumber: string; fullName: string; nameWithInitials: string; dateOfBirth: string; sex: string; category: string; centerId: number; centerName: string; registrationDate: string }
export interface PatientDetail extends PatientSummary { identificationType: string; identificationNumber: string; address1: string; address2?: string; province: string; district: string; city?: string; cityOther?: string; email?: string; nationality?: string; center: string; assignedClinicianName?: string; contacts: PatientContact[] }
export interface PatientContact { id: string; telephoneNumber: string; dateConfirmed?: string; personChecked?: string }
export interface Episode { id: string; patientId: string; patientNumber: string; patientName: string; centerId: number; centerName: string; status: string; recordDate: string; recordTime?: string; remarks?: string; isRestricted: boolean; assessmentCount: number; fittingCount: number; deliveryCount: number; followUpCount: number; documentCount: number }
export interface EpisodeOptions { centers: RegistrationOption[]; statuses: string[] }
export interface SaveEpisodeRequest { patientId: string; centerId: number; status: string; recordDate: string; recordTime: string; remarks?: string; isRestricted: boolean }
export interface ClinicalOption { id: number; name: string }
export interface ClinicalOptions { mainProblemTypes: ClinicalOption[]; causeReasonTypes: ClinicalOption[]; devices: ClinicalOption[]; assessmentTypes: string[]; limbCategories: string[]; sides: string[] }
export interface PrescriptionOption { code: string; label: string; subTypes: string[] }
export interface Prescription { id: string; side: string; code: string; label: string; subType?: string; otherText?: string }
export interface Assessment { id: string; episodeId: string; assessmentType: string; limbCategory: string; assessedOn: string; startTime?: string; endTime?: string; mainProblemTypeId: number; mainProblemType: string; side: string; causeReasonTypeId: number; causeReasonType: string; causeReasonOther?: string; additionalInformation?: string; isRestricted: boolean; prescriptions: Prescription[] }
export interface Fitting { id: string; episodeId: string; fittingDate: string; notes?: string; isRestricted: boolean }
export interface Delivery { id: string; episodeId: string; deliveryDate: string; deliveryTime?: string; notes?: string; deviceId?: number; deviceName?: string; isRestricted: boolean }
export interface FollowUp { id: string; episodeId: string; followUpDate: string; startTime?: string; endTime?: string; notes?: string; isRestricted: boolean }
export interface EpisodeClinicalRecords { assessments: Assessment[]; fittings: Fitting[]; deliveries: Delivery[]; followUps: FollowUp[] }
export interface SaveAssessmentRequest { episodeId: string; assessmentType: string; limbCategory: string; assessedOn: string; startTime: string; endTime: string; mainProblemTypeId: number; side: string; causeReasonTypeId: number; causeReasonOther?: string; additionalInformation?: string; isRestricted: boolean; prescriptions: { side: string; code: string; subType?: string; otherText?: string }[] }
export interface SaveFittingRequest { episodeId: string; fittingDate: string; notes?: string; isRestricted: boolean }
export interface SaveDeliveryRequest { episodeId: string; deliveryDate: string; deliveryTime: string; notes?: string; deviceId?: number; isRestricted: boolean }
export interface SaveFollowUpRequest { episodeId: string; followUpDate: string; startTime: string; endTime: string; notes?: string; isRestricted: boolean }
export interface DocumentOptions { documentTypes: string[]; maxFileSizeMb: number; allowedExtensions: string[] }
export interface StoredDocument { id: string; scope: 'patient' | 'episode'; ownerId: string; documentType: string; fileName: string; contentType: string; fileSize?: number; notes?: string; uploadedBy: string; uploadedAt: string; isRestricted: boolean }
export interface Appointment { id: string; patientId: string; patientNumber: string; patientName: string; episodeId?: string; type: string; appointmentDate: string; appointmentTime?: string; status: string; assignedClinicianUserId?: string; assignedClinicianName?: string; notes?: string }
export interface AppointmentOptions { assignees: AssigneeOption[] }
export interface CreateAppointmentRequest { patientId: string; type: string; appointmentDate: string; appointmentTime?: string; assignedClinicianEntry: string; assignedClinicianUserId?: string; notes?: string }
export interface ListOptions { page?: number; pageSize?: number; search?: string; dateFrom?: string; dateTo?: string; status?: string }
export interface RegistrationOption { id: number; name: string; parentId?: number }
export interface AssigneeOption { userId: string; displayName: string; fullName: string; isPreferred: boolean }
export interface PatientRegistrationOptions { provinces: RegistrationOption[]; districts: RegistrationOption[]; cities: RegistrationOption[]; centers: RegistrationOption[]; referralSources: RegistrationOption[]; assignees: AssigneeOption[] }
export interface CreatePatientRequest {
  fullName: string; nameWithInitials: string; dateOfBirth: string; sex: string; employment?: string; category: string; nationality?: string;
  identificationType: string; identificationNumber?: string; address1: string; address2?: string; provinceId: number; districtId: number;
  cityId?: number; cityOther?: string; email?: string; centerId: number; registrationDate: string; assignedClinicianEntry: string;
  assignedClinicianUserId?: string; guardianName: string; guardianRelationship: string; guardianAddress?: string; guardianPhone?: string;
  guardianMobile?: string; remarks?: string; confirmPossibleDuplicate: boolean; contacts: { telephoneNumber: string }[];
}
