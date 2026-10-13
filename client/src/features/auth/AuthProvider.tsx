import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import type { AuthSession, AuthTokensResponse, UserRole } from './authTypes'
import { AuthContext } from './authContext'
import {
  clearTokens,
  getStoredTokens,
  storeTokens,
  subscribeToSessionChanges,
} from './tokenStorage'

function createSession(tokens: AuthTokensResponse | null): AuthSession | null {
  if (!tokens) return null

  try {
    const payload = tokens.accessToken.split('.')[1]
    if (!payload) return null

    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const claims = JSON.parse(atob(base64)) as { role?: unknown }
    const role = claims.role

    if (role !== 'Client' && role !== 'Employee') return null
    return { ...tokens, role: role as UserRole }
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState(() => createSession(getStoredTokens()))

  useEffect(
    () =>
      subscribeToSessionChanges(() =>
        setSession(createSession(getStoredTokens())),
      ),
    [],
  )

  function signIn(tokens: AuthTokensResponse) {
    storeTokens(tokens)
    setSession(createSession(tokens))
  }

  function signOut() {
    clearTokens()
    setSession(null)
  }

  return (
    <AuthContext.Provider value={{ session, signIn, signOut }}>
      {children}
    </AuthContext.Provider>
  )
}
