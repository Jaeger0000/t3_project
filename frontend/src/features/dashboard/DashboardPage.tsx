import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { formatCompactMoney, formatMoney } from '@/lib/format'
import { sectorLabels } from '@/lib/labels'
import { Button, Card, ErrorState, Input, Select, Spinner } from '@/components/ui'
import { usePrograms } from '@/features/startups/queries'
import { BarChart, ChartCard, DonutChart } from './charts'
import { defaultDashboardFilters, exportStartupsCsv, useEcosystemStats } from './queries'
import type { DashboardFilters } from './queries'
import type { EcosystemStats, Sector } from '@/api/types'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

/**
 * Ekosistem panosu (karar destek). Panodaki her sayı, girişim listesiyle aynı
 * kapsam filtresinden geçer: Program Yöneticisi burada da yalnızca kendi
 * programlarının karnesini görür, ayrı bir "rapor yetkisi" yok.
 */
export default function DashboardPage() {
  useDocumentTitle('Ekosistem panosu')
  const { session } = useAuth()
  const [filters, setFilters] = useState<DashboardFilters>(defaultDashboardFilters)
  const [exportError, setExportError] = useState<unknown>(null)
  const [isExporting, setIsExporting] = useState(false)

  const stats = useEcosystemStats(filters)
  const programs = usePrograms()

  const update = <K extends keyof DashboardFilters>(key: K, value: DashboardFilters[K]) =>
    setFilters((current) => ({ ...current, [key]: value }))

  const download = async () => {
    setExportError(null)
    setIsExporting(true)
    try {
      await exportStartupsCsv(filters)
    } catch (error) {
      setExportError(error)
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">Ekosistem panosu</h1>
          <p className="mt-1 text-sm text-stone-500">
            {session?.role === 'ProgramManager'
              ? 'Sorumlu olduğunuz programların karnesi.'
              : session?.role === 'StartupUser'
                ? 'Girişiminizin ekosistem karnesi.'
                : 'T3 girişim ekosisteminin güncel karnesi.'}
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button variant="outline" onClick={download} disabled={isExporting}>
            {isExporting ? 'Hazırlanıyor…' : '⬇ CSV dışa aktar'}
          </Button>
        </div>
      </header>

      {exportError ? (
        <ErrorState
          message={exportError instanceof Error ? exportError.message : 'Dosya indirilemedi.'}
          error={exportError}
        />
      ) : null}

      <Card className="p-4">
        <div className="grid gap-3 sm:grid-cols-4">
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

          <Input
            label="Şehir"
            placeholder="Örn. Ankara"
            value={filters.city}
            onChange={(event) => update('city', event.target.value)}
          />

          {/* Seçenekler sunucudan geliyor: kapsamdaki finansal kayıtların
              gerçekten bulunduğu yıllar, uydurma bir aralık değil. Yıl
              seçilince yalnızca finansal dağılımlar (yatırım, hibe, ciro,
              ihracat, başarı sayısı) daralır — girişim listesi ve sektör/şehir
              dağılımı kapsamdaki TÜM girişimleri göstermeye devam eder. */}
          <Select
            label="Yıl"
            value={filters.year}
            onChange={(event) => update('year', event.target.value)}
          >
            <option value="">Tümü</option>
            {(stats.data?.availableYears ?? []).map((year) => (
              <option key={year} value={year}>
                {year}
              </option>
            ))}
          </Select>
        </div>
      </Card>

      {stats.isPending ? <Spinner label="Karne hesaplanıyor…" /> : null}
      {stats.error ? <ErrorState message={stats.error.message} error={stats.error} /> : null}

      {stats.data ? <StatsBody stats={stats.data} /> : null}
    </div>
  )
}

function StatsBody({ stats }: { stats: EcosystemStats }) {
  const { totals } = stats

  return (
    <>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Kpi label="Girişim" value={String(totals.startups)} hint={`${totals.activeStartups} faal`} />
        <Kpi
          label="Program katılımı"
          value={String(totals.participations)}
          hint={`${totals.programs} program`}
        />
        <Kpi
          label="Toplam yatırım"
          value={money(totals.totalInvestment, stats)}
          hint={`${totals.investedStartups} girişim yatırım aldı`}
        />
        <Kpi
          label="Toplam hibe"
          value={money(totals.totalGrant, stats)}
          hint={`${totals.achievements} başarı kaydı`}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <ChartCard title="Sektör dağılımı" hint="Kapsamınızdaki girişimlerin sektörleri">
          <DonutChart
            data={stats.bySector.map((slice) => ({
              key: slice.key,
              label: slice.label,
              value: slice.count,
            }))}
            centerLabel="girişim"
            centerValue={String(totals.startups)}
          />
        </ChartCard>

        <ChartCard title="Program başına girişim" hint="Bir girişim birden çok programda sayılabilir">
          <BarChart
            data={stats.byProgram.map((slice) => ({
              key: slice.key,
              label: slice.label,
              value: slice.count,
            }))}
          />
        </ChartCard>

        <ChartCard title="Yatırım turu dağılımı" hint="Tur sayısına göre">
          <BarChart
            data={stats.investmentByRound.map((slice) => ({
              key: slice.key,
              label: slice.label,
              value: slice.count,
            }))}
          />
        </ChartCard>

        <ChartCard
          title="Yıllara göre yatırım"
          hint={
            stats.amountsVisible
              ? `Toplamlar ${stats.currency} cinsinden`
              : 'Tutarları görme yetkiniz yok; yalnızca tur sayıları gösteriliyor'
          }
        >
          <BarChart
            data={stats.investmentByYear.map((slice) => ({
              key: slice.key,
              label: slice.label,
              value: stats.amountsVisible ? (slice.total ?? 0) : slice.count,
            }))}
            formatValue={(value) =>
              stats.amountsVisible
                ? (formatCompactMoney(value, stats.currency) ?? '—')
                : `${value} tur`
            }
          />
        </ChartCard>

        <ChartCard title="Şehir dağılımı" hint="En çok girişim barındıran ilk 8 şehir">
          <BarChart
            data={stats.byCity.map((slice) => ({
              key: slice.key,
              label: slice.label,
              value: slice.count,
            }))}
          />
        </ChartCard>

        <ChartCard title="En çok yatırım alan girişimler" hint="Sıralama herkese açık, tutar role bağlı">
          {stats.topByInvestment.length === 0 ? (
            <p className="py-6 text-center text-sm text-stone-500">
              Kapsamınızda yatırım kaydı olan girişim yok.
            </p>
          ) : (
            <ol className="flex flex-col divide-y divide-stone-100 dark:divide-stone-800">
              {stats.topByInvestment.map((row, index) => (
                <li key={row.startupId} className="flex items-center gap-3 py-2.5">
                  <span className="w-5 text-sm font-semibold text-stone-400 tabular-nums">
                    {index + 1}
                  </span>
                  <Link
                    to={`/girisimler/${row.startupId}`}
                    className="min-w-0 flex-1 truncate text-sm font-medium text-stone-900 hover:text-brand-700 dark:text-stone-100"
                  >
                    {row.name}
                    <span className="ml-2 text-xs font-normal text-stone-500">{row.sectorLabel}</span>
                  </Link>
                  <span className="text-sm font-semibold tabular-nums text-stone-900 dark:text-stone-100">
                    {row.investment === null ? (
                      <span className="text-stone-400" title="Tekil tutarı görme yetkiniz yok">
                        🔒
                      </span>
                    ) : (
                      formatCompactMoney(row.investment, stats.currency)
                    )}
                  </span>
                </li>
              ))}
            </ol>
          )}
        </ChartCard>
      </div>
    </>
  )
}

/**
 * Tutar KPI'ı. `null` iki farklı şey demek: kayıt yok ya da yetki yok. Ayrımı
 * `amountsVisible` bayrağı taşıyor — Faz 2'de bu ayrım olmadığı için ekranda
 * "0 ₺" görünmüştü.
 */
function money(amount: number | null, stats: EcosystemStats): string {
  if (!stats.amountsVisible) return '🔒'
  return formatMoney(amount, stats.currency) ?? 'kayıt yok'
}

function Kpi({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <Card className="p-5">
      <p className="text-xs font-medium tracking-wide text-stone-500 uppercase">{label}</p>
      <p className="mt-1 text-2xl font-bold tabular-nums text-stone-900 dark:text-stone-50">
        {value}
      </p>
      {hint ? <p className="mt-1 text-xs text-stone-500">{hint}</p> : null}
    </Card>
  )
}
