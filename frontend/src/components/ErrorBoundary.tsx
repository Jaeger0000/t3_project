import { Component } from 'react'
import type { ErrorInfo, ReactNode } from 'react'

type Props = { children: ReactNode }
type State = { error: Error | null }

/**
 * Beklenmeyen render hatasında beyaz ekran yerine Türkçe bir açıklama.
 *
 * Hata izleme (Sentry vb.) buraya **takılacak yerden** ibaret: dış servis
 * eklemek bir hesap, bir DSN ve KVKK tarafında bir aktarım kararı gerektiriyor
 * — üçü de bu depoda kararlaştırılamaz. `report` bu yüzden yalnızca konsola
 * yazıyor ve sağlayıcı geldiğinde değişecek tek yer burası.
 */
export default class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Kişisel veri taşımamak için yalnızca hata metni ve bileşen yığını:
    // ekrandaki veriyi (girişim adı, iletişim bilgisi) hiçbir koşulda dışarıya
    // taşımıyoruz.
    console.error('[T3] Beklenmeyen arayüz hatası', error.message, info.componentStack)
  }

  render() {
    if (!this.state.error) return this.props.children

    return (
      <div className="mx-auto flex min-h-screen max-w-lg flex-col justify-center gap-4 px-6">
        <div>
          <h1 className="text-xl font-bold text-stone-900 dark:text-stone-50">
            Beklenmeyen bir hata oluştu
          </h1>
          <p className="mt-2 text-sm text-stone-600 dark:text-stone-300">
            Ekran yüklenirken bir sorun çıktı. Sayfayı yenilemek genellikle
            yeterli oluyor; sorun sürerse aşağıdaki adresten bize yazın.
          </p>
        </div>

        {/* Teknik metin gizlenmiyor: destek isteyen kullanıcı hatayı
            kopyalayabilmeli. Yığın izi değil, yalnızca mesaj. */}
        <p className="rounded-lg bg-stone-100 px-3 py-2 font-mono text-xs text-stone-600 dark:bg-stone-900 dark:text-stone-300">
          {this.state.error.message}
        </p>

        <div className="flex flex-wrap items-center gap-3">
          <button
            type="button"
            onClick={() => window.location.reload()}
            className="inline-flex items-center rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-white hover:bg-brand-600 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
          >
            Sayfayı yenile
          </button>
          <a
            href="mailto:kvkk@t3vakfi.org.tr"
            className="text-sm text-brand-700 hover:underline dark:text-brand-200"
          >
            Destek
          </a>
        </div>
      </div>
    )
  }
}
