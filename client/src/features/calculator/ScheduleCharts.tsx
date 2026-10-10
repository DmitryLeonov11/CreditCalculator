import { useMemo } from 'react'
import {
  Area,
  AreaChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { ScheduleItemResponse } from '../../api/calculator.types'
import { formatCurrency, formatDate } from '../../lib/formatters'

type ScheduleChartsProps = {
  payments: ScheduleItemResponse[]
}

export default function ScheduleCharts({ payments }: ScheduleChartsProps) {
  const chartData = useMemo(
    () =>
      payments.map((payment) => ({
        month: payment.number,
        date: payment.date,
        remainingBalance: payment.remainingBalance,
        principalPart: payment.principalPart,
        interestPart: payment.interestPart,
      })),
    [payments],
  )

  return (
    <div className="schedule-charts">
      <section className="chart-section" aria-labelledby="balance-chart-title">
        <div className="chart-heading">
          <div>
            <h3 id="balance-chart-title">Остаток задолженности</h3>
            <p>Как уменьшается долг после каждого платежа</p>
          </div>
          <span>BYN</span>
        </div>
        <div
          className="chart-container"
          role="group"
          aria-label="График остатка задолженности по месяцам"
        >
          <ResponsiveContainer width="100%" height={230}>
            <LineChart
              data={chartData}
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
                formatter={(value) => [
                  formatCurrency(Number(value ?? 0)),
                  'Остаток долга',
                ]}
                labelStyle={{ color: '#28372e', fontWeight: 600 }}
                contentStyle={{
                  border: '1px solid #dfe7df',
                  borderRadius: 8,
                  boxShadow: '0 8px 24px rgba(33, 51, 39, 0.08)',
                }}
              />
              <Line
                type="monotone"
                dataKey="remainingBalance"
                name="Остаток долга"
                stroke="#357553"
                strokeWidth={2.5}
                dot={false}
                activeDot={{ r: 4, fill: '#285d43' }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </section>

      <section
        className="chart-section"
        aria-labelledby="composition-chart-title"
      >
        <div className="chart-heading">
          <div>
            <h3 id="composition-chart-title">Состав ежемесячного платежа</h3>
            <p>Соотношение основного долга и процентов</p>
          </div>
          <span>BYN</span>
        </div>
        <div
          className="chart-container"
          role="group"
          aria-label="Состав платежа по месяцам: проценты и основной долг"
        >
          <ResponsiveContainer width="100%" height={230}>
            <AreaChart
              data={chartData}
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
                labelFormatter={(label, payload) => {
                  const date = payload[0]?.payload?.date as string | undefined
                  return date
                    ? `Платёж ${label} · ${formatDate(date)}`
                    : `Платёж ${label}`
                }}
                formatter={(value, name) => [
                  formatCurrency(Number(value ?? 0)),
                  name === 'principalPart' ? 'Основной долг' : 'Проценты',
                ]}
                labelStyle={{ color: '#28372e', fontWeight: 600 }}
                contentStyle={{
                  border: '1px solid #dfe7df',
                  borderRadius: 8,
                  boxShadow: '0 8px 24px rgba(33, 51, 39, 0.08)',
                }}
              />
              <Legend
                formatter={(value) =>
                  value === 'principalPart' ? 'Основной долг' : 'Проценты'
                }
                wrapperStyle={{ fontSize: 12, color: '#657168' }}
              />
              <Area
                type="monotone"
                dataKey="principalPart"
                name="principalPart"
                stackId="payment"
                stroke="#357553"
                fill="#78a987"
                fillOpacity={0.82}
              />
              <Area
                type="monotone"
                dataKey="interestPart"
                name="interestPart"
                stackId="payment"
                stroke="#b2c9b6"
                fill="#cbdcc9"
                fillOpacity={0.95}
              />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </section>
    </div>
  )
}
