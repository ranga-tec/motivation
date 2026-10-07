export interface PagedResponse<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }
export interface PatientSummary { id: string; patientNumber: string; fullName: string; nameWithInitials: string; dateOfBirth: string; sex: string; category: string; centerId: number; centerName: string; registrationDate: string }
export interface Appointment { id: string; patientId: string; patientNumber: string; patientName: string; episodeId?: string; type: string; appointmentDate: string; appointmentTime?: string; status: string; assignedClinicianUserId?: string; assignedClinicianName?: string; notes?: string }
export interface ListOptions { page?: number; pageSize?: number; search?: string; dateFrom?: string; dateTo?: string; status?: string }
