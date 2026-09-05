import { useState } from 'react'
import type { RegistrationRequestRow, RegistrationRequestStatus } from '@/api/types'
import { formatDate } from '@/lib/format'
import { sectorLabels } from '@/lib/labels'
import { Badge, Button, Card, EmptyState, ErrorState, Input, Spinner } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import {
  defaultRegistrationFilters,
  useApproveRegistrationRequest,
  useRegistrationRequests,
  useRejectRegistrationRequest,
} from './queries'

const statusLabels: Record<RegistrationRequestStatus, string> = {
  Pending: 'Bekleyen',
  Approved: 'Onaylanan',
  Rejected: 'Reddedilen',
}

const statusTone: Record<RegistrationRequestStatus, string> = {
  Pending: 'bg-amber-50 text-amber-800 dark:bg-amber-950 dark:text-amber-200',
  Approved: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300',
  Rejected: 'bg-stone-200 text-stone-700 dark:bg-stone-800 dark:text-stone-300',
}

/**
 * Ana sayfadaki "Kayıt Ol" formundan gelen başvuruların yönetimi.
 *
 * Onay geri alınamaz — gerçek `Startup` ve `StartupUser` hesabı burada
 * doğuyor — bu yüzden düğme tek tıkla değil `ApprovalsPage`'teki karar
 * paneliyle aynı iki adımlı örüntüyle çalışıyor: önce "onaylamak üzeresiniz"
 * uyarısı, sonra kesin düğme.
 */
export default function RegistrationRequestsPage() {
  useDocumentTitle('Kayıt başvuruları')
  const [filters, setFilters] = useState(defaultRegistrationFilters)
  const { data, isPending, error } = useRegistrationRequests(filters)

  const tabs: { value: RegistrationRequestStatus | ''; label: string }[] = [
    { value: 'Pending', label: 'Bekleyen' },
    { value: 'Approved', label: 'Onaylanan' },
    { value: 'Rejected', label: 'Reddedilen' },
    { value: '', label: 'Tümü' },
  ]

  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-semibold text-stone-900 dark:text-stone-50">
          Kayıt başvuruları
        </h1>
        <p className="mt-1 text-sm text-stone-500">
          Ana sayfadaki "Kayıt Ol" formundan gelen girişim başvuruları. Onay,
          gerçek bir girişim kartı ve portal hesabı açar; reddetme hiçbir kayıt
          oluşturmaz.
        </p>
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
          </button>
        ))}
      </div>

      {error ? <ErrorState message={error.message} error={error} /> : null}
      {isPending ? <Spinner label="Başvurular yükleniyor…" /> : null}

      {data && data.items.length === 0 ? (
        <EmptyState
          title="Bu filtrede başvuru yok"
          hint="Ana sayfadaki kayıt formundan yeni bir başvuru geldiğinde burada listelenir."
        />
      ) : null}

      <div className="flex flex-col gap-3">
        {data?.items.map((item) => (
          <RegistrationRequestCard key={item.id} item={item} />
        ))}
      </div>

      {data && data.totalPages > 1 ? (
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
            {data.page} / {data.totalPages}
          </span>
          <button
            type="button"
            disabled={!data.hasNext}
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

function RegistrationRequestCard({ item }: { item: RegistrationRequestRow }) {
  const [mode, setMode] = useState<'none' | 'confirm-approve' | 'reject'>('none')
  const approve = useApproveRegistrationRequest(item.id)

  return (
    <Card className="px-5 py-4">
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <div className="min-w-48 flex-1">
          <p className="font-medium break-words text-stone-900 dark:text-stone-100">
            {item.startupName}
          </p>
          <p className="text-sm break-words text-stone-500">
            {item.fullName} · {item.email}
          </p>
        </div>

        <Badge>{sectorLabels[item.sector]}</Badge>

        <Badge tone={statusTone[item.status]}>{statusLabels[item.status]}</Badge>

        <span className="text-xs text-stone-500">
          Başvuru {formatDate(item.createdAt)}
        </span>

        {item.status === 'Pending' ? (
          <div className="flex gap-1">
            <Button
              variant="ghost"
              onClick={() => setMode(mode === 'confirm-approve' ? 'none' : 'confirm-approve')}
            >
              Onayla
            </Button>
            <Button variant="ghost" onClick={() => setMode(mode === 'reject' ? 'none' : 'reject')}>
              Reddet
            </Button>
          </div>
        ) : null}
      </div>

      {item.status !== 'Pending' ? (
        <p className="mt-3 text-sm text-stone-600 dark:text-stone-300">
          {formatDate(item.reviewedAt)} tarihinde sonuçlandırıldı.
          {item.rejectionReason ? (
            <span className="mt-1 block border-l-2 border-stone-300 pl-3 text-stone-600 dark:border-stone-700 dark:text-stone-300">
              Gerekçe: {item.rejectionReason}
            </span>
          ) : null}
        </p>
      ) : null}

      {mode === 'confirm-approve' ? (
        <div className="mt-4 flex flex-wrap items-center gap-3 border-t border-stone-100 pt-4 dark:border-stone-800">
          <span className="text-sm text-stone-500">
            Onaylandığında "{item.startupName}" girişim kartı ve "{item.email}" portal
            hesabı hemen açılır. Geri alınamaz.
          </span>
          <Button
            disabled={approve.isPending}
            onClick={() => approve.mutate(undefined, { onSuccess: () => setMode('none') })}
          >
            {approve.isPending ? 'Onaylanıyor…' : 'Evet, onayla ve hesap aç'}
          </Button>
          <Button variant="outline" disabled={approve.isPending} onClick={() => setMode('none')}>
            Vazgeç
          </Button>
        </div>
      ) : null}

      {approve.error ? (
        <div className="mt-3">
          <ErrorState message={approve.error.message} error={approve.error} />
        </div>
      ) : null}

      {mode === 'reject' ? <RejectForm item={item} onDone={() => setMode('none')} /> : null}
    </Card>
  )
}

/**
 * Ret gerekçesi: sunucu 10-2000 karakter arası zorunlu kılıyor
 * (`RejectRegistrationRequestValidator`), düğme bu eşiğin altında kapalı
 * kalıyor ki kullanıcı sunucu hatasını görmeden neyin eksik olduğunu anlasın.
 */
function RejectForm({ item, onDone }: { item: RegistrationRequestRow; onDone: () => void }) {
  const reject = useRejectRegistrationRequest(item.id)
  const [note, setNote] = useState('')
  const tooShort = note.trim().length < 10

  return (
    <div className="mt-4 flex flex-col gap-2 border-t border-stone-100 pt-4 dark:border-stone-800">
      <Input
        label="Ret gerekçesi"
        value={note}
        onChange={(e) => setNote(e.target.value)}
        placeholder="En az 10 karakter — başvuru sahibi bu gerekçeyi göremez ama SuperAdmin izinde kalır."
      />
      <div className="flex flex-wrap items-center gap-3">
        <Button
          variant="outline"
          disabled={reject.isPending || tooShort}
          onClick={() => reject.mutate(note.trim(), { onSuccess: onDone })}
        >
          {reject.isPending ? 'Reddediliyor…' : 'Reddet'}
        </Button>
        <Button variant="ghost" disabled={reject.isPending} onClick={onDone}>
          Vazgeç
        </Button>
        {tooShort ? (
          <span className="text-xs text-stone-500">Gerekçe en az 10 karakter olmalı.</span>
        ) : null}
      </div>
      {reject.error ? <ErrorState message={reject.error.message} error={reject.error} /> : null}
    </div>
  )
}
