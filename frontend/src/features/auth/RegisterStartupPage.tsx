import { useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import type { Sector } from '@/api/types'
import { useAuth } from '@/lib/auth'
import { sectorLabels } from '@/lib/labels'
import { Button, ErrorState, Input, Select } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import AuthLayout from './AuthLayout'
import { KVKK_ONAY_SURUMU, kvkkOnayiniOku, kvkkOnayiniYaz } from './kvkkConsent'
import { useRegisterStartup } from './queries'

const sectors = Object.entries(sectorLabels) as [Sector, string][]

/**
 * Ana sayfadan erişilen "Kayıt Ol" formu — girişimin kendi kendine başvurusu.
 *
 * Onaydan geçmeden giriş yapılamıyor: hesap SuperAdmin onayına kadar hiçbir
 * tabloya yazılmıyor (bkz. RegisterStartupHandler). Bu yüzden başarı ekranı
 * LoginPage'e değil "başvurunuz alındı" mesajına düşüyor — otomatik giriş
 * kullanıcıya sistemin hemen açıldığı yanlış izlenimini verirdi.
 */
export default function RegisterStartupPage() {
  useDocumentTitle('Kayıt ol')
  const { session } = useAuth()
  const register = useRegisterStartup()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fullName, setFullName] = useState('')
  const [startupName, setStartupName] = useState('')
  const [sector, setSector] = useState<Sector>('Software')

  const [kayitliOnay] = useState(kvkkOnayiniOku)
  const [onay, setOnay] = useState(() => kayitliOnay !== null)

  if (session) return <Navigate to="/pano" replace />

  if (register.isSuccess) {
    return (
      <AuthLayout
        title="Başvurunuz alındı"
        description="Girişiminiz Süper Yönetici onayından sonra ekosisteme katılır."
      >
        <div className="flex flex-col gap-4">
          <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">
            Başvurunuz alındı. Süper Yönetici onayladıktan sonra bu e-posta ve
            şifreyle giriş yapabilirsiniz.
          </p>
          <p className="text-xs text-stone-500">
            Onay süreci sırasında ek bilgi gerekirse başvuru sırasında verdiğiniz
            e-posta adresinden sizinle iletişime geçilir.
          </p>
          <Link
            to="/giris"
            className="text-center text-sm text-brand-700 hover:underline dark:text-brand-200"
          >
            Giriş ekranına dön
          </Link>
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout
      title="Girişim kaydı"
      description="Girişiminizi ekosisteme kaydedin; başvurunuz Süper Yönetici onayından sonra etkinleşir."
    >
      <form
        className="flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault()
          if (!onay) return

          // Onay girişten önce yazılıyor: istek başarısız olsa da kullanıcı
          // metni okuduğunu bildirmişti, kutuyu tekrar aramamalı.
          kvkkOnayiniYaz()
          register.mutate({
            email,
            password,
            fullName,
            startupName,
            sector,
            city: null,
            contactPhone: null,
            kvkkConsentVersion: KVKK_ONAY_SURUMU,
          })
        }}
      >
        <Input
          label="Ad soyad"
          required
          value={fullName}
          onChange={(event) => setFullName(event.target.value)}
        />
        <Input
          label="E-posta"
          type="email"
          autoComplete="username"
          required
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          placeholder="ad.soyad@girisiminiz.test"
        />
        <Input
          label="Şifre"
          type="password"
          autoComplete="new-password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          placeholder="En az 10 karakter, harf ve rakam"
        />
        <Input
          label="Girişim adı"
          required
          value={startupName}
          onChange={(event) => setStartupName(event.target.value)}
        />
        <Select
          label="Sektör"
          required
          value={sector}
          onChange={(event) => setSector(event.target.value as Sector)}
        >
          {sectors.map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </Select>

        {/* KVKK aydınlatma yükümlülüğü veri girilmeden önce başlıyor: metin
            bağlantıları burada, formun içinde. Onay kutusu girişi kilitliyor —
            "kayıt olarak kabul etmiş olursunuz" kalıbı açık rıza sayılmaz. */}
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

        {register.error ? (
          <ErrorState message={register.error.message} error={register.error} />
        ) : null}

        <Button type="submit" disabled={register.isPending || !onay} className="mt-2 w-full">
          {register.isPending ? 'Gönderiliyor…' : 'Başvuruyu gönder'}
        </Button>

        {!onay ? (
          <p className="text-center text-xs text-stone-500">
            Başvuru için aydınlatma metnini onaylamanız gerekiyor.
          </p>
        ) : null}

        <Link
          to="/giris"
          className="text-center text-sm text-brand-700 hover:underline dark:text-brand-200"
        >
          Zaten hesabınız var mı? Giriş yapın
        </Link>
      </form>
    </AuthLayout>
  )
}
