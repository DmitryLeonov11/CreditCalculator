import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { profileApi } from '../../api/profileApi'
import type { ProfileResponse } from '../../api/profile.types'
import AuthNavigation from '../auth/AuthNavigation'
import { formatCurrency, formatDate } from '../../lib/formatters'

const profileSchema = z.object({
  fullName: z
    .string()
    .trim()
    .min(1, 'Укажите ФИО.')
    .max(200, 'Не более 200 символов.'),
  birthDate: z.string().refine((value) => {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false
    const birthDate = new Date(`${value}T00:00:00Z`)
    if (
      Number.isNaN(birthDate.getTime()) ||
      birthDate.toISOString().slice(0, 10) !== value
    )
      return false
    const today = new Date()
    let age = today.getUTCFullYear() - birthDate.getUTCFullYear()
    const birthdayHasPassed =
      today.getUTCMonth() > birthDate.getUTCMonth() ||
      (today.getUTCMonth() === birthDate.getUTCMonth() &&
        today.getUTCDate() >= birthDate.getUTCDate())
    if (!birthdayHasPassed) age -= 1
    return age >= 18 && age < 100
  }, 'Возраст должен быть от 18 до 100 лет.'),
  gender: z.enum(['Male', 'Female'], { error: 'Выберите пол.' }),
  monthlyIncome: z.number().min(0, 'Доход не может быть отрицательным.'),
  employmentMonths: z.number().int().min(0).max(600, 'Не более 600 месяцев.'),
  existingMonthlyPayments: z
    .number()
    .min(0, 'Платежи не могут быть отрицательными.'),
  dependents: z.number().int().min(0).max(20, 'Укажите значение от 0 до 20.'),
})

type ProfileFormValues = z.infer<typeof profileSchema>

const emptyProfile: ProfileFormValues = {
  fullName: '',
  birthDate: '',
  gender: 'Female',
  monthlyIncome: 0,
  employmentMonths: 0,
  existingMonthlyPayments: 0,
  dependents: 0,
}

function toFormValues(profile: ProfileResponse): ProfileFormValues {
  return {
    fullName: profile.fullName,
    birthDate: profile.birthDate,
    gender: profile.gender ?? 'Female',
    monthlyIncome: profile.monthlyIncome,
    employmentMonths: profile.employmentMonths,
    existingMonthlyPayments: profile.existingMonthlyPayments,
    dependents: profile.dependents,
  }
}

function getErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ detail?: string; title?: string }>(error)) {
    return error.response?.data.detail ?? error.response?.data.title ?? fallback
  }
  return fallback
}

