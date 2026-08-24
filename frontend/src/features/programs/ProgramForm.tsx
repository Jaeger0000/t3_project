import { useState } from 'react'
import type { Program, ProgramType } from '@/api/types'
import { programTypeLabels } from '@/lib/labels'
import { Button, ErrorState, Input, Select } from '@/components/ui'
import { useCreateProgram, useUpdateProgram } from './queries'

const types = Object.keys(programTypeLabels) as ProgramType[]

/**
 * Program tanım formu. Oluşturma ve düzenleme aynı bileşen: sunucudaki gövde
 * de tek (PUT tam değiştirme), iki form tutmak alan listesini iki yerde
 * senkron tutma yükü olurdu.
 */
export default function ProgramForm({
  program,
  onDone,
  onCancel,
}: {
  program?: Program
  onDone: (message: string) => void
  onCancel: () => void
}) {
  const [name, setName] = useState(program?.name ?? '')
  const [type, setType] = useState<ProgramType>(program?.type ?? 'Incubation')
  const [coordinatorship, setCoordinatorship] = useState(program?.coordinatorship ?? '')
  const [description, setDescription] = useState(program?.description ?? '')

  const create = useCreateProgram()
  const update = useUpdateProgram(program?.id ?? '')
  const active = program ? update : create
  const busy = create.isPending || update.isPending

  return (
    <div className="rounded-xl border border-stone-200 p-4 dark:border-stone-800">
      <h3 className="text-sm font-medium text-stone-900 dark:text-stone-100">
        {program ? 'Programı düzenle' : 'Yeni program'}
      </h3>

      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <Input
          label="Program adı"
          value={name}
          maxLength={200}
          onChange={(e) => setName(e.target.value)}
        />

        <Select label="Tür" value={type} onChange={(e) => setType(e.target.value as ProgramType)}>
          {types.map((value) => (
            <option key={value} value={value}>
              {programTypeLabels[value]}
            </option>
          ))}
        </Select>

        <Input
          label="Koordinatörlük (isteğe bağlı)"
          value={coordinatorship}
          maxLength={200}
          onChange={(e) => setCoordinatorship(e.target.value)}
        />

        <Input
          label="Açıklama (isteğe bağlı)"
          value={description}
          maxLength={2000}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>

      {active.error ? (
        <div className="mt-3">
          <ErrorState message={active.error.message} />
        </div>
      ) : null}

      <div className="mt-4 flex flex-wrap gap-3">
        <Button
          disabled={busy || name.trim().length === 0}
          onClick={() => {
            const model = {
              name: name.trim(),
              type,
              coordinatorship: coordinatorship.trim() || null,
              description: description.trim() || null,
            }

            if (program) {
              update.mutate(model, { onSuccess: () => onDone(`${model.name} güncellendi.`) })
            } else {
              create.mutate(model, { onSuccess: () => onDone(`${model.name} oluşturuldu.`) })
            }
          }}
        >
          {program ? 'Kaydet' : 'Programı oluştur'}
        </Button>
        <Button variant="outline" disabled={busy} onClick={onCancel}>
          Vazgeç
        </Button>
      </div>
    </div>
  )
}
