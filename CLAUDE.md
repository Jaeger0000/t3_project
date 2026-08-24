# CLAUDE.md

Bu dosya her oturumda otomatik yüklenir. **Kısa tutulur**: burada yalnızca her
görevde geçerli olan kurallar durur, ayrıntı ayrı dosyalara bırakılır. Bir
karar/gotcha tekrar tekrar lazım olmuyorsa buraya değil
[docs/Gelistirme_Kararlari.md](docs/Gelistirme_Kararlari.md) içine yazılır.

## Proje

T3 Vakfı Bursiyer Yapay Zekâ Creathonu — **Problem 7: T3 Girişim Ekosistemi
Yönetim Sistemi**. Dağınık girişim verisini (profil, program geçmişi, satış,
yatırım, ekip, başarı, doküman) tek profilde birleştiren, rol bazlı erişimli
platform. 6 zorunlu MVP maddesinden biri bile eksikse takım bir sonraki
değerlendirme aşamasına geçemez — **önce MVP, sonra güzellik**.

| Belge | Ne zaman okunur |
|---|---|
| [docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md](docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md) | Gereksinim, rol, MVP kapsamı, program kuralları |
| [docs/Problem7_Teknik_Plan.md](docs/Problem7_Teknik_Plan.md) | Mimari, veri modeli, yetki matrisi, API yüzeyi, faz planı |
| [docs/Gelistirme_Kararlari.md](docs/Gelistirme_Kararlari.md) | Yerleşik teknik kararlar, reddedilen alternatifler, ortam tuzakları |
| [README.md](README.md) | Kurulum, demo hesapları, komutlar, mevcut durum |
| [scripts/README.md](scripts/README.md) | Çalışan sisteme karşı doğrulama betikleri ve çalıştırma sırası |

## Takvim

- Görev teslimi (iş modeli kanvası + prototip videosu + sunum): **26 Ağustos 10.00**
- Yüz yüze Creathon + Demo Day: **5–6 Eylül** (5 dk sunum + 5 dk jüri sorusu)

## Pazarlık dışı kurallar

- **KVKK:** hassas alanlar rol bazlı maskelenir. Maskeleme koşulu kodda
  **okunabilir** kalmalı — jüriye "bu alanı kim neden göremiyor" gösterilecek.
- **Sırlar:** `.env` git'e girmez; `.env.example` yalnızca yer tutucu içerir.
  Tüm sırlar `T3_` önekli ortam değişkeniyle gelir. JWT anahtarı hiçbir koşulda
  `appsettings.json` içine yazılmaz.
- **Tohum verisi (`DevDataSeeder`) üretimde çalışmaz** — ortam kontrolü bir
  güvenlik sınırıdır, "kolaylık olsun" diye gevşetilmez. Tüm demo verisi
  kurgudur: `.test` alan adları (RFC 6761), tahsis edilmemiş telefon öneki.
- **T3 Vakfı'na ait operasyonel veri/kılavuz** yalnızca program kapsamında
  kullanılır; üçüncü taraflarla paylaşılmaz.
- **Kullanıcının e-postası** yalnızca kimlik/atıf içindir; ilgisiz bir servise
  istek başlığı, URL veya gövde içinde gönderilmez.

## Mimari kuralları

- **Bağımlılık yönü:** `Api → Application → Domain`, `Infrastructure → Application`.
- **Dikey dilim:** use-case'ler `Features/Startups/CreateStartup/` gibi klasörlerde;
  teknik klasörleme (Services/, Repositories/) yok.
- **MediatR yok.** Düz `*Handler` sınıfları DI'dan çözülür; use-case olmayan
  bileşenler (`StartupEditGuard`, `ChangeRequestApplier`, `UserAdminGuard`) açıkça kaydedilir.
- **Mapper kütüphanesi yok.** Entity → DTO eşlemesi elle yazılır (maskeleme okunur kalsın).
- **DTO'lar dilim başına.** Ortak yazma modeli yalnızca create+update aynı gövdeyi
  yazdığında paylaşılır (`StartupWriteModel`, `TeamMemberWriteModel`).
- **Doğrulama endpoint filtresinde** (`.WithValidation<T>()`), handler'da değil.
  Her istek tipi için **tek** doğrulayıcı.
- **Yetkilendirme varsayılan kapalı** (`FallbackPolicy`); herkese açık uç
  `.AllowAnonymous()` demek zorunda.
- **Derinlemesine savunma:** endpoint politikası + handler içinde tekrar kontrol.
  MCP araçları handler'ları doğrudan çağıracağı için politika hattı atlanabilir.
- **RBAC'ın tek noktaları:** `IStartupScope` (girişim satırı), `StartupVisibility`
  (alan/KVKK), `IChangeRequestScope` (onay satırları), `ProgramAccessGuard`
  (program sahipliği: tanım SuperAdmin'de, dönem/katılım kendi programında).
  Yeni kural bunların dışına yazılmaz.
- **Maskeleme yazma yolunda da tutulur:** maskeli alan istemciye `null` gittiği
  için tam değiştirmeli `PUT` onu sessizce siler
  (`StartupWriteModel.ApplyTo(startup, visibility)`).
- **MCP araçları REST ile aynı Application handler'larını sarar** — asla paralel veri yolu.
- **Girişim kullanıcısı hiçbir tabloya doğrudan yazmaz** — yalnızca `ChangeRequest` üretir.

## Sık çarpılan tuzaklar (ayrıntı: [Gelistirme_Kararlari.md](docs/Gelistirme_Kararlari.md))

- Postgres **5433**, API **5080**, Vite **5173**.
- `.env` değerleri **çift tırnaklı** kalmalı (bağlantı dizesinde `;` var).
- Application katmanında **sağlayıcıya özel EF API'si yok**: `EF.Functions.ILike`,
  `AsSplitQuery`, `ExecuteUpdate/Delete` kullanılamaz.
- Türkçe **İ** küçültmede bozulur → karşılaştırmalar `SearchText.Normalize` üzerinden;
  gösterim etiketlerinde küçültme yapılmaz.
- Sunucuda üretilen tutar/boyut metinlerinde **kültür açıkça verilir** (`tr-TR`);
  yerel ayara bırakılan biçimlendirme makineye bağlı diff üretir.
- Soft delete süzgeçleri yalnızca **okumayı** daraltır; zincir elle yürütülür.
- Bash aracı **zsh** çalıştırır (fish değil); `pkill -f 'T3[.]Api'` yaz, yoksa kendi
  kabuğunu öldürür.
- `dotnet ef` için proje-yerel manifest (8.0.10) + **mutlak** `--project` yolları.

## Çalışma biçimi

- Kod yorumları ve kullanıcıya dönen mesajlar **Türkçe**. Yorum "ne yaptığını"
  değil "neden böyle" olduğunu anlatır.
- Doğrulama alışkanlığı: `dotnet build && dotnet test` → gerçek API'ye tüm
  rollerle vuran Python E2E betiği → `google-chrome-stable --headless=new --dump-dom`
  ile gerçek render. index.html'in 200 dönmesi uygulamanın açıldığını göstermez;
  Faz 2'deki `0 ₺` maskeleme hatasını yalnızca headless render yakaladı.
