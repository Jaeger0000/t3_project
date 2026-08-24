import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { Card } from '@/components/ui'

/**
 * KVKK metinlerinin ortak çerçevesi.
 *
 * Taslak uyarısı görünür şekilde duruyor: metnin hukuki içeriği ekip dışından
 * onay ister ve onaylanmamış bir aydınlatma metnini "onaylanmış" gibi göstermek
 * yükümlülüğü karşılamaz, yalnızca karşılanmış gibi gösterir.
 */
export default function LegalDocument({
  title,
  updatedOn,
  children,
}: {
  title: string
  updatedOn: string
  children: ReactNode
}) {
  return (
    <article className="flex flex-col gap-4">
      <header>
        <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">{title}</h1>
        <p className="mt-1 text-sm text-stone-500">Son güncelleme: {updatedOn}</p>
      </header>

      <p className="rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800 dark:bg-amber-950 dark:text-amber-200">
        <strong>Taslak.</strong> Bu metnin hukuki içeriği T3 Vakfı'nın onayından
        geçmedi; Creathon prototipinde şeffaflık amacıyla yayımlanıyor.
      </p>

      <Card className="flex flex-col gap-4 p-5 text-sm leading-relaxed text-stone-700 dark:text-stone-300">
        {children}
      </Card>

      <p className="text-xs text-stone-500">
        <Link to="/giris" className="text-brand-700 hover:underline dark:text-brand-200">
          Giriş ekranına dön
        </Link>
      </p>
    </article>
  )
}

/** Belge içindeki başlık — metin bloklarının okunabilirliği için. */
export function LegalSection({ heading, children }: { heading: string; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-2">
      <h2 className="font-semibold text-stone-900 dark:text-stone-100">{heading}</h2>
      {children}
    </section>
  )
}
