import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type { MyNotificationsResponse, SentNotificationsResponse } from '@/api/types'

const MY_NOTIFICATIONS_KEY = ['notifications', 'mine']
const SENT_NOTIFICATIONS_KEY = ['notifications', 'sent']

/**
 * Oturum sahibinin kendi bildirimleri. Menüdeki rozet de aynı sorguyu
 * kullanıyor (bkz. AppShell) — filtre içermeyen tek bir liste olduğu için
 * `usePendingCount`'taki gibi ayrı bir anahtar gerekmiyor, TanStack Query
 * ikisini tek istekten besliyor.
 */
export function useMyNotifications(enabled = true) {
  return useQuery({
    queryKey: MY_NOTIFICATIONS_KEY,
    queryFn: () => api.get<MyNotificationsResponse>('/api/notifications'),
    enabled,
    staleTime: 15_000,
  })
}

export function useUnreadNotificationCount(enabled: boolean) {
  return useQuery({
    queryKey: MY_NOTIFICATIONS_KEY,
    queryFn: () => api.get<MyNotificationsResponse>('/api/notifications'),
    select: (data) => data.unreadCount,
    enabled,
    staleTime: 15_000,
  })
}

/** "Tümünü okundu işaretle" düğmesi — tekil işaretlemenin toplu hâli. */
export function useMarkNotificationsRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => api.post<number>('/api/notifications/read-all'),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: MY_NOTIFICATIONS_KEY })
    },
  })
}

/**
 * Tek bir bildirimi okundu işaretler — hem kendi gelen kutumdaki hem
 * (SuperAdmin için) gözetim ekranındaki "Okundu işaretle" düğmesi aynı uca
 * gidiyor, bu yüzden ikisi de geçersiz kılınıyor.
 */
export function useMarkNotificationRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.post(`/api/notifications/${id}/read`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: MY_NOTIFICATIONS_KEY })
      void queryClient.invalidateQueries({ queryKey: SENT_NOTIFICATIONS_KEY })
    },
  })
}

/**
 * Bildirimi siler. Alıcı kendi kutusundan siliyorsa kayıt sunucuda kalır,
 * yalnızca kendi listesinden çıkar; SuperAdmin gözetim ekranından siliyorsa
 * kayıt gerçekten kalkar (bkz. DeleteNotificationHandler) — o yüzden başarı
 * sonrası iki liste de geçersiz kılınıyor.
 */
export function useDeleteNotification() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.del(`/api/notifications/${id}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: MY_NOTIFICATIONS_KEY })
      void queryClient.invalidateQueries({ queryKey: SENT_NOTIFICATIONS_KEY })
    },
  })
}

/** "Silinenler" sekmesindeki "Geri yükle" düğmesi — yalnızca gerçek alıcı kullanabilir. */
export function useRestoreNotification() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.post(`/api/notifications/${id}/restore`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: MY_NOTIFICATIONS_KEY })
    },
  })
}

/** SuperAdmin gözetim ekranı: tüm bildirimler, gönderen role göre ayrılmış. */
export function useSentNotifications(enabled: boolean) {
  return useQuery({
    queryKey: SENT_NOTIFICATIONS_KEY,
    queryFn: () => api.get<SentNotificationsResponse>('/api/notifications/sent'),
    enabled,
  })
}

export function useSendNotification(startupId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (message: string) =>
      api.post(`/api/startups/${startupId}/notifications`, { message }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: SENT_NOTIFICATIONS_KEY })
    },
  })
}
