/**
 * El yazımı SVG grafikler.
 *
 * Grafik kütüphanesi eklemedik: ihtiyacımız iki grafik türü (yatay çubuk ve
 * halka) ve bunlar 60 satır SVG. Bir kütüphane, paket boyutunun yanında kendi
 * tema/erişilebilirlik varsayımlarını da getirirdi.
 *
 * Her grafik metin karşılığını da basar — headless render doğrulaması ve ekran
 * okuyucu, SVG yolundan değil metinden okur.
 */
import type { ReactNode } from 'react'

export type ChartDatum = { key: string; label: string; value: number; hint?: string }

const palette = [
  'var(--color-brand-500)',
  'var(--color-accent-500)',
  '#0ea5e9',
  '#8b5cf6',
  '#f59e0b',
  '#10b981',
  '#ef4444',
  '#64748b',
]

export function BarChart({
  data,
  formatValue = (value: number) => String(value),
  emptyHint = 'Gösterilecek veri yok.',
}: {
  data: ChartDatum[]
  formatValue?: (value: number) => string
  emptyHint?: string
}) {
  const max = Math.max(...data.map((d) => d.value), 0)

  if (data.length === 0 || max === 0) {
    return <p className="py-6 text-center text-sm text-slate-500">{emptyHint}</p>
  }

  return (
    <ul className="flex flex-col gap-2.5">
      {data.map((datum, index) => (
        <li key={datum.key} className="grid grid-cols-[9rem_1fr_auto] items-center gap-3">
          <span className="truncate text-sm text-slate-600 dark:text-slate-300" title={datum.label}>
            {datum.label}
          </span>
          <span className="h-2.5 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
            <span
              className="block h-full rounded-full"
              style={{
                width: `${Math.max((datum.value / max) * 100, 2)}%`,
                backgroundColor: palette[index % palette.length],
              }}
            />
          </span>
          <span className="text-sm font-semibold tabular-nums text-slate-900 dark:text-slate-100">
            {formatValue(datum.value)}
          </span>
        </li>
      ))}
    </ul>
  )
}

/**
 * Halka grafik. Dilimler `stroke-dasharray` ile çiziliyor: tek bir çember
 * elemanı, dilim başına bir katman — yol (path) hesabı yapmadan.
 */
export function DonutChart({
  data,
  centerLabel,
  centerValue,
}: {
  data: ChartDatum[]
  centerLabel: string
  centerValue: string
}) {
  const total = data.reduce((sum, datum) => sum + datum.value, 0)

  if (total === 0) {
    return <p className="py-6 text-center text-sm text-slate-500">Gösterilecek veri yok.</p>
  }

  const radius = 60
  const circumference = 2 * Math.PI * radius

  // Dilim uzunlukları ve başlangıç kaymaları render öncesinde hesaplanıyor;
  // map içinde biriken bir sayaç tutmak render sırasında yan etki olurdu.
  const slices = data.reduce<{ datum: ChartDatum; length: number; offset: number }[]>(
    (acc, datum) => {
      const previous = acc.at(-1)
      const offset = previous ? previous.offset + previous.length : 0
      return [...acc, { datum, length: (datum.value / total) * circumference, offset }]
    },
    [],
  )

  return (
    <div className="flex flex-wrap items-center justify-center gap-6">
      <svg viewBox="0 0 160 160" className="size-40 shrink-0" role="img" aria-label={centerLabel}>
        <g transform="rotate(-90 80 80)">
          {slices.map((slice, index) => (
            <circle
              key={slice.datum.key}
              cx="80"
              cy="80"
              r={radius}
              fill="none"
              strokeWidth="20"
              stroke={palette[index % palette.length]}
              strokeDasharray={`${slice.length} ${circumference - slice.length}`}
              strokeDashoffset={-slice.offset}
            />
          ))}
        </g>
        <text
          x="80"
          y="74"
          textAnchor="middle"
          className="fill-slate-900 text-[22px] font-bold dark:fill-slate-100"
        >
          {centerValue}
        </text>
        <text x="80" y="94" textAnchor="middle" className="fill-slate-500 text-[11px]">
          {centerLabel}
        </text>
      </svg>

      <ul className="flex min-w-40 flex-col gap-1.5">
        {data.map((datum, index) => (
          <li key={datum.key} className="flex items-center gap-2 text-sm">
            <span
              className="size-2.5 shrink-0 rounded-full"
              style={{ backgroundColor: palette[index % palette.length] }}
              aria-hidden
            />
            <span className="flex-1 truncate text-slate-600 dark:text-slate-300">{datum.label}</span>
            <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">
              {datum.value}
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}

export function ChartCard({
  title,
  hint,
  children,
}: {
  title: string
  hint?: string
  children: ReactNode
}) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <header className="mb-4">
        <h2 className="font-semibold text-slate-900 dark:text-slate-100">{title}</h2>
        {hint ? <p className="mt-0.5 text-xs text-slate-500">{hint}</p> : null}
      </header>
      {children}
    </section>
  )
}
