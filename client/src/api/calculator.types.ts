export type PaymentType = 'Annuity' | 'Differentiated'

export type EarlyRepaymentMode = 'ReduceTerm' | 'ReducePayment'

export interface ScheduleRequest {
  amount: number
  termMonths: number
  annualRatePercent: number
  paymentType: PaymentType
  firstPaymentDate: string
}

export interface ScheduleItemResponse {
  number: number
  date: string
  payment: number
  interestPart: number
  principalPart: number
  remainingBalance: number
}

export interface ScheduleResponse {
  payments: ScheduleItemResponse[]
  monthlyPayment: number
  totalPaid: number
  overpayment: number
  effectiveRate: number
}

export interface EarlyRepaymentItemRequest {
  month: number
  amount: number
}

export interface EarlyRepaymentRequest {
  amount: number
  termMonths: number
  annualRatePercent: number
  firstPaymentDate: string
  mode: EarlyRepaymentMode
  earlyRepayments: EarlyRepaymentItemRequest[]
}

export interface EarlyRepaymentResponse {
  original: ScheduleResponse
  withEarlyRepayments: ScheduleResponse
}
