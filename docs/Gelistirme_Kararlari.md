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

- **KVKK onayı giriş ekranında kapı, izde kayıt (25 Ağustos).** Üç karar bir
  arada:
  1. *Kutu girişi kilitliyor.* "Giriş yaparak kabul etmiş olursunuz" kalıbı
     hukuken beyan değil varsayım; kutu işaretlenmeden düğme açılmıyor ve
     nedeni ekranda yazıyor (gerekçesiz devre dışı düğme "form bozuk" gibi
     okunuyor).
  2. *Onay sürüme bağlı* (`KVKK_ONAY_SURUMU`, `features/auth/kvkkConsent.ts`).
     Metin değişip sürüm artınca aynı kişiye yeniden soruluyor. Sürümsüz bir
     bayrak, güncellenmiş metne eski onayı saymak olurdu.
  3. *Kanıt sunucuda.* İstemcideki `localStorage` kaydı yalnızca "bu tarayıcıda
     bir daha sormayalım" kolaylığı — kullanıcı silebilir. Giriş isteği sürümü
     taşıyor ve `LoginHandler` denetim izine ayrı bir `Auth.KvkkConsent` satırı
     (maskeli e-posta + sürüm) yazıyor; ayrı satır, çünkü "kim hangi metni ne
     zaman onayladı" sorusu giriş olaylarından bağımsız süzülmeli (denetim
     ekranındaki "KVKK onayı" süzgeci).
  `LoginRequest.KvkkConsentVersion` bilinçli olarak **isteğe bağlı**: aynı uçtan
  giriş ekranını hiç görmeyen istemciler (doğrulama betikleri, MCP, Swagger)
  geçiyor, alanı zorunlu kılmak onları kırardı. Şema değişmedi — kayıt zaten
  değiştirilemez olan denetim izine gidiyor, yeni tablo/kolon gerekmedi.
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

## 3g. Denetim Dalga 2 kararları (canlıya çıkış altyapısı)

- **Arayüzü API'nin kendisi sunuyor** (`SpaHosting`), nginx + ayrı konteyner
  değil. Tek origin CORS'u, vekil yapılandırmasını ve çerezin `SameSite`
  gevşetmesini birden ortadan kaldırıyor; alternatif aynı sonucu iki imaj ve bir
  konfigürasyon dosyası daha ile veriyordu. Reddedilen: `MapFallbackToFile`'ı
  koşulsuz kullanmak — `/api/olmayan-uc` için index.html dönerdi ve 404
  sözleşmesi bozulurdu (istemci JSON beklerken HTML ayrıştırır).
- **Göç uygulaması seçmeli** (`Database:MigrateOnStartup`, varsayılan kapalı).
  Konteynerde açık, geliştirme makinesinde kapalı: bir uygulama sürümünün
  üretim şemasını haberimiz olmadan değiştirmesi varsayılan olamaz.
- **TLS bayrağı yapılandırmada** (`Hosting:RequireHttps`). Uygulama TLS
  sonlandırıyorsa HSTS + yönlendirme onda; ters vekil kurulumunda kapalı kalıyor
  ama üretimde **uyarı log'u** düşüyor — sessiz bir "TLS yok" durumu yok.
- **`X-Forwarded-*` yalnızca güvenilen vekil adına** (`Hosting:TrustedProxies`,
  boşsa yalnızca loopback). "Hepsine güven" seçeneği bilinçli olarak sunulmadı:
  denetim izine yazılan IP o başlıktan geliyor ve herkesin yazabildiği bir
  başlığa güvenmek izi kanıttan saldırganın kalemine çevirir. `HttpClientContext`
  bu yüzden başlığı artık elle okumuyor, tek kaynak `RemoteIpAddress`.
- **Jeton `HttpOnly` çerezde, ama `Authorization: Bearer` yolu duruyor.** İkisi
  aynı jeton, iki taşıma yolu: MCP istemcileri, doğrulama betikleri ve Swagger
  çerez taşımıyor. `OnMessageReceived` başlığı önceliyor.
- **CSRF yalnızca çerezle kimliklenen yazma isteklerinde.** Başka bir sitenin
  sayfası bizim jetonumuzu *başlığa* koyamaz; dolayısıyla Bearer istekleri CSRF
  yüzeyi değil. `SameSite=Strict` tek savunma olarak bırakılmadı (eski
  tarayıcılar + ileride gevşetilebilecek bir çerez ayarı kuralı sessizce
  çürütürdü). Çıkış ucu kuralın dışında: zorlanmış çıkışın zararı yeniden giriş,
  karşı taraftaki risk "çerezini temizleyemeyen kullanıcı".
- **CSP'de `style-src 'unsafe-inline'` var, `script-src`'de yok.** Grafikler
  ölçüleri element `style` özniteliğine yazıyor; XSS'in tehlikeli kolu script
  tarafı ve orası `'self'` ile kapalı. Swagger yalnızca geliştirmede ve kendi
  script bloklarını gömdüğü için politikanın dışında.
- **`localStorage`'da yalnızca `t3.session.active` işareti var** (sır değil).
  İki işi var: siteye ilk gelen ziyaretçiye "oturum süreniz doldu" dememek ve
  sekmeler arası senkronu `storage` olayıyla tetiklemek.
- **Oturum durumu açık bir React durumu** (`signedOut`). `queryClient.clear()`
  önbelleği boşaltıyor ama bileşenlere yeni sonuç bildirmiyor; React Query hata
  durumunda eldeki `data`'yı da koruyor. İkisi birlikte "çıkış düğmesi çalışmıyor"
  ve "süresi dolmuş oturum ekranda açık" hatalarını üretti.
