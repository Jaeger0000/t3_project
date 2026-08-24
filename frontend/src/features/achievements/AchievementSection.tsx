import { useState } from 'react'
import type { Achievement } from '@/api/types'
import { formatDate, formatMoney } from '@/lib/format'
import { Badge, Button, Card, EmptyState, ErrorState, Sensitive, Spinner } from '@/components/ui'
import { useSubmitChangeRequest } from '@/features/approvals/queries'
import AchievementForm from './AchievementForm'
import { achievementKindTone } from './labels'
import { useAchievements, useDeleteAchievement } from './queries'

/**
 * `direct` yetkilinin doğrudan yazma yolu, `proposal` girişim kullanıcısının
 * onay isteği yolu. Aynı liste ve aynı form iki yolu da besliyor: ekranın
 * ikiye ayrılması, iki yerde ayrı doğrulama ve ayrı hata mesajı demekti.
 */
export type AchievementMode = 'readonly' | 'direct' | 'proposal'

export default function AchievementSection({
  startupId,
  mode,
}: {
  startupId: string
  mode: AchievementMode
}) {
  const list = useAchievements(startupId)
  const [editing, setEditing] = useState<Achievement | null>(null)
  const [adding, setAdding] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)

  if (list.isPending) return <Spinner label="Başarı kayıtları yükleniyor…" />
  if (list.error) return <ErrorState message={list.error.message} />
  if (!list.data) return null

  const { items, exactAmountsVisible } = list.data

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-semibold text-stone-900 dark:text-stone-100">
            Başarı ve finans kayıtları
          </h2>
          <p className="text-sm text-stone-500">
            {items.length} kayıt
            {mode === 'proposal'
              ? ' · eklediğiniz kayıt onaydan sonra yayına girer'
              : ''}
          </p>
        </div>

        {mode !== 'readonly' && !adding && !editing ? (
          <Button onClick={() => { setAdding(true); setNotice(null) }}>
            {mode === 'proposal' ? 'Kayıt öner' : 'Kayıt ekle'}
          </Button>
        ) : null}
      </div>

      {!exactAmountsVisible ? (
        <p className="rounded-lg bg-stone-100 px-4 py-2.5 text-sm text-stone-600 dark:bg-stone-800 dark:text-stone-300">
          🔒 Rolünüz kayıtların varlığını görür, tutarları görmez. Finansal
          büyüklükler yalnızca ekosistem geneli raporlarda toplu olarak sunulur.
        </p>
      ) : null}

      {notice ? (
        <p className="rounded-lg bg-emerald-50 px-4 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
          {notice}
        </p>
      ) : null}

      {adding ? (
        <AchievementForm
          startupId={startupId}
          mode={mode === 'proposal' ? 'proposal' : 'direct'}
          onDone={(message) => { setAdding(false); setNotice(message) }}
          onCancel={() => setAdding(false)}
        />
      ) : null}

      {editing ? (
        <AchievementForm
          startupId={startupId}
          mode={mode === 'proposal' ? 'proposal' : 'direct'}
          existing={editing}
          onDone={(message) => { setEditing(null); setNotice(message) }}
          onCancel={() => setEditing(null)}
        />
      ) : null}

      {items.length === 0 && !adding ? (
        <EmptyState
          title="Henüz başarı kaydı yok"
          hint="Yatırım turu, hibe, ciro, ihracat ve ödüller buraya alan bazlı olarak girilir."
        />
      ) : null}

      <div className="flex flex-col gap-3">
        {items.map((item) => (
          <AchievementRow
            key={item.id}
            startupId={startupId}
            item={item}
            mode={mode}
            onEdit={() => { setEditing(item); setAdding(false); setNotice(null) }}
            onRemoved={(message) => setNotice(message)}
          />
        ))}
      </div>
    </div>
  )
}

