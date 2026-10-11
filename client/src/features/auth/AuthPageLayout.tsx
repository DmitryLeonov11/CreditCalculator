import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import './auth.css'

type AuthPageLayoutProps = {
  title: string
  description: string
  children: ReactNode
  footer: ReactNode
}

export default function AuthPageLayout({
  title,
  description,
  children,
  footer,
}: AuthPageLayoutProps) {
  return (
    <main className="auth-shell">
      <Link className="auth-brand" to="/">
        Кредитный калькулятор
      </Link>
      <section className="auth-panel" aria-labelledby="auth-title">
        <p className="eyebrow">Личный кабинет</p>
        <h1 id="auth-title">{title}</h1>
        <p className="auth-description">{description}</p>
        {children}
        <p className="auth-footer">{footer}</p>
      </section>
    </main>
  )
}
