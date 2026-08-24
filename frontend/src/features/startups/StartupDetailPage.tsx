import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { formatDate, formatMoney } from '@/lib/format'
import {
  participationStatusLabels,
  programTypeLabels,
  sectorLabels,
  startupStatusLabels,
  startupStatusTone,
} from '@/lib/labels'
import {
  Badge,
  Card,
  DataRow,
  EmptyState,
  ErrorState,
  Sensitive,
  Spinner,
} from '@/components/ui'
import { useAuth } from '@/lib/auth'
import AchievementSection from '@/features/achievements/AchievementSection'
import DocumentSection from '@/features/documents/DocumentSection'
import StartupTimeline from './StartupTimeline'
import StartupSummaryCard from '@/features/assistant/StartupSummaryCard'
import { useStartupCard } from './queries'
import type { CardParticipation, CardTeamMember, StartupCard } from '@/api/types'

type Tab = 'genel' | 'ekip' | 'programlar' | 'basarilar' | 'dokumanlar' | 'yolculuk'

const tabs: { key: Tab; label: string }[] = [
  { key: 'genel', label: 'Genel' },
  { key: 'ekip', label: 'Ekip' },
  { key: 'programlar', label: 'Programlar' },
  { key: 'basarilar', label: 'Başarılar' },
  { key: 'dokumanlar', label: 'Dokümanlar' },
  { key: 'yolculuk', label: 'Gelişim yolculuğu' },
]

export default function StartupDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { session } = useAuth()
  const card = useStartupCard(id)
  const [tab, setTab] = useState<Tab>('genel')

  // Girişim kullanıcısı kendi kartını da salt okunur görür: düzenleme portalda,
  // çünkü oradan giden her değişiklik onay isteğine dönüşüyor.
  const editMode = session?.permissions.canManageStartups ? 'direct' : 'readonly'

  if (card.isPending) return <Spinner label="Girişim kartı yükleniyor…" />
  if (card.error) {
    return (
      <div className="flex flex-col gap-4">
        <ErrorState message={card.error.message} />
        <Link to="/girisimler" className="text-sm text-brand-600 hover:underline">
          ← Girişim listesine dön
        </Link>
      </div>
    )
  }
  if (!card.data || !id) return null

  const startup = card.data

  return (
    <div className="flex flex-col gap-6">
      <Link to="/girisimler" className="text-sm text-brand-600 hover:underline">
        ← Girişimler
      </Link>

      <CardHeader startup={startup} />
      <FinancialSummary startup={startup} />

      <div className="flex flex-wrap gap-1 border-b border-slate-200 dark:border-slate-800">
        {tabs.map((item) => (
          <button
            key={item.key}
            type="button"
            onClick={() => setTab(item.key)}
            className={`-mb-px border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
              tab === item.key
                ? 'border-brand-500 text-brand-700 dark:text-brand-100'
                : 'border-transparent text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'
            }`}
          >
            {item.label}
            {item.key === 'ekip' ? ` (${startup.team.length})` : ''}
            {item.key === 'programlar' ? ` (${startup.programs.length})` : ''}
            {item.key === 'basarilar' ? ` (${startup.achievements.totalCount})` : ''}
            {item.key === 'dokumanlar' && startup.visibility.documents
              ? ` (${startup.documentCount})`
              : ''}
          </button>
        ))}
      </div>

      {tab === 'genel' ? <GeneralTab startup={startup} /> : null}
      {tab === 'ekip' ? <TeamTab startup={startup} /> : null}
      {tab === 'programlar' ? <ProgramsTab participations={startup.programs} /> : null}
      {tab === 'basarilar' ? <AchievementSection startupId={id} mode={editMode} /> : null}
      {tab === 'dokumanlar' ? <DocumentSection startupId={id} mode={editMode} /> : null}
      {tab === 'yolculuk' ? <StartupTimeline startupId={id} /> : null}
    </div>
  )
}

