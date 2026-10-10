import { httpClient } from './httpClient'
import type {
  EarlyRepaymentRequest,
  EarlyRepaymentResponse,
  ScheduleRequest,
  ScheduleResponse,
} from './calculator.types'

export const calculatorApi = {
  async getSchedule(
    request: ScheduleRequest,
    signal?: AbortSignal,
  ): Promise<ScheduleResponse> {
    const response = await httpClient.post<ScheduleResponse>(
      '/calculator/schedule',
      request,
      { signal },
    )
    return response.data
  },

  async getEarlyRepaymentComparison(
    request: EarlyRepaymentRequest,
    signal?: AbortSignal,
  ): Promise<EarlyRepaymentResponse> {
    const response = await httpClient.post<EarlyRepaymentResponse>(
      '/calculator/early-repayment',
      request,
      { signal },
    )
    return response.data
  },
}
