import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type {
  CreateUserBody,
  PagedResult,
  UpdateUserBody,
  UserRole,
  UserRow,
} from '@/api/types'

export type UserFilters = {
  q: string
  role: UserRole | ''
  isActive: '' | 'true' | 'false'
  page: number
  pageSize: number
}

export const defaultUserFilters: UserFilters = {
  q: '',
  role: '',
  isActive: '',
  page: 1,
  pageSize: 20,
}

function toQueryString(filters: UserFilters): string {
  const params = new URLSearchParams()
  if (filters.q.trim()) params.set('q', filters.q.trim())
  if (filters.role) params.set('role', filters.role)
  if (filters.isActive) params.set('isActive', filters.isActive)
  params.set('page', String(filters.page))
  params.set('pageSize', String(filters.pageSize))
  return params.toString()
}

export function useUsers(filters: UserFilters) {
  return useQuery({
    queryKey: ['users', filters],
    queryFn: () => api.get<PagedResult<UserRow>>(`/api/users?${toQueryString(filters)}`),
    placeholderData: (previous) => previous,
  })
}

function useUserMutation<TBody, TResult>(
  run: (body: TBody) => Promise<TResult>,
) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: run,
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['users'] }),
  })
}

export function useCreateUser() {
  return useUserMutation((body: CreateUserBody) => api.post<UserRow>('/api/users', body))
}

export function useUpdateUser(id: string) {
  return useUserMutation((body: UpdateUserBody) => api.put<UserRow>(`/api/users/${id}`, body))
}

export function useSetPassword(id: string) {
  return useUserMutation((password: string) =>
    api.put<UserRow>(`/api/users/${id}/password`, { password }),
  )
}

export function useDeactivateUser(id: string) {
  return useUserMutation(() => api.del<void>(`/api/users/${id}`))
}
