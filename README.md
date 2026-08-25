# T3 Girişim Ekosistemi Yönetim Sistemi

Programdan yatırıma, T3 girişimcilik ekosisteminin tek kurumsal hafızası ve karar destek platformu.

T3 Vakfı Bursiyer Yapay Zekâ Creathonu — **Problem 7** çözümü.

## Belgeler

| Belge | İçerik |
|---|---|
| [docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md](docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md) | Ürün gereksinimleri, roller, zorunlu MVP maddeleri, program kuralları |
| [docs/Problem7_Teknik_Plan.md](docs/Problem7_Teknik_Plan.md) | Mimari, veri modeli, yetki matrisi, API yüzeyi, faz planı |
| [docs/Gelistirme_Kararlari.md](docs/Gelistirme_Kararlari.md) | Yerleşik teknik kararlar, reddedilen alternatifler, ortam tuzakları |
| [docs/Denetim_Duzeltme_Plani.md](docs/Denetim_Duzeltme_Plani.md) | Ürün denetiminin bulguları, üç dalgalık düzeltme planı ve her maddenin nerede doğrulandığı |
| [docs/Is_Modeli_Kanvasi.md](docs/Is_Modeli_Kanvasi.md) | İş modeli kanvası (26 Ağustos teslimi) — **taslak**, ekip onayı bekliyor |
| [docs/Demo_Senaryosu.md](docs/Demo_Senaryosu.md) | Prototip videosu çekim planı, 5 dk pitch iskeleti, jüri soruları — **taslak** |
| [CLAUDE.md](CLAUDE.md) | Yapay zekâ asistanı için kısa proje sözleşmesi — kurallar ve belge dizini |
| [scripts/README.md](scripts/README.md) | Uçtan uca ve render doğrulama betikleri, çalıştırma sırası |

## Teknoloji

- **Backend:** .NET 8 · Clean Architecture + dikey dilim · Minimal API · EF Core 8
- **Frontend:** React 19 · TypeScript · Vite · Tailwind CSS 4 · TanStack Query
- **Veritabanı:** PostgreSQL 16 (Docker)
- **AI:** MCP sunucusu (.NET içinde) — model bağlantısı **MCP üzerinden** kurulur; uygulama içi Claude API anahtarı opsiyonel ve bu kurulumda tanımlı değil

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

## API yüzeyi

| Uç | Yetki |
|---|---|
| `POST /api/auth/login` | herkese açık — **IP + e-posta başına** dakikada 10 istek; jetonu `HttpOnly` çerezde ve gövdede döner |
| `POST /api/auth/logout` | herkese açık — oturum ve CSRF çerezlerini siler |
| `POST /api/auth/forgot-password` | herkese açık — yanıt adresin kayıtlı olup olmadığını **söylemez** |
| `POST /api/auth/reset-password` | herkese açık — jeton tek kullanımlık ve 2 saat geçerli |
| `POST /api/auth/change-password` | kimlik doğrulanmış — mevcut şifre yeniden doğrulanır |
| `GET /api/me` | kimlik doğrulanmış — `mustChangePassword` bayrağını da taşır |
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
| `POST /api/programs` · `PUT /api/programs/{id}` · `DELETE /api/programs/{id}` | **yalnızca Süper Yönetici** — program listesi aynı zamanda yetki kapsamının tanımı |
| `POST/PUT/DELETE /api/programs/{id}/terms[/{termId}]` | Süper Yönetici, Program Yöneticisi (kendi programı) — katılımı olan dönem kapatılamaz (409) |
| `POST /api/participations` | Süper Yönetici, Program Yöneticisi (kendi programı) |
| `PUT /api/participations/{id}` · `DELETE /api/participations/{id}` | Süper Yönetici, Program Yöneticisi (kendi programı) |
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

### Bu kurulumda model bağlantısı: yalnızca MCP

