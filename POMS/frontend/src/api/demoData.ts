import type { Appointment, CreateAppointmentRequest, CreatePatientRequest, Episode, PagedResponse, PatientDetail, PatientRegistrationOptions, PatientSummary, SaveEpisodeRequest } from './types'

const patients: PatientSummary[] = [
  { id: '1', patientNumber: 'POM-2026-0042', fullName: 'Nimali Perera', nameWithInitials: 'N. Perera', dateOfBirth: '1987-04-18', sex: 'Female', category: 'Adult', centerId: 1, centerName: 'Colombo Central Clinic', registrationDate: '2026-09-28' },
  { id: '2', patientNumber: 'POM-2026-0041', fullName: 'Dinesh Fernando', nameWithInitials: 'D. Fernando', dateOfBirth: '1992-11-03', sex: 'Male', category: 'Adult', centerId: 1, centerName: 'Colombo Central Clinic', registrationDate: '2026-09-27' },
  { id: '3', patientNumber: 'POM-2026-0039', fullName: 'Shalini Jayawardena', nameWithInitials: 'S. Jayawardena', dateOfBirth: '1979-01-22', sex: 'Female', category: 'Adult', centerId: 2, centerName: 'Kandy Outreach Centre', registrationDate: '2026-09-25' },
  { id: '4', patientNumber: 'POM-2026-0037', fullName: 'Kasun Silva', nameWithInitials: 'K. Silva', dateOfBirth: '2001-08-14', sex: 'Male', category: 'Young Adult', centerId: 1, centerName: 'Colombo Central Clinic', registrationDate: '2026-09-23' },
  { id: '5', patientNumber: 'POM-2026-0035', fullName: 'Fathima Rizna', nameWithInitials: 'F. Rizna', dateOfBirth: '1995-06-07', sex: 'Female', category: 'Adult', centerId: 3, centerName: 'Galle Community Clinic', registrationDate: '2026-09-20' },
]
let appointments: Appointment[] = [
  { id: 'a1', patientId: '1', patientNumber: 'POM-2026-0042', patientName: 'Nimali Perera', type: 'Assessment', appointmentDate: '2026-10-07', appointmentTime: '09:00:00', status: 'Scheduled', assignedClinicianName: 'Dr. A. Fernando' },
  { id: 'a2', patientId: '2', patientNumber: 'POM-2026-0041', patientName: 'Dinesh Fernando', type: 'FollowUp', appointmentDate: '2026-10-07', appointmentTime: '10:30:00', status: 'Completed', assignedClinicianName: 'Dr. S. Perera' },
  { id: 'a3', patientId: '3', patientNumber: 'POM-2026-0039', patientName: 'Shalini Jayawardena', type: 'Fitting', appointmentDate: '2026-10-07', appointmentTime: '11:15:00', status: 'Scheduled', assignedClinicianName: 'Ms. I. Silva' },
  { id: 'a4', patientId: '4', patientNumber: 'POM-2026-0037', patientName: 'Kasun Silva', type: 'Delivery', appointmentDate: '2026-10-08', appointmentTime: '08:45:00', status: 'Scheduled', assignedClinicianName: 'Dr. A. Fernando' },
]
const page = <T,>(items: T[], pageNumber = 1, pageSize = 20): PagedResponse<T> => ({ items, page: pageNumber, pageSize, totalCount: items.length, totalPages: 1 })
const patientDetail = (patient: PatientSummary): PatientDetail => ({ ...patient, identificationType: 'NationalId', identificationNumber: `NIC-${patient.id.padStart(4, '0')}`, address1: '12 Example Road', province: 'Western', district: 'Colombo', city: 'Colombo', email: `${patient.fullName.toLowerCase().replaceAll(' ', '.')}@example.test`, center: patient.centerName, assignedClinicianName: 'Dr. A. Fernando', contacts: [{ id: `c-${patient.id}`, telephoneNumber: '0771234567' }] })
let episodes: Episode[] = [{ id: 'e1', patientId: '1', patientNumber: patients[0].patientNumber, patientName: patients[0].fullName, centerId: 1, centerName: 'Colombo Central Clinic', status: 'Active', recordDate: '2026-09-28', recordTime: '09:30:00', remarks: 'Initial prosthetic assessment pathway.', isRestricted: false, assessmentCount: 1, fittingCount: 0, deliveryCount: 0, followUpCount: 0, documentCount: 1 }]
export const demoApi = { patients: (search?: string) => page(search ? patients.filter((p) => `${p.fullName} ${p.patientNumber}`.toLowerCase().includes(search.toLowerCase())) : patients), patient: (id: string) => patientDetail(patients.find((item) => item.id === id)!), appointments: () => page(appointments), episodes: (patientId: string) => episodes.filter((item) => item.patientId === patientId) }