- **`SearchText.Fold` Normalize'dan ayrı.** Normalize'ın çıktısı veritabanındaki
  *katlanmamış* kolonla karşılaştırılıyor (e-posta eşitliği, isim tekilliği);
  katlamayı oraya koymak girişi sessizce bozardı. Sorgu tarafında katlama
  `StartupSearch` içindeki `Expression` yüklemlerinde, SQL `replace()` zincirine
  çevrilerek yapılıyor — `unaccent`/`ILIKE` sağlayıcıya özel olurdu. Zincir her
  kolon için tekrar yazılıyor çünkü EF gövdesi başka metotta duran çağrıyı
  çeviremiyor; tekrarın bedeli tek dosyada kalıyor.
- **Yönlendirici `createBrowserRouter`'a taşındı.** `useBlocker` (kirli form
  uyarısı) yalnızca veri yönlendiricisiyle çalışıyor. Rota ağacı JSX olarak
  kaldı (`createRoutesFromElements`), okunabilirlik değişmedi; bedeli ana
  paketin ~55 kB büyümesi — kod bölmenin kazancının yanında kabul edildi.
- **Sentry bağlanmadı, yeri hazırlandı.** Hesap, DSN ve KVKK tarafında yurt dışı
  aktarım kararı gerekiyor; üçü de bu depoda kararlaştırılamaz. `ErrorBoundary`
  beyaz ekranı Türkçe açıklamayla değiştiriyor ve raporlama tek bir noktada
  toplanıyor.

---

## 3h. Canlı demo dağıtımı kararları (VPS)

- **Demo verisi tohumlayıcıyla değil `pg_dump` ile taşındı.** Canlı kopyayı
  `Development` yapıp tohumlatmak en kolay yoldu ve tam olarak reddedilen yol:
  tohumlayıcının ortam kontrolü bir güvenlik sınırı, "demo olsun" diye
  gevşetilmez. Ayrıca `Development` ortamı ayrıntılı hata sayfalarını ve
  Swagger'ı da açardı. Yerelde temiz bir veritabanı tohumlanıp dökümü taşındı;
  sunucuda uygulama `Production` + `Seed:Enabled=false`.
- **İmaj yerelde derlenip `docker save | docker load` ile taşındı.** Sunucuda
  5 GB boş disk ve 3 GB kullanılabilir RAM var; .NET SDK imajı + `npm ci` +
  NuGet önbelleği oradaki beş yığını riske atardı. Bedeli tek seferlik ~230 MB
  transfer.
- **Diğer yığınlara dokunmama biçimi:** ayrı compose projesi (`t3ekosistem`),
  ayrı ağ/hacim, `container_name` çakışmayan adlar (`t3-ekosistem-api`,
  `t3-ekosistem-postgres`), kullanılmayan tek port (8090). `docker system prune`
  **çalıştırılmadı** — 3.8 GB "geri kazanılabilir" imaj başka projelerin
  yeniden derlemesini yavaşlatabilirdi ve disk yetiyordu.
- **Sırlar sunucuda üretildi** (`openssl rand`, `.env` `chmod 600`): bu makineden
  geçmediler, depoya da girmediler. `PUBLIC_BASE_URL` şifre sıfırlama
  bağlantısının kaynağı — istek `Host` başlığından okumak istemcinin
  uydurabildiği bir adrese bağlantı üretmek olurdu.
- **TLS bilinçli olarak yok** (port doğrudan dinliyor): alan adı ve vekil kararı
  ekipte. Çerezin `Secure` bayrağı isteğin şemasına bağlı olduğu için HTTP'de
  oturum çalışıyor, HTTPS'e geçilince kendiliğinden sıkılaşıyor —
  `Hosting:RequireHttps` o gün açılır.

### Render kontrollerinde Türkçe ve CSS tuzakları

