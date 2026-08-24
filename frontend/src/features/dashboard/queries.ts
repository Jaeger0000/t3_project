import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type { EcosystemStats, Sector } from '@/api/types'

export type DashboardFilters = {
  sector: Sector | ''
  programId: string
  city: string
}

export const defaultDashboardFilters: DashboardFilters = {
  sector: '',
  programId: '',
  city: '',
}

function toQueryString(filters: DashboardFilters): string {
  const params = new URLSearchParams()
  if (filters.sector) params.set('sector', filters.sector)
  if (filters.programId) params.set('programId', filters.programId)
  if (filters.city.trim()) params.set('city', filters.city.trim())
  return params.toString()
}

export function useEcosystemStats(filters: DashboardFilters) {
  return useQuery({
    queryKey: ['reports', 'ecosystem', filters],
    queryFn: () => api.get<EcosystemStats>(`/api/reports/ecosystem?${toQueryString(filters)}`),
    placeholderData: (previous) => previous,
  })
}

/**
 * CSV indirme. Aynı süzgeçler dışa aktarma ucuna taşınır: kullanıcı ekranda ne
 * görüyorsa onu indirmeli.
 */
export function exportStartupsCsv(filters: DashboardFilters): Promise<void> {
  const query = toQueryString(filters)
  return api.download(
    `/api/reports/export${query ? `?${query}` : ''}`,
    't3-girisimler.csv',
  )
}