function AchievementRow({
  startupId,
  item,
  mode,
  onEdit,
  onRemoved,
}: {
  startupId: string
  item: Achievement
  mode: AchievementMode
  onEdit: () => void
  onRemoved: (message: string) => void
}) {
  const remove = useDeleteAchievement(startupId)
  const propose = useSubmitChangeRequest()
  const [confirming, setConfirming] = useState(false)

  const busy = remove.isPending || propose.isPending
  const error = remove.error ?? propose.error

  function handleRemove() {
    if (mode === 'proposal') {
      propose.mutate(
        {
          targetType: 'Achievement',
          operation: 'Delete',
          targetId: item.id,
          achievement: null,
        },
        {
          onSuccess: () => {
            setConfirming(false)
            onRemoved('Kaydın kaldırılması önerildi; yetkili onayına gönderildi.')
          },
        },
      )
      return
    }

    remove.mutate(item.id, {
      onSuccess: () => {
        setConfirming(false)
        onRemoved('Kayıt pasife alındı.')
      },
    })
  }

  return (
    <Card className="p-4">
      <div className="flex flex-wrap items-start gap-x-6 gap-y-2">
        <div className="min-w-56 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone={achievementKindTone[item.kind]}>{item.kindLabel}</Badge>
            <p className="font-medium text-stone-900 dark:text-stone-100">{item.title}</p>
            {item.isVerified ? (
              <Badge tone="bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-200">
                ✓ Doğrulandı
              </Badge>
            ) : (
              <Badge>Doğrulanmadı</Badge>
            )}
          </div>

          <p className="mt-1 text-sm text-stone-500">
            {formatDate(item.occurredOn)}
            {item.periodLabel ? ` · ${item.periodLabel}` : ''}
            {item.programName ? ` · ${item.programName}` : ''}
            {item.organization ? ` · ${item.organization}` : ''}
          </p>

          {item.investorNames.length > 0 ? (
            <p className="mt-1 text-sm text-stone-600 dark:text-stone-400">
              Yatırımcılar: {item.investorNames.join(', ')}
            </p>
          ) : null}

          {item.targetCountries.length > 0 ? (
            <p className="mt-1 text-sm text-stone-600 dark:text-stone-400">
              Ülkeler: {item.targetCountries.join(', ')}
            </p>
          ) : null}

          {item.note ? (
            <p className="mt-1 text-sm text-stone-600 dark:text-stone-400">{item.note}</p>
          ) : null}
        </div>

        <div className="text-right">
          {/* Tutar taşımayan kayıtta (ödül) hiçbir yer tutucu gösterilmiyor:
              maskeleme ile "alan yok" birbirine karışmasın. */}
          {item.amountMasked || item.amount !== null ? (
            <p className="text-lg font-bold tabular-nums text-stone-900 dark:text-stone-100">
              <Sensitive
                value={formatMoney(item.amount, item.currency ?? 'TRY')}
                authorized={!item.amountMasked}
              />
            </p>
          ) : null}

          {item.valuation !== null ? (
            <p className="text-xs text-stone-500">
              Değerleme: {formatMoney(item.valuation, item.currency ?? 'TRY')}
            </p>
          ) : null}
        </div>

        {mode !== 'readonly' ? (
          <div className="flex items-center gap-1">
            <Button variant="ghost" onClick={onEdit} disabled={busy}>
              Düzenle
            </Button>

            {confirming ? (
              <>
                <Button variant="outline" onClick={handleRemove} disabled={busy}>
                  {mode === 'proposal' ? 'Kaldırmayı öner' : 'Kaldır'}
                </Button>
                <Button variant="ghost" onClick={() => setConfirming(false)} disabled={busy}>
                  Vazgeç
                </Button>
              </>
            ) : (
              <Button variant="ghost" onClick={() => setConfirming(true)} disabled={busy}>
                Kaldır
              </Button>
            )}
          </div>
        ) : null}
      </div>

      {error ? <p className="mt-2 text-sm text-red-600">{error.message}</p> : null}
    </Card>
  )
}
