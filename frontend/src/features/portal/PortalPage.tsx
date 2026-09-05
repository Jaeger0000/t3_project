import { Link } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { useStartupCard } from '@/features/startups/queries'
import StartupForm from '@/features/startups/StartupForm'
import TeamSection from '@/features/startups/TeamSection'
import AchievementSection from '@/features/achievements/AchievementSection'
import DocumentSection from '@/features/documents/DocumentSection'
import { ErrorState, Spinner } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

/**
 * Girişim portalı (MVP #3).
 *
 * Buradaki hiçbir form doğrudan yazmaz; hepsi `/api/change-requests` üzerinden
 * öneri üretir. Ekranın metinleri de bunu açıkça söyler — kullanıcı "kaydettim"
 * sanıp değişikliğin yayına girdiğini varsaymamalı.
 */
export default function PortalPage() {
  const { session } = useAuth()
  const startupId = session?.startupId ?? undefined
  const { data: card, isPending, error } = useStartupCard(startupId)

  useDocumentTitle(card ? `${card.name} portalı` : 'Girişim portalı')

  if (!startupId) {
    return (
      <ErrorState message="Hesabınız bir girişime bağlı değil; portal ekranı yalnızca girişim kullanıcılarına açıktır." />
    )
  }
  if (isPending) return <Spinner label="Girişim bilgileri yükleniyor…" />
  if (error) return <ErrorState message={error.message} error={error} />
  if (!card) return null

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-stone-900 dark:text-stone-50">
            {card.name}
          </h1>
          <p className="mt-1 text-sm text-stone-500">
            Girişim portalı — buradan gönderdiğiniz her değişiklik program
            yöneticisinin onayından sonra yayına girer.
          </p>
        </div>
        <Link
          to="/onaylar"
          className="text-sm text-brand-700 hover:underline dark:text-brand-300"
        >
          Önerilerim ve durumları →
        </Link>
      </header>

      {/* İki form da girişim diliminden geliyor; `proposal` kipi bugünkü
          davranışı koruyor: hiçbiri tabloya doğrudan yazmıyor. */}
      <StartupForm mode="proposal" card={card} />

      <hr className="border-stone-200 dark:border-stone-800" />
      <TeamSection card={card} mode="proposal" />

      <hr className="border-stone-200 dark:border-stone-800" />
      <AchievementSection startupId={startupId} mode="proposal" />

      <hr className="border-stone-200 dark:border-stone-800" />
      <DocumentSection startupId={startupId} mode="proposal" />
    </div>
  )
}
