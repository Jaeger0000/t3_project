import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@/lib/auth'
import { Button, Card, ErrorState, Input } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import { useChangePassword } from './queries'

/**
 * Oturum içi şifre değiştirme.
 *
 * İki yoldan geliniyor: kullanıcı kendi isteğiyle ya da yönetici şifre attığı
 * için zorunlu olarak (o durumda başka ekrana geçilemiyor — bkz. RequireAuth).
 * Mevcut şifre isteniyor: jeton çalınmış bir oturumda hesabın tamamen
 * devralınmasını engelleyen tek kontrol bu.
 */
export default function ChangePasswordPage() {
  useDocumentTitle('Şifre değiştir')
  const { session } = useAuth()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const change = useChangePassword()

  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [repeat, setRepeat] = useState('')

  const mismatch = repeat.length > 0 && next !== repeat
  const forced = session?.mustChangePassword ?? false

  return (
    <div className="mx-auto flex max-w-lg flex-col gap-6">
      <header>
        <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">Şifre değiştir</h1>
        <p className="mt-1 text-sm text-stone-500">
          {forced
            ? 'Şifreniz yönetici tarafından atandı. Devam etmek için kendi şifrenizi belirleyin.'
            : 'Şifreniz en az 10 karakter olmalı, harf ve rakam içermelidir.'}
        </p>
      </header>

      <Card className="p-5">
        {change.isSuccess ? (
          <div className="flex flex-col gap-4">
            <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
              Şifreniz güncellendi.
            </p>
            <Button
              onClick={() => {
                // Oturum tazelensin: "şifre değiştir" bayrağı sunucudan düştü,
                // arayüzdeki kilidi kaldıran şey bu tazeleme.
                void queryClient.invalidateQueries({ queryKey: ['session'] })
                void navigate('/pano')
              }}
            >
              Panoya git
            </Button>
          </div>
        ) : (
          <form
            className="flex flex-col gap-4"
            onSubmit={(event) => {
              event.preventDefault()
              if (mismatch) return
              change.mutate({ currentPassword: current, newPassword: next })
            }}
          >
            <Input
              label="Mevcut şifre"
              type="password"
              autoComplete="current-password"
              required
              value={current}
              onChange={(event) => setCurrent(event.target.value)}
            />
            <Input
              label="Yeni şifre"
              type="password"
              autoComplete="new-password"
              required
              minLength={10}
              value={next}
              onChange={(event) => setNext(event.target.value)}
            />
            <Input
              label="Yeni şifre (yeniden)"
              type="password"
              autoComplete="new-password"
              required
              value={repeat}
              onChange={(event) => setRepeat(event.target.value)}
            />

            {mismatch ? <ErrorState message="İki şifre birbiriyle aynı değil." /> : null}
            {change.error ? <ErrorState message={change.error.message} error={change.error} /> : null}

            <Button
              type="submit"
              disabled={change.isPending || mismatch || next.length === 0}
              className="mt-2"
            >
              {change.isPending ? 'Kaydediliyor…' : 'Şifreyi güncelle'}
            </Button>
          </form>
        )}
      </Card>
    </div>
  )
}
