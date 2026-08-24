import { useRef, useState } from 'react'
import type { DocumentType, StartupDocument } from '@/api/types'
import { ApiError, api } from '@/lib/apiClient'
import { formatDate } from '@/lib/format'
import { Badge, Button, Card, EmptyState, ErrorState, Select, Spinner } from '@/components/ui'
import { useSubmitChangeRequest } from '@/features/approvals/queries'
import { documentTypeLabels } from './labels'
import { useDeleteDocument, useDocuments, useUploadDocument } from './queries'

export type DocumentMode = 'readonly' | 'direct' | 'proposal'

const types = Object.keys(documentTypeLabels) as DocumentType[]

/**
 * Doküman bölümü (MVP #4).
 *
 * Yükleme tek uca gidiyor; yetkili doğrudan kaydediyor, girişim kullanıcısının
 * dosyası onay kuyruğuna düşüyor. Ekran bu farkı yanıttaki `applied` alanından
 * öğrenip kullanıcıya açıkça söylüyor — "yükledim" ile "yayına girdi" aynı şey
 * değil.
 */
export default function DocumentSection({
  startupId,
  mode,
}: {
  startupId: string
  mode: DocumentMode
}) {
  const list = useDocuments(startupId)
  const [notice, setNotice] = useState<string | null>(null)

  if (list.isPending) return <Spinner label="Dokümanlar yükleniyor…" />

  // 403 bir hata değil bir cevap: rolün doküman görme yetkisi yok.
  if (list.error instanceof ApiError && list.error.status === 403) {
    return (
      <p className="rounded-lg bg-stone-100 px-4 py-2.5 text-sm text-stone-600 dark:bg-stone-800 dark:text-stone-300">
        🔒 Doküman görüntüleme yetkiniz yok. Dosya adları tek başına ticari bilgi
        taşıyabildiği için bu listede satır sayısı da gösterilmez.
      </p>
    )
  }

  if (list.error) return <ErrorState message={list.error.message} />
  if (!list.data) return null

  const items = list.data.items

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h2 className="font-semibold text-stone-900 dark:text-stone-100">Dokümanlar</h2>
        <p className="text-sm text-stone-500">
          {items.length} dosya
          {mode === 'proposal' ? ' · yüklediğiniz dosya onaydan sonra listeye girer' : ''}
        </p>
      </div>

      {notice ? (
        <p className="rounded-lg bg-emerald-50 px-4 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
          {notice}
        </p>
      ) : null}

      {mode !== 'readonly' ? (
        <UploadForm startupId={startupId} onDone={setNotice} />
      ) : null}

      {items.length === 0 ? (
        <EmptyState
          title="Doküman yüklenmemiş"
          hint="Sunum, finansal tablo, kuruluş belgesi ve raporlar burada tutulur."
        />
      ) : null}

      <div className="flex flex-col gap-3">
        {items.map((item) => (
          <DocumentRow
            key={item.id}
            startupId={startupId}
            item={item}
            mode={mode}
            onRemoved={setNotice}
          />
        ))}
      </div>
    </div>
  )
}

