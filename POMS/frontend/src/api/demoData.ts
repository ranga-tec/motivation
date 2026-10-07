import type { Appointment, PagedResponse, PatientSummary } from './types'

const patients: PatientSummary[] = [
  { id: '1', patientNumber: 'POM-2026-0042', fullName: 'Nimali Perera', nameWithInitials: 'N. Perera', dateOfBirth: '1987-04-18', sex: 'Female', category: 'Adult', centerId: 1, centerName: 'Colombo Central Clinic', registrationDate: '2026-09-28' },
  { id: '2', patientNumber: 'POM-2026-0041', fullName: 'Dinesh Fernando', nameWithInitials: 'D. Fernando', dateOfBirth: '1992-11-03', sex: 'Male', category: 'Adult', centerId: 1, centerName: 'Colombo Central Clinic', registrationDate: '2026-09-27' },
  { id: '3', patientNumber: 'POM-2026-0039', fullName: 'Shalini Jayawardena', nameWithInitials: 'S. Jayawardena', dateOfBirth: '1979-01-22', sex: 'Female', category: 'Adult', centerId: 2, centerName: 'Kandy Outreach Centre', registrationDate: '2026-09-25' },
  { id: '4', patientNumber: 'POM-2026-0037', fullName: 'Kasun Silva', nameWithInitials: 'K. Silva', dateOfBirth: '2001-08-14', sex: 'Male', category: 'Young Adult', centerId: 1, centerName: 'Colombo Central Clinic', registrationDate: '2026-09-23' },
  { id: '5', patientNumber: 'POM-2026-0035', fullName: 'Fathima Rizna', nameWithInitials: 'F. Rizna', dateOfBirth: '1995-06-07', sex: 'Female', category: 'Adult', centerId: 3, centerName: 'Galle Community Clinic', registrationDate: '2026-09-20' },
]
const appointments: Appointment[] = [
  { id: 'a1', patientId: '1', patientNumber: 'POM-2026-0042', patientName: 'Nimali Perera', type: 'Consultation', appointmentDate: '2026-10-07', appointmentTime: '09:00:00', status: 'Scheduled', assignedClinicianName: 'Dr. A. Fernando' },
  { id: 'a2', patientId: '2', patientNumber: 'POM-2026-0041', patientName: 'Dinesh Fernando', type: 'FollowUp', appointmentDate: '2026-10-07', appointmentTime: '10:30:00', status: 'CheckedIn', assignedClinicianName: 'Dr. S. Perera' },
  { id: 'a3', patientId: '3', patientNumber: 'POM-2026-0039', patientName: 'Shalini Jayawardena', type: 'Counselling', appointmentDate: '2026-10-07', appointmentTime: '11:15:00', status: 'Scheduled', assignedClinicianName: 'Ms. I. Silva' },
  { id: 'a4', patientId: '4', patientNumber: 'POM-2026-0037', patientName: 'Kasun Silva', type: 'Review', appointmentDate: '2026-10-08', appointmentTime: '08:45:00', status: 'Scheduled', assignedClinicianName: 'Dr. A. Fernando' },
]
const page = <T,>(items: T[], pageNumber = 1, pageSize = 20): PagedResponse<T> => ({ items, page: pageNumber, pageSize, totalCount: items.length, totalPages: 1 })
export const demoApi = { patients: (search?: string) => page(search ? patients.filter((p) => `${p.fullName} ${p.patientNumber}`.toLowerCase().includes(search.toLowerCase())) : patients), appointments: () => page(appointments) }
