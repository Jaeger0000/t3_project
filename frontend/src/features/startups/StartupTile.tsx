import { Link } from 'react-router-dom'
import { Badge, Card } from '@/components/ui'
import { formatCompactMoney, formatYear } from '@/lib/format'
import { sectorLabels, startupStatusLabels, startupStatusTone } from '@/lib/labels'
import type { StartupListItem } from '@/api/types'

/**
 * Liste döşemesi. `min-w-0` olmadan döşeme kendi min-content'i kadar yer
 * istiyor ve 375 px'te sayfayı yatay kaydırıyordu; tutar sütunu dar ekranda
 * bir punto küçülüyor.
 *
 * Girişimler sayfasının içinden buraya taşındı: AI sohbeti de cevabın
 * dayandığı girişimleri kartla gösteriyor. İki kopya olsaydı maskeleme
 * (kilit simgesi) iki yerde bakım isterdi — KVKK kuralının ekrandaki
 * karşılığı tek dosyada durmalı.
 */
export default function StartupTile({ startup }: { startup: StartupListItem }) {
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
