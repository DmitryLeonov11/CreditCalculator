import { createContext } from 'react'
import type { AuthSession, AuthTokensResponse } from './authTypes'

export type AuthContextValue = {
  session: AuthSession | null
  signIn: (tokens: AuthTokensResponse) => void
  signOut: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