Sistem, dil modelini kendi içine gömmek yerine **MCP sunucusu olarak
yayımlanıyor**: harici bir ajan (Claude Desktop, Claude Code vb.) `POST /mcp`
ucuna kendi Bearer jetonuyla bağlanır ve yukarıdaki altı aracı kullanır — kendi
rolünün yetkisi kadar görerek. Model istemcide, veri sunucuda kalır; kurumun
API kotası ve anahtarı uygulamaya girmez.

Bunun sonucu ürün içinde de görünür: `T3_Ai__ApiKey` tanımlı değil, dolayısıyla
panel içi sorular yerel planlayıcıyla yanıtlanıyor. Yerel planlayıcı anahtar
kelimelerden araç çağrıları üretir ve yanıtı araç özetlerinin birleşiminden
kurar; cümle üretmediği için uydurma da üretmez. Panel bu durumu rozetle
("Model: …" / "Yerel plan (model yok)") ve kalıcı bir bilgi notuyla söylüyor —
kullanıcı hangi modun çalıştığını ekranda okumalı. Anahtar bir gün girilirse
aynı uç modele bağlanır, başka değişiklik gerekmez.

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
python3 scripts/render_faz6.py   # Dalga 0: yazma yolları, 404, mobil taşma
python3 scripts/render_faz7.py   # Dalga 1: program/dönem, şifre kurtarma, KVKK, iz
python3 scripts/render_faz8.py   # Dalga 2: çerez/CSP/CSRF, tek origin, URL durumu

