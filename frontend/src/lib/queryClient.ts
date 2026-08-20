import { QueryClient } from '@tanstack/react-query'
import { ApiError } from './apiClient'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Yetki ve bulunamadı hatalarını tekrar denemek anlamsız.
      retry: (failureCount, error) => {
        if (error instanceof ApiError && [400, 401, 403, 404].includes(error.status)) {
          return false
        }
        return failureCount < 2
      },
    },
  },
})