export default function ProfilePage() {
  const queryClient = useQueryClient()
  const [isEditing, setIsEditing] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const profileQuery = useQuery({
    queryKey: ['profile'],
    queryFn: profileApi.get,
    retry: (failureCount, error) =>
      !(isAxiosError(error) && error.response?.status === 404) &&
      failureCount < 2,
  })
  const saveProfile = useMutation({
    mutationFn: profileApi.update,
    onSuccess: async (profile) => {
      await queryClient.setQueryData(['profile'], profile)
      setIsEditing(false)
      setSaved(true)
      setServerError(null)
    },
    onError: (error) => {
      setSaved(false)
      setServerError(getErrorMessage(error, 'Не удалось сохранить анкету.'))
    },
  })
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: emptyProfile,
  })

  useEffect(() => {
    if (profileQuery.data && !isEditing) reset(toFormValues(profileQuery.data))
  }, [isEditing, profileQuery.data, reset])

  const profileMissing =
    isAxiosError(profileQuery.error) &&
    profileQuery.error.response?.status === 404
  const isNewProfile = profileMissing && !profileQuery.isFetching

  function startEditing() {
    setSaved(false)
    setServerError(null)
    reset(profileQuery.data ? toFormValues(profileQuery.data) : emptyProfile)
    setIsEditing(true)
  }

  function cancelEditing() {
    reset(profileQuery.data ? toFormValues(profileQuery.data) : emptyProfile)
    setServerError(null)
    setIsEditing(false)
  }

  async function onSubmit(values: ProfileFormValues) {
    setSaved(false)
    setServerError(null)
    await saveProfile.mutateAsync(values)
  }

  return (
    <main className="account-page">
      <AuthNavigation />
      <header className="page-header">
        <p className="eyebrow">Личный кабинет · 01</p>
        <h1>Анкета клиента</h1>
        <p className="page-description">
          Данные анкеты используются для предварительной оценки заявки.
          Паспортные данные не запрашиваются.
        </p>
      </header>

      {profileQuery.isPending ? (
        <section
          className="account-panel account-loading"
          aria-label="Загрузка анкеты"
        >
          <span className="loading-line" />
          <span className="loading-line loading-line-short" />
          <span className="loading-line" />
        </section>
      ) : profileQuery.isError && !profileMissing ? (
        <section className="account-panel account-message" role="alert">
          <h2>Не удалось загрузить анкету</h2>
          <p>
            {getErrorMessage(
              profileQuery.error,
              'Проверьте подключение и попробуйте ещё раз.',
            )}
          </p>
          <button
            className="secondary-button"
            type="button"
            onClick={() => void profileQuery.refetch()}
          >
            Повторить
          </button>
        </section>
      ) : isEditing || isNewProfile ? (
        <section
          className="account-panel profile-editor"
          aria-labelledby="profile-form-title"
        >
          <div className="account-panel-heading">
            <div>
              <p className="section-index">
                {isNewProfile ? 'ПЕРВЫЙ ШАГ' : 'ДАННЫЕ КЛИЕНТА'}
              </p>
              <h2 id="profile-form-title">
                {isNewProfile ? 'Заполните анкету' : 'Изменить данные'}
              </h2>
            </div>
            {profileQuery.data && (
              <button
                className="text-button"
                type="button"
                onClick={cancelEditing}
              >
                Отменить
              </button>
            )}
          </div>
          <form
            className="profile-form"
            onSubmit={handleSubmit(onSubmit)}
            noValidate
          >
            <label className="account-field" htmlFor="profile-name">
              ФИО
              <input
                id="profile-name"
                autoComplete="name"
                aria-invalid={Boolean(errors.fullName)}
                {...register('fullName')}
              />
              {errors.fullName && (
                <span className="field-error">{errors.fullName.message}</span>
              )}
            </label>
            <div className="account-field-grid">
              <label className="account-field" htmlFor="profile-birth-date">
                Дата рождения
                <input
                  id="profile-birth-date"
                  type="date"
                  aria-invalid={Boolean(errors.birthDate)}
                  {...register('birthDate')}
                />
                {errors.birthDate && (
                  <span className="field-error">
                    {errors.birthDate.message}
                  </span>
                )}
              </label>
              <label className="account-field" htmlFor="profile-gender">
                Пол
                <select
                  id="profile-gender"
                  aria-invalid={Boolean(errors.gender)}
                  {...register('gender')}
                >
                  <option value="Female">Женский</option>
                  <option value="Male">Мужской</option>
                </select>
                {errors.gender && (
                  <span className="field-error">{errors.gender.message}</span>
                )}
              </label>
              <label className="account-field" htmlFor="profile-income">
                Ежемесячный доход, BYN
                <input
                  id="profile-income"
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  aria-invalid={Boolean(errors.monthlyIncome)}
                  {...register('monthlyIncome', { valueAsNumber: true })}
                />
                {errors.monthlyIncome && (
                  <span className="field-error">
                    {errors.monthlyIncome.message}
                  </span>
                )}
              </label>
              <label className="account-field" htmlFor="profile-employment">
                Стаж на текущем месте, месяцев
                <input
                  id="profile-employment"
                  type="number"
                  min="0"
                  max="600"
                  step="1"
                  aria-invalid={Boolean(errors.employmentMonths)}
                  {...register('employmentMonths', { valueAsNumber: true })}
                />
                {errors.employmentMonths && (
                  <span className="field-error">
                    {errors.employmentMonths.message}
                  </span>
                )}
              </label>
              <label className="account-field" htmlFor="profile-payments">
                Текущие платежи в месяц, BYN
                <input
                  id="profile-payments"
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  aria-invalid={Boolean(errors.existingMonthlyPayments)}
                  {...register('existingMonthlyPayments', {
                    valueAsNumber: true,
                  })}
                />
                {errors.existingMonthlyPayments && (
                  <span className="field-error">
                    {errors.existingMonthlyPayments.message}
                  </span>
                )}
              </label>
              <label className="account-field" htmlFor="profile-dependents">
                Иждивенцы
                <input
                  id="profile-dependents"
                  type="number"
                  min="0"
                  max="20"
                  step="1"
                  aria-invalid={Boolean(errors.dependents)}
                  {...register('dependents', { valueAsNumber: true })}
                />
                {errors.dependents && (
                  <span className="field-error">
                    {errors.dependents.message}
                  </span>
                )}
              </label>
            </div>
            {serverError && (
              <p className="form-server-error" role="alert">
                {serverError}
              </p>
            )}
            {saved && (
              <p className="form-success" role="status">
                Анкета сохранена.
              </p>
            )}
            <div className="form-actions">
              {profileQuery.data && (
                <button
                  className="secondary-button"
                  type="button"
                  onClick={cancelEditing}
                >
                  Отмена
                </button>
              )}
              <button
                className="primary-button"
                type="submit"
                disabled={saveProfile.isPending || (!isDirty && !isNewProfile)}
              >
                {saveProfile.isPending ? 'Сохранение…' : 'Сохранить анкету'}
              </button>
            </div>
          </form>
        </section>
      ) : profileQuery.data ? (
        <section
          className="account-panel profile-details"
          aria-labelledby="profile-details-title"
        >
          <div className="account-panel-heading">
            <div>
              <p className="section-index">
                ОБНОВЛЕНО {formatDate(profileQuery.data.updatedAt)}
              </p>
              <h2 id="profile-details-title">Личные и финансовые данные</h2>
            </div>
            <button
              className="secondary-button"
              type="button"
              onClick={startEditing}
            >
              Редактировать
            </button>
          </div>
          <dl className="profile-detail-grid">
            <div>
              <dt>ФИО</dt>
              <dd>{profileQuery.data.fullName}</dd>
            </div>
            <div>
              <dt>Дата рождения</dt>
              <dd>{formatDate(profileQuery.data.birthDate)}</dd>
            </div>
            <div>
              <dt>Пол</dt>
              <dd>
                {profileQuery.data.gender === 'Male' ? 'Мужской' : 'Женский'}
              </dd>
            </div>
            <div>
              <dt>Ежемесячный доход</dt>
              <dd>{formatCurrency(profileQuery.data.monthlyIncome)}</dd>
            </div>
            <div>
              <dt>Стаж на текущем месте</dt>
              <dd>{profileQuery.data.employmentMonths} мес.</dd>
            </div>
            <div>
              <dt>Текущие платежи</dt>
              <dd>
                {formatCurrency(profileQuery.data.existingMonthlyPayments)}
              </dd>
            </div>
            <div>
              <dt>Иждивенцы</dt>
              <dd>{profileQuery.data.dependents}</dd>
            </div>
          </dl>
        </section>
      ) : null}
    </main>
  )
}
