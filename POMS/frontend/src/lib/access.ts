const any = (roles: string[], allowed: string[]) => roles.some((role) => allowed.includes(role))

/** Mirrors the API authorization policies so the UI only offers actions the server will accept. */
export const access = (roles: string[]) => ({
  isAdmin: roles.includes('ADMIN'),
  readPatients: any(roles, ['DATA_ENTRY', 'CLINICIAN', 'MANAGEMENT', 'ADMIN']),
  registerPatients: any(roles, ['DATA_ENTRY', 'ADMIN']),
  clinical: any(roles, ['CLINICIAN', 'ADMIN']),
  reports: any(roles, ['VIEWER', 'MANAGEMENT', 'ADMIN']),
})
