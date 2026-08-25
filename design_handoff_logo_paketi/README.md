# Handoff: TGM Logo Paketi (Kurumsal İletişim)

## Overview
Türkiye Teknoloji Takımı Vakfı Girişim Merkezi için tek sayfalık logo/marka kullanım paneli.
Üç bölüm: (1) ana marka işareti kuralları, (2) renk paleti — HEX/RGB/CMYK/Pantone, tıklayınca HEX kopyalanır, (3) arka plan kullanımı — açık zemin / koyu zemin / fotoğraf üzerinde geçişli önizleme + üç "yanlış kullanım" örneği.
Hedef: siteye "Marka / Basın kiti" sayfası olarak eklenmesi.

## About the Design Files
Bu pakette bulunan HTML dosyaları **tasarım referansıdır** — hedeflenen görünüm ve davranışı gösteren prototiplerdir, doğrudan kopyalanacak production kodu değildir.
Yapılacak iş: bu tasarımı hedef kod tabanının mevcut ortamında (React/Next, Vue, Astro vb.) o projenin yerleşik component ve stil desenleriyle **yeniden kurmak**. Ortam henüz yoksa proje için en uygun framework seçilip orada uygulanmalıdır.
`TGM Logo Paketi.dc.html` özel bir prototip runtime'ı (`support.js`) kullanır; bu runtime taşınmaz. `preview.html` runtime bağımsız, tek dosya çalışan referanstır — tarayıcıda açıp birebir görebilirsiniz.

## Fidelity
**High-fidelity.** Renkler, tipografi, boşluklar ve etkileşimler nihai. UI birebir yeniden kurulmalı; değerler aşağıdaki Design Tokens bölümünden alınmalı.

## Screens / Views

### Tek sayfa — "Logo Paketi"
**Amaç:** İç ekipler, sponsorlar ve partnerlerin doğru logo/renk kullanımını tek ekranda görmesi.
**Layout:** Tek kolon, `max-width: 1180px`, ortalanmış. Sayfa dolgusu `27px 20px 54px` (üst/yan/alt). Bölümler arası dikey boşluk **44px**. Bölüm içi boşluk **17px**, ızgara boşlukları **24px**.

**1. Header**
- `display:flex; justify-content:space-between; align-items:flex-end`, alt kenarda 1px `--color-neutral-300` çizgi, alt dolgu 17px.
- Kicker: 13px, Barlow Condensed, uppercase, `letter-spacing:.18em`, renk `--color-accent-700` — "Kurumsal İletişim / Logo Paketi".
- H1: Barlow Condensed, `clamp(34px,4.4vw,56px)`, `line-height:1.02`, uppercase — "Türkiye Teknoloji Takımı Vakfı / Girişim Merkezi" (iki satır, `<br>`).
- Sağ meta: 13px, `--color-neutral-700`, iki satır — "Sürüm 1.0", "Renk paleti & arka plan kullanımı".

