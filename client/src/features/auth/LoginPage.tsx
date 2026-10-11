import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { useAuth } from './useAuth'
import { authApi } from './authApi'
import { getAuthErrorMessage } from './authErrors'
import AuthPageLayout from './AuthPageLayout'

const loginSchema = z.object({
  email: z.string().trim().min(1, 'Укажите email.').email('Некорректный формат email.'),
  password: z.string().min(1, 'Укажите пароль.'),
})

type LoginFormValues = z.infer<typeof loginSchema>

export default function LoginPage() {
  const [serverError, setServerError] = useState<string | null>(null)
  const { signIn } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  async function onSubmit(values: LoginFormValues) {
    setServerError(null)
    try {
      const tokens = await authApi.login(values)
      signIn(tokens)
      const state = location.state as { from?: { pathname?: string } } | null
      navigate(state?.from?.pathname ?? '/', { replace: true })
    } catch (error) {
      setServerError(getAuthErrorMessage(error, 'Не удалось войти. Проверьте email и пароль.'))
    }
  }

  return (
    <AuthPageLayout
      title="Войти в аккаунт"
      description="Введите данные, указанные при регистрации."
      footer={<>Нет аккаунта? <Link to="/register">Зарегистрироваться</Link></>}
    >
      <form className="auth-form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <label className="auth-field" htmlFor="login-email">
          Email
          <input
            id="login-email"
            type="email"
            autoComplete="email"
            aria-invalid={Boolean(errors.email)}
            aria-describedby={errors.email ? 'login-email-error' : undefined}
            {...register('email')}
          />
          {errors.email && <span className="field-error" id="login-email-error">{errors.email.message}</span>}
        </label>
        <label className="auth-field" htmlFor="login-password">
          Пароль
          <input
            id="login-password"
            type="password"
            autoComplete="current-password"
            aria-invalid={Boolean(errors.password)}
            aria-describedby={errors.password ? 'login-password-error' : undefined}
            {...register('password')}
          />
          {errors.password && <span className="field-error" id="login-password-error">{errors.password.message}</span>}
        </label>
        {serverError && <p className="auth-error" role="alert">{serverError}</p>}
        <button className="auth-submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Вход…' : 'Войти'}
        </button>
      </form>
    </AuthPageLayout>
  )
}
