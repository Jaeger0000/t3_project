# Teknik Plan — T3 Girişim Ekosistemi Yönetim Sistemi

> Ürün gereksinimleri için bkz. [Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md](Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md).
> Bu belge **nasıl** inşa edeceğimizi tanımlar: yığın, mimari, veri modeli, yetki matrisi, API yüzeyi ve faz planı.

---

## 1. Teknoloji Kararları

| Katman | Seçim | Gerekçe |
|---|---|---|
| Backend | **.NET 8** (`net8.0`, LTS) | Ekibin en iyi bildiği dil; DI, EF Core ve katmanlı yapı bu projenin onay akışı + RBAC ihtiyacına doğal oturuyor. Kurulu SDK 8.0.128. |
| Frontend | **React 18 + TypeScript + Vite** | Backend ayrı bir servis olduğu için Next.js'in SSR'ına gerek yok; Vite'ın dev server'ı anında açılıyor. |
| UI | Tailwind CSS + shadcn/ui | Jüriye gösterilecek ekranları sıfırdan CSS yazmadan üretmek için. |
| Veri erişimi | EF Core 8 + Npgsql | Migration'lar, TPH kalıtımı ve global query filter (soft delete + RBAC) hazır geliyor. |
| Veritabanı | **PostgreSQL 16** (Docker) | Yerelde Docker Compose, demoda yönetilen Postgres. |
| Kimlik doğrulama | Kendi kullanıcı/rol tablolarımız + **JWT** | Tam kontrol; KVKK anlatımı platforma bağımlı kalmıyor. |
| Doğrulama | FluentValidation | Handler'dan bağımsız, test edilebilir kural tanımı. |
| API dokümantasyonu | Swagger / OpenAPI | Frontend ekibi backend'i beklemeden tip üretebilir. |
| AI | **MCP sunucusu (.NET içinde)** + Claude API | `ModelContextProtocol` C# SDK; tool'lar REST'in kullandığı aynı handler'ları sarar. |
| Dosya depolama | Yerel disk → S3 uyumlu (demo) | `IDocumentStorage` arayüzü arkasında; ortam değişkeniyle takas edilir. |

### Bilinçli olarak **kullanmadığımız** şeyler

- **MediatR / CQRS pipeline** — ek soyutlama ve v13+ lisans sorusu; sade handler sınıfları aynı işi yapıyor.
- **Supabase / BaaS** — yetki mantığı platforma kaçardı, KVKK anlatımı zayıflardı.
- **Ayrı TypeScript MCP sunucusu** — üçüncü servis ve iki yerde yetki kontrolü demek.
- **Mikroservis** — bu ölçekte sadece maliyet.

---

## 2. Mimari

Clean Architecture'ın bağımlılık yönü, Application katmanında **dikey dilim** (feature folder) düzeniyle birlikte.

```
Api  ──────────►  Application  ──────────►  Domain
                        ▲
Infrastructure ─────────┘
```

**Kural:** Domain hiçbir şeye bağımlı değil. Application yalnızca Domain'i bilir ve ihtiyaç duyduğu dış dünyayı *arayüz* olarak tanımlar. Infrastructure bu arayüzleri uygular. Api sadece endpoint bağlar.

### Çözüm yapısı

