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
  Active: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300',
  Inactive: 'bg-stone-100 text-stone-700 dark:bg-stone-800 dark:text-stone-300',
  Graduated: 'bg-brand-100 text-brand-800 dark:bg-brand-950 dark:text-brand-200',
  // Çerçeveli rozet: "Mezun" ile aynı sıcak aileden ama dolgusuz olduğu için
  // listede karışmıyor.
  Exited: 'bg-white text-stone-600 ring-1 ring-stone-300 dark:bg-stone-900 dark:text-stone-300 dark:ring-stone-700',
  Acquired: 'bg-stone-800 text-white dark:bg-stone-200 dark:text-stone-900',
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
  ProgramJoined: { icon: '→', tone: 'bg-brand-100 text-brand-800 dark:bg-brand-900 dark:text-brand-100' },
  ProgramCompleted: { icon: '✓', tone: 'bg-brand-200 text-brand-800 dark:bg-brand-900 dark:text-brand-100' },
  Milestone: { icon: '◆', tone: 'bg-stone-200 text-stone-700 dark:bg-stone-700 dark:text-stone-200' },
  Investment: { icon: '₺', tone: 'bg-emerald-500 text-white' },
  Grant: { icon: '⬢', tone: 'bg-brand-700 text-white' },
  Revenue: { icon: '▲', tone: 'bg-brand-600 text-white' },
  Export: { icon: '⇄', tone: 'bg-brand-800 text-white' },
  Award: { icon: '🏆', tone: 'bg-gold-500 text-stone-900' },
}
