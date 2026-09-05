import type { ReactNode } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { LogoLockup, LogoMark } from '@/components/Logo'
import { useAuth, oturumIzi } from '@/lib/auth'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

/**
 * Açılış sayfası — siteye ilk gelen kişinin gördüğü ekran.
 *
 * Neden var: kök adres doğrudan panoya yönleniyordu, oturumu olmayan ziyaretçi
 * de bir anda giriş formuyla karşılaşıyordu. Bağlantıyı ilk kez açan jüri
 * üyesinin sistemin ne yaptığını formu doldurmadan önce okuyabilmesi gerekiyor.
 *
 * Sayfa bilinçli olarak veri çekmiyor: API kapalıyken bile açılır, tek dış
 * bağımlılığı logo dosyaları. Tanıtım metni istatistik uydurmuyor — ekrandaki
 * her sayı MVP maddesi sayısı gibi sabit, doğrulanabilir bir şey.
 */
export default function LandingPage() {
  useDocumentTitle('Girişim Ekosistemi Yönetim Sistemi')
  const { session } = useAuth()

  /*
   * Oturumu olan kullanıcı kök adreste tanıtım sayfasını görmemeli, panoya
   * gitmeli. Karar /api/me cevabı beklenmeden veriliyor: yerel iz "bu tarayıcıda
   * oturum açılmıştı" diyorsa doğrudan yönlendiriyoruz, demiyorsa tanıtımı
   * gösteriyoruz. Beklemek, siteye ilk gelen herkese gereksiz bir yükleniyor
   * çarkı izletirdi; iz yanılırsa da zararı yok — /pano kendi korumasıyla
   * kullanıcıyı girişe yolluyor.
   */
  if (session || oturumIzi.exists()) return <Navigate to="/pano" replace />

  return (
    <div className="min-h-screen bg-white dark:bg-stone-950">
      <header className="mx-auto flex max-w-5xl items-center justify-between gap-4 px-6 py-5">
        <LogoLockup compact />
        <div className="flex items-center gap-3">
          <Link
            to="/kayit-ol"
            className="inline-flex items-center rounded-lg border border-stone-300 px-4 py-2 text-sm font-medium text-stone-700 transition-colors hover:border-brand-500 hover:bg-brand-500 hover:text-white dark:border-stone-700 dark:text-stone-200"
          >
            Girişim misiniz? Kayıt olun
          </Link>
          <Link
            to="/giris"
            className="inline-flex items-center rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-600 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
          >
            Giriş yap
          </Link>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-6 pb-16">
        {/* Tanıtım başlığı: sistemin ne yaptığı tek cümlede, süslemeden. */}
        <section className="border-b border-stone-200 py-14 dark:border-stone-800 sm:py-20">
          <p className="text-sm font-semibold tracking-wide text-brand-700 uppercase dark:text-brand-300">
            T3 Girişim Merkezi
          </p>
          <h1 className="mt-3 max-w-3xl text-4xl leading-tight font-bold text-stone-900 sm:text-5xl dark:text-stone-50">
            Girişim ekosisteminin{' '}
            <span className="text-brand-600 dark:text-brand-400">tek kurumsal hafızası</span>
          </h1>
          <p className="mt-5 max-w-2xl text-lg leading-relaxed text-stone-600 dark:text-stone-300">
            Profil, program geçmişi, satış, yatırım, ekip, başarı ve dokümanlar
            dağınık dosyalarda değil tek girişim kartında. Kim neyi görebilir,
            kim neyi değiştirebilir — rol bazlı ve kayıt altında.
          </p>
          <div className="mt-8 flex flex-wrap items-center gap-3">
            <Link
              to="/giris"
              className="inline-flex items-center rounded-lg bg-brand-500 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-brand-600 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
            >
              Sisteme giriş yap
            </Link>
            <Link
              to="/kayit-ol"
              className="inline-flex items-center rounded-lg border border-stone-300 px-5 py-2.5 text-sm font-medium text-stone-700 transition-colors hover:border-brand-500 hover:bg-brand-500 hover:text-white dark:border-stone-700 dark:text-stone-200"
            >
              Girişim misiniz? Kayıt olun
            </Link>
            <a
              href="#neler-var"
              className="inline-flex items-center rounded-lg px-5 py-2.5 text-sm font-medium text-stone-500 transition-colors hover:text-brand-700 dark:text-stone-400 dark:hover:text-brand-300"
            >
              Neler yapıyor?
            </a>
          </div>
        </section>

        {/* Dört blok = şartnamedeki zorunlu MVP maddeleri. Sıra da oradan. */}
        <section id="neler-var" className="scroll-mt-6 py-14">
          <h2 className="text-2xl font-bold text-stone-900 dark:text-stone-50">
            Dört blokta ekosistem yönetimi
          </h2>
          <div className="mt-8 grid gap-5 sm:grid-cols-2">
            <Ozellik
              sira="01"
              baslik="Merkezi girişim kartı"
              metin="Genel bilgiler, teknoloji alanı, ekip, ürün ve temel göstergeler tek profilde toplanır."
            />
            <Ozellik
              sira="02"
              baslik="Program geçmişi ve yolculuk"
              metin="Take Off, Ön Kuluçka, TEKNOFEST — katılınan programlar, dönemler ve gelişim adımları kronolojik akışta."
            />
            <Ozellik
              sira="03"
              baslik="Girişim portalı + onay akışı"
              metin="Girişim kendi verisini günceller; değişiklik doğrudan yayınlanmaz, yönetici onayından geçer."
            />
            <Ozellik
              sira="04"
              baslik="Satış, yatırım, başarı, doküman"
              metin="Ciro, ihracat, yatırım turları, hibeler, ödüller ve belgeler serbest metin değil alan bazlı veriyle saklanır."
            />
          </div>
        </section>

        {/* Onay akışı sistemin can damarı: üç adımda anlatılıyor çünkü demoda
            jüriye gösterilen ana senaryo bu. */}
        <section className="border-t border-stone-200 py-14 dark:border-stone-800">
          <h2 className="text-2xl font-bold text-stone-900 dark:text-stone-50">
            Veri nasıl yayına giriyor?
          </h2>
          <ol className="mt-8 grid gap-6 sm:grid-cols-3">
            <Adim
              no={1}
              baslik="Girişim gönderir"
              metin="Girişim kullanıcısı hiçbir tabloya doğrudan yazmaz; portalda yaptığı her düzenleme bir değişiklik önerisi olur."
            />
            <Adim
              no={2}
              baslik="Yönetici inceler"
              metin="Program yöneticisi eski ve yeni değeri yan yana görür, gerekçesiyle onaylar ya da reddeder."
            />
            <Adim
              no={3}
              baslik="Ekosistem raporlanır"
              metin="Onaylanan veri panoya, filtrelere ve karar verici ekranlarına anında yansır; her adım denetim izinde kalır."
            />
          </ol>
        </section>

        {/* Roller, yetki matrisinin kısa hâli. KVKK maskelemesi burada
            görünür bir vaat olarak duruyor. */}
        <section className="border-t border-stone-200 py-14 dark:border-stone-800">
          <h2 className="text-2xl font-bold text-stone-900 dark:text-stone-50">
            Herkes yalnızca kendi alanını görür
          </h2>
          <p className="mt-3 max-w-2xl text-stone-600 dark:text-stone-300">
            Hassas alanlar (iletişim bilgisi, ciro, yatırım tutarı) role göre
            maskelenir. Yetkisi olmayan kullanıcıya değer boş gitmez —
            <span className="font-medium text-stone-900 dark:text-stone-100">
              {' '}
              hiç gitmez.
            </span>
          </p>
          <dl className="mt-8 grid gap-4 sm:grid-cols-2">
            <Rol
              ad="Süper Admin"
              metin="Programları ve kullanıcıları tanımlar, tüm ekosistemi görür, onayları sonuçlandırır."
            />
            <Rol
              ad="Program Yöneticisi"
              metin="Yalnızca kendi programındaki girişimleri yönetir; başka programın verisine erişemez."
            />
            <Rol
              ad="Girişim Kullanıcısı"
              metin={
                <>
                  Yalnızca kendi girişiminin kartını görür ve güncelleme önerisi
                  gönderir.{' '}
                  <Link to="/kayit-ol" className="font-medium underline hover:text-brand-700 dark:hover:text-brand-300">
                    Henüz hesabınız yoksa kayıt olun.
                  </Link>
                </>
              }
            />
            <Rol
              ad="Karar Verici"
              metin="Ekosistem panosunu ve raporları okur; tutarlar yetkisi ölçüsünde maskelenir."
            />
          </dl>
        </section>
      </main>

      <footer className="border-t border-stone-200 dark:border-stone-800">
        <div className="mx-auto flex max-w-5xl flex-col gap-4 px-6 py-8 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3">
            <LogoMark className="h-8 w-[46px]" />
            <p className="text-xs text-stone-500">
              T3 Vakfı Bursiyer Yapay Zekâ Creathonu · Problem 7
              <br />
              Prototip — ekrandaki tüm veriler kurgudur.
            </p>
          </div>
          <nav className="flex flex-wrap gap-x-4 gap-y-1 text-xs">
            <Link to="/giris" className="text-brand-700 hover:underline dark:text-brand-300">
              Giriş
            </Link>
            <Link
              to="/kvkk-aydinlatma"
              className="text-brand-700 hover:underline dark:text-brand-300"
            >
              KVKK aydınlatma metni
            </Link>
            <Link
              to="/kullanim-sartlari"
              className="text-brand-700 hover:underline dark:text-brand-300"
            >
              Kullanım şartları
            </Link>
          </nav>
        </div>
      </footer>
    </div>
  )
}