function CardHeader({ startup }: { startup: StartupCard }) {
  const initials = startup.name
    .split(' ')
    .slice(0, 2)
    .map((word) => word[0])
    .join('')

  return (
    <div className="flex flex-wrap items-start gap-5">
      <span className="flex size-16 shrink-0 items-center justify-center rounded-xl bg-brand-900 text-xl font-bold text-white">
        {initials}
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold text-brand-900 dark:text-brand-100">{startup.name}</h1>
          <Badge tone={startupStatusTone[startup.status]}>
            {startupStatusLabels[startup.status]}
          </Badge>
        </div>

        {startup.legalName ? (
          <p className="mt-0.5 text-sm text-slate-500">{startup.legalName}</p>
        ) : null}

        <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-slate-600 dark:text-slate-400">
          <span>{sectorLabels[startup.sector]}</span>
          {startup.city ? <span>{startup.city}</span> : null}
          {startup.foundedOn ? <span>Kuruluş: {formatDate(startup.foundedOn)}</span> : null}
          {startup.website ? (
            <a
              href={startup.website}
              target="_blank"
              rel="noreferrer noopener"
              className="text-brand-600 hover:underline dark:text-brand-100"
            >
              {startup.website.replace(/^https?:\/\//, '')}
            </a>
          ) : null}
        </div>
      </div>
    </div>
  )
}

/**
 * Finansal özet. Tutarları görme yetkisi olmayan rol sayıları görür, meblağ
 * yerine kilit görür — "kayıt yok" ile "yetkiniz yok" ayrımı burada kritik.
 */
function FinancialSummary({ startup }: { startup: StartupCard }) {
  const { achievements: a, visibility } = startup

  const cells: { label: string; value: string | null; hint?: string }[] = [
    {
      label: 'Toplam yatırım',
      value: formatMoney(a.totalInvestment, a.currency),
      hint: `${a.investmentRoundCount} tur`,
    },
    { label: 'Toplam hibe', value: formatMoney(a.totalGrant, a.currency) },
    {
      label: 'Son yıllık ciro',
      value: formatMoney(a.latestAnnualRevenue, a.currency),
      hint: a.latestRevenueYear ? String(a.latestRevenueYear) : undefined,
    },
    { label: 'Toplam ihracat', value: formatMoney(a.totalExport, a.currency) },
  ]

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
      {cells.map((cell) => (
        <Card key={cell.label} className="p-4">
          <p className="text-xs font-medium tracking-wide text-slate-500 uppercase">{cell.label}</p>
          <p className="mt-1.5 text-lg font-bold tabular-nums text-slate-900 dark:text-slate-100">
            <Sensitive value={cell.value} authorized={visibility.exactAmounts} />
          </p>
          {cell.hint ? <p className="mt-0.5 text-xs text-slate-500">{cell.hint}</p> : null}
        </Card>
      ))}

      <Card className="p-4">
        <p className="text-xs font-medium tracking-wide text-slate-500 uppercase">Ödül</p>
        <p className="mt-1.5 text-lg font-bold tabular-nums text-slate-900 dark:text-slate-100">
          {a.awardCount}
        </p>
        <p className="mt-0.5 text-xs text-slate-500">{a.totalCount} başarı kaydı</p>
      </Card>
    </div>
  )
}

function GeneralTab({ startup }: { startup: StartupCard }) {
  const { visibility } = startup

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <Card className="p-5 lg:col-span-2">
        <h2 className="font-semibold text-slate-900 dark:text-slate-100">Ürün ve teknoloji</h2>
        <p className="mt-2 text-sm leading-relaxed text-slate-600 dark:text-slate-400">
          {startup.productDescription ?? 'Ürün açıklaması girilmemiş.'}
        </p>

        {startup.technologyAreas.length > 0 ? (
          <div className="mt-4 flex flex-wrap gap-1.5">
            {startup.technologyAreas.map((area) => (
              <Badge key={area} tone="bg-brand-50 text-brand-900 dark:bg-brand-950 dark:text-brand-100">
                {area}
              </Badge>
            ))}
          </div>
        ) : null}
      </Card>

      <div className="lg:col-span-2">
        <StartupSummaryCard startupId={startup.id} />
      </div>

      <Card className="p-5">
        <h2 className="font-semibold text-slate-900 dark:text-slate-100">Kurumsal bilgiler</h2>
        <dl className="mt-2 divide-y divide-slate-100 dark:divide-slate-800">
          <DataRow label="Vergi kimlik no">
            <Sensitive value={startup.taxNumber} authorized={visibility.taxNumber} />
          </DataRow>
          <DataRow label="İletişim e-postası">
            <Sensitive value={startup.contactEmail} authorized={visibility.contactDetails} />
          </DataRow>
          <DataRow label="İletişim telefonu">
            <Sensitive value={startup.contactPhone} authorized={visibility.contactDetails} />
          </DataRow>
          <DataRow label="Doküman">
            {visibility.documents ? `${startup.documentCount} dosya` : (
              <Sensitive value={null} authorized={false} />
            )}
          </DataRow>
          <DataRow label="Kayıt güncellemesi">
            {formatDate(startup.updatedAt) ?? formatDate(startup.createdAt) ?? '—'}
          </DataRow>
        </dl>
      </Card>
    </div>
  )
}