```
t3_project/
├── docs/
│   ├── Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md
│   └── Problem7_Teknik_Plan.md
│
├── backend/
│   ├── T3.Ekosistem.sln
│   ├── src/
│   │   ├── T3.Domain/
│   │   │   ├── Common/            (Entity, IAuditable, ISoftDelete)
│   │   │   ├── Startups/          (Startup, TeamMember, StartupStatus)
│   │   │   ├── Programs/          (Program, ProgramTerm, Participation)
│   │   │   ├── Achievements/      (Achievement + türevleri)
│   │   │   ├── Documents/         (Document, DocumentType)
│   │   │   ├── Approvals/         (ChangeRequest, ChangeRequestStatus)
│   │   │   └── Identity/          (User, Role, UserProgramAssignment)
│   │   │
│   │   ├── T3.Application/
│   │   │   ├── Common/
│   │   │   │   ├── Interfaces/    (IAppDbContext, ICurrentUser,
│   │   │   │   │                   IDocumentStorage, IAuditWriter)
│   │   │   │   ├── Rbac/          (yetki kapsamı + alan maskeleme)
│   │   │   │   ├── Results/       (Result<T>, hata tipleri)
│   │   │   │   └── Paging/
│   │   │   └── Features/                     ◄── DİKEY DİLİMLER
│   │   │       ├── Auth/
│   │   │       │   ├── Login/                (Request, Handler, Validator)
│   │   │       │   └── RefreshToken/
│   │   │       ├── Startups/
│   │   │       │   ├── SearchStartups/
│   │   │       │   ├── GetStartupCard/
│   │   │       │   ├── CreateStartup/
│   │   │       │   ├── UpdateStartup/
│   │   │       │   └── GetStartupTimeline/
│   │   │       ├── Programs/
│   │   │       │   ├── ListPrograms/
│   │   │       │   ├── CreateProgram/
│   │   │       │   └── AddParticipation/
│   │   │       ├── Achievements/
│   │   │       │   ├── ListAchievements/
│   │   │       │   └── AddAchievement/
│   │   │       ├── Documents/
│   │   │       │   ├── UploadDocument/
│   │   │       │   └── DownloadDocument/
│   │   │       ├── Approvals/
│   │   │       │   ├── SubmitChangeRequest/
│   │   │       │   ├── ListPendingRequests/
│   │   │       │   ├── ApproveChangeRequest/
│   │   │       │   └── RejectChangeRequest/
│   │   │       ├── Reporting/
│   │   │       │   ├── EcosystemStats/
│   │   │       │   └── ExportStartups/
│   │   │       └── Ai/
│   │   │           └── AskEcosystem/         (agent döngüsü)
│   │   │
│   │   ├── T3.Infrastructure/
│   │   │   ├── Persistence/       (AppDbContext, configurations, migrations)
│   │   │   ├── Identity/          (JWT üretimi, PasswordHasher, CurrentUser)
│   │   │   ├── Storage/           (LocalDocumentStorage, S3DocumentStorage)
│   │   │   ├── Audit/             (AuditWriter → AuditLog)
│   │   │   └── Ai/
│   │   │       ├── Claude/        (Anthropic API istemcisi)
│   │   │       └── Mcp/           (MCP tool tanımları)
│   │   │
│   │   └── T3.Api/
│   │       ├── Endpoints/         (MapStartupEndpoints, MapApprovalEndpoints…)
│   │       ├── Middleware/        (hata yakalama, request logging)
│   │       ├── Program.cs
│   │       └── appsettings.json
│   │
│   └── tests/
│       └── T3.Application.Tests/  (onay akışı + RBAC kritik testleri)
│
├── frontend/
│   ├── package.json
│   └── src/
│       ├── api/            (üretilen tipler + fetch katmanı)
│       ├── features/       (backend dilimleriyle simetrik)
│       │   ├── auth/  startups/  approvals/  reporting/  ai/
│       ├── components/ui/  (shadcn)
│       ├── lib/            (query client, rbac guard, formatters)
│       └── routes/
│
├── docker-compose.yml
└── .env.example
```

**Dikey dilim anatomisi** — bir özellik = bir klasör, dört dosya:

```
Features/Startups/CreateStartup/
├── CreateStartupRequest.cs     // gelen veri
├── CreateStartupResponse.cs    // dönen veri
├── CreateStartupHandler.cs     // iş mantığı (tek public metot)
└── CreateStartupValidator.cs   // FluentValidation kuralları
```

Endpoint bağlantısı `T3.Api/Endpoints/StartupEndpoints.cs` içinde:

```csharp
group.MapPost("/", async (CreateStartupRequest req,
                          CreateStartupHandler handler,
                          CancellationToken ct)
    => Results.Ok(await handler.Handle(req, ct)))
     .RequireAuthorization(Policies.CanManageStartups);
```

Bu düzenin pratik faydası: 3-4 kişi aynı anda farklı klasörlerde çalışır, merge çatışması çıkmaz.

---

## 3. Veri Modeli

### 3.1. Girişim ve ekip

