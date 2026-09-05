import { useMemo, useState } from 'react'
import type { Sector, StartupCard, StartupStatus, StartupWriteModel } from '@/api/types'
import { sectorLabels, startupStatusLabels } from '@/lib/labels'
import { Button, Card, ErrorState, Input, Select } from '@/components/ui'
import UnsavedChangesGuard from '@/lib/UnsavedChangesGuard'
import { useSubmitChangeRequest } from '@/features/approvals/queries'
import { useCreateStartup, useUpdateStartup } from './queries'

/**
 * Girişim profili formu. Tek form iki yolu besliyor (AchievementSection'daki
 * `mode` deseninin aynısı):
 *
 * - `proposal` → `POST /api/change-requests`; girişim kullanıcısı hiçbir tabloya
 *   doğrudan yazmaz, önerisi onaydan sonra yayına girer.
 * - `direct`   → `POST/PUT /api/startups`; yalnızca `canManageStartups` doğruyken
 *   render edilir, sunucu tarafı `Policies.ManageStartups` ile ikinci kez kontrol eder.
 *
 * Formu ikiye ayırmak, aynı 13 alan için iki ayrı doğrulama ve iki ayrı hata
 * mesajı demekti.
 */
export type StartupFormMode = 'proposal' | 'direct'

const sectors = Object.keys(sectorLabels) as Sector[]
const statuses = Object.keys(startupStatusLabels) as StartupStatus[]

const emptyStartup: StartupWriteModel = {
  name: '',
  legalName: null,
  taxNumber: null,
  foundedOn: null,
  sector: 'Other',
  technologyAreas: [],
  productDescription: null,
  website: null,
  logoUrl: null,
  city: null,
  contactEmail: null,
  contactPhone: null,
  status: 'Active',
}

