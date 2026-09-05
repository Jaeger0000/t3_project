import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'
import type {
  ChangePasswordBody,
  ForgotPasswordBody,
  RegisterStartupBody,
  ResetPasswordBody,
} from '@/api/types'

/**
 * Şifre kurtarma uçları. Üçü de Dalga 1'de eklendi: öncesinde tek yol
 * yöneticinin şifre atamasıydı, yani her hesabın şifresini bir başkası da
 * biliyordu.
 */
export function useRequestPasswordReset() {
  return useMutation({
    mutationFn: (body: ForgotPasswordBody) =>
      api.post<{ message: string }>('/api/auth/forgot-password', body),
  })
}

export function useResetPassword() {
  return useMutation({
    mutationFn: (body: ResetPasswordBody) =>
      api.post<{ email: string; changedAt: string }>('/api/auth/reset-password', body),
  })
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (body: ChangePasswordBody) =>
      api.post<{ changedAt: string }>('/api/auth/change-password', body),
  })
}

/**
 * Ana sayfadaki "Kayıt Ol" formu. Anonim uç: hesap burada doğmuyor, yalnızca
 * SuperAdmin onayı bekleyen bir başvuru satırı oluşuyor.
 */
export function useRegisterStartup() {
  return useMutation({
    mutationFn: (body: RegisterStartupBody) => api.post<{ id: string }>('/api/auth/register', body),
  })
}
