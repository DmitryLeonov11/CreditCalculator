import { useQuery } from '@tanstack/react-query'
import type {
  EarlyRepaymentRequest,
  ScheduleRequest,
} from '../../api/calculator.types'
import { calculatorApi } from '../../api/calculatorApi'

export const calculatorQueryKeys = {
  all: ['calculator'] as const,
  schedule: (request: ScheduleRequest | null) =>
    [...calculatorQueryKeys.all, 'schedule', request] as const,
  earlyRepayment: (request: EarlyRepaymentRequest | null) =>
    [...calculatorQueryKeys.all, 'early-repayment', request] as const,
}

export function useScheduleQuery(request: ScheduleRequest | null) {
  return useQuery({
    queryKey: calculatorQueryKeys.schedule(request),
    queryFn: ({ signal }) => {
      if (!request) {
        throw new Error('Для расчёта графика необходимо заполнить параметры.')
      }

      return calculatorApi.getSchedule(request, signal)
    },
    enabled: request !== null,
  })
}

export function useEarlyRepaymentQuery(request: EarlyRepaymentRequest | null) {
  return useQuery({
    queryKey: calculatorQueryKeys.earlyRepayment(request),
    queryFn: ({ signal }) => {
      if (!request) {
        throw new Error(
          'Для сравнения необходимо заполнить параметры досрочного погашения.',
        )
      }

      return calculatorApi.getEarlyRepaymentComparison(request, signal)
    },
    enabled: request !== null,
  })
}
