import { createContext, useContext } from 'react'
import type { SessionUser } from '@/api/types'

export type AuthState = {
  session: SessionUser | null
  /** Jeton var ama oturum henüz doğrulanmadı — yönlendirme kararı beklemeli. */
  isResolving: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  loginError: string | null
  isLoggingIn: boolean
}

/**
 * Bağlam ve hook bilinçli olarak sağlayıcıdan ayrı dosyada: bir modül hem
 * bileşen hem yardımcı ihraç ettiğinde Vite'ın fast refresh'i devre dışı kalır.
 */
export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth, AuthProvider içinde kullanılmalı.')
  return context
}