export const demoRegistrationOptions: PatientRegistrationOptions = {
  provinces: [{ id: 1, name: 'Western' }, { id: 2, name: 'Central' }, { id: 3, name: 'Southern' }],
  districts: [{ id: 1, name: 'Colombo', parentId: 1 }, { id: 2, name: 'Gampaha', parentId: 1 }, { id: 3, name: 'Kandy', parentId: 2 }, { id: 4, name: 'Galle', parentId: 3 }],
  cities: [{ id: 1, name: 'Colombo', parentId: 1 }, { id: 2, name: 'Dehiwala', parentId: 1 }, { id: 3, name: 'Ragama', parentId: 2 }, { id: 4, name: 'Kandy', parentId: 3 }],
  centers: [{ id: 1, name: 'Colombo Central Clinic', parentId: 1 }, { id: 2, name: 'Kandy Outreach Centre', parentId: 3 }, { id: 3, name: 'Galle Community Clinic', parentId: 4 }],
  referralSources: [{ id: 1, name: 'Hospital referral' }, { id: 2, name: 'Self referral' }],
  assignees: [{ userId: 'demo-clinician', displayName: 'Dr. A. Fernando - Prosthetist (P001)', fullName: 'Dr. A. Fernando', isPreferred: true }],
}

export const createDemoPatient = (request: CreatePatientRequest): PatientDetail => ({
  id: crypto.randomUUID(), patientNumber: '2026/0043', fullName: request.fullName, nameWithInitials: request.nameWithInitials,
  dateOfBirth: request.dateOfBirth, sex: request.sex, category: request.category, centerId: request.centerId,
  centerName: demoRegistrationOptions.centers.find((item) => item.id === request.centerId)?.name ?? '', registrationDate: request.registrationDate,
  identificationType: request.identificationType, identificationNumber: request.identificationNumber ?? '', address1: request.address1,
  province: demoRegistrationOptions.provinces.find((item) => item.id === request.provinceId)?.name ?? '',
  district: demoRegistrationOptions.districts.find((item) => item.id === request.districtId)?.name ?? '', center: demoRegistrationOptions.centers.find((item) => item.id === request.centerId)?.name ?? '', contacts: [],
})

export const demoAppointmentActions = {
  create: (request: CreateAppointmentRequest) => {
    const patient = patients.find((item) => item.id === request.patientId)!
    const item: Appointment = { id: crypto.randomUUID(), patientId: patient.id, patientNumber: patient.patientNumber, patientName: patient.fullName, type: request.type, appointmentDate: request.appointmentDate, appointmentTime: request.appointmentTime, status: 'Scheduled', assignedClinicianUserId: request.assignedClinicianUserId, assignedClinicianName: request.assignedClinicianEntry.split(' - ')[0], notes: request.notes }
    appointments = [...appointments, item]; return item
  },
  complete: (id: string) => updateAppointment(id, { status: 'Completed' }),
  cancel: (id: string) => updateAppointment(id, { status: 'Cancelled' }),
  reschedule: (id: string, appointmentDate: string, appointmentTime?: string) => updateAppointment(id, { appointmentDate, appointmentTime }),
}
function updateAppointment(id: string, changes: Partial<Appointment>) { const item = appointments.find((appointment) => appointment.id === id)!; Object.assign(item, changes); return item }

export const demoEpisodeActions = {
  create: (request: SaveEpisodeRequest) => { const patient = patients.find((item) => item.id === request.patientId)!; const item: Episode = { id: crypto.randomUUID(), patientId: patient.id, patientNumber: patient.patientNumber, patientName: patient.fullName, centerId: request.centerId, centerName: demoRegistrationOptions.centers.find((center) => center.id === request.centerId)?.name ?? '', status: request.status, recordDate: request.recordDate, recordTime: request.recordTime, remarks: request.remarks, isRestricted: request.isRestricted, assessmentCount: 0, fittingCount: 0, deliveryCount: 0, followUpCount: 0, documentCount: 0 }; episodes = [item, ...episodes]; return item },
  update: (id: string, request: SaveEpisodeRequest) => { const item = episodes.find((episode) => episode.id === id)!; Object.assign(item, request, { centerName: demoRegistrationOptions.centers.find((center) => center.id === request.centerId)?.name ?? '' }); return item },
}
