import type {
  ParticipationStatus,
  ProgramType,
  Sector,
  StartupStatus,
  TimelineEntryKind,
  UserRole,
} from '@/api/types'

/**
 * Enum değerlerinin Türkçe karşılıkları. Filtre açılırları ve etiketler
 * buradan beslenir. Zaman çizelgesinin anlatı metinleri sunucuda üretilir,
 * bu yüzden burada yalnızca ayrık değerlerin adları var.
 */

export const sectorLabels: Record<Sector, string> = {
  Defense: 'Savunma',
  Health: 'Sağlık',
  Software: 'Yazılım',
  Energy: 'Enerji',
  Agriculture: 'Tarım',
  Education: 'Eğitim',
  Finance: 'Finans',
  Mobility: 'Ulaşım',
  Space: 'Uzay',
  Manufacturing: 'Üretim',
  Other: 'Diğer',
}

export const startupStatusLabels: Record<StartupStatus, string> = {
  Active: 'Aktif',
  Inactive: 'Pasif',
  Graduated: 'Mezun',
  Exited: 'Çıkış yaptı',
  Acquired: 'Satın alındı',
}

export const startupStatusTone: Record<StartupStatus, string> = {
  Active: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300',
  Inactive: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  Graduated: 'bg-brand-100 text-brand-900 dark:bg-brand-950 dark:text-brand-100',
  Exited: 'bg-amber-100 text-amber-900 dark:bg-amber-950 dark:text-amber-200',
  Acquired: 'bg-violet-100 text-violet-900 dark:bg-violet-950 dark:text-violet-200',
}

export const programTypeLabels: Record<ProgramType, string> = {
  PreIncubation: 'Ön Kuluçka',
  Incubation: 'Kuluçka',
  Acceleration: 'Hızlandırma',
  Competition: 'Yarışma',
  Event: 'Etkinlik',
  Training: 'Eğitim',
  Other: 'Diğer',
}

export const participationStatusLabels: Record<ParticipationStatus, string> = {
  Applied: 'Başvurdu',
  Accepted: 'Kabul edildi',
  InProgress: 'Devam ediyor',
  Completed: 'Tamamlandı',
  Graduated: 'Mezun oldu',
  Dropped: 'Ayrıldı',
}

export const roleLabels: Record<UserRole, string> = {
  SuperAdmin: 'Süper Yönetici',
  ProgramManager: 'Program Yöneticisi',
  StartupUser: 'Girişim Kullanıcısı',
  DecisionMaker: 'Karar Verici',
}

/** Çizelge girdisinin simgesi ve rengi. */
export const timelineStyles: Record<TimelineEntryKind, { icon: string; tone: string }> = {
  Founding: { icon: '★', tone: 'bg-brand-500 text-white' },
  ProgramJoined: { icon: '→', tone: 'bg-brand-100 text-brand-900 dark:bg-brand-900 dark:text-brand-100' },
  ProgramCompleted: { icon: '✓', tone: 'bg-brand-100 text-brand-900 dark:bg-brand-900 dark:text-brand-100' },
  Milestone: { icon: '◆', tone: 'bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-200' },
  Investment: { icon: '₺', tone: 'bg-emerald-500 text-white' },
  Grant: { icon: '⬢', tone: 'bg-sky-500 text-white' },
  Revenue: { icon: '▲', tone: 'bg-teal-500 text-white' },
  Export: { icon: '⇄', tone: 'bg-cyan-600 text-white' },
  Award: { icon: '🏆', tone: 'bg-accent-500 text-white' },
}
