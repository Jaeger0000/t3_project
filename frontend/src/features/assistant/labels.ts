import type { AssistantMode } from '@/api/types'

/**
 * Örnek sorular. Pano paneli ile sohbet ekranı aynı listeyi gösteriyor:
 * kullanıcı iki ekranda iki farklı "başlangıç sorusu" seti görürse hangisinin
 * gerçekten çalıştığını denemekle öğrenmek zorunda kalır.
 */
export const assistantExamples = [
  'Savunma sektöründe kaç girişim var?',
  "TEKNOFEST'ten geçmiş girişimler hangileri?",
  'En çok yatırım alan enerji girişimlerini listele',
]

/**
 * Mod rozetinin metni. `Local`, model anahtarı tanımlı değilken yerel anahtar
 * sözcük planlayıcısının yanıtladığını söyler — kullanıcı neden daha kısıtlı
 * bir cevap aldığını ekrandan anlamalı, yoksa "AI çalışıyor" sanılır.
 */
export function assistantModeLabel(mode: AssistantMode, modelName: string | null): string {
  return mode === 'Model' ? `Model: ${modelName ?? '—'}` : 'Yerel plan (model yok)'
}

export const assistantModeTone: Record<AssistantMode, string> = {
  Model: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300',
  Local: 'bg-brand-100 text-brand-800 dark:bg-brand-950 dark:text-brand-200',
}
