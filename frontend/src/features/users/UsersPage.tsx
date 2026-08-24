import { useState } from 'react'
import type { UserRole, UserRow } from '@/api/types'
import { useAuth } from '@/lib/auth'
import { formatDate } from '@/lib/format'
import { roleLabels } from '@/lib/labels'
import { usePrograms, useStartups } from '@/features/startups/queries'
import { defaultFilters } from '@/features/startups/queries'
import {
  Badge,
  Button,
  Card,
  EmptyState,
  ErrorState,
  Input,
  Select,
  Spinner,
} from '@/components/ui'
import {
  defaultUserFilters,
  useCreateUser,
  useDeactivateUser,
  useSetPassword,
  useUpdateUser,
  useUsers,
} from './queries'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

const roles: UserRole[] = ['SuperAdmin', 'ProgramManager', 'StartupUser', 'DecisionMaker']

/**
 * Kullanıcı ve rol yönetimi. Faz 1'den ertelenen parça; onay akışıyla birlikte
 * geldi çünkü portal hesabı açmadan girişim portalı gösterilemiyor.
 *
 * Rol ile kapsam bağı (girişim / program) sunucuda zorunlu tutuluyor; form
 * aynı kuralı yalnızca alanları gizleyip göstererek yansıtıyor — doğrulama
 * kopyalanmıyor, hata mesajı sunucudan geliyor.
 */
export default function UsersPage() {
  useDocumentTitle('Kullanıcılar')
  const [filters, setFilters] = useState(defaultUserFilters)
  const { data, isPending, error } = useUsers(filters)
  const [creating, setCreating] = useState(false)

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-stone-900 dark:text-stone-50">
            Kullanıcılar
          </h1>
          <p className="mt-1 text-sm text-stone-500">
            Hesaplar, roller ve kapsam atamaları. Hesaplar silinmez, pasife alınır —
            denetim izindeki aktör bilgisi korunur.
          </p>
        </div>
        <Button onClick={() => setCreating(!creating)}>
          {creating ? 'Formu kapat' : 'Yeni kullanıcı'}
        </Button>
      </header>

      {creating ? <CreateUserForm onDone={() => setCreating(false)} /> : null}

      <Card className="px-5 py-4">
        <div className="grid gap-4 sm:grid-cols-3">
          <Input
            label="Ara"
            placeholder="Ad veya e-posta"
            value={filters.q}
            onChange={(e) => setFilters({ ...filters, q: e.target.value, page: 1 })}
          />
          <Select
            label="Rol"
            value={filters.role}
            onChange={(e) =>
              setFilters({ ...filters, role: e.target.value as UserRole | '', page: 1 })
            }
          >
            <option value="">Tümü</option>
            {roles.map((r) => (
              <option key={r} value={r}>
                {roleLabels[r]}
              </option>
            ))}
          </Select>
          <Select
            label="Durum"
            value={filters.isActive}
            onChange={(e) =>
              setFilters({
                ...filters,
                isActive: e.target.value as UserFilterActive,
                page: 1,
              })
            }
          >
            <option value="">Tümü</option>
            <option value="true">Aktif</option>
            <option value="false">Pasif</option>
          </Select>
        </div>
      </Card>

      {error ? <ErrorState message={error.message} /> : null}
      {isPending ? <Spinner label="Kullanıcılar yükleniyor…" /> : null}
      {data && data.items.length === 0 ? (
        <EmptyState title="Bu filtrede kullanıcı yok" />
      ) : null}

      <div className="flex flex-col gap-3">
        {data?.items.map((user) => (
          <UserCard key={user.id} user={user} />
        ))}
      </div>
    </div>
  )
}

type UserFilterActive = '' | 'true' | 'false'

