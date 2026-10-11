import { Navigate, Route, Routes } from 'react-router-dom'
import AuthNavigation from './features/auth/AuthNavigation'
import LoginPage from './features/auth/LoginPage'
import RegisterPage from './features/auth/RegisterPage'
import { RequireAuth, RequireRole } from './features/auth/RouteGuards'
import type { UserRole } from './features/auth/authTypes'
import CalculatorPage from './features/calculator/CalculatorPage'

function EmployeePlaceholder() {
  return (
    <main className="route-message">
      <AuthNavigation />
      <p className="eyebrow">Рабочая область</p>
      <h1>Кабинет сотрудника</h1>
      <p>Раздел заявок сотрудника будет доступен на следующем этапе.</p>
    </main>
  )
}

function App() {
  const employeeRole: UserRole = 'Employee'

  return (
    <Routes>
      <Route path="/" element={<CalculatorPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<RequireRole role={employeeRole} />}>
          <Route path="/employee" element={<EmployeePlaceholder />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
