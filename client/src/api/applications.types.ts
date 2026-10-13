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

export type EmployeeApplicationSortField =
  'Amount' | 'CreatedAt' | 'Score' | 'Status'

export interface EmployeeApplicationsQuery {
  page: number
  pageSize: number
  minAmount?: number
  maxAmount?: number
  createdFrom?: string
  createdTo?: string
  minScore?: number
  maxScore?: number
  status?: ApplicationStatus
  sortBy: EmployeeApplicationSortField
  descending: boolean
}

export interface EmployeeApplicationListItem {
  id: string
  creditProductId: string
  productName: string
  amount: number
  termMonths: number
  interestRate: number
  status: ApplicationStatus
  score: number | null
  createdAt: string
}

export interface EmployeeApplicationsPageResponse {
  page: number
  pageSize: number
  totalCount: number
  items: EmployeeApplicationListItem[]
}

export interface EmployeeScoringResult {
  ruleCode: string
  ruleName: string
  passed: boolean
  points: number
  details: string
}

export interface EmployeeApplicationDetailsResponse {
  application: ApplicationResponse
  scoringResults: EmployeeScoringResult[]
  proposedSchedule: ApplicationScheduleResponse
}

export interface ApproveApplicationRequest {
  amount: number
  termMonths: number
  interestRate: number
  comment?: string | null
}

export interface RejectApplicationRequest {
  comment: string
}
