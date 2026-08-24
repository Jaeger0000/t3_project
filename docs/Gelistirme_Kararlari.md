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
render betiği (`e2e_faz3/4/5.py`, `render_faz3/4/5.py`) ve ikincilerin
kullandığı `cdp.py`. Çalıştırma sırası ve veritabanı sıfırlama kuralı
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
