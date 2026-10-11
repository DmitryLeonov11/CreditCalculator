import { Link } from 'react-router-dom'
import { useAuth } from './useAuth'
import './auth.css'

export default function AuthNavigation() {
  const { session, signOut } = useAuth()

  return (
    <nav className="account-nav" aria-label="Учётная запись">
      {session ? (
        <>
          {session.role === 'Employee' && <Link to="/employee">Кабинет сотрудника</Link>}
          <button type="button" onClick={signOut}>Выйти</button>
        </>
      ) : (
        <>
          <Link to="/login">Войти</Link>
          <Link className="account-nav-primary" to="/register">Создать аккаунт</Link>
        </>
      )}
    </nav>
  )
}
