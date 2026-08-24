import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { ChangeRequestStatus } from '@/api/types'
import { useAuth } from '@/lib/auth'
import { formatDate } from '@/lib/format'
import { Badge, Card, EmptyState, ErrorState, Spinner } from '@/components/ui'
import { defaultQueueFilters, useChangeRequests } from './queries'
import { changeStatusLabels, changeStatusTone, waitingLabel, waitingTone } from './labels'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

/**
 * Onay kuyruğu (MVP #3). Aynı ekran iki iş görüyor: yetkili için karar
 * bekleyen öneriler listesi, girişim kullanıcısı için "gönderdiğim
 * önerilerin durumu". Ayrımı sunucudaki kapsam yapıyor, arayüz yalnızca
 * başlığı değiştiriyor — iki ayrı sayfa iki ayrı sorgu ve iki ayrı hata
 * yolu demek olurdu.
 */
export default function ApprovalsPage() {
  const { session } = useAuth()
  const isReviewer = session?.permissions.canReviewApprovals ?? false
  useDocumentTitle(isReviewer ? 'Onay kuyruğu' : 'Önerilerim')
  const [filters, setFilters] = useState(defaultQueueFilters)
  const { data, isPending, error } = useChangeRequests(filters)

  const tabs: { value: ChangeRequestStatus | ''; label: string; count?: number }[] = [
    { value: 'Pending', label: 'Bekleyen', count: data?.pendingCount },
    { value: 'Approved', label: 'Onaylanan', count: data?.approvedCount },
    { value: 'Rejected', label: 'Reddedilen', count: data?.rejectedCount },
    { value: '', label: 'Tümü' },
  ]

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-stone-900 dark:text-stone-50">
            {isReviewer ? 'Onay kuyruğu' : 'Önerilerim'}
          </h1>
          <p className="mt-1 text-sm text-stone-500">
            {isReviewer
              ? 'Girişimlerden gelen değişiklik önerileri. Onaylanmadan hiçbir veri yayına girmez.'
              : 'Gönderdiğiniz değişiklik önerileri ve yönetici kararları.'}
          </p>
        </div>
      </header>

      <div className="flex flex-wrap gap-2">
        {tabs.map((tab) => (
          <button
            key={tab.label}
            type="button"
            onClick={() => setFilters({ ...filters, status: tab.value, page: 1 })}
            className={`rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
              filters.status === tab.value
                ? 'bg-brand-500 text-white'
                : 'bg-stone-100 text-stone-600 hover:bg-brand-100 hover:text-brand-700 dark:bg-stone-800 dark:text-stone-300'
            }`}
          >
            {tab.label}
            {tab.count === undefined ? null : (
              <span className="ml-1.5 opacity-70">({tab.count})</span>
            )}
          </button>
        ))}
      </div>

      {error ? <ErrorState message={error.message} /> : null}
      {isPending ? <Spinner label="Kuyruk yükleniyor…" /> : null}

      {data && data.page.items.length === 0 ? (
        <EmptyState
          title="Bu filtrede öneri yok"
          hint={
            isReviewer
              ? 'Kapsamınızdaki girişimlerden bekleyen bir değişiklik önerisi bulunmuyor.'
              : 'Girişim portalından profil veya ekip değişikliği önerdiğinizde burada listelenir.'
          }
        />
      ) : null}

      <div className="flex flex-col gap-3">
        {data?.page.items.map((item) => (
          <Link key={item.id} to={`/onaylar/${item.id}`} className="block">
            <Card className="px-5 py-4 transition-colors hover:border-brand-400">
              <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
                <Badge tone={changeStatusTone[item.status]}>
                  {changeStatusLabels[item.status]}
                </Badge>

                {/* Hedef etiketi doküman adı taşıyabiliyor; alt tire içeren uzun
                    dosya adı bölünmezse satır telefonda ekranı taşırıyor. */}
                <div className="min-w-0 flex-1 sm:min-w-48">
                  <p className="font-medium break-words text-stone-900 dark:text-stone-100">
                    {item.startupName}
                  </p>
                  <p className="text-sm break-words text-stone-500">
                    {item.targetLabel} · {item.operationLabel}
                    {item.changedFieldCount > 0
                      ? ` · ${item.changedFieldCount} alan değişiyor`
                      : null}
                  </p>
                </div>

                <div className="text-right text-sm">
                  <p className="text-stone-700 dark:text-stone-200">{item.submittedByName}</p>
                  <p className="text-stone-500">{formatDate(item.submittedAt)}</p>
                </div>

                {item.status === 'Pending' ? (
                  <Badge tone={waitingTone(item.waitingDays)}>
                    {waitingLabel(item.waitingDays)}
                  </Badge>
                ) : (
                  <Badge>
                    {item.reviewedByName ?? '—'} · {formatDate(item.reviewedAt)}
                  </Badge>
                )}
              </div>

              {item.reviewNote ? (
                <p className="mt-3 border-l-2 border-stone-300 pl-3 text-sm text-stone-600 dark:border-stone-700 dark:text-stone-300">
                  {item.reviewNote}
                </p>
              ) : null}
            </Card>
          </Link>
        ))}
      </div>

      {data && data.page.totalPages > 1 ? (
        <div className="flex items-center justify-center gap-3 text-sm">
          <button
            type="button"
            disabled={filters.page <= 1}
            onClick={() => setFilters({ ...filters, page: filters.page - 1 })}
            className="rounded-lg px-3 py-1.5 disabled:opacity-40"
          >
            ← Önceki
          </button>
          <span className="text-stone-500">
            {data.page.page} / {data.page.totalPages}
          </span>
          <button
            type="button"
            disabled={!data.page.hasNext}
            onClick={() => setFilters({ ...filters, page: filters.page + 1 })}
            className="rounded-lg px-3 py-1.5 disabled:opacity-40"
          >
            Sonraki →
          </button>
        </div>
      ) : null}
    </div>
  )
}
