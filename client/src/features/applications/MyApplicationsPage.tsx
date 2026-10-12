import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowRight,
  CheckCircle,
  ClockCounterClockwise,
  FilePlus,
  XCircle,
} from '@phosphor-icons/react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { applicationsApi } from '../../api/applicationsApi'
import type {
  ApplicationResponse,
  ApplicationStatus,
} from '../../api/applications.types'
import { formatCurrency, formatDate, formatPercent } from '../../lib/formatters'
import { getApiErrorMessage } from '../../lib/apiErrorMessage'
import AuthNavigation from '../auth/AuthNavigation'

const statusLabels: Record<ApplicationStatus, string> = {
  Draft: 'Черновик',
  Submitted: 'Подана',
  Scoring: 'Оценка',
  AutoApproved: 'Предварительно одобрена',
  UnderReview: 'На рассмотрении',
  Approved: 'Одобрена',
  Rejected: 'Отклонена',
  Withdrawn: 'Отозвана',
}

const withdrawableStatuses: ApplicationStatus[] = [
  'Draft',
  'Submitted',
  'Scoring',
  'UnderReview',
]
const pageSize = 8

function canWithdraw(status: ApplicationStatus) {
  return withdrawableStatuses.includes(status)
}

function getStatusClass(status: ApplicationStatus) {
  if (status === 'Approved' || status === 'AutoApproved')
    return 'status-approved'
  if (status === 'Rejected' || status === 'Withdrawn') return 'status-closed'
  return 'status-pending'
}

