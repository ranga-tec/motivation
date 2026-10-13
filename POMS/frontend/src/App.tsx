import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/AppShell'
import { ProtectedRoute } from './auth/AuthProvider'
import { SignInPage } from './pages/SignInPage'
import { DashboardPage } from './pages/DashboardPage'
import { PatientsPage } from './pages/PatientsPage'
import { PatientRegistrationPage } from './pages/PatientRegistrationPage'
import { PatientDetailPage } from './pages/PatientDetailPage'
import { RecordsPage } from './pages/RecordsPage'
import { ClinicalRecordsPage } from './pages/ClinicalRecordsPage'
import { AppointmentsPage } from './pages/AppointmentsPage'
import { ReportsPage } from './pages/ReportsPage'
import { AdminPage } from './pages/AdminPage'
import { ReferencePage } from './pages/ReferencePage'
import { SettingsPage } from './pages/SettingsPage'
import { NotFoundPage } from './pages/NotFoundPage'

export default function App() {
  return (
    <Routes>
      <Route path="/signin" element={<SignInPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route index element={<DashboardPage />} />
          <Route path="patients" element={<PatientsPage />} />
          <Route path="patients/new" element={<PatientRegistrationPage />} />
          <Route path="patients/:id" element={<PatientDetailPage />} />
          <Route path="patients/:id/edit" element={<PatientRegistrationPage />} />
          <Route path="records" element={<RecordsPage />} />
          <Route path="records/:episodeId" element={<ClinicalRecordsPage />} />
          <Route path="appointments" element={<AppointmentsPage />} />
          <Route path="reports" element={<ReportsPage />} />
          <Route path="admin" element={<Navigate to="/admin/users" replace />} />
          <Route path="admin/users" element={<AdminPage />} />
          <Route path="admin/reference" element={<Navigate to="/admin/reference/centres" replace />} />
          <Route path="admin/reference/:section" element={<ReferencePage />} />
          <Route path="references/:section" element={<ReferencePage />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