# Veritabanını sıfırla (betikler temiz tohum verisi bekliyor)
docker compose down -v && docker compose up -d postgres
```

### Üretim benzeri yığın

Arayüzü artık API'nin kendisi sunuyor (tek origin: CORS yok, vekil yok):

```bash
cp .env.prod.example .env.prod          # değerleri doldurun
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
# → http://127.0.0.1:8080 (dışa açan yüz, TLS sonlandıran vekil olmalı)
```

Konteyner şemayı kendisi kuruyor (`T3_Database__MigrateOnStartup=true`) ve tohum
verisi **çalışmıyor** — ortam `Production`. İmaj bu makinede derlenip boş bir
veritabanına karşı kaldırıldı (`t3-ekosistem:local`, 339 MB): göçler uygulandı,
tohumlama kapalı kaldı, süreç `uid=1654(app)` ile koştu, `/health` 200, derin
bağlantı index.html'e düştü, `/api/yok` JSON 404 verdi, CSP ve `no-store`
başlıkları yerindeydi. `docker buildx` kurulu değilse `DOCKER_BUILDKIT=0`
gerekir; docker köprüsü `DOWN` ise derleme `--network host` ister (yoksa
`npm ci` ağsız kalır).

### Marka ve logo

İşaret kurumun kendi logosu: TGM — dört renkli tek blok (kırmızı T, antrasit
işaret, turuncu G, mavi M). Kaynak `design_handoff_logo_paketi/` altındaki
handoff paketi; arayüzdeki dosyalar `frontend/public/marka/`. Handoff'un kuralı
kodda da geçerli: harf kompozisyonu, renk sırası ve blok oranları
değiştirilmiyor, işaret bir bütün olarak yerleştiriliyor — bu yüzden
[Logo.tsx](frontend/src/components/Logo.tsx) çizim değil dosya kullanıyor.

- **İki dosya, iki tema:** renkli sürüm açık zeminde, tek renk beyaz sürüm koyu
  zeminde (`dark:` sınıfları). Seçim CSS'te; tema değişince ikinci bir render
  beklenmiyor.
- **Arayüz dosyaları kırpılmış** (`*-isaret*.png`, 333×232): özgün 447×447
  karenin şeffaf kenar boşluğu, 40 piksellik bir kutuda işareti gereksiz
  küçültüyordu. İşaret değişmedi; "net alan" kuralı düzenin kendi boşluğuyla
  veriliyor.
- **Sekme simgesi** aynı işaretten üretildi (`favicon-32.png`,
  `apple-touch-icon.png`). Önceki simge Vite'ın mor şimşeğiydi.
- **`/marka`** — logo paketi sayfası ([BrandKitPage.tsx](frontend/src/features/brand/BrandKitPage.tsx)):
  ana işaret kuralları, dört rengin HEX/RGB/CMYK/Pantone değerleri (renk alanına
  tıklayınca HEX panoya kopyalanıyor), üç zemin modu (açık / koyu / fotoğraf
  üzerinde) ve üç yanlış kullanım örneği. Oturum istemiyor: sponsor ve
  partnerler de doğru kullanımı görebilmeli.
- **Barlow ve Barlow Condensed kendi sunucumuzda** (`frontend/public/fonts/`,
  yalnızca latin + latin-ext, 164 kB). CSP `font-src 'self'` diyor; Google
  Fonts'a bağlanmak o sınırı gevşetmek **ve** her ziyaretçinin IP'sini yurt
  dışına göndermek olurdu. Yüzler yalnızca `/marka` sayfasında kullanıldığı için
  başka hiçbir ekranda indirilmiyor.

> Vektör aslı geldiğinde `tgm-logo-renkli.svg` ve `tgm-logo-beyaz.svg` üretilip
> PNG'lerin yerine konmalı (handoff da bunu söylüyor): şeffaf ve beyaz sürümler
> PNG'den türetildiği için kenarları tam keskin değil. `/marka` sayfasındaki
> örnek fotoğraf da yer tutucu — kurumun gerçek görseliyle değişecek.

### Canlı demo dağıtımı (VPS)

Sistem 25 Ağustos'ta bir Ubuntu 24.04 VPS'e kuruldu ve
**https://t3girisimportali.com** adresinde çalışıyor (Let's Encrypt sertifikası;
konteyner 8090'da dinliyor, önünde host'taki nginx TLS'i sonlandırıyor). Sunucuda başka beş compose projesi olduğu için dağıtım
onlardan tamamen ayrı duruyor: kendi compose projesi (`t3ekosistem`), kendi ağı
ve hacimleri, kullanılmayan bir port (8090). Diğer yığınların portlarına
(6540/8080/8081/3001/5246/5341/12000/12001) ve dosyalarına dokunulmadı.

Dosyalar sunucuda `/opt/t3ekosistem/`: `docker-compose.yml` (imajı derlemez,
yükler), `.env` (sırlar **sunucuda** üretildi, `chmod 600`, depoya girmez).

İki karar açıklama istiyor:

- **İmaj yerelde derlenip taşındı** (`docker save | ssh 'docker load'`).
  Sunucuda 5 GB boş disk var; .NET SDK imajı + `npm ci` + NuGet önbelleği bunu
  yiyip diğer projeleri riske atardı.
- **Demo verisi tohumlayıcıyla değil veritabanı dökümüyle geldi.** Tohumlayıcı
  `IsDevelopment()` ile korunuyor ve bu bir güvenlik sınırı — canlı kopyayı
  "Development" yapıp tohumlamak o sınırı gevşetmek olurdu. Yerelde temiz bir
  veritabanı tohumlanıp `pg_dump` ile taşındı (23 KB); uygulama `Production`
  ortamında, `Seed:Enabled=false` ile çalışıyor.

```bash
# Güncelleme (kod değiştiğinde): imajı yeniden derle, taşı, konteyneri yenile
DOCKER_BUILDKIT=0 docker build --network host -t t3-ekosistem:vps .
docker save t3-ekosistem:vps | gzip -1 | ssh root@SUNUCU 'gunzip | docker load'
ssh root@SUNUCU 'cd /opt/t3ekosistem && docker compose -p t3ekosistem up -d'

