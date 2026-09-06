import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type {
  AiChatReply,
  AiConversationDetail,
  AiConversationSummary,
  PagedResult,
  StartupSummary,
} from '@/api/types'

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

// --- Kalıcı sohbet --------------------------------------------------------

/** Yalnızca kullanıcının kendi sohbetleri; kapsam sunucuda daraltılıyor. */
export function useChatConversations(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ['ai', 'conversations', page, pageSize],
    queryFn: () =>
      api.get<PagedResult<AiConversationSummary>>(
        `/api/ai/chat/conversations?page=${page}&pageSize=${pageSize}`,
      ),
    // Sayfa değişirken sol liste boşalmasın.
    placeholderData: (previous) => previous,
  })
}

/**
 * Sohbetin tam geçmişi. Seçili sohbet yokken (yeni sohbet) istek atılmıyor:
 * `enabled` olmadan TanStack `undefined` kimlikle uca vurur ve 404 üretirdi.
 */
export function useChatConversation(conversationId: string | null) {
  return useQuery({
    queryKey: ['ai', 'conversation', conversationId],
    queryFn: () =>
      api.get<AiConversationDetail>(`/api/ai/chat/conversations/${conversationId}`),
    enabled: Boolean(conversationId),
  })
}

/**
 * Bir tur ekler. Yanıttaki metin doğrudan ekrana basılmıyor: geçmişin tek
 * doğru kaynağı sunucu, bu yüzden yazma sonrası hem liste hem de sohbetin
 * kendisi tazeleniyor. Aksi hâlde iyimser satır ile kalıcı satır ikiye
 * ayrılabilir ve kullanıcı aynı cevabı iki kez görürdü.
 */
export function useSendChatMessage() {
  const client = useQueryClient()

  return useMutation({
    mutationFn: (input: { conversationId: string | null; question: string }) =>
      api.post<AiChatReply>('/api/ai/chat', {
        conversationId: input.conversationId,
        question: input.question,
      }),
    onSuccess: (reply) => {
      void client.invalidateQueries({ queryKey: ['ai', 'conversations'] })
      void client.invalidateQueries({ queryKey: ['ai', 'conversation', reply.conversationId] })
    },
  })
}
