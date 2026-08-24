import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import type {
  CardTeamMember,
  Sector,
  StartupCard,
  StartupStatus,
  StartupWriteModel,
  TeamMemberWriteModel,
} from '@/api/types'
import { useAuth } from '@/lib/auth'
import { sectorLabels, startupStatusLabels } from '@/lib/labels'
import { useStartupCard } from '@/features/startups/queries'
import AchievementSection from '@/features/achievements/AchievementSection'
import DocumentSection from '@/features/documents/DocumentSection'
import { useSubmitChangeRequest } from '@/features/approvals/queries'
import { Badge, Button, Card, ErrorState, Input, Select, Spinner } from '@/components/ui'

/**
 * Girişim portalı (MVP #3).
 *
 * Buradaki hiçbir form doğrudan yazmaz; hepsi `/api/change-requests` üzerinden
 * öneri üretir. Ekranın metinleri de bunu açıkça söyler — kullanıcı "kaydettim"
 * sanıp değişikliğin yayına girdiğini varsaymamalı.
 */
export default function PortalPage() {
  const { session } = useAuth()
  const startupId = session?.startupId ?? undefined
  const { data: card, isPending, error } = useStartupCard(startupId)

  if (!startupId) {
    return (
      <ErrorState message="Hesabınız bir girişime bağlı değil; portal ekranı yalnızca girişim kullanıcılarına açıktır." />
    )
  }
  if (isPending) return <Spinner label="Girişim bilgileri yükleniyor…" />
  if (error) return <ErrorState message={error.message} />
  if (!card) return null

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900 dark:text-slate-50">
            {card.name}
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            Girişim portalı — buradan gönderdiğiniz her değişiklik program
            yöneticisinin onayından sonra yayına girer.
          </p>
        </div>
        <Link
          to="/onaylar"
          className="text-sm text-brand-600 hover:underline dark:text-brand-300"
        >
          Önerilerim ve durumları →
        </Link>
      </header>

      <ProfileForm card={card} />
      <TeamSection card={card} />

      <hr className="border-slate-200 dark:border-slate-800" />
      <AchievementSection startupId={startupId} mode="proposal" />

      <hr className="border-slate-200 dark:border-slate-800" />
      <DocumentSection startupId={startupId} mode="proposal" />
    </div>
  )
}

// --------------------------------------------------------------- profil formu

const sectors = Object.keys(sectorLabels) as Sector[]
const statuses = Object.keys(startupStatusLabels) as StartupStatus[]

