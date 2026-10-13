import axios from 'axios'
import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import {
  clearTokens,
  getStoredTokens,
  storeTokens,
} from '../features/auth/tokenStorage'
import type { AuthTokensResponse } from '../features/auth/authTypes'

type RetriableRequestConfig = InternalAxiosRequestConfig & {
  _retry?: boolean
}

export const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
})

const refreshClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api/v1',
})

let refreshInProgress: Promise<string> | null = null

httpClient.interceptors.request.use((config) => {
  const accessToken = getStoredTokens()?.accessToken
  if (accessToken) config.headers.set('Authorization', `Bearer ${accessToken}`)
  return config
})

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const request = error.config as RetriableRequestConfig | undefined
    const refreshToken = getStoredTokens()?.refreshToken
    const isAuthRequest = request?.url?.includes('/auth/')

    if (
      error.response?.status !== 401 ||
      !request ||
      request._retry ||
      !refreshToken ||
      isAuthRequest
    ) {
      return Promise.reject(error)
    }

    request._retry = true

    try {
      refreshInProgress ??= refreshClient
        .post<AuthTokensResponse>('/auth/refresh', { refreshToken })
        .then(({ data: tokens }) => {
          storeTokens(tokens)
          return tokens.accessToken
        })
        .finally(() => {
          refreshInProgress = null
        })

      const accessToken = await refreshInProgress
      request.headers.set('Authorization', `Bearer ${accessToken}`)
      return httpClient(request)
    } catch (refreshError) {
      clearTokens()
      return Promise.reject(refreshError)
    }
  },
)