**2. Ana marka işareti**
- `grid-template-columns: minmax(300px,1fr) 1.25fr; gap:24px`.
- Sol: `.blueprint` figür + dört `<i class="corner tl|tr|bl|br">` işareti, beyaz (#ffffff) zemin, 24px dolgu, `min-height:320px`, logo `max-width:240px`.
- Sağ: H2 22px Barlow Condensed uppercase `letter-spacing:.06em`; paragraf 17px / `line-height:1.6` / `max-width:52ch` / `text-wrap:pretty`.
- Altında 3 meta hücresi (`repeat(auto-fit,minmax(150px,1fr))`, gap 14px), her biri üstte 1px `--color-neutral-300` çizgi + 10px dolgu; etiket 12px uppercase `.14em` `--color-accent-700`, değer 15px:
  - Minimum boyut — "Dijital 32 px · Baskı 12 mm"
  - Net alan — "İşaret yüksekliğinin %25'i"
  - Dosya — "SVG tercih edilir, PNG yedek"

**3. 01 — Renk paleti**
- Bölüm başlığı satırı: H2 26px uppercase `.05em` + sağda 13px ipucu metni; altta 1px çizgi.
- İpucu metni state'e bağlı: varsayılan "Kodu kopyalamak için renk alanına tıklayın"; kopyalandıktan sonra "`#HEX` kopyalandı" (1400 ms sonra geri döner).
- Kart ızgarası: `repeat(auto-fit,minmax(180px,1fr)); gap:17px`. Her kart `.blueprint` + 4 corner, zemin şeffaf.
- Kart üstü: 132px yüksek, tam genişlik `<button>`, dolgu rengi swatch rengi, `cursor:pointer`, border yok. Tıklama → `navigator.clipboard.writeText(hex)`.
- Kart gövdesi: üstte 1px çizgi, 14px dolgu; isim 17px Barlow Condensed uppercase `.08em`; rol 12px `--color-neutral-700`; ardından 13px `<dl>` — HEX / RGB / CMYK / Pantone satırları `justify-content:space-between`, değerler `font-variant-numeric:tabular-nums`.
- Dört renk (sırayla):
  | İsim | Rol | HEX | RGB | CMYK | Pantone* |
  |---|---|---|---|---|---|
  | Antrasit | Ana kurumsal ton, tipografi | #303C48 | 48 60 72 | 33 17 0 72 | 7545 C |
  | Kırmızı | Vurgu, uyarı ve enerji | #E43C24 | 228 60 36 | 0 74 84 11 | 179 C |
  | Turuncu | Vurgu, girişim ve hareket | #F99B1C | 249 155 28 | 0 38 89 2 | 1375 C |
  | Mavi | Kurumsal derinlik, veri | #0C4878 | 12 72 120 | 90 40 0 53 | 301 C |

  \* Pantone karşılıkları yaklaşıktır; baskı öncesi doğrulanmalı.
- Altta "Degrade eşleri — yalnızca işaret içinde": başlık 15px uppercase `.14em` `--color-accent-700`; `repeat(auto-fit,minmax(200px,1fr))`, gap 14px; her biri 44px yüksek şerit (1px `--color-neutral-300` çerçeve) + 12px etiket:
  - `linear-gradient(135deg,#E43C24,#B32A1C)`
  - `linear-gradient(135deg,#303C48,#1E2833)`
  - `linear-gradient(135deg,#F9A81C,#F08A14)`
  - `linear-gradient(135deg,#1878A8,#0C4878)`
- Bu blok `showGradients` (boolean, default true) ile açılıp kapanır.

**4. 02 — Arka plan kullanımı**
- Başlık satırında sağda segmented control (`.seg` + `.seg-opt`, aktif olan `.is-active`, `aria-pressed`): "Açık zemin" / "Koyu zemin" / "Fotoğraf üzerinde".
- Gövde: `grid-template-columns: 1.35fr 1fr; gap:20px`.
- Sol sahne: `.blueprint` figür + 4 corner, `overflow:hidden`, `min-height:360px`, zemin seçime göre değişir, logo 210px genişlik.
- Sağ: 17px açıklama paragrafı + üç kurallı liste (her satır üstte 1px çizgi, 10px dolgu; etiket 12px uppercase `.14em` `--color-accent-700`, `min-width:52px`).
- Üç mod:
  1. **Açık zemin** — zemin #ffffff, renkli logo (`tgm-logo.png`). Metin: "Birincil kullanım. İşaret beyaz veya çok açık nötr zeminde, kendi renkleriyle yer alır." Kurallar: Zemin "Beyaz ya da %0–8 gri; renkli açık zeminlerde kullanılmaz." / Kontrast "Zemin ile işaret arasında en az 4,5:1 kontrast korunur." / Dosya "tgm-logo-renkli.svg".
  2. **Koyu zemin** — zemin #1E2833, renkli logo. Metin: "Koyu zeminde işaret renkleri korunur; antrasit blok zeminde kaybolursa tek renk beyaz versiyon kullanılır." Kurallar: Zemin "Antrasit #303C48 veya daha koyu tonlar." / Uyarı "Kırmızı ve lacivert zeminlerde yalnızca beyaz versiyon." / Dosya "tgm-logo-beyaz.svg".
  3. **Fotoğraf üzerinde** — `assets/photo.jpg` `object-fit:cover` + `filter:grayscale(1) brightness(.55)`, üstüne `rgba(30,40,51,.45)` örtü, en üstte beyaz logo (`tgm-logo-beyaz.png`). Metin: "Fotoğraf üzerinde yalnızca tek renk beyaz versiyon kullanılır. Görsel, işaretin oturduğu alanda sakin ve koyu olmalıdır." Kurallar: Katman "Gerekirse %40–50 koyu örtü eklenir." / Alan "İşaret, detaylı veya yüksek kontrastlı bölgeye yerleştirilmez." / Dosya "tgm-logo-beyaz.svg".
- Altında üç "yanlış kullanım" kartı (`repeat(auto-fit,minmax(200px,1fr))`, gap 17px), her biri `.blueprint` + 4 corner, 17px dolgu; içinde 110px yüksek örnek alan ve altında ✕ (18px, `--color-accent-700`) + 14px metin:
  1. zemin #F99B1C, logo normal → "Doygun kurumsal renk zeminde renkli işaret kullanmayın."
  2. zemin #ffffff, logo `filter:hue-rotate(140deg) saturate(1.4)` → "İşaretin renklerini değiştirmeyin veya yeniden renklendirmeyin."
  3. zemin #9aa3ab, logo `opacity:.4; mix-blend-mode:multiply` → "Şeffaflık, gölge veya karışım efekti uygulamayın."
  Bu üç örnekte şeffaf arka planlı `tgm-logo-trans.png` kullanılır.

**5. Footer**
Üstte 1px çizgi, 17px dolgu, 13px `--color-neutral-700`, iki uca yayılmış: "Kullanım soruları ve dosya talepleri için kurumsal iletişim ekibine yazın." · "Türkiye Teknoloji Takımı Vakfı Girişim Merkezi · İç kullanım".

## Interactions & Behavior
- **Renk kopyalama:** swatch butonuna tıkla → `navigator.clipboard.writeText(hex)`; bölüm başlığındaki ipucu 1400 ms boyunca "`#HEX` kopyalandı" olur, sonra varsayılana döner. Clipboard API yoksa sessizce yut (görsel geri bildirim yine gösterilir). Butonlara `aria-label` ("HEX kodunu kopyala") eklenmesi önerilir.
- **Zemin seçici:** üç durumlu segmented control; seçim sahne zeminini, logo varyantını, açıklama metnini ve üç kuralı birlikte değiştirir. Geçiş anlık (animasyon yok); istenirse 150 ms opacity cross-fade eklenebilir.
- **Focus:** tüm etkileşimli öğeler `:focus-visible { outline:2px solid var(--color-accent); outline-offset:2px }`.
- **Responsive:** iki kolonlu ızgaralar (`minmax(300px,1fr) 1.25fr` ve `1.35fr 1fr`) ~900px altında tek kolona düşmeli; kart ızgaraları `auto-fit` ile kendiliğinden sarar. Header ~640px altında dikey yığılmalı.

## State Management
- `mode: 0 | 1 | 2` — aktif arka plan modu (0 açık, 1 koyu, 2 fotoğraf).
- `copied: string | null` — son kopyalanan HEX; 1400 ms timeout ile null'a döner (unmount'ta timeout temizlenmeli).
- `showGradients: boolean` — degrade bloğunun görünürlüğü (prop/config, default true).
- Veri fetch yok; palet ve metinler statik.