- **`innerText` CSS'in `text-transform`'unu uyguluyor.** Marka sayfasının
  başlıkları büyük harfe çevrildiği için "Renk paleti" araması hiç eşleşmedi ve
  `wait_for` zaman aşımına düştü. Beklenen metin gövde cümlesinden alınır
  (render_faz5'te aynı not var: "beklemeyi metne değil öğeye bağla").
- **Türkçe büyük/küçük dönüşümü karşılaştırmada kullanılamaz.**
  `"TÜRKİYE".lower()` birleşik noktalı bir `i̇` üretiyor, `"TAKIMI".lower()`
  ise `takimi` veriyor — iki kontrol bu yüzden düştü. Betiklerde artık sunucu
  tarafındaki `SearchText.Fold` ile aynı katlama var (`katla()`).
- **Beklemeyi "yükleniyor" metnine bağlamayın.** İlk çözüm markör olarak
  "yükleniyor" arıyordu; ekranda "Yükleniyor…" büyük Y ile yazıyor, panoda ise
  ifade bambaşka ("Karne hesaplanıyor…"). Eşleşmeyen markör beklemeyi sessizce
  atlıyor ve kontrol yine veri gelmeden ölçüyor. `render_vps.py` artık
  **beklenen içeriğin kendisini** bekliyor (`bekle_metin("₺")`): gelmezse kontrol
  zaten düşmeli.
- **`wait_for` kabuktan değil veriden seçilir.** `render_faz3.py` onay
  kuyruğunu `wait_for="kuyru"`, portalı `wait_for="portal"` ile bekliyordu; iki
  metin de menüde/başlıkta veri gelmeden duruyor, yani bekleme ilk 0,35 sn'lik
  yoklamada dönüyor ve kontrol "Kuyruk yükleniyor…" ekranını ölçüyordu. Beş
  kontrol bu yüzden makinenin hızına göre bazen geçip bazen düşüyordu — hata
  üründe değil, ölçümdeydi. Artık sekmedeki sayı (`"Bekleyen("`) ve ekranın en
  altındaki `"Yeni üye öner"` bekleniyor.
- **Betiğin kendi yan etkisi kontrolü düşürebilir.** Denetim izi kontrolü ilk
  sayfada `ChangeRequest.` / `Startup.` / `User.` eylemi arıyordu; betik her
  koşuda beş kez giriş yaptığı için birkaç koşu sonra ilk sayfanın tamamı
  `Auth.LoginSucceeded` oldu ve kontrol düştü. Ölçülen şey "satır render oluyor
  mu" olduğu için `Auth.` de kabul ediliyor.

### VPS / nginx tuzakları

- **certbot kurulu olması yenilemenin kurulu olduğu anlamına gelmiyor.**
  Sunucuda certbot `/opt/certbot` altında venv olarak duruyordu; systemd
  zamanlayıcısı da cron kaydı da yoktu ve makinedeki başka bir projenin
  sertifikası bu sessizlik yüzünden dolmuştu. `certbot renew --dry-run`
  başarılı çıkması yalnızca "yenileyebilir" demek, "yeniliyor" demek değil —
  `systemctl list-timers | grep certbot` sorulacak soru.
- **certbot `--nginx` 80 bloğuna `return 404` bırakıyor.** Alan adı için
  301 üretiyor, geri kalan her `Host` (IP dâhil) 404 alıyor. Kanonik adrese
  yönlendiren bir `location /` elle eklendi.
- **nginx'in `client_max_body_size` varsayılanı 1 MB.** Uygulamanın 20 MB'lık
  doküman sınırı vekil arkasında görünmez oluyor: yükleme daha uygulamaya
  varmadan 413 ile ölüyor.
- **Vekil arkasında `TrustedProxies` boş kalırsa** denetim izindeki IP ve giriş
  hız sınırı kovası vekilin adresine sabitlenir — bütün istemciler tek kovaya
  düşer ve iz kanıt değerini kaybeder.
- **`default_server` mevcut siteyi bozmadan devralmanın yolu.** Aynı portta
  `server_name` eşleşmesi her zaman önce gelir; başka projenin alan adı kendi
  bloğunda kalırken IP ve tanımsız `Host` bizim bloğa düşüyor. Dosya silmek,
  yeniden adlandırmak ya da `sites-enabled` bağını kaldırmak gerekmedi.

## 3i. Marka ve logo paketi kararları

Kaynak: `design_handoff_logo_paketi/` (handoff README + preview.html).

- **Logo çizilmedi, dosya kullanıldı.** Daha önce arayüzde kurumsal palete göre
  çizilmiş bir simge vardı; kurumun gerçek işareti gelince o simge silindi.
  Handoff "harf kompozisyonu, renk sırası ve blok oranları değiştirilemez"
  diyor — yeniden çizmek bu kuralı kaçınılmaz olarak ihlal ederdi.
- **`/marka` logo paketi sayfası geri alındı.** Sayfa (renk kartları,
  Pantone/CMYK, üç zemin modu, yanlış kullanım örnekleri) yazılmıştı ve
  çalışıyordu; ürün kararıyla kaldırıldı: arayüz bir iş uygulaması, marka
  kılavuzunun yeri `design_handoff_logo_paketi/`. Arayüzde artık yalnızca
  işaretin kendisi var. Sayfayla birlikte giden şeyler: `BrandKitPage`, lazy
  kaydı, `PublicPage`'in yalnızca o sayfa için eklenen `wide` seçeneği,
  `--font-marka*` belirteçleri, `public/fonts/` altındaki sekiz Barlow dosyası
  (164 kB — CSP `font-src 'self'` yüzünden kendi sunucumuzdan servis
  ediliyordu), yalnızca orada kullanılan üç görsel ve `render_vps.py`'nin dokuz
  kontrolü. Silinen her şeyin aslı handoff paketinde duruyor.
- **Arayüzdeki logo dosyaları kırpıldı** (447×447 → 333×232). Şeffaf kenar
  boşluğu 40 piksellik bir başlık kutusunda işareti gereksiz küçültüyordu;
  işaretin kendisine dokunulmadı, "net alan" kuralı CSS boşluğuyla veriliyor.
- **PNG kabul edildi, SVG borç yazıldı.** Elimizdeki kaynak PNG ve handoff da
  vektör aslından SVG üretilmesini istiyor. Kararı gizlemek yerine README'ye
  açık borç olarak yazıldı.

## 3j. Açılış (tanıtım) sayfası kararları

- **Kök adres artık panoya yönlenmiyor, tanıtım sayfası gösteriyor.** Önceki
  hâlde `/` koşulsuz `/pano`'ya gidiyordu; oturumu olmayan ziyaretçi bir anda
  giriş formuyla karşılaşıyordu. Bağlantıyı ilk kez açan jüri üyesinin sistemin
  ne yaptığını form doldurmadan okuyabilmesi gerekiyor.
- **Yönlendirme kararı `/api/me` beklenmeden veriliyor.** Sayfa, "bu tarayıcıda
  oturum açılmıştı" izine (`t3.session.active`) bakıyor: iz varsa doğrudan
  `/pano`, yoksa tanıtım. Cevabı beklemek siteye ilk gelen herkese gereksiz bir
  yükleniyor çarkı izletirdi; iz yanılırsa zarar yok, `/pano` kendi korumasıyla
  kullanıcıyı girişe yolluyor. İz bu yüzden sağlayıcıdan `lib/auth.ts` içine
  taşındı (bileşen ihraç eden modüle yardımcı eklemek fast refresh'i kapatıyor).
- **Sayfa hiç veri çekmiyor.** API kapalıyken bile açılıyor; tek dış bağımlılığı
  logo dosyaları. Tanıtımda uydurma istatistik yok — anlatılan dört blok
  şartnamedeki zorunlu MVP maddelerinin kendisi.
- **Statik paket, `lazy` değil.** Artık her ziyaretin ilk karesi bu sayfa; giriş
  ekranı ve 404 ile aynı gerekçeyle ana pakette duruyor.
- **Logo paketi bağlantısı hiçbir alt bilgide yok.** Önce giriş ekranından
  alınıp açılış sayfasına taşındı, ardından sayfanın kendisi kaldırıldı
  (bkz. 3i). Arayüzde marka artık bir bağlantı değil, yalnızca işaretin
  kendisi.

## 3k. Uygulama günlüğü (loglama) kararları

Uygulandı — karar 4 Eylül 2026, aynı gün uygulamaya alındı. Ayrıntılı gerekçe
ve reddedilen alternatifler için bkz. [Loglama_Plani.md](Loglama_Plani.md)
(uygulama tamamlandığı için artık yalnızca arşiv niteliğinde).

- **Serilog + Grafana Loki, Seq değil.** Lisans farkı belirleyici: Loki AGPLv3
  (kullanıcı sınırı yok), Seq'in ücretsiz sürümü tek kişiyle sınırlı. Sistem T3
  Vakfı'na devredilecek; devredilen bir gözlemleme katmanının "tek kişi
  bakabilir" olması kabul edilmedi.
- **Üç kavram ayrı tutuluyor:** `AuditLog` (kim neyi değiştirdi — Postgres, rol
  kapılı, KVKK kaydı), uygulama günlüğü (bu istekte ne oldu — Serilog, döner ve
  silinir), hata izleme (hangi hata yeni — açık iş, bkz. Loglama_Plani.md §9).
  `AuditLog` hiçbir koşulda teşhis logu olarak kullanılmıyor.
- **Promtail/Alloy kurulmadı.** Serilog Loki'ye doğrudan HTTP ile yazıyor;
  toplayıcı katmanı gereksiz bir bileşen olurdu.
- **Kişisel veri maskesi log seviyesinde de var:** `KisiselVeriMaskesi`
  (`T3.Infrastructure/Logging/`), Serilog `IDestructuringPolicy` — bir nesne
  `{@...}` ile loglandığında `Email`/`Phone`/`TaxNumber`/`Amount` gibi adlar
  taşıyan alanlar ham değerle yazılmıyor. E-posta için `MaskedEmail.Of()`
  tekrar kullanıldı — ikinci bir maskeleme kuralı yazılmadı.
- **Korelasyon: `RequestIdMiddleware` `HttpContext.TraceIdentifier`'ı
  `X-Request-Id`'ye eşitliyor.** Bilinçli tasarım: `ExceptionHandlingMiddleware`
  500 gövdesine `Referans = TraceIdentifier` koyduğu için, ikisi aynı değer
  olmazsa kullanıcının ekranda gördüğü kod ile Loki'de arayacağı `IstekId`
  birbirini tutmazdı. 4xx'e `Referans` konmuyor — kullanıcının kendi
  düzeltebileceği bir hata orada gürültü olur.
- **Etiket disiplini (Loki):** yalnızca `app`, `env`, `level` etiket.
  `IstekId`/`RequestPath`/`Rol` etiket **değil**, satırın içinde kalıp
  `| json | IstekId="…"` ile aranıyor — yüksek kardinaliteli alan etiket
  olursa Loki'nin akış sayısı patlar.
- **`compactor.retention_enabled: true` açıkça verildi.** Loki varsayılan
  olarak hiçbir şeyi silmez; bu satır unutulursa log deposu VPS diskini
  doldurur ve o makinedeki diğer projeleri de düşürür (~5 GB boş alan, 5 başka
  compose projesi paylaşıyor).
- **Loki + Grafana `--profile observability` arkasında, `pgadmin` gibi.**
  Uygulamanın çalışması için gerekli değil (Serilog konsol + dosyaya yine
  yazıyor); varsayılan `docker compose up -d postgres` bunları başlatmıyor.
- **Development ortamında `Serilog:WriteTo` dizisi tam olarak yeniden
  yazılıyor** (Console + File + GrafanaLoki), tek elemanla eklenmiyor.
  Sebep: .NET yapılandırma sağlayıcıları JSON dizilerini **indekse göre**
  birleştiriyor — `appsettings.Development.json`'a yalnızca üçüncü elemanı
  eklemek, base'teki Console/File elemanlarının yerine geçerdi, yanlarına
  eklenmezdi.

## 3l. Güvenlik denetimi düzeltmeleri kararları (Faz 0-2)

Uygulandı — 4 Eylül 2026. Bulgu numaraları
[Guvenlik_Denetimi_ve_Iyilestirme_Plani.md](Guvenlik_Denetimi_ve_Iyilestirme_Plani.md)'ye
karşılık geliyor.

- **G-01, yenileme jetonu bilinçli olarak eklenmedi.** Jeton ömrü 480 → 60
  dakikaya indirildi ve her isteği canlı veritabanı durumuyla karşılaştıran
  `IUserStateProvider`/`UserStateMiddleware` eklendi (`IsActive`, `Role`,
  `SecurityStamp`; 45 sn önbellek + handler'ların çağırdığı anlık
  `Invalidate`). Bu, "yetkiyi geri alma" sorununu (asıl bulgu) tamamen çözüyor:
  pasife alınan/rolü düşürülen/şifresi değişen kullanıcının eski jetonu bir
  sonraki istekte 401 alıyor. **Reddedilen alternatif:** HttpOnly çerezde ayrı
  bir yenileme jetonu + rotasyon + istemci tarafı sessiz yenileme akışı — bu,
  Faz 1'in geri kalanından daha büyük bir yüzey ve rapor sırasında Demo Day'e
  günler kalmışken riskli. Kullanıcı artık 60 dakikada bir yeniden giriş
  yapıyor; Dalga 1'in 1.3 maddesinde zaten kabul edilmiş "kısa yol seçildi"
  deseniyle aynı takas.
- **Çıkış (`/api/auth/logout`) kullanıcının TÜM jetonlarını iptal ediyor,
  yalnızca isteği yapan tarayıcıyı değil.** Ayrı bir oturum/jeton tablosu
  olmadığı için tek jetonu hedefli iptal etmenin yolu yok; `SecurityStamp`'i
  kullanıcı bazında yenilemek en basit doğru çözüm. Yan etki: bir cihazda
  çıkış yapmak diğer cihazlardaki oturumları da düşürür. Kabul edildi — karşı
  taraf (çalınmış bir jetonun çıkıştan sonra da çalışması) daha kötü.
- **Şifre değiştirme (`ChangePasswordHandler`) kendi SecurityStamp'ini bumpladıktan
  hemen sonra yeni bir jeton üretip aynı yanıtta çereze yazıyor.** Aksi hâlde
  kullanıcı kendi isteğiyle şifresini değiştirdiği anda kendi oturumundan da
  atılırdı — güvenlik doğru ama UX kırık olurdu.
- **G-05, hesap kilidi mesajı jenerik kalıyor.** 10 başarısız denemeden sonra
  hesap 15 dakika kilitleniyor ama kullanıcıya hâlâ "e-posta veya şifre
  hatalı" deniyor — "hesap kilitli" gibi farklı bir mesaj, kilitlenmenin
  yalnızca gerçek hesaplarda mümkün olması nedeniyle e-posta numaralandırmaya
  hizmet ederdi. Ayrım yalnızca denetim izinde.
- **G-04, (a) redaksiyon seçildi, (b) hukuki aktarım kurulumu değil.**
  `AiRedaction` katmanı (`T3.Application/Features/Assistant/AiRedaction.cs`)
  modele giden araç sonucundan `ContactEmail`, `ContactPhone`, `TaxNumber`,
  ekip üyesi `FullName`/`Email`/`Phone`/`LinkedInUrl` alanlarını alan adına
  bakarak (DTO'dan bağımsız, JSON ağacı gezerek) çıkarıyor; REST ve MCP
  yolları bundan etkilenmiyor, yalnızca `AskAssistantHandler`'ın model turu
  bu süzgeçten geçiyor. **Reddedilen alternatif:** veri işleyen sözleşmesi +
  aktarım mekanizması + envanter kaydı + ilgili kişiye bildirim kurup
  özelliği olduğu gibi bırakmak — bu, hukuki/organizasyonel bir süreç ve bir
  kod değişikliğiyle Demo Day'e yetiştirilecek bir iş değil. Aydınlatma metni
  (`PrivacyNoticePage.tsx`) bu daraltılmış aktarımı dürüstçe anlatacak şekilde
  güncellendi; kalan aktarım (girişim adı, sektör, program geçmişi,
  toplulaştırılmış sayılar) hâlâ KVKK m.9 kapsamında ve mekanizması ayrı
  kurulmalı — bu iş kapanmadı, yalnızca kapsamı daraltıldı.
- **G-15, PBKDF2 iterasyonu 600.000'e sabit değer olarak yükseltildi**
  (OWASP'ın PBKDF2-HMAC-SHA256 için önerdiği alt sınır), yapılandırılabilir
  yapılmadı — tek bir hash algoritması/parametre seti var, ortama göre
  değişmiyor. `VerifyAndGetRehash` eski (düşük iterasyonlu) hash'leri başarılı
  girişte sessizce yükseltiyor.

- **G-07, üç ayrı kova.** `MassExportPolicy` (saatte 10, CSV aktarımı +
  doküman indirme) ve ayrı bir `McpPolicy` (dakikada 100) eklendi — `/mcp`'yi
  export kovasına sokmak Demo Day'de Claude Desktop'tan birden çok soru
  sormayı kırardı, o yüzden ayrı ve daha gevşek bir kova. `options.GlobalLimiter`
  (dakikada 300, kullanıcı/IP başına) tüm uçlara ek olarak uygulanıyor. Eşik
  aşımında ayrı bir `Security.MassExport` izi düşüyor; `Report.Export` artık
  satır kimliklerini de taşıyor.
- **G-09, kısmen.** `IAuditWriter`'a `saveChanges: false` parametresi eklendi
  (varsayılan `true` — mevcut 30'a yakın çağıran hiç değişmedi) ve bu turda
  dokunulan kimlik/şifre handler'ları (Login, Deactivate, UpdateUser,
  SetUserPassword, ChangePassword, ResetPassword) buna geçirildi: iş
  değişikliği ile iz satırı artık tek `SaveChanges`'ta. **Reddedilen/ertelenen:**
  geri kalan ~30 handler'ın aynı kalıba geçirilmesi — geniş, mekanik ama riskli
  bir değişiklik, ayrı bir turda yapılmalı. `RetentionCleanupService` eklendi
  (günlük): süresi dolan `PasswordResetToken`'ları siler, 10 yıldan eski
  `AuditLog` satırlarını (IP, istemci, önce/sonra JSON) anonimleştirir — olayın
  kendisi (`Action`, `EntityType`, `OccurredAt`) kalır. **Yapılmayan:** DB
  düzeyinde `UPDATE`/`DELETE` yetkisinin uygulama kullanıcısından alınması
  (ayrı bir DB rolü + migration gerektirir, canlı bir Postgres'e karşı
  doğrulanmadan güvenle uygulanamaz) ve hash zinciri (yapısal bir değişiklik,
  ayrı tur).
- **G-10, tam.** `ChangeRequestDetailResponse.RequiresElevation` (maskeli VE
  değişen en az bir alan varsa `true`) hem ekranda uyarı hem
  `ApproveChangeRequestHandler`'da sunucu tarafı 403 — inceleyicinin
  göremediği bir alanı değiştiren öneriyi yalnızca Süper Yönetici onaylayabilir.
- **G-11, kısmen.** CSRF jetonu artık rastgele değil, `HMAC(Jwt:Secret,
  oturum jetonu)` — `SessionCookie.DeriveCsrfToken`. **Reddedilen alternatif:**
  çerezleri `__Host-` önekiyle yeniden adlandırmak. Bu önek `Secure` bayrağını
  zorunlu kılıyor; geliştirme makinesi düz HTTP kullanıyor ve `Secure` çerez
  orada hiç saklanmazdı — yerel geliştirme akışını kırardı. Ayrıca doğrulama
  betikleri (`cdp.py`) çerez adını sabit string olarak biliyor; adı
  değiştirmek onları da güncellemeyi gerektirirdi. Üretim zaten HTTPS
  üzerinde ve `Domain` özniteliği hiç ayarlanmıyor (alt alan adı riski bu
  yüzden bugün de düşük); önek eklemenin kazancı bu maliyete değmedi.
- **G-16, ilk adım tamamlandı.** `.github/workflows/ci.yml`: `dotnet build` +
  `dotnet test` + `dotnet list package --vulnerable` + `npm run build` +
  `npm run lint` + `npm audit`. Bu hat ilk çalıştırmada gerçek bir bulgu
  buldu: `xunit` 2.4.2'nin geçişli bağımlılıkları (`System.Net.Http` 4.3.0,
  `System.Text.RegularExpressions` 4.3.0) yüksek önemli CVE'ler taşıyordu.
  `xunit` 2.9.3 + `xunit.runner.visualstudio` 3.0.2 + `coverlet.collector`
  6.0.4'e yükseltildi, 210 test hâlâ yeşil, tarama artık temiz.
- **G-17, dev `docker-compose.yml` sertleştirildi.** Postgres portu
  `127.0.0.1`'e bağlandı; pgAdmin `SERVER_MODE=True` + zorunlu (varsayılansız)
  parola + `127.0.0.1`'e bağlı port + sabit `8.14` imaj etiketi; tüm
  servislere `cap_drop: [ALL]` + `mem_limit`. **Reddedilen/ertelenen:**
  `read_only: true` kök dosya sistemi — hangi servisin hangi yola (tmp, run)
  yazması gerektiği canlı bir konteynerde doğrulanmadan tahminle eklenirse
  sessizce kırılabilir; bu ortamda docker köprüleri kısmen `DOWN` olduğu için
  (bkz. §4) canlı doğrulama riskli. `SSL Mode=Require` bağlantı dizesine
  eklenmedi: postgres imajı TLS'i varsayılan açmıyor, yalnızca loopback'te
  gezen bir bağlantıya zorunlu kılmak yereldeki geliştirmeyi tamamen kırardı.

**Bu turda hiç ele alınmayan (Faz 3, altyapı/organizasyon kararı):** .NET 10
geçişi (destek Kasım 2026'da bitiyor), şifreli+makine dışı yedekleme,
bağımsız sızma testi, KVKK VERBİS/envanter gözden geçirmesi. Bunlar bir kod
değişikliği turunda güvenle kapatılamayacak kararlar; plan bu şekilde kalıyor.

---

## 3m. E-posta gönderimi kararları (SMTP)

**Kendi mail sunucumuz kurulmadı.** Değerlendirilen alternatif: VPS'e
mailcow/Mailu. Reddedildi — 25. port çoğu sağlayıcıda kapalı, taze bir IP'nin
itibarı yokken şifre sıfırlama maili doğrudan spam'e düşer ve sunucu sürekli
bakım ister. Aynı VPS'te beş başka proje koşuyor; mail sunucusu oraya eklenecek
en kırılgan bileşen olurdu.

**Gönderme ve alma ayrı servislerde.** Gönderim Brevo SMTP relay'i (günde 300
mail, süresiz ücretsiz), kutular Zoho Mail Forever Free (5 kullanıcı × 5 GB).
Tek servisle olmuyor: Zoho'nun ücretsiz planı IMAP/POP/SMTP vermiyor, yani
uygulama o hesap üzerinden mail atamıyor. MX Zoho'yu gösterirken SPF/DKIM her
iki servisi de yetkilendiriyor; **SPF tek satır** olmak zorunda (iki ayrı
`v=spf1` kaydı SPF'i geçersiz kılar ve iki servisin de maili spam'e düşer).

**Gönderici seçimi ortama değil yapılandırmaya bakıyor.** `Email:Smtp` üçlüsü
(host + kullanıcı + parola) tamsa `SmtpEmailSender`, değilse üretimde
`ThrowingEmailSender` / geliştirmede `FileOutboxEmailSender`. Ortam koşuluyla
başlasaydı gerçek gönderim yolu ilk kez canlıda denenmiş olurdu. Yarım
yapılandırma (parolası boş host) bilinçli olarak "yok" sayılıyor: aksi hâlde
hata ancak ilk sıfırlama isteğinde, üretimde ortaya çıkardı.

**Gönderim hatası şifre sıfırlama yanıtını değiştirmiyor.** SMTP arızası
istisnayı yukarı taşısaydı kayıtlı adres 500, kayıtsız adres 200 dönerdi —
sabit yanıt ve sabit süre için ödenen bedel (G-05) tek bir istisnayla boşa
giderdi. Hata yutulmuyor: `SmtpEmailSender` maskeli alıcıyla ERROR log'u
düşürüyor ve denetim izine `Result = "gönderim başarısız"` yazılıyor.

**Log'a gövde yazılmıyor.** Şifre sıfırlama gövdesi ham jeton taşıyor; başarı
log'u yalnızca maskeli alıcı ve konu içeriyor. Aynı gerekçe `FileOutbox`'ın
üretimde kayıtlı olmama sebebiyle aynı (G-02): sırrın ikinci kopyası üretilmez.

**MailKit, `System.Net.Mail.SmtpClient` değil.** Microsoft ikincisini yeni kod
için önermiyor; MailKit STARTTLS'i ve iptal jetonunu doğru işliyor.
`SecureSocketOptions` açıkça veriliyor (`StartTls`/`SslOnConnect`) — "auto"
bazı sağlayıcılarda düz metne düşüp parolayı ağa açık yollayabiliyor.

Kurulum adımları, DNS kayıtları ve tuzaklar:
[Mail_Servisi_Kurulumu.md](Mail_Servisi_Kurulumu.md).

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

### Dalga 2 (denetim düzeltmeleri)

- **`npx tsc --noEmit` bu repoda hiçbir şeyi kontrol etmiyor.** Kök
  `tsconfig.json` yalnızca referans dosyası (`references`), kendi `files`
  listesi boş: komut sessizce başarıyla çıkıyor. Geçersiz JSX konumundaki bir
  yorum bu yüzden fark edilmeden depoya girdi ve **arayüz derlenmez** hâle
  geldi (Dalga 1'in son render koşusunun asılı kalma sebebi). Tek geçerli
  kontrol `npm run build` (= `tsc -b && vite build`).
- **JSX yorumu koşullu ifadenin parantezinden hemen sonra konulamaz.**
  `{session ? (\n {/* … */}\n <div>` geçersiz; yorum koşulun *dışına* ya da
  etiketin *içine* alınmalı.
- **Headless Chrome'da `:focus` seçicisi eşleşmiyor**: pencere odakta sayılmadığı
  için element `document.activeElement` olsa bile odak stilleri uygulanmıyor ve
  erişilebilirlik ölçülemiyor. `Emulation.setFocusEmulationEnabled` açılınca
  `oklab(… / 0.6)` halka ölçülebiliyor (bkz. `cdp.py`).
- **Jeton çereze taşınınca render betiklerinin oturum kurma yolu kapandı**:
  `localStorage.setItem('t3.accessToken', …)` artık hiçbir şey yapmıyor —
  kapanan açığın kendisi bu. Çerezi yalnızca CDP yazabiliyor
  (`Browser.set_session`), ayrıca CSRF çerezi de yazılmalı yoksa her yazma
  isteği 403 alır.
- **Betikler birbirinin verisini yiyor.** `e2e_faz3` her koşuda bir girişimi
  pasife alıyor; `e2e_faz5` o kaydı **adıyla** arıyordu ve ikinci koşuda
  çöküyordu. Sabit ada bağlı kontrol ürünü değil veri durumunu ölçer: kapsam
  dışı kayıt artık kapsamdan türetiliyor. Aynı sebeple `e2e_faz3`'ün kullanıcı
  sayısı kontrolü `== 8` yerine "tohum hesaplarının tamamı listede" oldu ve
  `render_faz7` açtığı tek kullanımlık hesabı sonunda pasife alıyor.
  Silme zincirinin dokümanı kapattığı kontrol de aynı sınıftaydı: seçilen kaydın
  tohumda dosyası olduğunu varsayıyordu, artık silmeden önce kendi dosyasını
  yükleyip zincire bakıyor.
  **Tam yeşil bir zincir yine de temiz veritabanı ister** — hacim silinemiyorsa
  (yetki yok, köprü bozuk) **tur başına ayrı veritabanı** aynı sonucu veriyor:
  `CREATE DATABASE t3_turN` → bağlantı dizesindeki `Database=` adını çevir →
  `dotnet ef database update`. Geliştirme tohumlayıcısı boş şemayı doldurduğu
  için tur temiz başlıyor, üstelik önceki turun verisi incelenmek üzere kalıyor.
- **Docker köprü arayüzleri host tarafında `DOWN` düşebiliyor** (`br-*`,
  `docker0`): yayımlanan port bağlantıyı kabul ediyor ama konteynere iletmiyor,
  Npgsql "Timeout during reading attempt" diyor ve teşhis yanlış yere gidiyor.
  Kontrol: `ip -br addr show | grep br-`. Çözüm root ister
  (`sudo ip link set <br> up` ya da docker yeniden başlatma); geçici çare
  `docker exec … nc` üzerinden yerel bir TCP köprüsü.
- **Köprü `DOWN` iken imaj derlemesinin de ağı yok**: `npm ci` ilk `RUN`
  adımında düşüyor. `docker build --network host` derleme adımlarını host
  ağına alıyor ve iş görüyor. Bu kutuda ayrıca `docker buildx` kurulu değil;
  `DOCKER_BUILDKIT=0` ile eski derleyici kullanılıyor (Dockerfile'ın
  `# syntax` satırı eski derleyicide yalnızca yorum, sorun çıkarmıyor).
  İmaj tek başına kalkarken host ağı `--network host` ile veriyor; `.env`
  köprüsündeki 5434 vekili aynı şekilde görülüyor.

### Loglama

- **`HttpResponse.Clear()` gövdeyle birlikte o ana kadar konan başlıkları da
  siliyor.** `ExceptionHandlingMiddleware` 500 gövdesini yazmadan önce
  `Response.Clear()` çağırıyor; `RequestIdMiddleware` isteğin başında koyduğu
  `X-Request-Id` başlığı bu yüzden sessizce kayboluyordu — gövdedeki `Referans`
  doluydu ama yanıt başlığı boştu. Canlı 500 testiyle yakalandı (curl'de
  başlık yoktu), `dotnet test` bunu göremezdi çünkü hiçbir birim testi HTTP
  başlığını doğrulamıyordu. Çözüm: `Clear()`'dan sonra başlık
  `context.TraceIdentifier`'dan yeniden yazılıyor.
- **JSON yapılandırma dosyalarında yorum satırı yok.** `Serilog:WriteTo`
  dizisine ilk yazımda `//` yorumları eklendi; `System.Text.Json` tabanlı
  `JsonConfigurationProvider` bunları kabul etmiyor (VS Code'un JSONC'siyle
  karıştırılmasın), uygulama açılışta patlıyordu.
- **Serilog'un `ExceptionHandlingMiddleware`'den kalan `JsonSerializer.Serialize`
  çağrısı `Results`/`WriteAsJsonAsync`'ten farklı yol izliyordu.** Eski kod
  anonim tip kullanarak (`new { status, title, errors }`) küçük harfli alan adı
  üretiyordu — DI'daki camelCase politikasını hiç görmeden. `ApiErrorBody`
  record'una geçince aynı satır PascalCase üretmeye başladı (frontend'in
  beklediği `title`/`errors` alanları `undefined` kaldı). Düzeltme:
  `context.Response.WriteAsJsonAsync(...)` — CSRF/status-code-pages
  yollarıyla aynı DI seçeneklerini kullanan tek doğru yol.

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
| Denetim Dalga 0 | ✅ | Ürün denetiminin videodan önce kapanması gereken altı bulgusu: yazma yolları arayüze, hız sınırı bölümlemesi, sekme başlığı/dil, 404 ekranı, mobil taşma, yazma yolunda maskeleme. Doğrulama: 156 birim testi, 201 render kontrolü (`render_faz6.py` yeni). |
| Denetim Dalga 1 | ✅ | Program/dönem yönetimi, şifre kurtarma ve değiştirme, oturum ömrü + ağ hatası ayrımı, giriş olaylarının denetim izi, KVKK metinleri, Karar Verici'nin onay ekranı. Doğrulama: 185 birim testi, `render_faz7.py` (yeni). |
| Denetim Dalga 2 | ✅ | Tek origin dağıtım (API arayüzü sunuyor) + `Dockerfile`/`docker-compose.prod.yml`, jeton `HttpOnly` çerezde + CSRF + CSP ve güvenlik başlıkları, güvenilen vekil listesi, URL'de filtre durumu, aksan katlaması, kirli form uyarısı, kod bölme, erişilebilirlik. Doğrulama: 191 birim testi, 310 uçtan uca kontrol, 352 render kontrolü (`render_faz8.py` yeni, 54 kontrol; KVKK onay kapısıyla `render_faz7.py` 97'ye çıktı). |
| Loglama altyapısı | ✅ | Serilog (konsol + dosya + Grafana Loki), `KisiselVeriMaskesi` (KVKK redaksiyonu), `RequestIdMiddleware` + `ApiErrorBody.Referans` (hata kodu ↔ `X-Request-Id` ↔ Loki `IstekId` üçlü eşleşmesi), frontend `ErrorState` kopyalanabilir referans satırı, `ops/loki-config.yaml` + `ops/grafana-datasource.yaml` (`--profile observability`), üretimde `docker logs` boyut sınırı. Doğrulama: 192 birim testi (yeni `KisiselVeriMaskesiTests`), canlı 500 testiyle `Referans`/`X-Request-Id` eşleşmesi, Loki'de `{app="t3-api"} | json | IstekId="…"` sorgusu, 81 uçtan uca kontrol (`e2e_faz3.py`, regresyon), render kontrolü (404 ekranı + pano). Ayrıntı: §3k. |

**Faz 1'den bilinçli ertelenen:** kullanıcı yönetimi CRUD'u — girişim kartı buna
ihtiyaç duymadığı için onay akışıyla birlikte Faz 3'e alındı.

**Faz 0'dan taşınan açık iş (Faz 3'te kapatıldı):** soft delete yalnızca sözleşme
düzeyinde zincirleniyordu; `ProgramParticipation`, `ProgramTerm`, `Milestone`,
`UserProgramAssignment` ve `ChangeRequest` `ISoftDelete` uygular ama ebeveyn pasife
alındığında çocukları işaretleyen kod yoktu. Sorgu süzgeçleri yalnızca *okumayı*
daralttığı için zincir `DeleteStartupHandler` içinde elle yürünüyor.
