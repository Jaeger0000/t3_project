# T3 Girişim Ekosistemi Yönetim Sistemi

Programdan yatırıma, T3 girişimcilik ekosisteminin tek kurumsal hafızası ve karar destek platformu.

T3 Vakfı Bursiyer Yapay Zekâ Creathonu — **Problem 7** çözümü.

## Belgeler

| Belge | İçerik |
|---|---|
| [docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md](docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md) | Ürün gereksinimleri, roller, zorunlu MVP maddeleri, program kuralları |
| [docs/Problem7_Teknik_Plan.md](docs/Problem7_Teknik_Plan.md) | Mimari, veri modeli, yetki matrisi, API yüzeyi, faz planı |
| [docs/Gelistirme_Kararlari.md](docs/Gelistirme_Kararlari.md) | Yerleşik teknik kararlar, reddedilen alternatifler, ortam tuzakları |
| [CLAUDE.md](CLAUDE.md) | Yapay zekâ asistanı için kısa proje sözleşmesi — kurallar ve belge dizini |
| [scripts/README.md](scripts/README.md) | Uçtan uca ve render doğrulama betikleri, çalıştırma sırası |

## Teknoloji

- **Backend:** .NET 8 · Clean Architecture + dikey dilim · Minimal API · EF Core 8
- **Frontend:** React 19 · TypeScript · Vite · Tailwind CSS 4 · TanStack Query
- **Veritabanı:** PostgreSQL 16 (Docker)
- **AI:** MCP sunucusu (.NET içinde) + Claude API — anahtar **opsiyonel**, yoksa sorular yerel planlayıcıyla yanıtlanır

## Kurulum

### Gereksinimler

.NET 8 SDK · Node.js 20+ · Docker & Docker Compose

### 1. Ortam değişkenleri

```bash
cp .env.example .env
```

