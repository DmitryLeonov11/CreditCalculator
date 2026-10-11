import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { z } from 'zod'
import { authApi } from './authApi'
import { getAuthErrorMessage } from './authErrors'
import AuthPageLayout from './AuthPageLayout'

const registerSchema = z
  .object({
    email: z
      .string()
      .trim()
      .min(1, 'Укажите email.')
      .email('Некорректный формат email.')
      .max(256, 'Email не должен быть длиннее 256 символов.'),
    password: z
      .string()
      .min(8, 'Пароль должен содержать не менее 8 символов.')
      .max(128, 'Пароль не должен быть длиннее 128 символов.')
      .regex(/^(?=.*\p{L})(?=.*\d).+$/u, 'Пароль должен содержать хотя бы одну букву и одну цифру.'),
    confirmPassword: z.string(),
    personalDataConsent: z.boolean().refine(
      (value) => value,
      'Для регистрации необходимо согласие на обработку персональных данных.',
    ),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Пароли не совпадают.',
  })

type RegisterFormValues = z.infer<typeof registerSchema>

export default function RegisterPage() {
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null)
  const [serverError, setServerError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: '',
      personalDataConsent: false,
    },
  })

  async function onSubmit(values: RegisterFormValues) {
    setServerError(null)
    try {
      await authApi.register({
        email: values.email,
        password: values.password,
        personalDataConsent: values.personalDataConsent,
      })
      setSubmittedEmail(values.email)
    } catch (error) {
      setServerError(
        getAuthErrorMessage(error, 'Не удалось отправить запрос на регистрацию.'),
      )
    }
  }

  if (submittedEmail) {
    return (
      <AuthPageLayout
        title="Проверьте почту"
        description={`Если регистрация доступна, письмо с подтверждением отправлено на ${submittedEmail}.`}
        footer={<>Уже подтвердили email? <Link to="/login">Войти</Link></>}
      >
        <div className="auth-notice" role="status">
          Перейдите по ссылке из письма, чтобы подтвердить адрес и войти в личный кабинет.
        </div>
      </AuthPageLayout>
    )
  }

  return (
    <AuthPageLayout
      title="Создать аккаунт"
      description="Зарегистрируйтесь, чтобы подать заявку и следить за её статусом."
      footer={<>Уже есть аккаунт? <Link to="/login">Войти</Link></>}
    >
      <form className="auth-form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <label className="auth-field" htmlFor="register-email">
          Email
          <input
            id="register-email"
            type="email"
            autoComplete="email"
            aria-invalid={Boolean(errors.email)}
            aria-describedby={errors.email ? 'register-email-error' : undefined}
            {...register('email')}
          />
          {errors.email && <span className="field-error" id="register-email-error">{errors.email.message}</span>}
        </label>
        <label className="auth-field" htmlFor="register-password">
          Пароль
          <input
            id="register-password"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(errors.password)}
            aria-describedby={errors.password ? 'register-password-error' : undefined}
            {...register('password')}
          />
          {errors.password && <span className="field-error" id="register-password-error">{errors.password.message}</span>}
        </label>
        <label className="auth-field" htmlFor="register-confirm-password">
          Повторите пароль
          <input
            id="register-confirm-password"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(errors.confirmPassword)}
            aria-describedby={errors.confirmPassword ? 'register-confirm-password-error' : undefined}
            {...register('confirmPassword')}
          />
          {errors.confirmPassword && <span className="field-error" id="register-confirm-password-error">{errors.confirmPassword.message}</span>}
        </label>
        <label className="auth-consent">
          <input type="checkbox" aria-invalid={Boolean(errors.personalDataConsent)} {...register('personalDataConsent')} />
          <span>Согласен на обработку персональных данных</span>
        </label>
        {errors.personalDataConsent && <span className="field-error">{errors.personalDataConsent.message}</span>}
        {serverError && <p className="auth-error" role="alert">{serverError}</p>}
        <button className="auth-submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Отправка…' : 'Зарегистрироваться'}
        </button>
      </form>
    </AuthPageLayout>
  )
}