function TeamTab({ startup }: { startup: StartupCard }) {
  if (startup.team.length === 0) {
    return <EmptyState title="Ekip üyesi eklenmemiş" />
  }

  return (
    <div className="flex flex-col gap-4">
      {!startup.visibility.teamPersonalData ? (
        <p className="rounded-lg bg-slate-100 px-4 py-2.5 text-sm text-slate-600 dark:bg-slate-800 dark:text-slate-300">
          🔒 Ekip üyelerinin iletişim bilgileri kişisel veridir (KVKK) ve rolünüze
          gösterilmez. İsim ve ünvan bilgisi görünür.
        </p>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {startup.team.map((member) => (
          <TeamCard key={member.id} member={member} showPersonalData={startup.visibility.teamPersonalData} />
        ))}
      </div>
    </div>
  )
}

function TeamCard({
  member,
  showPersonalData,
}: {
  member: CardTeamMember
  showPersonalData: boolean
}) {
  return (
    <Card className="p-5">
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="truncate font-medium text-slate-900 dark:text-slate-100">{member.fullName}</p>
          <p className="mt-0.5 text-sm text-slate-500">{member.title ?? '—'}</p>
        </div>
        {member.isFounder ? (
          <Badge tone="bg-accent-500/15 text-accent-600 dark:text-accent-400">Kurucu</Badge>
        ) : null}
      </div>

      <dl className="mt-3 divide-y divide-slate-100 text-sm dark:divide-slate-800">
        <DataRow label="E-posta">
          <Sensitive value={member.email} authorized={showPersonalData} />
        </DataRow>
        <DataRow label="Telefon">
          <Sensitive value={member.phone} authorized={showPersonalData} />
        </DataRow>
        <DataRow label="Katılım">{formatDate(member.joinedOn) ?? '—'}</DataRow>
      </dl>
    </Card>
  )
}

function ProgramsTab({ participations }: { participations: CardParticipation[] }) {
  if (participations.length === 0) {
    return (
      <EmptyState
        title="Program katılımı yok"
        hint="Girişim bir program dönemine bağlandığında burada ve gelişim yolculuğunda görünür."
      />
    )
  }

  return (
    <div className="flex flex-col gap-3">
      {participations.map((participation) => (
        <Card key={participation.id} className="flex flex-wrap items-center gap-x-6 gap-y-2 p-4">
          <div className="min-w-56 flex-1">
            <p className="font-medium text-slate-900 dark:text-slate-100">
              {participation.programName}
            </p>
            <p className="text-sm text-slate-500">
              {participation.termName} · {programTypeLabels[participation.programType]}
              {participation.coordinatorship ? ` · ${participation.coordinatorship}` : ''}
            </p>
          </div>

          <Badge>{participationStatusLabels[participation.status]}</Badge>

          <p className="text-sm tabular-nums text-slate-600 dark:text-slate-400">
            {formatDate(participation.joinedOn)}
            {participation.leftOn ? ` → ${formatDate(participation.leftOn)}` : ' → sürüyor'}
          </p>

          {participation.notes ? (
            <p className="w-full text-sm text-slate-600 dark:text-slate-400">{participation.notes}</p>
          ) : null}
        </Card>
      ))}
    </div>
  )
}
