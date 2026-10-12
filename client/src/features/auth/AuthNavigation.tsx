import { Link, NavLink } from 'react-router-dom'
import { useAuth } from './useAuth'
import './auth.css'

export default function AuthNavigation() {
  const { session, signOut } = useAuth()

  return (
    <nav className="account-nav" aria-label="Учётная запись">
      {session ? (
        <>
          <div className="account-nav-links">
            {session.role === 'Employee' ? (
              <Link to="/employee">Кабинет сотрудника</Link>
            ) : (
              <>
                <NavLink to="/profile">Анкета</NavLink>
                <NavLink to="/applications">Мои заявки</NavLink>
                <NavLink className="account-nav-primary" to="/applications/new">
                  Новая заявка
                </NavLink>
              </>
            )}
          </div>
          <button type="button" onClick={signOut}>
            Выйти
          </button>
        </>
      ) : (
        <>
          <Link to="/login">Войти</Link>
          <Link className="account-nav-primary" to="/register">
            Создать аккаунт
          </Link>
        </>
      )}
    </nav>
  )
}
