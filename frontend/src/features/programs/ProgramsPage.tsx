import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { Program, ProgramTerm } from '@/api/types'
import { useAuth } from '@/lib/auth'
import { formatDate } from '@/lib/format'
import { programTypeLabels } from '@/lib/labels'
import { Badge, Button, Card, EmptyState, ErrorState, Spinner } from '@/components/ui'
import { usePrograms } from '@/features/startups/queries'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import ProgramForm from './ProgramForm'
import TermForm from './TermForm'
import { useDeleteProgram, useDeleteTerm } from './queries'

/**
 * Program ve dönem yönetimi.
 *
 * Ekran Dalga 1'e kadar tamamen salt okunurdu: programlar yalnızca
 * tohumlayıcıyla, yani doğrudan veritabanına yazılarak var olabiliyordu.
 *
 * Yetki ikiye ayrılıyor ve ekran bu ayrımı görünür kılıyor: program
 * <em>tanımı</em> (oluştur/düzenle/kapat) yalnızca Süper Yönetici'de, çünkü
 * program listesi aynı zamanda Program Yöneticisi'nin yetki kapsamının tanımı.
 * Dönem ekleme/düzenleme günlük operasyon ve Program Yöneticisi'ne de açık ama
 * yalnızca kendi programında.
 */
export default function ProgramsPage() {
  useDocumentTitle('Programlar')
  const { session } = useAuth()
  const programs = usePrograms()

  const [creating, setCreating] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)

  const canManagePrograms = session?.permissions.canManagePrograms ?? false
  const canManageTerms = session?.permissions.canManageProgramTerms ?? false

  if (programs.isPending) return <Spinner label="Programlar yükleniyor…" />
  if (programs.error) return <ErrorState message={programs.error.message} error={programs.error} />
  if (!programs.data) return null

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">Programlar</h1>
          <p className="mt-1 text-sm text-stone-500">
            Kapsamınızdaki T3 girişimcilik programları ve dönemleri.
          </p>
        </div>
        {canManagePrograms ? (
          <Button
            onClick={() => {
              setNotice(null)
              setCreating(true)
            }}
          >
            Yeni program
          </Button>
        ) : null}
      </header>

      {notice ? (
        <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
          {notice}
        </p>
      ) : null}

      {creating ? (
        <ProgramForm
          onDone={(message) => {
            setNotice(message)
            setCreating(false)
          }}
          onCancel={() => setCreating(false)}
        />
      ) : null}

      {programs.data.length === 0 ? (
        <EmptyState
          title="Görüntülenecek program yok"
          hint={
            canManagePrograms
              ? '"Yeni program" ile ilk programı tanımlayabilirsiniz.'
              : 'Program Yöneticisi rolündeyseniz henüz bir programa atanmamış olabilirsiniz.'
          }
        />
      ) : null}

      <div className="flex flex-col gap-4">
        {programs.data.map((program) => (
          <ProgramCard
            key={program.id}
            program={program}
            canManagePrograms={canManagePrograms}
            canManageTerms={canManageTerms}
            onNotice={setNotice}
          />
        ))}
      </div>
    </div>
  )
}

function ProgramCard({
  program,
  canManagePrograms,
  canManageTerms,
  onNotice,
}: {
  program: Program
  canManagePrograms: boolean
  canManageTerms: boolean
  onNotice: (message: string) => void
}) {
  const [editing, setEditing] = useState(false)
  const [addingTerm, setAddingTerm] = useState(false)

  return (
    <Card className="min-w-0 p-4 sm:p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <h2 className="font-semibold text-stone-900 dark:text-stone-100">{program.name}</h2>
          <p className="mt-0.5 text-sm text-stone-500">
            {programTypeLabels[program.type]}
            {program.coordinatorship ? ` · ${program.coordinatorship}` : ''}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Link
            to={`/girisimler?program=${program.id}`}
            className="text-sm text-brand-700 hover:underline dark:text-brand-100"
          >
            {program.startupCount} girişim →
          </Link>
          {canManagePrograms ? (
            <>
              <Button variant="outline" onClick={() => setEditing((value) => !value)}>
                {editing ? 'Düzenlemeyi kapat' : 'Düzenle'}
              </Button>
              <CloseProgramButton program={program} onNotice={onNotice} />
            </>
          ) : null}
        </div>
      </div>

      {program.description ? (
        <p className="mt-2 text-sm text-stone-600 dark:text-stone-400">{program.description}</p>
      ) : null}

      {editing ? (
        <div className="mt-4">
          <ProgramForm
            program={program}
            onDone={(message) => {
              onNotice(message)
              setEditing(false)
            }}
            onCancel={() => setEditing(false)}
          />
        </div>
      ) : null}

      <div className="mt-4 flex flex-wrap items-center gap-2">
        <h3 className="text-sm font-medium text-stone-700 dark:text-stone-300">
          Dönemler ({program.terms.length})
        </h3>
        {canManageTerms ? (
          <Button variant="ghost" onClick={() => setAddingTerm((value) => !value)}>
            {addingTerm ? 'Vazgeç' : 'Dönem ekle'}
          </Button>
        ) : null}
      </div>

      {addingTerm ? (
        <TermForm
          programId={program.id}
          onDone={(message) => {
            onNotice(message)
            setAddingTerm(false)
          }}
          onCancel={() => setAddingTerm(false)}
        />
      ) : null}

      {program.terms.length === 0 ? (
        <p className="mt-2 text-sm text-stone-500">
          Bu programda tanımlı dönem yok; girişim eklenebilmesi için önce dönem
          oluşturulmalı.
        </p>
      ) : (
        <div className="mt-3 flex flex-wrap gap-2">
          {program.terms.map((term) => (
            <TermTile
              key={term.id}
              programId={program.id}
              term={term}
              canManage={canManageTerms}
              onNotice={onNotice}
            />
          ))}
        </div>
      )}
    </Card>
  )
}

