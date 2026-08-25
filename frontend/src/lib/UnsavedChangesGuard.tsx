import { useEffect } from 'react'
import { useBlocker } from 'react-router-dom'
import { Button } from '@/components/ui'

/**
 * Doldurulmuş ama kaydedilmemiş formdan çıkışta uyarı.
 *
 * İki farklı çıkış yolu var ve ikisi de kapatılmak zorunda:
 * <ul>
 *   <li>Uygulama içinde gezinme — `useBlocker` (veri yönlendiricisi gerektirir,
 *       bkz. main.tsx). Kendi diyaloğumuzu gösteriyoruz: tarayıcının
 *       `confirm()` penceresi İngilizce ve biçimlendirilemiyor.</li>
 *   <li>Sekmeyi kapatma / yenileme — `beforeunload`. Burada metni tarayıcı
 *       seçiyor, biz yalnızca "sorulsun" diyebiliyoruz.</li>
 * </ul>
 *
 * Girişim profili formu 15'ten fazla alan taşıyor; yanlış tıklamayla hepsini
 * kaybetmek veri girişini caydıran, sessiz bir maliyet.
 */
export default function UnsavedChangesGuard({ dirty }: { dirty: boolean }) {
  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      dirty && currentLocation.pathname + currentLocation.search !== nextLocation.pathname + nextLocation.search,
  )

  useEffect(() => {
    if (!dirty) return

    const handler = (event: BeforeUnloadEvent) => event.preventDefault()
    window.addEventListener('beforeunload', handler)
    return () => window.removeEventListener('beforeunload', handler)
  }, [dirty])

  if (blocker.state !== 'blocked') return null

  return (
    <div className="rounded-lg border border-brand-300 bg-brand-50 px-4 py-3 dark:border-brand-800 dark:bg-brand-950">
      <p className="text-sm font-medium text-stone-900 dark:text-stone-100">
        Kaydedilmemiş değişiklikler var
      </p>
      <p className="mt-1 text-sm text-stone-600 dark:text-stone-300">
        Bu sayfadan ayrılırsanız girdiğiniz bilgiler kaybolacak.
      </p>
      <div className="mt-3 flex flex-wrap gap-2">
        <Button variant="outline" onClick={() => blocker.reset()}>
          Formda kal
        </Button>
        <Button onClick={() => blocker.proceed()}>Ayrıl, kaydetmeden</Button>
      </div>
    </div>
  )
}
