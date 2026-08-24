import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Button, ErrorState, Input } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import AuthLayout from './AuthLayout'
import { useResetPassword } from './queries'

/**
 * Sıfırlama bağlantısını harcayıp yeni şifreyi belirler. Jeton adresten
 * geliyor; ekranda hiç gösterilmiyor (kopyalanan ekran görüntüsü bir sırrı
 * taşımasın).
 */
export default function ResetPasswordPage() {
  useDocumentTitle('Yeni şifre belirle')
  const { token } = useParams<{ token: string }>()
  const [password, setPassword] = useState('')
  const [repeat, setRepeat] = useState('')
  const reset = useResetPassword()

  const mismatch = repeat.length > 0 && password !== repeat

  return (
    <AuthLayout
      title="Yeni şifre belirle"
      description="Şifreniz en az 10 karakter olmalı, harf ve rakam içermelidir."
    >
      {reset.isSuccess ? (
        <div className="flex flex-col gap-4">
          <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
            Şifreniz güncellendi. Yeni şifrenizle giriş yapabilirsiniz.
          </p>
          <Link
            to="/giris"
            className="text-center text-sm text-brand-700 hover:underline dark:text-brand-200"
          >
            Giriş ekranına dön
          </Link>
        </div>
      ) : (
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault()
            if (mismatch || !token) return
            reset.mutate({ token, newPassword: password })
          }}
        >
          <Input
            label="Yeni şifre"
            type="password"
            autoComplete="new-password"
            required
            minLength={10}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
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
          {reset.error ? <ErrorState message={reset.error.message} /> : null}

          <Button
            type="submit"
            disabled={reset.isPending || mismatch || password.length === 0}
            className="mt-2 w-full"
          >
            {reset.isPending ? 'Kaydediliyor…' : 'Şifreyi güncelle'}
          </Button>

          <Link
            to="/sifremi-unuttum"
            className="text-center text-sm text-brand-700 hover:underline dark:text-brand-200"
          >
            Yeni bağlantı iste
          </Link>
        </form>
      )}
    </AuthLayout>
  )
}
