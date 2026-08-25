import { useEffect, useRef, useState } from 'react'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

/**
 * Logo paketi — kurumsal iletişim sayfası.
 *
 * Kaynak: `design_handoff_logo_paketi/` (README + preview.html). Handoff
 * "high-fidelity" diyor: renkler, tipografi, boşluklar ve etkileşimler nihai.
 * Prototip HTML'i kopyalanmadı, sayfa bu projenin kendi desenleriyle (Tailwind
 * yardımcıları, Türkçe yorum, `useDocumentTitle`) yeniden kuruldu — handoff da
 * bunu istiyor.
 *
 * İki bilinçli sapma:
 *
 * 1. **Koyu tema.** Handoff tek bir açık zemin (#f2f2f3) veriyor; arayüz ise
 *    tema duyarlı. Sayfanın kendi kabuğu (zemin, yazı, saç çizgileri) koyu
 *    temada karşılığına geçiyor, ama **marka değerleri hiç kaymıyor**: swatch
 *    renkleri, degradeler ve logo dosyaları iki temada da birebir aynı. Marka
 *    kartında gösterilen rengin temaya göre değişmesi, sayfanın kendi amacını
 *    yok ederdi.
 * 2. **Yazı tipleri kendi sunucumuzda** (bkz. public/fonts/barlow.css): CSP
 *    `font-src 'self'` ve Google Fonts için o sınırı gevşetmek gerekirdi.
 */

type Renk = {
  ad: string
  rol: string
  hex: string
  rgb: string
  cmyk: string
  pantone: string
}

const RENKLER: Renk[] = [
  { ad: 'Antrasit', rol: 'Ana kurumsal ton, tipografi', hex: '#303C48', rgb: '48 60 72', cmyk: '33 17 0 72', pantone: '7545 C' },
  { ad: 'Kırmızı', rol: 'Vurgu, uyarı ve enerji', hex: '#E43C24', rgb: '228 60 36', cmyk: '0 74 84 11', pantone: '179 C' },
  { ad: 'Turuncu', rol: 'Vurgu, girişim ve hareket', hex: '#F99B1C', rgb: '249 155 28', cmyk: '0 38 89 2', pantone: '1375 C' },
  { ad: 'Mavi', rol: 'Kurumsal derinlik, veri', hex: '#0C4878', rgb: '12 72 120', cmyk: '90 40 0 53', pantone: '301 C' },
]

const DEGRADELER = [
  { etiket: '#E43C24 → #B32A1C', css: 'linear-gradient(135deg,#E43C24,#B32A1C)' },
  { etiket: '#303C48 → #1E2833', css: 'linear-gradient(135deg,#303C48,#1E2833)' },
  { etiket: '#F9A81C → #F08A14', css: 'linear-gradient(135deg,#F9A81C,#F08A14)' },
  { etiket: '#1878A8 → #0C4878', css: 'linear-gradient(135deg,#1878A8,#0C4878)' },
]

const ZEMINLER = [
  {
    sekme: 'Açık zemin',
    zemin: '#ffffff',
    logo: '/marka/tgm-logo-trans.png',
    aciklama:
      'Birincil kullanım. İşaret beyaz veya çok açık nötr zeminde, kendi renkleriyle yer alır.',
    kurallar: [
      ['Zemin', 'Beyaz ya da %0–8 gri; renkli açık zeminlerde kullanılmaz.'],
      ['Kontrast', 'Zemin ile işaret arasında en az 4,5:1 kontrast korunur.'],
      ['Dosya', 'tgm-logo-renkli.svg'],
    ],
  },
  {
    sekme: 'Koyu zemin',
    zemin: '#1E2833',
    logo: '/marka/tgm-logo-trans.png',
    aciklama:
      'Koyu zeminde işaret renkleri korunur; antrasit blok zeminde kaybolursa tek renk beyaz versiyon kullanılır.',
    kurallar: [
      ['Zemin', 'Antrasit #303C48 veya daha koyu tonlar.'],
      ['Uyarı', 'Kırmızı ve lacivert zeminlerde yalnızca beyaz versiyon.'],
      ['Dosya', 'tgm-logo-beyaz.svg'],
    ],
  },
  {
    sekme: 'Fotoğraf üzerinde',
    zemin: '',
    logo: '/marka/tgm-logo-beyaz.png',
    aciklama:
      'Fotoğraf üzerinde yalnızca tek renk beyaz versiyon kullanılır. Görsel, işaretin oturduğu alanda sakin ve koyu olmalıdır.',
    kurallar: [
      ['Katman', 'Gerekirse %40–50 koyu örtü eklenir.'],
      ['Alan', 'İşaret, detaylı veya yüksek kontrastlı bölgeye yerleştirilmez.'],
      ['Dosya', 'tgm-logo-beyaz.svg'],
    ],
  },
]

