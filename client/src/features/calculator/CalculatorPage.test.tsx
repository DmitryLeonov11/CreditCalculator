import { fireEvent, render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { App, ConfigProvider } from 'antd'
import type {
  ScheduleRequest,
  ScheduleResponse,
} from '../../api/calculator.types'
import CalculatorPage from './CalculatorPage'

/**
 * Stubbed schedule returned by the mocked `calculatorApi`. Two variants keep
 * the test deterministic: one for the default amount (20 000 BYN) and one for
 * a reduced amount (5 000 BYN) so we can assert that the schedule actually
 * recomputes after the input debounce settles.
 */
const defaultSchedule: ScheduleResponse = {
  monthlyPayment: 946.06,
  totalPaid: 23_905.44,
  overpayment: 3_905.44,
  effectiveRate: 18.76,
  payments: [
    {
      number: 1,
      date: '2026-02-01',
      payment: 946.06,
      interestPart: 300.0,
      principalPart: 646.06,
      remainingBalance: 19_353.94,
    },
    {
      number: 2,
      date: '2026-03-01',
      payment: 946.06,
      interestPart: 290.0,
      principalPart: 656.06,
      remainingBalance: 18_707.88,
    },
    {
      number: 3,
      date: '2026-04-01',
      payment: 946.06,
      interestPart: 280.0,
      principalPart: 666.06,
      remainingBalance: 0.0,
    },
  ],
}

const reducedSchedule: ScheduleResponse = {
  monthlyPayment: 236.51,
  totalPaid: 6_017.44,
  overpayment: 1_017.44,
  effectiveRate: 18.76,
  payments: [
    {
      number: 1,
      date: '2026-02-01',
      payment: 236.51,
      interestPart: 75.0,
      principalPart: 161.51,
      remainingBalance: 4_838.49,
    },
    {
      number: 2,
      date: '2026-03-01',
      payment: 236.51,
      interestPart: 72.0,
      principalPart: 164.51,
      remainingBalance: 4_673.98,
    },
    {
      number: 3,
      date: '2026-04-01',
      payment: 236.51,
      interestPart: 69.0,
      principalPart: 167.51,
      remainingBalance: 0.0,
    },
  ],
}

const { mockGetSchedule } = vi.hoisted(() => ({
  mockGetSchedule: vi.fn(),
}))

vi.mock('../../api/calculatorApi', () => ({
  calculatorApi: {
    getSchedule: mockGetSchedule,
    getEarlyRepaymentComparison: vi.fn(),
    downloadSchedulePdf: vi.fn(),
    downloadScheduleExcel: vi.fn(),
  },
}))

// The lazy sub-components pull in recharts, react-router and auth context that
// are irrelevant to the calculator form behaviour under test - stub them out.
vi.mock('./ScheduleCharts', () => ({
  default: () => null,
}))
vi.mock('./EarlyRepaymentPanel', () => ({
  default: () => null,
}))
vi.mock('../auth/AuthNavigation', () => ({
  default: () => null,
}))

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: Infinity },
    },
  })
}

function renderPage() {
  const queryClient = createQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <ConfigProvider>
        <App>
          <CalculatorPage />
        </App>
      </ConfigProvider>
    </QueryClientProvider>,
  )
}

/** Advance fake timers and flush pending microtasks (resolves in-flight queries). */
async function flush(ms = 600) {
  await vi.advanceTimersByTimeAsync(ms)
}

beforeEach(() => {
  vi.useFakeTimers()
  mockGetSchedule.mockImplementation(
    async (request: ScheduleRequest): Promise<ScheduleResponse> =>
      request.amount === 5000 ? reducedSchedule : defaultSchedule,
  )
})

afterEach(() => {
  vi.useRealTimers()
  mockGetSchedule.mockReset()
})

