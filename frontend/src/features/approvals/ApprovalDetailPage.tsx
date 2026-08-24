import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import type { DiffField } from '@/api/types'
import { ApiError } from '@/lib/apiClient'
import { formatDate } from '@/lib/format'
import { Badge, Button, Card, ErrorState, Spinner } from '@/components/ui'
import { useChangeRequest, useReviewChangeRequest } from './queries'
import { changeStatusLabels, changeStatusTone } from './labels'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

/**
 * Before/after diff ekranı — MVP #3'ün jüriye gösterilen yüzü.
 *
 * Maskelenmiş alanlar satırdan silinmiyor, "değişiyor ama göremezsiniz"
 * olarak gösteriliyor: yetkisiz bir yönetici bile kararını "hangi alanlar
 * dokunuluyor" bilgisiyle verebilmeli.
 */
export default function ApprovalDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { data, isPending, error } = useChangeRequest(id)
  useDocumentTitle(data ? `${data.startupName} önerisi` : 'Öneri')

  if (isPending) return <Spinner label="Öneri yükleniyor…" />
  if (error) {
    return (
      <ErrorState
        message={
          error instanceof ApiError && error.status === 404
            ? 'Bu öneri bulunamadı ya da görüntüleme yetkiniz yok.'
            : error.message
        }
      />
    )
  }
  if (!data) return null

  const decided = data.status !== 'Pending'

  return (
    <div className="flex flex-col gap-6">
      <div>
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="text-sm text-stone-500 hover:text-brand-700"
        >
          ← Kuyruğa dön
        </button>
      </div>

      <Card className="px-6 py-5">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-3">
              <h1 className="text-xl font-semibold text-stone-900 dark:text-stone-50">
                {data.startupName}
              </h1>
              <Badge tone={changeStatusTone[data.status]}>
                {changeStatusLabels[data.status]}
              </Badge>
            </div>
            <p className="mt-1 text-sm text-stone-500">
              {data.targetLabel} · {data.operationLabel} · {data.changedFieldCount} alan
              değişiyor
            </p>
            <p className="mt-2 text-sm text-stone-600 dark:text-stone-300">
              <span className="font-medium">{data.submittedByName}</span> gönderdi ·{' '}
              {formatDate(data.submittedAt)}
            </p>
            {decided ? (
              <p className="mt-1 text-sm text-stone-600 dark:text-stone-300">
                <span className="font-medium">{data.reviewedByName ?? '—'}</span> karar
                verdi · {formatDate(data.reviewedAt)}
              </p>
            ) : null}
          </div>

          <Link
            to={`/girisimler/${data.startupId}`}
            className="text-sm text-brand-700 hover:underline dark:text-brand-300"
          >
            Girişim kartını aç →
          </Link>
        </div>

        {data.reviewNote ? (
          <p className="mt-4 rounded-lg bg-stone-50 px-4 py-3 text-sm text-stone-700 dark:bg-stone-800/60 dark:text-stone-200">
            <span className="font-medium">Karar notu:</span> {data.reviewNote}
          </p>
        ) : null}
      </Card>

      {data.isReadable ? (
        <DiffTable fields={data.fields} />
      ) : (
        <ErrorState message="Öneri gövdesi okunamadı; bu öneri uygulanamaz. Lütfen girişimden yeniden göndermesini isteyin." />
      )}

      {data.canReview && !decided && data.isReadable ? (
        <ReviewPanel id={data.id} />
      ) : null}
    </div>
  )
}

