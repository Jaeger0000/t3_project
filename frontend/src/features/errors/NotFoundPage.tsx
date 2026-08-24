import { Link, useLocation } from 'react-router-dom'
import { Card } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

/**
 * Bilinmeyen adres artık sessizce panoya yönlendirilmiyor. Yönlendirme,
 * paylaşılan bir bağlantıdaki yazım hatasını "pano zaten burası" gibi
 * gösteriyordu; kullanıcı yanlış adrese gittiğini hiç öğrenmiyordu.
 */
export default function NotFoundPage() {
  const location = useLocation()
  useDocumentTitle('Sayfa bulunamadı')

  return (
    <Card className="mx-auto flex max-w-xl flex-col gap-4 px-6 py-8 text-center">
      <p className="text-4xl font-bold text-brand-500">404</p>
      <h1 className="text-xl font-semibold text-stone-900 dark:text-stone-50">
        Aradığınız sayfa bulunamadı
      </h1>
      <p className="text-sm text-stone-500">
        Adres yanlış yazılmış ya da bağlantı artık geçerli olmayabilir. Aşağıdaki
        ekranlardan devam edebilirsiniz.
      </p>
      <p className="rounded-lg bg-stone-100 px-3 py-2 font-mono text-xs break-all text-stone-600 dark:bg-stone-800 dark:text-stone-300">
        {location.pathname}
      </p>
      <div className="flex flex-wrap justify-center gap-4 text-sm">
        <Link to="/pano" className="text-brand-700 hover:underline dark:text-brand-300">
          Panoya dön
        </Link>
        <Link to="/girisimler" className="text-brand-700 hover:underline dark:text-brand-300">
          Girişimler
        </Link>
      </div>
    </Card>
  )
}