function UserCard({ user }: { user: UserRow }) {
  const { session } = useAuth()
  const [mode, setMode] = useState<'none' | 'edit' | 'password'>('none')
  const deactivate = useDeactivateUser(user.id)
  const isSelf = session?.id === user.id

  return (
    <Card className="px-5 py-4">
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <div className="min-w-48 flex-1">
          <p className="font-medium text-stone-900 dark:text-stone-100">
            {user.fullName}
            {isSelf ? <span className="ml-2 text-xs text-stone-400">(siz)</span> : null}
          </p>
          <p className="text-sm text-stone-500">{user.email}</p>
        </div>

        <Badge>{user.roleLabel}</Badge>

        <div className="text-sm text-stone-600 dark:text-stone-300">
          {user.startupName ??
            (user.programs.length > 0
              ? user.programs.map((p) => p.name).join(', ')
              : 'tüm ekosistem')}
        </div>

        <Badge
          tone={
            user.isActive
              ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300'
              : 'bg-stone-200 text-stone-600 dark:bg-stone-800 dark:text-stone-300'
          }
        >
          {user.isActive ? 'Aktif' : 'Pasif'}
        </Badge>

        <span className="text-xs text-stone-500">
          {user.lastLoginAt ? `son giriş ${formatDate(user.lastLoginAt)}` : 'hiç giriş yapmadı'}
        </span>

        <div className="flex gap-1">
          <Button variant="ghost" onClick={() => setMode(mode === 'edit' ? 'none' : 'edit')}>
            Düzenle
          </Button>
          <Button
            variant="ghost"
            onClick={() => setMode(mode === 'password' ? 'none' : 'password')}
          >
            Şifre
          </Button>
          {user.isActive && !isSelf ? (
            <Button variant="ghost" onClick={() => deactivate.mutate(undefined)}>
              Pasife al
            </Button>
          ) : null}
        </div>
      </div>

      {deactivate.error ? (
        <div className="mt-3">
          <ErrorState message={deactivate.error.message} />
        </div>
      ) : null}

      {mode === 'edit' ? <EditUserForm user={user} onDone={() => setMode('none')} /> : null}
      {mode === 'password' ? (
        <PasswordForm user={user} onDone={() => setMode('none')} />
      ) : null}
    </Card>
  )
}

/**
 * Rol seçimine göre kapsam alanı değiştiren ortak parça. Girişim kullanıcısı
 * bir girişime, program yöneticisi en az bir programa bağlanmak zorunda;
 * diğer iki rol hiçbirine bağlanamaz.
 */
function ScopeFields({
  role,
  startupId,
  programIds,
  onStartup,
  onPrograms,
}: {
  role: UserRole
  startupId: string | null
  programIds: string[]
  onStartup: (id: string | null) => void
  onPrograms: (ids: string[]) => void
}) {
  const { data: programs } = usePrograms()
  const { data: startups } = useStartups({ ...defaultFilters, pageSize: 100 })

  if (role === 'StartupUser') {
    return (
      <Select
        label="Girişim"
        value={startupId ?? ''}
        onChange={(e) => onStartup(e.target.value || null)}
      >
        <option value="">Seçin…</option>
        {startups?.items.map((s) => (
          <option key={s.id} value={s.id}>
            {s.name}
          </option>
        ))}
      </Select>
    )
  }

  if (role === 'ProgramManager') {
    return (
      <fieldset className="flex flex-col gap-1.5">
        <legend className="text-sm font-medium text-stone-700 dark:text-stone-300">
          Programlar
        </legend>
        <div className="flex flex-wrap gap-3">
          {programs?.map((p) => (
            <label key={p.id} className="flex items-center gap-1.5 text-sm">
              <input
                type="checkbox"
                checked={programIds.includes(p.id)}
                onChange={(e) =>
                  onPrograms(
                    e.target.checked
                      ? [...programIds, p.id]
                      : programIds.filter((id) => id !== p.id),
                  )
                }
              />
              {p.name}
            </label>
          ))}
        </div>
      </fieldset>
    )
  }

  return (
    <p className="self-end text-sm text-stone-500">
      Bu rol tüm ekosistemi görür; ayrı kapsam ataması yapılmaz.
    </p>
  )
}

