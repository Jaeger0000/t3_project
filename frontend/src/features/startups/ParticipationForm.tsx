import { useState } from 'react'
import type { ParticipationStatus } from '@/api/types'
import { formatDate } from '@/lib/format'
import { participationStatusLabels } from '@/lib/labels'
import { Button, ErrorState, Input, Select, Spinner } from '@/components/ui'
import { useAddParticipation, usePrograms } from './queries'

/**
 * Girişimi bir program dönemine bağlar — gelişim yolculuğunun (MVP #2)
 * birincil kaynağı. Dönem listesi `GET /api/programs` yanıtındaki `terms`
 * dizisinden geliyor, ek uç yok.
 *
 * Program açılırında yalnızca kullanıcının kapsamındaki programlar var; kapsam
 * dışı bir dönem yine denenirse sunucu "Bu program sizin sorumluluğunuzda
 * değil." hatası döner ve mesaj burada gösterilir.
 */
const statuses = Object.keys(participationStatusLabels) as ParticipationStatus[]

export default function ParticipationForm({
  startupId,
  onDone,
  onCancel,
}: {
  startupId: string
  onDone: (message: string) => void
  onCancel: () => void
}) {
  const programs = usePrograms()
  const add = useAddParticipation()

  const [programId, setProgramId] = useState('')
  const [programTermId, setProgramTermId] = useState('')
  const [status, setStatus] = useState<ParticipationStatus>('Accepted')
  const [joinedOn, setJoinedOn] = useState('')
  const [notes, setNotes] = useState('')

  if (programs.isPending) return <Spinner label="Programlar yükleniyor…" />
  if (programs.error) return <ErrorState message={programs.error.message} error={programs.error} />

  const list = programs.data ?? []
  const terms = list.find((p) => p.id === programId)?.terms ?? []

  return (
    <div className="rounded-xl border border-stone-200 p-4 dark:border-stone-800">
      <h3 className="text-sm font-medium text-stone-900 dark:text-stone-100">
        Programa ekle
      </h3>

      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <Select
          label="Program"
          value={programId}
          onChange={(e) => {
            setProgramId(e.target.value)
            // Dönem programa bağlı: program değişince eski dönem geçersiz.
            setProgramTermId('')
          }}
        >
          <option value="">Seçin…</option>
          {list.map((program) => (
            <option key={program.id} value={program.id}>
              {program.name}
            </option>
          ))}
        </Select>

        <Select
          label="Dönem"
          value={programTermId}
          disabled={!programId}
          onChange={(e) => setProgramTermId(e.target.value)}
        >
          <option value="">{programId ? 'Seçin…' : 'Önce program seçin'}</option>
          {terms.map((term) => (
            <option key={term.id} value={term.id}>
              {term.name} ({formatDate(term.startsOn)})
            </option>
          ))}
        </Select>

        <Select
          label="Durum"
          value={status}
          onChange={(e) => setStatus(e.target.value as ParticipationStatus)}
        >
          {statuses.map((s) => (
            <option key={s} value={s}>
              {participationStatusLabels[s]}
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
          label="Not (isteğe bağlı)"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
        />
      </div>

      {programId && terms.length === 0 ? (
        <p className="mt-3 text-sm text-stone-500">
          Bu programda tanımlı dönem yok; katılım eklenebilmesi için önce dönem
          oluşturulmalı.
        </p>
      ) : null}

      {add.error ? (
        <div className="mt-3">
          <ErrorState message={add.error.message} error={add.error} />
        </div>
      ) : null}

      <div className="mt-4 flex flex-wrap gap-3">
        <Button
          disabled={add.isPending || !programTermId || !joinedOn}
          onClick={() =>
            add.mutate(
              {
                startupId,
                programTermId,
                status,
                joinedOn,
                leftOn: null,
                notes: notes.trim() || null,
              },
              { onSuccess: () => onDone('Girişim program dönemine bağlandı.') },
            )
          }
        >
          Programa ekle
        </Button>
        <Button variant="outline" disabled={add.isPending} onClick={onCancel}>
          Vazgeç
        </Button>
      </div>
    </div>
  )
}