function Ozellik({ sira, baslik, metin }: { sira: string; baslik: string; metin: string }) {
  return (
    <div className="rounded-xl border border-stone-200 bg-white p-6 transition-colors hover:border-brand-300 dark:border-stone-800 dark:bg-stone-900">
      <span className="text-sm font-bold text-brand-600 dark:text-brand-400">{sira}</span>
      <h3 className="mt-2 font-semibold text-stone-900 dark:text-stone-50">{baslik}</h3>
      <p className="mt-2 text-sm leading-relaxed text-stone-600 dark:text-stone-300">{metin}</p>
    </div>
  )
}

function Adim({ no, baslik, metin }: { no: number; baslik: string; metin: string }) {
  return (
    <li className="flex flex-col gap-2">
      <span className="flex size-8 items-center justify-center rounded-full bg-brand-500 text-sm font-bold text-white">
        {no}
      </span>
      <h3 className="font-semibold text-stone-900 dark:text-stone-50">{baslik}</h3>
      <p className="text-sm leading-relaxed text-stone-600 dark:text-stone-300">{metin}</p>
    </li>
  )
}

function Rol({ ad, metin }: { ad: string; metin: ReactNode }) {
  return (
    <div className="border-l-2 border-brand-500 pl-4">
      <dt className="font-semibold text-stone-900 dark:text-stone-50">{ad}</dt>
      <dd className="mt-1 text-sm leading-relaxed text-stone-600 dark:text-stone-300">{metin}</dd>
    </div>
  )
}
