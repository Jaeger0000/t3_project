import { useCallback, useState } from 'react'
import type { ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, api, tokenStore } from '@/lib/apiClient'
import { AuthContext } from '@/lib/auth'
import type { LoginResponse, SessionUser } from '@/api/types'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [token, setToken] = useState<string | null>(() => tokenStore.get())
  const [signedOutReason, setSignedOutReason] = useState<string | null>(null)

  // Jetonun ömrü bir iş günü; /api/me her açılışta rolü, hesabın aktifliğini ve
  // "şifre değiştir" bayrağını sunucudan tazeler — jetonun içindeki bilgiye
  // körü körüne güvenmiyoruz.
  const session = useQuery({
    queryKey: ['session'],
    queryFn: async () => {
      try {
        return await api.get<SessionUser>('/api/me')
      } catch (error) {
        // Oturum yalnızca 401'de düşüyor. Ağ hatası (status 0) jetonu
        // silmiyordu ama eskiden burada ayrım yoktu: API kapandığında
        // kullanıcı oturumdan atılıyor ve ham "Failed to fetch" görüyordu.
        if (error instanceof ApiError && error.status === 401) {
          setToken(null)
          setSignedOutReason('Oturum süresi doldu. Lütfen tekrar giriş yapın.')
        }

        throw error
      }
    },
    enabled: token !== null,
    retry: false,
    staleTime: 5 * 60 * 1000,
  })

  const loginMutation = useMutation({
    mutationFn: (body: { email: string; password: string }) =>
      api.post<LoginResponse>('/api/auth/login', body),
    onSuccess: (data) => {
      tokenStore.set(data.accessToken)
      setToken(data.accessToken)
      setSignedOutReason(null)
      // Giriş yanıtı oturum bilgisini de taşıyor: fazladan /api/me turu yok.
      queryClient.setQueryData(['session'], data.user)
    },
  })

  const login = useCallback(
    async (email: string, password: string) => {
      await loginMutation.mutateAsync({ email, password })
    },
    [loginMutation],
  )

  const logout = useCallback(() => {
    tokenStore.clear()
    setToken(null)
    setSignedOutReason(null)
    queryClient.clear()
  }, [queryClient])

  const retrySession = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['session'] })
  }, [queryClient])

  // Jeton elimizde ama sunucu yanıt vermiyor: bu "oturum yok" değil "şu an
  // doğrulanamıyor" durumu ve ekranda öyle görünmesi gerekiyor.
  const connectionError =
    token !== null && session.error instanceof ApiError && session.error.status === 0
      ? session.error.message
      : null

  return (
    <AuthContext.Provider
      value={{
        session: session.data ?? null,
        isResolving: token !== null && session.isPending,
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
