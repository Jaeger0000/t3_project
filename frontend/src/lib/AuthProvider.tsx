import { useCallback, useState } from 'react'
import type { ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, tokenStore } from '@/lib/apiClient'
import { AuthContext } from '@/lib/auth'
import type { LoginResponse, SessionUser } from '@/api/types'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [token, setToken] = useState<string | null>(() => tokenStore.get())

  // Jeton 15 dakika geçerli; /api/me her açılışta rolü ve hesabın aktifliğini
  // sunucudan tazeler, böylece jetonun içindeki bilgiye körü körüne güvenmiyoruz.
  const session = useQuery({
    queryKey: ['session'],
    queryFn: async () => {
      try {
        return await api.get<SessionUser>('/api/me')
      } catch (error) {
        // apiClient 401'de jetonu zaten sildi; yerel durumu da hizalıyoruz.
        // Bu, effect içinde senkronlamaya göre daha doğru yer: durumu
        // değiştiren olayın tam kendisi burada.
        setToken(null)
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
    queryClient.clear()
  }, [queryClient])

  return (
    <AuthContext.Provider
      value={{
        session: session.data ?? null,
        isResolving: token !== null && session.isPending,
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
