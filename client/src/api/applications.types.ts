import type { ScheduleItemResponse } from './calculator.types'

export type ApplicationStatus =
  | 'Draft'
  | 'Submitted'
  | 'Scoring'
  | 'AutoApproved'
  | 'UnderReview'
  | 'Approved'
  | 'Rejected'
  | 'Withdrawn'

export interface ApplicationStatusHistoryResponse {
  fromStatus: ApplicationStatus | null
  toStatus: ApplicationStatus
  changedAt: string
  comment: string | null
}

export interface ApplicationScoringResultResponse {
  ruleCode: string
  ruleName: string
  passed: boolean
  points: number
  details: string
}

export interface ApplicationResponse {
  id: string
  creditProductId: string
  productName: string
  amount: number
  termMonths: number
  interestRate: number
  status: ApplicationStatus
  score: number | null
  incomeAtApply: number | null
  existingPaymentsAtApply: number | null
  ageAtApply: number | null
  createdAt: string
  updatedAt: string
  statusHistory: ApplicationStatusHistoryResponse[]
  scoringResults: ApplicationScoringResultResponse[]
}

export interface ApplicationsPageResponse {
  page: number
  pageSize: number
  totalCount: number
  items: ApplicationResponse[]
}

export interface CreateApplicationRequest {
  creditProductId: string
  amount: number
  termMonths: number
  downPaymentAmount?: number | null
}

export interface ApplicationScheduleResponse {
  firstPaymentDate: string
  schedule: {
    payments: ScheduleItemResponse[]
    totalPaid: number
    overpayment: number
  }
}
