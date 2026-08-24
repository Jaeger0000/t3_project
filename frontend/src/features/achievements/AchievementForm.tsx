import { useState } from 'react'
import type {
  Achievement,
  AchievementKind,
  AchievementWriteModel,
  GrantInstitution,
  InvestmentRoundType,
} from '@/api/types'
import { Button, Card, Input, Select } from '@/components/ui'
import { useSubmitChangeRequest } from '@/features/approvals/queries'
import {
  achievementFields,
  achievementKindLabels,
  grantInstitutionLabels,
  investmentRoundLabels,
} from './labels'
import { useAddAchievement, useUpdateAchievement } from './queries'

const kinds = Object.keys(achievementKindLabels) as AchievementKind[]
const roundTypes = Object.keys(investmentRoundLabels) as InvestmentRoundType[]
const institutions = Object.keys(grantInstitutionLabels) as GrantInstitution[]

/**
 * Tür değişince alanlar sıfırlanır ama açılırların varsayılanları modele de
 * yazılır. Yazılmasaydı, kullanıcı hiç dokunmadığı açılırda "TÜBİTAK" görüp
 * sunucudan "destek veren kurum zorunludur" hatası alırdı: ekranda seçili
 * görünen değer gövdede `null` olurdu.
 */
function defaultsFor(kind: AchievementKind): AchievementWriteModel {
  return {
    kind,
    occurredOn: new Date().toISOString().slice(0, 10),
    note: null,
    amount: null,
    currency: achievementFields.hasMoney(kind) ? 'TRY' : null,
    fiscalYear: achievementFields.hasPeriod(kind) ? new Date().getFullYear() : null,
    quarter: null,
    roundType: kind === 'Investment' ? 'Seed' : null,
    valuation: null,
    investorNames: null,
    institution: kind === 'Grant' ? 'Tubitak' : null,
    programName: null,
    awardName: null,
    organization: null,
    rank: null,
    targetCountries: null,
  }
}

function fromExisting(a: Achievement): AchievementWriteModel {
  return {
    kind: a.kind,
    occurredOn: a.occurredOn,
    note: a.note,
    amount: a.amount,
    currency: a.currency ?? 'TRY',
    fiscalYear: a.fiscalYear,
    quarter: a.quarter,
    roundType: a.roundType,
    valuation: a.valuation,
    investorNames: a.investorNames.length > 0 ? a.investorNames : null,
    institution: a.institution,
    programName: a.programName,
    awardName: a.awardName,
    organization: a.organization,
    rank: a.rank,
    targetCountries: a.targetCountries.length > 0 ? a.targetCountries : null,
  }
}

/**
 * Başarı/finans kaydı formu (MVP #4).
 *
 * Tür seçimi alanları belirliyor; sunucudaki doğrulayıcı da aynı bağı kuruyor.
 * Form o kuralı tekrar etmiyor, yalnızca ilgisiz alanları gizliyor — tek
 * doğruluk kaynağı sunucuda kalsın, iki kural birbirinden kaymasın.
 *
 * Düzenlemede tür kilitli: kayıt türü veritabanında ayrıştırıcı kolon,
 * yerinde değiştirilemiyor. Sunucu da 409 ile reddediyor; burada seçeneği
 * kapatmak kullanıcıyı o hataya götürmemek için.
 */
