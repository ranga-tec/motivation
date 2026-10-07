export interface PagedResponse<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }
export interface PatientSummary { id: string; patientNumber: string; fullName: string; nameWithInitials: string; dateOfBirth: string; sex: string; category: string; centerId: number; centerName: string; registrationDate: string }
export interface PatientDetail extends PatientSummary { identificationType: string; identificationNumber: string; address1: string; address2?: string; province: string; district: string; city?: string; cityOther?: string; email?: string; nationality?: string; center: string; assignedClinicianName?: string; contacts: PatientContact[] }
export interface PatientContact { id: string; telephoneNumber: string; dateConfirmed?: string; personChecked?: string }
export interface Appointment { id: string; patientId: string; patientNumber: string; patientName: string; episodeId?: string; type: string; appointmentDate: string; appointmentTime?: string; status: string; assignedClinicianUserId?: string; assignedClinicianName?: string; notes?: string }
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