function TermTile({
  programId,
  term,
  canManage,
  onNotice,
}: {
  programId: string
  term: ProgramTerm
  canManage: boolean
  onNotice: (message: string) => void
}) {
  const [editing, setEditing] = useState(false)

  return (
    <div className="min-w-0 rounded-lg border border-stone-200 px-3 py-2 dark:border-stone-800">
      <p className="text-sm font-medium text-stone-900 dark:text-stone-100">{term.name}</p>
      <p className="text-xs text-stone-500">
        {formatDate(term.startsOn)}
        {term.endsOn ? ` → ${formatDate(term.endsOn)}` : ' → sürüyor'}
      </p>
      <Badge>{term.participantCount} katılım</Badge>

      {canManage ? (
        <div className="mt-2 flex flex-wrap items-center gap-2">
          <button
            type="button"
            onClick={() => setEditing((value) => !value)}
            className="text-xs text-brand-700 hover:underline dark:text-brand-200"
          >
            {editing ? 'Vazgeç' : 'Düzenle'}
          </button>
          <CloseTermButton programId={programId} term={term} onNotice={onNotice} />
        </div>
      ) : null}

      {editing ? (
        <TermForm
          programId={programId}
          term={term}
          onDone={(message) => {
            onNotice(message)
            setEditing(false)
          }}
          onCancel={() => setEditing(false)}
        />
      ) : null}
    </div>
  )
}

/**
 * Program kapatma. İki adımlı: kapatma dönemleri, katılımları ve yönetici
 * atamalarını da pasife alıyor, yani yetki kapsamını daraltıyor — tek tıkla
 * olmamalı. Sonuç mesajı ne kadarının kapandığını söylüyor.
 */
function CloseProgramButton({
  program,
  onNotice,
}: {
  program: Program
  onNotice: (message: string) => void
}) {
  const [confirming, setConfirming] = useState(false)
  const remove = useDeleteProgram()

  if (!confirming) {
    return (
      <Button variant="ghost" onClick={() => setConfirming(true)}>
        Programı kapat
      </Button>
    )
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <span className="text-xs text-stone-500">
        {program.startupCount} girişimin kaydı etkilenecek. Emin misiniz?
      </span>
      <Button
        variant="outline"
        disabled={remove.isPending}
        onClick={() =>
          remove.mutate(program.id, {
            onSuccess: (result) =>
              onNotice(
                `${result.name} kapatıldı: ${result.terms} dönem, ` +
                  `${result.participations} katılım, ${result.managerAssignments} yönetici ataması pasife alındı.`,
              ),
          })
        }
      >
        {remove.isPending ? 'Kapatılıyor…' : 'Evet, kapat'}
      </Button>
      <Button variant="ghost" disabled={remove.isPending} onClick={() => setConfirming(false)}>
        Vazgeç
      </Button>
      {remove.error ? <ErrorState message={remove.error.message} error={remove.error} /> : null}
    </div>
  )
}

/**
 * Dönem kapatma. Sunucu, katılım varsa reddediyor (girişimlerin gelişim
 * yolculuğu bu kayıtlardan üretiliyor); hata mesajı olduğu gibi gösteriliyor.
 */
function CloseTermButton({
  programId,
  term,
  onNotice,
}: {
  programId: string
  term: ProgramTerm
  onNotice: (message: string) => void
}) {
  const [confirming, setConfirming] = useState(false)
  const remove = useDeleteTerm(programId)

  if (!confirming) {
    return (
      <button
        type="button"
        onClick={() => setConfirming(true)}
        className="text-xs text-stone-500 hover:text-brand-700 hover:underline dark:hover:text-brand-200"
      >
        Kapat
      </button>
    )
  }

  return (
    <div className="flex flex-col gap-1">
      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          disabled={remove.isPending}
          onClick={() =>
            remove.mutate(term.id, {
              onSuccess: (result) => onNotice(`${result.name} dönemi kapatıldı.`),
            })
          }
          className="text-xs font-medium text-brand-700 hover:underline dark:text-brand-200"
        >
          {remove.isPending ? 'Kapatılıyor…' : 'Evet, kapat'}
        </button>
        <button
          type="button"
          onClick={() => setConfirming(false)}
          className="text-xs text-stone-500 hover:underline"
        >
          Vazgeç
        </button>
      </div>
      {remove.error ? <ErrorState message={remove.error.message} error={remove.error} /> : null}
    </div>
  )
}
