import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { formatCompactMoney, formatYear } from '@/lib/format'
import { sectorLabels, startupStatusLabels, startupStatusTone } from '@/lib/labels'
import { Badge, Button, Card, EmptyState, ErrorState, Input, Select, Spinner } from '@/components/ui'
import { defaultFilters, useStartups, usePrograms } from './queries'
import type { StartupFilters } from './queries'
import type { Sector, StartupListItem, StartupSort, StartupStatus } from '@/api/types'

export default function StartupsPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()

  // Programlar sayfası buraya ?program=<id> ile bağlanıyor; filtre o değerle
  // başlatılır, sonrasını kullanıcı açılırdan yönetir.
  const [filters, setFilters] = useState<StartupFilters>(() => ({
    ...defaultFilters,
    programId: searchParams.get('program') ?? '',
  }))
  const [searchText, setSearchText] = useState('')

  // Her tuş vuruşunda istek atmamak için arama terimi geciktirilir.
  useEffect(() => {
    const timer = setTimeout(
      () => setFilters((current) => ({ ...current, q: searchText, page: 1 })),
      350,
    )
    return () => clearTimeout(timer)
  }, [searchText])

  const startups = useStartups(filters)
  const programs = usePrograms()

  const update = <K extends keyof StartupFilters>(key: K, value: StartupFilters[K]) =>
    setFilters((current) => ({ ...current, [key]: value, page: 1 }))

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-brand-900 dark:text-brand-100">Girişimler</h1>
          <p className="mt-1 text-sm text-slate-500">
            {session?.role === 'ProgramManager'
              ? 'Sorumlu olduğunuz programlardan geçmiş girişimler.'
              : session?.role === 'StartupUser'
                ? 'Girişiminizin kurumsal kaydı.'
                : 'Ekosistemdeki tüm girişimlerin tek doğrulanmış kaydı.'}
          </p>
        </div>
        {startups.data ? (
          <p className="text-sm text-slate-500">
            <span className="font-semibold text-slate-900 dark:text-slate-100">
              {startups.data.totalCount}
            </span>{' '}
            girişim
          </p>
        ) : null}
      </header>

      <Card className="p-4">
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
          <Input
            label="Ara"
            placeholder="Ad, ürün ya da şehir"
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
          />

          <Select
            label="Sektör"
            value={filters.sector}
            onChange={(event) => update('sector', event.target.value as Sector | '')}
          >
            <option value="">Tümü</option>
            {Object.entries(sectorLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </Select>

          <Select
            label="Durum"
            value={filters.status}
            onChange={(event) => update('status', event.target.value as StartupStatus | '')}
          >
            <option value="">Tümü</option>
            {Object.entries(startupStatusLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </Select>

          <Select
            label="Program"
            value={filters.programId}
            onChange={(event) => update('programId', event.target.value)}
          >
            <option value="">Tümü</option>
            {(programs.data ?? []).map((program) => (
              <option key={program.id} value={program.id}>
                {program.name}
              </option>
            ))}
          </Select>

          <Select
            label="Sırala"
            value={filters.sort}
            onChange={(event) => update('sort', event.target.value as StartupSort)}
          >
            <option value="Name">Ada göre</option>
            <option value="Newest">En yeni kayıt</option>
            <option value="MostInvestment">En çok yatırım</option>
          </Select>
        </div>
      </Card>

      {startups.isPending ? <Spinner label="Girişimler yükleniyor…" /> : null}
      {startups.error ? <ErrorState message={startups.error.message} /> : null}

      {startups.data && startups.data.items.length === 0 ? (
        <EmptyState
          title="Filtrelere uyan girişim yok"
          hint="Arama terimini kısaltmayı ya da filtreleri temizlemeyi deneyin."
        />
      ) : null}

      {startups.data && startups.data.items.length > 0 ? (
        <>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {startups.data.items.map((startup) => (
              <StartupTile key={startup.id} startup={startup} />
            ))}
          </div>

          {startups.data.totalPages > 1 ? (
            <div className="flex items-center justify-center gap-4">
              <Button
                variant="outline"
                disabled={filters.page <= 1}
                onClick={() => setFilters((c) => ({ ...c, page: c.page - 1 }))}
              >
                ← Önceki
              </Button>
              <span className="text-sm text-slate-500">
                Sayfa {startups.data.page} / {startups.data.totalPages}
              </span>
              <Button
                variant="outline"
                disabled={!startups.data.hasNext}
                onClick={() => setFilters((c) => ({ ...c, page: c.page + 1 }))}
              >
                Sonraki →
              </Button>
            </div>
          ) : null}
        </>
      ) : null}
    </div>
  )
}

function StartupTile({ startup }: { startup: StartupListItem }) {
  const investment = formatCompactMoney(startup.totalInvestment, startup.currency)

  return (
    <Link to={`/girisimler/${startup.id}`} className="group">
      <Card className="flex h-full flex-col gap-3 p-5 transition-shadow group-hover:shadow-md">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="truncate font-semibold text-slate-900 group-hover:text-brand-600 dark:text-slate-100">
              {startup.name}
            </h2>
            <p className="mt-0.5 text-xs text-slate-500">
              {sectorLabels[startup.sector]}
              {startup.city ? ` · ${startup.city}` : ''}
              {startup.foundedOn ? ` · ${formatYear(startup.foundedOn)}` : ''}
            </p>
          </div>
          <Badge tone={startupStatusTone[startup.status]}>
            {startupStatusLabels[startup.status]}
          </Badge>
        </div>

        {startup.technologyAreas.length > 0 ? (
          <div className="flex flex-wrap gap-1.5">
            {startup.technologyAreas.slice(0, 3).map((area) => (
              <Badge key={area}>{area}</Badge>
            ))}
          </div>
        ) : null}

        <dl className="mt-auto grid grid-cols-3 gap-2 border-t border-slate-100 pt-3 text-center dark:border-slate-800">
          <div>
            <dt className="text-xs text-slate-500">Program</dt>
            <dd className="text-sm font-semibold tabular-nums">{startup.programCount}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">Kayıt</dt>
            <dd className="text-sm font-semibold tabular-nums">{startup.achievementCount}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">Yatırım</dt>
            <dd className="text-sm font-semibold tabular-nums">
              {!startup.amountsVisible ? (
                <span className="text-slate-400" title="Tutarı görme yetkiniz yok">
                  🔒
                </span>
              ) : (
                (investment ?? <span className="text-slate-400">—</span>)
              )}
            </dd>
          </div>
        </dl>

        {startup.latestProgramName ? (
          <p className="truncate text-xs text-slate-500">
            Son program: <span className="text-slate-700 dark:text-slate-300">{startup.latestProgramName}</span>
          </p>
        ) : null}
      </Card>
    </Link>
  )
}
