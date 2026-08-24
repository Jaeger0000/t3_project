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
  Investment: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-200',
  Grant: 'bg-brand-100 text-brand-800 dark:bg-brand-950 dark:text-brand-200',
  Revenue: 'bg-brand-50 text-brand-700 dark:bg-brand-950 dark:text-brand-300',
  Export: 'bg-brand-200 text-brand-800 dark:bg-brand-900 dark:text-brand-100',
  Award: 'bg-gold-100 text-stone-800 dark:bg-gold-500 dark:text-stone-900',
}

/** Hangi türde hangi alanların anlamlı olduğu — form da doğrulayıcı da aynı bağı kurar. */
export const achievementFields = {
  hasMoney: (kind: AchievementKind) =>
    kind === 'Revenue' || kind === 'Export' || kind === 'Investment' || kind === 'Grant',
  hasPeriod: (kind: AchievementKind) => kind === 'Revenue' || kind === 'Export',
}
