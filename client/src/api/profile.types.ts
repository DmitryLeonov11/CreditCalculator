export type Gender = 'Male' | 'Female'

export interface ProfileResponse {
  fullName: string
  birthDate: string
  gender: Gender | null
  monthlyIncome: number
  employmentMonths: number
  existingMonthlyPayments: number
  dependents: number
  updatedAt: string
}

export type UpdateProfileRequest = Omit<ProfileResponse, 'updatedAt'> & {
  gender: Gender
}
