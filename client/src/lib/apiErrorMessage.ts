import { isAxiosError } from 'axios'

type ProblemDetails = {
  detail?: string
  title?: string
  errors?: Record<string, string[]>
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!isAxiosError<ProblemDetails>(error)) return fallback

  const response = error.response?.data
  const validationMessage = response?.errors
    ? Object.values(response.errors)
        .flat()
        .find((message) => message.length > 0)
    : undefined

  return response?.detail ?? validationMessage ?? response?.title ?? fallback
}
