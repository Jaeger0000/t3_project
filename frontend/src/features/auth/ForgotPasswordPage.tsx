import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Button, ErrorState, Input } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import AuthLayout from './AuthLayout'
import { useRequestPasswordReset } from './queries'

/**
 * Şifre sıfırlama talebi.
 *
 * Ekran, adresin kayıtlı olup olmadığını <b>söylemiyor</b>: yanıt her durumda
 * aynı. Sunucu da aynı gövdeyi dönüyor; buradaki metin o sözleşmenin arayüz
 * tarafı, "bulunamadı" göstermek numaralandırma kapısı açardı.
 */
export default function ForgotPasswordPage() {
  useDocumentTitle('Şifremi unuttum')
  const [email, setEmail] = useState('')
  const request = useRequestPasswordReset()

  return (
    <AuthLayout
      title="Şifre sıfırlama"
      description="Kayıtlı e-posta adresinizi girin, sıfırlama bağlantısı gönderelim."
    >
      {request.isSuccess ? (
        <div className="flex flex-col gap-4">
          <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
            {request.data.message}
          </p>
          <p className="text-xs text-stone-500">
            Bağlantı iki saat geçerlidir ve yalnızca bir kez kullanılabilir.
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
            request.mutate({ email })
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

          {request.error ? <ErrorState message={request.error.message} /> : null}

          <Button type="submit" disabled={request.isPending} className="mt-2 w-full">
            {request.isPending ? 'Gönderiliyor…' : 'Sıfırlama bağlantısı gönder'}
          </Button>

          <Link
            to="/giris"
            className="text-center text-sm text-brand-700 hover:underline dark:text-brand-200"
          >
            Giriş ekranına dön
          </Link>
        </form>
      )}
    </AuthLayout>
  )
}
