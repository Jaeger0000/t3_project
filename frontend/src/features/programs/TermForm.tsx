import { useState } from 'react'
import type { ProgramTerm } from '@/api/types'
import { Button, ErrorState, Input } from '@/components/ui'
import { useAddTerm, useUpdateTerm } from './queries'

/**
 * Dönem formu. Dönem, katılımın (dolayısıyla gelişim yolculuğunun) çapası:
 * tarih sırası doğrulaması sunucuda da var, buradaki `min` yalnızca kullanıcıyı
 * hatadan önce uyarıyor.
 */
export default function TermForm({
  programId,
  term,
  onDone,
  onCancel,
}: {
  programId: string
  term?: ProgramTerm
  onDone: (message: string) => void
  onCancel: () => void
}) {
  const [name, setName] = useState(term?.name ?? '')
  const [startsOn, setStartsOn] = useState(term?.startsOn ?? '')
  const [endsOn, setEndsOn] = useState(term?.endsOn ?? '')

  const add = useAddTerm(programId)
  const update = useUpdateTerm(programId, term?.id ?? '')
  const active = term ? update : add
  const busy = add.isPending || update.isPending

  return (
    <div className="mt-3 rounded-xl border border-stone-200 p-4 dark:border-stone-800">
      <h4 className="text-sm font-medium text-stone-900 dark:text-stone-100">
        {term ? 'Dönemi düzenle' : 'Yeni dönem'}
      </h4>

      <div className="mt-3 grid gap-4 sm:grid-cols-3">
        <Input
          label="Dönem adı"
          value={name}
          maxLength={200}
          placeholder="2026 Bahar"
          onChange={(e) => setName(e.target.value)}
        />
        <Input
          label="Başlangıç"
          type="date"
          value={startsOn}
          onChange={(e) => setStartsOn(e.target.value)}
        />
        <Input
          label="Bitiş (isteğe bağlı)"
          type="date"
          min={startsOn || undefined}
          value={endsOn}
          onChange={(e) => setEndsOn(e.target.value)}
        />
      </div>

      {active.error ? (
        <div className="mt-3">
          <ErrorState message={active.error.message} />
        </div>
      ) : null}

      <div className="mt-4 flex flex-wrap gap-3">
        <Button
          disabled={busy || name.trim().length === 0 || startsOn.length === 0}
          onClick={() => {
            const model = { name: name.trim(), startsOn, endsOn: endsOn || null }

            if (term) {
              update.mutate(model, { onSuccess: () => onDone(`${model.name} güncellendi.`) })
            } else {
              add.mutate(model, { onSuccess: () => onDone(`${model.name} dönemi eklendi.`) })
            }
          }}
        >
          {term ? 'Kaydet' : 'Dönemi ekle'}
        </Button>
        <Button variant="outline" disabled={busy} onClick={onCancel}>
          Vazgeç
        </Button>
      </div>
    </div>
  )
}
