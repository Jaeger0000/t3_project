import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type {
  ChangeRequestDetail,
  ChangeRequestQueue,
  ChangeRequestStatus,
  ReviewChangeRequestResult,
  SubmitChangeRequestBody,
  SubmitChangeRequestResult,
} from '@/api/types'

export type QueueFilters = {
  status: ChangeRequestStatus | ''
  startupId: string
  page: number
  pageSize: number
}

export const defaultQueueFilters: QueueFilters = {
  status: 'Pending',
  startupId: '',
  page: 1,
  pageSize: 20,
}

function toQueryString(filters: QueueFilters): string {
  const params = new URLSearchParams()
  if (filters.status) params.set('status', filters.status)
  if (filters.startupId) params.set('startupId', filters.startupId)
  params.set('page', String(filters.page))
  params.set('pageSize', String(filters.pageSize))
  return params.toString()
}

export function useChangeRequests(filters: QueueFilters) {
  return useQuery({
    queryKey: ['change-requests', filters],
    queryFn: () =>
      api.get<ChangeRequestQueue>(`/api/change-requests?${toQueryString(filters)}`),
    placeholderData: (previous) => previous,
    // Kuyruk paylaşılan bir çalışma listesi: başkasının kararı sonrası bayat
    // satır göstermemek için kısa tutuluyor.
    staleTime: 5_000,
  })
}

/**
 * Menüdeki bekleyen sayısı. Kuyruk sorgusunun kendisiyle aynı anahtarı
 * kullanmıyor: filtre her değiştiğinde rozetin sıfırlanmaması gerekiyor.
 */
export function usePendingCount(enabled: boolean) {
  return useQuery({
    queryKey: ['change-requests', 'pending-count'],
    queryFn: () =>
      api.get<ChangeRequestQueue>('/api/change-requests?status=Pending&pageSize=1'),
    select: (data) => data.pendingCount,
    enabled,
    staleTime: 30_000,
  })
}

export function useChangeRequest(id: string | undefined) {
  return useQuery({
    queryKey: ['change-requests', id],
    queryFn: () => api.get<ChangeRequestDetail>(`/api/change-requests/${id}`),
    enabled: Boolean(id),
  })
}

/**
 * Karar mutasyonları. Başarıdan sonra yalnızca kuyruk değil girişim kartı da
 * geçersizleştiriliyor: onay, hedef kaydı gerçekten değiştiriyor.
 */
export function useReviewChangeRequest(id: string, decision: 'approve' | 'reject') {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (note: string | null) =>
      api.post<ReviewChangeRequestResult>(`/api/change-requests/${id}/${decision}`, {
        note,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['change-requests'] })
      void queryClient.invalidateQueries({ queryKey: ['startups'] })
    },
  })
}

export function useSubmitChangeRequest() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: SubmitChangeRequestBody) =>
      api.post<SubmitChangeRequestResult>('/api/change-requests', body),
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: ['change-requests'] })

      // Girişim profili önerisi hemen uygulanıyor (bkz. SubmitChangeRequestResult);
      // girişim kartı/listesi de bayat kalmasın diye onay akışıyla aynı
      // geçersizleştirmeyi burada da yapıyoruz.
      if (result.status === 'Approved')
        void queryClient.invalidateQueries({ queryKey: ['startups'] })
    },
  })
}
