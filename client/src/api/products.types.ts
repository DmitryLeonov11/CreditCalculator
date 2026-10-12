export type CreditPurpose = 'Consumer' | 'Auto' | 'Mortgage'

export interface CreditProductResponse {
  id: string
  name: string
  purpose: CreditPurpose
  minAmount: number
  maxAmount: number
  minTermMonths: number
  maxTermMonths: number
  baseRate: number
}
