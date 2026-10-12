import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import type { ScheduleRequest } from '../../api/calculator.types'
import { applicationsApi } from '../../api/applicationsApi'
import { profileApi } from '../../api/profileApi'
import { productsApi } from '../../api/productsApi'
import type { CreditProductResponse } from '../../api/products.types'
import { formatCurrency, formatPercent } from '../../lib/formatters'
import { getApiErrorMessage } from '../../lib/apiErrorMessage'
import AuthNavigation from '../auth/AuthNavigation'
import { useScheduleQuery } from '../calculator/useCalculatorQueries'

type ApplicationForm = {
  productId: string
  amount: string
  termMonths: string
  downPaymentAmount: string
}

function getFirstPaymentDate() {
  const date = new Date()
  date.setMonth(date.getMonth() + 1, 1)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-01`
}

function getProductPurpose(purpose: CreditProductResponse['purpose']) {
  return {
    Consumer: 'Потребительский кредит',
    Auto: 'Автокредит',
    Mortgage: 'Ипотека',
  }[purpose]
}

function getFormErrors(
  form: ApplicationForm,
  product: CreditProductResponse | undefined,
) {
  const errors: Partial<Record<keyof ApplicationForm, string>> = {}
  const amount = Number(form.amount)
  const termMonths = Number(form.termMonths)
  const downPaymentAmount = Number(form.downPaymentAmount)

  if (!product) errors.productId = 'Выберите кредитный продукт.'
  if (
    !form.amount ||
    !Number.isFinite(amount) ||
    (product && (amount < product.minAmount || amount > product.maxAmount))
  ) {
    errors.amount = product
      ? `Сумма должна быть от ${formatCurrency(product.minAmount)} до ${formatCurrency(product.maxAmount)}.`
      : 'Укажите сумму кредита.'
  }
  if (
    !form.termMonths ||
    !Number.isInteger(termMonths) ||
    (product &&
      (termMonths < product.minTermMonths ||
        termMonths > product.maxTermMonths))
  ) {
    errors.termMonths = product
      ? `Срок должен быть от ${product.minTermMonths} до ${product.maxTermMonths} месяцев.`
      : 'Укажите срок кредита.'
  }
  if (product?.purpose === 'Mortgage') {
    if (
      !form.downPaymentAmount ||
      !Number.isFinite(downPaymentAmount) ||
      downPaymentAmount < 0
    ) {
      errors.downPaymentAmount = 'Укажите неотрицательный первоначальный взнос.'
    }
  }

  return errors
}

export default function ApplicationSubmissionPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [form, setForm] = useState<ApplicationForm>({
    productId: '',
    amount: '',
    termMonths: '',
    downPaymentAmount: '',
  })
  const [debouncedForm, setDebouncedForm] = useState(form)
  const [serverError, setServerError] = useState<string | null>(null)
  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: productsApi.getActive,
  })
  const profileQuery = useQuery({
    queryKey: ['profile'],
    queryFn: profileApi.get,
  })
  const firstProduct = productsQuery.data?.[0]
  const selectedProductId = form.productId || firstProduct?.id || ''
  const selectedProduct = productsQuery.data?.find(
    (product) => product.id === selectedProductId,
  )
  const activeForm: ApplicationForm =
    form.productId || !selectedProduct
      ? form
      : {
          productId: selectedProduct.id,
          amount: String(selectedProduct.minAmount),
          termMonths: String(selectedProduct.minTermMonths),
          downPaymentAmount: '',
        }
  const errors = getFormErrors(activeForm, selectedProduct)
  const previewProduct = productsQuery.data?.find(
    (product) => product.id === debouncedForm.productId,
  )
  const previewErrors = getFormErrors(debouncedForm, previewProduct)
  const isPreviewCurrent =
    activeForm.productId === debouncedForm.productId &&
    activeForm.amount === debouncedForm.amount &&
    activeForm.termMonths === debouncedForm.termMonths &&
    activeForm.downPaymentAmount === debouncedForm.downPaymentAmount
  const scheduleRequest: ScheduleRequest | null =
    isPreviewCurrent &&
    !profileQuery.isError &&
    previewProduct &&
    Object.keys(previewErrors).length === 0
      ? {
          amount: Number(debouncedForm.amount),
          termMonths: Number(debouncedForm.termMonths),
          annualRatePercent: previewProduct.baseRate,
          paymentType: 'Annuity',
          firstPaymentDate: getFirstPaymentDate(),
        }
      : null
  const scheduleQuery = useScheduleQuery(scheduleRequest)
  const createApplication = useMutation({
    mutationFn: applicationsApi.create,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['applications'] })
      navigate('/applications')
    },
    onError: (error) =>
      setServerError(getApiErrorMessage(error, 'Не удалось отправить заявку.')),
  })
  const profileMissing =
    isAxiosError(profileQuery.error) &&
    profileQuery.error.response?.status === 404
  const canSubmit =
    Object.keys(errors).length === 0 &&
    !profileMissing &&
    Boolean(profileQuery.data)

  const { productId, amount, termMonths, downPaymentAmount } = activeForm

  useEffect(() => {
    const timeoutId = window.setTimeout(
      () =>
        setDebouncedForm({ productId, amount, termMonths, downPaymentAmount }),
      350,
    )
    return () => window.clearTimeout(timeoutId)
  }, [amount, downPaymentAmount, productId, termMonths])

  function selectProduct(product: CreditProductResponse) {
    setForm({
      productId: product.id,
      amount: String(product.minAmount),
      termMonths: String(product.minTermMonths),
      downPaymentAmount: '',
    })
  }

  function updateField(field: keyof ApplicationForm, value: string) {
    setForm({ ...activeForm, [field]: value })
  }

  async function submitApplication(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setServerError(null)
    if (!canSubmit || !selectedProduct) return

    await createApplication.mutateAsync({
      creditProductId: selectedProduct.id,
      amount: Number(activeForm.amount),
      termMonths: Number(activeForm.termMonths),
      ...(selectedProduct.purpose === 'Mortgage'
        ? { downPaymentAmount: Number(activeForm.downPaymentAmount) }
        : {}),
    })
  }

  return (
    <main className="account-page">
      <AuthNavigation />
      <header className="page-header">
        <p className="eyebrow">Личный кабинет · 02</p>
        <h1>Новая заявка</h1>
        <p className="page-description">
          Выберите продукт и параметры кредита. Перед отправкой покажем
          предварительный график платежей.
        </p>
      </header>

      {profileMissing ? (
        <section className="account-panel account-message">
          <p className="section-index">АНКЕТА НЕ ЗАПОЛНЕНА</p>
          <h2>Сначала заполните профиль</h2>
          <p>Анкета нужна для предварительной оценки заявки.</p>
          <Link className="primary-button primary-link" to="/profile">
            Перейти к анкете
          </Link>
        </section>
      ) : profileQuery.isError ? (
        <section className="account-panel account-message" role="alert">
          <h2>Не удалось проверить анкету</h2>
          <p>
            {getApiErrorMessage(
              profileQuery.error,
              'Попробуйте обновить страницу.',
            )}
          </p>
        </section>
      ) : productsQuery.isPending ? (
        <section
          className="account-panel account-loading"
          aria-label="Загрузка кредитных продуктов"
        >
          <span className="loading-line" />
          <span className="loading-line loading-line-short" />
        </section>
      ) : productsQuery.isError ? (
        <section className="account-panel account-message" role="alert">
          <h2>Не удалось загрузить продукты</h2>
          <p>
            {getApiErrorMessage(productsQuery.error, 'Попробуйте ещё раз.')}
          </p>
          <button
            className="secondary-button"
            type="button"
            onClick={() => void productsQuery.refetch()}
          >
            Повторить
          </button>
        </section>
      ) : !productsQuery.data?.length ? (
        <section className="account-panel account-message">
          <h2>Нет доступных продуктов</h2>
          <p>
            Подача заявки станет доступна, когда появятся активные предложения.
          </p>
        </section>
      ) : (
        <div className="application-create-layout">
          <section
            className="account-panel application-editor"
            aria-labelledby="application-form-title"
          >
            <div className="account-panel-heading">
              <div>
                <p className="section-index">ПАРАМЕТРЫ</p>
                <h2 id="application-form-title">Условия кредита</h2>
              </div>
              <span className="rate-chip">
                {selectedProduct
                  ? `${formatPercent(selectedProduct.baseRate)} годовых`
                  : '—'}
              </span>
            </div>
            <form
              className="profile-form"
              onSubmit={submitApplication}
              noValidate
            >
              <label className="account-field" htmlFor="application-product">
                Кредитный продукт
                <select
                  id="application-product"
                  value={activeForm.productId}
                  onChange={(event) => {
                    const product = productsQuery.data.find(
                      (item) => item.id === event.target.value,
                    )
                    if (product) selectProduct(product)
                  }}
                >
                  {productsQuery.data.map((product) => (
                    <option key={product.id} value={product.id}>
                      {product.name} · {getProductPurpose(product.purpose)}
                    </option>
                  ))}
                </select>
                {errors.productId && (
                  <span className="field-error">{errors.productId}</span>
                )}
              </label>
              {selectedProduct && (
                <p className="field-hint">
                  Сумма: {formatCurrency(selectedProduct.minAmount)}–
                  {formatCurrency(selectedProduct.maxAmount)} · срок:{' '}
                  {selectedProduct.minTermMonths}–
                  {selectedProduct.maxTermMonths} мес.
                </p>
              )}
              <div className="account-field-grid">
                <label className="account-field" htmlFor="application-amount">
                  Сумма кредита, BYN
                  <input
                    id="application-amount"
                    type="number"
                    min={selectedProduct?.minAmount}
                    max={selectedProduct?.maxAmount}
                    step="100"
                    inputMode="decimal"
                    value={activeForm.amount}
                    onChange={(event) =>
                      updateField('amount', event.target.value)
                    }
                    aria-invalid={Boolean(errors.amount)}
                  />
                  {errors.amount && (
                    <span className="field-error">{errors.amount}</span>
                  )}
                </label>
                <label className="account-field" htmlFor="application-term">
                  Срок, месяцев
                  <input
                    id="application-term"
                    type="number"
                    min={selectedProduct?.minTermMonths}
                    max={selectedProduct?.maxTermMonths}
                    step="1"
                    value={activeForm.termMonths}
                    onChange={(event) =>
                      updateField('termMonths', event.target.value)
                    }
                    aria-invalid={Boolean(errors.termMonths)}
                  />
                  {errors.termMonths && (
                    <span className="field-error">{errors.termMonths}</span>
                  )}
                </label>
                {selectedProduct?.purpose === 'Mortgage' && (
                  <label
                    className="account-field"
                    htmlFor="application-down-payment"
                  >
                    Первоначальный взнос, BYN
                    <input
                      id="application-down-payment"
                      type="number"
                      min="0"
                      step="100"
                      inputMode="decimal"
                      value={activeForm.downPaymentAmount}
                      onChange={(event) =>
                        updateField('downPaymentAmount', event.target.value)
                      }
                      aria-invalid={Boolean(errors.downPaymentAmount)}
                    />
                    {errors.downPaymentAmount && (
                      <span className="field-error">
                        {errors.downPaymentAmount}
                      </span>
                    )}
                  </label>
                )}
              </div>
              {serverError && (
                <p className="form-server-error" role="alert">
                  {serverError}
                </p>
              )}
              <button
                className="primary-button application-submit"
                type="submit"
                disabled={!canSubmit || createApplication.isPending}
              >
                {createApplication.isPending ? 'Отправка…' : 'Подать заявку'}
              </button>
              <p className="form-footnote">
                Ставка берётся из условий выбранного продукта. После подачи
                заявка пройдёт автоматическую оценку.
              </p>
            </form>
          </section>

          <section
            className="account-panel application-preview"
            aria-labelledby="schedule-preview-title"
          >
            <div className="account-panel-heading">
              <div>
                <p className="section-index">ПРЕДВАРИТЕЛЬНЫЙ РАСЧЁТ</p>
                <h2 id="schedule-preview-title">График платежей</h2>
              </div>
              <span className="preview-payment-count">
                {scheduleQuery.data?.payments.length ?? '—'} мес.
              </span>
            </div>
            {scheduleQuery.isFetching || !isPreviewCurrent ? (
              <div className="preview-loading" aria-label="Пересчёт графика">
                <span className="loading-line" />
                <span className="loading-line" />
                <span className="loading-line loading-line-short" />
              </div>
            ) : scheduleQuery.isError ? (
              <p className="form-server-error" role="alert">
                {getApiErrorMessage(
                  scheduleQuery.error,
                  'Не удалось рассчитать график.',
                )}
              </p>
            ) : scheduleQuery.data ? (
              <>
                <div className="preview-summary">
                  <div>
                    <span>Первый платёж</span>
                    <strong>
                      {formatCurrency(scheduleQuery.data.monthlyPayment)}
                    </strong>
                  </div>
                  <div>
                    <span>Переплата</span>
                    <strong>
                      {formatCurrency(scheduleQuery.data.overpayment)}
                    </strong>
                  </div>
                  <div>
                    <span>ППС</span>
                    <strong>
                      {formatPercent(scheduleQuery.data.effectiveRate)}
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
                      {scheduleQuery.data.payments
                        .slice(0, 6)
                        .map((payment) => (
                          <tr key={payment.number}>
                            <td>{String(payment.number).padStart(2, '0')}</td>
                            <td>{payment.date.slice(0, 10)}</td>
                            <td>{formatCurrency(payment.payment)}</td>
                            <td>{formatCurrency(payment.remainingBalance)}</td>
                          </tr>
                        ))}
                    </tbody>
                  </table>
                  {scheduleQuery.data.payments.length > 6 && (
                    <p className="field-hint">
                      Показаны первые 6 из {scheduleQuery.data.payments.length}{' '}
                      платежей.
                    </p>
                  )}
                </div>
              </>
            ) : (
              <div className="preview-empty">
                <p>Укажите сумму и срок, чтобы увидеть график.</p>
              </div>
            )}
          </section>
        </div>
      )}
    </main>
  )
}