export default function StartupForm({
  mode,
  card,
  onDone,
  onCancel,
  onCreated,
}: {
  mode: StartupFormMode
  /** Yoksa yeni kayıt: yalnızca `direct` kipinde anlamlı. */
  card?: StartupCard | null
  onDone?: (message: string) => void
  onCancel?: () => void
  onCreated?: (created: { id: string; name: string }) => void
}) {
  const propose = useSubmitChangeRequest()
  const create = useCreateStartup()
  // Kanca koşulsuz çağrılıyor; kimlik yokken zaten kullanılmıyor.
  const update = useUpdateStartup(card?.id ?? '')

  const initial = useMemo<StartupWriteModel>(
    () =>
      card
        ? {
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
          }
        : emptyStartup,
    [card],
  )

  const [form, setForm] = useState(initial)
  const [sent, setSent] = useState<string | null>(null)

  const set = <K extends keyof StartupWriteModel>(key: K, value: StartupWriteModel[K]) => {
    setForm({ ...form, [key]: value })
    setSent(null)
  }

  // Maskeli alanlar form dışında bırakılır. Sebep: sunucudan null gelen vergi
  // numarasını geri yollamak "alanı boşalt" demek olurdu. `direct` kipte alanı
  // gizlemek yetiyor — sunucu maskeli alanı zaten koruyor (StartupWriteModel
  // .ApplyTo(startup, visibility)). `proposal` kipte form hiç açılmıyor: öneri
  // gövdesi diff üretiyor ve göremediğiniz bir alan için değişiklik önermek
  // karar vericiyi yanıltırdı.
  const showTaxNumber = !card || card.visibility.taxNumber
  const showContact = !card || card.visibility.contactDetails

  if (mode === 'proposal' && (!showTaxNumber || !showContact)) {
    return (
      <Card className="px-6 py-5">
        <h2 className="font-medium text-stone-900 dark:text-stone-100">Girişim profili</h2>
        <p className="mt-2 text-sm text-stone-500">
          Profil formunu açabilmek için tüm alanları görme yetkiniz olmalı. Bazı
          alanlar hesabınıza maskeli geldiği için düzenleme kapalı — aksi hâlde
          göremediğiniz bir alanı yanlışlıkla boşaltmayı önerebilirdiniz.
        </p>
        {onCancel ? (
          <div className="mt-4">
            <Button variant="outline" onClick={onCancel}>
              Kapat
            </Button>
          </div>
        ) : null}
      </Card>
    )
  }

  // Kirli form: kullanıcının girdiği hiçbir değer kaydedilmemişse uyarı
  // gerekmiyor; başarıyla gönderildiyse de (sent) artık kayıp riski yok.
  const dirty = sent === null && JSON.stringify(form) !== JSON.stringify(initial)

  const busy = propose.isPending || create.isPending || update.isPending
  const error = propose.error ?? create.error ?? update.error
  const areas = (form.technologyAreas ?? []).join(', ')

  function handleSubmit() {
    if (mode === 'proposal') {
      propose.mutate(
        { targetType: 'Startup', operation: 'Update', startup: form },
        {
          onSuccess: () => {
            setSent('Öneriniz kuyruğa alındı. Onaylanana kadar kartta eski bilgiler görünür.')
            onDone?.('Öneriniz onay kuyruğuna alındı.')
          },
        },
      )
      return
    }

    if (card) {
      update.mutate(form, {
        onSuccess: () => {
          setSent('Girişim kaydı güncellendi.')
          onDone?.('Girişim kaydı güncellendi.')
        },
      })
      return
    }

    create.mutate(form, {
      onSuccess: (created) => {
        onDone?.(`${created.name} kaydedildi.`)
        onCreated?.(created)
      },
    })
  }

  const heading = card
    ? 'Girişim profili'
    : 'Yeni girişim'

  return (
    <Card className="px-6 py-5">
      <h2 className="font-medium text-stone-900 dark:text-stone-100">{heading}</h2>
      <p className="mt-1 text-sm text-stone-500">
        {mode === 'proposal'
          ? 'Değişiklikleriniz öneri olarak kuyruğa girer, onaylanana kadar kartta görünmez.'
          : card
            ? 'Değişiklik kaydedildiği anda karta ve raporlara yansır.'
            : 'Zorunlu alanlar girişim adı ve sektör; kalanı sonradan tamamlanabilir.'}
      </p>

      {dirty ? (
        <div className="mt-4">
          <UnsavedChangesGuard dirty={dirty} />
        </div>
      ) : null}

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
        {showTaxNumber ? (
          <Input
            label="Vergi numarası"
            value={form.taxNumber ?? ''}
            onChange={(e) => set('taxNumber', e.target.value || null)}
          />
        ) : null}
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
        {showContact ? (
          <>
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
          </>
        ) : null}
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
          <span className="text-sm font-medium text-stone-700 dark:text-stone-300">
            Ürün açıklaması
          </span>
          <textarea
            rows={3}
            value={form.productDescription ?? ''}
            onChange={(e) => set('productDescription', e.target.value || null)}
            className="rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/60 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100"
          />
        </label>
      </div>

      {!showTaxNumber || !showContact ? (
        <p className="mt-4 rounded-lg bg-stone-100 px-4 py-2.5 text-sm text-stone-600 dark:bg-stone-800 dark:text-stone-300">
          🔒 Rolünüze maskeli gelen alanlar ({[!showTaxNumber ? 'vergi numarası' : null,
          !showContact ? 'iletişim bilgileri' : null].filter(Boolean).join(', ')}) bu
          formda yok. Kaydettiğinizde mevcut değerleri korunur.
        </p>
      ) : null}

      {error ? (
        <div className="mt-4">
          <ErrorState message={error.message} error={error} />
        </div>
      ) : null}

      {sent ? (
        <p className="mt-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
          {sent}
        </p>
      ) : null}

      <div className="mt-5 flex flex-wrap gap-3">
        <Button disabled={busy || !form.name.trim()} onClick={handleSubmit}>
          {mode === 'proposal' ? 'Değişikliği öner' : card ? 'Kaydet' : 'Girişimi oluştur'}
        </Button>

        {onCancel ? (
          <Button variant="outline" disabled={busy} onClick={onCancel}>
            Vazgeç
          </Button>
        ) : (
          <Button
            variant="outline"
            disabled={busy}
            onClick={() => {
              setForm(initial)
              setSent(null)
            }}
          >
            Sıfırla
          </Button>
        )}
      </div>
    </Card>
  )
}
