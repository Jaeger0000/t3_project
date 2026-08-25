import { useCallback, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, api } from '@/lib/apiClient'
import { AuthContext } from '@/lib/auth'
import type { LoginResponse, SessionUser } from '@/api/types'

/**
 * "Bu tarayıcıda oturum açılmıştı" izi. Jeton **değil** — jeton HttpOnly
 * çerezde ve JavaScript onu göremiyor. Buradaki tek bilgi bir bayrak ve iki işi
 * var: (1) ilk kez gelen ziyaretçiye "oturum süresi doldu" demeyi engellemek,
 * (2) sekmeler arası senkron için `storage` olayını tetiklemek.
 */
const SESSION_MARKER = 't3.session.active'

const marker = {
  exists: () => localStorage.getItem(SESSION_MARKER) === '1',
  set: () => localStorage.setItem(SESSION_MARKER, '1'),
  clear: () => localStorage.removeItem(SESSION_MARKER),
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [signedOutReason, setSignedOutReason] = useState<string | null>(null)

  /*
   * "Oturum kapandı" bilgisi açık bir React durumu olmak zorunda.
   *
   * Jeton istemcide tutulduğu sürece bu iş jeton durumundan geliyordu.
   * Çerez modeline geçince ilk deneme `queryClient.clear()` idi ve **çalışmadı**:
   * clear önbelleği boşaltıyor ama ekrandaki bileşenlere yeni bir sonuç
   * bildirmiyor — kullanıcı "Çıkış"a bastıktan sonra panoda kalıyordu. Aynı
   * boşluk 401'de de vardı: React Query hata durumunda elindeki `data`'yı
   * koruyor, yani süresi dolmuş oturum ekranda açık görünüyordu.
   */
  const [signedOut, setSignedOut] = useState(false)

  // Oturumun kaynağı artık istemcideki bir jeton değil, sunucunun çerezi:
  // "elimde jeton var mı" diye bakamıyoruz, her açılışta /api/me soruyoruz.
  // Yanıt aynı zamanda rolü, hesabın aktifliğini ve "şifre değiştir" bayrağını
  // tazeliyor — jetonun içindeki bilgiye körü körüne güvenmiyoruz.
  const session = useQuery({
    queryKey: ['session'],
    queryFn: async () => {
      try {
        const user = await api.get<SessionUser>('/api/me')
        marker.set()
        return user
      } catch (error) {
        // Oturum yalnızca 401'de düşüyor. Ağ hatası (status 0) oturumu
        // düşürmüyor: API kapandığında kullanıcı atılıyor ve ham
        // "Failed to fetch" görüyordu.
        if (error instanceof ApiError && error.status === 401) {
          // Gerekçeyi yalnızca daha önce oturum açmış kullanıcıya gösteriyoruz;
          // siteye ilk kez gelen kişi için 401 normal durum.
          if (marker.exists())
            setSignedOutReason('Oturum süresi doldu. Lütfen tekrar giriş yapın.')

          marker.clear()
          setSignedOut(true)
        }

        throw error
      }
    },
    enabled: !signedOut,
    retry: false,
    staleTime: 5 * 60 * 1000,
  })

  const loginMutation = useMutation({
    mutationFn: (body: { email: string; password: string; kvkkConsentVersion?: string }) =>
      api.post<LoginResponse>('/api/auth/login', body),
    onSuccess: (data) => {
      // Jeton yanıt gövdesinde de geliyor ama bilinçli olarak saklanmıyor:
      // tarayıcı onu çerezden kullanıyor, gövdedeki kopya betikler için.
      marker.set()
      setSignedOut(false)
      setSignedOutReason(null)
      // Giriş yanıtı oturum bilgisini de taşıyor: fazladan /api/me turu yok.
      queryClient.setQueryData(['session'], data.user)
    },
  })

  const login = useCallback(
    async (email: string, password: string, kvkkOnaySurumu?: string) => {
      // Onay sürümü isteğe bağlı: Bearer ile gelen betikler, MCP istemcileri ve
      // Swagger giriş ekranını hiç görmüyor, alanı zorunlu yapmak onları kırardı.
      await loginMutation.mutateAsync({ email, password, kvkkConsentVersion: kvkkOnaySurumu })
    },
    [loginMutation],
  )

  const logout = useCallback(() => {
    marker.clear()
    // Ekran hemen tepki vermeli: sunucu turunu beklemek "düğme çalışmıyor"
    // izlenimi verirdi.
    setSignedOut(true)

    void (async () => {
      // Çerezi yalnızca sunucu geçersiz kılabilir; istemcinin "unutması"
      // yetmez. Önbellek de ancak çerez silindikten sonra temizleniyor, aksi
      // halde yeniden yüklenen /api/me eski çerezle 200 dönebilirdi.
      try {
        await api.post('/api/auth/logout')
      } catch {
        // Çerez zaten yoksa ya da sunucu erişilemezse: yerel durum temizlensin.
      }

      queryClient.clear()
    })()
  }, [queryClient])

  const retrySession = useCallback(() => {
    setSignedOut(false)
    void queryClient.invalidateQueries({ queryKey: ['session'] })
  }, [queryClient])

  // Sekmeler arası senkron: bir sekmede çıkış yapan kullanıcı diğerinde açık
  // kalmış bir ekranla devam etmemeli (ve tersi — bir sekmede giriş yapıldıysa
  // ötekinin giriş ekranında beklemesi anlamsız). `storage` olayı yalnızca
  // *diğer* sekmelerde tetikleniyor, döngü yok.
  useEffect(() => {
    const onStorage = (event: StorageEvent) => {
      if (event.key !== SESSION_MARKER) return

      // Diğer sekme çıkış yaptıysa işaret gitmiştir; giriş yaptıysa gelmiştir.
      // İki yönde de sunucuya bir kez sormak yerine durumu doğrudan
      // taşıyoruz — çerez zaten paylaşılıyor.
      if (marker.exists()) {
        setSignedOut(false)
        void queryClient.invalidateQueries({ queryKey: ['session'] })
      } else {
        setSignedOut(true)
      }
    }

    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
  }, [queryClient])

  // Sunucu yanıt vermiyor: bu "oturum yok" değil "şu an doğrulanamıyor" durumu
  // ve ekranda öyle görünmesi gerekiyor.
  const connectionError =
    !signedOut && session.error instanceof ApiError && session.error.status === 0
      ? session.error.message
      : null

  return (
    <AuthContext.Provider
      value={{
        session: signedOut ? null : session.data ?? null,
        isResolving: !signedOut && session.isPending,
        connectionError,
        retrySession,
        signedOutReason,
        login,
        logout,
        loginError: loginMutation.error?.message ?? null,
        isLoggingIn: loginMutation.isPending,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
