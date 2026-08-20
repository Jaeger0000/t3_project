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
JWT altyapısı, Swagger, sağlık uçları ve frontend iskeleti ayakta; uçtan uca
zincir doğrulandı.

Sıradaki: **Faz 1** — kimlik doğrulama, rol bazlı yetkilendirme ve kullanıcı
yönetimi. Faz listesi için bkz. [teknik plan](docs/Problem7_Teknik_Plan.md#8-faz-planı).
