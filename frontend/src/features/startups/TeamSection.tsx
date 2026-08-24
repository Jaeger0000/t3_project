import { useState } from 'react'
import type { CardTeamMember, StartupCard, TeamMemberWriteModel } from '@/api/types'
import { formatDate } from '@/lib/format'
import { Badge, Button, Card, DataRow, Input, Sensitive } from '@/components/ui'
import { useSubmitChangeRequest } from '@/features/approvals/queries'
import { useAddTeamMember, useRemoveTeamMember, useUpdateTeamMember } from './queries'

/**
 * Ekip bölümü. Portal (öneri) ve girişim kartı (doğrudan yazma) aynı listeyi ve
 * aynı formu kullanıyor; ayrım tek propta duruyor. `readonly` kip yetkisiz
 * rollerde düğmeleri hiç render etmez — gerçek kontrol yine sunucuda.
 */
export type TeamSectionMode = 'readonly' | 'direct' | 'proposal'

const emptyMember: TeamMemberWriteModel = {
  fullName: '',
  title: null,
  email: null,
  phone: null,
  linkedInUrl: null,
  isFounder: false,
  joinedOn: null,
}

export default function TeamSection({
  card,
  mode,
}: {
  card: StartupCard
  mode: TeamSectionMode
}) {
  const [editing, setEditing] = useState<CardTeamMember | 'new' | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const canWrite = mode !== 'readonly'

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-semibold text-stone-900 dark:text-stone-100">
            Ekip ({card.team.length})
          </h2>
          <p className="mt-0.5 text-sm text-stone-500">
            {mode === 'proposal'
              ? 'Ekip değişiklikleri de onaya tabidir.'
              : mode === 'direct'
                ? 'Eklediğiniz üye anında karta yansır.'
                : 'Kurucular ve ekip üyeleri.'}
          </p>
        </div>

        {canWrite && !editing ? (
          <Button
            onClick={() => {
              setEditing('new')
              setNotice(null)
            }}
          >
            {mode === 'proposal' ? 'Yeni üye öner' : 'Üye ekle'}
          </Button>
        ) : null}
      </div>

      {!card.visibility.teamPersonalData ? (
        <p className="rounded-lg bg-stone-100 px-4 py-2.5 text-sm text-stone-600 dark:bg-stone-800 dark:text-stone-300">
          🔒 Ekip üyelerinin iletişim bilgileri kişisel veridir (KVKK) ve rolünüze
          gösterilmez. İsim ve ünvan bilgisi görünür.
        </p>
      ) : null}

      {notice ? (
        <p className="rounded-lg bg-emerald-50 px-4 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
          {notice}
        </p>
      ) : null}

      {editing ? (
        <MemberForm
          startupId={card.id}
          mode={mode === 'proposal' ? 'proposal' : 'direct'}
          member={editing === 'new' ? null : editing}
          onDone={(message) => {
            setEditing(null)
            setNotice(message)
          }}
          onCancel={() => setEditing(null)}
        />
      ) : null}

      {card.team.length === 0 && !editing ? (
        <p className="rounded-xl border border-dashed border-stone-300 px-4 py-8 text-center text-sm text-stone-500 dark:border-stone-700">
          Kayıtlı ekip üyesi yok.
        </p>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {card.team.map((member) => (
          <MemberCard
            key={member.id}
            startupId={card.id}
            member={member}
            mode={mode}
            showPersonalData={card.visibility.teamPersonalData}
            onEdit={() => {
              setEditing(member)
              setNotice(null)
            }}
            onRemoved={(message) => setNotice(message)}
          />
        ))}
      </div>
    </div>
  )
}

function MemberCard({
  startupId,
  member,
  mode,
  showPersonalData,
  onEdit,
  onRemoved,
}: {
  startupId: string
  member: CardTeamMember
  mode: TeamSectionMode
  showPersonalData: boolean
  onEdit: () => void
  onRemoved: (message: string) => void
}) {
  const remove = useRemoveTeamMember(startupId)
  const propose = useSubmitChangeRequest()
  const [confirming, setConfirming] = useState(false)

  const busy = remove.isPending || propose.isPending
  const error = remove.error ?? propose.error

  // Silme önerisi gövde taşımaz; hedef kimliği yeterli.
  function handleRemove() {
    if (mode === 'proposal') {
      propose.mutate(
        { targetType: 'TeamMember', operation: 'Delete', targetId: member.id },
        {
          onSuccess: () => {
            setConfirming(false)
            onRemoved(`${member.fullName} için çıkarma önerisi gönderildi.`)
          },
        },
      )
      return
    }

    remove.mutate(member.id, {
      onSuccess: () => {
        setConfirming(false)
        onRemoved(`${member.fullName} ekipten çıkarıldı.`)
      },
    })
  }

  return (
    <Card className="flex flex-col p-5">
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="truncate font-medium text-stone-900 dark:text-stone-100">
            {member.fullName}
          </p>
          <p className="mt-0.5 text-sm text-stone-500">{member.title ?? '—'}</p>
        </div>
        {member.isFounder ? (
          <Badge tone="bg-brand-100 text-brand-800 dark:bg-brand-950 dark:text-brand-200">
            Kurucu
          </Badge>
        ) : null}
      </div>

      <dl className="mt-3 divide-y divide-stone-100 text-sm dark:divide-stone-800">
        <DataRow label="E-posta">
          <Sensitive value={member.email} authorized={showPersonalData} />
        </DataRow>
        <DataRow label="Telefon">
          <Sensitive value={member.phone} authorized={showPersonalData} />
        </DataRow>
        <DataRow label="Katılım">{formatDate(member.joinedOn) ?? '—'}</DataRow>
      </dl>

      {mode !== 'readonly' ? (
        <div className="mt-3 flex flex-wrap items-center gap-1">
          <Button variant="ghost" onClick={onEdit} disabled={busy}>
            {mode === 'proposal' ? 'Düzenleme öner' : 'Düzenle'}
          </Button>

          {confirming ? (
            <>
              <Button variant="outline" onClick={handleRemove} disabled={busy}>
                {mode === 'proposal' ? 'Çıkarmayı öner' : 'Çıkar'}
              </Button>
              <Button variant="ghost" onClick={() => setConfirming(false)} disabled={busy}>
                Vazgeç
              </Button>
            </>
          ) : (
            <Button variant="ghost" onClick={() => setConfirming(true)} disabled={busy}>
              Çıkar
            </Button>
          )}
        </div>
      ) : null}

      {error ? <p className="mt-2 text-sm text-red-600">{error.message}</p> : null}
    </Card>
  )
}

function MemberForm({
  startupId,
  mode,
  member,
  onDone,
  onCancel,
}: {
  startupId: string
  mode: 'direct' | 'proposal'
  member: CardTeamMember | null
  onDone: (message: string) => void
  onCancel: () => void
}) {
  const propose = useSubmitChangeRequest()
  const add = useAddTeamMember(startupId)
  const update = useUpdateTeamMember(startupId, member?.id ?? '')

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

  const busy = propose.isPending || add.isPending || update.isPending
  const error = propose.error ?? add.error ?? update.error

  function handleSubmit() {
    if (mode === 'proposal') {
      propose.mutate(
        {
          targetType: 'TeamMember',
          operation: member ? 'Update' : 'Create',
          targetId: member?.id ?? null,
          teamMember: form,
        },
        { onSuccess: () => onDone('Ekip önerisi onay kuyruğuna alındı.') },
      )
      return
    }

    if (member) {
      update.mutate(form, { onSuccess: () => onDone(`${form.fullName} güncellendi.`) })
      return
    }

    add.mutate(form, { onSuccess: () => onDone(`${form.fullName} ekibe eklendi.`) })
  }

  const heading = member
    ? mode === 'proposal'
      ? `${member.fullName} için düzenleme önerisi`
      : `${member.fullName} kaydını düzenle`
    : mode === 'proposal'
      ? 'Yeni ekip üyesi önerisi'
      : 'Yeni ekip üyesi'

  return (
    <div className="rounded-xl border border-stone-200 p-4 dark:border-stone-800">
      <h3 className="text-sm font-medium text-stone-900 dark:text-stone-100">{heading}</h3>

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
        <label className="flex items-center gap-2 text-sm text-stone-700 dark:text-stone-300">
          <input
            type="checkbox"
            checked={form.isFounder}
            onChange={(e) => set('isFounder', e.target.checked)}
          />
          Kurucu ortak
        </label>
      </div>

      {/* Portal formunda kişisel veri uyarısı: girilen iletişim bilgisi KVKK
          kapsamında ve yalnızca yetkili rollere gösteriliyor. */}
      <p className="mt-3 text-xs text-stone-500">
        Girdiğiniz iletişim bilgileri kişisel veridir; yalnızca yetkili roller görür.
      </p>

      {error ? <p className="mt-3 text-sm text-red-600">{error.message}</p> : null}

      <div className="mt-4 flex flex-wrap gap-3">
        <Button disabled={busy || !form.fullName.trim()} onClick={handleSubmit}>
          {mode === 'proposal' ? 'Öneriyi gönder' : 'Kaydet'}
        </Button>
        <Button variant="outline" disabled={busy} onClick={onCancel}>
          Vazgeç
        </Button>
      </div>
    </div>
  )
}
