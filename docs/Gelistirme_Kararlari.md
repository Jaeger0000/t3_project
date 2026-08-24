# Geliştirme Kararları ve Ortam Notları

Bu dosya, oturumlar arasında kaybolmaması gereken **yerleşik kararların** ve
**tekrar tekrar çarpılan tuzakların** kaydıdır. Ürün gereksinimleri
[proje brifinde](Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md), mimari ve faz
planı [teknik planda](Problem7_Teknik_Plan.md); burada yalnızca *neden böyle
yaptık* durur.

**Nasıl kullanılır:** Aşağıdaki kararlar, takım açıkça yeniden açmadıkça
**kapalıdır** — tekrar tartışılmaz. Yeni bir mimari karar verildiğinde ya da
bir gün kaybettiren bir tuzağa çarpıldığında bu dosyaya bir madde eklenir.
Her oturumda yüklenen kısa özet [../CLAUDE.md](../CLAUDE.md) içindedir; oraya
yalnızca gerçekten her görevde lazım olan satırlar taşınır.

---

## 1. Ürün kapsamı

Creathon kitapçığındaki 7 problemden **Problem 7 — T3 Girişim Ekosistemi
Yönetim Sistemi** seçildi: girişimin profili, program geçmişi, satış, yatırım,
ekip, başarı ve dokümanları tek profilde birleştiren, rol bazlı erişimli
(Süper Yönetici, Program Yöneticisi, Girişim Kullanıcısı, Karar Verici) merkezi
platform.

Zorunlu MVP blokları: **merkezi girişim kartı**, **program geçmişi / gelişim
yolculuğu**, **girişim portalı + yönetici onayı**, **satış / yatırım / başarı /
doküman takibi**. Kurallar gereği bir MVP maddesi bile eksikse takım bir
sonraki değerlendirme aşamasına geçemez — mimari tartışmalarında öncelik her
zaman eksik MVP maddesini kapatmaktır.

Kaynak PDF'ler ve ara transkript dosyaları bilinçli olarak silindi; tek doğruluk
kaynağı `docs/` altındaki brif dosyasıdır.

### Takvim (kararları sıkıştıran şey)

- Görev teslimi (iş modeli kanvası + prototip videosu + sunum): **26 Ağustos 10.00**
- Yüz yüze Creathon + Demo Day: **5–6 Eylül** — 5 dakika sunum, 5 dakika jüri
  sorusu; jüri T3 Vakfı mütevelli, yönetim kurulu üyeleri ve koordinatörlerinden oluşuyor.

Stack ve mimari seçimleri "mühendislik saflığı" ile "jüriye gösterilebilir demo"
arasında bilinçli bir takas: bu kısıt hatırlanmadan kararlar mantıksız görünür.

---

## 2. Yerleşik teknik kararlar

1. **Backend: .NET 8 (`net8.0`).** Takımın en iyi bildiği ve mimariyi en rahat
   kurabildiği dil C#. Makinede yalnızca SDK 8.0.128 kurulu — hedef 9/10 değil, **net8.0**.
2. **Frontend: React + TypeScript + Vite.** Ayrı bir .NET API olduğu için
   Next.js SSR'a gerek kalmıyor.
3. **Clean Architecture + dikey dilim.** Dört proje (`T3.Domain`,
   `T3.Application`, `T3.Infrastructure`, `T3.Api`), ama Application katmanı
   teknik klasör yerine özellik klasörleriyle bölünüyor
   (`Features/Startups/CreateStartup/`) — 3-4 kişi çakışmadan paralel çalışsın diye.
4. **Minimal API + DI'dan çözülen düz handler sınıfları. MediatR yok** — fazladan
   soyutlama ve v13+ lisans sorusu istenmedi. FluentValidation ve Swagger var.
5. **Docker'da PostgreSQL, kendi kimlik altyapımız** (kendi kullanıcı/rol
   tablolarımız + JWT), dokümanlar önce yerel diskte, demo için S3 uyumlu depolama.
   Supabase yerine tam kontrol ve daha güçlü bir KVKK anlatısı için seçildi.
6. **AI: .NET backend'in içinde barınan MCP sunucusu** (resmî `ModelContextProtocol`
   C# SDK'sı). Kritik kısıt: MCP araçları **REST API'nin kullandığı aynı Application
   handler'larını** sarar, asla paralel bir veri yolu açmaz. RBAC ve KVKK maskelemesi
   araç sınırında uygulanır; AI, çağıran kullanıcının yetkisi olmayan veriyi okuyamaz.
7. **Mapper kütüphanesi yok.** Entity → DTO eşlemesi elle yazılıyor. Gerekçe:
   maskeleme koşullu mantık (`visibility.ShowTaxNumber ? s.TaxNumber : null`) ve
   "hangi alan neden gizlendi" sorusunun cevabı kodda okunabilir kalmalı.
   AutoMapper/Mapperly tam olarak bunu saklıyor.
8. **DTO sözleşmesi: dilim başına `Request`/`Response` record'ları.** Ortak bir
   `StartupDto` yok — onay kuyruğunun gördüğü ile Karar Verici'nin gördüğü aynı
   girişim farklı; tek DTO nullable alan çorbasına dönüşür. İstisna: create ve
   update **aynı** gövdeyi yazıyorsa tek yazma modeli + tek doğrulayıcı paylaşılır
   (`StartupWriteModel`, `TeamMemberWriteModel`). İkinci istisna "tek özelliğin iki
   ucu" (`SessionUserResponse`, `ReviewChangeRequestResponse`).
9. **Doğrulama Minimal API endpoint filtresinde** (`ValidationFilter<TRequest>` +
   `.WithValidation<T>()`), handler içinde değil. MediatR hattı olmadığı için filtre
   olmadan bir handler doğrulayıcısını sessizce atlayabilir. Her istek tipi için tek doğrulayıcı.
10. **Yetkilendirme varsayılan olarak kapalı:** `options.FallbackPolicy =
    RequireAuthenticatedUser()`. Herkese açık uçlar (health, login) `.AllowAnonymous()`
    demek zorunda. Auth düşünülmeden eklenen uç kapalı tarafa düşer.
11. **Enum'lar tel üzerinde string** (`JsonStringEnumConverter`); frontend tipleri
    string union. `erasableSyntaxOnly` zaten TS `enum`'ını yasaklıyor.
12. **Derinlemesine savunma:** endpoint politikası + handler içinde rol/kapsam
    tekrar kontrolü. MCP araçları handler'ları doğrudan çağıracağı için politika
    hattını atlar; tek kontrol noktası yeterli değil.

### Bilinçli olarak reddedilenler

Supabase (satıcı bağımlılığı, zayıf mimari anlatısı), ayrı bir TypeScript MCP
sunucusu (üçüncü servis, yetkilendirmenin ikinci kopyası), MediatR/CQRS hattı,
düz N-tier Controller-Service-Repository, her türlü otomatik mapper.

---

## 3. Onay akışı ve denetim izi kararları (Faz 3)

