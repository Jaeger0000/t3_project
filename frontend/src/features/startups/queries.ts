import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type {
  AddParticipationBody,
  CardTeamMember,
  DeleteStartupResult,
  PagedResult,
  Program,
  Sector,
  StartupCard,
  StartupListItem,
  StartupSort,
  StartupStatus,
  StartupTimeline,
  StartupWriteModel,
  TeamMemberWriteModel,
} from '@/api/types'

export type StartupFilters = {
  q: string
  sector: Sector | ''
  status: StartupStatus | ''
  programId: string
  sort: StartupSort
  page: number
  pageSize: number
}

export const defaultFilters: StartupFilters = {
  q: '',
  sector: '',
  status: '',
  programId: '',
  sort: 'Name',
  page: 1,
  pageSize: 12,
}

function toQueryString(filters: StartupFilters): string {
  const params = new URLSearchParams()
  if (filters.q.trim()) params.set('q', filters.q.trim())
  if (filters.sector) params.set('sector', filters.sector)
  if (filters.status) params.set('status', filters.status)
  if (filters.programId) params.set('programId', filters.programId)
  params.set('sort', filters.sort)
  params.set('page', String(filters.page))
  params.set('pageSize', String(filters.pageSize))
  return params.toString()
}

/**
 * `enabled`: AI paneli girişim kartlarını ancak bir cevap geldiğinde çiziyor,
 * o yüzden listeyi de o ana kadar istemiyor — panoyu her açan kullanıcı için
 * boşuna bir istek atmasın.
 */
export function useStartups(filters: StartupFilters, enabled = true) {
  return useQuery({
    queryKey: ['startups', filters],
    queryFn: () => api.get<PagedResult<StartupListItem>>(`/api/startups?${toQueryString(filters)}`),
    // Sayfa/filtre değişirken listenin boşalmaması için önceki veri korunur.
    placeholderData: (previous) => previous,
    enabled,
  })
}

export function useStartupCard(id: string | undefined) {
  return useQuery({
    queryKey: ['startups', id, 'card'],
    queryFn: () => api.get<StartupCard>(`/api/startups/${id}`),
    enabled: Boolean(id),
  })
}

export function useStartupTimeline(id: string | undefined) {
  return useQuery({
    queryKey: ['startups', id, 'timeline'],
    queryFn: () => api.get<StartupTimeline>(`/api/startups/${id}/timeline`),
    enabled: Boolean(id),
  })
}

export function usePrograms() {
  return useQuery({
    queryKey: ['programs'],
    queryFn: () => api.get<Program[]>('/api/programs'),
    staleTime: 10 * 60 * 1000,
  })
}

// --- Yazma yolları --------------------------------------------------------
// Uçlar Faz 3'ten beri hazırdı ama hiçbir ekran çağırmıyordu: girişimi sisteme
// yalnızca tohumlayıcı ekleyebiliyordu. Aşağıdaki kancalar o boşluğu kapatıyor.

/**
 * Yazma sonrası tazeleme. `['startups']` öneki liste, kart ve kronolojiyi
 * birlikte kapsıyor; kartın finansal özeti ve yolculuk aynı kayıtlardan
 * türediği için üçünü ayrı ayrı geçersizleştirmek birini bayat bırakırdı.
 * Program listesi de tazeleniyor: katılım sayıları orada gösteriliyor.
 */
function useInvalidateStartups() {
  const client = useQueryClient()
  return () => {
    void client.invalidateQueries({ queryKey: ['startups'] })
    void client.invalidateQueries({ queryKey: ['programs'] })
    void client.invalidateQueries({ queryKey: ['reports'] })
  }
}

export function useCreateStartup() {
  const invalidate = useInvalidateStartups()
  return useMutation({
    mutationFn: (model: StartupWriteModel) =>
      api.post<{ id: string; name: string }>('/api/startups', model),
    onSuccess: invalidate,
  })
}

export function useUpdateStartup(startupId: string) {
  const invalidate = useInvalidateStartups()
  return useMutation({
    mutationFn: (model: StartupWriteModel) =>
      api.put<StartupCard>(`/api/startups/${startupId}`, model),
    onSuccess: invalidate,
  })
}

/** Silme aslında soft delete; yanıt hangi bağlı kaydın pasife alındığını sayar. */
export function useDeleteStartup() {
  const invalidate = useInvalidateStartups()
  return useMutation({
    mutationFn: (startupId: string) =>
      api.del<DeleteStartupResult>(`/api/startups/${startupId}`),
    onSuccess: invalidate,
  })
}

export function useAddTeamMember(startupId: string) {
  const invalidate = useInvalidateStartups()
  return useMutation({
    mutationFn: (model: TeamMemberWriteModel) =>
      api.post<CardTeamMember>(`/api/startups/${startupId}/team`, model),
    onSuccess: invalidate,
  })
}

export function useUpdateTeamMember(startupId: string, memberId: string) {
  const invalidate = useInvalidateStartups()
  return useMutation({
    mutationFn: (model: TeamMemberWriteModel) =>
      api.put<CardTeamMember>(`/api/startups/${startupId}/team/${memberId}`, model),
    onSuccess: invalidate,
  })
}

export function useRemoveTeamMember(startupId: string) {
  const invalidate = useInvalidateStartups()
  return useMutation({
    mutationFn: (memberId: string) =>
      api.del<void>(`/api/startups/${startupId}/team/${memberId}`),
    onSuccess: invalidate,
  })
}

/**
 * Girişimi bir program dönemine bağlar. Dönem listesi ayrı bir uçtan değil
 * `GET /api/programs` yanıtındaki `terms` dizisinden geliyor.
 */
export function useAddParticipation() {
  const invalidate = useInvalidateStartups()
  return useMutation({
    mutationFn: (body: AddParticipationBody) =>
      api.post<{ id: string }>('/api/participations', body),
    onSuccess: invalidate,
  })
}
