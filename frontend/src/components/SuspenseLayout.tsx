import { Suspense } from 'react'
import { Outlet } from 'react-router-dom'
import { Spinner } from '@/components/ui'

/**
 * Tek Suspense sınırı: rota geçişinde parça inerken ekranda aynı bekleme
 * göstergesi duruyor, her rotaya ayrı fallback yazmak gerekmiyor.
 */
export default function SuspenseLayout() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-screen items-center justify-center">
          <Spinner label="Ekran yükleniyor…" />
        </div>
      }
    >
      <Outlet />
    </Suspense>
  )
}