export default function AchievementForm({
  startupId,
  mode,
  existing,
  onDone,
  onCancel,
}: {
  startupId: string
  mode: 'direct' | 'proposal'
  existing?: Achievement
  onDone: (message: string) => void
  onCancel: () => void
}) {
  const [form, setForm] = useState<AchievementWriteModel>(
    existing ? fromExisting(existing) : defaultsFor('Investment'),
  )

  const add = useAddAchievement(startupId)
  const update = useUpdateAchievement(startupId, existing?.id ?? '')
  const propose = useSubmitChangeRequest()

  const busy = add.isPending || update.isPending || propose.isPending
  const error = add.error ?? update.error ?? propose.error

  function set<K extends keyof AchievementWriteModel>(key: K, value: AchievementWriteModel[K]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  /** Boş dizge alanı "boşaltıldı" anlamına gelsin diye null'a çevriliyor. */
  function text(value: string): string | null {
    return value.trim() === '' ? null : value.trim()
  }

  function number(value: string): number | null {
    const parsed = Number(value.replace(',', '.'))
    return value.trim() === '' || Number.isNaN(parsed) ? null : parsed
  }

  function list(value: string): string[] | null {
    const parts = value.split(',').map((v) => v.trim()).filter(Boolean)
    return parts.length > 0 ? parts : null
  }

  function submit() {
    if (mode === 'proposal') {
      propose.mutate(
        {
          targetType: 'Achievement',
          operation: existing ? 'Update' : 'Create',
          targetId: existing?.id ?? null,
          achievement: form,
        },
        {
          onSuccess: () =>
            onDone('Öneriniz onay kuyruğuna alındı; yetkili onayladıktan sonra yayına girer.'),
        },
      )
      return
    }

    if (existing) {
      update.mutate(form, { onSuccess: () => onDone('Kayıt güncellendi.') })
      return
    }

    add.mutate(form, { onSuccess: () => onDone('Kayıt eklendi.') })
  }

  const hasMoney = achievementFields.hasMoney(form.kind)
  const hasPeriod = achievementFields.hasPeriod(form.kind)

  return (
    <Card className="p-5">
      <h3 className="font-semibold text-slate-900 dark:text-slate-100">
        {existing ? 'Kaydı düzenle' : 'Yeni başarı / finans kaydı'}
      </h3>

      <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Select
          label="Kayıt türü"
          value={form.kind}
          disabled={Boolean(existing)}
          onChange={(e) => setForm(defaultsFor(e.target.value as AchievementKind))}
        >
          {kinds.map((kind) => (
            <option key={kind} value={kind}>
              {achievementKindLabels[kind]}
            </option>
          ))}
        </Select>

        <Input
          label="Tarih"
          type="date"
          value={form.occurredOn}
          onChange={(e) => set('occurredOn', e.target.value)}
        />

        {hasMoney ? (
          <>
            <Input
              label="Tutar"
              type="number"
              min={0}
              value={form.amount ?? ''}
              onChange={(e) => set('amount', number(e.target.value))}
            />
            <Select
              label="Para birimi"
              value={form.currency ?? 'TRY'}
              onChange={(e) => set('currency', e.target.value)}
            >
              <option value="TRY">TRY — Türk lirası</option>
              <option value="USD">USD — ABD doları</option>
              <option value="EUR">EUR — Euro</option>
            </Select>
          </>
        ) : null}

        {hasPeriod ? (
          <>
            <Input
              label="Mali yıl"
              type="number"
              value={form.fiscalYear ?? ''}
              onChange={(e) => set('fiscalYear', number(e.target.value))}
            />
            <Select
              label="Çeyrek"
              value={form.quarter === null ? '' : String(form.quarter)}
              onChange={(e) => set('quarter', e.target.value === '' ? null : Number(e.target.value))}
            >
              <option value="">Yıllık (çeyrek yok)</option>
              <option value="1">1. çeyrek</option>
              <option value="2">2. çeyrek</option>
              <option value="3">3. çeyrek</option>
              <option value="4">4. çeyrek</option>
            </Select>
          </>
        ) : null}

        {form.kind === 'Investment' ? (
          <>
            <Select
              label="Tur tipi"
              value={form.roundType ?? 'Seed'}
              onChange={(e) => set('roundType', e.target.value as InvestmentRoundType)}
            >
              {roundTypes.map((type) => (
                <option key={type} value={type}>
                  {investmentRoundLabels[type]}
                </option>
              ))}
            </Select>
            <Input
              label="Tur sonrası değerleme"
              type="number"
              min={0}
              value={form.valuation ?? ''}
              onChange={(e) => set('valuation', number(e.target.value))}
            />
            <Input
              label="Yatırımcılar (virgülle ayırın)"
              value={form.investorNames?.join(', ') ?? ''}
              onChange={(e) => set('investorNames', list(e.target.value))}
            />
          </>
        ) : null}

        {form.kind === 'Grant' ? (
          <>
            <Select
              label="Destek veren kurum"
              value={form.institution ?? 'Tubitak'}
              onChange={(e) => set('institution', e.target.value as GrantInstitution)}
            >
              {institutions.map((item) => (
                <option key={item} value={item}>
                  {grantInstitutionLabels[item]}
                </option>
              ))}
            </Select>
            <Input
              label="Destek programı"
              value={form.programName ?? ''}
              onChange={(e) => set('programName', text(e.target.value))}
            />
          </>
        ) : null}

        {form.kind === 'Award' ? (
          <>
            <Input
              label="Ödül adı"
              value={form.awardName ?? ''}
              onChange={(e) => set('awardName', text(e.target.value))}
            />
            <Input
              label="Ödülü veren kurum"
              value={form.organization ?? ''}
              onChange={(e) => set('organization', text(e.target.value))}
            />
            <Input
              label="Derece (varsa)"
              type="number"
              min={1}
              value={form.rank ?? ''}
              onChange={(e) => set('rank', number(e.target.value))}
            />
          </>
        ) : null}

        {form.kind === 'Export' ? (
          <Input
            label="Hedef ülkeler (virgülle ayırın)"
            value={form.targetCountries?.join(', ') ?? ''}
            onChange={(e) => set('targetCountries', list(e.target.value))}
          />
        ) : null}

        <Input
          label="Not"
          value={form.note ?? ''}
          onChange={(e) => set('note', text(e.target.value))}
        />
      </div>

      {existing ? (
        <p className="mt-3 text-xs text-slate-500">
          Kayıt türü değiştirilemez; farklı bir tür için kaydı kaldırıp yenisini ekleyin.
        </p>
      ) : null}

      {error ? <p className="mt-3 text-sm text-rose-600">{error.message}</p> : null}

      <div className="mt-4 flex gap-2">
        <Button onClick={submit} disabled={busy}>
          {mode === 'proposal' ? 'Onaya gönder' : existing ? 'Kaydet' : 'Ekle'}
        </Button>
        <Button variant="ghost" onClick={onCancel} disabled={busy}>
          Vazgeç
        </Button>
      </div>
    </Card>
  )
}