function CreateUserForm({ onDone }: { onDone: () => void }) {
  const create = useCreateUser()
  const [email, setEmail] = useState('')
  const [fullName, setFullName] = useState('')
  const [role, setRole] = useState<UserRole>('ProgramManager')
  const [password, setPassword] = useState('')
  const [startupId, setStartupId] = useState<string | null>(null)
  const [programIds, setProgramIds] = useState<string[]>([])

  return (
    <Card className="px-6 py-5">
      <h2 className="font-medium text-stone-900 dark:text-stone-100">Yeni kullanıcı</h2>

      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <Input label="E-posta" value={email} onChange={(e) => setEmail(e.target.value)} />
        <Input
          label="Ad soyad"
          value={fullName}
          onChange={(e) => setFullName(e.target.value)}
        />
        <Select
          label="Rol"
          value={role}
          onChange={(e) => {
            setRole(e.target.value as UserRole)
            setStartupId(null)
            setProgramIds([])
          }}
        >
          {roles.map((r) => (
            <option key={r} value={r}>
              {roleLabels[r]}
            </option>
          ))}
        </Select>
        <Input
          label="Başlangıç şifresi"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="En az 10 karakter, harf ve rakam"
        />
        <ScopeFields
          role={role}
          startupId={startupId}
          programIds={programIds}
          onStartup={setStartupId}
          onPrograms={setProgramIds}
        />
      </div>

      {create.error ? (
        <div className="mt-4">
          <ErrorState message={create.error.message} />
        </div>
      ) : null}

      <div className="mt-5 flex gap-3">
        <Button
          disabled={create.isPending}
          onClick={() =>
            create.mutate(
              {
                email,
                fullName,
                role,
                password,
                startupId: role === 'StartupUser' ? startupId : null,
                programIds: role === 'ProgramManager' ? programIds : null,
              },
              { onSuccess: onDone },
            )
          }
        >
          Oluştur
        </Button>
        <Button variant="outline" onClick={onDone}>
          Vazgeç
        </Button>
      </div>
    </Card>
  )
}

function EditUserForm({ user, onDone }: { user: UserRow; onDone: () => void }) {
  const update = useUpdateUser(user.id)
  const [fullName, setFullName] = useState(user.fullName)
  const [role, setRole] = useState(user.role)
  const [isActive, setIsActive] = useState(user.isActive)
  const [startupId, setStartupId] = useState(user.startupId)
  const [programIds, setProgramIds] = useState(user.programs.map((p) => p.id))

  return (
    <div className="mt-4 border-t border-stone-100 pt-4 dark:border-stone-800">
      <div className="grid gap-4 sm:grid-cols-2">
        <Input
          label="Ad soyad"
          value={fullName}
          onChange={(e) => setFullName(e.target.value)}
        />
        <Select
          label="Rol"
          value={role}
          onChange={(e) => {
            setRole(e.target.value as UserRole)
            setStartupId(null)
            setProgramIds([])
          }}
        >
          {roles.map((r) => (
            <option key={r} value={r}>
              {roleLabels[r]}
            </option>
          ))}
        </Select>
        <ScopeFields
          role={role}
          startupId={startupId}
          programIds={programIds}
          onStartup={setStartupId}
          onPrograms={setProgramIds}
        />
        <label className="flex items-center gap-2 self-end text-sm text-stone-700 dark:text-stone-300">
          <input
            type="checkbox"
            checked={isActive}
            onChange={(e) => setIsActive(e.target.checked)}
          />
          Hesap aktif
        </label>
      </div>

      <p className="mt-3 text-xs text-stone-500">
        E-posta değiştirilemez: denetim izinde aktörün karşılığı olduğu için taşınması
        geçmişi bulanıklaştırır.
      </p>

      {update.error ? (
        <div className="mt-3">
          <ErrorState message={update.error.message} />
        </div>
      ) : null}

      <div className="mt-4 flex gap-3">
        <Button
          disabled={update.isPending}
          onClick={() =>
            update.mutate(
              {
                fullName,
                role,
                startupId: role === 'StartupUser' ? startupId : null,
                programIds: role === 'ProgramManager' ? programIds : null,
                isActive,
              },
              { onSuccess: onDone },
            )
          }
        >
          Kaydet
        </Button>
        <Button variant="outline" onClick={onDone}>
          Vazgeç
        </Button>
      </div>
    </div>
  )
}

function PasswordForm({ user, onDone }: { user: UserRow; onDone: () => void }) {
  const setPassword = useSetPassword(user.id)
  const [value, setValue] = useState('')

  return (
    <div className="mt-4 flex flex-wrap items-end gap-3 border-t border-stone-100 pt-4 dark:border-stone-800">
      <Input
        label="Yeni şifre"
        type="password"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder="En az 10 karakter, harf ve rakam"
      />
      <Button
        disabled={setPassword.isPending || !value}
        onClick={() => setPassword.mutate(value, { onSuccess: onDone })}
      >
        Şifreyi ata
      </Button>
      <Button variant="outline" onClick={onDone}>
        Vazgeç
      </Button>
      {setPassword.error ? <ErrorState message={setPassword.error.message} /> : null}
    </div>
  )
}