- **Girişim kullanıcısı hiçbir tabloya doğrudan yazmaz** — teknik planın 3.5
  bölümündeki temel kural. Demo değeri: "hiçbir girişim verisi onaysız yayına
  girmiyor" ekranda kanıtlanabilir olmalı.
- **Uygulayıcı (`ChangeRequestApplier`) `SaveChanges` çağırmaz**; izlenen grafiği
  değiştirir, kaydı onay handler'ı yazar. Böylece "istek onaylandı" ile "veri
  değişti" tek işlemde commit olur.
- **Gövde onay anında yeniden doğrulanır** ve ad tekilliği yeniden kontrol edilir:
  gönderim ile onay arasında günler geçebilir.
- **Kapsam dışı istek 404 döner, 403 değil** — 403 isteğin varlığını sızdırır.
- **Onay iki denetim satırı yazar** (`ChangeRequest.Approve` + `Startup.Update`):
  iz, değişikliğin portaldan mı doğrudan mı geldiğine bakmadan aynı biçimde sorgulanabilsin.
- **Reddedilen öneriler silinmez** — denetim izinin parçası ve portalın "önerim
  neden kabul edilmedi" ekranının kaynağı. Ret gerekçesi zorunlu (min. 10 karakter);
  gerekçesiz ret aynı önerinin tekrar gönderilmesiyle sonuçlanır. Onayda not zorunlu
  **değil** — zorunlu olsa "ok" gibi anlamsız notlar üretir.
- **`AuditLog`'un `User`'a FK'si yok**: kullanıcı silinse de iz ayakta kalsın diye.
  Aktör adları ayrı bir sorguda çözülür.
- **Denetim izi ve kullanıcı yönetimi ayrı politikalar** (`audit:view`, `users:manage`),
  ikisi bugün aynı role açık olsa da farklı gerekçelerle açık.
- **Parola özeti hiçbir yanıta ve hiçbir denetim gövdesine girmez.** `User.SetPassword`
  izi yalnızca e-postayı yazar.
- **Kendini kilitleme koruması:** kullanıcı kendi hesabını pasife alamaz, kendi rolünü düşüremez.
- **E-posta değiştirilemez** (update isteğinde alan yok): kimliğin çapası ve denetim
  izindeki eşleşme noktası.

---

## 3b. Başarı kayıtları ve doküman kararları (Faz 4)

- **Beş kayıt türü tek tabloda (TPH), C# tarafında beş sınıf.** Brief "serbest
  metin değil, alan bazlı veri modeli" istiyor; JSON sütunu ya da tek düz tablo
  ile tutarsız kayıt (mali yılı olmayan ciro, tutarı olmayan yatırım turu)
  kaydedilebilirdi. Ayrıştırıcı kolon: `Kind`.
- **Dış dünyaya tek gövde** (`AchievementWriteModel`) açılıyor, tür başına ayrı
  uç değil: form da tek (önce tür seçilir), beş uç beş kez aynı yetki ve onay
  mantığını kopyalamak olurdu. Tür–alan bağını doğrulayıcı kuruyor.
- **Kayıt türü güncellemede değiştirilemez (409).** TPH ayrıştırıcısı satırın
  kimliğinin parçası, yerinde değiştirilemiyor. Sessizce eskiyi silip yeni
  yazmak yerine açıkça reddediliyor; arayüz de tür seçimini kilitliyor.
- **Tutar maskelemesi `amountMasked` bayrağıyla taşınır.** Faz 2'deki `0 ₺`
  hatasının kaynağı bu ayrımın olmamasıydı: maskelenen tutar ile girilmemiş
  tutar aynı görünüyordu. Ödül gibi tutarsız kayıtlarda bayrak kalkmaz.
- **Doküman listesi yetkisiz role hiç açılmaz** (403), başarı kayıtlarının
  aksine "satırı göster, içeriği gizle" orta yolu yok: dosya adı tek başına
  ticari bilgi taşıyabiliyor (`2025_satis_sozlesmesi_ASELSAN.pdf`).
- **İçerik tipi istemciden alınmaz, uzantıdan türetilir.** İstemcinin
  Content-Type başlığına güvenmek, `.pdf` adlı dosyanın `text/html` olarak
  işaretlenip indirme anında tarayıcıda çalıştırılmasına açık kapı bırakırdı.
  İndirme ayrıca `X-Content-Type-Options: nosniff` ile dönüyor.
- **Her indirme denetim izine yazılır** (`Document.Download`). KVKK açısından
  "bu belgeyi kim indirdi" cevaplanabilir olmak zorunda.
- **Yükleme tek uç, iki yol.** Yetkili doğrudan kaydeder, girişim kullanıcısının
  dosyası depoya alınıp `ChangeRequest` üretir. Ayrı uçlar olsaydı "girişim
  hiçbir tabloya doğrudan yazmaz" kuralı iki yerde ayrı korunurdu. Yanıttaki
  `applied` alanı farkı arayüze anlatıyor.
- **Dosya onaydan önce depoya yazılır.** Yükleme tek seferlik; içeriği onaya
  kadar bekletecek yer yok (veritabanına koymak dosyayı veritabanına taşımak
  olurdu). Reddedildiğinde `ChangeRequestApplier.DiscardAsync` dosyayı siler —
  uygulama ile geri alma aynı bileşende, yan yana okunabilsin diye.
- **Soft delete edilen dokümanın dosyası silinmez:** kayıt yanlışlıkla
  kaldırılırsa geri alınabilsin ve denetim izindeki "indirildi" satırlarının
  işaret ettiği içerik ortadan kalkmasın. Kalıcı silme ayrı bir saklama
  politikası işi.
- **Onaydan geçen kayıt "doğrulanmış" olur**, doğrulayan olarak onaylayan
  yetkili yazılır. Portaldan gelen veri onaya kadar doğrulanmamış kalır.
- **Kültüre bağlı biçimlendirme yasak.** Tutar ve dosya boyutu metinleri
  sunucuda `tr-TR` kültürü **açıkça** verilerek üretiliyor; yerel ayara
  bırakılsa aynı öneri iki makinede iki farklı diff metni üretirdi.

---

## 3c. Karar destek, CSV ve AI kararları (Faz 5)

- **Agregat maskeleme kuralı `StartupVisibility` içine yazıldı** (`Aggregate`),
  rapora özel yeni bir yetki bileşeni açılmadı. Kural şu: Karar Verici *tek bir
  girişimin* tutarını göremez ama ekosistem *toplamını* görebilir — karar
  desteğin var oluş sebebi bu. Ayrı bir "rapor yetkisi" sınıfı, RBAC'ın üç tek
  noktasına dördüncüsünü eklerdi.
- **Sıralama listesinde satır maskelemesi kullanılır.** "En çok yatırım alan
  girişimler" kartı agregat değil satır gösterir; bu yüzden her satır
  `StartupVisibility.For` ile maskelenir. İlk sürüm agregat bayrağını satırlara
  uygulamış ve Karar Verici'ye tekil tutarları göstermişti — hata duman testinde
  yakalandı, `TopStartupSlice.Investment == null` artık tek anlama geliyor:
  yetki yok.
