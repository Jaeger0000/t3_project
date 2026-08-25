import { useState } from 'react'
import { Link, Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { Button, ErrorState, Input } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import AuthLayout from './AuthLayout'
import { KVKK_ONAY_SURUMU, kvkkOnayiniOku, kvkkOnayiniYaz } from './kvkkConsent'

export default function LoginPage() {
  useDocumentTitle('Giriş')
  const { session, login, loginError, isLoggingIn, signedOutReason } = useAuth()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  // Onay daha önce verilmişse kutu işaretli açılıyor: aydınlatma metni
  // değişmedikçe (sürüm aynı kaldıkça) aynı kişiye her girişte sormak
  // bilgilendirme değil sürtünme olur.
  const [kayitliOnay] = useState(kvkkOnayiniOku)
  const [onay, setOnay] = useState(() => kayitliOnay !== null)

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
          if (!onay) return

          // Onay girişten önce yazılıyor: istek başarısız olsa da kullanıcı
          // metni okuduğunu bildirmişti, kutuyu tekrar aramamalı.
          kvkkOnayiniYaz()
          void login(email, password, KVKK_ONAY_SURUMU)
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

        {/* KVKK aydınlatma yükümlülüğü veri girilmeden önce başlıyor: metin
            bağlantıları burada, formun içinde. Onay kutusu girişi kilitliyor —
            "giriş yaparak kabul etmiş olursunuz" kalıbı açık rıza sayılmaz. */}
        <div className="rounded-lg border border-stone-200 bg-stone-50 p-3 dark:border-stone-800 dark:bg-stone-900/60">
          <label className="flex items-start gap-2.5 text-sm text-stone-700 dark:text-stone-300">
            <input
              type="checkbox"
              checked={onay}
              onChange={(event) => setOnay(event.target.checked)}
              className="mt-0.5 size-4 shrink-0 rounded border-stone-300 text-brand-600 focus:ring-2 focus:ring-brand-500/60 dark:border-stone-600"
              data-testid="kvkk-onay"
            />
            <span>
              <Link
                to="/kvkk-aydinlatma"
                className="font-medium text-brand-700 hover:underline dark:text-brand-200"
              >
                KVKK aydınlatma metnini
              </Link>{' '}
              ve{' '}
              <Link
                to="/kullanim-sartlari"
                className="font-medium text-brand-700 hover:underline dark:text-brand-200"
              >
                kullanım şartlarını
              </Link>{' '}
              okudum; kişisel verilerimin metinde açıklanan amaç ve sürelerle
              işlenmesini kabul ediyorum.
            </span>
          </label>

          {kayitliOnay ? (
            <p className="mt-2 pl-6 text-xs text-stone-500">
              Bu tarayıcıda onay verilmişti:{' '}
              {new Date(kayitliOnay.tarih).toLocaleString('tr-TR')} · metin sürümü{' '}
              {kayitliOnay.surum}
            </p>
          ) : null}
        </div>

        {loginError ? <ErrorState message={loginError} /> : null}

        <Button type="submit" disabled={isLoggingIn || !onay} className="mt-2 w-full">
          {isLoggingIn ? 'Giriş yapılıyor…' : 'Giriş yap'}
        </Button>

        {/* Düğme neden kapalı: devre dışı bir düğme gerekçesiz kalırsa
            kullanıcı formu bozuk sanıyor. */}
        {!onay ? (
          <p className="text-center text-xs text-stone-500">
            Giriş için aydınlatma metnini onaylamanız gerekiyor.
          </p>
        ) : null}

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
