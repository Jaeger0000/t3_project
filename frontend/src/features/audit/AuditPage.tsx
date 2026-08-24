import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { AuditLogRow, PagedResult } from '@/api/types'
import { api } from '@/lib/apiClient'
import { roleLabels } from '@/lib/labels'
import { Badge, Card, EmptyState, ErrorState, Input, Select, Spinner } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

const timestamp = new Intl.DateTimeFormat('tr-TR', {
  dateStyle: 'medium',
  timeStyle: 'medium',
})

/** Uçtaki eylem adları kodda üretiliyor; süzgeç önek eşleşmesi yapıyor. */
const actionGroups = [
  { value: '', label: 'Tümü' },
  { value: 'ChangeRequest', label: 'Onay akışı' },
  { value: 'Startup', label: 'Girişim' },
  { value: 'TeamMember', label: 'Ekip' },
  { value: 'User', label: 'Kullanıcı' },
  { value: 'Auth', label: 'Oturum' },
]

/**
 * Denetim izi — yalnızca sistem yöneticisine açık.
 *
 * Gövdeler ham JSON olarak gösteriliyor: iz kanıt niteliği taşıdığı için
 * biçimlendirme, alan atlama ya da yeniden adlandırma yapılmıyor.
 */
export default function AuditPage() {
  useDocumentTitle('Denetim izi')
  const [action, setAction] = useState('')
  const [entityId, setEntityId] = useState('')
  const [page, setPage] = useState(1)

  const query = useQuery({
    queryKey: ['audit-logs', action, entityId, page],
    queryFn: () => {
      const params = new URLSearchParams()
      if (action) params.set('action', action)
      if (entityId.trim()) params.set('entityId', entityId.trim())
      params.set('page', String(page))
      params.set('pageSize', '25')
      return api.get<PagedResult<AuditLogRow>>(`/api/audit-logs?${params}`)
    },
    placeholderData: (previous) => previous,
  })

  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-semibold text-stone-900 dark:text-stone-50">
          Denetim izi
        </h1>
        <p className="mt-1 text-sm text-stone-500">
          Kim, ne zaman, neyi değiştirdi. Kayıtlar silinmez ve değiştirilemez; kullanıcı
          hesabı kapatılsa bile iz ayakta kalır.
        </p>
      </header>

      <Card className="px-5 py-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <Select
            label="Eylem grubu"
            value={action}
            onChange={(e) => {
              setAction(e.target.value)
              setPage(1)
            }}
          >
            {actionGroups.map((g) => (
              <option key={g.value} value={g.value}>
                {g.label}
              </option>
            ))}
          </Select>
          <Input
            label="Kayıt kimliği (isteğe bağlı)"
            placeholder="Bir girişimin veya kullanıcının GUID'i"
            value={entityId}
            onChange={(e) => {
              setEntityId(e.target.value)
              setPage(1)
            }}
          />
        </div>
      </Card>

      {query.error ? <ErrorState message={query.error.message} /> : null}
      {query.isPending ? <Spinner label="İz yükleniyor…" /> : null}
      {query.data && query.data.items.length === 0 ? (
        <EmptyState title="Bu filtrede kayıt yok" />
      ) : null}

      <div className="flex flex-col gap-2">
        {query.data?.items.map((row) => (
          <AuditRow key={row.id} row={row} />
        ))}
      </div>

      {query.data && query.data.totalPages > 1 ? (
        <div className="flex items-center justify-center gap-3 text-sm">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage(page - 1)}
            className="rounded-lg px-3 py-1.5 disabled:opacity-40"
          >
            ← Önceki
          </button>
          <span className="text-stone-500">
            {query.data.page} / {query.data.totalPages} · {query.data.totalCount} kayıt
          </span>
          <button
            type="button"
            disabled={!query.data.hasNext}
            onClick={() => setPage(page + 1)}
            className="rounded-lg px-3 py-1.5 disabled:opacity-40"
          >
            Sonraki →
          </button>
        </div>
      ) : null}
    </div>
  )
}

function AuditRow({ row }: { row: AuditLogRow }) {
  const [open, setOpen] = useState(false)
  const hasBody = Boolean(row.beforeJson || row.afterJson)

  return (
    <Card className="px-5 py-3">
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <Badge tone="bg-brand-50 text-brand-800 dark:bg-brand-950 dark:text-brand-100">
          {row.action}
        </Badge>
        <div className="min-w-40 flex-1 text-sm">
          <p className="text-stone-900 dark:text-stone-100">{row.actorName}</p>
          <p className="text-stone-500">{roleLabels[row.actorRole]}</p>
        </div>
        <span className="text-sm text-stone-500">{row.entityType}</span>
        <span className="text-sm text-stone-500">
          {timestamp.format(new Date(row.occurredAt))}
        </span>
        <span className="text-xs text-stone-400">{row.ipAddress ?? '—'}</span>
        {hasBody ? (
          <button
            type="button"
            onClick={() => setOpen(!open)}
            className="text-sm text-brand-700 hover:underline dark:text-brand-300"
          >
            {open ? 'Gövdeyi gizle' : 'Gövdeyi göster'}
          </button>
        ) : null}
      </div>

      {open ? (
        <div className="mt-3 grid gap-3 sm:grid-cols-2">
          <JsonBlock title="Önce" json={row.beforeJson} />
          <JsonBlock title="Sonra" json={row.afterJson} />
        </div>
      ) : null}
    </Card>
  )
}

function JsonBlock({ title, json }: { title: string; json: string | null }) {
  return (
    <div>
      <p className="mb-1 text-xs font-medium tracking-wide text-stone-500 uppercase">
        {title}
      </p>
      <pre className="max-h-64 overflow-auto rounded-lg bg-stone-50 p-3 text-xs text-stone-700 dark:bg-stone-800/60 dark:text-stone-200">
        {json ? prettify(json) : '—'}
      </pre>
    </div>
  )
}

/** Okunabilirlik için girintileme; bozuk JSON gelirse ham metin gösterilir. */
function prettify(json: string): string {
  try {
    return JSON.stringify(JSON.parse(json), null, 2)
  } catch {
    return json
  }
}
