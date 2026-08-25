import { createContext, useContext } from 'react'
import type { SessionUser } from '@/api/types'

export type AuthState = {
  session: SessionUser | null
  /**
   * Oturum sunucudan doğrulanmadı — yönlendirme kararı beklemeli. Kimlik
   * HttpOnly çerezde olduğu için istemci "jetonum var mı" diye bakamıyor;
   * cevabı yalnızca /api/me veriyor.
   */
  isResolving: boolean
  /**
   * Sunucuya ulaşılamıyor. Oturumu düşürmüyoruz: ağ kesintisi yetki sorunu
   * değil, kullanıcı bağlantı gelince kaldığı yerden devam etmeli.
   */
  connectionError: string | null
  /** Bağlantı hatasından sonra yeniden denemek için. */
  retrySession: () => void
  /**
   * Oturumun neden kapandığı (jeton süresi doldu vb.). Giriş ekranı bunu
   * gösteriyor — kullanıcı sessizce atıldığını sanmasın.
   */
  signedOutReason: string | null
  /**
   * `kvkkOnaySurumu`: giriş ekranında onaylanan aydınlatma metninin sürümü.
   * Sunucu bunu denetim izine yazıyor; istemcideki kayıt yalnızca kolaylık.
   */
  login: (email: string, password: string, kvkkOnaySurumu?: string) => Promise<void>
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