const YANLIS_KULLANIM = [
  {
    zemin: '#F99B1C',
    filtre: '',
    metin: 'Doygun kurumsal renk zeminde renkli işaret kullanmayın.',
  },
  {
    zemin: '#ffffff',
    filtre: 'hue-rotate(140deg) saturate(1.4)',
    metin: 'İşaretin renklerini değiştirmeyin veya yeniden renklendirmeyin.',
  },
  {
    zemin: '#9AA3AB',
    filtre: '',
    opaklik: true,
    metin: 'Şeffaflık, gölge veya karışım efekti uygulamayın.',
  },
]

/**
 * Blueprint çerçevesi: kare köşe, saç çizgisi ve dört registration işareti.
 * Handoff'un "wireframe grameri" bu ve işaretlerin kaldırılmaması isteniyor —
 * paketin tamamını bir arada tutan görsel imza.
 */
function Blueprint({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={`relative border border-[#d4d4d7] dark:border-stone-700 ${className}`}>
      {(['tl', 'tr', 'bl', 'br'] as const).map((kose) => (
        <i
          key={kose}
          aria-hidden="true"
          className={[
            'pointer-events-none absolute size-[11px] text-[#1d1f20]/55 dark:text-stone-400/60',
            'before:absolute before:left-[5px] before:top-0 before:h-full before:w-px before:bg-current',
            'after:absolute after:left-0 after:top-[5px] after:h-px after:w-full after:bg-current',
            kose === 'tl' ? '-left-1.5 -top-1.5' : '',
            kose === 'tr' ? '-right-1.5 -top-1.5' : '',
            kose === 'bl' ? '-bottom-1.5 -left-1.5' : '',
            kose === 'br' ? '-bottom-1.5 -right-1.5' : '',
          ].join(' ')}
        />
      ))}
      {children}
    </div>
  )
}

/** Bölüm başlığı satırı: solda numaralı başlık, sağda duruma bağlı ipucu. */
function BolumBasligi({ baslik, ipucu }: { baslik: string; ipucu?: React.ReactNode }) {
  return (
    <div className="flex flex-wrap items-end justify-between gap-3 border-b border-[#d4d4d7] pb-[10px] dark:border-stone-700">
      <h2 className="font-marka-baslik text-[26px] font-semibold uppercase leading-none tracking-[0.05em]">
        {baslik}
      </h2>
      {ipucu ? <p className="text-[13px] text-[#5d5d60] dark:text-stone-400">{ipucu}</p> : null}
    </div>
  )
}

