import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type { Achievement, AchievementList, AchievementWriteModel } from '@/api/types'

export function useAchievements(startupId: string | undefined) {
  return useQuery({
    queryKey: ['startups', startupId, 'achievements'],
    queryFn: () => api.get<AchievementList>(`/api/startups/${startupId}/achievements`),
    enabled: Boolean(startupId),
  })
}

/**
 * Yazma sonrası hem liste hem kart tazeleniyor: kartın finansal özeti aynı
 * kayıtlardan türüyor, yalnızca listeyi yenilemek özeti eski bırakırdı.
 */
function useInvalidate(startupId: string) {
  const client = useQueryClient()
  return () => {
    void client.invalidateQueries({ queryKey: ['startups', startupId] })
  }
}

export function useAddAchievement(startupId: string) {
  const invalidate = useInvalidate(startupId)
  return useMutation({
    mutationFn: (model: AchievementWriteModel) =>
      api.post<Achievement>(`/api/startups/${startupId}/achievements`, model),
    onSuccess: invalidate,
  })
}

export function useUpdateAchievement(startupId: string, achievementId: string) {
  const invalidate = useInvalidate(startupId)
  return useMutation({
    mutationFn: (model: AchievementWriteModel) =>
      api.put<Achievement>(`/api/startups/${startupId}/achievements/${achievementId}`, model),
    onSuccess: invalidate,
  })
}

export function useDeleteAchievement(startupId: string) {
  const invalidate = useInvalidate(startupId)
  return useMutation({
    mutationFn: (achievementId: string) =>
      api.del<void>(`/api/startups/${startupId}/achievements/${achievementId}`),
    onSuccess: invalidate,
  })
}