- **Pano kapsam filtresinden geçer.** Program Yöneticisi panoda da yalnızca
  kendi programlarının karnesini görür; sayılar `IStartupScope`'tan geçen aynı
  sorgudan üretilir, ayrı bir "rapor sorgusu" yok.
- **Grafik kütüphanesi yok.** İhtiyaç iki grafik türü (yatay çubuk, halka) ve
  bunlar ~60 satır SVG/CSS. Kütüphane, paket boyutunun yanında kendi tema ve
  erişilebilirlik varsayımlarını da getirirdi. Her grafik değerini **metin
  olarak da** basıyor: hem ekran okuyucu hem render doğrulaması SVG yolundan
  değil metinden okuyor.
- **Agregasyon bellekte yapılır.** Sorgu `OfType<T>()` ile TPH alt tiplerini
  ayrı ayrı çekiyor, gruplama C# tarafında. Sağlayıcıya özel çeviri riski
  (tip testi + `GroupBy` bileşimi) alınmıyor ve kural okunur kalıyor; veri
  hacmi ekosistem ölçeğinde (binler) bunu kaldırır.
- **CSV Excel'in Türkçe kurulumuna göre üretilir:** ayraç `;`, kodlama UTF-8 +
  BOM, sayılar `tr-TR`. Virgül ayraçla çakışırdı, BOM'suz dosyada Türkçe
  karakterler bozulurdu.
- **Maskeli hücre boş bırakılmaz, "yetkiniz yok" yazar.** Boş hücre "veri yok"
  demektir; ikisi karışırsa dışa aktarılan dosya yanlış bilgi taşır — ekrandaki
  `—` / 🔒 ayrımının dosyadaki karşılığı.
- **Formül enjeksiyonuna karşı hücre önekleme.** `=`, `+`, `-`, `@` ile başlayan
  hücreler `'` ile öneklenir; girişim adından gelen bir metin Excel'de formül
  olarak çalıştırılamamalı. Satır içi yeni satırlar da temizlenir.
- **Dışa aktarma denetim izine yazılır** (`Report.Export`, satır sayısı ve
  süzgeçle) ve 2000 satırla sınırlıdır. "Tüm ekosistemi kim indirdi"
  cevaplanabilir olmak zorunda.
- **AI zorunlu bağımlılık değil.** Anahtar yoksa `DisabledChatModel` kaydediliyor
  ve soru `OfflineAssistant` tarafından planlanıyor: anahtar kelimelerden araç
  çağrıları üretiliyor, yanıt **araç özetlerinin birleşimi** oluyor. Cümle
  üretilmediği için uydurma da üretilemiyor. Hangi yolun yanıtladığı arayüzde
  rozetle söyleniyor — demo, internet ya da kota olmadan da çalışır.
- **AI araçları REST ile aynı handler'ları sarar.** `AssistantToolbox` altı
  handler'ı çağırıyor; paralel bir veri yolu açılsaydı maskeleme ve kapsam iki
  yerde ayrı korunurdu. Aynı sebeple MCP araçları da aynı handler'lara bağlı.
- **Sistem istemi kullanıcının adını/e-postasını taşımaz**, yalnızca rol adını.
  Modelden gelen yanıt gövdesi hiçbir koşulda loglanmaz: içinde girişim verisi
  olabilir.
- **MCP elle yazıldı (JSON-RPC 2.0, 4 metot).** SDK'nın olgunluğu belirsizken
  `initialize`/`ping`/`tools/list`/`tools/call` toplamda küçük bir yüzey. Araç
  hatası protokol hatası değil: `result.isError = true` dönüyor ki model hatayı
  okuyup düzeltebilsin. Bildirimler (`id` taşımayan istekler) 204 döner.
- **Tohum verisi 32 girişime çıkarıldı ve boşluklar bilinçli:** dört girişim
  hiçbir programa bağlı değil, biri hiç başarı kaydı taşımıyor. Her satırı dolu
  bir veri kümesi, "kapsam dışı" ve "boş durum" ekranlarını demoda hiç
  göstermezdi.

---

## 3d. Renk paleti kararları

- **Palet uydurulmadı, T3KYS'ten alındı.** `t3kys.com` giriş sayfası ve
  `cdn.t3kys.com/static/new/assets/css/color.css` indirilip kurumsal renkler
  doğrudan okundu: `#E73A13` (birincil turuncu), `#D92C05` (üzerine gelme —
  kızarıp koyulaşıyor), `#FFB900` (sarı vurgu), `#FFA996` (soluk somon zemin).
  Arayüz ekosistemin geri kalanıyla aynı yerden geldiği izlenimini vermeli;
  "turuncuya benzer bir şey" yeterli değil. Kurumsal lacivert `#0A406C`
  bilinçli olarak alınmadı: beyaz/turuncu bir arayüzde tek başına duruyor.
- **İki aile var:** turuncu (`brand-*`, `frontend/src/index.css` içindeki
  `@theme` bloğu) ve sıcak nötr (Tailwind `stone`). Sarı ayrı bir jeton
  (`gold-*`) ama yalnızca ikincil vurgu: grafik serisi ve ödül rozeti. Soğuk
  gri (`slate`), mavi, mor, turkuaz arayüzde yok. Kalan tek yabancı ton
  yeşil/kırmızı: onay–ret ve "doğrulandı" semantiği renkle taşınıyor.
