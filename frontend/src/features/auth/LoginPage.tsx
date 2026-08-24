import { useState } from 'react'
import { Link, Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { Button, ErrorState, Input } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import AuthLayout from './AuthLayout'

export default function LoginPage() {
  useDocumentTitle('Giriş')
  const { session, login, loginError, isLoggingIn, signedOutReason } = useAuth()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  // Giriş sonrası kullanıcı gitmek istediği ekrana döner; korunan bir bağlantıyı
  // paylaşan kişi girişten sonra panoda değil o sayfada olmalı.
  const target = (location.state as { from?: string } | null)?.from ?? '/girisimler'

  if (session) return <Navigate to={target} replace />

  return (
    <AuthLayout
      title="Girişim Ekosistemi Yönetim Sistemi"
      description="Programdan yatırıma, ekosistemin tek kurumsal hafızası."
    >
      <form
        className="flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault()
          void login(email, password)
        }}
      >
        {/* Sessiz atılma yerine gerekçe: jeton dolduğunda kullanıcı ne olduğunu
            okumalı. */}
        {signedOutReason ? (
          <p className="rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800 dark:bg-amber-950 dark:text-amber-200">
            {signedOutReason}
          </p>
        ) : null}

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

        <Link
          to="/sifremi-unuttum"
          className="text-center text-sm text-brand-700 hover:underline dark:text-brand-200"
        >
          Şifremi unuttum
        </Link>
      </form>
    </AuthLayout>
  )
}
