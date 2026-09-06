import { useState } from 'react'
import { api, ApiError } from '@/lib/apiClient'
import { Button, Card, ErrorState } from '@/components/ui'
import type { StartupCard } from '@/api/types'

/**
 * Standart rapor bölümleri. Anahtarlar backend'deki `ReportSections.Standard`
 * ile birebir eşleşmek zorunda — burada ayrıca doğrulanmıyor, bilinmeyen bir
 * anahtar sunucuda sessizce yok sayılır (bkz. GenerateStartupReportHandler
 * .ResolveSections).
 */
const SECTIONS: { key: string; label: string }[] = [
  { key: 'Ozet', label: 'Yönetici özeti' },
  { key: 'GucluYonler', label: 'Güçlü yönler ve öne çıkanlar' },
  { key: 'Oneriler', label: 'Gelişim önerileri' },
  { key: 'EkipVeFaaliyetler', label: 'Ekip ve faaliyetler' },
  { key: 'ProgramGecmisi', label: 'Program geçmişi ve kronoloji' },
  { key: 'BasariVeYatirim', label: 'Başarı, yatırım ve finans durumu' },
]

/**
 * Girişim için AI destekli PDF raporu isteme paneli.
 *
 * Model her bölüme AYRI bir çağrıda yanıt verir (bkz. backend), bu yüzden
 * tam rapor 10-20 saniye sürebilir — düğme bu süre boyunca kilitli ve
 * bekleme metni gösteriyor, "tıklama çalışmadı" izlenimini önlemek için.
 */
export default function AiReportPanel({
  startup,
  onClose,
}: {
  startup: StartupCard
  onClose: () => void
}) {
  const [selected, setSelected] = useState<Set<string>>(new Set(SECTIONS.map((s) => s.key)))
  const [customFocus, setCustomFocus] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const toggle = (key: string) => {
    setSelected((previous) => {
      const next = new Set(previous)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })
  }

  const trimmedFocus = customFocus.trim()
  // Hiç bölüm seçilmemişse ve özel istek de yoksa arka planda "tam rapor"
  // üretilir (backend varsayılanı) — burada erken uyarmak, boş bir isteğin
  // 10-20 saniye sonra "tam rapor" olarak dönmesini şaşırtıcı bulmamalı.
  const willGenerateFull = selected.size === 0 && trimmedFocus.length === 0

  async function handleGenerate() {
    setBusy(true)
    setError(null)

    const params = new URLSearchParams()
    for (const key of selected) params.append('sections', key)
    if (trimmedFocus) params.set('customFocus', trimmedFocus)

    const query = params.toString()
    const fileName = `${startup.name.replace(/\s+/g, '-')}-ai-raporu.pdf`

    try {
      await api.download(
        `/api/startups/${startup.id}/ai-report${query ? `?${query}` : ''}`,
        fileName,
      )
      onClose()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Rapor oluşturulamadı.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Card className="p-5">
      <h2 className="font-semibold text-stone-900 dark:text-stone-100">AI Raporu</h2>
      <p className="mt-1 text-sm text-stone-500">
        Model her bölüme, tüm girişim verisini kullanarak ayrı ayrı yanıt verir. Bölüm
        seçmezseniz tam rapor, yalnızca özel istek yazarsanız yalnızca o konu üretilir.
      </p>

      <div className="mt-4 grid gap-2 sm:grid-cols-2">
        {SECTIONS.map((section) => (
          <label
            key={section.key}
            className="flex items-center gap-2 text-sm text-stone-700 dark:text-stone-300"
          >
            <input
              type="checkbox"
              checked={selected.has(section.key)}
              onChange={() => toggle(section.key)}
              disabled={busy}
            />
            {section.label}
          </label>
        ))}
      </div>

      <label className="mt-4 flex flex-col gap-1.5">
        <span className="text-sm font-medium text-stone-700 dark:text-stone-300">
          Özel istek (opsiyonel)
        </span>
        <textarea
          rows={2}
          maxLength={1000}
          disabled={busy}
          placeholder='Örn. "Bu girişimin büyümesi için yapılması gerekenlerin bir önerisini hazırla"'
          value={customFocus}
          onChange={(e) => setCustomFocus(e.target.value)}
          className="rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/60 disabled:opacity-60 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100"
        />
      </label>

      {willGenerateFull ? (
        <p className="mt-2 text-xs text-stone-500">
          Hiçbir bölüm seçili değil ve özel istek boş — tam rapor oluşturulacak.
        </p>
      ) : null}

      {error ? (
        <div className="mt-4">
          <ErrorState message={error} />
        </div>
      ) : null}

      {busy ? (
        <p className="mt-4 text-sm text-stone-500" aria-live="polite">
          Rapor hazırlanıyor, bu birkaç dakika sürebilir…
        </p>
      ) : null}

      <div className="mt-5 flex flex-wrap gap-3">
        <Button disabled={busy} onClick={handleGenerate}>
          {busy ? 'Hazırlanıyor…' : 'PDF oluştur'}
        </Button>
        <Button variant="outline" disabled={busy} onClick={onClose}>
          Vazgeç
        </Button>
      </div>
    </Card>
  )
}
