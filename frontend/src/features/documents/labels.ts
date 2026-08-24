import type { DocumentType } from '@/api/types'

/**
 * Yükleme formundaki tür seçenekleri. Kaydedilmiş dokümanın etiketi sunucudan
 * geliyor (`typeLabel`); bu tablo yalnızca henüz kaydedilmemiş seçim için.
 */
export const documentTypeLabels: Record<DocumentType, string> = {
  PitchDeck: 'Sunum',
  Financials: 'Finansal tablo',
  Incorporation: 'Kuruluş belgesi',
  Patent: 'Patent / fikrî mülkiyet',
  Report: 'Rapor',
  Contract: 'Sözleşme',
  Other: 'Diğer',
}
