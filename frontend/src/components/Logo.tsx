/**
 * TGM işareti.
 *
 * Kurumun gerçek logosu (`design_handoff_logo_paketi/`): dört renkli tek blok —
 * kırmızı T, antrasit işaret, turuncu G, mavi M. Handoff kuralı net: harf
 * kompozisyonu, renk sırası ve blok oranları değiştirilmez, işaret her zaman bir
 * bütün olarak yerleştirilir. Bu yüzden burada çizim yok, dosya var.
 *
 * İki dosya iki tema için: renkli sürüm açık zeminde, tek renk beyaz sürüm koyu
 * zeminde. Handoff bunu "antrasit blok zeminde kaybolursa beyaz versiyon" diye
 * yazıyor ve arayüzün koyu teması `stone-950`, yani tam o durum. Seçim CSS ile
 * yapılıyor (`dark:` sınıfları), JavaScript'e sorulmuyor: tema değişince ikinci
 * bir render beklemeden doğru dosya görünüyor.
 *
 * PNG, SVG değil — çünkü elimizdeki kaynak PNG. Handoff da bunu söylüyor:
 * vektör aslından `tgm-logo-renkli.svg` / `tgm-logo-beyaz.svg` üretilip
 * bunların yerine konmalı.
 *
 * Arayüzde kullanılan dosyalar (`*-isaret*.png`) özgün 447×447 karenin şeffaf
 * kenar boşluğu kırpılmış hâli (333×232). İşaret değişmedi; yalnızca dosyanın
 * içindeki boşluk kalktı, çünkü o boşluk 40 piksellik bir kutuda işareti
 * gereksiz küçültüyordu. Handoff'un "net alan" kuralı (işaret yüksekliğinin
 * %25'i) artık CSS tarafında, düzenin kendi boşluğuyla veriliyor.
 */
export function LogoMark({ className = '', label }: { className?: string; label?: string }) {
  return (
    <span className={`relative block shrink-0 ${className}`} data-testid="logo">
      <img
        src="/marka/tgm-logo-isaret.png"
        alt={label ?? ''}
        aria-hidden={label ? undefined : true}
        className="block size-full object-contain dark:hidden"
      />
      <img
        src="/marka/tgm-logo-isaret-beyaz.png"
        alt={label ?? ''}
        aria-hidden={label ? undefined : true}
        className="hidden size-full object-contain dark:block"
      />
    </span>
  )
}

/**
 * İşaret + kelime markası. Başlıkta ve giriş ekranında aynı hizalama kullanılıyor;
 * iki yerde elle kurulunca biri diğerinden kayıyordu.
 */
export function LogoLockup({ compact = false }: { compact?: boolean }) {
  return (
    <span className="flex items-center gap-3">
      <LogoMark className={compact ? 'h-9 w-[52px]' : 'h-12 w-[69px]'} />
      <span className="leading-tight">
        <span className="block text-sm font-semibold text-stone-900 dark:text-stone-50">
          Girişim Ekosistemi
        </span>
        <span className="block text-xs text-stone-500">Yönetim Sistemi</span>
      </span>
    </span>
  )
}
