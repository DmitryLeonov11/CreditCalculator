import { useState } from 'react'
import type { FormEvent } from 'react'
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type {
  EarlyRepaymentMode,
  EarlyRepaymentRequest,
} from '../../api/calculator.types'
import { formatCurrency, formatDate, formatPercent } from '../../lib/formatters'
import { useEarlyRepaymentQuery } from './useCalculatorQueries'

type EarlyRepaymentPanelProps = {
  amount: number
  termMonths: number
  annualRatePercent: number
  firstPaymentDate: string
  enabled: boolean
}

type EarlyPaymentRow = {
  id: number
  month: string
  amount: string
}

const initialPayments: EarlyPaymentRow[] = [
  { id: 1, month: '6', amount: '1000' },
]

export default function EarlyRepaymentPanel({
  amount,
  termMonths,
  annualRatePercent,
  firstPaymentDate,
  enabled,
}: EarlyRepaymentPanelProps) {
  const [mode, setMode] = useState<EarlyRepaymentMode>('ReduceTerm')
  const [payments, setPayments] = useState(initialPayments)
  const [request, setRequest] = useState<EarlyRepaymentRequest | null>(null)
  const query = useEarlyRepaymentQuery(request)

  function updatePayment(id: number, field: 'month' | 'amount', value: string) {
    setPayments((current) =>
      current.map((payment) =>
        payment.id === id ? { ...payment, [field]: value } : payment,
      ),
    )
  }

  function addPayment() {
    setRequest(null)
    setPayments((current) => [
      ...current,
      {
        id: Math.max(0, ...current.map((payment) => payment.id)) + 1,
        month: '',
        amount: '',
      },
    ])
  }

  function removePayment(id: number) {
    setPayments((current) => current.filter((payment) => payment.id !== id))
    setRequest(null)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!enabled || payments.length === 0) {
      return
    }

    setRequest({
      amount,
      termMonths,
      annualRatePercent,
      firstPaymentDate,
      mode,
      earlyRepayments: payments.map((payment) => ({
        month: Number(payment.month),
        amount: Number(payment.amount),
      })),
    })
  }

  const original = query.data?.original
  const recalculated = query.data?.withEarlyRepayments
  const overpaymentSavings =
    original && recalculated
      ? original.overpayment - recalculated.overpayment
      : 0
  const termSavings =
    original && recalculated
      ? original.payments.length - recalculated.payments.length
      : 0
  const comparisonData =
    original && recalculated
      ? Array.from(
          {
            length: Math.max(
              original.payments.length,
              recalculated.payments.length,
            ),
          },
          (_, index) => ({
            month: index + 1,
            originalPayment: original.payments[index]?.payment ?? 0,
            recalculatedPayment: recalculated.payments[index]?.payment ?? 0,
          }),
        )
      : []

  return (
    <section className="early-repayment-panel" aria-labelledby="early-title">
      <div className="section-heading">
        <span className="section-index">03</span>
        <div>
          <h2 id="early-title">Досрочное погашение</h2>
          <p>Сравните переплату и срок после дополнительных платежей</p>
        </div>
      </div>

      <form className="early-repayment-form" onSubmit={handleSubmit}>
        <label className="field" htmlFor="earlyMode">
          <span>Как пересчитать кредит</span>
          <select
            id="earlyMode"
            value={mode}
            onChange={(event) => {
              setMode(event.target.value as EarlyRepaymentMode)
              setRequest(null)
            }}
          >
            <option value="ReduceTerm">
              Сократить срок — сохранить платёж
            </option>
            <option value="ReducePayment">
              Сократить платёж — сохранить срок
            </option>
          </select>
        </label>

        <div className="early-payments-list">
          <div className="early-payments-heading">
            <div>
              <h3>Дополнительные платежи</h3>
              <p>Месяц отсчитывается от первого платежа по кредиту.</p>
            </div>
            <button
              className="secondary-button"
              type="button"
              onClick={addPayment}
            >
              Добавить платёж
            </button>
          </div>

          {payments.length === 0 ? (
            <p className="early-empty-state">
              Добавьте хотя бы один досрочный платёж для сравнения.
            </p>
          ) : (
            payments.map((payment, index) => (
              <div className="early-payment-row" key={payment.id}>
                <label className="field" htmlFor={`early-month-${payment.id}`}>
                  <span>Платёж {index + 1}, месяц</span>
                  <input
                    id={`early-month-${payment.id}`}
                    type="number"
                    min="1"
                    max={termMonths}
                    step="1"
                    value={payment.month}
                    onChange={(event) =>
                      updatePayment(payment.id, 'month', event.target.value)
                    }
                    onInput={() => setRequest(null)}
                    required
                  />
                </label>
                <label className="field" htmlFor={`early-amount-${payment.id}`}>
                  <span>Сумма досрочного платежа</span>
                  <span className="input-wrap">
                    <input
                      id={`early-amount-${payment.id}`}
                      type="number"
                      min="0.01"
                      max={amount}
                      step="0.01"
                      inputMode="decimal"
                      value={payment.amount}
                      onChange={(event) =>
                        updatePayment(payment.id, 'amount', event.target.value)
                      }
                      onInput={() => setRequest(null)}
                      required
                    />
                    <span className="input-suffix">BYN</span>
                  </span>
                </label>
                <button
                  className="remove-payment-button"
                  type="button"
                  aria-label={`Удалить досрочный платёж ${index + 1}`}
                  onClick={() => removePayment(payment.id)}
                >
                  Удалить
                </button>
              </div>
            ))
          )}
        </div>

        <p className="early-repayment-note">
          Сравнение строится по аннуитетному графику. Сумма досрочного погашения
          применяется в указанном месяце после планового платежа.
        </p>

        <button
          className="calculate-button"
          type="submit"
          disabled={!enabled || payments.length === 0}
        >
          Сравнить графики
          <span aria-hidden="true">→</span>
        </button>
      </form>

      {!enabled ? (
        <p className="early-query-message">
          Сначала задайте корректные параметры основного кредита.
        </p>
      ) : null}

      {query.isError ? (
        <div className="result-message error-message" role="alert">
          <strong>Не удалось сравнить графики</strong>
          <span>
            {query.error instanceof Error
              ? query.error.message
              : 'Проверьте данные и попробуйте ещё раз.'}
          </span>
          <button
            className="text-button"
            type="button"
            onClick={() => void query.refetch()}
          >
            Повторить запрос
          </button>
        </div>
      ) : null}

      {query.isFetching ? (
        <div className="result-message" aria-live="polite">
          <div className="skeleton-line skeleton-title" />
          <div className="skeleton-line" />
          <span className="result-hint">Сравниваем графики платежей…</span>
        </div>
      ) : null}

      {original && recalculated ? (
        <div className="early-comparison" aria-live="polite">
          <div className="comparison-heading">
            <div>
              <h3>Результат сравнения</h3>
              <p>
                {mode === 'ReduceTerm'
                  ? 'Размер планового платежа сохраняется'
                  : 'Срок кредита сохраняется, плановый платёж уменьшается'}
              </p>
            </div>
            <span className="comparison-savings">
              {overpaymentSavings >= 0 ? 'Экономия' : 'Переплата'}{' '}
              {formatCurrency(Math.abs(overpaymentSavings))}
            </span>
          </div>

          <div className="comparison-grid">
            <div className="comparison-item">
              <span>Переплата без досрочного погашения</span>
              <strong>{formatCurrency(original.overpayment)}</strong>
            </div>
            <div className="comparison-item comparison-highlight">
              <span>Переплата после пересчёта</span>
              <strong>{formatCurrency(recalculated.overpayment)}</strong>
            </div>
            <div className="comparison-item">
              <span>Платёж до погашения</span>
              <strong>{formatCurrency(original.monthlyPayment)}</strong>
            </div>
            <div className="comparison-item">
              <span>Первый платёж после пересчёта</span>
              <strong>{formatCurrency(recalculated.monthlyPayment)}</strong>
            </div>
            <div className="comparison-item">
              <span>Срок без досрочного погашения</span>
              <strong>{original.payments.length} мес.</strong>
            </div>
            <div className="comparison-item">
              <span>Срок после пересчёта</span>
              <strong>{recalculated.payments.length} мес.</strong>
            </div>
          </div>

          {mode === 'ReduceTerm' && termSavings > 0 ? (
            <p className="term-savings">
              Срок сократился на {termSavings} мес.
            </p>
          ) : null}

          <div className="comparison-chart">
            <h4>Плановые платежи: до и после</h4>
            <div
              className="chart-container"
              role="group"
              aria-label="Сравнение ежемесячных платежей до и после досрочного погашения"
            >
              <ResponsiveContainer width="100%" height={230}>
                <LineChart
                  data={comparisonData}
                  margin={{ top: 10, right: 12, bottom: 0, left: 8 }}
                >
                  <CartesianGrid stroke="#e8eee8" strokeDasharray="3 5" />
                  <XAxis
                    dataKey="month"
                    tickLine={false}
                    axisLine={false}
                    minTickGap={26}
                    tick={{ fill: '#78827b', fontSize: 11 }}
                  />
                  <YAxis
                    width={82}
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(value: number) => formatCurrency(value)}
                    tick={{ fill: '#78827b', fontSize: 10 }}
                  />
                  <Tooltip
                    labelFormatter={(label) => `Месяц ${label}`}
                    formatter={(value, name) => [
                      formatCurrency(Number(value ?? 0)),
                      name === 'originalPayment'
                        ? 'Без досрочного погашения'
                        : 'После пересчёта',
                    ]}
                    contentStyle={{
                      border: '1px solid #dfe7df',
                      borderRadius: 8,
                    }}
                  />
                  <Line
                    type="monotone"
                    dataKey="originalPayment"
                    name="originalPayment"
                    stroke="#9aa69d"
                    strokeWidth={2}
                    dot={false}
                  />
                  <Line
                    type="monotone"
                    dataKey="recalculatedPayment"
                    name="recalculatedPayment"
                    stroke="#357553"
                    strokeWidth={2.5}
                    dot={false}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div className="comparison-schedule-grid">
            <div>
              <h4>Исходный график</h4>
              <p>
                {formatDate(original.payments[0]?.date ?? firstPaymentDate)} —{' '}
                {formatDate(original.payments.at(-1)?.date ?? firstPaymentDate)}
              </p>
            </div>
            <div>
              <h4>После досрочного погашения</h4>
              <p>
                {formatDate(recalculated.payments[0]?.date ?? firstPaymentDate)}{' '}
                —{' '}
                {formatDate(
                  recalculated.payments.at(-1)?.date ?? firstPaymentDate,
                )}
              </p>
            </div>
          </div>

          <p className="comparison-effective-rate">
            ППС: {formatPercent(original.effectiveRate)} →{' '}
            {formatPercent(recalculated.effectiveRate)}
          </p>
        </div>
      ) : null}
    </section>
  )
}
