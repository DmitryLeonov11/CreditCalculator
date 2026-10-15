import { httpClient } from './httpClient'
import { saveDownloadedFile } from './fileDownloads'
import type {
  EarlyRepaymentRequest,
  EarlyRepaymentResponse,
  ScheduleRequest,
  ScheduleResponse,
} from './calculator.types'

export const calculatorApi = {
  async downloadSchedulePdf(request: ScheduleRequest): Promise<void> {
    const response = await httpClient.post<Blob>(
      '/calculator/schedule/pdf',
      request,
      { responseType: 'blob' },
    )
    saveDownloadedFile(response, 'payment-schedule.pdf')
  },

  async downloadScheduleExcel(request: ScheduleRequest): Promise<void> {
    const response = await httpClient.post<Blob>(
      '/calculator/schedule/excel',
      request,
      { responseType: 'blob' },
    )
    saveDownloadedFile(response, 'payment-schedule.xlsx')
  },

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
