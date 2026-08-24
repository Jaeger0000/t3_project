import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type {
  DeleteProgramResult,
  ProgramSummary,
  ProgramTermWriteModel,
  ProgramWriteModel,
  UpdateParticipationBody,
} from '@/api/types'

/**
 * Program yazma kancaları. Uçlar Dalga 1'de eklendi: program → dönem → katılım
 * zincirinin ilk iki halkası daha önce yalnızca tohumlayıcıyla, yani doğrudan
 * veritabanına yazılarak var olabiliyordu.
 */
function useInvalidatePrograms() {
  const client = useQueryClient()
  return () => {
    void client.invalidateQueries({ queryKey: ['programs'] })
    // Girişim listesi ve pano program filtresi/sayıları aynı kayıtlardan
    // türüyor; yalnızca program listesini tazelemek diğerlerini bayat bırakırdı.
    void client.invalidateQueries({ queryKey: ['startups'] })
    void client.invalidateQueries({ queryKey: ['reports'] })
  }
}

export function useCreateProgram() {
  const invalidate = useInvalidatePrograms()
  return useMutation({
    mutationFn: (model: ProgramWriteModel) => api.post<ProgramSummary>('/api/programs', model),
    onSuccess: invalidate,
  })
}

export function useUpdateProgram(programId: string) {
  const invalidate = useInvalidatePrograms()
  return useMutation({
    mutationFn: (model: ProgramWriteModel) =>
      api.put<ProgramSummary>(`/api/programs/${programId}`, model),
    onSuccess: invalidate,
  })
}

/** Silme aslında soft delete; yanıt hangi bağlı kaydın pasife alındığını sayar. */
export function useDeleteProgram() {
  const invalidate = useInvalidatePrograms()
  return useMutation({
    mutationFn: (programId: string) => api.del<DeleteProgramResult>(`/api/programs/${programId}`),
    onSuccess: invalidate,
  })
}

export function useAddTerm(programId: string) {
  const invalidate = useInvalidatePrograms()
  return useMutation({
    mutationFn: (model: ProgramTermWriteModel) =>
      api.post<{ id: string }>(`/api/programs/${programId}/terms`, model),
    onSuccess: invalidate,
  })
}

export function useUpdateTerm(programId: string, termId: string) {
  const invalidate = useInvalidatePrograms()
  return useMutation({
    mutationFn: (model: ProgramTermWriteModel) =>
      api.put<{ id: string }>(`/api/programs/${programId}/terms/${termId}`, model),
    onSuccess: invalidate,
  })
}

export function useDeleteTerm(programId: string) {
  const invalidate = useInvalidatePrograms()
  return useMutation({
    mutationFn: (termId: string) =>
      api.del<{ id: string; name: string }>(`/api/programs/${programId}/terms/${termId}`),
    onSuccess: invalidate,
  })
}

export function useUpdateParticipation(participationId: string) {
  const invalidate = useInvalidatePrograms()
  return useMutation({
    mutationFn: (body: UpdateParticipationBody) =>
      api.put<{ id: string }>(`/api/participations/${participationId}`, body),
    onSuccess: invalidate,
  })
}

export function useRemoveParticipation() {
  const invalidate = useInvalidatePrograms()
  return useMutation({
    mutationFn: (participationId: string) =>
      api.del<{ id: string }>(`/api/participations/${participationId}`),
    onSuccess: invalidate,
  })
}