function ProfileForm({ card }: { card: StartupCard }) {
  const submit = useSubmitChangeRequest()
  const [sent, setSent] = useState(false)

  const initial = useMemo<StartupWriteModel>(
    () => ({
      name: card.name,
      legalName: card.legalName,
      taxNumber: card.taxNumber,
      foundedOn: card.foundedOn ? card.foundedOn.slice(0, 10) : null,
      sector: card.sector,
      technologyAreas: card.technologyAreas,
      productDescription: card.productDescription,
      website: card.website,
      logoUrl: card.logoUrl,
      city: card.city,
      contactEmail: card.contactEmail,
      contactPhone: card.contactPhone,
      status: card.status,
    }),
    [card],
  )

  const [form, setForm] = useState(initial)
  const set = <K extends keyof StartupWriteModel>(key: K, value: StartupWriteModel[K]) => {
    setForm({ ...form, [key]: value })
    setSent(false)
  }

  // Maskelenmiş bir alanla form gönderilemez: sunucudan null gelen vergi
  // numarasını geri yollamak "alanı boşalt" önerisi anlamına gelirdi.
  if (!card.visibility.taxNumber || !card.visibility.contactDetails) {
    return (
      <Card className="px-6 py-5">
        <h2 className="font-medium text-slate-900 dark:text-slate-100">Girişim profili</h2>
        <p className="mt-2 text-sm text-slate-500">
          Profil formunu açabilmek için tüm alanları görme yetkiniz olmalı. Bazı
          alanlar hesabınıza maskeli geldiği için düzenleme kapalı — aksi hâlde
          göremediğiniz bir alanı yanlışlıkla boşaltmayı önerebilirdiniz.
        </p>
      </Card>
    )
  }

  const areas = (form.technologyAreas ?? []).join(', ')

  return (
    <Card className="px-6 py-5">
      <h2 className="font-medium text-slate-900 dark:text-slate-100">Girişim profili</h2>
      <p className="mt-1 text-sm text-slate-500">
        Değişiklikleriniz öneri olarak kuyruğa girer, onaylanana kadar kartta
        görünmez.
      </p>

      <div className="mt-5 grid gap-4 sm:grid-cols-2">
        <Input
          label="Girişim adı"
          value={form.name}
          onChange={(e) => set('name', e.target.value)}
        />
        <Input
          label="Ticari unvan"
          value={form.legalName ?? ''}
          onChange={(e) => set('legalName', e.target.value || null)}
        />
        <Input
          label="Vergi numarası"
          value={form.taxNumber ?? ''}
          onChange={(e) => set('taxNumber', e.target.value || null)}
        />
        <Input
          label="Kuruluş tarihi"
          type="date"
          value={form.foundedOn ?? ''}
          onChange={(e) => set('foundedOn', e.target.value || null)}
        />
        <Select
          label="Sektör"
          value={form.sector}
          onChange={(e) => set('sector', e.target.value as Sector)}
        >
          {sectors.map((s) => (
            <option key={s} value={s}>
              {sectorLabels[s]}
            </option>
          ))}
        </Select>
        <Select
          label="Durum"
          value={form.status ?? 'Active'}
          onChange={(e) => set('status', e.target.value as StartupStatus)}
        >
          {statuses.map((s) => (
            <option key={s} value={s}>
              {startupStatusLabels[s]}
            </option>
          ))}
        </Select>
        <Input
          label="Şehir"
          value={form.city ?? ''}
          onChange={(e) => set('city', e.target.value || null)}
        />
        <Input
          label="Web sitesi"
          value={form.website ?? ''}
          onChange={(e) => set('website', e.target.value || null)}
        />
        <Input
          label="İletişim e-postası"
          value={form.contactEmail ?? ''}
          onChange={(e) => set('contactEmail', e.target.value || null)}
        />
        <Input
          label="İletişim telefonu"
          value={form.contactPhone ?? ''}
          onChange={(e) => set('contactPhone', e.target.value || null)}
        />
        <Input
          label="Teknoloji alanları (virgülle ayırın)"
          value={areas}
          onChange={(e) =>
            set(
              'technologyAreas',
              e.target.value
                .split(',')
                .map((a) => a.trim())
                .filter(Boolean),
            )
          }
        />
        <label className="flex flex-col gap-1.5 sm:col-span-2">
          <span className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Ürün açıklaması
          </span>
          <textarea
            rows={3}
            value={form.productDescription ?? ''}
            onChange={(e) => set('productDescription', e.target.value || null)}
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-100"
          />
        </label>
      </div>

      {submit.error ? (
        <div className="mt-4">
          <ErrorState message={submit.error.message} />
        </div>
      ) : null}
      {sent ? (
        <p className="mt-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
          Öneriniz kuyruğa alındı. Onaylanana kadar kartta eski bilgiler görünür.
        </p>
      ) : null}

      <div className="mt-5 flex gap-3">
        <Button
          disabled={submit.isPending}
          onClick={() =>
            submit.mutate(
              { targetType: 'Startup', operation: 'Update', startup: form },
              { onSuccess: () => setSent(true) },
            )
          }
        >
          Değişikliği öner
        </Button>
        <Button variant="outline" onClick={() => { setForm(initial); setSent(false) }}>
          Sıfırla
        </Button>
      </div>
    </Card>
  )
}

// ----------------------------------------------------------------- ekip bölümü

const emptyMember: TeamMemberWriteModel = {
  fullName: '',
  title: null,
  email: null,
  phone: null,
  linkedInUrl: null,
  isFounder: false,
  joinedOn: null,
}

