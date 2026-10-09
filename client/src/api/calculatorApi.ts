import { httpClient } from './httpClient'
import type {
  EarlyRepaymentRequest,
  EarlyRepaymentResponse,
  ScheduleRequest,
  ScheduleResponse,
} from './calculator.types'

export const calculatorApi = {
  async getSchedule(request: ScheduleRequest): Promise<ScheduleResponse> {
    const response = await httpClient.post<ScheduleResponse>(
      '/calculator/schedule',
      request,
    )
    return response.data
  },

  async getEarlyRepaymentComparison(
    request: EarlyRepaymentRequest,
  ): Promise<EarlyRepaymentResponse> {
    const response = await httpClient.post<EarlyRepaymentResponse>(
      '/calculator/early-repayment',
      request,
    )
    return response.data
  },
}
