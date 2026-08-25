# Denetim Düzeltme Planı

Ürün denetimi raporunda çıkan 7 bloklayıcı ve 12 yüksek/orta bulgunun
uygulama planı. Her madde: **ne değişecek · hangi dosya · kabul kriteri · efor**.

Denetim yöntemi ve bulguların kanıtları için bkz.
[Urun_Denetcisi_Ajan_Promptu.md](Urun_Denetcisi_Ajan_Promptu.md).

---

## 0. Takvim gerçeği — sıralamayı bu belirliyor

| Tarih | Ne teslim ediliyor |
|---|---|
| **26 Ağustos 10.00** | İş modeli kanvası + **prototip videosu** + sunum |
| **5–6 Eylül** | Creathon + Demo Day (5 dk pitch + 5 dk jüri sorusu) |

Bugün **24 Ağustos**. Video teslimine **2 gün** var. Bu yüzden plan üç dalgaya
bölündü ve dalgalar önem sırasına göre değil, **hangi tarihte kime ne
göstereceğine** göre dizildi:

- **Dalga 0 (bugün–yarın):** videoda ve jüri sorusunda görünen eksikler.
  Toplam ~1 gün; dördü tek dosyalık düzeltme.
- **Dalga 1 (26 Ağustos–5 Eylül):** MVP ve KVKK bütünlüğü. Demo Day'de
  "bu ürün gerçekten kurumsal mı" sorusunun cevabı burada.
- **Dalga 2 (Demo Day sonrası):** gerçek canlıya çıkış altyapısı.

> Bir dalgayı yarım bırakıp sonrakine geçmeyin: Dalga 0'ın tamamı Dalga 1'in
> ilk maddesinden daha değerli.

---

## Dalga 0 — Videodan önce (hedef: 25 Ağustos akşamı)

> **Durum: tamamlandı (24 Ağustos).** Altı maddenin hepsi kapandı; doğrulama
> 156 birim testi, 307 uçtan uca kontrol, 201 render kontrolü — sonuncusu
> `scripts/render_faz6.py` (yeni, 75 kontrol) dâhil. 0.1'in kabul kriteri
> Dalga 1'de bilinçli olarak değişti: AI, uygulamaya gömülü anahtarla değil
> **MCP sunucusu** olarak sunuluyor (bkz. 0.1 notu ve Dalga 1 başlığı).
> Aşağıdaki her maddenin altında ne yapıldığı ve nerede doğrulandığı yazıyor.

### 0.1 · AI anahtarı adını düzelt — **5 dakika** `[Y-04]`
> **Yapıldı.** `.env` anahtarı `T3_Ai__ApiKey` oldu; `Program.cs` açılışta
> modeli sorguluyor — anahtar boşsa `LogWarning`, varsa `LogInformation` ile
> model adı.
>
> **Kabul kriteri Dalga 1'de bilinçli olarak değişti:** anahtar uygulamaya
> girilmiyor, model bağlantısı **MCP üzerinden** kuruluyor (harici ajan
> `POST /mcp`'ye kendi jetonuyla bağlanır). Bu yüzden rozet "Yerel plan (model
> yok)" kalıyor ve panel kurulumu kalıcı bir bilgi notuyla açıklıyor: sessizlik
> gitti, yanıltma da. Anahtar bir gün girilirse aynı uç modele bağlanır.


