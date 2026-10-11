import { httpClient } from '../../api/httpClient'
import type { AuthTokensResponse, LoginRequest, RegisterRequest } from './authTypes'

export const authApi = {
  async register(request: RegisterRequest): Promise<void> {
    await httpClient.post('/auth/register', request)
  },

  async login(request: LoginRequest): Promise<AuthTokensResponse> {
    const response = await httpClient.post<AuthTokensResponse>(
      '/auth/login',
      request,
    )
    return response.data
  },
}
