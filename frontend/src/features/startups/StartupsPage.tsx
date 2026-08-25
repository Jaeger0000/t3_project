import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { formatCompactMoney, formatYear } from '@/lib/format'
import { sectorLabels, startupStatusLabels, startupStatusTone } from '@/lib/labels'
import { Badge, Button, Card, EmptyState, ErrorState, Input, Select, Spinner } from '@/components/ui'
import { defaultFilters, useStartups, usePrograms } from './queries'
import StartupForm from './StartupForm'
import ParticipationForm from './ParticipationForm'
import type { StartupFilters } from './queries'
import type { StartupListItem, StartupSort } from '@/api/types'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

export default function StartupsPage() {
  useDocumentTitle('Girişimler')
  const { session } = useAuth()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const [creating, setCreating] = useState(false)
  // Yeni kayıt Program Yöneticisi'nin kapsamına ancak bir program dönemine
  // bağlanınca girer (kapsam tanımı: "programlarımdan geçmiş girişimler").
  // Bu yüzden kaydetmeden sonra kart yerine katılım adımı gösteriliyor.
  const [created, setCreated] = useState<{ id: string; name: string } | null>(null)

  /*
   * Filtre, sıralama ve sayfa numarası URL'de duruyor — bileşende ikinci bir
   * kopya yok. Kazanç üç yerde görülüyor: geri tuşu bir önceki filtreye
   * dönüyor, "şu listeye bak" diye paylaşılan bağlantı karşı tarafta aynı
   * listeyi açıyor ve sayfa yenilenince seçim kaybolmuyor. Programlar sayfası
   * zaten ?program=<id> ile buraya bağlanıyordu; artık tüm filtreler aynı dili
   * konuşuyor.
   */
  const filters = useMemo<StartupFilters>(
    () => ({
      ...defaultFilters,
      q: searchParams.get('q') ?? '',
      sector: (searchParams.get('sektor') ?? '') as StartupFilters['sector'],
      status: (searchParams.get('durum') ?? '') as StartupFilters['status'],
      programId: searchParams.get('program') ?? '',
      sort: (searchParams.get('sirala') ?? defaultFilters.sort) as StartupSort,
      // Elle kurcalanmış ?sayfa=abc değeri listeyi bozmasın.
      page: Math.max(1, Number(searchParams.get('sayfa')) || 1),
    }),
    [searchParams],
  )

  const setParam = useCallback(
    (key: string, value: string) => {
      setSearchParams(
        (previous) => {
          const next = new URLSearchParams(previous)

          if (value) next.set(key, value)
          else next.delete(key)

          // Filtre değişince ilk sayfaya dönülür: 4. sayfada duran kullanıcı
          // filtreyi daralttığında boş liste görürdü.
          if (key !== 'sayfa') next.delete('sayfa')

          return next
        },
        // Arama her tuş vuruşunda geçmişe satır eklemesin; geri tuşu filtreden
        // filtreye atlamalı, harften harfe değil.
        { replace: key === 'q' },
      )
    },
    [setSearchParams],
  )

  const [searchText, setSearchText] = useState(filters.q)
  const [syncedTerm, setSyncedTerm] = useState(filters.q)

  // Geri/ileri tuşu ya da paylaşılan bağlantı URL'yi değiştirdiğinde kutu da
  // onunla gelsin. Bu iş effect'te değil **render sırasında** yapılıyor: React'in
  // "dışarıdan gelen değer değişince state'i düzelt" kalıbı. Effect'le yazmak
  // fazladan bir tur render üretiyor ve kutu bir kare eski değeri gösteriyor.
  if (syncedTerm !== filters.q) {
    setSyncedTerm(filters.q)
    setSearchText(filters.q)
  }

  // Her tuş vuruşunda istek atmamak için arama terimi geciktirilir.
  useEffect(() => {
    if (searchText === filters.q) return

    const timer = setTimeout(() => setParam('q', searchText), 350)
    return () => clearTimeout(timer)
  }, [searchText, filters.q, setParam])

  const startups = useStartups(filters)
  const programs = usePrograms()

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">Girişimler</h1>
          <p className="mt-1 text-sm text-stone-500">
            {session?.role === 'ProgramManager'
              ? 'Sorumlu olduğunuz programlardan geçmiş girişimler.'
              : session?.role === 'StartupUser'
                ? 'Girişiminizin kurumsal kaydı.'
                : 'Ekosistemdeki tüm girişimlerin tek doğrulanmış kaydı.'}
          </p>
        </div>
        <div className="flex items-center gap-4">
          {startups.data ? (
            <p className="text-sm text-stone-500">
              <span className="font-semibold text-stone-900 dark:text-stone-100">
                {startups.data.totalCount}
              </span>{' '}
              girişim
            </p>
          ) : null}

          {/* "Girişimi sisteme kim ekliyor?" sorusunun ekrandaki cevabı.
              Düğme yalnızca yetkiliye çıkıyor; uç zaten politikayla korumalı. */}
          {session?.permissions.canManageStartups && !creating ? (
            <Button onClick={() => setCreating(true)}>Yeni girişim</Button>
          ) : null}
        </div>
      </header>

      {creating ? (
        <StartupForm
          mode="direct"
          onCreated={(startup) => {
            setCreating(false)
            if (session?.role === 'SuperAdmin') {
              navigate(`/girisimler/${startup.id}`)
              return
            }
            setCreated(startup)
          }}
          onCancel={() => setCreating(false)}
        />
      ) : null}

      {created ? (
        <Card className="flex flex-col gap-4 p-5">
          <div>
            <h2 className="font-semibold text-stone-900 dark:text-stone-100">
              {created.name} kaydedildi — sıradaki adım
            </h2>
            <p className="mt-1 text-sm text-stone-500">
              Kaydı listenizde görebilmek için bir program dönemine bağlamanız
              gerekiyor: kapsamınız “sorumlu olduğunuz programlardan geçmiş
              girişimler” olarak tanımlı.
            </p>
          </div>
          <ParticipationForm
            startupId={created.id}
            onDone={() => navigate(`/girisimler/${created.id}`)}
            onCancel={() => setCreated(null)}
          />
        </Card>
      ) : null}

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
            onChange={(event) => setParam('sektor', event.target.value)}
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
            onChange={(event) => setParam('durum', event.target.value)}
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
            onChange={(event) => setParam('program', event.target.value)}
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
            onChange={(event) => setParam('sirala', event.target.value)}
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
                onClick={() => setParam('sayfa', String(filters.page - 1))}
              >
                ← Önceki
              </Button>
              <span className="text-sm text-stone-500">
                Sayfa {startups.data.page} / {startups.data.totalPages}
              </span>
              <Button
                variant="outline"
                disabled={!startups.data.hasNext}
                onClick={() => setParam('sayfa', String(filters.page + 1))}
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

/**
 * Liste döşemesi. `min-w-0` olmadan döşeme kendi min-content'i kadar yer
 * istiyor ve 375 px'te sayfayı yatay kaydırıyordu; tutar sütunu dar ekranda
 * bir punto küçülüyor.
 */
function StartupTile({ startup }: { startup: StartupListItem }) {
  const investment = formatCompactMoney(startup.totalInvestment, startup.currency)

  return (
    <Link to={`/girisimler/${startup.id}`} className="group min-w-0">
      <Card className="flex h-full flex-col gap-3 p-4 transition-shadow group-hover:shadow-md sm:p-5">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="truncate font-semibold text-stone-900 group-hover:text-brand-700 dark:text-stone-100">
              {startup.name}
            </h2>
            <p className="mt-0.5 text-xs text-stone-500">
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

        <dl className="mt-auto grid grid-cols-3 gap-2 border-t border-stone-100 pt-3 text-center dark:border-stone-800">
          <div className="min-w-0">
            <dt className="text-xs text-stone-500">Program</dt>
            <dd className="text-xs font-semibold tabular-nums sm:text-sm">{startup.programCount}</dd>
          </div>
          <div className="min-w-0">
            <dt className="text-xs text-stone-500">Kayıt</dt>
            <dd className="text-xs font-semibold tabular-nums sm:text-sm">{startup.achievementCount}</dd>
          </div>
          <div className="min-w-0">
            <dt className="text-xs text-stone-500">Yatırım</dt>
            <dd className="text-xs font-semibold tabular-nums sm:text-sm">
              {!startup.amountsVisible ? (
                <span className="text-stone-400" title="Tutarı görme yetkiniz yok">
                  🔒
                </span>
              ) : (
                (investment ?? <span className="text-stone-400">—</span>)
              )}
            </dd>
          </div>
        </dl>

        {startup.latestProgramName ? (
          <p className="truncate text-xs text-stone-500">
            Son program: <span className="text-stone-700 dark:text-stone-300">{startup.latestProgramName}</span>
          </p>
        ) : null}
      </Card>
    </Link>
  )
}
