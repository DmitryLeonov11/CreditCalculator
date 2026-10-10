import { lazy, Suspense, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import type { PaymentType, ScheduleRequest } from '../../api/calculator.types'
import { formatCurrency, formatDate, formatPercent } from '../../lib/formatters'
import { useScheduleQuery } from './useCalculatorQueries'

const EarlyRepaymentPanel = lazy(() => import('./EarlyRepaymentPanel'))
const ScheduleCharts = lazy(() => import('./ScheduleCharts'))

type CalculatorForm = {
  amount: string
  termMonths: string
  annualRatePercent: string
  paymentType: PaymentType
  firstPaymentDate: string
}

type FormErrors = Partial<Record<keyof CalculatorForm, string>>

function getFirstDayOfNextMonth() {
  const date = new Date()
  date.setMonth(date.getMonth() + 1, 1)
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function validateForm(form: CalculatorForm): FormErrors {
  const errors: FormErrors = {}
  const amount = Number(form.amount)
  const termMonths = Number(form.termMonths)
  const annualRatePercent = Number(form.annualRatePercent)
  const paymentDate = new Date(`${form.firstPaymentDate}T00:00:00`)
  const [year, month, day] = form.firstPaymentDate.split('-').map(Number)
  const isValidDate =
    paymentDate.getFullYear() === year &&
    paymentDate.getMonth() === month - 1 &&
    paymentDate.getDate() === day

  if (
    !form.amount.trim() ||
    !Number.isFinite(amount) ||
    amount < 500 ||
    amount > 300_000
  ) {
    errors.amount = 'Укажите сумму от 500 до 300 000 BYN.'
  }

  if (
    !form.termMonths.trim() ||
    !Number.isInteger(termMonths) ||
    !Number.isFinite(termMonths) ||
    termMonths < 1 ||
    termMonths > 240
  ) {
    errors.termMonths = 'Срок должен быть от 1 до 240 месяцев.'
  }

  if (
    !form.annualRatePercent.trim() ||
    !Number.isFinite(annualRatePercent) ||
    annualRatePercent < 0.1 ||
    annualRatePercent > 60
  ) {
    errors.annualRatePercent = 'Ставка должна быть от 0,1% до 60%.'
  }

  if (
    !/^\d{4}-\d{2}-\d{2}$/.test(form.firstPaymentDate) ||
    Number.isNaN(paymentDate.getTime()) ||
    !isValidDate
  ) {
    errors.firstPaymentDate = 'Укажите корректную дату первого платежа.'
  }

  return errors
}

const initialForm: CalculatorForm = {
  amount: '20000',
  termMonths: '24',
  annualRatePercent: '18',
  paymentType: 'Annuity',
  firstPaymentDate: getFirstDayOfNextMonth(),
}

export default function CalculatorPage() {
  const [form, setForm] = useState(initialForm)
  const [debouncedForm, setDebouncedForm] = useState(initialForm)
  const errors = validateForm(form)
  const isValid = Object.keys(errors).length === 0
  const isSettled = form === debouncedForm
  const request: ScheduleRequest | null =
    isValid && isSettled
      ? {
          amount: Number(form.amount),
          termMonths: Number(form.termMonths),
          annualRatePercent: Number(form.annualRatePercent),
          paymentType: form.paymentType,
          firstPaymentDate: form.firstPaymentDate,
        }
      : null
  const scheduleQuery = useScheduleQuery(request)

  useEffect(() => {
    const timeoutId = window.setTimeout(() => setDebouncedForm(form), 450)
    return () => window.clearTimeout(timeoutId)
  }, [form])

  function updateField<K extends keyof CalculatorForm>(
    field: K,
    value: CalculatorForm[K],
  ) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setDebouncedForm(form)
  }

  return (
    <main className="calculator-shell">
      <header className="page-header">
        <p className="eyebrow">Кредитный калькулятор</p>
        <h1>Рассчитайте кредит до подачи заявки</h1>
        <p className="page-description">
          Настройте параметры и получите помесячный график платежей с полной
          стоимостью кредита.
        </p>
      </header>

      <div className="calculator-layout">
        <section className="form-panel" aria-labelledby="form-title">
          <div className="section-heading">
            <span className="section-index">01</span>
            <div>
              <h2 id="form-title">Параметры кредита</h2>
              <p>Расчёт обновится после небольшой паузы ввода.</p>
            </div>
          </div>

          <form className="calculator-form" onSubmit={handleSubmit}>
            <label className="field" htmlFor="amount">
              <span>Сумма кредита</span>
              <span className="input-wrap">
                <input
                  id="amount"
                  name="amount"
                  type="number"
                  min="500"
                  max="300000"
                  step="100"
                  inputMode="decimal"
                  value={form.amount}
                  onChange={(event) =>
                    updateField('amount', event.target.value)
                  }
                  aria-invalid={Boolean(errors.amount)}
                  aria-describedby={errors.amount ? 'amount-error' : undefined}
                  required
                />
                <span className="input-suffix">BYN</span>
              </span>
              {errors.amount && (
                <span className="field-error" id="amount-error" role="alert">
                  {errors.amount}
                </span>
              )}
            </label>

            <div className="field-row">
              <label className="field" htmlFor="termMonths">
                <span>Срок кредита</span>
                <span className="input-wrap">
                  <input
                    id="termMonths"
                    name="termMonths"
                    type="number"
                    min="1"
                    max="240"
                    step="1"
                    inputMode="numeric"
                    value={form.termMonths}
                    onChange={(event) =>
                      updateField('termMonths', event.target.value)
                    }
                    aria-invalid={Boolean(errors.termMonths)}
                    aria-describedby={
                      errors.termMonths ? 'term-error' : undefined
                    }
                    required
                  />
                  <span className="input-suffix">мес.</span>
                </span>
                {errors.termMonths && (
                  <span className="field-error" id="term-error" role="alert">
                    {errors.termMonths}
                  </span>
                )}
              </label>

              <label className="field" htmlFor="annualRatePercent">
                <span>Годовая ставка</span>
                <span className="input-wrap">
                  <input
                    id="annualRatePercent"
                    name="annualRatePercent"
                    type="number"
                    min="0.1"
                    max="60"
                    step="0.1"
                    inputMode="decimal"
                    value={form.annualRatePercent}
                    onChange={(event) =>
                      updateField('annualRatePercent', event.target.value)
                    }
                    aria-invalid={Boolean(errors.annualRatePercent)}
                    aria-describedby={
                      errors.annualRatePercent ? 'rate-error' : undefined
                    }
                    required
                  />
                  <span className="input-suffix">%</span>
                </span>
                {errors.annualRatePercent && (
                  <span className="field-error" id="rate-error" role="alert">
                    {errors.annualRatePercent}
                  </span>
                )}
              </label>
            </div>

            <label className="field" htmlFor="paymentType">
              <span>Тип платежа</span>
              <select
                id="paymentType"
                name="paymentType"
                value={form.paymentType}
                onChange={(event) =>
                  updateField('paymentType', event.target.value as PaymentType)
                }
              >
                <option value="Annuity">Аннуитетный — одинаковый платёж</option>
                <option value="Differentiated">
                  Дифференцированный — платёж уменьшается
                </option>
              </select>
            </label>

            <label className="field" htmlFor="firstPaymentDate">
              <span>Дата первого платежа</span>
              <input
                id="firstPaymentDate"
                name="firstPaymentDate"
                type="date"
                value={form.firstPaymentDate}
                onChange={(event) =>
                  updateField('firstPaymentDate', event.target.value)
                }
                aria-invalid={Boolean(errors.firstPaymentDate)}
                aria-describedby={
                  errors.firstPaymentDate ? 'date-error' : undefined
                }
                required
              />
              {errors.firstPaymentDate && (
                <span className="field-error" id="date-error" role="alert">
                  {errors.firstPaymentDate}
                </span>
              )}
            </label>

            <button className="calculate-button" type="submit">
              Рассчитать график
              <span aria-hidden="true">→</span>
            </button>
          </form>
        </section>

        <section className="result-panel" aria-labelledby="result-title">
          <div className="section-heading result-heading">
            <span className="section-index">02</span>
            <div>
              <h2 id="result-title">Ваш расчёт</h2>
              <p>Предварительный график платежей</p>
            </div>
            {scheduleQuery.isFetching && (
              <span className="loading-indicator" aria-label="Загрузка" />
            )}
          </div>

          {scheduleQuery.isError ? (
            <div className="result-message error-message" role="alert">
              <strong>Не удалось выполнить расчёт</strong>
              <span>
                Проверьте соединение с API и повторите попытку.{' '}
                {scheduleQuery.error instanceof Error
                  ? scheduleQuery.error.message
                  : ''}
              </span>
              <button
                className="text-button"
                type="button"
                onClick={() => void scheduleQuery.refetch()}
              >
                Повторить запрос
              </button>
            </div>
          ) : !isValid ? (
            <div className="result-message">
              <strong>Проверьте параметры кредита</strong>
              <span>Исправьте значения в форме, чтобы выполнить расчёт.</span>
            </div>
          ) : scheduleQuery.isPending || !isSettled ? (
            <div className="result-message" aria-live="polite">
              <div className="skeleton-line skeleton-title" />
              <div className="skeleton-line" />
              <div className="skeleton-line skeleton-short" />
              <span className="result-hint">
                {isSettled
                  ? 'Рассчитываем график платежей…'
                  : 'Пересчитаем после завершения ввода.'}
              </span>
            </div>
          ) : scheduleQuery.data ? (
            <div className="schedule-result">
              <div className="summary-grid" aria-label="Итоги расчёта">
                <div className="summary-item summary-primary">
                  <span>Первый платёж</span>
                  <strong>
                    {formatCurrency(scheduleQuery.data.monthlyPayment)}
                  </strong>
                </div>
                <div className="summary-item">
                  <span>Переплата</span>
                  <strong>
                    {formatCurrency(scheduleQuery.data.overpayment)}
                  </strong>
                </div>
                <div className="summary-item">
                  <span>Всего к возврату</span>
                  <strong>
                    {formatCurrency(scheduleQuery.data.totalPaid)}
                  </strong>
                </div>
                <div className="summary-item">
                  <span>Эффективная ставка (ППС)</span>
                  <strong>
                    {formatPercent(scheduleQuery.data.effectiveRate)}
                  </strong>
                </div>
              </div>

              <div className="schedule-heading">
                <div>
                  <h3>График платежей</h3>
                  <span>
                    Всего платежей: {scheduleQuery.data.payments.length}
                  </span>
                </div>
                <span className="schedule-type">
                  {form.paymentType === 'Annuity'
                    ? 'Аннуитетный'
                    : 'Дифференцированный'}
                </span>
              </div>

              <Suspense
                fallback={
                  <div
                    className="chart-loading"
                    aria-label="Загрузка графиков"
                  />
                }
              >
                <ScheduleCharts payments={scheduleQuery.data.payments} />
              </Suspense>

              <div className="payment-table-wrap">
                <table className="payment-table">
                  <caption className="visually-hidden">
                    Помесячный график платежей по кредиту
                  </caption>
                  <thead>
                    <tr>
                      <th scope="col">№</th>
                      <th scope="col">Дата</th>
                      <th scope="col">Платёж</th>
                      <th scope="col">Проценты</th>
                      <th scope="col">Основной долг</th>
                      <th scope="col">Остаток</th>
                    </tr>
                  </thead>
                  <tbody>
                    {scheduleQuery.data.payments.map((payment) => (
                      <tr key={payment.number}>
                        <td>{payment.number}</td>
                        <td>{formatDate(payment.date)}</td>
                        <td>{formatCurrency(payment.payment)}</td>
                        <td>{formatCurrency(payment.interestPart)}</td>
                        <td>{formatCurrency(payment.principalPart)}</td>
                        <td>{formatCurrency(payment.remainingBalance)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ) : (
            <div className="result-message">
              <strong>Задайте параметры кредита</strong>
              <span>Результат расчёта появится в этом разделе.</span>
            </div>
          )}
        </section>
      </div>

      <Suspense
        fallback={
          <div
            className="chart-loading early-loading"
            aria-label="Загрузка формы"
          />
        }
      >
        <EarlyRepaymentPanel
          key={`${form.amount}-${form.termMonths}-${form.annualRatePercent}-${form.firstPaymentDate}`}
          amount={Number(form.amount)}
          termMonths={Number(form.termMonths)}
          annualRatePercent={Number(form.annualRatePercent)}
          firstPaymentDate={form.firstPaymentDate}
          enabled={isValid && isSettled}
        />
      </Suspense>

      <footer className="page-footer">
        Расчёт носит информационный характер и не является публичной офертой.
      </footer>
    </main>
  )
}
