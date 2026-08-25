/**
 * Logo.
 *
 * Biçim neden bu: üç yükselen çubuk girişimin program yolculuğunu (ön kuluçka →
 * kuluçka → hızlandırma) ve büyümeyi anlatıyor, altlarındaki tek zemin çizgisi
 * "hepsi aynı ekosistemde" demek — ürünün tek cümlelik özeti bu. En yüksek
 * çubuğun tepesindeki altın nokta başarı/ödül kaydı; paletteki altın tam olarak
 * bu iş için ayrılmıştı (bkz. index.css).
 *
 * Kısıtlar tasarımı belirledi:
 * - **16 pikselde okunmalı** (sekme simgesi). Bu yüzden üç kalın çubuk ve tek
 *   vurgu var; ince çizgi ve küçük harf yok.
 * - **Yazı SVG içinde değil.** Harfleri path'e çevirmek dosyayı şişirir,
 *   `<text>` bırakmak ise sunucuda olmayan bir yazı tipine güvenmek olurdu.
 *   Kelime markası HTML tarafında duruyor, logo yalnızca simge.
 * - **Tek renk zemin, iki renk vurgu.** Koyu temada da aynı görünüyor: zemin
 *   markanın turuncusu, üstündeki her şey beyaz — tema değişince kontrast
 *   kaybolmuyor.
 * - **Harici dosya yok.** Satır içi SVG, çünkü CSP `default-src 'self'` ve
 *   `<img>` yerine simgeyi doğrudan gömmek bir istek daha tasarruf ediyor.
 */
export function LogoMark({ className = '', label }: { className?: string; label?: string }) {
  return (
    <svg
      viewBox="0 0 32 32"
      className={className}
      // Etiket verilmediyse simge süs: yanında zaten adı yazıyor ve ekran
      // okuyucunun aynı şeyi iki kez söylemesi gürültü.
      role={label ? 'img' : undefined}
      aria-label={label}
      aria-hidden={label ? undefined : true}
      focusable="false"
      data-testid="logo"
    >
      <defs>
        <linearGradient id="t3-logo-zemin" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#f76f4b" />
          <stop offset="0.55" stopColor="#e73a13" />
          <stop offset="1" stopColor="#b52205" />
        </linearGradient>
      </defs>

      <rect width="32" height="32" rx="8" fill="url(#t3-logo-zemin)" />

      {/* Yolculuk çizgisi: çubukların tepelerini birleştiriyor. Çubuklar bunun
          üstüne çiziliyor, yani yalnızca aralarda görünüyor — 16 pikselde
          ayrıntı değil yükselen bir hat olarak okunuyor. */}
      <path
        d="M9.5 17 16 13 22.5 9.5"
        fill="none"
        stroke="#fff"
        strokeOpacity="0.45"
        strokeWidth="1.5"
        strokeLinecap="round"
      />

      <g fill="#fff">
        <rect x="7.5" y="17" width="4" height="7" rx="2" />
        <rect x="14" y="13" width="4" height="11" rx="2" />
        <rect x="20.5" y="9.5" width="4" height="14.5" rx="2" />
      </g>

      {/* Ortak zemin: üç çubuk da aynı çizgiden yükseliyor. */}
      <rect x="7" y="24" width="18" height="2" rx="1" fill="#fff" fillOpacity="0.65" />

      {/* Başarı kaydı. Çubukla arasında bilinçli bir boşluk var: değmesi
          küçük boyutta çubuğun devamı gibi okunuyordu. */}
      <circle cx="22.5" cy="6" r="2.4" fill="#ffb900" />
    </svg>
  )
}

/**
 * Simge + kelime markası. Başlıkta ve giriş ekranında aynı hizalama kullanılıyor;
 * iki yerde elle kurulunca biri diğerinden kayıyordu.
 */
export function LogoLockup({ compact = false }: { compact?: boolean }) {
  return (
    <span className="flex items-center gap-3">
      <LogoMark className={compact ? 'size-9' : 'size-12'} />
      <span className="leading-tight">
        <span className="block text-sm font-semibold text-stone-900 dark:text-stone-50">
          Girişim Ekosistemi
        </span>
        <span className="block text-xs text-stone-500">Yönetim Sistemi</span>
      </span>
    </span>
  )
}
