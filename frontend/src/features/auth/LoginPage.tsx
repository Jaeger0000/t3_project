import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { Button, Card, ErrorState, Input } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

export default function LoginPage() {
  useDocumentTitle('Giriş')
  const { session, login, loginError, isLoggingIn } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  if (session) return <Navigate to="/girisimler" replace />

  return (
    <div className="flex min-h-screen items-center justify-center px-6 py-12">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <span className="mx-auto mb-4 flex size-12 items-center justify-center rounded-xl bg-brand-500 font-bold text-white">
            T3
          </span>
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">
            Girişim Ekosistemi Yönetim Sistemi
          </h1>
          <p className="mt-2 text-sm text-stone-500">
            Programdan yatırıma, ekosistemin tek kurumsal hafızası.
          </p>
        </div>

        <Card className="p-6">
          <form
            className="flex flex-col gap-4"
            onSubmit={(event) => {
              event.preventDefault()
              void login(email, password)
            }}
          >
            <Input
              label="E-posta"
              type="email"
              autoComplete="username"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="ad.soyad@t3ekosistem.test"
            />
            <Input
              label="Şifre"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />

            {loginError ? <ErrorState message={loginError} /> : null}

            <Button type="submit" disabled={isLoggingIn} className="mt-2 w-full">
              {isLoggingIn ? 'Giriş yapılıyor…' : 'Giriş yap'}
            </Button>
          </form>
        </Card>

        <p className="mt-6 text-center text-xs text-stone-500">
          T3 Vakfı Bursiyer Yapay Zekâ Creathonu · Problem 7
        </p>
      </div>
    </div>
  )
}
