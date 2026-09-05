import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type { PagedResult, RegistrationRequestRow, RegistrationRequestStatus } from '@/api/types'

export type RegistrationRequestFilters = {
  status: RegistrationRequestStatus | ''
  page: number
  pageSize: number
}

export const defaultRegistrationFilters: RegistrationRequestFilters = {
  status: 'Pending',
  page: 1,
  pageSize: 20,
}

function toQueryString(filters: RegistrationRequestFilters): string {
  const params = new URLSearchParams()
  if (filters.status) params.set('status', filters.status)
  params.set('page', String(filters.page))
  params.set('pageSize', String(filters.pageSize))
  return params.toString()
}

export function useRegistrationRequests(filters: RegistrationRequestFilters) {
  return useQuery({
    queryKey: ['registration-requests', filters],
    queryFn: () =>
      api.get<PagedResult<RegistrationRequestRow>>(
        `/api/registration-requests?${toQueryString(filters)}`,
      ),
    placeholderData: (previous) => previous,
    // Kuyruk paylaşılan bir iş listesi: başka bir yönetici karar verdiyse
    // bayat satır göstermemek için kısa tutuluyor (bkz. change-requests kuyruğu).
    staleTime: 5_000,
  })
}

/**
 * Onay: `Startup` + `StartupUser` hesabı burada gerçekten doğuyor, geri
 * alınamaz. Başarıdan sonra kuyruk geçersizleştiriliyor; kullanıcı listesi de
 * geçersizleştiriliyor çünkü onay yeni bir hesap üretiyor.
 */
export function useApproveRegistrationRequest(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () =>
      api.post<{ startupId: string; userId: string }>(
        `/api/registration-requests/${id}/approve`,
      ),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['registration-requests'] })
      void queryClient.invalidateQueries({ queryKey: ['users'] })
      void queryClient.invalidateQueries({ queryKey: ['startups'] })
    },
  })
}

export function useRejectRegistrationRequest(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (note: string) =>
      api.post<void>(`/api/registration-requests/${id}/reject`, { note }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['registration-requests'] })
    },
  })
}