- **Ölçek "ton"a göre değil "iş"e göre.** `brand-300` (#FFA996) soluk zemin,
  `brand-500` (#E73A13) düğme/rozet zemini **beyaz yazıyla**, `brand-600`
  (#D92C05) üzerine gelme hâli, `brand-700` (#B52205) beyaz üstünde turuncu
  yazı (bağlantı, aktif sekme).
- **Beyaz yazı kontrastı: 4.2:1 — bilinerek kabul edildi.** `#E73A13`
  üzerinde beyaz, WCAG AA'nın küçük yazı için istediği 4.5:1'in hemen altında
  (üzerine gelince 4.9:1'e çıkıyor). Bir tık koyu bir turuncu (#DE3812) eşiği
  geçiyordu ama T3KYS'in kendi düğmesi tam olarak bu renk; kurumsal kimlikle
  birebir aynı olmak tercih edildi. Sarıda beyaz yazı **kullanılmıyor**
  (1.7:1); `gold-500` üstüne koyu mürekkep geliyor (10.2:1).
- **`dark:` yardımcıları işletim sistemi temasını dinlemiyor.** `index.css`
  içinde `@custom-variant dark (&:where(.dark, .dark *))` tanımlı; yani karanlık
  tema ancak kökte `.dark` sınıfı varsa açılıyor, ki hiçbir yerde eklenmiyor.
  Sebep: kurumsal kimlik beyaz zemine kurulu, ama tarayıcısı koyu temada olan
  kullanıcı siyah bir arayüz görüyordu. Sınıflar kodda bırakıldı — gerçek bir
  tema düğmesi eklenirse `<html class="dark">` demek yetecek.
- **Hover'ın rengi tek yerden gelir.** Birincil düğme turuncudan kırmızıya
  koyulur (T3KYS'teki `.btn-orange:hover` davranışı); çerçeveli düğme turuncu
  dolguya geçer; hayalet düğme, sekme ve menü bağlantısı soluk turuncu zemin +
  `brand-700` yazıya döner. Nötr griye açılan tek bir hover kalmadı.
- **Başlıklar kahverengi değil sıcak siyah.** Turuncu ölçeğinin koyu ucu
  kaçınılmaz olarak kahveye düşüyor; başlık ve KPI rakamları `stone-900`.
- **Grafik serileri komşusundan açıklıkla ayrışır.** Seriler kurumsal turuncu
  ile kurumsal sarı arasında geziniyor, sonuna iki nötr ekleniyor. Sıralama
  rastgele değil: halka grafikte bitişik iki dilim hep farklı açıklıkta.
- **Varsayılan rozet gri değil soluk turuncu.** Teknoloji etiketleri sayfanın
  en kalabalık öğesi; gri kaldıklarında palet turuncu değil gri okunuyordu.
- **Durum rozetleri aynı aileden ama dolgu/çerçeve farkıyla ayrışır.**
  "Mezun" soluk turuncu, "Çıkış yaptı" çerçeveli beyaz, "Satın alındı" koyu
  nötr: renk körlüğünde de ayırt ediliyor.
- **Palet kararı gözle verilir.** Kontrast oranı hesaplanabilir ama "turuncu
  ağırlıklı mı" hesaplanamaz: her tur headless Chrome ekran görüntüsüyle
  bakıldı. Birinci turda logo kutusu koyu kahveydi, ikinci turda grafikler
  kahverengiydi, üçüncü turda tarayıcı koyu temada olduğu için sayfa siyahtı.
  Ekran görüntüsü artık `Emulation.setEmulatedMedia` ile **koyu tema
  taklit edilerek** alınıyor — beyaz zeminin her koşulda beyaz kaldığı
  doğrulanabilsin.

---

## 3e. Denetim Dalga 0 kararları (ürün denetimi düzeltmeleri)

Kaynak: [Denetim_Duzeltme_Plani.md](Denetim_Duzeltme_Plani.md) Dalga 0.

- **Girişim yazma yolları arayüze portaldaki formlar yeniden kullanılarak açıldı.**
  Uçlar (`POST/PUT/DELETE /api/startups`, `.../team`, `POST /api/participations`)
  Faz 3'ten beri hazırdı ama hiçbir ekran çağırmıyordu: girişimi sisteme
  yalnızca tohumlayıcı ekleyebiliyordu. Portalın `ProfileForm`/`MemberForm`
  bileşenleri `frontend/src/features/startups/` altına taşındı ve
  `AchievementSection`'daki **`mode` deseni** uygulandı: `proposal` →
  `ChangeRequest`, `direct` → tabloya yazma. İkinci bir form yazmak, aynı 13 alan
  için iki doğrulama ve iki hata mesajı demekti.
- **`direct` kip yalnızca `canManageStartups` doğruyken render ediliyor**, ama bu
  bir güvenlik önlemi değil: sunucu tarafı `Policies.ManageStartups` + handler
  içindeki ikinci kontrol yerinde duruyor. Girişim kullanıcısı hâlâ hiçbir
  tabloya doğrudan yazmıyor.
- **Yeni girişim kaydettikten sonra kart açılmıyor, katılım adımı açılıyor.**
  Program Yöneticisi'nin kapsamı "programlarımdan geçmiş girişimler" olarak
  tanımlı (`StartupScope`), dolayısıyla yeni kayıt bir program dönemine
  bağlanana kadar **kendi kapsamına girmiyor** — kartına gitmek 404 verirdi.
  `StartupsPage` bu yüzden kaydetmenin ardından `ParticipationForm`'u açıp
  kuralı ekranda yazıyor. Süper Yönetici'de bu adım yok, doğrudan karta gidiyor.
  Alternatif (girişime `CreatedByUserId` ekleyip kapsamı gevşetmek) reddedildi:
  kapsam tanımını veri modeliyle bulanıklaştırıyordu.
- **Maskeleme yalnızca okuma süzgeci değil, yazma yolunda da tutulur.** Tam
  değiştirmeli `PUT` maskeli alanı istemciye `null` gönderdiği için geri
  yazarken **siliyordu**: vergi numarasını göremeyen Program Yöneticisi'nin
  kaydettiği her düzenleme numarayı boşaltırdı ve denetim izi bunu "kullanıcı
  sildi" diye kaydederdi. `StartupWriteModel.ApplyTo(startup, visibility)`
  göremediği alanı koruyor; arayüz `direct` kipte o alanı hiç göstermiyor ve
  gerekçesini yazıyor. Alanı forma koyup "değiştirme" demek yeterli değildi:
  gövde yine null taşıyacaktı. Kural aynı yerde (`StartupVisibility`) duruyor,
  RBAC'ın üç tek noktasına dördüncüsü eklenmedi.
- **Girişim silme düğmesi yalnızca Süper Yönetici'de.** Politika
  `ManageStartups` Program Yöneticisi'ni de kapsıyor, ama silmenin kapsamı daha
  dar ve kontrol handler'da; arayüz bu daha dar kuralı yansıtıyor.
- **Hız sınırı kovası IP + e-posta ile bölümlendi.** Bölüm anahtarsız tek kova,
  bir hesaba yapılan 10 yanlış denemenin **tüm** kullanıcıların girişini 429'a
  düşürmesi anlamına geliyordu. E-posta gövdeden okunuyor; hız sınırlayıcının
  bölüm anahtarı üreten geri çağrısı **eşzamanlı** olduğu için gövde
  sınırlayıcıdan önce ayrı bir ara katmanda (`AuthRateLimit.CaptureLoginEmail`)
  tamponlanıp başa alınıyor. Yalnızca IP ile bölümlemek yetmezdi: demo ve tüm
  betikler aynı makineden, yani tek IP'den giriş yapıyor. AI kovası kullanıcı
  kimliğine göre bölümlü; reddedilen yanıt `Retry-After` taşıyor.
- **AI anahtarı `Ai` bölümünden okunuyor** (`T3_Ai__ApiKey`). `.env` içindeki
  ad `T3_Anthropic__ApiKey`'di, yani anahtar hiç okunmuyordu ve asistan sessizce
  yerel plana düşüyordu. Sessiz yedek mekanizma bir daha yanıltmasın diye açılışta
  log: anahtar yoksa `LogWarning`, varsa model adı `LogInformation`.
- **Bilinmeyen adres panoya yönlendirilmiyor, 404 gösteriyor.** Yönlendirme,
  paylaşılan bir bağlantıdaki yazım hatasını "pano zaten burası" gibi
  gösteriyordu. Oturum açıkken kabuk içinde (menü elde kalsın), kapalıyken
  çıplak render ediliyor. `/girisimler/99999` ile geçerli-ama-yok GUID artık
  aynı ifadeyi veriyor: kullanıcı için ikisi aynı durum.
- **Sekme başlığı sayfanın H1'iyle aynı** (`useDocumentTitle`). Üç girişim
  kartını üç sekmede açan yönetici hangisinin hangisi olduğunu ayırt edemiyordu;
  55 render'ın hepsinde başlık "frontend" yazıyordu.

---

## 3f. Denetim Dalga 1 kararları (MVP ve KVKK bütünlüğü)

Kaynak: [Denetim_Duzeltme_Plani.md](Denetim_Duzeltme_Plani.md) Dalga 1.

- **Program yetkisi ikiye ayrıldı: `ManagePrograms` (tanım) ve
  `ManageProgramTerms` (dönem/katılım).** Tek politika kullanmak kolaydı ama
  yanlış olurdu: program listesi aynı zamanda Program Yöneticisi'nin **yetki
  kapsamının tanımı** (`StartupScope` bu tablodan besleniyor), dolayısıyla
  program oluşturabilen bir Program Yöneticisi kendi kapsamını kendisi
  büyütebilir ve RBAC anlamsızlaşır. Dönem açmak ise günlük operasyon ve kapsamı
  büyütmüyor — o yüzden Program Yöneticisi'ne açık, ama satır düzeyinde kendi
  programıyla sınırlı.
- **Kapsam kontrolü tek noktada: `ProgramAccessGuard`.** `AddParticipation`
  aynı kontrolü kendi içinde yazmıştı; dört yeni dilim eklenince kural beş yerde
  olacaktı. Muhafız `StartupEditGuard`/`UserAdminGuard` ile aynı gerekçeyle
  ayrı bir bileşen (use-case değil, ortak ön kontrol) ve elle DI'a kaydediliyor.
- **Katılımı olan dönem kapatılamıyor (409), program kapatılınca ise zincir
  yürüyor.** Asimetri bilinçli: dönem silmek Program Yöneticisi'ne açık ve tek
  tıkla başkasının gelişim yolculuğunu boşaltabilen bir işlem olmamalı; programı
  kapatmak yalnızca Süper Yönetici'de ve kaç dönem, kaç katılım, kaç yönetici
  ataması kapandığı yanıtta raporlanıyor (girişim silmedeki desenin aynısı).
- **Katılım taşınmıyor, kaldırılıp yeniden ekleniyor.** `UpdateParticipation`
  yalnızca durum/tarih/not kabul ediyor; girişim ve dönem alanları gövdede yok.
  Dönemi değiştirebilen bir "düzenle" ucu, gelişim yolculuğundaki tarihi tek
  istekle yeniden yazmak olurdu.
- **Sıfırlama jetonunun kendisi değil SHA-256 özeti saklanıyor.** Yedek
  dosyasını ya da veritabanını okuyan biri hiçbir hesabın şifresini
  sıfırlayamamalı. Özet için PBKDF2 kullanılmadı: jeton 256 bitlik kriptografik
  rastgele değer, sözlük saldırısına konu değil — yavaş türetme koruma değil
  yalnızca gecikme olurdu. Şifreler bundan farklı ve PBKDF2 ile saklanmaya
  devam ediyor.
- **Jeton HTTP yanıtında hiç dönmüyor; e-posta diske yazılıyor.** "Geliştirmede
  kolaylık olsun" diye jetonu yanıta koymak, sıfırlama isteyen herkese hesabı
  devretmek demekti. `IEmailSender` arkasındaki `FileOutboxEmailSender`
  e-postayı sunucunun diskindeki kutuya yazıyor: ağdan erişilemez ama uçtan uca
  doğrulanabilir (`render_faz7.py` jetonu o dosyadan okuyor). SMTP geldiğinde
  yalnızca DI satırı değişir.
- **Sıfırlama bağlantısı `Host` başlığından değil yapılandırmadan kuruluyor**
  (`EmailOptions.AppBaseUrl`, `IResetLinkBuilder`). İstekten türetmek,
  saldırganın kendi alan adına giden bir sıfırlama bağlantısı ürettirmesine izin
  verirdi.
- **`forgot-password` adresin kayıtlı olup olmadığını söylemiyor** — giriş
  ucundaki aynı kural. Ayrım yalnızca denetim izine yazılıyor ("kayıtlı olmayan
  adres" / "hesap pasif"), çünkü ize yalnızca Süper Yönetici erişiyor ve
  güvenlik incelemesinin sorusu tam olarak bu.
- **Yöneticinin attığı şifre geçici: `User.MustChangePassword`.** Bayrak
  `CreateUser` ve `SetUserPassword` ile kalkıyor, kullanıcı kendi şifresini
  belirlediğinde (`change-password` ya da `reset-password`) düşüyor. Sunucu
  tarafında ekran kilidi yok — kilit arayüzde (`RequireAuth`), çünkü yöneticinin
  bildiği şifreyle yapılabilecek her şey zaten o rolün yetkisi kadar; amaç
  yetkiyi kısmak değil **şifrenin ikinci sahibini** ortadan kaldırmak.
- **Erişim jetonu 15 dakika değil bir iş günü (480 dakika).** Doğru çözüm
  yenileme jetonunu HttpOnly çereze koymak, ama o değişiklik jetonun tamamını
  çereze taşımaya ve tek origin kararına bağlı (Dalga 2). O gelene kadar
  bilinçli takas: tek jeton, iş günü kadar ömür, dolduğunda giriş ekranında
  gerekçe. Gerekçesiz atılma en sinir bozucu hataydı.
- **Ağ hatası `ApiError(0)` olarak normalleştiriliyor ve oturumu düşürmüyor.**
  `fetch` reddi eskiden 401 gibi ele alınıyordu: API kapanınca kullanıcı
  oturumdan atılıp ham "Failed to fetch" görüyordu. Artık `AuthProvider`
  yalnızca 401'de jetonu siliyor, status 0'da "sunucuya ulaşılamıyor" durumu ve
  yeniden deneme düğmesi gösteriliyor. Doğrulama, Chrome'un
  `Network.setBlockedURLs` komutuyla **yalnızca `/api/*`** isteklerini
  engelliyor; tüm ağı kesmek uygulamanın kendisini de indirilemez yapıp
  Chrome'un hata sayfasını sınamak olurdu.
- **Denetim izinde aktör ve rol artık nullable.** Başarısız girişte kimlik
  doğrulanmamıştır; eski kod aktörü `Guid.Empty`, rolü de `DecisionMaker` diye
  yazıyordu — yani iz, hiç var olmayan bir rolü olay yapmış gibi gösteriyordu.
  Ekranda üç durum ayrı: "(kimlik doğrulanmadı)", "(kayıt yok: …)" ve gerçek ad.
- **Başarısız denemede e-posta maskeli saklanıyor** (`MaskedEmail`,
  `k***@alan.test`). Alan adı korunuyor çünkü incelemenin sorusu "hangi kurumdan
  deniyorlar"; yerel kısım düşüyor çünkü iz, saldırganın denediği ham adreslerin
  listesine dönüşürse kendisi bir sızıntı kaynağı olur.
- **Hız sınırı kilidi pencere başına tek satır yazıyor.** Reddedilen istek
  sayısı sınırsız; her redde satır açmak saldırganın izi şişirip kendi izini
  boğmasına ya da diski doldurmasına izin verirdi. Bellek önbelleği burada bir
  hız iyileştirmesi değil güvenlik önlemi ve `SizeLimit` bilinçli: anahtar
  denenen e-postadan türüyor, sınırsız sözlük bellek şişirme kapısı olurdu.
- **`IClientContext` kimlikten ayrı arayüz.** IP ve istemci bilgisi "kim"
  değil "nereden" sorusunun cevabı; `ICurrentUser`'a eklemek iki farklı soruyu
  tek arayüzde toplamak olurdu. MCP gibi HTTP dışı yollarda boş kalması normal.
- **KVKK metinleri kod içinde bileşen, veritabanında içerik değil.** Sürüm
  kontrolünde tutulmaları versiyonlanabilir olmalarını sağlıyor (metin
  değişikliği bir commit'tir) ve giriş yapmadan açılabilmeleri için
  yetkilendirme dışında duruyorlar. Görünür **taslak** uyarısı bilinçli:
  onaylanmamış bir aydınlatma metnini onaylanmış gibi göstermek yükümlülüğü
  karşılamaz, karşılanmış gibi gösterir.
- **`RequirePermission` "herhangi biri" (anyOf) semantiğine geçti.** Karar
  Verici'de `canReviewApprovals` ve `mustSubmitForApproval` ikisi de false
  olduğu için `/onaylar` sonsuza dek boş kalıyordu; tek izinli koruma bu durumu
  ifade edemiyordu.

---

## 4. Ortam tuzakları — tekrar çarpılacak olanlar

### Faz 0

- **Postgres 5433'te çalışır**, 5432'de değil: bu makinede zaten yerel bir Postgres
  127.0.0.1:5432'ye bağlı.
- **`.env` değerleri çift tırnaklı kalmalı.** Bağlantı dizesi `;` içeriyor;
  tırnaksız haliyle `set -a && . ../.env` değişkeni sessizce **atamaz**, kabuk `;`'yi
  komut ayırıcı sayar.
- **Global `dotnet-ef` 10.x, net8.0 projelerini okuyamaz** ("Unable to retrieve project
  metadata"). Proje-yerel araç manifesti dotnet-ef 8.0.10'u sabitliyor: `dotnet tool restore`
  çalıştırın ve `--project`/`--startup-project` bayraklarını **mutlak yol** olarak
  `T3.Infrastructure`'a verin (orada `IDesignTimeDbContextFactory` var, migration API host'unu açmaz).
- Frontend `tsconfig.app.json`'da `erasableSyntaxOnly` ve TypeScript 6 var:
  **constructor parametre özelliği kullanılamaz**, `baseUrl` kullanımdan kalktı (yalnız `paths`).
- **EF Core TPH:** her özellik, onu **tanımlayan** tipin konfigürasyonunda ayarlanır.
  Türetilmiş bir özelliği taban builder'da ayarlamak shadow-property hatası verir.
  Kardeş tiplerin paylaştığı özellikler ortak bir soyut taban ister —
  `PeriodicMoneyAchievement` bu yüzden var, yoksa EF çift `FiscalYear` kolonu üretiyor.

### Faz 2

- **Application katmanı sağlayıcıya özel EF API'si kullanamaz — tasarım gereği.**
  `EF.Functions.ILike` Npgsql'de, `AsSplitQuery` EF Relational'da; ikisi de
  `T3.Application`'dan referanslı değil. Sağlayıcıdan bağımsız karşılıklarını kullanın
  (`string.Contains`, `%`/`_` kaçışını da o yapıyor). "Düzeltmek" için Application'a
  Npgsql referansı **eklemeyin**. Aynı gerekçeyle `ExecuteUpdate`/`ExecuteDelete` de yok —
  soft delete zinciri elle yürünüyor.
- **Türkçe noktalı İ, harf duyarsız aramayı bozuyor.** `"İstanbul".ToLowerInvariant()`
  U+0130'ı **değiştirmiyor** (bu çalışma zamanında doğrulandı, 8 karakter), PostgreSQL
  `lower()` ise düz `i`'ye çeviriyor. Yani `col.ToLower() == param.ToLowerInvariant()`
  sessizce hiç eşleşmiyor. Arama/karşılaştırma terimleri her zaman `SearchText.Normalize`
  üzerinden geçer (önce İ→I, sonra küçültme). `SearchTextTests` bunu kilitliyor.
- **`launchSettings.json`, `dotnet run` altında `ASPNETCORE_URLS`'i yener.** Üretilen
  profil 5117'yi sabitlemiş ve README'deki 5080'i sessizce yok saymıştı. Profil artık
  5080'e sabit; port "bir türlü değişmiyorsa" önce oraya bakın.
- **Bir DTO alanı iki farklı sebeple null olabiliyorsa açık bir bayrak taşıyın.**
  `TotalInvestment: null` hem "yatırım turu yok" hem "yetkiniz yok" demekti; liste
  yetkili kullanıcıya kilit, herkese `0 ₺` gösteriyordu. `AmountsVisible` eklendi ve
  para sözlüklerinde `GetValueOrDefault` kullanılmıyor — "kayıt yok"u `0`'a çeviriyor.

### Faz 4

- **Minimal API'de `IFormFile` alan uç `.DisableAntiforgery()` ister.** .NET 8'de
  multipart uçlar varsayılan olarak antiforgery jetonu bekliyor ve jeton
  gelmeyince istek doğrulama hatasıyla düşüyor. API çerezle değil Bearer
  jetonuyla kimlik doğruladığı için CSRF yüzeyi yok; açık bırakmak yalnızca
  anlaşılmaz 400'ler üretirdi.
- **`ToLowerInvariant` etiket üretirken de tuzak.** "İhracat" invariant kültürde
  bozuk küçülüyor (Faz 2'deki arama sorununun aynısı). Sunucuda üretilen
  başlıklarda etiketler küçültülmeden kullanılıyor.
- **`string.Format`/interpolasyon sunucunun yerel ayarını kullanır.** Tutar ve
  dosya boyutu metinleri `CultureInfo.GetCultureInfo("tr-TR")` **açıkça**
  verilerek üretiliyor; aksi hâlde aynı öneri iki makinede iki farklı diff
  metni üretir ve testler makineye bağlı hâle gelir.
- **Doğrulama betiklerine tohum sayısı sabit yazmayın.** `e2e_faz3.py` "8 onay
  isteği" diye sabitlemişti; Faz 4 kuyruğa üç öneri ekleyince betik gerçek bir
  hata olmadığı hâlde kırmızıya döndü. Sabit sayı yerine değişmeyen ilişki
  doğrulanıyor (durum sayılarının toplamı == toplam).
- **React denetimli girdiye `value` atamak yetmez.** Render betiği formu
  doldururken yerel `value` setter'ını çağırıp `input`/`change` olaylarını elle
  yaymak zorunda; doğrudan atama React'in değer takipçisini atlıyor ve durum
  güncellenmiyor.
- **Açılırın ekranda seçili görünen değeri gövdeye de yazılmalı.** Başarı kaydı
  formunda tür değişince model sıfırlanıyordu; kullanıcı "TÜBİTAK" seçili görüp
  hiç dokunmadığında gövdede `institution: null` gidiyor ve sunucu 400
  döndürüyordu. Varsayılanlar artık türe göre modele de yazılıyor —
  `render_faz4.py` bu senaryoyu açılıra **dokunmadan** doğruluyor.
- **Render betikleri çalıştırma sırasına bağlı kalmasın.** `render_faz4.py`
  ihtiyacı olan bekleyen öneriyi kuyrukta bulamazsa kendisi gönderiyor; aksi
  hâlde "önce uçtan uca betiği çalıştır" sessiz bir ön koşula dönüşüyordu.

### Faz 5

- **Bileşen kalan öznitelikleri geçirmezse `data-testid` sessizce düşer.**
  `Card` yalnızca `children` ve `className` alıyordu; panellere yazılan
  `data-testid` DOM'a hiç ulaşmıyor, render betiği de kartı bulamıyordu. Kart
  artık kalan öznitelikleri `div`'e geçiriyor.
- **`innerText` CSS'in dönüştürdüğü metni döndürür.** `uppercase` sınıfı taşıyan
  başlıklar metinde `KAYNAKLAR`/`DAYANAK` olarak okunur; "Kaynaklar" bekleyen
  bir `wait_for` sessizce zaman aşımına uğrar. Bekleme metne değil öğeye
  (`data-testid`) bağlanmalı.
- **`response.text()` BOM'u yutar.** CSV'nin BOM ile başladığını doğrulamak için
  `arrayBuffer()` üzerinden ham baytlara bakmak gerekiyor; çözülmüş metinde BOM
  görünmez.
- **`"0 ₺" in text` yanlış alarm verir.** "649.150.000 ₺" de bu metni içeriyor.
  Maskelemenin `0 ₺` göstermediğini doğrulayan kontrol, sıfırın tek başına
  durduğunu aramalı (`(?<![\d.,])0 ₺`).
- **Türkçe anahtar kelime tabloları `SearchText.Normalize` biçiminde yazılır.**
  Normalize edilmiş metin ğ/ş/ı/ç/ö/ü harflerini koruyor; tabloya ASCII karşılık
  ("saglik") yazılırsa kelime hiç eşleşmez. Yerel planlayıcıda her iki biçim de
  duruyor.
- **`Enum<T>` adlı bir yardımcı `System.Enum`'u gölgeler** (CS0119). Genel adlar
  yerine `EnumOf<T>`. Aynı dosyada `cond ? null : x.Prop` üçlüsü de tip
  çıkaramaz (CS0173) — hedef tip açıkça yazılmalı (`DateOnly? first = …`).

### Dalga 0 (denetim düzeltmeleri)

- **`min-w-0` olmadan grid/flex öğesi kendi min-content'i kadar yer ister.**
  375 px'te `/pano` 394 px, `/girisimler` 386 px genişliğe taşıyordu. Suçlu
  düğme ya da grafik değildi: pano grafikleri tek sütunlu bir grid'in *aynı*
  izinde duruyor ve iz, en geniş öğenin min-content'i kadar büyüyor — "En çok
  yatırım alan girişimler" listesindeki `truncate` bağlantı (nowrap olduğu için
  min-content'i tüm metin genişliği) izi 378 px'e çıkarıyordu. Grid öğesine
  `min-w-0` verilmeden `truncate` hiç kısalmıyor.
- **`/onaylar` taşmasının sebebi ayrıydı:** öneri satırı hedef etiketinde doküman
  adı taşıyor ve alt tire içeren uzun dosya adı bölünmüyordu → `break-words`.
- **Taşan öğeyi gözle aramayın, ikiye bölerek bulun.** İşe yarayan teşhis:
  öğeleri tek tek `display:none` yapıp `documentElement.scrollWidth`'in düştüğü
  dalı izlemek; ve şüpheli öğeye geçici olarak `width:min-content` verip
  `offsetWidth` okumak. `getBoundingClientRect().right > innerWidth` taraması
  bu iki durumu da **bulamıyordu**, çünkü taşan öğe kendi kutusunun içinde
  kalıyor.
- **`cdp.Browser.goto(url, wait_for="")` her çağrıda 20 sn zaman aşımına düşer.**
  Boş dize `None` değil: ilk koşul (`wait_for is None`) sağlanmaz, ikincisi
  (`wait_for and …`) boş dizeyi hiç doğrulamaz. Beklenen metni verin ya da
  `wait_for=None` yazın; 27 rotalı bir döngüde fark 18 dakika.
- **`Input` bileşenine `className` geçmek işe yaramaz** — bileşen kendi
  `className`'ini `{...props}`'tan sonra yazıyor, dışarıdan geleni eziyor.
  Sütun genişliği gibi düzen kararları sarmalayıcı `div`'e yazılır.
- **Çok satırlı `import { … } from` bloğu olan dosyaya "son import satırından
  sonra ekle" mantığıyla satır enjekte etmeyin**: blok ortasına düşüyor ve
  TS1003 veriyor.

### Dalga 1 (denetim düzeltmeleri)

- **`Network.emulateNetworkConditions(offline=True)` ağ hatasını sınamıyor**,
  belgeyi de indirilemez yapıyor: ekranda Chrome'un `ERR_INTERNET_DISCONNECTED`
  sayfası çıkıyor ve test kendi kurgusunu ölçüyor. Doğrusu
  `Network.setBlockedURLs(urls=["*/api/*"])` — uygulama açık kalıyor, yalnızca
  API çağrıları düşüyor.
- **Sekme düğmesi ile menü bağlantısı aynı metni taşıyor** ("Programlar").
  `cdp.click_text` `button, a` arasında belge sırasına göre ilkini bulduğu için
  sekme yerine menüyü tıklıyor ve betik sessizce yanlış sayfada devam ediyor —
  hatta "isim ekranda mı" kontrolü **yeşil kalıyor**, çünkü program adı
  `/programlar` sayfasında da var. Sekme tıklamaları yalnızca `button` arayan
  ayrı bir yardımcıyla yapılıyor.
- **Aynı etiketli düğme ekranda birden fazla** (her program kartında bir "Dönem
  ekle"). Doğrulama, ilgili kaydın adını taşıyan öğeden DOM'da yukarı yürüyüp o
  kartın içindeki düğmeyi buluyor (`click_in_card`).
- **`IAppDbContext`'e yeni `DbSet` eklemek test derlemesini kırar:**
  `UnreachableDbContext` arayüzü elle uyguluyor. Kırılma bilinçli — o sınıf
  "bu karar veritabanına gitmemeli" beklentisini kanıtlıyor.
- **Zorunlu ilişkinin iki tarafı farklı sorgu süzgeci görürse EF uyarı veriyor.**
  `User` soft-delete süzgeçli, `PasswordResetToken` değildi; eşleşen süzgeç
  (`HasQueryFilter(t => !t.User.IsDeleted)`) eklendi — silinmiş kullanıcının
  jetonu geçerli sayılsa kapatılmış hesap sıfırlama bağlantısıyla geri açılırdı.
- **Gövdesinde e-posta olmayan istekler tek hız sınırı kovasında toplanıyor**
  (`IP|-`): `reset-password` ve `change-password` bölüm anahtarı için e-posta
  taşımıyor. Kimlik doğrulamalı ya da jeton taşıyan uçlar olduğu için kabul
  edilebilir, ama doğrulama betiği bu kovayı hesaba katmak zorunda.
- **`dotnet run --no-launch-profile` ortamı `Production` yapıyor**: tohum verisi
  yüklenmiyor ve Swagger kapanıyor. Betikleri koşturmak için
  `ASPNETCORE_ENVIRONMENT=Development` açıkça verilmeli.

### Kabuk / araç tuzakları

- Bash aracı, ortam bloğu fish dese de **zsh** çalıştırır. `for … end` çalışmaz;
  POSIX `while`/`for … do … done` kullanın.
- **`pkill -f 'T3.Api'` kendi kabuğunu öldürür** (desen, komutu çalıştıran satırla da
  eşleşiyor). `pkill -f 'T3[.]Api'` yazın — ama bu köşeli parantez hilesi yalnızca
  komut satırının **geri kalanında** `T3.Api` geçmiyorsa işe yarar. `pkill … && …
  dotnet run --project src/T3.Api/…` gibi zincirlenmiş bir satır yine kendini
  öldürür (çıkış kodu 144, sonraki komutlar hiç çalışmaz). Süreç öldürmeyi ayrı
  bir çağrıda yapın.
- Python E2E betiği `http.client` kullanıyorsa istek satırı ASCII kodlanır; sorgu
  dizesinde Türkçe karakter varsa `urllib.parse.quote(path, safe="/?&=,")` şart.
- **Login hız sınırı dakikada 10 istek.** Çok rollü E2E betiği arka arkaya iki kez
  çalıştırılırsa 429 gelir; sınır API süreci yeniden başlayınca sıfırlanır. Temiz koşu
  için: `docker compose down -v` → `up -d postgres` → `dotnet ef database update` → API'yi yeniden başlat.

---

## 5. Doğrulama alışkanlığı

Faz 1–2'de işe yarayan ve tekrarlanması gereken sıra:

1. `dotnet build && dotnet test` (Application katmanında saf birim testleri —
   RBAC, maskeleme, sayfalama, harf duyarlılığı, gövde doğrulama).
2. Gerçek API'ye **tüm rollerle** vuran Python E2E betiği: kapsam daralması,
   maskeleme, yazma yetkisi reddi, 404/403 ayrımı.
3. Gerçek tarayıcıda render. index.html'in 200 dönmesi uygulamanın açıldığını
   **göstermez**; Faz 2'deki `0 ₺` maskeleme hatasını API testleri geçmiş, yalnızca
   bu adım yakalamıştı.

Betikler repoda: [../scripts/](../scripts/) — faz başına bir uçtan uca, bir
render betiği (`e2e_faz3/4/5.py`, `render_faz3/4/5.py`), denetim Dalga 0
düzeltmelerini kapsayan `render_faz6.py` ve hepsinin kullandığı `cdp.py`.
`render_faz6.py` yeni yazma ekranlarını, sekme başlığını, 404'ü, üç genişlikte
mobil taşmayı ve giriş hız sınırı bölümlemesini doğruluyor; veriyi
**değiştirdiği** için en son turda çalışır. Çalıştırma sırası ve veritabanı sıfırlama kuralı
[../scripts/README.md](../scripts/README.md) içinde; uçtan uca betikler veriyi
değiştirdiği için her turdan önce sıfırlama şart.

Render adımıyla ilgili iki tuzak:

- **`--dump-dom` tek başına yetmiyor.** Jeton `localStorage`'da duruyor ve
  dışarıdan yazılamıyor, yani yalnızca giriş ekranı görüntülenebiliyor. Çözüm
  `cdp.py`: Chrome'u `--remote-debugging-port` ile açıp DevTools Protocol
  üzerinden önce jetonu yerleştiren, sonra hedef rotayı açan bağımlılıksız
  (saf soket üzerinden WebSocket) küçük bir istemci.
- **`innerText` bitişik satır içi öğeler arasına boşluk koymaz.** Ekranda
  "Bekleyen (4)" görünen sekme metinde `Bekleyen(4)` olarak okunur; render
  iddiaları buna göre yazılmalı.

Testlerde işe yarayan bir kalıp: rol tabanlı kararların veritabanına hiç gitmediğini
kanıtlayan `UnreachableDbContext` — her `DbSet` erişimi istisna fırlatıyor.

---

## 6. Faz durumu

| Faz | Durum | Kapsam |
|---|---|---|
| Faz 0 (`faf2ed7`) | ✅ | Çözüm iskeleti, 12 tablolu veri modeli, Docker Postgres, JWT altyapısı, Swagger, sağlık uçları, React iskeleti. Uçtan uca doğrulandı: tarayıcı → Vite (5173) → API (5080) → PostgreSQL (5433). |
| Faz 1 + 2 (`83d7ccf`) | ✅ | MVP #1 (merkezi girişim kartı) ve MVP #2 (gelişim yolculuğu). Login + JWT, `/api/me`, rol politikaları, `IStartupScope`, `StartupVisibility`, arama/kart/kronoloji, ekip CRUD, program listesi + katılım, denetim izi, kod tabanlı tohumlayıcı. |
| Faz 3 | ✅ | `ChangeRequest` onay akışı (MVP #3), girişim portalı, onay kuyruğu + before/after diff, denetim izi ucu, Faz 1'den ertelenen kullanıcı yönetimi CRUD'u ve soft delete zinciri. Doğrulama: 91 birim testi, 80 uçtan uca kontrol, 32 render kontrolü. |
| Faz 4 | ✅ | Başarı/finans kayıtları (MVP #4, TPH ile beş tip), doküman yükleme/indirme, tutar ve doküman maskelemesi, onay akışının yeni hedef türleri. Doğrulama: 129 birim testi, 123 uçtan uca kontrol, 41 render kontrolü. |
| Faz 5 | ✅ | Ekosistem panosu ve grafikler, CSV dışa aktarma, MCP sunucusu, AI karar destek paneli ve yönetici özeti, 32 girişimlik gerçekçi tohum verisi. Doğrulama: 153 birim testi, 103 uçtan uca kontrol, 53 render kontrolü. |

**Faz 1'den bilinçli ertelenen:** kullanıcı yönetimi CRUD'u — girişim kartı buna
ihtiyaç duymadığı için onay akışıyla birlikte Faz 3'e alındı.

**Faz 0'dan taşınan açık iş (Faz 3'te kapatıldı):** soft delete yalnızca sözleşme
düzeyinde zincirleniyordu; `ProgramParticipation`, `ProgramTerm`, `Milestone`,
`UserProgramAssignment` ve `ChangeRequest` `ISoftDelete` uygular ama ebeveyn pasife
alındığında çocukları işaretleyen kod yoktu. Sorgu süzgeçleri yalnızca *okumayı*
daralttığı için zincir `DeleteStartupHandler` içinde elle yürünüyor.
