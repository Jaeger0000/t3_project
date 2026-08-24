import { Link } from 'react-router-dom'
import { formatDate } from '@/lib/format'
import { programTypeLabels } from '@/lib/labels'
import { Badge, Card, EmptyState, ErrorState, Spinner } from '@/components/ui'
import { usePrograms } from '@/features/startups/queries'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

export default function ProgramsPage() {
  useDocumentTitle('Programlar')
  const programs = usePrograms()

  if (programs.isPending) return <Spinner label="Programlar yükleniyor…" />
  if (programs.error) return <ErrorState message={programs.error.message} />
  if (!programs.data) return null

  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">Programlar</h1>
        <p className="mt-1 text-sm text-stone-500">
          Kapsamınızdaki T3 girişimcilik programları ve dönemleri.
        </p>
      </header>

      {programs.data.length === 0 ? (
        <EmptyState
          title="Görüntülenecek program yok"
          hint="Program Yöneticisi rolündeyseniz henüz bir programa atanmamış olabilirsiniz."
        />
      ) : null}

      <div className="flex flex-col gap-4">
        {programs.data.map((program) => (
          <Card key={program.id} className="p-5">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h2 className="font-semibold text-stone-900 dark:text-stone-100">{program.name}</h2>
                <p className="mt-0.5 text-sm text-stone-500">
                  {programTypeLabels[program.type]}
                  {program.coordinatorship ? ` · ${program.coordinatorship}` : ''}
                </p>
              </div>
              <Link
                to={`/girisimler?program=${program.id}`}
                className="text-sm text-brand-700 hover:underline dark:text-brand-100"
              >
                {program.startupCount} girişim →
              </Link>
            </div>

            {program.description ? (
              <p className="mt-2 text-sm text-stone-600 dark:text-stone-400">{program.description}</p>
            ) : null}

            <div className="mt-4 flex flex-wrap gap-2">
              {program.terms.map((term) => (
                <div
                  key={term.id}
                  className="rounded-lg border border-stone-200 px-3 py-2 dark:border-stone-800"
                >
                  <p className="text-sm font-medium text-stone-900 dark:text-stone-100">{term.name}</p>
                  <p className="text-xs text-stone-500">
                    {formatDate(term.startsOn)}
                    {term.endsOn ? ` → ${formatDate(term.endsOn)}` : ' → sürüyor'}
                  </p>
                  <Badge>{term.participantCount} katılım</Badge>
                </div>
              ))}
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}
