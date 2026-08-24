import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, api } from '@/lib/apiClient'
import type { DocumentList, DocumentType, DocumentUploadResult } from '@/api/types'

/**
 * Doküman listesi. Yetkisiz rol 403 alır ve bu bir hata değil bir cevap:
 * çağıran ekran "yetkiniz yok" durumunu bu koddan ayırt ediyor.
 */
export function useDocuments(startupId: string | undefined) {
  return useQuery({
    queryKey: ['startups', startupId, 'documents'],
    queryFn: () => api.get<DocumentList>(`/api/startups/${startupId}/documents`),
    enabled: Boolean(startupId),
    retry: (count, error) =>
      error instanceof ApiError && error.status === 403 ? false : count < 2,
  })
}

export function useUploadDocument(startupId: string) {
  const client = useQueryClient()
  return useMutation({
    mutationFn: ({ file, type }: { file: File; type: DocumentType }) =>
      api.upload<DocumentUploadResult>(
        `/api/startups/${startupId}/documents`,
        file,
        `?type=${type}`,
      ),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: ['startups', startupId] })
      // Girişim kullanıcısının yüklemesi kuyruğa düşüyor; onay ekranındaki
      // bekleyen sayısı da tazelenmeli.
      void client.invalidateQueries({ queryKey: ['change-requests'] })
    },
  })
}

export function useDeleteDocument(startupId: string) {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (documentId: string) =>
      api.del<void>(`/api/startups/${startupId}/documents/${documentId}`),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: ['startups', startupId] })
    },
  })
}
