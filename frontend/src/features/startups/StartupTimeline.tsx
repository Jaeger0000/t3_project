import { formatMoney, formatMonthYear } from '@/lib/format'
import { timelineStyles } from '@/lib/labels'
import { Badge, EmptyState, ErrorState, Spinner } from '@/components/ui'
import { useStartupTimeline } from './queries'
import type { TimelineEntry } from '@/api/types'

/**
 * Gelişim yolculuğu (MVP #2). Girdiler yıla göre gruplanır; kaynak üç ayrı
 * tablo olsa da kullanıcı tek bir kronoloji görür.
 */
export default function StartupTimeline({ startupId }: { startupId: string }) {
  const timeline = useStartupTimeline(startupId)

  if (timeline.isPending) return <Spinner label="Yolculuk yükleniyor…" />
  if (timeline.error) return <ErrorState message={timeline.error.message} />
  if (!timeline.data) return null

  if (timeline.data.entries.length === 0) {
    return (
      <EmptyState
        title="Henüz kayıt yok"
        hint="Program katılımı, yatırım turu, hibe ya da kilometre taşı eklendikçe bu çizelge kendiliğinden oluşur."
      />
    )
  }

  const groups = groupByYear(timeline.data.entries)

  return (
    <div className="flex flex-col gap-8">
      {!timeline.data.exactAmountsVisible ? (
        <p className="rounded-lg bg-slate-100 px-4 py-2.5 text-sm text-slate-600 dark:bg-slate-800 dark:text-slate-300">
          🔒 Finansal tutarlar rolünüze göre maskelenmiştir; kayıtların varlığı ve türü görünür.
        </p>
      ) : null}

      {groups.map(([year, entries]) => (
        <section key={year}>
          <h3 className="mb-3 text-sm font-bold tracking-widest text-brand-600 dark:text-brand-100">
            {year}
          </h3>
          <ol className="relative flex flex-col gap-4 border-l border-slate-200 pl-6 dark:border-slate-800">
            {entries.map((entry, index) => (
              <TimelineRow key={`${entry.kind}-${entry.sourceId ?? index}-${entry.occurredOn}`} entry={entry} />
            ))}
          </ol>
        </section>
      ))}
    </div>
  )
}

function TimelineRow({ entry }: { entry: TimelineEntry }) {
  const style = timelineStyles[entry.kind]
  const amount = formatMoney(entry.amount, entry.currency ?? 'TRY')

  return (
    <li className="relative">
      <span
        className={`absolute -left-[2.1rem] flex size-6 items-center justify-center rounded-full text-xs ${style.tone}`}
        aria-hidden
      >
        {style.icon}
      </span>

      <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
        <p className="font-medium text-slate-900 dark:text-slate-100">{entry.title}</p>
        {entry.badge ? <Badge>{entry.badge}</Badge> : null}
        {amount ? (
          <span className="text-sm font-semibold tabular-nums text-emerald-700 dark:text-emerald-400">
            {amount}
          </span>
        ) : null}
        {!entry.isVerified ? (
          <Badge tone="bg-amber-100 text-amber-900 dark:bg-amber-950 dark:text-amber-200">
            doğrulanmadı
          </Badge>
        ) : null}
      </div>

      <p className="mt-0.5 text-xs text-slate-500">{formatMonthYear(entry.occurredOn)}</p>

      {entry.description ? (
        <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">{entry.description}</p>
      ) : null}
    </li>
  )
}

function groupByYear(entries: TimelineEntry[]): [string, TimelineEntry[]][] {
  const groups = new Map<string, TimelineEntry[]>()

  for (const entry of entries) {
    const year = entry.occurredOn.slice(0, 4)
    const bucket = groups.get(year)
    if (bucket) bucket.push(entry)
    else groups.set(year, [entry])
  }

  // Sunucu zaten tarihe göre azalan sıralı döndürüyor; Map ekleme sırasını korur.
  return [...groups.entries()]
}