# Doğrulama (gerçek tarayıcıda, dağıtılan kopyaya karşı)
T3_VPS_BASE=https://t3girisimportali.com python3 scripts/render_vps.py
```

Doğrulandı (11 render kontrolü, 0 başarısız): giriş ekranı ve KVKK onay kapısı,
CSP'nin kendi paketini engellemediği, 32 girişimin listelenmesi, panonun
çizilmesi, Karar Verici'nin denetim izine girememesi, konsolda sıfır hata.
Ayrıca `/api/olmayan-uc` JSON 404 dönüyor, `Auth.KvkkConsent` izi düşüyor,
güvenlik başlıkları yerinde.

**80 portu ve nginx.** Sunucuda zaten bir nginx ve bir site vardı
(`mesutyesiloren.com` → `localhost:3001`). Hiçbir dosya silinmedi: kendi
bloğumuz (`/etc/nginx/sites-available/t3ekosistem`) **`default_server`** olarak
eklendi, yani IP ile ya da tanınmayan `Host` başlığıyla gelen istek bize düşüyor,
o alan adıyla gelen istek eskisi gibi kendi sitesine gidiyor (nginx önce
`server_name` eşleşmesine bakar). Eski yapılandırmanın kopyası
`/root/nginx-yedek-20260825/` altında. Vekil `X-Forwarded-For`/`-Proto`
gönderiyor ve uygulama bu başlıkları **yalnızca** güvenilen vekilden kabul
ediyor (`Hosting:TrustedProxies` = docker ağ geçidi); doğrulandı: denetim izine
vekilin adresi değil gerçek istemci IP'si düşüyor. `client_max_body_size 25m`
gerekliydi — nginx'in 1 MB varsayılanı 20 MB'lık doküman sınırını görünmez kılar
ve yükleme daha uygulamaya varmadan 413 alırdı.

**TLS (25 Ağustos akşamı).** `t3girisimportali.com` sunucuya yönlendirildi ve
sertifika `certbot --nginx` ile alındı (`t3girisimportali.com` +
`www.t3girisimportali.com`, 23 Kasım'a kadar geçerli). Uygulama tarafında
`Hosting:RequireHttps=true` açıldı: HSTS geliyor ve oturum çerezi artık `secure`
bayraklı — çerezin bayrağı isteğin şemasına bağlı olduğu için kod değişmedi,
şema değişti. TLS'i vekil sonlandırdığı için karar `X-Forwarded-Proto` ile
geliyor ve o başlık yalnızca güvenilen vekilden kabul ediliyor; ikisi birlikte
olmasa uygulama ya sonsuz yönlendirmeye girerdi ya da HTTP'yi HTTPS sanardı.

Alan adı dışından gelen istekler (IP, tanımsız `Host`) kanonik adrese 301 ile
gidiyor: certbot 80 bloğuna `return 404` bırakıyor ve IP ile aranan bir demo
adresinin boşluğa düşmesi iyi görünmüyordu.

> ⚠️ **Sunucuda sertifika yenileme otomasyonu yoktu.** certbot `/opt/certbot`
> altında elle (venv) kurulmuş; ne systemd zamanlayıcısı ne cron kaydı vardı —
> aynı makinedeki başka bir projenin sertifikası bu yüzden Temmuz'da dolmuş.
> Kendi sertifikamız için `certbot-renew.timer` kuruldu (günde iki kez,
> rastgele gecikmeli, `--cert-name t3girisimportali.com` ile **yalnızca** bizim
> sertifika). Kuru koşu başarılı.

Yığını kurmadan aynı yolu yerelde denemek için:

```bash
cd frontend && npm run build
rm -rf ../backend/src/T3.Api/wwwroot && cp -r dist ../backend/src/T3.Api/wwwroot
# API yeniden başlatılır; derlenmiş arayüz http://localhost:5080 adresinden gelir
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

**Denetim Dalga 1 tamamlandı** — MVP ve KVKK bütünlüğü
([plan](docs/Denetim_Duzeltme_Plani.md)):