function UploadForm({
  startupId,
  onDone,
}: {
  startupId: string
  onDone: (message: string) => void
}) {
  const upload = useUploadDocument(startupId)
  const [type, setType] = useState<DocumentType>('PitchDeck')
  const [file, setFile] = useState<File | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  function submit() {
    if (!file) return

    upload.mutate(
      { file, type },
      {
        onSuccess: (result) => {
          setFile(null)
          if (inputRef.current) inputRef.current.value = ''
          onDone(result.message)
        },
      },
    )
  }

  return (
    <Card className="flex flex-wrap items-end gap-4 p-4">
      <Select
        label="Doküman türü"
        value={type}
        onChange={(e) => setType(e.target.value as DocumentType)}
      >
        {types.map((item) => (
          <option key={item} value={item}>
            {documentTypeLabels[item]}
          </option>
        ))}
      </Select>

      <label className="flex flex-1 flex-col gap-1.5">
        <span className="text-sm font-medium text-stone-700 dark:text-stone-300">Dosya</span>
        <input
          ref={inputRef}
          type="file"
          accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.csv,.txt,.png,.jpg,.jpeg"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          className="rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 file:mr-3 file:rounded-md file:border-0 file:bg-stone-100 file:px-3 file:py-1 file:text-sm dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100 dark:file:bg-stone-800 dark:file:text-stone-200"
        />
      </label>

      <Button onClick={submit} disabled={!file || upload.isPending}>
        {upload.isPending ? 'Yükleniyor…' : 'Yükle'}
      </Button>

      {upload.error ? (
        <p className="w-full text-sm text-red-600">{upload.error.message}</p>
      ) : (
        <p className="w-full text-xs text-stone-500">
          En fazla 20 MB. Kabul edilen türler: PDF, Word, Excel, PowerPoint, CSV,
          metin ve görsel dosyaları.
        </p>
      )}
    </Card>
  )
}

function DocumentRow({
  startupId,
  item,
  mode,
  onRemoved,
}: {
  startupId: string
  item: StartupDocument
  mode: DocumentMode
  onRemoved: (message: string) => void
}) {
  const remove = useDeleteDocument(startupId)
  const propose = useSubmitChangeRequest()
  const [confirming, setConfirming] = useState(false)
  const [downloading, setDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  const busy = remove.isPending || propose.isPending || downloading
  const error = remove.error ?? propose.error

  async function handleDownload() {
    setDownloading(true)
    setDownloadError(null)
    try {
      await api.download(`/api/documents/${item.id}/download`, item.fileName)
    } catch (e) {
      setDownloadError(e instanceof Error ? e.message : 'Dosya indirilemedi.')
    } finally {
      setDownloading(false)
    }
  }

  function handleRemove() {
    if (mode === 'proposal') {
      propose.mutate(
        { targetType: 'Document', operation: 'Delete', targetId: item.id },
        {
          onSuccess: () => {
            setConfirming(false)
            onRemoved('Dokümanın kaldırılması önerildi; yetkili onayına gönderildi.')
          },
        },
      )
      return
    }

    remove.mutate(item.id, {
      onSuccess: () => {
        setConfirming(false)
        onRemoved('Doküman listeden kaldırıldı.')
      },
    })
  }

  return (
    <Card className="p-4">
      <div className="flex flex-wrap items-center gap-x-6 gap-y-2">
        <div className="min-w-56 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <Badge>{item.typeLabel}</Badge>
            <p className="font-medium break-all text-stone-900 dark:text-stone-100">
              {item.fileName}
            </p>
          </div>
          <p className="mt-1 text-sm text-stone-500">
            {item.sizeLabel} · {formatDate(item.uploadedAt)}
            {item.uploadedByName ? ` · ${item.uploadedByName}` : ''}
          </p>
        </div>

        <div className="flex items-center gap-1">
          <Button variant="outline" onClick={handleDownload} disabled={busy}>
            {downloading ? 'İndiriliyor…' : 'İndir'}
          </Button>

          {mode !== 'readonly' ? (
            confirming ? (
              <>
                <Button variant="outline" onClick={handleRemove} disabled={busy}>
                  {mode === 'proposal' ? 'Kaldırmayı öner' : 'Kaldır'}
                </Button>
                <Button variant="ghost" onClick={() => setConfirming(false)} disabled={busy}>
                  Vazgeç
                </Button>
              </>
            ) : (
              <Button variant="ghost" onClick={() => setConfirming(true)} disabled={busy}>
                Kaldır
              </Button>
            )
          ) : null}
        </div>
      </div>

      {downloadError ? <p className="mt-2 text-sm text-red-600">{downloadError}</p> : null}
      {error ? <p className="mt-2 text-sm text-red-600">{error.message}</p> : null}
    </Card>
  )
}
