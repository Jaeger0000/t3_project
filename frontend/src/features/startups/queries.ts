import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type {
  PagedResult,
  Program,
  Sector,
  StartupCard,
  StartupListItem,
  StartupSort,
  StartupStatus,
  StartupTimeline,
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

export function useStartups(filters: StartupFilters) {
  return useQuery({
    queryKey: ['startups', filters],
    queryFn: () => api.get<PagedResult<StartupListItem>>(`/api/startups?${toQueryString(filters)}`),
    // Sayfa/filtre değişirken listenin boşalmaması için önceki veri korunur.
    placeholderData: (previous) => previous,
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
