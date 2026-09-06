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
  /**
   * `redirectTo` verilirse `RequireAuth` oturumun düştüğünü görünce oraya
   * yönlendirir (varsayılan `/giris`). Çıkış düğmesi tanıtım sayfasına
   * dönmek istediğinde kullanılır — bunu burada tek bir durumdan
   * yönetmemizin nedeni, çıkışla eşzamanlı ayrı bir `navigate()` çağrısının
   * `RequireAuth`'un kendi yönlendirmesiyle yarışıp bazen kaybetmesiydi.
   */
  logout: (redirectTo?: string) => void
  logoutRedirect: string | null
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

/**
 * "Bu tarayıcıda oturum açılmıştı" izi. Jeton **değil** — jeton HttpOnly
 * çerezde ve JavaScript onu göremiyor. Buradaki tek bilgi bir bayrak ve üç işi
 * var: (1) ilk kez gelen ziyaretçiye "oturum süresi doldu" demeyi engellemek,
 * (2) sekmeler arası senkron için `storage` olayını tetiklemek, (3) açılış
 * sayfasının, /api/me cevabı gelmeden önce, tanıtımı mı yoksa panoyu mu
 * göstereceğini bilmesi.
 *
 * Sağlayıcının değil bu dosyanın içinde: bileşen ihraç eden bir modüle yardımcı
 * eklenince Vite'ın fast refresh'i o dosya için kapanıyor.
 */
const SESSION_MARKER = 't3.session.active'

export const oturumIzi = {
  exists: () => localStorage.getItem(SESSION_MARKER) === '1',
  set: () => localStorage.setItem(SESSION_MARKER, '1'),
  clear: () => localStorage.removeItem(SESSION_MARKER),
  anahtar: SESSION_MARKER,
}
