import { useState } from 'react'
import type { CardParticipation, ParticipationStatus } from '@/api/types'
import { participationStatusLabels } from '@/lib/labels'
import { Button, ErrorState, Input, Select } from '@/components/ui'
import { useRemoveParticipation, useUpdateParticipation } from '@/features/programs/queries'

const statuses = Object.keys(participationStatusLabels) as ParticipationStatus[]

/**
 * Katılım düzeltmesi ve kaldırma.
 *
 * Program ve dönem alanları bilinçli olarak <b>yok</b>: yanlış döneme eklenen
 * kayıt taşınmıyor, kaldırılıp yeniden ekleniyor. Taşımak, gelişim
 * yolculuğundaki tarihi tek istekle yeniden yazmak olurdu — sunucu da bu yüzden
 * yalnızca durum/tarih/not kabul ediyor.
 */
export default function ParticipationEditor({
  participation,
  onDone,
  onCancel,
}: {
  participation: CardParticipation
  onDone: (message: string) => void
  onCancel: () => void
}) {
  const [status, setStatus] = useState<ParticipationStatus>(participation.status)
  const [joinedOn, setJoinedOn] = useState(participation.joinedOn)
  const [leftOn, setLeftOn] = useState(participation.leftOn ?? '')
  const [notes, setNotes] = useState(participation.notes ?? '')
  const [confirming, setConfirming] = useState(false)

  const update = useUpdateParticipation(participation.id)
  const remove = useRemoveParticipation()
  const busy = update.isPending || remove.isPending

  return (
    <div className="rounded-xl border border-stone-200 p-4 dark:border-stone-800">
      <h4 className="text-sm font-medium text-stone-900 dark:text-stone-100">
        {participation.programName} · {participation.termName}
      </h4>

      <div className="mt-3 grid gap-4 sm:grid-cols-2">
        <Select
          label="Durum"
          value={status}
          onChange={(e) => setStatus(e.target.value as ParticipationStatus)}
        >
          {statuses.map((value) => (
            <option key={value} value={value}>
              {participationStatusLabels[value]}
            </option>
          ))}
        </Select>

        <Input
          label="Katılım tarihi"
          type="date"
          value={joinedOn}
          onChange={(e) => setJoinedOn(e.target.value)}
        />

        <Input
          label="Ayrılış tarihi (isteğe bağlı)"
          type="date"
          min={joinedOn || undefined}
          value={leftOn}
          onChange={(e) => setLeftOn(e.target.value)}
        />

        <Input
          label="Not (isteğe bağlı)"
          value={notes}
          maxLength={2000}
          onChange={(e) => setNotes(e.target.value)}
        />
      </div>

      {update.error ? (
        <div className="mt-3">
          <ErrorState message={update.error.message} error={update.error} />
        </div>
      ) : null}
      {remove.error ? (
        <div className="mt-3">
          <ErrorState message={remove.error.message} error={remove.error} />
        </div>
      ) : null}

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <Button
          disabled={busy || joinedOn.length === 0}
          onClick={() =>
            update.mutate(
              {
                status,
                joinedOn,
                leftOn: leftOn || null,
                notes: notes.trim() || null,
              },
              { onSuccess: () => onDone('Katılım kaydı güncellendi.') },
            )
          }
        >
          {update.isPending ? 'Kaydediliyor…' : 'Kaydet'}
        </Button>

        {/* Kaldırma iki adımlı: kayıt gelişim yolculuğunun kaynağı. */}
        {confirming ? (
          <>
            <span className="text-xs text-stone-500">
              Bu katılım gelişim yolculuğundan da düşecek. Emin misiniz?
            </span>
            <Button
              variant="outline"
              disabled={busy}
              onClick={() =>
                remove.mutate(participation.id, {
                  onSuccess: () => onDone('Katılım kaydı kaldırıldı.'),
                })
              }
            >
              {remove.isPending ? 'Kaldırılıyor…' : 'Evet, kaldır'}
            </Button>
          </>
        ) : (
          <Button variant="ghost" disabled={busy} onClick={() => setConfirming(true)}>
            Katılımı kaldır
          </Button>
        )}

        <Button variant="ghost" disabled={busy} onClick={onCancel}>
          Vazgeç
        </Button>
      </div>
    </div>
  )
}
