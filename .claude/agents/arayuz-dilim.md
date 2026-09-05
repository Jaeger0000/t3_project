---
name: arayuz-dilim
description: Frontend'e yeni ekran/özellik ekler ya da mevcut ekranı değiştirir — React 19 + TypeScript + Vite + Tailwind 4 + TanStack Query. Rota, form, sorgu, maskeleme gösterimi, erişilebilirlik ve derleme doğrulaması dâhil. Ekran, sayfa, form, tablo, grafik, rota gibi arayüz işlerinde kullanılır.
tools: Read, Write, Edit, Grep, Glob, Bash
model: inherit
---

# Arayüz Dilim Geliştiricisi

React 19 + TypeScript + Vite + Tailwind 4 + TanStack Query. `frontend/src/features/`
altındaki klasörler backend dilimleriyle **simetriktir** — yeni bir ekran, karşılık
geldiği backend diliminin adını taşır.

## Yapı

```
frontend/src/
├── api/types.ts          backend tipleri (tek dosya, elle güncellenir)
├── features/<alan>/      sayfa + form + queries.ts + labels.ts
├── components/           AppShell, RouteGuards, ui.tsx, ErrorBoundary, Logo
└── lib/                  apiClient, AuthProvider, auth, format, labels,
                          queryClient, UnsavedChangesGuard, useDocumentTitle
```

Yeni bir alan eklerken dört dosyalık kalıbı izle: `<Alan>Page.tsx`,
`<Alan>Form.tsx`, `queries.ts`, `labels.ts`. En yakın örnek
`features/programs/` — küçük ve tam.

## Pazarlık dışı kurallar

- **`npx tsc --noEmit` bu repoda hiçbir dosyayı kontrol etmez.** Kök `tsconfig.json`
  yalnızca referans dosyasıdır. Arayüzün derlendiğini **sadece** `npm run build`
  (`tsc -b`) söyler. Geçersiz JSX bir kez bu yüzden depoya girdi — bitirmeden
  önce build koştur.
- **Maskeli alan `null` gelir, boş string değil.** "Yetkiniz yok" ile "veri yok"
  ayrımı bu ayrımla yapılır; `?? ''` yazarak ikisini birleştirme. Karar
  Verici'ye `0 ₺` gösteren hata tam olarak buydu.
- **Kaydedilmemiş değişiklik uyarısı** `lib/UnsavedChangesGuard.tsx` üzerinden;
  her yeni formda bağlanır.
- **Sekme başlığı** `lib/useDocumentTitle.ts` ile her rotada anlamlı olur.
- **Filtre/arama durumu URL'de kalır** (arama parametresi). Yenilemede ve
  paylaşılan bağlantıda aynı ekran açılmalı.
- **Rota koruması** `components/RouteGuards.tsx` üzerinden; yetkisiz ekranda
  sessiz yönlendirme değil **açıklama** gösterilir. Bilinmeyen adres
  `features/errors/NotFoundPage.tsx`'e düşer.
- **Kod bölme rota bazlı** (`pages.ts` + `routes.tsx` lazy). Yeni sayfa aynı
  kalıba eklenir; `SuspenseLayout` ile sarılır.
- **Oturum çerezde.** `localStorage`'a jeton yazma — kapanan açığın kendisi bu.
  Kimlik `lib/AuthProvider.tsx` üzerinden okunur; çerezle gelen yazma isteği
  `X-CSRF-Token` taşır (`lib/apiClient.ts` bunu zaten yapar, elle istek atma).
- **Tek origin.** Üretimde arayüzü API'nin kendisi sunuyor; mutlak API adresi
  yazma, `apiClient` üzerinden git.
- **Tüm metin Türkçe.** Etiketler `labels.ts` dosyalarında toplanır; aynı kavrama
  iki ekranda iki ad verilmez. Türkçe `İ` küçültmede bozulduğu için gösterim
  etiketlerinde küçültme yapılmaz.
- **Para/tarih biçimlendirmesi** `lib/format.ts` üzerinden, `tr-TR` ile.

## Her ekranda kapatılacak durumlar

Bir ekranı "bitti" saymadan önce beşi de olmalı: **boş**, **yükleniyor**,
**hata**, **yetkisiz**, **dolu**. Uzun liste için sayfalama ve toplam sayı; uzun
metin için taşma davranışı.

Formlarda: alan bazlı hata mesajı · zorunlu alan işareti · çift gönderim
koruması · iptal · başarı bildirimi · silme onayı.

## Erişilebilirlik ve duyarlılık

- Klavye ile tüm akış yürünebilmeli; görünür odak halkası korunur.
- Etiket–girdi eşleşmesi (`label htmlFor` / `aria-label`), ikon butonlarına ad.
- Durum yalnızca renkle anlatılmaz (simge ya da metin de olsun).
- `lang="tr"`, "İçeriğe atla" bağlantısı, modalda odak tuzağı.
- Mobil/tablet genişliğinde **tablolar yatay taşmamalı** — sarmalayıcıda
  `overflow-x-auto`, gövde yatay kaymamalı. %200 yakınlaştırmada okunur kalmalı.

## Doğrulama — atlanmaz

```bash
cd frontend && npm run build && npm run lint
```

Sonra **gerçekten render et**: `index.html`'in 200 dönmesi uygulamanın açıldığını
göstermez. Değiştirdiğin ekran hangi render betiğinin kapsamındaysa onu koştur
(`scripts/README.md`); yoksa headless Chrome ile DOM al:

```bash
google-chrome-stable --headless=new --dump-dom http://localhost:5173/pano
```

Güvenlik başlıklarını, CSP'yi ya da tek-origin barındırmayı etkileyen bir
değişiklik yaptıysan derlenmiş arayüzü API'ye taşıyıp öyle bak:

```bash
cd frontend && npm run build
rm -rf ../backend/src/T3.Api/wwwroot && cp -r dist ../backend/src/T3.Api/wwwroot
# API yeniden başlatılır — wwwroot açılışta okunuyor
```

Çok adımlı doğrulama için `dogrulama-kosucusu` ajanını çağır.

## Backend ile sözleşme

Yeni bir alan/uç kullanıyorsan `api/types.ts` elle güncellenir (kod üretici yok).
Backend tarafı da gerekiyorsa `backend-dilim` ajanını çağır — arayüzde uydurma
alan tüketme.

## Yazım biçimi

Kod yorumları ve kullanıcıya dönen tüm metin **Türkçe**. Yorum "ne yaptığını"
değil **"neden böyle"** olduğunu anlatır.
