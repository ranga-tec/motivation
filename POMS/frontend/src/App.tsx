import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/AppShell'
import { AuthCallback, ProtectedRoute, SignInPage } from './auth/AuthProvider'
import { AppointmentsPage } from './pages/AppointmentsPage'
import { DashboardPage } from './pages/DashboardPage'
import { PatientsPage } from './pages/PatientsPage'
import { PatientRegistrationPage } from './pages/PatientRegistrationPage'
import { PatientDetailPage } from './pages/PatientDetailPage'
import { ClinicalRecordsPage } from './pages/ClinicalRecordsPage'
import './App.css'

export default function App() {
  return (
    <Routes>
      <Route path="/signin" element={<SignInPage />} />
      <Route path="/auth/callback" element={<AuthCallback />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route index element={<DashboardPage />} />
          <Route path="patients" element={<PatientsPage />} />
          <Route path="patients/new" element={<PatientRegistrationPage />} />
          <Route path="patients/:id" element={<PatientDetailPage />} />
          <Route path="records/:episodeId" element={<ClinicalRecordsPage />} />
          <Route path="appointments" element={<AppointmentsPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
