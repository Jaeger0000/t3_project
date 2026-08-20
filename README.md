# T3 Girişim Ekosistemi Yönetim Sistemi

Programdan yatırıma, T3 girişimcilik ekosisteminin tek kurumsal hafızası ve karar destek platformu.

T3 Vakfı Bursiyer Yapay Zekâ Creathonu — **Problem 7** çözümü.

## Belgeler

| Belge | İçerik |
|---|---|
| [docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md](docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md) | Ürün gereksinimleri, roller, zorunlu MVP maddeleri, program kuralları |
| [docs/Problem7_Teknik_Plan.md](docs/Problem7_Teknik_Plan.md) | Mimari, veri modeli, yetki matrisi, API yüzeyi, faz planı |

## Teknoloji

- **Backend:** .NET 8 · Clean Architecture + dikey dilim · Minimal API · EF Core 8
- **Frontend:** React 19 · TypeScript · Vite · Tailwind CSS 4 · TanStack Query
- **Veritabanı:** PostgreSQL 16 (Docker)
- **AI:** MCP sunucusu (.NET içinde) + Claude API — *Faz 5*

## Kurulum

### Gereksinimler

.NET 8 SDK · Node.js 20+ · Docker & Docker Compose

### 1. Ortam değişkenleri

```bash
cp .env.example .env
```

`.env` içindeki iki değeri mutlaka doldurun:

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
(5 program, 12 girişim, ekipler, yatırım/hibe/ödül kayıtları, kilometre
taşları). Tüm hesapların şifresi aynıdır: `T3.Creathon!2026`

| E-posta | Rol | Ne görür |
|---|---|---|
| `admin@t3ekosistem.test` | Süper Yönetici | 12 girişimin tamamı, tüm hassas alanlar |
| `kulucka.yoneticisi@t3ekosistem.test` | Program Yöneticisi | Yalnızca Ön Kuluçka + Kuluçka'daki 6 girişim; vergi no **göremez** |
| `teknofest.yoneticisi@t3ekosistem.test` | Program Yöneticisi | Yalnızca TEKNOFEST/DENEYAP/Hızlandırma'daki 6 girişim |
| `karar.verici@t3ekosistem.test` | Karar Verici | 12 girişimin tamamı; tutarlar ve kişisel veriler maskeli |
| `girisim@t3ekosistem.test` | Girişim Kullanıcısı | Yalnızca Anadolu Robotik |

> Bu hesaplar yalnızca yerel geliştirme ve demo içindir. Tohumlayıcı üretim
> ortamında hiçbir koşulda çalışmaz; şifre `T3_Seed__Password` ile,
> yükleme `T3_Seed__Enabled=false` ile kapatılabilir.
>
> Verinin tamamı **kurgudur**. E-posta ve alan adları `.test` uzantısını
> (RFC 6761) kullanır, telefonlar tahsis edilmemiş bir önek taşır.

İki Program Yöneticisi hesabı bilinçli olarak ayrık programlara atanmıştır:
aynı ucu çağırdıklarında tamamen farklı girişim listesi görürler. Rol bazlı
yetkilendirmeyi göstermenin en hızlı yolu bu iki hesapla giriş yapmaktır.

## API yüzeyi (Faz 2)

| Uç | Yetki |
|---|---|
| `POST /api/auth/login` | herkese açık, dakikada 10 istek |
| `GET /api/me` | kimlik doğrulanmış |
| `GET /api/startups` | kimlik doğrulanmış — satırlar role göre daraltılır |
| `GET /api/startups/{id}` | kimlik doğrulanmış — hassas alanlar role göre maskelenir |
| `GET /api/startups/{id}/timeline` | kimlik doğrulanmış |
| `POST /api/startups` · `PUT /api/startups/{id}` | Süper Yönetici, Program Yöneticisi |
| `POST/PUT/DELETE /api/startups/{id}/team[/{memberId}]` | Süper Yönetici, Program Yöneticisi |
| `GET /api/programs` | kimlik doğrulanmış — kapsama göre daraltılır |
| `POST /api/participations` | Süper Yönetici, Program Yöneticisi (kendi programı) |

Yetkilendirme varsayılan olarak kapalıdır (`FallbackPolicy`): üst veri
taşımayan her uç kimlik ister, herkese açık uçlar bunu açıkça belirtir.

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
cd frontend && npm run build

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
- **Demo verisi:** 5 program, 12 kurgu girişim, 5 rol hesabı

Arayüzde hassas alanlar "veri yok" (—) ile "yetkiniz yok" (🔒) ayrımını
gösterir; bu ayrım kasıtlıdır, boş kutu kullanıcıyı yanıltır.

Sıradaki: **Faz 3** — `ChangeRequest` onay akışı (MVP #3), girişim portalı,
onay kuyruğu ve before/after diff görünümü, denetim izi ucu. Ayrıca Faz 1'in
kalan parçası olan kullanıcı yönetimi CRUD'u. Faz listesi için bkz.
[teknik plan](docs/Problem7_Teknik_Plan.md#8-faz-planı).

### Bilinen açık işler

- **Soft delete yalnızca sözleşme düzeyinde zincirleniyor.** Bağımlı varlıkların
  hepsi `ISoftDelete` uyguluyor ve sorgu filtreleri ebeveynleriyle uyumlu, ama
  bir girişim pasife alındığında çocuklarını işaretleyen kod henüz yok; silme
  handler'ı bu zincirlemeyi açıkça yapmalı.
- Doküman yükleme/indirme uçları Faz 4'te; kart şimdilik yalnızca doküman
  sayısını gösteriyor.
