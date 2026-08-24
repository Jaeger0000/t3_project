import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type { AssistantAnswer, StartupSummary } from '@/api/types'

export function useAskAssistant() {
  return useMutation({
    mutationFn: (question: string) =>
      api.post<AssistantAnswer>('/api/ai/ask', { question }),
  })
}

/**
 * Girişim kartındaki özet. Kullanıcı isteyene kadar çağrılmıyor (`enabled`):
 * model yapılandırıldığında her kart açılışı bir dış istek üretmemeli.
 */
export function useStartupSummary(startupId: string | undefined, enabled: boolean) {
  return useQuery({
    queryKey: ['ai', 'summary', startupId],
    queryFn: () => api.get<StartupSummary>(`/api/ai/startups/${startupId}/summary`),
    enabled: Boolean(startupId) && enabled,
    staleTime: 5 * 60 * 1000,
  })
}
