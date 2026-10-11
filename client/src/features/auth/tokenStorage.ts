import type { AuthTokensResponse } from './authTypes'

const storageKey = 'credit-calculator.auth'
const sessionChangedEvent = 'credit-calculator:session-changed'

export function getStoredTokens(): AuthTokensResponse | null {
  const value = sessionStorage.getItem(storageKey)
  if (!value) return null

  try {
    return JSON.parse(value) as AuthTokensResponse
  } catch {
    clearTokens()
    return null
  }
}

export function storeTokens(tokens: AuthTokensResponse) {
  sessionStorage.setItem(storageKey, JSON.stringify(tokens))
  window.dispatchEvent(new Event(sessionChangedEvent))
}

export function clearTokens() {
  sessionStorage.removeItem(storageKey)
  window.dispatchEvent(new Event(sessionChangedEvent))
}

export function subscribeToSessionChanges(callback: () => void) {
  window.addEventListener(sessionChangedEvent, callback)
  return () => window.removeEventListener(sessionChangedEvent, callback)
}