export default function BrandKitPage() {
  useDocumentTitle('Logo paketi')

  const [mod, setMod] = useState(0)
  const [kopyalanan, setKopyalanan] = useState<string | null>(null)
  const sayacRef = useRef<number | null>(null)

  // Zamanlayıcı sökülürken temizleniyor: sayfadan çıkıp geri gelen kullanıcıya
  // eski bir "kopyalandı" bildirimi düşmesin.
  useEffect(() => () => {
    if (sayacRef.current) window.clearTimeout(sayacRef.current)
  }, [])

  async function kopyala(hex: string) {
    try {
      await navigator.clipboard?.writeText(hex)
    } catch {
      // Pano API'si yoksa ya da izin verilmediyse sessizce geçiyoruz: görsel
      // geri bildirim yine gösteriliyor, kullanıcı kodu elle de okuyabiliyor.
    }
    setKopyalanan(hex)
    if (sayacRef.current) window.clearTimeout(sayacRef.current)
    sayacRef.current = window.setTimeout(() => setKopyalanan(null), 1400)
  }

  const zemin = ZEMINLER[mod]

  return (
    <div className="font-marka mx-auto flex w-full max-w-[1180px] flex-col gap-11 px-5 pb-[54px] pt-[27px] text-[#1d1f20] dark:text-stone-100">
      {/* 1. Başlık */}
      <header className="flex flex-wrap items-end justify-between gap-4 border-b border-[#d4d4d7] pb-[17px] dark:border-stone-700">
        <div>
          <p className="font-marka-baslik text-[13px] font-semibold uppercase tracking-[0.18em] text-[#416180] dark:text-[#8fb3d4]">
            Kurumsal İletişim / Logo Paketi
          </p>
          <h1 className="mt-2 font-marka-baslik text-[clamp(34px,4.4vw,56px)] font-bold uppercase leading-[1.02]">
            Türkiye Teknoloji Takımı Vakfı
            <br />
            Girişim Merkezi
          </h1>
        </div>
        <div className="text-[13px] text-[#5d5d60] dark:text-stone-400 sm:text-right">
          <p>Sürüm 1.0</p>
          <p>Renk paleti &amp; arka plan kullanımı</p>
        </div>
      </header>

      {/* 2. Ana marka işareti */}
      <section className="grid gap-6 lg:grid-cols-[minmax(300px,1fr)_1.25fr]">
        <Blueprint className="flex min-h-[320px] items-center justify-center bg-white p-6">
          <img
            src="/marka/tgm-logo-trans.png"
            alt="TGM işareti — Türkiye Teknoloji Takımı Vakfı Girişim Merkezi"
            className="w-full max-w-[240px]"
          />
        </Blueprint>

        <div className="flex flex-col gap-[17px]">
          <h2 className="font-marka-baslik text-[22px] font-semibold uppercase tracking-[0.06em]">
            Ana marka işareti
          </h2>
          <p className="max-w-[52ch] text-[17px] leading-[1.6] text-pretty">
            TGM işareti dört renkli tek bir bloktur. Harf kompozisyonu, renk sırası ve blok
            oranları değiştirilemez; işaret her zaman bir bütün olarak yerleştirilir.
          </p>

          <dl className="grid gap-[14px] sm:grid-cols-[repeat(auto-fit,minmax(150px,1fr))]">
            {[
              ['Minimum boyut', 'Dijital 32 px · Baskı 12 mm'],
              ['Net alan', "İşaret yüksekliğinin %25'i"],
              ['Dosya', 'SVG tercih edilir, PNG yedek'],
            ].map(([etiket, deger]) => (
              <div key={etiket} className="border-t border-[#d4d4d7] pt-[10px] dark:border-stone-700">
                <dt className="font-marka-baslik text-[12px] font-semibold uppercase tracking-[0.14em] text-[#416180] dark:text-[#8fb3d4]">
                  {etiket}
                </dt>
                <dd className="mt-1 text-[15px]">{deger}</dd>
              </div>
            ))}
          </dl>
        </div>
      </section>

      {/* 3. Renk paleti */}
      <section className="flex flex-col gap-[17px]">
        <BolumBasligi
          baslik="01 — Renk paleti"
          ipucu={
            kopyalanan ? (
              <span data-testid="kopya-ipucu">
                <code className="font-mono">{kopyalanan}</code> kopyalandı
              </span>
            ) : (
              <span data-testid="kopya-ipucu">Kodu kopyalamak için renk alanına tıklayın</span>
            )
          }
        />

        <div className="grid gap-[17px] sm:grid-cols-[repeat(auto-fit,minmax(180px,1fr))]">
          {RENKLER.map((renk) => (
            <Blueprint key={renk.hex}>
              {/* Swatch'ın kendisi düğme: rengin üstüne tıklamak en doğal
                  hareket ve ayrı bir "kopyala" düğmesi kartı kalabalıklaştırırdı. */}
              <button
                type="button"
                onClick={() => void kopyala(renk.hex)}
                aria-label={`${renk.ad} rengin HEX kodunu kopyala`}
                data-testid={`swatch-${renk.hex.slice(1)}`}
                className="block h-[132px] w-full cursor-pointer border-0 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#5980a6]"
                style={{ background: renk.hex }}
              />
              <div className="border-t border-[#d4d4d7] p-[14px] dark:border-stone-700">
                <p className="font-marka-baslik text-[17px] font-semibold uppercase tracking-[0.08em]">
                  {renk.ad}
                </p>
                <p className="text-[12px] text-[#5d5d60] dark:text-stone-400">{renk.rol}</p>
                <dl className="mt-3 flex flex-col gap-1 text-[13px] tabular-nums">
                  {[
                    ['HEX', renk.hex],
                    ['RGB', renk.rgb],
                    ['CMYK', renk.cmyk],
                    ['Pantone', renk.pantone],
                  ].map(([etiket, deger]) => (
                    <div key={etiket} className="flex justify-between gap-3">
                      <dt className="text-[#5d5d60] dark:text-stone-400">{etiket}</dt>
                      <dd>{deger}</dd>
                    </div>
                  ))}
                </dl>
              </div>
            </Blueprint>
          ))}
        </div>

        <p className="text-[12px] text-[#5d5d60] dark:text-stone-400">
          Pantone karşılıkları yaklaşıktır; baskı öncesi doğrulanmalı.
        </p>

        <div className="mt-1 flex flex-col gap-[14px]">
          <h3 className="font-marka-baslik text-[15px] font-semibold uppercase tracking-[0.14em] text-[#416180] dark:text-[#8fb3d4]">
            Degrade eşleri — yalnızca işaret içinde
          </h3>
          <div className="grid gap-[14px] sm:grid-cols-[repeat(auto-fit,minmax(200px,1fr))]">
            {DEGRADELER.map((degrade) => (
              <div key={degrade.etiket}>
                <div
                  className="h-[44px] border border-[#d4d4d7] dark:border-stone-700"
                  style={{ background: degrade.css }}
                />
                <p className="mt-2 text-[12px] tabular-nums text-[#5d5d60] dark:text-stone-400">
                  {degrade.etiket}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* 4. Arka plan kullanımı */}
      <section className="flex flex-col gap-[17px]">
        <div className="flex flex-wrap items-end justify-between gap-3 border-b border-[#d4d4d7] pb-[10px] dark:border-stone-700">
          <h2 className="font-marka-baslik text-[26px] font-semibold uppercase leading-none tracking-[0.05em]">
            02 — Arka plan kullanımı
          </h2>
          <div className="flex border border-[#d4d4d7] dark:border-stone-700" role="group" aria-label="Zemin seçimi">
            {ZEMINLER.map((z, i) => (
              <button
                key={z.sekme}
                type="button"
                aria-pressed={mod === i}
                onClick={() => setMod(i)}
                data-testid={`zemin-${i}`}
                className={[
                  'cursor-pointer px-3 py-[7px] text-[13px] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#5980a6]',
                  i > 0 ? 'border-l border-[#d4d4d7] dark:border-stone-700' : '',
                  mod === i
                    ? 'bg-[#303C48] text-white dark:bg-stone-100 dark:text-stone-900'
                    : 'bg-transparent text-[#1d1f20] dark:text-stone-200',
                ].join(' ')}
              >
                {z.sekme}
              </button>
            ))}
          </div>
        </div>

        <div className="grid gap-5 lg:grid-cols-[1.35fr_1fr]">
          <Blueprint
            className="relative flex min-h-[360px] items-center justify-center overflow-hidden"
            {...{}}
          >
            {/* Fotoğraf modu: gri tonlama + koyu örtü. Handoff'un kuralı,
                işaretin sakin ve koyu bir alanda oturması. */}
            {mod === 2 ? (
              <>
                <img
                  src="/marka/ornek-fotograf.jpg"
                  alt=""
                  aria-hidden="true"
                  className="absolute inset-0 size-full object-cover [filter:grayscale(1)_brightness(.55)]"
                />
                <div aria-hidden="true" className="absolute inset-0 bg-[rgba(30,40,51,.45)]" />
              </>
            ) : null}
            <div
              aria-hidden={mod === 2 ? 'true' : undefined}
              className="absolute inset-0"
              style={{ background: zemin.zemin || undefined }}
            />
            <img
              src={zemin.logo}
              alt="İşaretin seçilen zeminde görünümü"
              data-testid="zemin-logosu"
              className="relative w-[210px]"
            />
          </Blueprint>

          <div className="flex flex-col gap-[17px]">
            <p className="text-[17px] leading-[1.6] text-pretty" data-testid="zemin-aciklama">
              {zemin.aciklama}
            </p>
            <dl className="flex flex-col">
              {zemin.kurallar.map(([etiket, deger]) => (
                <div
                  key={etiket}
                  className="flex gap-4 border-t border-[#d4d4d7] pt-[10px] pb-[10px] dark:border-stone-700"
                >
                  <dt className="min-w-[52px] font-marka-baslik text-[12px] font-semibold uppercase tracking-[0.14em] text-[#416180] dark:text-[#8fb3d4]">
                    {etiket}
                  </dt>
                  <dd className="text-[15px]">{deger}</dd>
                </div>
              ))}
            </dl>
          </div>
        </div>

        <div className="grid gap-[17px] sm:grid-cols-[repeat(auto-fit,minmax(200px,1fr))]">
          {YANLIS_KULLANIM.map((ornek) => (
            <Blueprint key={ornek.metin} className="p-[17px]">
              <div
                className="flex h-[110px] items-center justify-center"
                style={{ background: ornek.zemin }}
              >
                <img
                  src="/marka/tgm-logo-trans.png"
                  alt=""
                  aria-hidden="true"
                  className="w-[96px]"
                  style={{
                    filter: ornek.filtre || undefined,
                    opacity: ornek.opaklik ? 0.4 : undefined,
                    mixBlendMode: ornek.opaklik ? 'multiply' : undefined,
                  }}
                />
              </div>
              <p className="mt-3 flex gap-2 text-[14px]">
                <span aria-hidden="true" className="text-[18px] leading-none text-[#416180] dark:text-[#8fb3d4]">
                  ✕
                </span>
                <span>{ornek.metin}</span>
              </p>
            </Blueprint>
          ))}
        </div>
      </section>

      {/* 5. Alt bilgi */}
      <footer className="flex flex-wrap justify-between gap-3 border-t border-[#d4d4d7] pt-[17px] text-[13px] text-[#5d5d60] dark:border-stone-700 dark:text-stone-400">
        <p>Kullanım soruları ve dosya talepleri için kurumsal iletişim ekibine yazın.</p>
        <p>Türkiye Teknoloji Takımı Vakfı Girişim Merkezi · İç kullanım</p>
      </footer>
    </div>
  )
}
