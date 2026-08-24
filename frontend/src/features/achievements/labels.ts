import type { AchievementKind, GrantInstitution, InvestmentRoundType } from '@/api/types'

/**
 * Form açılırlarının etiketleri. Listelenen kayıtların etiketi sunucudan
 * geliyor (`kindLabel`, `roundTypeLabel`); buradaki tablo yalnızca henüz
 * kaydedilmemiş seçenekler için — kaydedilmiş bir kaydın etiketi iki yerden
 * üretilmiyor.
 */
export const achievementKindLabels: Record<AchievementKind, string> = {
  Revenue: 'Ciro',
  Export: 'İhracat',
  Investment: 'Yatırım turu',
  Grant: 'Hibe / destek',
  Award: 'Ödül',
}

export const investmentRoundLabels: Record<InvestmentRoundType, string> = {
  Angel: 'Melek yatırım',
  PreSeed: 'Pre-Seed',
  Seed: 'Seed',
  SeriesA: 'Seri A',
  SeriesB: 'Seri B',
  SeriesC: 'Seri C',
  Debt: 'Borç finansmanı',
  Other: 'Diğer',
}

export const grantInstitutionLabels: Record<GrantInstitution, string> = {
  Tubitak: 'TÜBİTAK',
  Kosgeb: 'KOSGEB',
  Teknofest: 'TEKNOFEST',
  EuropeanUnion: 'Avrupa Birliği',
  Ministry: 'Bakanlık',
  DevelopmentAgency: 'Kalkınma Ajansı',
  Other: 'Diğer kurum',
}

/** Kayıt türüne göre rozet rengi: kartta finansal ve finansal olmayan ayrışsın. */
export const achievementKindTone: Record<AchievementKind, string> = {
  Investment: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200',
  Grant: 'bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-200',
  Revenue: 'bg-brand-50 text-brand-900 dark:bg-brand-950 dark:text-brand-100',
  Export: 'bg-violet-100 text-violet-800 dark:bg-violet-950 dark:text-violet-200',
  Award: 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-200',
}

/** Hangi türde hangi alanların anlamlı olduğu — form da doğrulayıcı da aynı bağı kurar. */
export const achievementFields = {
  hasMoney: (kind: AchievementKind) =>
    kind === 'Revenue' || kind === 'Export' || kind === 'Investment' || kind === 'Grant',
  hasPeriod: (kind: AchievementKind) => kind === 'Revenue' || kind === 'Export',
}