function TeamSection({ card }: { card: StartupCard }) {
  const [editing, setEditing] = useState<CardTeamMember | 'new' | null>(null)

  return (
    <Card className="px-6 py-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-medium text-slate-900 dark:text-slate-100">Ekip</h2>
          <p className="mt-1 text-sm text-slate-500">
            Ekip değişiklikleri de onaya tabidir.
          </p>
        </div>
        <Button variant="outline" onClick={() => setEditing('new')}>
          Yeni üye öner
        </Button>
      </div>

      <ul className="mt-4 divide-y divide-slate-100 dark:divide-slate-800">
        {card.team.map((member) => (
          <li key={member.id} className="flex flex-wrap items-center gap-3 py-3">
            <div className="min-w-40 flex-1">
              <p className="font-medium text-slate-900 dark:text-slate-100">
                {member.fullName}
              </p>
              <p className="text-sm text-slate-500">{member.title ?? '—'}</p>
            </div>
            {member.isFounder ? <Badge>Kurucu</Badge> : null}
            <Button variant="ghost" onClick={() => setEditing(member)}>
              Düzenleme öner
            </Button>
            <DeleteProposal member={member} />
          </li>
        ))}
      </ul>

      {card.team.length === 0 ? (
        <p className="py-4 text-sm text-slate-500">Kayıtlı ekip üyesi yok.</p>
      ) : null}

      {editing ? (
        <MemberForm
          member={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
        />
      ) : null}
    </Card>
  )
}

function MemberForm({
  member,
  onClose,
}: {
  member: CardTeamMember | null
  onClose: () => void
}) {
  const submit = useSubmitChangeRequest()
  const [form, setForm] = useState<TeamMemberWriteModel>(
    member
      ? {
          fullName: member.fullName,
          title: member.title,
          email: member.email,
          phone: member.phone,
          linkedInUrl: member.linkedInUrl,
          isFounder: member.isFounder,
          joinedOn: member.joinedOn ? member.joinedOn.slice(0, 10) : null,
        }
      : emptyMember,
  )

  const set = <K extends keyof TeamMemberWriteModel>(
    key: K,
    value: TeamMemberWriteModel[K],
  ) => setForm({ ...form, [key]: value })

  return (
    <div className="mt-4 rounded-lg border border-slate-200 p-4 dark:border-slate-800">
      <h3 className="text-sm font-medium text-slate-900 dark:text-slate-100">
        {member ? `${member.fullName} için düzenleme önerisi` : 'Yeni ekip üyesi önerisi'}
      </h3>

      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <Input
          label="Ad soyad"
          value={form.fullName}
          onChange={(e) => set('fullName', e.target.value)}
        />
        <Input
          label="Ünvan"
          value={form.title ?? ''}
          onChange={(e) => set('title', e.target.value || null)}
        />
        <Input
          label="E-posta"
          value={form.email ?? ''}
          onChange={(e) => set('email', e.target.value || null)}
        />
        <Input
          label="Telefon"
          value={form.phone ?? ''}
          onChange={(e) => set('phone', e.target.value || null)}
        />
        <Input
          label="LinkedIn"
          value={form.linkedInUrl ?? ''}
          onChange={(e) => set('linkedInUrl', e.target.value || null)}
        />
        <Input
          label="Katılım tarihi"
          type="date"
          value={form.joinedOn ?? ''}
          onChange={(e) => set('joinedOn', e.target.value || null)}
        />
        <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={form.isFounder}
            onChange={(e) => set('isFounder', e.target.checked)}
          />
          Kurucu ortak
        </label>
      </div>

      {submit.error ? (
        <div className="mt-3">
          <ErrorState message={submit.error.message} />
        </div>
      ) : null}

      <div className="mt-4 flex gap-3">
        <Button
          disabled={submit.isPending || !form.fullName.trim()}
          onClick={() =>
            submit.mutate(
              {
                targetType: 'TeamMember',
                operation: member ? 'Update' : 'Create',
                targetId: member?.id ?? null,
                teamMember: form,
              },
              { onSuccess: onClose },
            )
          }
        >
          Öneriyi gönder
        </Button>
        <Button variant="outline" onClick={onClose}>
          Vazgeç
        </Button>
      </div>
    </div>
  )
}

/** Silme önerisi gövde taşımaz; hedef kimliği yeterli. */
function DeleteProposal({ member }: { member: CardTeamMember }) {
  const submit = useSubmitChangeRequest()
  const [confirming, setConfirming] = useState(false)

  if (submit.isSuccess) {
    return <span className="text-xs text-emerald-600">Silme önerisi gönderildi</span>
  }

  if (!confirming) {
    return (
      <Button variant="ghost" onClick={() => setConfirming(true)}>
        Çıkarılmasını öner
      </Button>
    )
  }

  return (
    <span className="flex items-center gap-2 text-xs">
      <span className="text-slate-500">Emin misiniz?</span>
      <Button
        variant="outline"
        disabled={submit.isPending}
        onClick={() =>
          submit.mutate({
            targetType: 'TeamMember',
            operation: 'Delete',
            targetId: member.id,
          })
        }
      >
        Evet, öner
      </Button>
      <Button variant="ghost" onClick={() => setConfirming(false)}>
        Vazgeç
      </Button>
    </span>
  )
}
