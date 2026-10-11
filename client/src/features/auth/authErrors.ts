import axios from 'axios'

export function getAuthErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError<{ title?: string; detail?: string }>(error)) {
    return error.response?.data.detail ?? error.response?.data.title ?? fallback
  }

  return fallback
}