`.env` içindeki `T3_Anthropic__ApiKey` hiçbir yerde okunmuyor; kod
`Ai` bölümünü bağlıyor ([AiOptions.cs:5](../backend/src/T3.Infrastructure/Ai/AiOptions.cs#L5)).
Anahtar tanımlı olmasına rağmen AI katmanı **her zaman** yerel plana düşüyor.

- `.env`: `T3_Anthropic__ApiKey=` → **`T3_Ai__ApiKey=`** (değeri koru).
- `.env.example` zaten doğru — dokunma.
- [DependencyInjection.cs:55](../backend/src/T3.Infrastructure/DependencyInjection.cs#L55)
  civarına açılış logu ekle: anahtar boşsa `LogWarning("Ai:ApiKey boş — asistan yerel plana düşecek.")`.
  Sessiz yedek mekanizma bir daha kimseyi yanıltmasın.

**Kabul:** Backend yeniden başlatılır, `/pano` → "Ekosisteme soru sor" panelinde
rozet **"Model: claude-…"** yazar (bugün "Yerel plan" yazıyor).

### 0.2 · Giriş hız sınırını bölümlendir — **15 dakika** `[B-04]`
> **Yapıldı.** `T3.Api/RateLimiting/AuthRateLimit.cs`: giriş kovası IP +
> e-posta, AI kovası kullanıcı kimliği başına; `OnRejected` `Retry-After`
> yazıyor. E-posta gövdeden okunuyor — bölüm anahtarı geri çağrısı eşzamanlı
> olduğu için gövde sınırlayıcıdan önce `CaptureLoginEmail` ara katmanında
> tamponlanıyor. Yalnızca IP ile bölümlemek yetmezdi: demo ve betikler aynı
> makineden giriyor. Doğrulama: `render_faz6.py` → A hesabı 429 alırken B
> hesabı sorunsuz giriyor.


[Program.cs:96](../backend/src/T3.Api/Program.cs#L96) — `AddFixedWindowLimiter("auth")`
bölüm anahtarsız: dakikada 10 izin **tüm uygulama için tek kova**. Doğrulandı:
`admin@` 10 kez yanlış şifre girdikten sonra `karar.verici@` doğru şifreyle
429 alıyor.

```csharp
options.AddPolicy("auth", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        // IP + e-posta: bir hesabın kilidi başka hesabı etkilemesin.
        partitionKey: $"{context.Connection.RemoteIpAddress}|{context.Request.Query["email"]}",
        _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10, QueueLimit = 0 }));
```

> Gövdedeki e-postayı bölüm anahtarına almak istiyorsan istek gövdesi
> tamponlanmalı; en basiti IP ile bölümlendirmek, e-posta bazlı kilitlemeyi
> **1.4**'teki denetim kaydının üstüne kurmak.

`"ai"` limiti de aynı şekilde kullanıcı kimliğine göre bölümlendirilmeli.
Ayrıca reddedilen yanıta `Retry-After` başlığı ekle (`OnRejected`).

**Kabul:** A kullanıcısı 10 yanlış denemeyle 429 alırken B kullanıcısı aynı
anda sorunsuz giriş yapar. `python3 scripts/e2e_faz3.py` temiz geçer.

### 0.3 · Sekme başlığı, `lang` ve meta — **20 dakika** `[Y-02]`
> **Yapıldı.** `index.html` (`lang="tr"`, başlık, description, Open Graph) +
> `lib/useDocumentTitle.ts`; on ekranda ve girişim kartında (girişim adı)
> kullanılıyor. Doğrulama: `render_faz6.py` dört ekranın başlığının farklı
> olduğunu ve kart başlığının girişim adını taşıdığını kontrol ediyor.


55 render'ın 55'inde sekme başlığı `frontend` yazıyor; sayfa dili `en`.

- [index.html](../frontend/index.html): `<html lang="tr">`,
  `<title>T3 Girişim Ekosistemi Yönetim Sistemi</title>`, `meta description`,
  Open Graph (`og:title`, `og:description`, `og:image`).
- Yeni `frontend/src/lib/useDocumentTitle.ts` — tek satırlık kanca:

```ts
export function useDocumentTitle(title: string) {
  useEffect(() => { document.title = `${title} · T3 Girişim Ekosistemi` }, [title])
}
```

- Her sayfa bileşeninde H1 ile aynı metni geçir. Girişim kartında girişim adı
  (`useDocumentTitle(startup.name)`).

**Kabul:** Üç girişim kartı üç sekmede açıldığında sekmeler ayırt edilir;
`document.documentElement.lang === 'tr'`.

### 0.4 · 404 sayfası — **30 dakika** `[Y-03]`
> **Yapıldı.** `features/errors/NotFoundPage.tsx`; oturum açıkken kabuk içinde,
> kapalıyken çıplak. `/` hâlâ panoya yönleniyor, bilinmeyen adres 404 gösteriyor
> ve adres çubuğu değişmiyor. `/girisimler/99999` ile var-olmayan GUID artık
> aynı ifadeyi veriyor.


[App.tsx:41](../frontend/src/App.tsx#L41) bilinmeyen her adresi sessizce
`/pano`'ya yolluyor.

- `frontend/src/features/errors/NotFoundPage.tsx` ekle: başlık
  "Aradığınız sayfa bulunamadı", adresin yanlış yazılmış olabileceği
  açıklaması, panoya ve girişimlere dönüş bağlantıları.
- `<Route path="*" element={<Navigate to="/pano" replace />} />` →
  `<Route path="*" element={<NotFoundPage />} />`. Oturum açıksa `AppShell`
  içinde, kapalıysa çıplak render edilsin.
- Aynı geçişte iki farklı "bulunamadı" metnini tek ifadeye indir:
  `/girisimler/99999` → "İstenen kaynak bulunamadı.", geçerli GUID →
  "Girişim bulunamadı." Kullanıcı için ikisi aynı durum.

**Kabul:** `/olmayan-sayfa` 404 ekranı gösterir, adres çubuğu değişmez.

### 0.5 · Girişim/ekip/katılım yazma yollarını arayüze aç — **~6 saat** `[B-01]`
> **Yapıldı.** Yeni dosyalar: `features/startups/StartupForm.tsx` (portaldan
> taşınan `ProfileForm`, `mode: proposal | direct`),
> `features/startups/TeamSection.tsx` (`MemberForm` + silme onayı, aynı `mode`
> ayrımı), `features/startups/ParticipationForm.tsx`. `queries.ts` yedi yazma
> kancasıyla genişledi. `StartupsPage`'e **Yeni girişim**, karta **Düzenle** ve
> **Kaydı pasife al** (yalnızca Süper Yönetici), ekip sekmesine ekle/düzenle/
> çıkar, programlar sekmesine **Programa ekle** geldi. Portal aynı bileşenleri
> `mode="proposal"` ile kullanıyor.
>
> **Planda olmayan bulgu — maskeleme yazma yolunda tutulmuyordu.** Tam
> değiştirmeli `PUT /api/startups/{id}` maskeli alanı istemciye `null`
> gönderdiği için geri yazarken **siliyordu**: vergi numarasını göremeyen
> Program Yöneticisi'nin kaydettiği her düzenleme numarayı boşaltırdı ve iz
> bunu "kullanıcı sildi" diye kaydederdi. Düzeltme:
> `StartupWriteModel.ApplyTo(startup, visibility)` göremediği alanı koruyor
> (üç birim testi), `StartupForm` `direct` kipte o alanı hiç göstermiyor ve
> gerekçesini yazıyor, `proposal` kipte form yine hiç açılmıyor.
>
> **Planda olmayan ek adım:** Program Yöneticisi yeni girişimi kaydettikten
> sonra kartına gitmek 404 veriyordu — kapsamı "programlarımdan geçmiş
> girişimler" olduğu için kayıt bir döneme bağlanana kadar kapsamına girmiyor.
> `StartupsPage` bu yüzden kaydetmenin ardından katılım formunu açıp kuralı
> ekranda yazıyor; Süper Yönetici doğrudan karta gidiyor.


**Bu, Dalga 0'ın gövdesi ve jürinin soracağı sorunun cevabı:**
"girişimi sisteme kim ekliyor?"

Backend **tamamen hazır** — eksik olan yalnızca ekran:

| Uç | Durum |
|---|---|
| `POST/PUT/DELETE /api/startups` | ✅ var ([StartupEndpoints.cs:74,84,97](../backend/src/T3.Api/Endpoints/StartupEndpoints.cs#L74)) |
| `POST/PUT/DELETE /api/startups/{id}/team` | ✅ var ([satır 106,117,128](../backend/src/T3.Api/Endpoints/StartupEndpoints.cs#L106)) |
| `POST /api/participations` | ✅ var ([ProgramEndpoints.cs:20](../backend/src/T3.Api/Endpoints/ProgramEndpoints.cs#L20)) |
| Bunları çağıran arayüz | ❌ **hiçbiri** |

**Yeniden kullanım — sıfırdan form yazma.** Portalda bu formların ikisi
zaten var, yalnızca öneri kipinde çalışıyor:
[PortalPage.tsx:77 `ProfileForm`](../frontend/src/features/portal/PortalPage.tsx#L77)
(StartupWriteModel'in tüm alanları) ve
[PortalPage.tsx:313 `MemberForm`](../frontend/src/features/portal/PortalPage.tsx#L313).
`AchievementSection`/`DocumentSection` bileşenlerinde kurulmuş
**`mode` deseni** aynen buraya uygulanacak:

1. `ProfileForm` → `frontend/src/features/startups/StartupForm.tsx`'e taşı,
   `mode: 'proposal' | 'direct'` propu ekle.
   - `proposal` → bugünkü davranış: `POST /api/change-requests`.
   - `direct` → `POST /api/startups` veya `PUT /api/startups/{id}`.
2. `MemberForm` + `DeleteProposal` → `frontend/src/features/startups/TeamSection.tsx`,
   aynı `mode` ayrımıyla.
3. `frontend/src/features/startups/queries.ts` bugün **yalnızca `useQuery`**
   içeriyor. Ekle: `useCreateStartup`, `useUpdateStartup`, `useDeleteStartup`,
   `useAddTeamMember`, `useUpdateTeamMember`, `useRemoveTeamMember`,
   `useAddParticipation`.
4. `StartupsPage.tsx`: **"Yeni girişim"** düğmesi, `session.permissions.canManageStartups`
   ile koşullu.
5. `StartupDetailPage.tsx`: başlıkta **"Düzenle"**; ekip sekmesinde ekle/düzenle/kaldır;
   programlar sekmesinde **"Programa ekle"** (program + dönem + durum + katılım tarihi).
   Dönem listesi `GET /api/programs` yanıtındaki `Terms` dizisinden geliyor —
   ek uç gerekmiyor.
6. `PortalPage.tsx` taşınan bileşenleri `mode="proposal"` ile içeri alsın;
   davranışı **değişmemeli**.

**Kurallar (bozulmayacak):** Girişim kullanıcısı hiçbir tabloya doğrudan
yazmaz — `direct` kipi yalnızca `canManageStartups` doğruyken render edilir,
sunucu tarafı zaten `Policies.ManageStartups` ile ikinci kez kontrol ediyor.
Silme onayı ve çift gönderim koruması için `AchievementSection`'daki
iki adımlı `confirming` + `disabled={busy}` desenini birebir kopyala.

**Kabul:**
- Program yöneticisi arayüzden yeni girişim oluşturur, kartını düzenler,
  iki ekip üyesi ekler, girişimi kendi programının 2026 dönemine bağlar.
- Yeni kayıt listede, kartta ve **kronolojide** görünür.
- Kapsamı dışındaki programa bağlamayı denerse "Bu program sizin
  sorumluluğunuzda değil." hatası alır.
- Girişim kullanıcısı aynı ekranlarda hâlâ yalnızca öneri gönderir.
- `python3 scripts/e2e_faz3.py` + yeni bir `render` kontrolü temiz geçer.

### 0.6 · Mobil yatay taşmayı kapat — **20 dakika** `[O-02]`
> **Yapıldı — sebep tahminden farklıydı.** Filtre satırı ya da grafik kabı değil,
> **grid öğesinin örtük min-content genişliği**: pano grafikleri tek sütunlu bir
> grid'in aynı izinde duruyor ve iz en geniş öğenin min-content'i kadar
> büyüyordu — "En çok yatırım alan girişimler" listesindeki `truncate` bağlantı
> (nowrap olduğu için min-content'i tüm metin genişliği) izi 378 px'e çıkarıyordu.
> Düzeltme: `ChartCard` ve girişim döşemesine `min-w-0`, dar ekranda daha küçük
> punto/kenar boşluğu, grafik etiket sütunu `6rem`. `/onaylar` taşmasının sebebi
> ayrıydı: öneri satırındaki uzun doküman adı bölünmüyordu → `break-words`.
> Doğrulama: `render_faz6.py` dokuz rotayı 360/375/414 px'te ölçüyor.


375 px'te `/girisimler` 394 px, `/pano` 402 px genişliğe taşıyor; taşan öğe
kök `div.min-h-screen` ([AppShell.tsx](../frontend/src/components/AppShell.tsx)).
Muhtemel neden: sabit genişlikli filtre satırı veya grafik kabı.
Videoda telefon görüntüsü kullanılacaksa bu görünür.

**Kabul:** 360/375/414 px'te `scrollWidth === clientWidth`.

---

## Dalga 1 — Creathon haftasına kadar (26 Ağustos – 5 Eylül)

> **Durum: tamamlandı (24 Ağustos).** Altı maddenin hepsi kapandı. Doğrulama:
> 185 birim testi, 307 uçtan uca kontrol, 287 render kontrolü — sonuncusu
> `scripts/render_faz7.py` (yeni, 86 kontrol; Dalga 2'de 91'e çıktı) dâhil. Tek kalan kayıt teknik
> değil: **1.5'in metinleri hukuki onay bekliyor** ve ekranda görünür biçimde
> "taslak" işaretli. Aşağıdaki her maddenin altında ne yapıldığı ve nerede
> doğrulandığı yazıyor.
>
> Bu dalgada AI tarafında bilinçli bir karar da netleşti: **model bağlantısı
> yalnızca MCP üzerinden kuruluyor.** Uygulamanın içine anahtar gömmek yerine
> sistem `POST /mcp` ile MCP sunucusu olarak yayımlanıyor; harici ajan kendi
> jetonuyla bağlanıp aynı araçları kendi yetkisi kadar kullanıyor. Panel içi
> yanıtlar bu yüzden yerel planlayıcıdan geliyor ve panel bunu kalıcı bir bilgi
> notuyla söylüyor (0.1'in "rozet Model: … yazsın" kabul kriteri bu kararla
> yerini bu nota bırakıyor).

### 1.1 · Program ve dönem yönetimi — **~1 gün** `[B-02]`
> **Yapıldı.** Sekiz yeni dikey dilim (`CreateProgram`, `UpdateProgram`,
> `DeleteProgram`, `Terms/AddTerm|UpdateTerm|DeleteTerm`,
> `UpdateParticipation`, `RemoveParticipation`), ortak yetki kapısı
> `ProgramAccessGuard` ve iki yeni politika: `Policies.ManagePrograms`
> (program tanımı — yalnızca SuperAdmin, çünkü program listesi yetki
> kapsamının tanımı) ile `Policies.ManageProgramTerms` (dönem/katılım —
> Program Yöneticisi'ne de açık, satır düzeyinde kendi programıyla sınırlı).
> `AddParticipation` de aynı muhafıza taşındı, kural beş yerde tekrarlanmasın.
> Arayüz: `ProgramsPage` artık yönetim ekranı (`ProgramForm`, `TermForm`,
> iki adımlı kapatma), girişim kartının programlar sekmesinde
> `ParticipationEditor` (düzelt/kaldır). Katılımı olan dönem 409 ile
> reddediliyor. Doğrulama: `render_faz7.py` yaşam döngüsünün tamamını
> arayüzden yürütüyor (oluştur → dönem ekle → katılım düzelt → katılım kaldır
> → dönem kapat → program kapat) ve Program Yöneticisi'nin API'den de program
> oluşturamadığını sınıyor.

Programlar bugün yalnızca `DevDataSeeder` ile, yani doğrudan veritabanına
yazılarak var oluyor. `EcosystemProgram` → `ProgramTerm` →
`ProgramParticipation` zincirinin ilk iki halkasının yazma yolu **hiç yok**.

Yeni dikey dilimler (`T3.Application/Features/Programs/` altında,
mevcut `AddParticipation` dilimi şablon):

```
Programs/CreateProgram/      ProgramWriteModel + Handler
Programs/UpdateProgram/
Programs/DeleteProgram/      soft delete — zinciri elle yürüt
Programs/Terms/AddTerm/      ProgramTermWriteModel + Handler
Programs/Terms/UpdateTerm/
Programs/Terms/DeleteTerm/
Programs/UpdateParticipation/   yanlış eklenen katılım düzeltilebilsin
Programs/RemoveParticipation/
```

- Handler'lar isim kuralıyla otomatik DI'ya giriyor
  ([DependencyInjection.cs:50](../backend/src/T3.Application/DependencyInjection.cs#L50)) —
  elle kayıt gerekmez.
- Yeni politika sabiti: `Policies.ManagePrograms` — **yalnızca SuperAdmin**.
  Dönem ekleme Program Yöneticisi'ne de açılabilir ama yalnızca kendi programına;
  bu kontrol `AddParticipationHandler`'daki `AssignedProgramIds` kalıbını izlesin.
- `ProgramWriteModel` için tek doğrulayıcı, `.WithValidation<T>()` ile
  uç filtresinde.
- Silme: program silinirken bağlı dönem ve katılımlar elle pasife alınacak —
  soft delete süzgeçleri yalnızca okumayı daraltır.
- Arayüz: [ProgramsPage.tsx](../frontend/src/features/programs/ProgramsPage.tsx)
  bugün tamamen salt okunur (tek `Button` yok). "Yeni program", program
  düzenleme ve dönem ekleme eklenecek.

**Kabul:** Süper Yönetici yeni program + 2026 dönemi oluşturur, bir program
yöneticisine kapsam olarak atar; o yönetici giriş yaptığında yeni programı ve
ona bağlı girişimleri görür; program yöneticisi program oluşturmayı denerse 403.

### 1.2 · Şifre kurtarma ve şifre değiştirme — **~1 gün** `[B-03]`
> **Yapıldı.** Üç dilim (`RequestPasswordReset`, `ResetPassword`,
> `ChangePassword`), yeni tablo `PasswordResetTokens` (migration
> `Faz7DalgaBir_ParolaKurtarma_DenetimGirisleri`) ve `User.MustChangePassword`.
> Jetonun **SHA-256 özeti** saklanıyor, tek kullanımlık ve 2 saat geçerli;
> `forgot-password` adresin kayıtlı olup olmadığını söylemiyor. E-posta
> `IEmailSender` arkasındaki `FileOutboxEmailSender` ile sunucunun diskindeki
> kutuya yazılıyor — jeton HTTP yanıtında **hiç dönmüyor**; bağlantı `Host`
> başlığından değil `Email:AppBaseUrl`'den kuruluyor. Arayüz:
> `/sifremi-unuttum`, `/sifre-sifirla/:token`, `/sifre-degistir` ve yönetici
> şifre attığında `RequireAuth` kullanıcıyı o ekrana kilitliyor. Doğrulama:
> `render_faz7.py` tek kullanımlık bir hesapla tüm akışı yürütüyor (jetonu
> kutudaki e-postadan okuyor, ikinci kullanımın reddedildiğini sınıyor);
> tohum hesaplarının şifresine dokunulmuyor.

Bugün: `forgot-password`, `reset-password`, `change-password` → **hepsi 404**.
Tek yol yöneticinin şifre ataması, yani **yönetici her hesabın şifresini biliyor**.

Yeni dilimler `T3.Application/Features/Auth/` altında:

```
Auth/RequestPasswordReset/   e-posta al, süreli tek kullanımlık jeton üret
Auth/ResetPassword/          jeton + yeni şifre
Auth/ChangePassword/         mevcut şifre doğrulamalı, oturum içi
```

- Yeni tablo/kolon: `PasswordResetToken` (hash, `ExpiresAt`, `UsedAt`,
  `UserId`) → migration gerekir. Jetonun **kendisi değil hash'i** saklanır.
- `User` üzerine `MustChangePassword` bayrağı: yönetici şifre atadığında
  `true`, kullanıcı değiştirince `false`.
- **Kullanıcı numaralandırmasına dikkat:** `forgot-password` e-posta kayıtlı
  olsun olmasın **aynı** yanıtı dönmeli.
- E-posta gönderimi: Creathon kapsamında gerçek SMTP kurulamıyorsa
  `IEmailSender` arayüzünün arkasına geliştirme ortamında log'a yazan bir
  uygulama koy — arayüz dursun, sağlayıcı sonra gelsin.
- Arayüz: `/sifremi-unuttum`, `/sifre-sifirla/:token`, ve oturum içi
  "Şifre değiştir". Giriş ekranına bağlantı
  ([LoginPage.tsx](../frontend/src/features/auth/LoginPage.tsx)).

**Kabul:** Kullanıcı e-postasını girer, bağlantıyı alır, yeni şifreyle giriş
yapar; bağlantı ikinci kullanımda ve süresi dolduğunda reddedilir; yönetici
şifre atadığında kullanıcı ilk girişte değiştirmeden başka ekrana geçemez.

### 1.3 · Oturum ömrü ve ağ hatası ayrımı — **~4 saat** `[Y-01]`
> **Yapıldı — plandaki kısa yol seçildi.** `AccessTokenMinutes = 480`
> (yenileme jetonu Dalga 2'deki çerez/tek origin kararına bağlı, sıra
> bozulmadı). `apiClient` ağ hatasını `ApiError(0, 'Sunucuya ulaşılamıyor…')`
> olarak normalleştiriyor; `AuthProvider` yalnızca **401'de** oturumu
> düşürüyor ve gerekçeyi (`signedOutReason`) giriş ekranında gösteriyor.
> `RequireAuth` bağlantı hatasında "yeniden dene" düğmesi gösteriyor,
> oturumsuz derin bağlantıda hedefi `state.from` ile taşıyor ve giriş sonrası
> kullanıcıyı oraya döndürüyor. Doğrulama: `render_faz7.py` Chrome'un
> `Network.setBlockedURLs` komutuyla yalnızca `/api/*` isteklerini engelleyip
> jetonun yerinde kaldığını ve ham "Failed to fetch" yazmadığını sınıyor.

Üç ayrı sorun, tek kök: oturum durumu tek noktada yönetilmiyor.

1. **15 dakikalık jeton, yenileme yok**
   ([JwtOptions.cs:14](../backend/src/T3.Infrastructure/Identity/JwtOptions.cs#L14)).
   Yenileme jetonu ekle (HttpOnly çerezde, `1.5` ile birlikte yapılabilir) ya
   da kısa yolu seç: `AccessTokenMinutes = 480` + süre dolmadan önce uyarı.
2. **Sessiz atılma.** [apiClient.ts:38](../frontend/src/lib/apiClient.ts#L38)
   "Oturum süresi doldu" mesajını üretiyor ama
   [AuthProvider.tsx](../frontend/src/lib/AuthProvider.tsx) onu yutuyor.
   Sebebi state'te taşı, giriş ekranında göster; giriş sonrası kullanıcıyı
   kaldığı rotaya döndür.
3. **Ağ hatası oturumu düşürüyor ve İngilizce mesaj basıyor.** API kapalıyken
   ekranda ham **"Failed to fetch"** yazıyor. `apiClient`'ta `fetch`'i
   `try/catch`'e al, ağ hatasını `ApiError(0, 'Sunucuya ulaşılamıyor…')`
   olarak normalleştir; `AuthProvider` yalnızca **401'de** oturumu düşürsün,
   ağ hatasında düşürmesin.

**Kabul:** API kapalıyken kullanıcı oturumda kalır, Türkçe "sunucuya
ulaşılamıyor" durumu ve "yeniden dene" düğmesi görür; jeton dolduğunda giriş
ekranında gerekçeyi okur ve giriş sonrası kaldığı sayfaya döner.

### 1.4 · Giriş olaylarını denetim izine yaz — **~2 saat** `[B-05]`
> **Yapıldı.** `LoginHandler` artık `Auth.LoginSucceeded` / `Auth.LoginFailed`
> yazıyor, hız sınırı reddi `Auth.RateLimited` olarak düşüyor, şifre olayları
> (`Auth.PasswordResetRequested`, `Auth.PasswordReset`, `Auth.PasswordChanged`,
> `Auth.PasswordChangeFailed`) da izde. Yeni `IClientContext` ile **IP ve
> istemci bilgisi** kaydediliyor (`AuditLogs.IpAddress` kolonu vardı ama hiç
> yazılmıyordu; `UserAgent` kolonu eklendi). E-posta **maskeli**
> (`MaskedEmail`, `k***@alan.test`). `ActorUserId`/`ActorRole` nullable oldu:
> başarısız girişte kimlik doğrulanmamıştır ve eski kod aktörü `Guid.Empty`,
> rolü `DecisionMaker` diye yazıyordu. Kilit satırı **pencere başına bir**
> yazılıyor — her redde satır açmak izi saldırı yüzeyine çevirirdi. `/denetim`
> süzgecine "Başarısız giriş", "Hız sınırı kilidi", "Şifre işlemleri" ve
> "Program ve dönem" grupları eklendi. Doğrulama: `render_faz7.py`.

15 başarısız giriş denemesinden sonra denetim izinde **sıfır** kayıt var;
eylem türleri yalnızca `Report.Export`, `Assistant.Ask`, `Document.Download`,
`ChangeRequest.Submit`, `User.Update`.

- [LoginHandler.cs](../backend/src/T3.Application/Features/Auth/Login/LoginHandler.cs)
  içine `IAuditWriter` enjekte et: `Auth.LoginSucceeded`, `Auth.LoginFailed`,
  `Auth.RateLimited`.
- IP ve kullanıcı ajanı kaydedilsin. Başarısız denemede e-posta ham
  saklanmasın (hash/maskeli) — kayıt kendisi bir kişisel veri yığınına
  dönüşmesin.
- `/denetim` ekranına eylem türü filtresi ekle.

**Kabul:** Bir başarılı + bir başarısız giriş sonrası denetim izinde iki yeni
satır; satırlar IP ve zaman damgası taşır; girişim kullanıcısı göremez.

### 1.5 · KVKK metinleri ve başvuru yolu — **~3 saat (teknik)** `[B-06]`
> **Yapıldı (teknik kısım).** `/kvkk-aydinlatma` ve `/kullanim-sartlari`
> oturum gerektirmiyor; oturum açıkken kabuk içinde, kapalıyken çıplak
> render ediliyor. `AppShell`'e her ekranda görünen alt bilgi geldi (sürüm,
> KVKK metni, kullanım şartları, destek/KVKK başvuru adresi); giriş ve şifre
> ekranları da aynı bağlantıları taşıyor. Portal ekip formundaki kişisel veri
> uyarısı Dalga 0'da eklenmişti, yerinde.
> ⚠️ **Metinlerin hukuki içeriği onaylanmadı** — sayfalar görünür biçimde
> "Taslak" işaretli. Onaylanmamış bir aydınlatma metnini onaylanmış gibi
> göstermek yükümlülüğü karşılamaz, karşılanmış gibi gösterir.

Sistemde aydınlatma metni, çerez bildirimi, kullanım şartları ve KVKK başvuru
yolu **hiç yok** — 55 render'ın hiçbirinde geçmiyor.

- Yeni rotalar: `/kvkk-aydinlatma`, `/kullanim-sartlari` — **`.AllowAnonymous`
  karşılığı**, yani giriş yapmadan açılabilmeli (`RequireAuth` dışında).
- Giriş ekranına ve `AppShell` alt bilgisine bağlantı; alt bilgide ayrıca
  sürüm ve iletişim/destek bağlantısı (`[O-06]` ile aynı dokunuş).
- Portalda ekip üyesi formuna kısa bilgilendirme: "Girdiğiniz iletişim
  bilgileri kişisel veridir; yalnızca yetkili roller görür."
- Metinlerin **hukuki içeriği ekip dışından onay ister** — teknik iş küçük,
  içeriği erken başlat.

**Kabul:** İki rota giriş yapılmadan açılır, giriş ekranından ve her sayfanın
alt bilgisinden erişilir; portal formunda kişisel veri uyarısı görünür.

### 1.6 · Karar Verici'nin `/onaylar` ekranı — **15 dakika** `[O-03]`
> **Yapıldı.** `RequirePermission` "herhangi biri" (anyOf) semantiğine geçti;
> `/onaylar` artık `canReviewApprovals` **veya** `mustSubmitForApproval`
> istiyor. Karar Verici gerekçeyi okuyor, diğer üç rolün davranışı değişmedi.
> `render_faz3.py`'deki eski beklenti (boş "Önerilerim" ekranı) düzeltildi —
> o kontrol denetimin bulduğu hatayı doğru sayıyordu.

Karar Verici doğrudan URL ile `/onaylar`'a girdiğinde sonsuza dek boş kalacak
"Önerilerim" ekranını görüyor; bu rolde `mustSubmitForApproval` ve
`canReviewApprovals` ikisi de `false`.

[App.tsx](../frontend/src/App.tsx) içinde rotayı, ikisinden **en az biri**
doğru olmayan role `/portal` ile aynı "Bu ekranı görme yetkiniz yok"
ekranını gösterecek biçimde koru.

**Kabul:** Karar Verici `/onaylar`'ı açtığında yetki açıklaması görür; diğer
üç rolün davranışı değişmez.

---

## Dalga 2 — Gerçek canlıya çıkış (Demo Day sonrası)

> **Durum: tamamlandı (24 Ağustos).** Üç maddenin hepsi kapandı. Doğrulama:
> 191 birim testi, 310 uçtan uca kontrol, 346 render kontrolü — sonuncusu
> `scripts/render_faz8.py` (yeni, 54 kontrol) dâhil. Yeni betik **iki origin'e**
> karşı koşuyor: davranış kontrolleri Vite'ta (5173), barındırma ve güvenlik
> başlıkları API'nin kendi sunduğu derlenmiş arayüzde (5080).
>
> Dalga 2'de planda olmayan iki bulgu daha kapandı; ikisi de doğrulama
> zincirinin kendisiyle ilgili:
> - **Arayüz derlenmiyordu.** Dalga 1'in son düzeltmesinde `AppShell.tsx` içine
>   konan JSX yorumu geçersiz konumdaydı. `npx tsc --noEmit` bunu *yakalamıyor*:
>   kök `tsconfig.json` yalnızca referans dosyası, hiçbir kaynağı kontrol
>   etmiyor. Gerçek kontrol `npm run build` (`tsc -b`). Dalga 1'in son render
>   koşusunun asılı kalmasının sebebi de buydu.
> - **Çıkış ekranda etkisizdi.** Jeton çereze taşındıktan sonra oturumu düşürme
>   işi `queryClient.clear()`'a bırakılmıştı; clear önbelleği boşaltıyor ama
>   bileşenlere yeni durum bildirmiyor. Aynı boşluk 401'de de vardı (React Query
>   hata durumunda eldeki `data`'yı koruyor), yani süresi dolmuş oturum ekranda
>   açık kalırdı. Çözüm: açık `signedOut` durumu (bkz. `AuthProvider`).
> - **Bir kontrol tohum düzenini ölçüyordu.** `e2e_faz4`'ün "zincir dokümanları
>   da kapatıyor" kontrolü, silinecek girişimin tohumda dosyası olduğunu
>   varsayıyordu; kapsam sırası değişince ürün doğru çalışırken kontrol
>   düşüyordu. Betik artık kaydı başarı sayısına bakarak seçiyor ve silmeden
>   önce kendi dosyasını yüklüyor (+1 kontrol, e2e_faz4 = 125).
>
> **25 Ağustos:** zincirin tamamı tur başına ayrı, temiz tohum verili
> veritabanlarında yeniden koşuldu — dört turda **310 uçtan uca + 346 render
> kontrolü, tek düşen yok**.

### 2.1 · Üretim dağıtım yolu — **~1 gün** `[B-07]`
> **Yapıldı — tercih edilen yol seçildi: API statik dosyaları kendisi sunuyor.**
> `SpaHosting` (statik dosyalar + SPA geri dönüşü), kök `Dockerfile` (üç aşama:
> arayüzü derle → API'yi yayınla → kök olmayan çalışma zamanı),
> `docker-compose.prod.yml` (postgres portu yayımlamıyor, API yalnızca
> loopback'e bağlanıyor), `.dockerignore` (sırlar ve yerel yüklemeler imaja
> girmiyor), `.env.prod.example`.
>
> Ayrıntılar ve gerekçeleri:
> - **API önekleri geri dönüşün dışında** (`/api`, `/health`, `/mcp`,
>   `/swagger`): `/api/olmayan-uc` için index.html döndürmek 404 sözleşmesini
>   bozar, istemci JSON beklerken HTML ayrıştırır.
> - **Önbellek başlıkları asimetrik**: özet adlı varlıklar `immutable`,
>   `index.html` `no-cache`. Tersi yapılsa dağıtımdan sonra eski paket adlarını
>   isteyen bir sayfa kalırdı.
> - **Göç uygulaması seçmeli** (`Database:MigrateOnStartup`, varsayılan kapalı;
>   compose'da açık): "tek komutla kalkan yığın" bunu gerektiriyor ama bir
>   uygulama sürümünün üretim şemasını haberimiz olmadan değiştirmesi
>   varsayılan olamaz.
> - **TLS**: `Hosting:RequireHttps` açıkken HSTS + HTTP→HTTPS yönlendirmesi
>   uygulamada; kapalıyken (ters vekil kurulumu) üretimde **uyarı log'u**
>   düşüyor. Sessiz bir "TLS yok" durumu bırakılmadı.
> - **`X-Forwarded-*` yalnızca güvenilen vekil adına**
>   (`Hosting:TrustedProxies`; boşsa yalnızca loopback). `HttpClientContext`
>   artık başlığı elle okumuyor — Dalga 1'de okuyordu ve denetim izindeki IP
>   istemcinin uydurabildiği bir değerdi.
> - **`Seed:Enabled` üretimde açıkça doğrulanıyor**: kapalıysa bilgi log'u,
>   yanlışlıkla açık bırakılmışsa hata log'u (ve tohumlama yine çalışmaz).
>
> Doğrulama: `render_faz8.py` derlenmiş arayüzü 5080'den açıp giriş yapıyor,
> derin bağlantının index.html'e düştüğünü ve `/api/olmayan-uc`'un JSON 404
> döndüğünü sınıyor.
>
> **25 Ağustos:** konteyner imajı da bu makinede derlendi ve çalıştırıldı
> (`t3-ekosistem:local`, 339 MB). Boş bir veritabanına karşı kaldırıldığında:
> göçleri kendisi uyguladı (`MigrateOnStartup`), "Tohum verisi kapalı (ortam:
> Production)" log'unu düştü, `uid=1654(app)` ile koştu (kök değil),
> `/health` 200, `/girisimler` derin bağlantısı index.html'e düştü,
> `/api/yok` JSON 404 verdi, `/api/...` yanıtları `no-store`, kök yanıtta CSP +
> `X-Frame-Options: DENY` + `Permissions-Policy` vardı. Yani imaj yalnızca
> "derleniyor" değil, üretim ayarlarıyla **doğru davranıyor**.
>
> İki makineye özgü tuzak: bu kutuda `docker buildx` kurulu değil
> (`DOCKER_BUILDKIT=0` ile eski derleyici) ve docker köprüleri `DOWN` olduğu
> için derleme adımlarının ağı yok — `docker build --network host` olmadan
> `npm ci` ilk adımda düşüyor.


Frontend tüm isteklerini göreli `/api/...` yoluna atıyor; bu yolu backend'e
taşıyan tek şey [vite.config.ts](../frontend/vite.config.ts) içindeki
**dev sunucusu proxy'si**. `import.meta.env` hiç kullanılmıyor, depoda
`Dockerfile`/`nginx.conf` yok, compose yalnızca `postgres` + `pgadmin`
tanımlıyor. Yani `npm run build` çıktısı hiçbir API'ye ulaşamaz.

- Tercih edilen yol: **API statik dosyaları kendisi sunsun**
  (`app.UseStaticFiles()` + SPA fallback). Tek origin, CORS yok, proxy yok.
- Alternatif: nginx ters vekil + ayrı konteyner.
- `docker-compose.prod.yml`: `postgres` + `api` (+ gerekiyorsa `web`).
- HTTPS sonlandırma, HTTP→HTTPS yönlendirme, üretim CORS alan adı.
- `T3_Seed__Enabled=false` üretimde açıkça doğrulansın.

**Kabul:** Tek komutla kalkan üretim benzeri yığında derlenmiş arayüzden
giriş yapılır ve girişim listesi görülür; karışık içerik uyarısı çıkmaz.

### 2.2 · Jetonu çerezine taşı ve CSP ekle — **~1 gün** `[Y-05]`
> **Yapıldı.** Jeton artık `HttpOnly` + `SameSite=Strict` çerezde
> (`SessionCookie`); `Secure` isteğin şemasına bağlı (geliştirmede düz HTTP,
> üretimde TLS zorunlu). Yeni uç: `POST /api/auth/logout` — çerezi yalnızca
> sunucu geçersiz kılabilir, istemcinin "unutması" yetmez.
>
> **Başlık yolu bilinçli olarak kaldırılmadı:** `Authorization: Bearer` hâlâ
> geçerli, çünkü MCP istemcileri, doğrulama betikleri ve Swagger çerez
> taşımıyor. `JwtBearerEvents.OnMessageReceived` başlığı önceliyor, yoksa çerezi
> okuyor. Bu ayrım CSRF kararının da temeli: **çerezle** kimliklenen yazma
> istekleri çift-gönderim jetonu istiyor (`t3.csrf` çerezi + `X-CSRF-Token`
> başlığı, sabit süreli karşılaştırma), **başlıkla** gelenler istemiyor — başka
> bir sitenin sayfası bizim jetonumuzu başlığa koyamaz. Çıkış ucu kuralın
> dışında: zorlanmış çıkışın zararı yeniden giriş yapmak, karşı taraftaki risk
> ise "çerezini temizleyemeyen kullanıcı".
>
> CSP `default-src 'self'` üzerine kurulu (tek origin kararı bunu mümkün kıldı);
> `script-src 'self'` sıkı, `style-src`'de `'unsafe-inline'` var çünkü grafikler
> ölçüleri element `style` özniteliğine yazıyor — XSS'in tehlikeli kolu script
> tarafı ve orası açık değil. Swagger yalnızca geliştirmede ve kendi script
> bloklarını gömdüğü için CSP'den muaf. Yanıtlara ayrıca `nosniff`,
> `Referrer-Policy`, `X-Frame-Options`, `Permissions-Policy`,
> `Cross-Origin-Opener-Policy` ve **API yolunda `Cache-Control: no-store`**
> ekleniyor (O-06'nın indirme maddesi de bununla kapandı).
>
> İstemci tarafında `localStorage`'da artık jeton yok; yalnızca `t3.session.active`
> işareti duruyor (sır değil): siteye ilk gelen ziyaretçiye "oturum süreniz
> doldu" dememek ve sekmeler arası senkron için.
>
> **Doğrulama betikleri de bu yüzden değişti:** jeton artık script'in
> erişemediği bir yerde, dolayısıyla `localStorage.setItem` ile oturum kurma
> yolu kapandı. `cdp.py`'ye `set_session`/`clear_session` eklendi (çerezi CDP
> yazıyor) ve beş render betiği buna geçti — kapanan açığın kendisi bu.


Erişim jetonu `localStorage`'da ([apiClient.ts:21](../frontend/src/lib/apiClient.ts#L21));
CSP başlığı yok. Süper Yönetici jetonu çalınırsa 32 girişimin vergi numarası,
iletişim bilgisi ve finansalları dışarı taşınır.

- Jeton → `HttpOnly` + `Secure` + `SameSite=Strict` çerez; yenileme jetonu
  ayrı çerezde (`1.3` ile birlikte yapmak en verimlisi).
- CSRF önlemi (SameSite yeterli değilse çift gönderim deseni).
- Yanıtlara sıkı `Content-Security-Policy`.
- **Not:** Bu değişiklik `2.1`'deki tek origin kararına bağlı — sırayı bozma.

### 2.3 · Kalan orta öncelikli maddeler

| # | İş | Efor | Durum |
|---|---|---|---|
| `[O-01]` | Filtre/sıralama/sayfa durumunu URL'ye yaz (`setSearchParams`); geri tuşu ve paylaşılan bağlantı çalışsın | Düşük | ✅ |
| `[O-04]` | `SearchText.Normalize` aksanları katlasın (ç→c, ğ→g, ı/İ→i, ö→o, ş→s, ü→u); "saglik" → "Sağlık" bulsun. Gösterim etiketleri değişmez | Düşük | ✅ |
| `[O-05]` | Kirli formdan çıkışta uyarı (`useBlocker`); `storage` olayıyla sekmeler arası oturum senkronu | Orta | ✅ |
| `[O-06]` | Hata izleme (Sentry), rota bazlı `lazy` kod bölme (bugün tek parça 362 kB), indirme yanıtına `Cache-Control: no-store` | Orta | ⚠️ kısmen |
| `[D-01]` | Girdi odak halkası `brand-500/20` → en az `/60`; gövde başına "İçeriğe atla" bağlantısı | Düşük | ✅ |

**O-01.** Filtre, sıralama ve sayfa `useSearchParams` ile URL'de duruyor;
bileşende ikinci bir kopya **yok** (iki kaynak birbirinden kayardı). Arama
terimi geçmişe satır eklemiyor (`replace: true`) — geri tuşu filtreden filtreye
atlamalı, harften harfe değil. Filtre değişince sayfa numarası düşüyor.

**O-04.** Katlama `SearchText.Normalize`'a **eklenmedi**, ayrı bir
`SearchText.Fold` olarak yazıldı. Sebep: Normalize'ın çıktısı veritabanındaki
*katlanmamış* kolonla karşılaştırılıyor (e-posta eşitliği, isim tekilliği);
katlamayı oraya koymak terimi "oguz" yaparken kolonu "oğuz" bırakır ve girişi
sessizce bozardı. Sorgu tarafında katlama `StartupSearch` içindeki
`Expression` yüklemlerinde, SQL `replace()` zincirine çevrilerek yapılıyor —
`unaccent` uzantısı ve `ILIKE` sağlayıcıya özel olurdu. Liste, CSV aktarımı ve
pano istatistiği aynı yüklemi kullanıyor: ekranda görülen satırların CSV'de
farklı çıkması en sinsi rapor hatası olurdu.

**O-05.** `useBlocker` veri yönlendiricisi istiyor; yönlendirici kurulumu
`createBrowserRouter` + `createRoutesFromElements`'e taşındı (rota ağacı JSX
olarak kaldı). Uyarı form doldurulurken değil **ayrılırken** çıkıyor. Sekmeler
arası senkron `storage` olayıyla: bir sekmede çıkış yapılınca diğeri de giriş
ekranına dönüyor.

**O-06 — kısmen.** Rota bazlı kod bölme yapıldı (tek parça 400 kB → 323 kB
ana parça + 14 rota parçası; veri yönlendiricisi ana parçayı ~55 kB büyüttü,
bilinçli takas). İndirme/API yanıtlarına `Cache-Control: no-store` eklendi.
**Sentry bağlanmadı:** hesap, DSN ve KVKK tarafında bir yurt dışı aktarım
kararı gerektiriyor — üçü de bu depoda kararlaştırılamaz. Yerine
`ErrorBoundary` eklendi (beyaz ekran yerine Türkçe açıklama + yenile) ve
raporlama tek bir `console.error` noktasına toplandı: sağlayıcı geldiğinde
değişecek tek yer orası.

**D-01.** Odak halkası `brand-500/20` → `/60` (ölçüldü: `oklab(… / 0.6)`),
düğmelere `focus-visible` halkası eklendi (klavye odağı hiç görünmüyordu),
`AppShell`'e "İçeriğe atla" bağlantısı ve `<main id="icerik">`. Bu maddenin
doğrulanabilmesi için `cdp.py`'de `Emulation.setFocusEmulationEnabled`
açıldı: headless tarayıcı pencereyi odakta saymadığı için `:focus` seçicisi
hiç eşleşmiyordu ve odak stilleri ölçülemiyordu.

---

## Her madde için doğrulama alışkanlığı

Sırayı bozma — bu proje bugüne kadarki hataların çoğunu son adımda yakaladı:

```bash
# Temiz tohum verisiyle başla: betikler veriyi değiştiriyor.
docker compose down -v && docker compose up -d postgres

cd backend && dotnet build && dotnet test
cd frontend && npm run build && npm run lint   # tsc --noEmit bu repoda hiçbir şeyi kontrol etmez

python3 scripts/e2e_faz3.py && python3 scripts/e2e_faz4.py && python3 scripts/e2e_faz5.py
python3 scripts/render_faz3.py && python3 scripts/render_faz4.py && python3 scripts/render_faz5.py
python3 scripts/render_faz6.py   # Dalga 0
python3 scripts/render_faz7.py   # Dalga 1  (arada ~1 dk: giriş kovası boşalsın)
python3 scripts/render_faz8.py   # Dalga 2  (öncesinde derlenmiş arayüz wwwroot'a kopyalanır)
```

`index.html`'in 200 dönmesi uygulamanın açıldığını göstermez — ama arayüzün
**derlendiğini** de `npx tsc --noEmit` göstermez (kök tsconfig yalnızca referans
dosyası; komut sessizce geçer). Dalga 2'de arayüz bir süre derlenmez hâldeydi ve
bunu ancak `npm run build` ortaya çıkardı.

Betikler birbirinin verisini yiyor (girişim pasife alma, kullanıcı oluşturma):
tam yeşil bir zincir **sıfırlanmış veritabanı** ister. Sabit sayıya/ada bağlı
kontroller bu yüzden kapsamdan türetilenlere çevrildi, ama sıfırlama gereği
ortadan kalkmadı.

Denetimde kullanılan rol × rota matrisi ve mutlu yol dışı senaryolar
(ağ kesintisi, jeton silme, bozuk jeton, mobil taşma, klavye turu)
tekrarlanabilir; her dalga sonunda yeniden koşulmalı. Dalga 2'den sonra "jeton
silme" senaryosu **çerez silme** demek: jeton artık script'in erişemediği bir
yerde ve betikler oturumu CDP ile kuruyor (`cdp.Browser.set_session`).

---

## Kapsam dışı bırakılanlar

Denetimde **sorunsuz** çıkan ve bu planda işi olmayan alanlar — yanlışlıkla
"iyileştirme" adına bozulmasın:

satır düzeyi kapsam · alan düzeyi KVKK maskelemesi (ham API yanıtlarında
sızıntı yok) · kapsam dışı kayıtta 404 (varlık sızdırmıyor) · doküman
indirmede IDOR yok · yükleme doğrulaması (uzantı, çift uzantı, boş dosya) ·
CSV maskelemesi ve formül enjeksiyonu öneki · onay diff'indeki `masked`
bayrağı · sayfalama sınırı kırpması · tohumlayıcının `IsDevelopment()`
koruması · alan bazlı Türkçe doğrulama mesajları · çift gönderim ve silme
onayı · denetim izinin indirme/aktarma/AI kapsamı · temiz `npm run build` ·
55 render'da sıfır konsol hatası.
