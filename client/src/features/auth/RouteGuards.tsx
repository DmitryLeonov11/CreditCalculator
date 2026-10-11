import { Link, Navigate, Outlet, useLocation } from 'react-router-dom'
import type { UserRole } from './authTypes'
import { useAuth } from './useAuth'

export function RequireAuth() {
  const { session } = useAuth()
  const location = useLocation()

  if (!session) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return <Outlet />
}

export function RequireRole({ role }: { role: UserRole }) {
  const { session } = useAuth()

  if (session?.role !== role) {
    return (
      <main className="route-message">
        <p className="eyebrow">Доступ ограничен</p>
        <h1>У вас нет доступа к этому разделу</h1>
        <p>Кабинет сотрудника доступен только пользователям с соответствующей ролью.</p>
        <Link to="/">Вернуться к калькулятору</Link>
      </main>
    )
  }

  return <Outlet />
}