function DiffTable({ fields }: { fields: DiffField[] }) {
  const [showAll, setShowAll] = useState(false)
  const changed = fields.filter((f) => f.changed)
  const visible = showAll ? fields : changed

  return (
    <Card className="overflow-hidden">
      <div className="flex items-center justify-between border-b border-stone-200 px-6 py-3 dark:border-stone-800">
        <h2 className="font-medium text-stone-900 dark:text-stone-100">
          Değişiklik karşılaştırması
        </h2>
        <button
          type="button"
          onClick={() => setShowAll(!showAll)}
          className="text-sm text-brand-700 hover:underline dark:text-brand-300"
        >
          {showAll ? 'Yalnızca değişenler' : `Tüm alanlar (${fields.length})`}
        </button>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-stone-50 text-left text-xs tracking-wide text-stone-500 uppercase dark:bg-stone-800/60">
            <tr>
              <th className="px-6 py-2 font-medium">Alan</th>
              <th className="px-6 py-2 font-medium">Mevcut</th>
              <th className="px-6 py-2 font-medium">Önerilen</th>
            </tr>
          </thead>
          <tbody>
            {visible.map((field) => (
              <tr
                key={field.field}
                className={`border-t border-stone-100 dark:border-stone-800 ${
                  field.changed ? 'bg-brand-50 dark:bg-brand-950/20' : ''
                }`}
              >
                <td className="px-6 py-3 align-top font-medium text-stone-700 dark:text-stone-200">
                  {field.label}
                  {field.masked ? (
                    <span
                      title="Bu alanı görme yetkiniz yok"
                      className="ml-2 text-stone-400"
                      aria-hidden
                    >
                      🔒
                    </span>
                  ) : null}
                </td>
                <DiffCell field={field} value={field.before} kind="before" />
                <DiffCell field={field} value={field.after} kind="after" />
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {visible.length === 0 ? (
        <p className="px-6 py-8 text-center text-sm text-stone-500">
          Bu öneri hiçbir alanı değiştirmiyor.
        </p>
      ) : null}
    </Card>
  )
}

/**
 * Tek hücre. Üç durum ayrı görünür: maskeli (yetki yok), boş (kayıt yok) ve
 * değer. Maskeliyi boş göstermek "alan siliniyor" izlenimi verirdi.
 */
function DiffCell({
  field,
  value,
  kind,
}: {
  field: DiffField
  value: string | null
  kind: 'before' | 'after'
}) {
  const emphasis =
    field.changed && kind === 'after'
      ? 'font-medium text-emerald-700 dark:text-emerald-300'
      : field.changed && kind === 'before'
        ? 'text-stone-500 line-through decoration-stone-400'
        : 'text-stone-600 dark:text-stone-300'

  if (field.masked) {
    return (
      <td className="px-6 py-3 align-top text-sm text-stone-400 italic">
        yetkiniz yok
      </td>
    )
  }

  return (
    <td className={`px-6 py-3 align-top break-words ${emphasis}`}>
      {value === null || value === '' ? <span className="text-stone-400">—</span> : value}
    </td>
  )
}

/**
 * Karar paneli. Onay notu isteğe bağlı, ret gerekçesi zorunlu (sunucu en az 10
 * karakter istiyor): gerekçesiz ret, aynı önerinin bir hafta sonra aynen
 * gönderilmesiyle sonuçlanıyor.
 */
function ReviewPanel({ id }: { id: string }) {
  const navigate = useNavigate()
  const [note, setNote] = useState('')
  const approve = useReviewChangeRequest(id, 'approve')
  const reject = useReviewChangeRequest(id, 'reject')
  const busy = approve.isPending || reject.isPending
  const failure = approve.error ?? reject.error

  const done = () => navigate('/onaylar')

  return (
    <Card className="px-6 py-5">
      <h2 className="font-medium text-stone-900 dark:text-stone-100">Karar</h2>
      <p className="mt-1 text-sm text-stone-500">
        Onayladığınızda değişiklik girişim kartına anında işlenir ve denetim izine
        yazılır.
      </p>

      <label className="mt-4 flex flex-col gap-1.5">
        <span className="text-sm font-medium text-stone-700 dark:text-stone-300">
          Not / gerekçe
        </span>
        <textarea
          value={note}
          onChange={(e) => setNote(e.target.value)}
          rows={3}
          maxLength={2000}
          placeholder="Onayda isteğe bağlı; ret için en az 10 karakter gerekçe zorunlu."
          className="rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 outline-none placeholder:text-stone-400 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100"
        />
      </label>

      {failure ? <div className="mt-3"><ErrorState message={failure.message} /></div> : null}

      <div className="mt-4 flex flex-wrap gap-3">
        <Button
          disabled={busy}
          onClick={() => approve.mutate(note.trim() || null, { onSuccess: done })}
        >
          Onayla ve uygula
        </Button>
        <Button
          variant="outline"
          disabled={busy || note.trim().length < 10}
          onClick={() => reject.mutate(note.trim(), { onSuccess: done })}
        >
          Reddet
        </Button>
        {note.trim().length < 10 ? (
          <p className="self-center text-xs text-stone-500">
            Reddetmek için gerekçe yazın.
          </p>
        ) : null}
      </div>
    </Card>
  )
}