function ApplicationDetails({ applicationId }: { applicationId: string }) {
  const queryClient = useQueryClient()
  const [actionError, setActionError] = useState<string | null>(null)
  const applicationQuery = useQuery({
    queryKey: ['application', applicationId],
    queryFn: () => applicationsApi.getById(applicationId),
  })
  const scheduleQuery = useQuery({
    queryKey: ['application-schedule', applicationId],
    queryFn: () => applicationsApi.getSchedule(applicationId),
    enabled: applicationQuery.data?.status === 'Approved',
  })
  const withdrawApplication = useMutation({
    mutationFn: () => applicationsApi.withdraw(applicationId),
    onSuccess: async (application) => {
      queryClient.setQueryData(['application', applicationId], application)
      await queryClient.invalidateQueries({ queryKey: ['applications'] })
      setActionError(null)
    },
    onError: (error) =>
      setActionError(getApiErrorMessage(error, 'Не удалось отозвать заявку.')),
  })

  if (applicationQuery.isPending) {
    return (
      <div
        className="application-detail-loading"
        aria-label="Загрузка деталей заявки"
      >
        <span className="loading-line" />
        <span className="loading-line" />
      </div>
    )
  }

  if (applicationQuery.isError || !applicationQuery.data) {
    return (
      <div className="application-detail-error" role="alert">
        <p>
          {getApiErrorMessage(
            applicationQuery.error,
            'Не удалось загрузить детали заявки.',
          )}
        </p>
        <button
          className="text-button"
          type="button"
          onClick={() => void applicationQuery.refetch()}
        >
          Повторить
        </button>
      </div>
    )
  }

  const application = applicationQuery.data
  const isWithdrawable = canWithdraw(application.status)

  return (
    <div className="application-detail-content">
      <div className="application-detail-topline">
        <div>
          <p className="section-index">
            ЗАЯВКА {application.id.slice(0, 8).toUpperCase()}
          </p>
          <h3>{application.productName}</h3>
        </div>
        <span className={`status-badge ${getStatusClass(application.status)}`}>
          {statusLabels[application.status]}
        </span>
      </div>

      <dl className="application-metrics">
        <div>
          <dt>Сумма</dt>
          <dd>{formatCurrency(application.amount)}</dd>
        </div>
        <div>
          <dt>Ставка</dt>
          <dd>{formatPercent(application.interestRate)}</dd>
        </div>
        <div>
          <dt>Срок</dt>
          <dd>{application.termMonths} мес.</dd>
        </div>
        <div>
          <dt>Результат оценки</dt>
          <dd>
            {application.score === null ? '—' : `${application.score} / 100`}
          </dd>
        </div>
      </dl>

      <section
        className="detail-section"
        aria-labelledby="scoring-results-title"
      >
        <div className="detail-section-heading">
          <div>
            <p className="section-index">ПРОВЕРКИ</p>
            <h4 id="scoring-results-title">Расшифровка скоринга</h4>
          </div>
          {application.score !== null && (
            <span className="score-mark">
              {application.score}
              <small> / 100</small>
            </span>
          )}
        </div>
        {application.scoringResults.length ? (
          <ul className="scoring-list">
            {application.scoringResults.map((result) => (
              <li key={result.ruleCode}>
                {result.passed ? (
                  <CheckCircle size={20} weight="regular" aria-hidden="true" />
                ) : (
                  <XCircle size={20} weight="regular" aria-hidden="true" />
                )}
                <div>
                  <strong>{result.ruleName}</strong>
                  <p>{result.details}</p>
                </div>
                <span
                  className={
                    result.points < 0
                      ? 'score-points score-points-negative'
                      : 'score-points'
                  }
                >
                  {result.points > 0 ? '+' : ''}
                  {result.points}
                </span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="detail-muted">
            Результаты скоринга для этой заявки пока недоступны.
          </p>
        )}
      </section>

      <section
        className="detail-section"
        aria-labelledby="application-schedule-title"
      >
        <div className="detail-section-heading">
          <div>
            <p className="section-index">ПЛАТЕЖИ</p>
            <h4 id="application-schedule-title">График платежей</h4>
          </div>
        </div>
        {application.status !== 'Approved' ? (
          <p className="detail-muted">
            График будет доступен после окончательного одобрения заявки.
          </p>
        ) : scheduleQuery.isPending ? (
          <div
            className="application-detail-loading"
            aria-label="Загрузка графика"
          >
            <span className="loading-line" />
            <span className="loading-line loading-line-short" />
          </div>
        ) : scheduleQuery.isError ? (
          <div className="application-detail-error" role="alert">
            <p>
              {getApiErrorMessage(
                scheduleQuery.error,
                'Не удалось загрузить график платежей.',
              )}
            </p>
            <button
              className="text-button"
              type="button"
              onClick={() => void scheduleQuery.refetch()}
            >
              Повторить
            </button>
          </div>
        ) : scheduleQuery.data ? (
          <>
            <div className="schedule-summary-inline">
              <div>
                <span>Первый платёж</span>
                <strong>
                  {formatDate(scheduleQuery.data.firstPaymentDate)}
                </strong>
              </div>
              <div>
                <span>Всего выплат</span>
                <strong>
                  {formatCurrency(scheduleQuery.data.schedule.totalPaid)}
                </strong>
              </div>
              <div>
                <span>Переплата</span>
                <strong>
                  {formatCurrency(scheduleQuery.data.schedule.overpayment)}
                </strong>
              </div>
            </div>
            <div className="preview-table-wrap">
              <table className="preview-table">
                <thead>
                  <tr>
                    <th>№</th>
                    <th>Дата</th>
                    <th>Платёж</th>
                    <th>Остаток</th>
                  </tr>
                </thead>
                <tbody>
                  {scheduleQuery.data.schedule.payments
                    .slice(0, 5)
                    .map((payment) => (
                      <tr key={payment.number}>
                        <td>{String(payment.number).padStart(2, '0')}</td>
                        <td>{formatDate(payment.date)}</td>
                        <td>{formatCurrency(payment.payment)}</td>
                        <td>{formatCurrency(payment.remainingBalance)}</td>
                      </tr>
                    ))}
                </tbody>
              </table>
              {scheduleQuery.data.schedule.payments.length > 5 && (
                <p className="field-hint">
                  Показаны первые 5 платежей из полного графика.
                </p>
              )}
            </div>
          </>
        ) : null}
      </section>

      <section
        className="detail-section status-history-section"
        aria-labelledby="application-history-title"
      >
        <div className="detail-section-heading">
          <div>
            <p className="section-index">ИСТОРИЯ</p>
            <h4 id="application-history-title">Изменения статуса</h4>
          </div>
          <ClockCounterClockwise
            size={20}
            weight="regular"
            aria-hidden="true"
          />
        </div>
        <ol className="status-history-list">
          {application.statusHistory.map((entry, index) => (
            <li key={`${entry.changedAt}-${entry.toStatus}-${index}`}>
              <span className="history-marker" />
              <div>
                <strong>{statusLabels[entry.toStatus]}</strong>
                <time>{formatDate(entry.changedAt)}</time>
                {entry.comment && <p>{entry.comment}</p>}
              </div>
            </li>
          ))}
        </ol>
      </section>

      {actionError && (
        <p className="form-server-error" role="alert">
          {actionError}
        </p>
      )}
      {isWithdrawable && (
        <div className="application-detail-actions">
          <button
            className="secondary-button danger-button"
            type="button"
            disabled={withdrawApplication.isPending}
            onClick={() => {
              if (
                window.confirm('Отозвать заявку? Это действие нельзя отменить.')
              )
                void withdrawApplication.mutateAsync()
            }}
          >
            {withdrawApplication.isPending
              ? 'Отзыв заявки…'
              : 'Отозвать заявку'}
          </button>
        </div>
      )}
    </div>
  )
}

export default function MyApplicationsPage() {
  const [page, setPage] = useState(1)
  const [selectedApplicationId, setSelectedApplicationId] = useState<
    string | null
  >(null)
  const applicationsQuery = useQuery({
    queryKey: ['applications', page],
    queryFn: () => applicationsApi.getPage(page, pageSize),
  })
  const totalPages = applicationsQuery.data
    ? Math.max(1, Math.ceil(applicationsQuery.data.totalCount / pageSize))
    : 1

  return (
    <main className="account-page">
      <AuthNavigation />
      <header className="page-header applications-page-header">
        <div>
          <p className="eyebrow">Личный кабинет · 03</p>
          <h1>Мои заявки</h1>
          <p className="page-description">
            Следите за статусом, результатами оценки и графиком платежей.
          </p>
        </div>
        <Link className="primary-button primary-link" to="/applications/new">
          <FilePlus size={18} weight="regular" aria-hidden="true" /> Новая
          заявка
        </Link>
      </header>

      {applicationsQuery.isPending ? (
        <section
          className="account-panel account-loading"
          aria-label="Загрузка заявок"
        >
          <span className="loading-line" />
          <span className="loading-line" />
          <span className="loading-line loading-line-short" />
        </section>
      ) : applicationsQuery.isError ? (
        <section className="account-panel account-message" role="alert">
          <h2>Не удалось загрузить заявки</h2>
          <p>
            {getApiErrorMessage(applicationsQuery.error, 'Попробуйте ещё раз.')}
          </p>
          <button
            className="secondary-button"
            type="button"
            onClick={() => void applicationsQuery.refetch()}
          >
            Повторить
          </button>
        </section>
      ) : applicationsQuery.data.totalCount === 0 ? (
        <section className="account-panel applications-empty">
          <div className="empty-mark">
            <FilePlus size={23} weight="regular" aria-hidden="true" />
          </div>
          <p className="section-index">ПОКА НЕТ ЗАЯВОК</p>
          <h2>Начните с выбора продукта</h2>
          <p>
            Заполните условия кредита — перед отправкой покажем предварительный
            график.
          </p>
          <Link className="primary-button primary-link" to="/applications/new">
            Подать заявку{' '}
            <ArrowRight size={18} weight="regular" aria-hidden="true" />
          </Link>
        </section>
      ) : (
        <div className="applications-workspace">
          <section
            className="account-panel applications-list-panel"
            aria-label="Список заявок"
          >
            <div className="applications-list-heading">
              <div>
                <p className="section-index">ВСЕГО ЗАЯВОК</p>
                <h2>{applicationsQuery.data.totalCount}</h2>
              </div>
              <span className="applications-page-number">
                {page} / {totalPages}
              </span>
            </div>
            <div className="applications-list">
              {applicationsQuery.data.items.map(
                (application: ApplicationResponse) => {
                  const isSelected = selectedApplicationId === application.id
                  return (
                    <button
                      className={`application-row ${isSelected ? 'application-row-selected' : ''}`}
                      type="button"
                      key={application.id}
                      aria-expanded={isSelected}
                      onClick={() =>
                        setSelectedApplicationId(
                          isSelected ? null : application.id,
                        )
                      }
                    >
                      <span
                        className={`application-status-dot ${getStatusClass(application.status)}`}
                        aria-hidden="true"
                      />
                      <span className="application-row-main">
                        <strong>{application.productName}</strong>
                        <span>
                          {formatDate(application.createdAt)} ·{' '}
                          {formatCurrency(application.amount)}
                        </span>
                      </span>
                      <span
                        className={`status-badge ${getStatusClass(application.status)}`}
                      >
                        {statusLabels[application.status]}
                      </span>
                      <ArrowRight
                        className="application-row-arrow"
                        size={18}
                        weight="regular"
                        aria-hidden="true"
                      />
                    </button>
                  )
                },
              )}
            </div>
            <div className="applications-pagination">
              <button
                className="text-button"
                type="button"
                disabled={page <= 1}
                onClick={() => setPage((current) => current - 1)}
              >
                Назад
              </button>
              <span>
                Страница {page} из {totalPages}
              </span>
              <button
                className="text-button"
                type="button"
                disabled={page >= totalPages}
                onClick={() => setPage((current) => current + 1)}
              >
                Далее
              </button>
            </div>
          </section>

          <aside
            className="account-panel application-detail-panel"
            aria-label="Детали заявки"
          >
            {selectedApplicationId ? (
              <ApplicationDetails
                key={selectedApplicationId}
                applicationId={selectedApplicationId}
              />
            ) : (
              <div className="application-detail-empty">
                <div className="empty-mark">
                  <ArrowRight size={22} weight="regular" aria-hidden="true" />
                </div>
                <p className="section-index">ДЕТАЛИ ЗАЯВКИ</p>
                <h2>Выберите заявку</h2>
                <p>
                  Здесь появятся история статусов, расшифровка скоринга и
                  график.
                </p>
              </div>
            )}
          </aside>
        </div>
      )}
    </main>
  )
}
