import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
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
  Button,
  Card,
  DataRow,
  EmptyState,
  ErrorState,
  Sensitive,
  Spinner,
} from '@/components/ui'
import { useAuth } from '@/lib/auth'
import { ApiError } from '@/lib/apiClient'
import AchievementSection from '@/features/achievements/AchievementSection'
import DocumentSection from '@/features/documents/DocumentSection'
import StartupTimeline from './StartupTimeline'
import StartupForm from './StartupForm'
import TeamSection from './TeamSection'
import ParticipationForm from './ParticipationForm'
import ParticipationEditor from './ParticipationEditor'
import StartupSummaryCard from '@/features/assistant/StartupSummaryCard'
import AiReportPanel from './AiReportPanel'
import SendNotificationPanel from './SendNotificationPanel'
import { useStartupCard, useDeleteStartup } from './queries'
import type { CardParticipation, StartupCard } from '@/api/types'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

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
  const [editingProfile, setEditingProfile] = useState(false)
  const [showReportPanel, setShowReportPanel] = useState(false)
  const [showNotifyPanel, setShowNotifyPanel] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)

  // Kanca koşulsuz çağrılıyor: kart yüklenene kadar başlık genel kalır.
  useDocumentTitle(card.data?.name)

  // Girişim kullanıcısı kendi kartını da salt okunur görür: düzenleme portalda,
  // çünkü oradan giden her değişiklik onay isteğine dönüşüyor.
  const editMode = session?.permissions.canManageStartups ? 'direct' : 'readonly'

  if (card.isPending) return <Spinner label="Girişim kartı yükleniyor…" />
  if (card.error) {
    return (
      <div className="flex flex-col gap-4">
        {card.error instanceof ApiError && card.error.status === 404 ? (
          <EmptyState
            title="Girişim bulunamadı"
            hint="Kayıt silinmiş, adres yanlış yazılmış ya da bu girişim kapsamınızda olmayabilir."
          />
        ) : (
          <ErrorState message={card.error.message} error={card.error} />
        )}
        <Link to="/girisimler" className="text-sm text-brand-700 hover:underline">
          ← Girişim listesine dön
        </Link>
      </div>
    )
  }
  if (!card.data || !id) return null

  const startup = card.data

  return (
    <div className="flex flex-col gap-6">
      <Link to="/girisimler" className="text-sm text-brand-700 hover:underline">
        ← Girişimler
      </Link>

      <CardHeader
        startup={startup}
        canEdit={editMode === 'direct'}
        canDelete={session?.role === 'SuperAdmin'}
        editing={editingProfile}
        onEdit={() => {
          setEditingProfile(true)
          setNotice(null)
        }}
        onOpenReport={() => setShowReportPanel(true)}
        showReportButton={!showReportPanel}
        onOpenNotify={() => setShowNotifyPanel(true)}
        showNotifyButton={!showNotifyPanel}
      />

      {notice ? (
        <p className="rounded-lg bg-emerald-50 px-4 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
          {notice}
        </p>
      ) : null}

      {editingProfile ? (
        <StartupForm
          mode="direct"
          card={startup}
          onDone={(message) => {
            setEditingProfile(false)
            setNotice(message)
          }}
          onCancel={() => setEditingProfile(false)}
        />
      ) : null}

      {showReportPanel ? (
        <AiReportPanel startup={startup} onClose={() => setShowReportPanel(false)} />
      ) : null}

      {showNotifyPanel ? (
        <SendNotificationPanel startupId={id} onClose={() => setShowNotifyPanel(false)} />
      ) : null}

      <FinancialSummary startup={startup} />

      <div className="flex flex-wrap gap-1 border-b border-stone-200 dark:border-stone-800">
        {tabs.map((item) => (
          <button
            key={item.key}
            type="button"
            onClick={() => setTab(item.key)}
            className={`-mb-px border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
              tab === item.key
                ? 'border-brand-500 text-brand-700 dark:text-brand-100'
                : 'border-transparent text-stone-500 hover:border-brand-300 hover:text-brand-700 dark:hover:text-stone-200'
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
      {tab === 'ekip' ? <TeamSection card={startup} mode={editMode} /> : null}
      {tab === 'programlar' ? (
        <ProgramsTab startup={startup} canEdit={editMode === 'direct'} />
      ) : null}
      {tab === 'basarilar' ? <AchievementSection startupId={id} mode={editMode} /> : null}
      {tab === 'dokumanlar' ? <DocumentSection startupId={id} mode={editMode} /> : null}
      {tab === 'yolculuk' ? <StartupTimeline startupId={id} /> : null}
    </div>
  )
}

function CardHeader({
  startup,
  canEdit,
  canDelete,
  editing,
  onEdit,
  onOpenReport,
  showReportButton,
  onOpenNotify,
  showNotifyButton,
}: {
  startup: StartupCard
  canEdit: boolean
  canDelete: boolean
  editing: boolean
  onEdit: () => void
  onOpenReport: () => void
  showReportButton: boolean
  onOpenNotify: () => void
  showNotifyButton: boolean
}) {
  const initials = startup.name
    .split(' ')
    .slice(0, 2)
    .map((word) => word[0])
    .join('')

  return (
    <div className="flex flex-wrap items-start gap-5">
      <span className="flex size-16 shrink-0 items-center justify-center rounded-xl bg-brand-500 text-xl font-bold text-white">
        {initials}
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">{startup.name}</h1>
          <Badge tone={startupStatusTone[startup.status]}>
            {startupStatusLabels[startup.status]}
          </Badge>
        </div>

        {startup.legalName ? (
          <p className="mt-0.5 text-sm text-stone-500">{startup.legalName}</p>
        ) : null}

        <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-stone-600 dark:text-stone-400">
          <span>{sectorLabels[startup.sector]}</span>
          {startup.city ? <span>{startup.city}</span> : null}
          {startup.foundedOn ? <span>Kuruluş: {formatDate(startup.foundedOn)}</span> : null}
          {startup.website ? (
            <a
              href={startup.website}
              target="_blank"
              rel="noreferrer noopener"
              className="text-brand-700 hover:underline dark:text-brand-100"
            >
              {startup.website.replace(/^https?:\/\//, '')}
            </a>
          ) : null}
        </div>
      </div>

      {/* Yazma yolu yalnızca `canManageStartups` doğruyken render ediliyor;
          sunucu aynı kontrolü Policies.ManageStartups ile tekrar yapıyor. */}
      {canEdit && !editing ? (
        <div className="flex flex-wrap items-center gap-2">
          {/* AI raporu da ManageStartups kapsamında: SuperAdmin/ProgramManager
              erişimi tam olarak canEdit ile örtüşüyor, ayrı bir izin bayrağı
              eklemek gerekmedi. */}
          {showReportButton ? (
            <Button variant="outline" onClick={onOpenReport}>
              AI Raporu
            </Button>
          ) : null}
          {showNotifyButton ? (
            <Button variant="outline" onClick={onOpenNotify}>
              Bildirim Gönder
            </Button>
          ) : null}
          <Button variant="outline" onClick={onEdit}>
            Düzenle
          </Button>
          {/* Silme yalnızca Süper Yönetici'de: politika ManageStartups Program
              Yöneticisi'ni de kapsıyor ama bu işlemin kapsamı daha dar ve
              sunucu handler'ı aynı kontrolü tekrar yapıyor. */}
          {canDelete ? <DeleteStartupButton startup={startup} /> : null}
        </div>
      ) : null}
    </div>
  )
}

/**
 * Girişimi pasife alır. Silme geri alınamaz gibi göründüğü için iki adımlı;
 * yanıt zincirin raporunu taşıyor (kaç ekip üyesi, kaç kayıt pasife alındı) ama
 * kullanıcı listeye döndüğü için rapor yalnızca özet mesaja indiriliyor.
 */
function DeleteStartupButton({ startup }: { startup: StartupCard }) {
  const navigate = useNavigate()
  const remove = useDeleteStartup()
  const [confirming, setConfirming] = useState(false)

  if (!confirming) {
    return (
      <Button variant="ghost" onClick={() => setConfirming(true)}>
        Kaydı pasife al
      </Button>
    )
  }

  return (
    <span className="flex flex-wrap items-center gap-2 text-sm">
      <span className="text-stone-500">Bağlı tüm kayıtlar pasife alınacak. Emin misiniz?</span>
      <Button
        variant="outline"
        disabled={remove.isPending}
        onClick={() =>
          remove.mutate(startup.id, {
            onSuccess: () => navigate('/girisimler', { replace: true }),
          })
        }
      >
        Evet, pasife al
      </Button>
      <Button variant="ghost" disabled={remove.isPending} onClick={() => setConfirming(false)}>
        Vazgeç
      </Button>
      {remove.error ? <span className="text-red-600">{remove.error.message}</span> : null}
    </span>
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
          <p className="text-xs font-medium tracking-wide text-stone-500 uppercase">{cell.label}</p>
          <p className="mt-1.5 text-lg font-bold tabular-nums text-stone-900 dark:text-stone-100">
            <Sensitive value={cell.value} authorized={visibility.exactAmounts} />
          </p>
          {cell.hint ? <p className="mt-0.5 text-xs text-stone-500">{cell.hint}</p> : null}
        </Card>
      ))}

      <Card className="p-4">
        <p className="text-xs font-medium tracking-wide text-stone-500 uppercase">Ödül</p>
        <p className="mt-1.5 text-lg font-bold tabular-nums text-stone-900 dark:text-stone-100">
          {a.awardCount}
        </p>
        <p className="mt-0.5 text-xs text-stone-500">{a.totalCount} başarı kaydı</p>
      </Card>
    </div>
  )
}

function GeneralTab({ startup }: { startup: StartupCard }) {
  const { visibility } = startup

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <Card className="p-5 lg:col-span-2">
        <h2 className="font-semibold text-stone-900 dark:text-stone-100">Ürün ve teknoloji</h2>
        <p className="mt-2 text-sm leading-relaxed text-stone-600 dark:text-stone-400">
          {startup.productDescription ?? 'Ürün açıklaması girilmemiş.'}
        </p>

        {startup.technologyAreas.length > 0 ? (
          <div className="mt-4 flex flex-wrap gap-1.5">
            {startup.technologyAreas.map((area) => (
              <Badge key={area} tone="bg-brand-50 text-brand-800 dark:bg-brand-950 dark:text-brand-100">
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
        <h2 className="font-semibold text-stone-900 dark:text-stone-100">Kurumsal bilgiler</h2>
        <dl className="mt-2 divide-y divide-stone-100 dark:divide-stone-800">
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

/**
 * Program katılımları. Ekleme yolu buradan geçiyor çünkü kapsamı kuran işlem
 * bu: Program Yöneticisi'nin girişim kapsamı "programlarımdan geçmiş
 * girişimler" olarak tanımlı, dolayısıyla yeni girişim ancak bir döneme
 * bağlandığında kapsama giriyor.
 */
function ProgramsTab({
  startup,
  canEdit,
}: {
  startup: StartupCard
  canEdit: boolean
}) {
  const [adding, setAdding] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const participations = startup.programs

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-semibold text-stone-900 dark:text-stone-100">
            Program katılımları ({participations.length})
          </h2>
          <p className="mt-0.5 text-sm text-stone-500">
            Katılımlar gelişim yolculuğunda da kronolojik olarak görünür.
          </p>
        </div>

        {canEdit && !adding ? (
          <Button
            onClick={() => {
              setAdding(true)
              setNotice(null)
            }}
          >
            Programa ekle
          </Button>
        ) : null}
      </div>

      {notice ? (
        <p className="rounded-lg bg-emerald-50 px-4 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
          {notice}
        </p>
      ) : null}

      {adding ? (
        <ParticipationForm
          startupId={startup.id}
          onDone={(message) => {
            setAdding(false)
            setNotice(message)
          }}
          onCancel={() => setAdding(false)}
        />
      ) : null}

      {participations.length === 0 && !adding ? (
        <EmptyState
          title="Program katılımı yok"
          hint="Girişim bir program dönemine bağlandığında burada ve gelişim yolculuğunda görünür."
        />
      ) : null}

      {participations.map((participation) => (
        <ParticipationRow
          key={participation.id}
          participation={participation}
          canEdit={canEdit}
          onNotice={setNotice}
        />
      ))}
    </div>
  )
}

/**
 * Katılım satırı. Düzeltme ve kaldırma Dalga 1'de eklendi: yanlış döneme
 * eklenen kayıt gelişim yolculuğunda kalıcı bir hata olarak duruyordu ve tek
 * çözüm veritabanına elle müdahaleydi.
 */
function ParticipationRow({
  participation,
  canEdit,
  onNotice,
}: {
  participation: CardParticipation
  canEdit: boolean
  onNotice: (message: string) => void
}) {
  const [editing, setEditing] = useState(false)

  return (
    <Card className="flex flex-wrap items-center gap-x-6 gap-y-2 p-4">
      <div className="min-w-56 flex-1">
        <p className="font-medium text-stone-900 dark:text-stone-100">
          {participation.programName}
        </p>
        <p className="text-sm text-stone-500">
          {participation.termName} · {programTypeLabels[participation.programType]}
          {participation.coordinatorship ? ` · ${participation.coordinatorship}` : ''}
        </p>
      </div>

      <Badge>{participationStatusLabels[participation.status]}</Badge>

      <p className="text-sm tabular-nums text-stone-600 dark:text-stone-400">
        {formatDate(participation.joinedOn)}
        {participation.leftOn ? ` → ${formatDate(participation.leftOn)}` : ' → sürüyor'}
      </p>

      {participation.notes ? (
        <p className="w-full text-sm text-stone-600 dark:text-stone-400">{participation.notes}</p>
      ) : null}

      {canEdit ? (
        <div className="flex w-full flex-wrap items-center gap-3">
          <Button variant="ghost" onClick={() => setEditing((value) => !value)}>
            {editing ? 'Düzenlemeyi kapat' : 'Katılımı düzelt'}
          </Button>
        </div>
      ) : null}

      {editing ? (
        <div className="w-full">
          <ParticipationEditor
            participation={participation}
            onDone={(message) => {
              setEditing(false)
              onNotice(message)
            }}
            onCancel={() => setEditing(false)}
          />
        </div>
      ) : null}
    </Card>
  )
}
