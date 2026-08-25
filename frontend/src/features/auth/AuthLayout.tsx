import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { Card } from '@/components/ui'
import { LogoMark } from '@/components/Logo'

/**
 * Oturum açmadan görülen ekranların ortak çerçevesi (giriş, şifre kurtarma).
 * KVKK bağlantıları buradan da erişilebilir: aydınlatma yükümlülüğü veri
 * girilmeden önce başlıyor.
 */
export default function AuthLayout({
  title,
  description,
  children,
}: {
  title: string
  description: string
  children: ReactNode
}) {
  return (
    <div className="flex min-h-screen items-center justify-center px-6 py-12">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <LogoMark
            className="mx-auto mb-4 h-14 w-[80px]"
            label="Türkiye Teknoloji Takımı Vakfı Girişim Merkezi"
          />
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">{title}</h1>
          <p className="mt-2 text-sm text-stone-500">{description}</p>
        </div>

        <Card className="p-6">{children}</Card>

        <p className="mt-6 text-center text-xs text-stone-500">
          T3 Vakfı Bursiyer Yapay Zekâ Creathonu · Problem 7
        </p>
        <p className="mt-2 flex flex-wrap justify-center gap-x-3 gap-y-1 text-center text-xs">
          <Link to="/kvkk-aydinlatma" className="text-brand-700 hover:underline dark:text-brand-200">
            KVKK aydınlatma metni
          </Link>
          <span className="text-stone-400">·</span>
          <Link to="/kullanim-sartlari" className="text-brand-700 hover:underline dark:text-brand-200">
            Kullanım şartları
          </Link>
          <span className="text-stone-400">·</span>
          <Link to="/marka" className="text-brand-700 hover:underline dark:text-brand-200">
            Logo paketi
          </Link>
        </p>
      </div>
    </div>
  )
}