describe('CalculatorPage', () => {
  it('renders calculation results for the default valid form', async () => {
    renderPage()
    await flush()

    expect(
      screen.getByRole('heading', { name: 'Ваш расчёт' }),
    ).toBeInTheDocument()
    expect(
      screen.getByText(/Помесячный график платежей по кредиту/),
    ).toBeInTheDocument()
    expect(screen.getByText(/Всего платежей:\s*3/)).toBeInTheDocument()

    // Summary row - first payment uses the formatted annuity amount.
    expect(screen.getByText('Первый платёж')).toBeInTheDocument()
    expect(screen.getByText(/946,06/)).toBeInTheDocument()
    expect(screen.getByText('Эффективная ставка (ППС)')).toBeInTheDocument()
    expect(screen.getByText('18,76%')).toBeInTheDocument()

    // Table header columns render in the declared order.
    ;['№', 'Дата', 'Платёж', 'Проценты', 'Основной долг', 'Остаток'].forEach(
      (header) => {
        expect(screen.getByText(header)).toBeInTheDocument()
      },
    )

    // First payment row.
    expect(screen.getByText('1')).toBeInTheDocument()
    expect(screen.getByText('01.02.2026')).toBeInTheDocument()
  })

  it('shows a validation error when the amount is empty', async () => {
    renderPage()
    await flush()

    const amount = screen.getByLabelText('Сумма кредита')
    fireEvent.change(amount, { target: { value: '' } })
    await flush(100)

    expect(
      screen.getByText('Укажите сумму от 500 до 300 000 BYN.'),
    ).toBeInTheDocument()
    expect(amount).toHaveAttribute('aria-invalid', 'true')
  })

  it('shows a validation error when the term is out of range', async () => {
    renderPage()
    await flush()

    const term = screen.getByLabelText('Срок кредита')
    fireEvent.change(term, { target: { value: '300' } })
    await flush(100)

    expect(
      screen.getByText('Срок должен быть от 1 до 240 месяцев.'),
    ).toBeInTheDocument()
    expect(term).toHaveAttribute('aria-invalid', 'true')
  })

  it('shows a validation error when the rate is out of range', async () => {
    renderPage()
    await flush()

    const rate = screen.getByLabelText('Годовая ставка')
    fireEvent.change(rate, { target: { value: '70' } })
    await flush(100)

    expect(
      screen.getByText('Ставка должна быть от 0,1% до 60%.'),
    ).toBeInTheDocument()
    expect(rate).toHaveAttribute('aria-invalid', 'true')
  })

  it('shows a validation error when the first payment date is invalid', async () => {
    renderPage()
    await flush()

    const date = screen.getByLabelText('Дата первого платежа')
    fireEvent.change(date, { target: { value: '2026-02-30' } })
    await flush(100)

    expect(
      screen.getByText('Укажите корректную дату первого платежа.'),
    ).toBeInTheDocument()
    expect(date).toHaveAttribute('aria-invalid', 'true')
  })

  it('recomputes the schedule after the input debounce settles', async () => {
    renderPage()
    await flush()

    const amount = screen.getByLabelText('Сумма кредита')
    fireEvent.change(amount, { target: { value: '5000' } })

    // Before the 450 ms debounce settles, the request is null so the table is
    // still hidden and the "recalculate after input pause" hint is shown.
    await flush(100)
    expect(
      screen.getByText('Пересчитаем после завершения ввода.'),
    ).toBeInTheDocument()
    expect(
      screen.queryByText(/Помесячный график платежей по кредиту/),
    ).not.toBeInTheDocument()

    // After the debounce settles and the refetched schedule resolves, the
    // table reappears with the recalculated payment for 5 000 BYN.
    await flush(600)
    expect(
      screen.getByText(/Помесячный график платежей по кредиту/),
    ).toBeInTheDocument()
    expect(screen.getByText('236,51')).toBeInTheDocument()
    expect(screen.queryByText('946,06')).not.toBeInTheDocument()
  })
})