**Startup** — merkezi girişim kartının gövdesi (MVP #1)

| Alan | Tip | Not |
|---|---|---|
| Id | Guid | |
| Name | string(200) | |
| LegalName | string(300)? | |
| TaxNumber | string(20)? | 🔒 hassas |
| FoundedOn | DateOnly? | |
| Sector | enum | Savunma, Sağlık, Yazılım, Enerji, Tarım, Eğitim, Finans, Diğer |
| TechnologyAreas | string[] | Postgres array kolonu |
| ProductDescription | text? | |
| Website / LogoUrl | string? | |
| ContactEmail / ContactPhone | string? | 🔒 hassas (KVKK) |
| City | string? | |
| Status | enum | Active, Inactive, Graduated, Exited, Acquired |
| CreatedAt / UpdatedAt / IsDeleted | | soft delete |

**TeamMember** — 🔒 tamamı kişisel veri

| Alan | Tip |
|---|---|
| Id, StartupId | Guid |
| FullName | string(200) |
| Title | string(120) |
| Email / Phone / LinkedInUrl | string? 🔒 |
| IsFounder | bool |
| JoinedOn | DateOnly? |

### 3.2. Program geçmişi (MVP #2)

**Program** → `Id, Name, Type (PreIncubation|Incubation|Acceleration|Competition|Event), Coordinatorship, Description`

**ProgramTerm** → `Id, ProgramId, Name ("2025 Bahar"), StartsOn, EndsOn`

**ProgramParticipation** → `Id, StartupId, ProgramTermId, Status (Applied|Accepted|InProgress|Completed|Graduated|Dropped), JoinedOn, LeftOn, Notes`

**Milestone** → `Id, StartupId, Type, Title, Description, OccurredOn, SourceType, SourceId`
Elle girilen gelişim adımları burada. Kronolojik yolculuk **sorgu zamanında** şunların birleşimiyle üretilir:
`ProgramParticipation` ∪ `Achievement` ∪ `Milestone` → `OccurredOn` alanına göre sıralı tek akış.
Böylece zaman çizelgesi hiçbir zaman veriyle çelişmez (türetilmiş, kopyalanmış değil).

### 3.3. Finansal / başarı kayıtları (MVP #4)

Brief "serbest metin değil, alan bazlı" diyor. EF Core **TPH kalıtımı** ile tek tabloda, C# tarafında güçlü tipli:

```
Achievement (abstract)          → Id, StartupId, OccurredOn, IsVerified,
                                  VerifiedByUserId, Currency
├── RevenueRecord               → Period (yıl/çeyrek), Amount
├── ExportRecord                → Period, Amount, TargetCountries[]
├── InvestmentRound             → RoundType (Angel|PreSeed|Seed|SeriesA|SeriesB),
│                                 Amount, Valuation, InvestorNames[]
├── GrantRecord                 → Institution (TÜBİTAK|KOSGEB|TEKNOFEST|AB|Diğer),
│                                 ProgramName, Amount
└── AwardRecord                 → Name, Organization, Rank
```

🔒 Tutar içeren tüm alanlar hassas veri; Karar Verici rolüne **agregat** olarak gösterilir.

### 3.4. Doküman

`Document` → `Id, StartupId, Type (PitchDeck|Financials|Incorporation|Patent|Report|Other), FileName, StoragePath, ContentType, SizeBytes, UploadedByUserId, UploadedAt, ApprovalStatus`

Dosyanın kendisi asla veritabanında tutulmaz; `IDocumentStorage` üzerinden yazılır.

### 3.5. Onay akışı — MVP #3'ün kalbi

Tasarımın en kritik kararı: **Startup kullanıcısı hiçbir tabloya doğrudan yazmaz.** Değişiklik önerisi olarak kaydeder, admin onaylayınca uygulanır.

```
ChangeRequest
├── Id, StartupId
├── SubmittedByUserId, SubmittedAt
├── TargetType     (Startup | TeamMember | Achievement | Document)
├── TargetId       (null → yeni kayıt oluşturma)
├── Operation      (Create | Update | Delete)
├── PayloadJson    (önerilen yeni değerler)
├── BeforeJson     (gönderim anındaki mevcut değerler → diff için)
├── Status         (Pending | Approved | Rejected)
└── ReviewedByUserId, ReviewedAt, ReviewNote
```

**Onay akışı:**

```
Startup kullanıcısı formu doldurur
        │
        ▼
ChangeRequest (Pending)  ── BeforeJson + PayloadJson saklanır
        │
        ▼
Admin / Program Yöneticisi onay kuyruğunda "önce / sonra" diff görür
        │
   ┌────┴────┐
Approve    Reject
   │           │
   ▼           ▼
Payload    ReviewNote
uygulanır  kaydedilir
   │
   ▼
AuditLog yazılır → veri yayına girer
```

Bu tasarımın demo değeri yüksek: "hiçbir girişim verisi onaysız yayına girmiyor" cümlesi ekranda kanıtlanabiliyor.

### 3.6. Kimlik ve denetim

**User** → `Id, Email, PasswordHash, FullName, Role, StartupId?, IsActive, CreatedAt`
`StartupId` yalnızca `StartupUser` rolünde dolu — kullanıcıyı kendi girişimine bağlar.

**UserProgramAssignment** → `UserId, ProgramId`
Program Yöneticisi'nin kapsamını belirler. **Bu tablo olmadan MVP'nin rol ayrımı çalışmaz.**

**AuditLog** → `Id, ActorUserId, ActorRole, Action, EntityType, EntityId, BeforeJson, AfterJson, OccurredAt, IpAddress`

---

## 4. Yetki Matrisi (RBAC) ve KVKK Maskeleme

| Yetenek | Super Admin | Program Yöneticisi | Startup Kullanıcısı | Karar Verici |
|---|---|---|---|---|
| Girişim listesi | tümü | kendi programındakiler | yalnız kendisi | tümü |
| Girişim oluştur / düzenle | ✅ | kendi programındakiler | ⚠️ ChangeRequest ile | ❌ |
| Finansal veri (tam tutar) | ✅ | kendi programındakiler | yalnız kendisi | ❌ agregat |
| Ekip kişisel verisi (e-posta/tel) | ✅ | kendi programı | yalnız kendisi | ❌ maskeli |
| Vergi no | ✅ | ❌ | yalnız kendisi | ❌ |
| Doküman indirme | ✅ | kendi programı | yalnız kendisi | ❌ |
| Onay verme / reddetme | ✅ | kendi programı | ❌ | ❌ |
| Program / dönem yönetimi | ✅ | ❌ | ❌ | ❌ |
| Kullanıcı ve rol yönetimi | ✅ | ❌ | ❌ | ❌ |
| Dashboard / rapor | ✅ | program kapsamı | kendi kartı | ✅ |
| Audit log | ✅ | ❌ | ❌ | ❌ |

### Uygulama noktası

İki mekanizma, ikisi de **tek yerde** tanımlanır ki her endpoint'te tekrar edilmesin:

1. **Kapsam filtresi** — `IStartupScope.Apply(IQueryable<Startup>)` çağrılan kullanıcının rolüne göre sorguyu daraltır. Program Yöneticisi için `UserProgramAssignment` üzerinden join, Startup kullanıcısı için `Id == user.StartupId`.
2. **Alan maskeleme** — `StartupCardMapper` rol parametresi alır; yetkisiz alanı `null` döndürür (boş string değil, ki frontend "yetkiniz yok" ayrımını yapabilsin).

Karar Verici rolü finansalları **yalnızca agregat** görür: "12 girişim toplam 48M₺ yatırım aldı" evet, "X girişimi 4M₺ aldı" hayır.

### Diğer KVKK / güvenlik kalemleri

- Şifreler ASP.NET Core `PasswordHasher<T>` (PBKDF2) ile — düz metin yok.
- JWT kısa ömürlü (15 dk) + refresh token; secret `.env`'den, repoya asla girmez.
- Tüm yazma işlemleri AuditLog'a düşer — kim, ne zaman, neyi, ne yaptı.
- Soft delete (`IsDeleted` + global query filter) — veri kaybı yerine iz.
- Doküman indirme URL'leri kısa ömürlü imzalı; doğrudan dosya yolu ifşa edilmez.
- Rate limiting (ASP.NET Core built-in) login ve AI endpoint'lerinde.
- `docs/` içindeki T3 verileri program dışına çıkmaz (şartname md. 8).

---

## 5. API Yüzeyi

> **Uygulanan yüzey ile fark (Dalga 1–2, 24 Ağustos):** `POST /api/auth/refresh`
> **yazılmadı**. Dalga 2'de jetonun tamamı `HttpOnly` + `SameSite=Strict` çereze
> taşındı ve ömrü bir iş günü kaldı; yenileme jetonu ayrı bir çerez olarak
> eklenmedi çünkü tek jetonun iş günü ömrü aynı sorunu (kullanıcının çalışırken
> atılması) daha az hareketli parçayla çözüyor. Onun yerine `POST
> /api/auth/logout` eklendi: çerezi yalnızca sunucu geçersiz kılabilir.
> Çerezle kimliklenen yazma istekleri `X-CSRF-Token` başlığı istiyor;
> `Authorization: Bearer` yolu (MCP, betikler, Swagger) değişmedi. Bunun karşılığında planda olmayan üç uç eklendi:
> `forgot-password`, `reset-password`, `change-password`. Program tarafında
> plandaki `POST /api/programs` ve `/api/programs/{id}/terms` uçları eklendi,
> yanlarına güncelleme/kapatma ve katılım düzeltme uçları geldi. Güncel liste:
> [README — API yüzeyi](../README.md#api-yüzeyi).

```
POST   /api/auth/login                          → JWT: HttpOnly çerez + gövde
POST   /api/auth/logout                         → oturum ve CSRF çerezlerini siler
POST   /api/auth/forgot-password                → sıfırlama bağlantısı (yanıt adresi doğrulamaz)
POST   /api/auth/reset-password                 → tek kullanımlık jetonla yeni şifre
POST   /api/auth/change-password                → oturum içi, mevcut şifre doğrulamalı
GET    /api/me                                  → kullanıcı + rol + kapsam + mustChangePassword

GET    /api/startups                            ?q&sector&programId&status&page
GET    /api/startups/{id}                       → girişim kartı (rol filtreli)
POST   /api/startups
PUT    /api/startups/{id}
GET    /api/startups/{id}/timeline              → kronolojik yolculuk
GET    /api/startups/{id}/team
GET    /api/startups/{id}/achievements
POST   /api/startups/{id}/achievements
GET    /api/startups/{id}/documents
POST   /api/startups/{id}/documents             (multipart upload)
GET    /api/documents/{id}/download

GET    /api/programs                            → dönemleriyle birlikte (kapsam filtreli)
POST   /api/programs                            (yalnızca SuperAdmin)
PUT    /api/programs/{id}
DELETE /api/programs/{id}                       → zinciri pasife alır, sayıları raporlar
POST   /api/programs/{id}/terms                 (kendi programı)
PUT    /api/programs/{id}/terms/{termId}
DELETE /api/programs/{id}/terms/{termId}        → katılım varsa 409
POST   /api/participations
PUT    /api/participations/{id}                 → durum/tarih/not düzeltmesi
DELETE /api/participations/{id}

POST   /api/change-requests                     → startup kullanıcısı gönderir
GET    /api/change-requests                     ?status=Pending  (onay kuyruğu)
GET    /api/change-requests/{id}                → before/after diff
POST   /api/change-requests/{id}/approve
POST   /api/change-requests/{id}/reject

GET    /api/reports/ecosystem                   → dashboard KPI'ları
GET    /api/reports/export                      → CSV / Excel

GET    /api/audit-logs                          (super admin)

POST   /api/ai/ask                              → agent döngüsü (MCP tool'lu)
       /mcp                                     → MCP endpoint (harici ajanlar)
```

---

## 6. AI Katmanı ve MCP

### Temel kural

MCP tool'ları **REST'in kullandığı aynı Application handler'larını** çağırır. Ayrı bir veri yolu yok — yani AI, kullanıcının göremediği hiçbir veriyi göremez, çünkü aynı kapsam filtresinden ve aynı maskeleme fonksiyonundan geçiyor.

```
                    ┌─── REST endpoints ────► React arayüzü
                    │
Application Handlers┤   (TEK veri yolu)
  + RBAC kapsamı    │
  + KVKK maskeleme  │
                    └─── MCP tools ─────────► Claude / harici ajan
```

### Tool seti

| Tool | Sardığı handler | İşlev |
|---|---|---|
| `search_startups` | `SearchStartupsHandler` | Sektör, program, durum, yatırım filtreli arama |
| `get_startup_card` | `GetStartupCardHandler` | Tek girişimin kartı (maskeli) |
| `get_program_history` | `GetStartupTimelineHandler` | Kronolojik gelişim yolculuğu |
| `list_achievements` | `ListAchievementsHandler` | Başarı/finans kayıtları (tutarlar maskeli) |
| `ecosystem_stats` | `EcosystemStatsHandler` | Agregat göstergeler, gruplama |
| `list_pending_approvals` | `ListChangeRequestsHandler` | Bekleyen onaylar (kuyruk kapsamıyla) |

Uygulanan hâli: `AssistantToolbox.Catalog` (Application katmanı) hem
`POST /api/ai/ask` hem `POST /mcp` tarafından kullanılıyor — tek katalog, tek
veri yolu. MCP ucu da kimlik ister; araçlar isteği yapan jetonun rolüyle
çalışır, ayrıcalıklı bir "ajan kullanıcısı" yok.

### Kullanıcıya görünen AI özelliği

1. **Doğal dil ekosistem sorgusu** — Dashboard'daki sohbet paneli. "Take Off'tan geçip Seed turu almış savunma sanayi girişimleri hangileri?" → backend agent döngüsü tool'ları çağırır, kaynak göstererek yanıtlar.
2. **Otomatik girişim özeti** — Girişim kartında "AI özeti" bloğu: program geçmişi + finansallardan 3-4 cümlelik yönetici özeti.

**Sınır:** AI karar verici değil, karar *destek* katmanıdır. Yanıt her zaman hangi kayıtlara dayandığını gösterir; yeterli veri yoksa uydurmaz, "veri yok" der. (Kitapçığın diğer problemlerinde de tekrarlanan ilke.)

---

## 7. Frontend Sayfa Planı

| Sayfa | Rol | İçerik |
|---|---|---|
| Login | herkes | e-posta + şifre |
| Dashboard | rol duyarlı | KPI kartları, filtreler, grafikler, AI sohbet paneli |
| Girişim listesi | Admin / PM / KV | tablo, arama, çoklu filtre, dışa aktar |
| **Girişim kartı** | rol duyarlı | sekmeler: Genel · Ekip · Ürün · Program Geçmişi (timeline) · Finansallar · Dokümanlar |
| Startup portalı | Startup | kendi kartını düzenle → "değişiklik gönder" |
| Onay kuyruğu | Admin / PM | bekleyen istekler, **önce/sonra diff**, onayla/reddet |
| Program yönetimi | Admin | program + dönem CRUD, katılım atama |
| Kullanıcı yönetimi | Admin | kullanıcı, rol, program ataması |
| Audit log | Admin | filtrelenebilir denetim izi |

Frontend'de `features/` klasörleri backend dilimleriyle **simetrik** duracak — aynı isim, aynı sınır.

---

## 8. Faz Planı

Bugün **20 Ağustos**. İlk teslim **26 Ağustos 10.00** (iş modeli canvası + prototip videosu + sunum) → çalışan prototip 25 Ağustos akşamına kadar hazır olmalı. Creathon **5-6 Eylül**.

| Faz | Gün | Çıktı | MVP maddesi |
|---|---|---|---|
| **0 — İskelet** ✅ | 20 Ağu | Solution + 4 proje, Docker Compose Postgres, EF Core ilk migration, health check endpoint, Vite+React iskeleti, Swagger açık | — |
| **1 — Kimlik & RBAC** ✅ | 20 Ağu | login + JWT, `/api/me`, kapsam filtresi, rol politikaları, frontend login + route guard. *Kullanıcı yönetimi CRUD'u Faz 3'e ertelendi — girişim kartı ona ihtiyaç duymuyor.* | roller |
| **2 — Girişim kartı** ✅ | 20 Ağu | Startup + TeamMember CRUD, girişim kartı ekranı, arama/filtre/sıralama/sayfalama, program geçmişi timeline, demo verisi, RBAC + maskeleme birim testleri | **#1, #2** |
| **3 — Onay akışı** ✅ | 23 Ağu | ChangeRequest, startup portalı, onay kuyruğu + diff görünümü, AuditLog | **#3** |
| **4 — Finansal & doküman** ✅ | 24 Ağu | Achievement TPH (5 tip), doküman yükleme/indirme, maskeleme kuralları | **#4** |
| **5 — Dashboard & AI** ✅ | 24 Ağu | EcosystemStats + ekosistem panosu, el yazımı SVG grafikler, CSV dışa aktarma (maskeli hücreler dâhil), JSON-RPC MCP sunucusu, AI karar destek paneli ve yönetici özeti (model yoksa yerel planlayıcı), 32 girişimlik gerçekçi tohum verisi | karar destek |
| **Denetim Dalga 0** ✅ | 24 Ağu | Ürün denetiminin videodan önce kapanması gereken bulguları: girişim/ekip/katılım yazma yolları arayüze, hız sınırı bölümlemesi, sekme başlığı/dil, 404 ekranı, mobil taşma, yazma yolunda maskeleme | **#1–#4 bütünlüğü** |
| **Denetim Dalga 1** ✅ | 24 Ağu | Program ve dönem yönetimi, şifre kurtarma/değiştirme, oturum ömrü ve ağ hatası ayrımı, giriş olaylarının denetim izi, KVKK metinleri, Karar Verici'nin onay ekranı | MVP + KVKK |
| **Denetim Dalga 2** ✅ | 24 Ağu | Canlıya çıkış altyapısı: derlenmiş arayüzü API sunuyor (tek origin) + `Dockerfile`/`docker-compose.prod.yml`, jeton `HttpOnly` çerezde + CSRF çift-gönderimi + CSP ve güvenlik başlıkları, `X-Forwarded-*` yalnızca güvenilen vekilden, kalan orta maddeler (URL'de filtre durumu, aksan katlaması, kirli form uyarısı, kod bölme, erişilebilirlik) | dağıtılabilirlik |
| **Teslim** | 26 Ağu | Canvas + prototip videosu + sunum | — |
| **Creathon** | 5-6 Eyl | Cilalama, AI genişletme, testler, Demo Day sunumu | — |

### Faz 0'da hemen yapılacaklar (bu oturum)

1. `backend/` altında solution + 4 proje + proje referansları
2. `docker-compose.yml` (Postgres 16 + pgAdmin opsiyonel)
3. `T3.Domain` temel tipleri (`Entity`, `IAuditable`, `ISoftDelete`) ve ilk entity'ler
4. `AppDbContext` + ilk migration
5. `T3.Api/Program.cs`: DI, Swagger, CORS, health check
6. `frontend/` Vite + React + TS + Tailwind + shadcn kurulumu
7. `.env.example`, `.gitignore`, README

### Demo için kritik: seed veri

Boş bir sistem demo edilemez. Faz 5'ten önce **30-40 gerçekçi girişim** üretilecek: farklı sektörler, 4-5 T3 programına yayılmış katılımlar, yatırım turları, hibeler, ödüller ve bekleyen 5-6 onay isteği. Bu veri `T3.Infrastructure/Persistence/Seed/` altında kod olarak duracak, elle girilmeyecek.

---

## 9. Riskler ve Önlemler

| Risk | Önlem |
|---|---|
| Kapsam şişmesi (6 MVP maddesi yetişmez) | Faz sırası MVP maddelerine kilitli; AI en sona bırakıldı çünkü zorunlu değil |
| RBAC her endpoint'e kopyalanır, tutarsızlaşır | Kapsam filtresi + maskeleme tek sınıfta; endpoint'ler yalnızca policy adı verir |
| Onay akışı sonradan eklenmeye çalışılır | Faz 3'te, finansal modülden **önce** yapılıyor — böylece finansal veri baştan akışa uyar |
| Demo günü boş ekran | Seed veri kod olarak, her `dotnet run`'da hazır |
| AI tool'ları veri sızdırır | Tool'lar aynı handler + aynı maskelemeyi kullanıyor; ayrı yol yok |
| İki servis (CORS, iki deploy) | `docker compose up` tek komutla ikisini de kaldırır |
| MCP C# SDK'sı beklenmedik davranır | Faz 5'te; sorun çıkarsa doğrudan Claude tool-calling'e düşülür (aynı handler'lar) |

---

## 10. Sonraki Adım

Faz 0'a başlıyoruz: solution iskeleti, Docker Compose, ilk migration ve frontend kurulumu.