## Design Tokens

**Marka renkleri (logodan örneklendi):** #303C48, #E43C24, #F99B1C, #0C4878. Türev koyu tonlar: #1E2833, #B32A1C, #F08A14, #1878A8. Fotoğraf örtüsü `rgba(30,40,51,.45)`. Yanlış-kullanım gri zemini #9AA3AB.

**Arayüz (Industry design system, `styles.css`):** `--color-bg` #f2f2f3, `--color-text` #1d1f20, `--color-accent` #5980a6 + 100–900 rampası (`--color-accent-700` paragraf boyu accent metin için), `--color-neutral-300` hairline çizgiler, `--color-neutral-700` ikincil metin.

**Tipografi:** başlıklar Barlow Condensed (`--font-heading`), gövde Barlow (`--font-body`). Kullanılan boyutlar: 56/34 (H1 clamp), 26 (bölüm H2), 22 (H2), 18, 17, 15, 14, 13, 12 px. Uppercase başlıklarda `letter-spacing` .05–.18em.

**Spacing:** 44 (bölüm arası), 27/20/54 (sayfa dolgusu), 24, 20, 17, 14, 10, 6, 4 px. `--space-*` tokenları 3.4px adımlıdır (density .85), sabit değerler bu ölçekten yuvarlanmıştır.

**Radius / border / shadow:** köşeler kare — `border-radius: 0`. Çerçeveler 1px hairline. Gölge yok (`.blueprint` çizgi çerçevesi kullanılır). Design system `--radius-*` 4px taşır ancak bu sayfada kullanılmaz.

**Wireframe grameri:** her kart/figür `.blueprint` sınıfı + dört `<i class="corner tl|tr|bl|br">` registration işareti taşır, zemini şeffaftır, köşeleri karedir. İşaretler kaldırılmamalı.

## Assets
`assets/` klasöründe:
- `tgm-logo.png` — kullanıcının verdiği orijinal logo (447×447, beyaz zeminli).
- `tgm-logo-trans.png` — beyaz zemin alfaya çevrilmiş şeffaf versiyon (PNG'den türetildi).
- `tgm-logo-beyaz.png` — alfa maskesinden üretilmiş tek renk beyaz versiyon.
- `photo.jpg` — Industry design system'in referans fotoğrafı; yer tutucudur, kurumun gerçek görseliyle değiştirilmelidir.

**Önemli:** şeffaf ve beyaz versiyonlar PNG'den türetildiği için kenarları tam keskin değildir. Production'da orijinal vektör dosyasından `tgm-logo-renkli.svg` ve `tgm-logo-beyaz.svg` üretilip bunların yerine konmalıdır (sayfa metinleri zaten bu SVG adlarına atıf yapar).

## Files
- `preview.html` — runtime bağımsız, tek dosya çalışan referans; tarayıcıda doğrudan açılır. **Uygulama için bunu referans alın.**
- `TGM Logo Paketi.dc.html` — prototip kaynağı (özel runtime gerektirir, taşınmaz; yapı/markup referansı olarak okunabilir).
- `_ds/styles.css` — Industry design system token ve component katmanı (`.blueprint`, `.corner`, `.seg`, `.seg-opt`, renk/tip/spacing değişkenleri).
- `assets/` — yukarıdaki görseller.