`.env` içindeki iki değeri mutlaka doldurun (üçüncüsü, `T3_Ai__ApiKey`,
opsiyonel — bkz. [AI katmanı](#ai-katmanı)):

```bash
# Rastgele parola
openssl rand -base64 24

# JWT anahtarı (en az 32 karakter)
openssl rand -base64 48
```

> `.env` git'e girmez. Yerel Postgres'iniz 5432'yi kullanıyorsa `POSTGRES_PORT`
> ve bağlantı dizesindeki portu birlikte değiştirin (varsayılan: **5433**).

### 2. Veritabanı

```bash
docker compose up -d postgres

# pgAdmin de isterseniz (http://localhost:5050)
docker compose --profile tools up -d
```

### 3. Migration'ları uygula

```bash
cd backend
set -a && . ../.env && set +a

dotnet tool restore
dotnet ef database update \
  --project src/T3.Infrastructure/T3.Infrastructure.csproj \
  --startup-project src/T3.Infrastructure/T3.Infrastructure.csproj
```

### 4. Backend

```bash
cd backend
set -a && . ../.env && set +a
ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/T3.Api/T3.Api.csproj
```

- API: http://localhost:5080
- Swagger: http://localhost:5080/swagger
- Sağlık: http://localhost:5080/health · http://localhost:5080/health/db

### 5. Frontend

```bash
cd frontend
npm install
npm run dev
```

Arayüz: http://localhost:5173 — `/api` ve `/health` istekleri Vite proxy'si
üzerinden backend'e gider, tarayıcıda CORS devreye girmez.

## Demo hesapları

Backend `Development` ortamında ilk açılışta **demo verisini kendisi yükler**
(5 program, 32 girişim, 41 program katılımı, 97 yatırım/hibe/ciro/ihracat/ödül
kaydı, kilometre taşları, 5 doküman ve 11 onay isteği). Tüm hesapların şifresi
aynıdır: `T3.Creathon!2026`

| E-posta | Rol | Ne görür |
|---|---|---|
| `admin@t3ekosistem.test` | Süper Yönetici | 32 girişimin tamamı, tüm hassas alanlar |
| `kulucka.yoneticisi@t3ekosistem.test` | Program Yöneticisi | Yalnızca Ön Kuluçka + Kuluçka'daki 16 girişim; vergi no **göremez** |
| `teknofest.yoneticisi@t3ekosistem.test` | Program Yöneticisi | Yalnızca TEKNOFEST/DENEYAP/Hızlandırma'daki 16 girişim |
| `karar.verici@t3ekosistem.test` | Karar Verici | 32 girişimin tamamı; tekil tutarlar ve kişisel veriler maskeli, ekosistem **toplamları** açık |
| `girisim@t3ekosistem.test` | Girişim Kullanıcısı | Yalnızca Anadolu Robotik |
| `girisim.marmara@t3ekosistem.test` | Girişim Kullanıcısı | Yalnızca Marmara Biyoteknoloji |
| `girisim.toros@t3ekosistem.test` | Girişim Kullanıcısı | Yalnızca Toros Uzay Bileşenleri |
| `girisim.trakya@t3ekosistem.test` | Girişim Kullanıcısı | Yalnızca Trakya Tarım Teknolojileri |

> Bu hesaplar yalnızca yerel geliştirme ve demo içindir. Tohumlayıcı üretim
> ortamında hiçbir koşulda çalışmaz; şifre `T3_Seed__Password` ile,
> yükleme `T3_Seed__Enabled=false` ile kapatılabilir.
>
> Verinin tamamı **kurgudur**. E-posta ve alan adları `.test` uzantısını
> (RFC 6761) kullanır, telefonlar tahsis edilmemiş bir önek taşır.

İki Program Yöneticisi hesabı bilinçli olarak ayrık programlara atanmıştır:
aynı ucu çağırdıklarında tamamen farklı girişim listesi ve tamamen farklı bir
ekosistem panosu görürler. Rol bazlı yetkilendirmeyi göstermenin en hızlı yolu
bu iki hesapla giriş yapmaktır.

Tohum verisindeki boşluklar da kasıtlıdır: dört girişim hiçbir programa bağlı
değil, biri hiç başarı kaydı taşımıyor — "kapsam dışı" ve "boş durum" ekranları
demoda gerçek veriyle görünsün diye.

## API yüzeyi (Faz 5)

| Uç | Yetki |
|---|---|
| `POST /api/auth/login` | herkese açık — **IP + e-posta başına** dakikada 10 istek |
| `GET /api/me` | kimlik doğrulanmış |
| `GET /api/startups` | kimlik doğrulanmış — satırlar role göre daraltılır |
| `GET /api/startups/{id}` | kimlik doğrulanmış — hassas alanlar role göre maskelenir |
| `GET /api/startups/{id}/timeline` | kimlik doğrulanmış |
| `POST /api/startups` · `PUT /api/startups/{id}` | Süper Yönetici, Program Yöneticisi |
| `DELETE /api/startups/{id}` | Süper Yönetici — bağlı kayıtları da pasife alır |
| `POST/PUT/DELETE /api/startups/{id}/team[/{memberId}]` | Süper Yönetici, Program Yöneticisi |
| `GET /api/startups/{id}/achievements` | kimlik doğrulanmış — tutarlar role göre maskelenir |
| `POST/PUT/DELETE /api/startups/{id}/achievements[/{achievementId}]` | Süper Yönetici, Program Yöneticisi |
| `GET /api/startups/{id}/documents` | kimlik doğrulanmış — Karar Verici listeyi göremez |
| `POST /api/startups/{id}/documents` | yetkili doğrudan kaydeder, girişim kullanıcısı onaya gönderir |
| `DELETE /api/startups/{id}/documents/{documentId}` | Süper Yönetici, Program Yöneticisi |
| `GET /api/documents/{id}/download` | kimlik doğrulanmış — her indirme denetim izine yazılır |
| `GET /api/programs` | kimlik doğrulanmış — kapsama göre daraltılır |
| `POST /api/participations` | Süper Yönetici, Program Yöneticisi (kendi programı) |
| `POST /api/change-requests` | Girişim Kullanıcısı — yetki role değil girişim bağına dayanır |
| `GET /api/change-requests` | kimlik doğrulanmış — yetkiliye kuyruk, girişime kendi geçmişi |
| `GET /api/change-requests/{id}` | kimlik doğrulanmış — before/after diff, alanlar role göre maskeli |
| `POST /api/change-requests/{id}/approve` · `/reject` | Süper Yönetici, Program Yöneticisi (kendi kapsamı) |
| `GET /api/reports/ecosystem` | kimlik doğrulanmış — sayılar kapsamla daralır, tutarlar role göre maskelenir |
| `GET /api/reports/export` | kimlik doğrulanmış — CSV; maskeli hücre "yetkiniz yok" yazar, her aktarma denetim izine düşer |
| `POST /api/ai/ask` | kimlik doğrulanmış, kullanıcı başına dakikada 20 istek — yanıt yalnızca kullanıcının görebildiği kayıtlardan üretilir |
| `GET /api/ai/startups/{id}/summary` | kimlik doğrulanmış — kart ve kronolojiden üretilen yönetici özeti |
| `POST /mcp` | kimlik doğrulanmış — JSON-RPC 2.0; araçlar aynı Application handler'larını sarar |
| `GET /api/audit-logs` | Süper Yönetici |
| `GET/POST /api/users` · `PUT /api/users/{id}[/password]` · `DELETE /api/users/{id}` | Süper Yönetici |

Yetkilendirme varsayılan olarak kapalıdır (`FallbackPolicy`): üst veri
taşımayan her uç kimlik ister, herkese açık uçlar bunu açıkça belirtir.

## AI katmanı

Üç uç aynı Application handler'larını kullanır; AI için **paralel bir veri yolu
yoktur**, dolayısıyla kapsam ve maskeleme kuralları tek yerde kalır.

- `POST /api/ai/ask` — doğal dil soru. Yanıtın altında **hangi araç çağrıldı**
  listesi durur; AI burada karar verici değil karar *destek* katmanı.
- `GET /api/ai/startups/{id}/summary` — girişim kartındaki yönetici özeti.
  Tutarları göremeyen role tutarsız özet üretilir ve bunu ekranda söyler.
- `POST /mcp` — JSON-RPC 2.0 MCP sunucusu (`initialize`, `ping`, `tools/list`,
  `tools/call`). Altı araç: `search_startups`, `get_startup_card`,
  `get_program_history`, `list_achievements`, `ecosystem_stats`,
  `list_pending_approvals`. Uç kimlik ister: MCP istemcisi de Bearer jetonu
  taşımak zorunda, araçlar jetonun rolüyle çalışır.

`T3_Ai__ApiKey` boşsa uygulama çalışmaya devam eder: soru yerel planlayıcıya
düşer, anahtar kelimelerden araç çağrıları üretilir ve yanıt araç özetlerinin
birleşiminden kurulur. Cümle üretilmediği için uydurma da üretilmez. Arayüz
hangi modun yanıtladığını rozetle söyler ("Model: …" / "Yerel plan (model
yok)") — demo internet ya da kota olmadan da çalışır.

## Proje yapısı

```
backend/
├── src/
│   ├── T3.Domain/          entity'ler, enum'lar, iş kuralları — bağımlılığı yok
│   ├── T3.Application/     use-case'ler (Features/ altında dikey dilimler), RBAC
│   ├── T3.Infrastructure/  EF Core, kimlik, depolama, denetim, MCP
│   └── T3.Api/             Minimal API endpoint'leri, middleware
└── tests/
    └── T3.Application.Tests/

frontend/src/
├── api/         backend tipleri
├── features/    backend dilimleriyle simetrik
├── components/  paylaşılan arayüz parçaları
└── lib/         HTTP istemcisi, query client
```

Bağımlılık yönü: `Api → Application → Domain`, `Infrastructure → Application`.

## Yararlı komutlar

```bash
# Derleme ve testler
cd backend && dotnet build && dotnet test

# Yeni migration
cd backend && set -a && . ../.env && set +a
dotnet ef migrations add <Ad> \
  --project src/T3.Infrastructure/T3.Infrastructure.csproj \
  --startup-project src/T3.Infrastructure/T3.Infrastructure.csproj \
  --output-dir Persistence/Migrations

# Frontend tip kontrolü ve üretim derlemesi
cd frontend && npm run build && npm run lint

# Çalışan sisteme karşı doğrulama (bkz. scripts/README.md)
python3 scripts/e2e_faz3.py      # onay akışı, denetim izi, kullanıcı yönetimi
python3 scripts/e2e_faz4.py      # başarı kayıtları ve dokümanlar
python3 scripts/e2e_faz5.py      # pano, CSV, AI uçları, MCP sunucusu
python3 scripts/render_faz3.py   # headless Chrome'da gerçek render
python3 scripts/render_faz4.py
python3 scripts/render_faz5.py

# Veritabanını sıfırla
docker compose down -v && docker compose up -d postgres
```

## Durum

**Faz 0 tamamlandı** — çözüm iskeleti, veri modeli (12 tablo), Docker Postgres,
JWT altyapısı, Swagger, sağlık uçları, frontend iskeleti.

**Faz 2 tamamlandı** — MVP #1 ve #2 çalışıyor:

- **Kimlik ve yetki:** login + JWT, `/api/me`, rol politikaları, varsayılan
  kapalı yetkilendirme, satır düzeyi kapsam (`IStartupScope`) ve alan düzeyi
  KVKK maskelemesi (`StartupVisibility`)
- **MVP #1 — merkezi girişim kartı:** arama/filtre/sıralama/sayfalama, tam
  kart, ekip yönetimi, denetim izi
- **MVP #2 — gelişim yolculuğu:** program katılımları, başarı kayıtları ve
  kilometre taşlarından sorgu anında üretilen tek kronoloji

**Faz 3 tamamlandı** — MVP #3 uçtan uca çalışıyor:

- **Onay akışı:** girişim kullanıcısı hiçbir tabloya doğrudan yazmıyor; her
  değişiklik `ChangeRequest` olarak kuyruğa giriyor. Onay anında gövde yeniden
  doğrulanıyor ve ad tekilliği yeniden kontrol ediliyor — gönderimle karar
  arasında günler geçebilir.
- **Girişim portalı:** kendi profilini ve ekibini düzenleme önerileri, kendi
  isteklerinin durumu ve ret gerekçeleri.
- **Onay kuyruğu + diff:** alan alan before/after karşılaştırması, bekleme
  süresi rozeti, durum sayıları. Maskelenmiş alan satırdan silinmiyor;
  "değişiyor ama göremezsiniz" olarak gösteriliyor.
- **Denetim izi ucu:** kim, ne zaman, neyi değiştirdi — ham gövdeleriyle.
  Onay iki satır yazar (`ChangeRequest.Approve` + `Startup.Update`), böylece iz
  değişikliğin portaldan mı doğrudan mı geldiğine bakmadan aynı sorgulanır.
- **Kullanıcı yönetimi:** Faz 1'den ertelenen CRUD; rol–kapsam bağı zorunlu,
  hesaplar silinmez pasife alınır, kendi hesabını kilitleme koruması var.
- **Soft delete zinciri:** `DELETE /api/startups/{id}` bağlı tüm kayıtları elle
  yürüyerek pasife alır ve kaç kaydın etkilendiğini raporlar.
- **Demo verisi:** 5 program, 12 kurgu girişim, 8 hesap, 11 onay isteği
  (bekleyen, onaylanmış ve gerekçesiyle reddedilmiş örnekler dâhil).

**Faz 4 tamamlandı** — MVP #4 uçtan uca çalışıyor:

- **Başarı ve finans kayıtları:** yatırım turu, hibe, ciro, ihracat ve ödül tek
  tabloda (TPH) ama beş güçlü tipli sınıf olarak duruyor. Gövde tek, doğrulama
  türe bağlı: mali yılı olmayan ciro, tutarı olmayan yatırım turu kaydedilemez.
  Kayıt türü sonradan değiştirilemez (409) — tür, satırın kimliğinin parçası.
- **Tutar maskelemesi:** Karar Verici satırı görür, meblağı görmez. Yanıt
  `amountMasked` taşır; arayüz maskelenen tutarı `0 ₺` diye göstermez.
- **Doküman yükleme/indirme:** yerel diske yazan `IDocumentStorage`, uzantı
  beyaz listesi ve 20 MB sınırı. İçerik tipi istemciden alınmaz, uzantıdan
  türetilir; indirme `nosniff` başlığıyla döner ve **her indirme denetim izine
  yazılır**.
- **Yükleme de onay akışından geçer:** yetkilinin dosyası doğrudan kaydedilir,
  girişim kullanıcısının dosyası depoya alınıp `ChangeRequest` olarak kuyruğa
  girer. Onaylanınca kayıt oluşur, reddedilince depoya yazılan dosya silinir.
- **Doğrulanmışlık:** portaldan gelen kayıt onaylanana kadar "doğrulanmadı"
  kalır; onaylayan yetkili kayıtta doğrulayan olarak yazılır.

Arayüzde hassas alanlar "veri yok" (—) ile "yetkiniz yok" (🔒) ayrımını
gösterir; bu ayrım kasıtlıdır, boş kutu kullanıcıyı yanıltır.

**Faz 5 tamamlandı** — karar destek katmanı çalışıyor:

- **Ekosistem panosu:** girişim/katılım/yatırım/hibe KPI'ları ve altı grafik
  (sektör, program, yatırım turu, yıllara göre yatırım, şehir, en çok yatırım
  alan girişimler). Pano girişim listesiyle **aynı kapsam filtresinden** geçer:
  Program Yöneticisi burada da yalnızca kendi programlarının karnesini görür.
- **Agregat/satır maskeleme ayrımı:** Karar Verici ekosistem toplamını görür,
  tekil girişimin tutarını görmez — sıralama listesinde tutar yerine 🔒 durur.
  Kural rapora özel bir bileşene değil, `StartupVisibility.Aggregate` içine
  yazıldı; RBAC'ın üç tek noktası korunuyor.
- **CSV dışa aktarma:** ekrandaki süzgeçlerle, Excel'in Türkçe kurulumuna göre
  (`;` ayraç, UTF-8 + BOM, `tr-TR` sayı). Maskeli hücre boş değil "yetkiniz yok"
  yazar, formül enjeksiyonuna karşı hücreler öneklenir ve her aktarma denetim
  izine düşer.
- **MCP sunucusu:** elle yazılmış JSON-RPC 2.0 uç (`POST /mcp`), altı araç. Aynı
  Application handler'larını sardığı için kapsam ve maskeleme REST ile bire bir
  aynı — uçtan uca betik MCP ve REST'in aynı sayıyı verdiğini doğruluyor.
- **AI karar destek:** doğal dil sorusu ve girişim kartı yönetici özeti. Anahtar
  yoksa yerel planlayıcı devreye giriyor; her yanıtın altında hangi aracın
  çağrıldığı listeleniyor. Ayrıntı: [AI katmanı](#ai-katmanı).
- **Gerçekçi demo verisi:** 32 girişim, 41 program katılımı, 97 başarı kaydı —
  bilinçli boşluklarıyla birlikte.

**Denetim Dalga 0 tamamlandı** — ürün denetiminin videodan önce kapatılması
gereken bulguları ([plan](docs/Denetim_Duzeltme_Plani.md)):

- **Girişim/ekip/katılım yazma yolları arayüze açıldı.** "Girişimi sisteme kim
  ekliyor?" sorusunun cevabı artık ekranda: `/girisimler`'de **Yeni girişim**,
  kartta **Düzenle** ve **Kaydı pasife al** (yalnızca Süper Yönetici), ekip
  sekmesinde ekle/düzenle/çıkar, programlar sekmesinde **Programa ekle**.
  Portalın formları `features/startups/` altına taşındı ve `mode` propuyla iki
  yolu birlikte besliyor: girişim kullanıcısı hâlâ yalnızca öneri gönderiyor.
- **Program Yöneticisi yeni kayıttan sonra doğrudan katılım adımına düşüyor** —
  kapsamı "programlarımdan geçmiş girişimler" olduğu için kayıt bir program
  dönemine bağlanmadan kendi listesinde görünmüyor; ekran bu kuralı yazıyor.
- **Giriş hız sınırı bölümlendi:** bir hesaba yapılan kaba kuvvet denemesi artık
  diğer kullanıcıların girişini kilitlemiyor; reddedilen yanıt `Retry-After`
  taşıyor.
- **AI anahtarı gerçekten okunuyor** (`T3_Ai__ApiKey`) ve anahtar boşsa açılışta
  uyarı log'u düşüyor — sessiz yedek mekanizma bir daha yanıltmasın.
- **Sekme başlığı, sayfa dili ve meta etiketleri** eklendi; her ekranın başlığı
  H1'iyle aynı, girişim kartında girişim adı.
- **404 ekranı:** bilinmeyen adres sessizce panoya yönlendirilmiyor.
- **Telefonda yatay taşma kapandı:** 360/375/414 px'te hiçbir ekran yatay
  kaymıyor.
- **Yazma yolunda maskeleme boşluğu kapandı** (denetimde görülmemişti): tam
  değiştirmeli `PUT`, maskeli alanı istemciye `null` gönderdiği için geri
  yazarken **siliyordu** — vergi numarasını göremeyen Program Yöneticisi'nin her
  düzenlemesi numarayı boşaltırdı. Sunucu artık göremediği alanı korur, arayüz
  o alanı formda hiç göstermez ve gerekçesini yazar.

**Doğrulama:** 156 birim testi, API'ye gerçek rollerle vuran 307 uçtan uca
kontrol (81 + 123 + 103) ve headless Chrome'da 201 render kontrolü
(32 + 41 + 53 + 75) — hepsi temiz veritabanında geçiyor. Render adımı yine iş gördü:
Faz 5'te panelin `data-testid`'sini yutan `Card` bileşenini ve Karar Verici'ye
tekil tutar sızdıran ilk maskeleme sürümünü bu adım yakaladı. Betikler ve
çalıştırma sırası: [scripts/](scripts/).

Sıradaki: **Creathon haftası** — cilalama, sunum ve Demo Day. Faz listesi için
bkz. [teknik plan](docs/Problem7_Teknik_Plan.md#8-faz-planı).

### Bilinen açık işler

Denetim raporunun Dalga 1 ve Dalga 2 maddeleri açık:
[Denetim_Duzeltme_Plani.md](docs/Denetim_Duzeltme_Plani.md). Başlıklar: program
ve dönem yönetimi arayüzü yok (programlar yalnızca tohumlayıcıyla oluşuyor),
şifre kurtarma/değiştirme uçları yok, erişim jetonu 15 dakikalık ve yenileme
yok, KVKK aydınlatma metni ve başvuru yolu yok, üretim dağıtım yolu (tek origin
statik sunum) yok.

- Doküman deposu yerel disk; S3 uyumlu sürüm aynı arayüzün arkasında duruyor
  ama henüz yazılmadı.
- Onaylanmayı bekleyen doküman yüklemeleri hiç karara bağlanmazsa dosyaları
  depoda kalır; süreli temizlik işi yok. Girişim başına yükleme kotası da yok —
  boyut ve tür sınırı var, sayı sınırı yok.
- Pano agregasyonu bellekte yapılıyor (sorgu kapsamla daraltıldıktan sonra);
  ekosistem ölçeğinde sorun değil ama on binlerce kayıtta veritabanı tarafına
  taşınması gerekir.
- CSV dışa aktarma 2000 satırla sınırlı ve tek seferde üretiliyor; akış
  (streaming) ya da arka plan işi yok.
- AI sohbeti tek soruluk; oturum geçmişi tutulmuyor, önceki soruya atıf
  yapılamıyor.