- **Program ve dönem yönetimi arayüze açıldı.** `EcosystemProgram → ProgramTerm
  → ProgramParticipation` zincirinin ilk iki halkası artık tohumlayıcıya bağlı
  değil. Yetki bilinçli olarak ikiye ayrıldı: program *tanımı* yalnızca Süper
  Yönetici'de (program listesi aynı zamanda Program Yöneticisi'nin yetki
  kapsamının tanımı — kendi kapsamını büyütebilen rol RBAC'ı anlamsız kılar),
  dönem ve katılım işlemleri Program Yöneticisi'ne de açık ama yalnızca kendi
  programında. Katılımı olan dönem kapatılamaz (409): gelişim yolculuğunun
  kaynağı tek tıkla boşaltılmamalı.
- **Şifre kurtarma ve değiştirme:** `forgot-password`, `reset-password`,
  `change-password`. Jetonun kendisi değil **SHA-256 özeti** saklanır, tek
  kullanımlıktır ve 2 saat geçerlidir; `forgot-password` adresin kayıtlı olup
  olmadığını söylemez. Yöneticinin attığı şifre artık geçici:
  `mustChangePassword` bayrağı düşene kadar kullanıcı başka ekrana geçemez —
  amaç şifrenin ikinci sahibini ortadan kaldırmak. Gerçek SMTP yok; e-posta
  sunucunun diskindeki geliştirme kutusuna yazılıyor (`IEmailSender` arkasında),
  jeton HTTP yanıtında **hiç dönmüyor**.
