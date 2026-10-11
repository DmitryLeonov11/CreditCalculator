export type AuthTokensResponse = {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

export type LoginRequest = {
  email: string
  password: string
}

export type RegisterRequest = LoginRequest & {
  personalDataConsent: boolean
}

export type UserRole = 'Client' | 'Employee'

export type AuthSession = AuthTokensResponse & {
  role: UserRole
}
