import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import LoginPage from './features/auth/LoginPage'
import RegisterPage from './features/auth/RegisterPage'
import { RequireAuth, RequireRole } from './features/auth/RouteGuards'
import type { UserRole } from './features/auth/authTypes'
import ApplicationSubmissionPage from './features/applications/ApplicationSubmissionPage'
import MyApplicationsPage from './features/applications/MyApplicationsPage'
import CalculatorPage from './features/calculator/CalculatorPage'
import ProfilePage from './features/profile/ProfilePage'

const EmployeeCabinetPage = lazy(
  () => import('./features/employee/EmployeeCabinetPage'),
)

function App() {
  const clientRole: UserRole = 'Client'
  const employeeRole: UserRole = 'Employee'

  return (
    <Routes>
      <Route path="/" element={<CalculatorPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<RequireRole role={clientRole} />}>
          <Route path="/profile" element={<ProfilePage />} />
          <Route
            path="/applications/new"
            element={<ApplicationSubmissionPage />}
          />
          <Route path="/applications" element={<MyApplicationsPage />} />
        </Route>
        <Route element={<RequireRole role={employeeRole} />}>
          <Route
            path="/employee"
            element={
              <Suspense
                fallback={
                  <main className="route-message">
                    Загружаем рабочую область…
                  </main>
                }
              >
                <EmployeeCabinetPage />
              </Suspense>
            }
          />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