- **Oturum ömrü ve ağ hatası ayrımı:** jeton 15 dakika yerine bir iş günü
  (yenileme jetonu Dalga 2'deki çerez kararına bağlı). Ağ hatası artık
  `ApiError(0)` olarak normalleşiyor: API kapalıyken kullanıcı oturumda kalıyor,
  ham "Failed to fetch" yerine Türkçe durum ve **yeniden dene** düğmesi görüyor.
  Jeton dolduğunda giriş ekranında gerekçeyi okuyor ve giriş sonrası kaldığı
  rotaya dönüyor.
- **Giriş olayları denetim izinde:** `Auth.LoginSucceeded`, `Auth.LoginFailed`,
  `Auth.RateLimited` ve şifre olayları. IP ve istemci bilgisi kaydediliyor,
  e-posta **maskeli** (`k***@alan.test`) yazılıyor — iz, denenen adreslerin ham
  listesine dönüşmemeli. Hız sınırı kilidi pencere başına **tek** satır açıyor:
  reddedilen istek sayısı sınırsız olduğu için her redde satır açmak izin
  kendisini bir saldırı yüzeyi yapardı.
- **KVKK onay kapısı:** giriş ekranındaki onay kutusu işaretlenmeden giriş
  düğmesi açılmıyor; onay bir metin sürümüne bağlı ve sunucuda denetim izine
  düşüyor (`Auth.KvkkConsent`, maskeli e-posta + sürüm). Tarayıcıdaki kayıt
  yalnızca "bir daha sormayalım" kolaylığı, kanıt izde.
- **KVKK metinleri:** `/kvkk-aydinlatma` ve `/kullanim-sartlari` giriş yapmadan
  açılıyor, her ekranın alt bilgisinden ve giriş ekranından erişiliyor, başvuru
  adresi metinde duruyor. Metinler görünür biçimde **taslak** işaretli: hukuki
  içerik T3 Vakfı onayını bekliyor.
- **Karar Verici'nin `/onaylar` ekranı:** sonsuza dek boş "Önerilerim" yerine
  yetki açıklaması.
- **AI tarafında karar netleşti: model bağlantısı yalnızca MCP üzerinden.**
  Uygulamaya anahtar gömülmüyor; sistem `POST /mcp` ile MCP sunucusu olarak
  yayımlanıyor ve harici ajan kendi jetonuyla bağlanıyor. Panel bunu kalıcı bir
  bilgi notuyla söylüyor — ayrıntı: [AI katmanı](#ai-katmanı).

**Dalga 2 tamamlandı** — canlıya çıkış altyapısı:

- **Tek origin dağıtım:** derlenmiş arayüzü API'nin kendisi sunuyor (SPA geri
  dönüşü API öneklerini dışarıda bırakıyor), kök `Dockerfile` (kök olmayan
  kullanıcı) ve `docker-compose.prod.yml` (postgres portu yayımlamıyor, API
  yalnızca loopback'e bağlanıyor, şemayı kendisi kuruyor, tohum verisi kapalı).
- **Jeton `HttpOnly` + `SameSite=Strict` çerezde**, `localStorage`'da değil;
  yeni `POST /api/auth/logout` çerezi sunucu tarafında siliyor. Çerezle
  kimliklenen yazma istekleri **CSRF** çift-gönderim jetonu istiyor; başlıkla
  gelen istekler (betik, MCP, Swagger) eskisi gibi çalışıyor.
- **Güvenlik başlıkları:** `default-src 'self'` CSP (script tarafı sıkı),
  `nosniff`, `Referrer-Policy`, `X-Frame-Options`, `Permissions-Policy` ve API
  yolunda `Cache-Control: no-store`. `X-Forwarded-*` yalnızca güvenilen vekil
  adına kabul ediliyor — denetim izindeki IP artık istemcinin uydurabildiği bir
  değer değil.
- **Kalan orta maddeler:** filtre/sıralama/sayfa URL'de (paylaşılabilir
  bağlantı, çalışan geri tuşu), aksansız arama aksanlı kaydı buluyor
  ("saglik" → "Sağlık"), kaydedilmemiş formdan çıkışta uyarı, sekmeler arası
  oturum senkronu, rota bazlı kod bölme (tek parça 400 kB → 323 kB + 14 parça),
  odak halkası %60 opaklık ve "İçeriğe atla" bağlantısı. Sentry **bağlanmadı**
  (hesap/DSN/KVKK aktarım kararı gerekiyor); yerine `ErrorBoundary` var.

**Doğrulama:** 191 birim testi, API'ye gerçek rollerle vuran 310 uçtan uca
kontrol (81 + 125 + 104) ve headless Chrome'da 352 render kontrolü
(32 + 41 + 53 + 75 + 97 + 54) — **temiz tohum verisiyle**. Betikler veriyi
değiştirdiği için (girişim pasife alma, kullanıcı oluşturma) tam yeşil bir zincir
sıfırlanmış veritabanı ister; ayrıntı [scripts/](scripts/). Render adımı yine iş
gördü: Faz 5'te panelin `data-testid`'sini yutan `Card` bileşenini, Karar
Verici'ye tekil tutar sızdıran ilk maskeleme sürümünü ve Dalga 2'de hem
derlenmeyen bir `AppShell`'i hem ekranda etkisiz kalan "Çıkış" düğmesini bu adım
yakaladı.

Sıradaki: **Creathon haftası** — cilalama, sunum ve Demo Day. Faz listesi için
bkz. [teknik plan](docs/Problem7_Teknik_Plan.md#8-faz-planı).

### Bilinen açık işler

Denetim planının üç dalgası da kapandı ([plan](docs/Denetim_Duzeltme_Plani.md)).
Geriye teknik olmayan ya da bu depoda karara bağlanamayan kalemler kaldı:

- **KVKK metinlerinin hukuki içeriği onaylanmadı** — teknik iş bitti, sayfalar
  görünür biçimde "Taslak" işaretli (Dalga 1.5).
- **Sentry bağlanmadı** — hesap, DSN ve yurt dışı aktarım kararı gerekiyor
  (Dalga 2.3 / O-06). Kod tarafında bağlanacak yer hazır.
- **Gerçek SMTP yok:** şifre sıfırlama e-postaları sunucunun diskindeki
  geliştirme kutusuna yazılıyor (`IEmailSender` arkasında sağlayıcı değişir).
  Arayüz akışı ve jeton yaşam döngüsü buna rağmen uçtan uca doğrulanıyor.

### Bilinçli sınırlar

Bunlar eksik değil, kapsam kararı — ölçek gerektirdiğinde değişecek yer belli:

- Yenileme jetonu yok; jeton dolduğunda kullanıcı gerekçeyi ekranda görerek
  yeniden giriş yapar.
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
