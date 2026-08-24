import type { ChangeRequestStatus } from '@/api/types'

export const changeStatusLabels: Record<ChangeRequestStatus, string> = {
  Pending: 'Bekliyor',
  Approved: 'Onaylandı',
  Rejected: 'Reddedildi',
}

export const changeStatusTone: Record<ChangeRequestStatus, string> = {
  Pending: 'bg-amber-100 text-amber-900 dark:bg-amber-950 dark:text-amber-200',
  Approved: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300',
  Rejected: 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-200',
}

/**
 * Bekleme süresi rozeti. Onay kuyruğunun ürün değeri "hiçbir öneri unutulmuyor"
 * olduğu için gecikme görsel olarak da ayrışıyor.
 */
export function waitingTone(days: number): string {
  if (days >= 7) return 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-200'
  if (days >= 3) return 'bg-amber-100 text-amber-900 dark:bg-amber-950 dark:text-amber-200'
  return 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300'
}

export function waitingLabel(days: number): string {
  if (days === 0) return 'bugün'
  return `${days} gündür bekliyor`
}
