# Mevcut Sistem Raporu — T3 Girişim Ekosistemi Yönetim Sistemi

*Sıfırdan yeniden yazım öncesi tam envanter. 9 Eylül 2026 itibarıyla `main`
dalındaki koda (`a4b2d2d`) dayanır.*

Bu belge üç soruyu cevaplar:

1. **Ne var?** Çalışan her uç, her handler, her ekran, her tablo — kanıtıyla.
2. **Neden böyle?** Hangi kararın hangi gereksinimden ya da hangi hatadan
   doğduğu.
3. **Yeniden yazarken ne değişmeli?** Aceleyle girmiş borç, kapanmamış boşluk
   ve mimari olarak yanlış yere oturmuş parçalar.

Rapor tarama üzerine değil **doğrulama** üzerine kurulu. Bu oturumda koşturulan
kanıtlar:

| Kontrol | Sonuç |
|---|---|
| `dotnet build` (4 proje + test projesi) | **Başarılı**, 0 hata, 2 uyarı (MailKit 4.8.0 bilinen zafiyet, NU1902) |
| `dotnet test` | **266/266 geçti**, 0 başarısız, 4 s |
| `npm run build` (`tsc -b && vite build`) | **Başarılı**, 221 ms, 355 kB ana paket + 27 parça |
| `npx oxlint` | Bulgu yok |

> README'de "191 birim testi" yazıyor — güncel sayı 266. Bu, raporun genelinde
> tekrar eden bir örüntünün ilk işareti: **kod belgeden ilerde.** Ayrıntı
> [§10.5](#105-belge-kod-ayrışması).

---

## 1. Büyüklük ve şekil

| Alan | Dosya | Satır |
|---|---|---|
| `T3.Domain` | 33 | 889 |
| `T3.Application` | 165 | 11.279 |
| `T3.Infrastructure` | 40 | 3.987 |
| `T3.Api` | 30 | 3.002 |
| EF Core migration'ları (9 göç + snapshot) | 19 | 11.851 |
| Birim testleri | 31 | 3.075 |
| `frontend/src` | 77 | 11.480 |
| Python doğrulama betikleri | 11 | 4.145 |
| Belgeler (`docs/` + README + CLAUDE.md) | 13 | 5.033 |

**Toplam elle yazılmış kod: ~34.700 satır** (migration'lar üretilmiş,
belgeler ve betikler hariç).

Dikkat çeken oran: `T3.Application` tek başına backend kodunun **%62'si**.
Sebep mimari tercih — dikey dilimler, elle yazılmış eşleme, handler başına
kendi DTO'su. Yeniden yazımda bu oranın korunup korunmayacağı ilk kararlardan
biri (bkz. [§11.2](#112-korunması-gereken-kararlar)).

---

## 2. Zorunlu kapsam: brief ne istiyor, kod ne veriyor

Brief'in ([docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md](Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md))
"MVP Kapsamı — Zorunlu Altı Gereksinim" başlığı altında **numaralı dört blok**
var; kalan iki zorunluluk aynı belgenin başka bölümlerinde duruyor (§2 rol
matrisi ve §5.3 KVKK). Yani "altı madde" = 4 işlevsel blok + 4 rol + KVKK
maskelemesi. Değerlendirme aşamasına geçmek bunların **hepsine** bağlı.

| # | Zorunluluk | Durum | Kod karşılığı | Boşluk |
|---|---|---|---|---|
| 1 | Merkezi girişim profili / girişim kartı | ✅ Tam | `GetStartupCardHandler`, `/girisimler/:id` (6 sekme) | — |
| 2 | Program geçmişi + gelişim yolculuğu (kronolojik) | ⚠️ **Kısmi** | `GetStartupTimelineHandler` üç kaynağı birleştiriyor | **Kilometre taşı (`Milestone`) yazma yolu hiç yok** — bkz. [§10.1](#101-mvp-riski-taşıyan-boşluklar) |
| 3 | Startup portalı + admin onayı ("doğrudan yayın yok") | ⚠️ **Sapma** | `ChangeRequest` akışı + `/portal` | **Girişim profili alanları onaysız yayına giriyor** — bkz. [§10.1](#101-mvp-riski-taşıyan-boşluklar) |
| 4 | Satış/yatırım/başarı/doküman — alan bazlı model | ✅ Tam | 5 tipli TPH hiyerarşisi + `Document` | — |
| 5 | Dört rol ve yetkileri | ✅ Tam | `UserRole`, 10 politika, `IStartupScope` | — |
| 6 | KVKK: hassas veri maskeleme, rol bazlı erişim | ✅ Tam | `StartupVisibility`, denetim izi, saklama süreleri | — |

Brief'in "önerilen veri modeli" listesindeki beş varlığın hepsi karşılanmış;
`ApprovalLog` ayrı tablo değil, `ChangeRequest` + `AuditLog` ikilisi olarak
uygulanmış (onay bir satır değil iki satır yazıyor — akış kaydı ve denetim izi
farklı sorulara cevap veriyor).

**İki maddedeki sapma jüri sorusu üretmeye açık.** İkisi de bilinçli karar
olarak belgelenmiş ama ikisi de brief'in sözünden ayrılıyor; yeniden yazımda
ya kapatılmalı ya da ekranda gerekçesiyle savunulabilir hâle getirilmeli.

---

## 3. Mimari haritası

### 3.1. Bağımlılık yönü

```
T3.Api ──────► T3.Application ──────► T3.Domain
                     ▲
                     │
            T3.Infrastructure
```

`T3.Domain` hiçbir şeye bağımlı değil (EF attribute'ü bile yok — eşleme
`Infrastructure/Persistence/Configurations/` altında dokuz `IEntityTypeConfiguration`
dosyasında). `T3.Application` yalnızca `IAppDbContext` arayüzünü görüyor,
`AppDbContext`'i görmüyor.

### 3.2. Dikey dilim düzeni

Klasörleme teknik değil işlevsel: `Features/Startups/CreateStartup/` içinde o
use-case'in handler'ı, isteği ve doğrulayıcısı birlikte durur. `Services/`,
`Repositories/`, `Controllers/` klasörü yok.

**Kasıtlı olarak kullanılmayanlar** — yeniden yazımda tekrar karara bağlanacak
kalemler:

| Kullanılmayan | Yerine | Gerekçe (koddaki) |
|---|---|---|
| MediatR | Düz `*Handler` sınıfları, DI'dan çözülür | Ardışık düzen soyutlamasının bedeli, faydasından fazla; handler'lar MCP'den doğrudan çağrılıyor |
| AutoMapper / Mapster | Elle yazılmış `ToResponse()` | Maskeleme kararının **okunabilir** kalması gerekiyor (jüriye gösterilecek) |
| Repository deseni | `IAppDbContext` + `IQueryable` | Kapsam filtresi `IQueryable` üzerinde besteleniyor |
| ASP.NET Identity | Kendi `User` + Pbkdf2 + JWT | Rol–kapsam bağı (girişim/program) Identity'nin modeline oturmuyor |
| Controller | Minimal API + `MapGroup` | 67 uç için sınıf başına dosya gereksiz |

`*Handler` adıyla biten her sınıf DI'a **otomatik** kaydedilir
(`DependencyInjection.cs`, refleksiyonla). Use-case olmayan ortak bileşenler
elle kaydedilir: `StartupEditGuard`, `ChangeRequestApplier`, `UserAdminGuard`,
`ProgramAccessGuard`, `AssistantToolbox`, `OfflineAssistant`,
`AssistantConversationRunner`, `IStartupScope`, `IChangeRequestScope`.

### 3.3. Sonuç tipi ve hata şekli

Handler'lar HTTP bilmiyor; `Result<T>` dönüyor. Eşleme tek yerde
(`ApiResults.ToHttp`):

| `ErrorKind` | HTTP |
|---|---|
| `NotFound` | 404 |
| `Validation` | 400 |
| `Forbidden` | 403 |
| `Conflict` | 409 |
| (diğer) | 500 |

Hata gövdesi üç kaynakta birebir aynı şekilde
(`ApiErrorBody(Status, Title, Errors?, Referans?)`): `ExceptionHandlingMiddleware`,
`ValidationFilter` ve `UseStatusCodePages`. `Referans` yalnızca 500'lerde dolar
— doğrulama hatası kullanıcının düzeltebileceği bir şey, referans numarası
orada gürültü.

---

## 4. Veri modeli — 17 tablo

Postgres 16. `DateOnly` iş takvimi tarihleri için, `DateTimeOffset` olay
zamanları için; JSON gövdeler `jsonb`, çoklu değerler Postgres `text[]`.

### 4.1. Kimlik ve yetki kapsamı

| Tablo | Alanlar | Kısıt / indeks |
|---|---|---|
| `Users` | `Email`, `PasswordHash`, `FullName`, `Role`, `StartupId?`, `IsActive`, `LastLoginAt?`, `SecurityStamp`, `FailedLoginCount`, `LockedUntil?`, `MustChangePassword` + zaman/soft-delete | `Email` **unique** (soft-delete'ten bağımsız) |
| `UserProgramAssignments` | `UserId`, `ProgramId`, `AssignedAt` + soft-delete | `(UserId, ProgramId)` **unique** — kaldırılan atama silinmez, işaretlenir; yeniden eklenirken satır **diriltilir** |
| `PasswordResetTokens` | `UserId`, `TokenHash` (SHA-256, ham jeton **saklanmaz**), `ExpiresAt`, `UsedAt?`, `RequestedFromIp?` | `TokenHash` unique |

`SecurityStamp` jetona gömülür ve her istekte canlı değerle karşılaştırılır
(`UserStateMiddleware`). Pasife alma, rol/program değişikliği, şifre
değişikliği ve **çıkış** onu yeniler → o kullanıcının tüm jetonları (tüm
cihazlar) bir sonraki istekte 401 alır. Ayrı oturum/jeton tablosu yok; bu
bilinçli kısayol.

### 4.2. Girişim çekirdeği

| Tablo | Alanlar |
|---|---|
| `Startups` | `Name`, `LegalName?`, **`TaxNumber?`**, `FoundedOn?`, `Sector`, `TechnologyAreas` (`text[]`), `ProductDescription?`, `Website?`, `LogoUrl?`, `City?`, **`ContactEmail?`**, **`ContactPhone?`**, `Status` |
| `TeamMembers` | `StartupId`, `FullName`, `Title?`, **`Email?`**, **`Phone?`**, `LinkedInUrl?`, `IsFounder`, `JoinedOn?` |

**Kalın alanlar KVKK'da hassas** sayılıyor ve role göre maskeleniyor
([§7](#7-rbac-ve-kvkk-maskelemesi)). İndeksler: `Name`, `Sector`, `Status`.

### 4.3. Program zinciri (MVP #2)

```
EcosystemProgram ──1:N──► ProgramTerm ──1:N──► ProgramParticipation ◄──N:1── Startup
```

| Tablo | Alanlar | Kısıt |
|---|---|---|
| `Programs` | `Name`, `Type`, `Coordinatorship?`, `Description?` | `Name` unique |
| `ProgramTerms` | `ProgramId`, `Name`, `StartsOn`, `EndsOn?` | `(ProgramId, Name)` unique |
| `ProgramParticipations` | `StartupId`, `ProgramTermId`, `Status`, `JoinedOn`, `LeftOn?`, `Notes?` | `(StartupId, ProgramTermId)` unique |

Sınıf adı `Program` değil `EcosystemProgram`: ASP.NET Core'un giriş noktası
olan `Program` sınıfıyla çakışmayı önlüyor.

### 4.4. Başarı / finans (MVP #4) — TPH hiyerarşisi

Tek tablo (`Achievements`), `Kind` string ayrıştırıcı kolonu, beş güçlü tipli
sınıf:

```
Achievement (soyut)            StartupId, OccurredOn, Note?, IsVerified,
│                              VerifiedByUserId?, VerifiedAt?
├── AwardRecord                Name, Organization?, Rank?
└── MoneyAchievement (soyut)   Amount (18,2), Currency
    ├── GrantRecord            Institution (7 değerli enum), ProgramName?
    ├── InvestmentRound        RoundType (8 değer), Valuation? (18,2),
    │                          InvestorNames (text[])
    └── PeriodicMoneyAchievement (soyut)   FiscalYear, Quarter?
        ├── RevenueRecord      (ek alan yok)
        └── ExportRecord       TargetCountries (text[])
```

`AchievementKind` (yazma modelinde: `Revenue`, `Export`, `Investment`,
`Grant`, `Award`) tek gövdeden hangi tipin yazılacağını seçer. Doğrulama türe
bağlı: mali yılı olmayan ciro, tutarı olmayan yatırım turu kaydedilemez.
**Tür sonradan değiştirilemez** (409) — TPH ayrıştırıcısı yerinde
güncellenemediği için değil sadece; tür satırın kimliğinin parçası.

İndeks: `(StartupId, OccurredOn)`.

### 4.5. Doküman ve kilometre taşı

| Tablo | Alanlar | Not |
|---|---|---|
| `Documents` | `StartupId`, `Type`, `FileName`, `StoragePath`, `ContentType`, `SizeBytes`, `UploadedByUserId`, `UploadedAt` | Dosya veritabanında değil; `IDocumentStorage` arkasında yerel disk. `ContentType` **istemciden alınmaz**, uzantıdan türetilir |
| `Milestones` | `StartupId`, `Type` (7 değer), `Title`, `Description?`, `OccurredOn` | **Yazma yolu yok** — yalnızca tohumlayıcı yazıyor (bkz. [§10.1](#101-mvp-riski-taşıyan-boşluklar)) |

### 4.6. Onay akışı (MVP #3'ün kalbi)

`ChangeRequests`: `StartupId`, `SubmittedByUserId`, `SubmittedAt`,
`TargetType`, `TargetId?`, `Operation`, `PayloadJson` (`jsonb`),
`BeforeJson?` (`jsonb`), `Status`, `ReviewedByUserId?`, `ReviewedAt?`,
`ReviewNote?`. İndeks: `(Status, SubmittedAt)`, `StartupId`.

- `ChangeTargetType`: `Startup`, `TeamMember`, `Achievement`, `Document`
  → **`Milestone` ve `ProgramParticipation` bu listede yok**
- `ChangeOperation`: `Create`, `Update`, `Delete`
- `ChangeRequestStatus`: `Pending`, `Approved`, `Rejected`

`BeforeJson` gönderim anındaki değerleri taşır; onay ekranındaki "önce/sonra"
karşılaştırması buradan üretilir. Gönderimle karar arasında günler geçebildiği
için **onay anında gövde yeniden doğrulanır** ve ad tekilliği yeniden
kontrol edilir.

### 4.7. Denetim, kayıt başvurusu, bildirim, sohbet

| Tablo | Alanlar | Not |
|---|---|---|
| `AuditLogs` | `ActorUserId?`, `ActorRole?`, `Action`, `EntityType`, `EntityId?`, `BeforeJson?`, `AfterJson?`, `IpAddress?`, `UserAgent?`, `OccurredAt` | Yalnızca eklenir. `ActorUserId` başarısız girişte `null`. İndeks: `OccurredAt`, `(EntityType, EntityId)`, `Action` |
| `StartupRegistrationRequests` | `Email`, `PasswordHash`, `FullName`, `StartupName`, `Sector`, `City?`, `ContactPhone?`, `Status`, `ReviewedAt?`, `ReviewedByUserId?`, `RejectionReason?`, `CreatedStartupId?`, `CreatedUserId?` | Başvuru sahibinin **kendi seçtiği** şifrenin özeti onay anında hesaba taşınır — admin şifreyi hiç bilmez |
| `Notifications` | `StartupId`, `RecipientUserId`, `SentByUserId`, `SentByRole`, `Message`, `SentAt`, `ReadAt?`, `EmailSent`, `RecipientDeletedAt?` | `SentByRole` gönderim anındaki rolü dondurur. `RecipientDeletedAt` yalnızca alıcının kutusundan gizler, SuperAdmin gözetiminde görünmeye devam eder |
| `AiConversations` | `OwnerUserId`, `Title`, `LastMessageAt` | **Soft delete YOK** — serbest metin kişisel veri taşıyabilir, saklama süresi dolunca gerçekten silinir |
| `AiConversationMessages` | `ConversationId`, `Role`, `Text`, `ToolNamesJson?`, `StartupIdsJson?`, `Mode?`, `ModelName?`, `ExportDownloadToken?`, `ExportFileName?` | Araç çağrısı/sonuç blokları saklanmaz; hangi araç çalıştığı ve hangi girişimlere dokunulduğu özet olarak durur |

### 4.8. Soft delete

`ISoftDelete` uygulayan her varlığa **global sorgu filtresi** ekleniyor
(`AppDbContext.ApplySoftDeleteFilters`, refleksiyonla; TPH'de yalnızca kök
tipe). Filtre yalnızca **okumayı** daraltır — silme zinciri elle yürütülür:
`DELETE /api/startups/{id}` ekip, katılım, başarı, doküman, kilometre taşı ve
onay isteklerini tek tek işaretler ve **kaç kaydın etkilendiğini raporlar**.

`CreatedAt`/`UpdatedAt` ve `DeletedAt` merkezi olarak
`SaveChangesAsync` içinde damgalanır (`StampTimestamps`). Açıkça verilmiş
`CreatedAt` korunur — tohum verisi gerçek geçmiş tarih taşıyabilsin diye.

### 4.9. Göç geçmişi

Dokuz migration, tümü Postgres'e karşı:

1. `InitialCreate` (20.08)
2. `Faz7DalgaBir_ParolaKurtarma_DenetimGirisleri` (24.08)
3. `G01_KullaniciGuvenlikDamgasi` (04.09)
4. `G05_KabaKuvvetKorumasi` (04.09)
5. `GirisimKendiKendineKayit` (05.09)
6. `SohbetGecmisi` (05.09)
7. `Bildirimler` (05.09)
8. `BildirimSilme` (06.09)
9. `AsistanExcelDisaAktarma` (06.09)

Son beşi tek haftada — modelin son dalgada hâlâ hareket ettiğinin işareti.

---

## 5. API yüzeyi — 67 uç

`FallbackPolicy` = `RequireAuthenticatedUser()`. Yani **yetkilendirme
varsayılan kapalı**: üst veri taşımayan her uç kimlik ister, herkese açık
uçlar `.AllowAnonymous()` demek zorunda.

Sütunlar: **Politika** = endpoint üzerindeki kaba kapı, **Satır kapsamı** =
handler içindeki ikinci kontrol (derinlemesine savunma; MCP araçları politika
hattını atladığı için zorunlu).

### 5.1. Sağlık (2)

| Uç | Politika | Not |
|---|---|---|
| `GET /health` | anonim | statü + servis adı + zaman |
| `GET /health/db` | anonim | `CanConnect` + **bekleyen göç sayısı** (adları bilinçli olarak dönmüyor — anonim uçta keşif değeri var) |

### 5.2. Kimlik (7)

| Uç | Politika | Hız sınırı | Handler / not |
|---|---|---|---|
| `POST /api/auth/login` | anonim | `auth` | `LoginHandler` → jeton `HttpOnly` çerezde **ve** gövdede |
| `POST /api/auth/logout` | anonim | — | Çerezleri siler **ve** `SecurityStamp`'i yeniler (tüm cihazlar düşer) |
| `POST /api/auth/register` | anonim | `auth` | `RegisterStartupHandler` → yalnızca `StartupRegistrationRequest` üretir |
| `POST /api/auth/forgot-password` | anonim | `auth` | Yanıt adresin kayıtlı olup olmadığını **söylemez**; jeton yanıtta hiç dönmez |
| `POST /api/auth/reset-password` | anonim | `auth` | Tek kullanımlık, 2 saat geçerli |
| `POST /api/auth/change-password` | kimlik | `auth` | Mevcut şifre yeniden doğrulanır; başarıda çerez tazelenir |
| `GET /api/me` | kimlik | — | `GetSessionHandler`; ayrıca kaybolmuş CSRF çerezini sessizce onarır |

### 5.3. Girişim (11)

| Uç | Politika | Satır kapsamı |
|---|---|---|
| `GET /api/startups` | kimlik | `IStartupScope.Apply` — `q`, `sector`, `status`, `programId`, `city`, `foundedYear`, `sort`, `page`, `pageSize` |
| `GET /api/startups/{id}` | kimlik | Kapsam + `StartupVisibility` alan maskelemesi |
| `GET /api/startups/{id}/timeline` | kimlik | Kapsam + tutar maskelemesi |
| `GET /api/startups/{id}/ai-report` | `reports:ai-generate` | **Hız sınırı koddan çıkarılmış** (bkz. [§10.2](#102-acele-borcu-koda-yazılı-geçici-çözümler)) |
| `POST /api/startups/{id}/notifications` | `notifications:send` | Uygulama içi + e-posta |
| `POST /api/startups` | `startups:manage` | `IStartupScope.CanCreateStartups` |
| `PUT /api/startups/{id}` | `startups:manage` | `StartupWriteModel.ApplyTo(startup, visibility)` — **göremediği alanı korur** |
| `DELETE /api/startups/{id}` | `startups:manage` | Handler içinde SuperAdmin'e daraltılır; zinciri pasife alır |
| `POST /api/startups/{id}/team` | `startups:manage` | `StartupEditGuard` |
| `PUT /api/startups/{id}/team/{memberId}` | `startups:manage` | `StartupEditGuard` |
| `DELETE /api/startups/{id}/team/{memberId}` | `startups:manage` | `StartupEditGuard` |

### 5.4. Başarı / finans (4)

| Uç | Politika |
|---|---|
| `GET /api/startups/{id}/achievements?kind=` | kimlik — tutarlar role göre maskeli, yanıt `amountMasked` taşır |
| `POST /api/startups/{id}/achievements` | `startups:manage` |
| `PUT /api/startups/{id}/achievements/{achievementId}` | `startups:manage` — tür değişimi 409 |
| `DELETE /api/startups/{id}/achievements/{achievementId}` | `startups:manage` |

### 5.5. Doküman (4)

| Uç | Politika | Not |
|---|---|---|
| `GET /api/startups/{id}/documents` | kimlik | Karar Verici listeyi göremez |
| `POST /api/startups/{id}/documents` | **yalnızca kimlik** | Rol değil satır kararı: yetkili doğrudan kaydeder, girişim kullanıcısı `ChangeRequest` üretir. 21 MB sunucu sınırı (uygulama sınırı 20 MB'tan **önce** devreye girsin diye), `DisableAntiforgery()` (yerleşik form jetonu; CSRF middleware'i ayrı ve devrede) |
| `DELETE /api/startups/{id}/documents/{documentId}` | `startups:manage` | Dosya depoda kalır |
| `GET /api/documents/{documentId}/download` | kimlik + `mass-export` kovası | `nosniff`; **her indirme denetim izine yazılır** |

### 5.6. Program (10)

| Uç | Politika |
|---|---|
| `GET /api/programs` | kimlik — kapsama göre daraltılır |
| `POST /api/programs` · `PUT /api/programs/{id}` · `DELETE /api/programs/{id}` | `programs:manage` = **yalnızca SuperAdmin** |
| `POST /api/programs/{programId}/terms` · `PUT .../{termId}` · `DELETE .../{termId}` | `program-terms:manage` + `ProgramAccessGuard.EnsureOwnsProgramAsync` |
| `POST /api/participations` · `PUT /api/participations/{id}` · `DELETE /api/participations/{id}` | `program-terms:manage` + kendi programı |

Yetki ikiye ayrılmış çünkü **program listesi aynı zamanda Program
Yöneticisi'nin yetki kapsamının tanımı** — kendi kapsamını büyütebilen bir rol
RBAC'ı anlamsız kılar. Katılımı olan dönem kapatılamaz (409).

### 5.7. Onay akışı (5)

| Uç | Politika | Not |
|---|---|---|
| `POST /api/change-requests` | **politika yok** | Yetki role değil girişim bağına dayanıyor: `IChangeRequestScope.CanSubmit` |
| `GET /api/change-requests` | kimlik | Aynı uç iki iş görüyor: yetkiliye kuyruk, girişime "önerilerim". Durum sayıları filtreden bağımsız hesaplanır |
| `GET /api/change-requests/{id}` | kimlik | Alan alan before/after; maskeli alan satırdan silinmez, "değişiyor ama göremezsiniz" olarak durur |
| `POST /api/change-requests/{id}/approve` | `approvals:review` + kapsam | `ChangeRequestApplier` (325 satır) hedef varlığa uygular |
| `POST /api/change-requests/{id}/reject` | `approvals:review` + kapsam | Gerekçe zorunlu; hiçbir veri değişmez, bekleyen doküman dosyası depodan silinir |

### 5.8. Kullanıcı, kayıt başvurusu, denetim (9)

| Uç | Politika |
|---|---|
| `GET /api/users` (`q`, `role`, `isActive`, `programId`, `startupId`, sayfalama) | `users:manage` |
| `POST /api/users` · `PUT /api/users/{id}` · `DELETE /api/users/{id}` | `users:manage` + `UserAdminGuard` |
| `PUT /api/users/{id}/password` | `users:manage` — ayrı uç: izde ayrı eylem olarak görünsün |
| `GET /api/registration-requests` · `POST .../{id}/approve` · `POST .../{id}/reject` | `users:manage` |
| `GET /api/audit-logs` (`entityType`, `entityId`, `actorUserId`, `action`, `from`, `to`) | `audit:view` = SuperAdmin, handler'da **bağımsız olarak tekrar** kontrol |

`UserAdminGuard` beş ayrı kuralı tek noktada tutuyor: rol–kapsam bağı
tutarlılığı, e-posta tekilliği (silinmişler dâhil), kendi hesabını kilitleme
koruması, girişim varlığı, program ataması eşitleme.

### 5.9. Rapor (2)

| Uç | Politika | Not |
|---|---|---|
| `GET /api/reports/ecosystem` | kimlik | Sayılar kapsamla daralır, tutarlar `StartupVisibility.Aggregate` ile maskelenir. **Ayrı bir "rapor yetkisi" bilinçli olarak yok** — dördüncü bir kural noktası yaratmamak için |
| `GET /api/reports/export` | kimlik + `mass-export` | CSV: `;` ayraç, UTF-8+BOM, `tr-TR`; maskeli hücre boş değil "yetkiniz yok" yazar; formül enjeksiyonuna karşı önekleme; **2000 satır sınırı**; her aktarma denetim izine |

### 5.10. AI asistanı (6) ve MCP (1)

Grup düzeyinde `RequireRateLimiting("ai")`.

| Uç | Not |
|---|---|
| `POST /api/ai/ask` | Tek soruluk uç. **Arayüzden hiç çağrılmıyor** — bkz. [§10.3](#103-ölü-ve-yarı-bağlı-kod) |
| `POST /api/ai/chat` | Çok turlu; bağlam **sunucuda**, istemci geçmiş göndermiyor (hiç sorulmamış turu sorulmuş gibi sunma yolu kapalı) |
| `GET /api/ai/chat/conversations` | Yalnızca kendi sohbetleri, son konuşulan önce |
| `GET /api/ai/chat/conversations/{id}` | Başkasının sohbeti **404** (403 değil — varlığı da sızdırmıyor) |
| `GET /api/ai/exports/{token}` | Tek kullanımlık jeton, 15 dk, bellek içi depo |
| `GET /api/ai/startups/{id}/summary` | Kart için yönetici özeti; tutarı göremeyen role tutarsız özet üretir ve **bunu söyler** |
| `POST /mcp` | JSON-RPC 2.0: `initialize`, `ping`, `tools/list`, `tools/call`. `mcp` kovası |

**Araç kataloğu (8 araç, MCP'de 7):**

| Araç | MCP'de | Sardığı handler |
|---|---|---|
| `search_startups` | ✅ | `SearchStartupsHandler` |
| `get_startup_card` | ✅ | `GetStartupCardHandler` |
| `get_program_history` | ✅ | `GetStartupTimelineHandler` |
| `list_achievements` | ✅ | `ListAchievementsHandler` |
| `ecosystem_stats` | ✅ | `EcosystemStatsHandler` |
| `list_programs` | ✅ | `ListProgramsHandler` |
| `list_pending_approvals` | ✅ | `ListChangeRequestsHandler` |
| `export_startups_excel` | ❌ | `ExportStartupsHandler` + `IExcelFileBuilder` |

Excel aracı MCP'den **dışlanmış**: indirme jetonu MCP'nin metin kanalına
gömülmemeli. Araç MCP'de hiç görünmez, yarım çalışmaz.

### 5.11. Bildirim (6)

| Uç | Politika |
|---|---|
| `GET /api/notifications` | kimlik — sahiplik handler'da `RecipientUserId` ile |
| `POST /api/notifications/read-all` · `POST /api/notifications/{id}/read` | kimlik |
| `DELETE /api/notifications/{id}` · `POST /api/notifications/{id}/restore` | kimlik — alıcı için yumuşak gizleme/geri alma |
| `GET /api/notifications/sent` | `notifications:view-all` = SuperAdmin |

---

## 6. Kesişen katmanlar

### 6.1. Ardışık düzen sırası (`Program.cs`)

Sıra tesadüfi değil; her adım kendinden öncekinin ürettiği bilgiye dayanıyor:

```
 1. UseForwardedHeaders          → gerçek istemci IP'si ve şeması (X-Forwarded-*)
 2. RequestIdMiddleware          → izlenebilirlik kimliği
 3. UseSerilogRequestLogging     → tek özet satır (gövde ASLA loglanmaz)
 4. ExceptionHandlingMiddleware  → beklenmeyen hata → referanslı JSON
 5. SecurityHeaders              → CSP/nosniff/Referrer/Frame/Permissions
 6. UseHsts + UseHttpsRedirection (yalnızca Hosting:RequireHttps ise)
 7. UseStatusCodePages           → gövdesiz 401/403/429'u aynı JSON şekline sokar
 8. Swagger                      (yalnızca Development)
 9. UseCompiledFrontend          → derlenmiş SPA, tek origin
10. UseCors                      → yalnızca ayrı origin kurulumunda anlamlı
11. CaptureLoginEmail            → hız sınırı anahtarı için gövdeyi tamponla
12. EnforceIpLimit               → yalnızca-IP kovası (elle, named policy dışı)
13. UseRateLimiter               → isimli politikalar + global kova
14. CsrfProtection               → çerezle gelen yazma isteği X-CSRF-Token ister
15. UseAuthentication            → JWT (Bearer başlığı > çerez)
16. UserStateMiddleware          → jeton canlı durumla uyuşuyor mu (G-01)
17. MustChangePasswordMiddleware → geçici şifre sunucuda da zorlanıyor (G-06)
18. UseAuthorization             → politika kapısı
19. Map*Endpoints (14 grup)
20. MapSpaFallback               → istemci rotaları index.html'e, API önekleri değil
```

**Kritik sıra kararı:** `UseForwardedHeaders` en başta, çünkü hız sınırı
bölümü, denetim izindeki IP ve çerezin `Secure` bayrağı isteğin gerçek
kaynağına bakıyor. `X-Forwarded-*` yalnızca **güvenilen vekiller** adına kabul
ediliyor (`Hosting:TrustedProxies`); liste boşsa yalnızca loopback. "Hepsine
güven" seçeneği bilinçli olarak yok — herkesin yazabildiği bir başlığa güvenmek
denetim izini saldırganın kalemine çevirir.

### 6.2. Kimlik: iki taşıma yolu, biri kaldırılamaz

| Yol | Kim kullanır | CSRF |
|---|---|---|
| `HttpOnly` + `SameSite=Strict` çerez (`t3.session`) | Tarayıcı | **İster** (`X-CSRF-Token`) |
| `Authorization: Bearer` | MCP istemcileri, Python betikleri, Swagger | İstemez (başka origin başlık koyamaz) |

`JwtBearerEvents.OnMessageReceived`: Bearer başlığı varsa o kullanılır, yoksa
çereze bakılır. Yani başlık yolunun **önceliği** var.

CSRF jetonu rastgele değil, **`HMAC-SHA256(Jwt:Secret, oturum jetonu)`**.
Sebep: iki bağımsız rastgele çerez modelinde alt alan adından çerez yazabilen
bir saldırgan ikisini de kendisi koyup çift-gönderim kontrolünü anlamsız
kılabiliyordu (G-11). Karşılaştırma sabit süreli
(`CryptographicOperations.FixedTimeEquals`). Çıkış kuralın dışında: CSRF çerezi
kaybolduğunda kullanıcının oturumunu **kapatamaz** hâle gelmesi daha büyük
risk.

`GET /api/me` eksik ya da **eskimiş** CSRF çerezini sessizce onarır — değer
oturum jetonundan türetildiği için önceki oturumdan kalan çerez artık
eşleşmiyor ve kullanıcı çıkışsız bir 403 duvarına toslardı.

### 6.3. Kaba kuvvet koruması — üç katman

1. **Hız sınırı** (`auth` kovası): IP + e-posta başına dakikada 10; ayrıca
   yalnızca-IP kovası dakikada 30 (parola serpiştirmeyi kapatır). İkisi
   zincirli, biri dolarsa 429 + `Retry-After`.
2. **Hesap kilidi**: `FailedLoginCount` 10'a ulaşınca `LockedUntil` dolar,
   sayaç sıfırlanır. Zaman içine yayılmış ya da farklı IP'lerden gelen
   denemeler için.
3. **Denetim izi**: `Auth.LoginSucceeded`, `Auth.LoginFailed`,
   `Auth.RateLimited`, `Auth.KvkkConsent` — e-posta **maskeli** (`k***@alan.test`),
   çünkü izin kendisi denenen adreslerin ham listesine dönüşmemeli. Hız sınırı
   kilidi pencere başına **tek** satır açar.

### 6.4. Hız sınırı kovaları

| Kova | Tasarım değeri | **Kodda etkin değer** | Uygulandığı yer |
|---|---|---|---|
| `auth` | 10 / dk (IP+e-posta) | **100 / dk** | `/api/auth/*` |
| `auth` (IP) | 30 / dk | **300 / dk** | `/api/auth/*`, elle |
| `mass-export` | 10 / saat (kullanıcı) | **100 / saat** | CSV export, doküman indirme |
| `ai` | 20 / dk (kullanıcı) | **200 / dk** | `/api/ai/*` grubu |
| `mcp` | 100 / dk | **1000 / dk** | `POST /mcp` |
| global | 300 / dk | **3000 / dk** | adı konmamış her uç |
| `ai-report` | 6 / saat | **60 / saat** | **hiçbir uca bağlı değil** |
| Excel aracı (işlem içi) | 10 / saat | **100 / saat** | `export_startups_excel` |

Tüm değerler `AuthRateLimit.TestMultiplier = 10` ile çarpılıyor. Bu **geçici
test çarpanı** koda yazılı ve hâlâ yerinde ([§10.2](#102-acele-borcu-koda-yazılı-geçici-çözümler)).

### 6.5. Doğrulama

FluentValidation, **endpoint filtresinde** (`.WithValidation<T>()`), handler'da
değil. Her istek tipi için tek doğrulayıcı; `AddValidatorsFromAssembly` ile
otomatik kaydedilir. Doğrulama hatası → 400 + `Errors[]` (tekilleştirilmiş).

İstisna: **doküman yükleme**. Filtre JSON gövdesi üzerinde çalışıyor, yükleme
multipart — kurallar `DocumentUploadRules` içinde, handler'ın çağırdığı tek
noktada:

- 20 MB üst sınır (sunucu tarafında 21 MB, önce devreye girsin diye)
- 12 uzantılı beyaz liste; **içerik tipi uzantıdan türetilir**, istemciden
  alınmaz
- **Sihirli bayt imzası** kontrolü (PDF `%PDF`, PNG, JPEG, ZIP tabanlı
  Office, OLE tabanlı eski Office). `.csv`/`.txt` imzasız olduğu için atlanır
- Dosya adı yol bileşenlerinden, kontrol karakterlerinden ve tırnaktan
  arındırılır (Content-Disposition enjeksiyonu)

### 6.6. Denetim izi

`IAuditWriter.WriteAsync(action, entityType, entityId, before?, after?)`.
Aktör ve rol `ICurrentUser`'dan, IP/User-Agent `IClientContext`'ten gelir
(kimlikten ayrı arayüz: "kim" ve "nereden" farklı sorular).

Onay **iki satır** yazar: `ChangeRequest.Approve` + `Startup.Update`. Böylece
iz, değişikliğin portaldan mı doğrudan mı geldiğine bakmadan aynı şekilde
sorgulanabiliyor. Otomatik onaylanan profil değişikliği de aynı adla yazılıyor.

Loglamada kişisel veri maskesi Serilog `Destructure.With<KisiselVeriMaskesi>()`
ile **her nesne loglanmadan önce** devrede. İstek logu rol ve kullanıcı Guid'i
taşır, **e-posta taşımaz** (Guid kişisel veri değil, e-posta öyle ve logun rol
kapısı yok).

### 6.7. Saklama süreleri (`RetentionCleanupService`, 24 saatte bir)

| Veri | Süre | İşlem |
|---|---|---|
| `PasswordResetToken` | süresi dolunca | **silinir** |
| `AuditLog` | 10 yıl | kişisel veriden **arındırılır**, olay kalır |
| `AiConversation` (+ mesajlar) | 1 yıl | **gerçekten silinir** |

Bu süreler aydınlatma metnindeki (`PrivacyNoticePage`) değerlerle aynı — metin
ile kod ayrışmasın diye kasıtlı olarak tek yerde.

### 6.8. AI altyapısı

```
IChatModel
├── AnthropicChatModel   (Ai:Provider = Anthropic)
├── OpenRouterChatModel  (varsayılan)
└── DisabledChatModel    (Ai:ApiKey boşsa)
```

Anahtar yoksa sistem **ayağa kalkmayı reddetmez** — AI zorunlu MVP maddesi
değil, karar destek eklentisi. Yerine `OfflineAssistant` (yerel planlayıcı)
devreye girer: anahtar kelimelerden araç çağrıları üretir ve yanıtı araç
özetlerinin birleşiminden kurar. Cümle üretmediği için **uydurma da
üretmez**. Açılışta uyarı log'u düşer; panel durumu rozetle söyler.

`AssistantConversationRunner` ajan döngüsünü tek yerde tutuyor: tek soruluk uç
(`/ask`) ve kalıcı sohbet (`/chat`) aynı bileşeni çağırıyor — döngüyü
kopyalamak, araç sonuçlarının modele giderken süzülmesi gibi adımların yalnızca
bir dilimde güncellenmesi riskini doğururdu.

`AiRedaction`: modele giden araç sonuçlarından hassas alanlar süzülüyor.

### 6.9. E-posta, depolama, rapor üreticileri

| Arayüz | Uygulama | Seçim kuralı |
|---|---|---|
| `IEmailSender` | `SmtpEmailSender` | SMTP dörtlüsü (host+port+user+password) **tam** doluysa |
| | `ThrowingEmailSender` | SMTP yok **ve** üretim → açık hata (sessiz arıza yok) |
| | `FileOutboxEmailSender` | SMTP yok ve geliştirme → diskteki kutu |
| `IDocumentStorage` | `LocalDocumentStorage` | Yerel disk; fiziksel ad rastgele |
| `IReportPdfRenderer` | `QuestPdfReportRenderer` | QuestPDF |
| `IExcelFileBuilder` | `ClosedXmlExcelFileBuilder` | ClosedXML |
| `IAssistantExportStore` | `InMemoryAssistantExportStore` | **Bellek içi**, singleton |
| `IOperationRateLimiter` | `InMemoryOperationRateLimiter` | **Bellek içi**, singleton |

Gönderici seçimi **ortama değil yapılandırmaya** bakıyor: SMTP'yi geliştirme
makinesinde de denemek mümkün olsun diye. "Yalnızca üretimde çalışan yol" ilk
kez canlıda denenmiş olurdu.

`FileOutboxEmailSender` üretimde bilinçli olarak kullanılmıyor: ham şifre
sıfırlama jetonunu sunucu diskine ikinci bir düz metin kopyası olarak yazardı
(G-02).

### 6.10. Tohum verisi

`DevDataSeeder` (~530 satır) **yalnızca `Development` ortamında** çalışır ve
`Seed:Enabled` ister; şifre `T3_Seed__Password`'dan gelir (yoksa açılışta
hata). Üretimde `Seed:Enabled` açık kalmışsa **hata log'u** düşer ve veri
yazılmaz — ortam kontrolü burada bir güvenlik sınırı.

İçerik: 5 program, 32 girişim, 41 katılım, 97 başarı kaydı, kilometre taşları,
5 doküman, 11 onay isteği, 8 hesap. Tüm veri kurgu: `.test` alan adları
(RFC 6761), tahsis edilmemiş telefon öneki.

**Boşluklar kasıtlı:** dört girişim hiçbir programa bağlı değil, biri hiç
başarı kaydı taşımıyor — "kapsam dışı" ve "boş durum" ekranları demoda gerçek
veriyle görünsün diye. İki Program Yöneticisi ayrık programlara atanmış: aynı
ucu çağırdıklarında tamamen farklı liste ve pano görüyorlar.

---

## 7. RBAC ve KVKK maskelemesi

### 7.1. Dört tek nokta

Yeni yetki kuralı bunların **dışına** yazılmaz:

| Bileşen | Sorumluluk |
|---|---|
| `IStartupScope` | Girişim **satırı** — hangi girişimleri görüyorum |
| `StartupVisibility` | **Alan** / KVKK — hangi alanları görüyorum |
| `IChangeRequestScope` | Onay **satırları** — hangi istekleri görüyorum, gönderebilir/karar verebilir miyim |
| `ProgramAccessGuard` | Program sahipliği — tanım SuperAdmin'de, dönem/katılım kendi programında |

### 7.2. Satır kapsamı (`StartupScope.Apply`)

| Rol | Gördüğü satırlar |
|---|---|
| SuperAdmin | Tümü |
| DecisionMaker | **Tümü** (kısıtı satır değil alan düzeyinde) |
| StartupUser | Yalnızca `user.StartupId` |
| ProgramManager | Atandığı programlardan **geçmiş** girişimler (`Participations.Any(p => programIds.Contains(p.ProgramTerm.ProgramId))`) |
| kimliksiz / diğer | `Where(_ => false)` |

Program Yöneticisi'nin kapsamı bir sonuç doğuruyor: **program dönemine
bağlanmamış yeni kayıt kendi listesinde görünmüyor.** Arayüz bu kuralı
yazıyor ve kayıttan sonra doğrudan katılım adımına düşürüyor.

### 7.3. Alan maskelemesi (`StartupVisibility`)

Beş bayrak: `ShowContactDetails`, `ShowTaxNumber`, `ShowExactAmounts`,
`ShowTeamPersonalData`, `ShowDocuments`.

| Rol | İletişim | Vergi no | Tekil tutar | Ekip kişisel verisi | Doküman |
|---|---|---|---|---|---|
| SuperAdmin | ✅ | ✅ | ✅ | ✅ | ✅ |
| ProgramManager | ✅ | ❌ | ✅ | ✅ | ✅ |
| StartupUser (kendi girişimi) | ✅ | ✅ | ✅ | ✅ | ✅ |
| DecisionMaker | ❌ | ❌ | ❌ | ❌ | ❌ |
| kimliksiz / başka girişim | ❌ | ❌ | ❌ | ❌ | ❌ |

**Agregat istisnası** (`StartupVisibility.Aggregate`): Karar Verici ekosistem
**toplamını** görür, tekil girişimin tutarını görmez. Brief finansal veriyi bu
role "yalnızca agregat" düzeyinde açıyor. Kural rapora özel bir bileşene değil
maskeleme sınıfının içine yazıldı ki dördüncü bir kural noktası doğmasın.

### 7.4. İki kritik maskeleme kararı

**1. Yetkisiz alan `null` döner, boş string dönmez.** Arayüz "veri yok" (—) ile
"yetkiniz yok" (🔒) ayrımını yapabilsin diye. Boş kutu kullanıcıyı yanıltır.

**2. Maskeleme yazma yolunda da tutuluyor.** Maskeli alan istemciye `null`
gittiği için tam değiştirmeli `PUT` onu **sessizce siliyordu**: vergi
numarasını göremeyen Program Yöneticisi'nin her düzenlemesi numarayı
boşaltırdı. `StartupWriteModel.ApplyTo(startup, visibility)` sunucunun
göremediği alanı korur, arayüz o alanı formda hiç göstermez ve gerekçesini
yazar.

### 7.5. Derinlemesine savunma

Her yazma yolu **iki kez** kontrol edilir: endpoint politikası + handler
içinde tekrar. Gerekçe teknik: **MCP araçları handler'ları doğrudan çağırıyor**
ve politika hattını atlıyor. `UserAdminGuard.EnsureCanManage()`,
`ProgramAccessGuard.EnsureCanManagePrograms()`, `ListAuditLogsHandler`'ın rol
kontrolü hep bu yüzden var.

### 7.6. Politika listesi (10)

| Politika | Roller |
|---|---|
| `startups:manage` | SuperAdmin, ProgramManager |
| `programs:manage` | **SuperAdmin** |
| `program-terms:manage` | SuperAdmin, ProgramManager |
| `approvals:review` | SuperAdmin, ProgramManager |
| `users:manage` | **SuperAdmin** |
| `audit:view` | **SuperAdmin** |
| `reports:ai-generate` | SuperAdmin, ProgramManager |
| `notifications:send` | SuperAdmin, ProgramManager |
| `notifications:view-all` | **SuperAdmin** |
| (fallback) | herhangi bir kimlik |

`audit:view` ile `users:manage` bugün aynı role açık ama **ayrı politika**:
tek sabiti paylaşmaları, birini gevşetirken diğerini sessizce gevşetmek olurdu.

---

## 8. Arayüz envanteri

### 8.1. Yığın

| Katman | Sürüm | Not |
|---|---|---|
| React | 19.2 | `StrictMode` |
| TypeScript | ~6.0 | `tsc -b` proje referansıyla |
| Vite | 8.2 | `@` → `src` takma adı, `/api` + `/health` vekili |
| Tailwind | 4.3 | `@tailwindcss/vite` eklentisiyle, config dosyası yok |
| TanStack Query | 5.101 | Tek sunucu-durum kaynağı |
| React Router | 7.18 | **Veri yönlendiricisi** (`createBrowserRouter`) |
| oxlint | 1.75 | ESLint yerine |

**Bağımlılık listesi bilinçli olarak dört paket.** Form kütüphanesi yok
(kontrollü `useState`), grafik kütüphanesi yok (`charts.tsx` içinde elle
yazılmış SVG `BarChart`/`DonutChart`), Markdown kütüphanesi yok
(`MarkdownText.tsx` model çıktısını React düğümlerine çeviriyor — hazır
çözümlerin `dangerouslySetInnerHTML` yolu bu ekranda XSS kapısı olurdu),
tarih kütüphanesi yok (`Intl` + `tr-TR`).

### 8.2. Rota ağacı (`routes.tsx`)

| Rota | Koruma | Sayfa |
|---|---|---|
| `/` | — | `LandingPage` (oturumu olanı panoya yollar) |
| `/giris` | — | `LoginPage` (statik, lazy değil) |
| `/kayit-ol` | — | `RegisterStartupPage` |
| `/sifremi-unuttum` | — | `ForgotPasswordPage` |
| `/sifre-sifirla/:token` | — | `ResetPasswordPage` |
| `/kvkk-aydinlatma` | `PublicPage` | `PrivacyNoticePage` |
| `/kullanim-sartlari` | `PublicPage` | `TermsPage` |
| `/pano` | `RequireAuth` + `AppShell` | `DashboardPage` |
| `/girisimler` | ↑ | `StartupsPage` |
| `/girisimler/:id` | ↑ | `StartupDetailPage` (6 sekme) |
| `/programlar` | ↑ | `ProgramsPage` |
| `/asistan` | ↑ | `AssistantChatPage` |
| `/bildirimler` | ↑ | `NotificationsPage` |
| `/profil` | ↑ | `ProfilePage` |
| `/sifre-degistir` | ↑ | `ChangePasswordPage` |
| `/onaylar`, `/onaylar/:id` | `canReviewApprovals` **veya** `mustSubmitForApproval` | `ApprovalsPage`, `ApprovalDetailPage` |
| `/portal` | `mustSubmitForApproval` | `PortalPage` |
| `/kullanicilar`, `/kayit-basvurulari`, `/denetim` | `canManageUsers` | `UsersPage`, `RegistrationRequestsPage`, `AuditPage` |
| `*` | `PublicPage` | `NotFoundPage` |

Rota bazlı kod bölme (`pages.ts`, `lazy()`): 18 ekran isteğe göre yükleniyor.
Açılış sayfası, giriş ekranı ve 404 bilinçli olarak statik — ilk ikisi her
ziyaretin ilk karesi, sonuncusu hata yolunda ek ağ isteğine bağlanmamalı.

Sonuç: **355 kB ana paket + 27 parça**, en büyük ekran 23 kB.

### 8.3. Koruyucular (`RouteGuards.tsx`)

- **`RequireAuth`** — oturum `/api/me`'den doğrulanana kadar bekler (kimlik
  çerezde olduğu için istemci "jetonum var mı" diye bakamıyor). Üç ayrı sonuç:
  `isResolving` → spinner, `connectionError` → "yeniden dene" düğmesi
  (**oturumu düşürmez**), `!session` → `/giris`'e (ya da bilinçli çıkışta
  `logoutRedirect`'e). Ayrıca `mustChangePassword` varsa her rotayı
  `/sifre-degistir`'e kilitler.
- **`RequirePermission anyOf={[...]}`** — yetkisiz rotada **yönlendirme
  yapmıyor**, açıklama gösteriyor: paylaşılan bağlantıya tıklayan kullanıcı
  "neden göremiyorum" cevabını almalı.
- **`PublicPage`** — oturum açıkken kabuk içinde, kapalıyken çıplak.

### 8.4. Oturum yönetimi (`AuthProvider` + `auth.ts`)

Kimlik `HttpOnly` çerezde; `apiClient.ts`'te **hiçbir jeton saklama kodu yok**
(bilinçli). İstemcide tutulan tek şey `localStorage`'daki
`t3.session.active` **bayrağı** — jeton değil. Üç işi var:

1. Siteye ilk gelen ziyaretçiye "oturum süresi doldu" demeyi engellemek
2. Sekmeler arası senkron (`storage` olayı)
3. Açılış sayfasının, `/api/me` cevabı gelmeden tanıtımı mı panoyu mu
   göstereceğini bilmesi

`signedOut` açık bir React durumu olmak **zorunda**: `queryClient.clear()`
önbelleği boşaltıyor ama ekrandaki bileşenlere yeni sonuç bildirmiyordu —
kullanıcı "Çıkış"a bastıktan sonra panoda kalıyordu. Aynı boşluk 401'de de
vardı (React Query hatada elindeki `data`'yı koruyor).

### 8.5. HTTP istemcisi (`apiClient.ts`, 161 satır)

Tek giriş noktası. `api.get/post/put/del/upload/download`.

- `ApiError(status, message, details?, referans?)` — ağ hatası **status 0**:
  "sunucuya ulaşılamıyor" ile "yetkin yok" ayrımı yapılabiliyor. Öncesinde
  `fetch` reddi yetki sorunu gibi ele alınıp oturumu düşürüyordu.
- CSRF jetonu okunabilir çerezden okunup başlığa konuyor; yalnızca durum
  değiştiren yöntemlerde.
- `upload`: `Content-Type` **bilinçli olarak ayarlanmıyor** (multipart
  boundary'yi tarayıcı üretir).
- `download`: `<a href>` yerine fetch — hata durumunda sunucunun Türkçe JSON
  mesajını gösterebilmek için. Blob URL 60 s sonra bırakılıyor.
- `credentials: 'same-origin'`.

`queryClient`: `staleTime` 30 s, 400/401/403/404'te **tekrar denemez**.

### 8.6. Özellik klasörleri (backend dilimleriyle simetrik)

| Klasör | Ekran / bileşen | Bağlandığı uçlar |
|---|---|---|
| `auth/` | Login, Register, Forgot, Reset, ChangePassword, AuthLayout, `kvkkConsent.ts` | `/api/auth/*` |
| `landing/` | LandingPage (248 satır) | — |
| `dashboard/` | DashboardPage (282), `charts.tsx` (176) | `/api/reports/ecosystem`, `/api/reports/export` |
| `startups/` | StartupsPage, StartupDetailPage (538), StartupForm (325), TeamSection (349), StartupTimeline, StartupTile, ParticipationEditor/Form, AiReportPanel, SendNotificationPanel | `/api/startups/*`, `/api/participations`, `/api/programs` |
| `achievements/` | AchievementSection (245), AchievementForm (327) | `/api/startups/{id}/achievements` |
| `documents/` | DocumentSection (257) | `/api/startups/{id}/documents`, `/api/documents/{id}/download` |
| `programs/` | ProgramsPage (363), ProgramForm, TermForm | `/api/programs/*` |
| `approvals/` | ApprovalsPage, ApprovalDetailPage (290) | `/api/change-requests/*` |
| `portal/` | PortalPage (69) | Aynı formları `mode="proposal"` ile besliyor |
| `assistant/` | AssistantChatPage (459), AssistantWidget (393), MarkdownText (209), StartupSummaryCard | `/api/ai/chat*`, `/api/ai/exports`, `/api/ai/startups/{id}/summary` |
| `notifications/` | NotificationsPage (307) | `/api/notifications/*` |
| `users/` | UsersPage (459) | `/api/users/*` |
| `registrations/` | RegistrationRequestsPage (234) | `/api/registration-requests/*` |
| `audit/` | AuditPage (202) | `/api/audit-logs` |
| `legal/` | PrivacyNoticePage (144), TermsPage, LegalDocument | — |
| `errors/` | NotFoundPage | — |
| `profile/` | ProfilePage | `/api/me` (oturumdan) |

**`/portal` ile `/girisimler/:id` aynı formları paylaşıyor**, `mode` propuyla:
`"direct"` doğrudan yazar, `"proposal"` `ChangeRequest` üretir. Denetim Dalga
0'da portalın formları `features/startups/` altına taşındı ki iki yol
ayrışmasın.

### 8.7. Ortak bileşenler

`components/ui.tsx` (222 satır): `Card`, `Avatar`, `Badge`, `Button`, `Input`,
`Select`, `Spinner`, `EmptyState`, `ErrorState`, **`Sensitive`**, `DataRow`.

`Sensitive` KVKK ayrımını taşıyan bileşen: "veri yok" (—) ile "yetkiniz yok"
(🔒) ekranda farklı görünür.

`AppShell` (202 satır): sol kenar çubuğu, rol bazlı menü (güvenlik önlemi
değil — yetki sunucuda; her tıklamada 403 yedirmenin alternatifi), onay ve
bildirim rozetleri, "İçeriğe atla" bağlantısı, mobilde katlanan panel,
`AssistantWidget` her ekranda.

`lib/`: `format.ts` (**`tr-TR` kültürü açıkça verilir**), `labels.ts` (enum →
Türkçe etiket sözlükleri), `UnsavedChangesGuard.tsx` (`useBlocker` — veri
yönlendiricisi bu yüzden gerekliydi), `useDocumentTitle.ts`, `ErrorBoundary`.

### 8.8. Tip aynası

`api/types.ts` (819 satır) backend DTO'larının **elle** tutulmuş aynası.
Enum'lar sunucuda metin olarak serileşiyor (`JsonStringEnumConverter`), bu
yüzden TypeScript tarafında `enum` değil birleşim tipi (`erasableSyntaxOnly`
açık).

**Bu dosya elle senkron tutuluyor** — üretilmiyor. Yeniden yazımda ilk
otomatikleştirilecek şey ([§11.3](#113-yeniden-yazımda-değişmesi-gerekenler)).

---

## 9. Doğrulama altyapısı

Projenin doğrulama alışkanlığı **üç katmanlı** ve sıra önemli:

### 9.1. Birim testleri — 266 test, 31 dosya, 3.075 satır

Veritabanına hiç dokunmuyorlar: `UnreachableDbContext` ve `FakeCurrentUser`
ile saf mantık test ediliyor. Kapsanan alanlar:

`StartupScopeTests`, `StartupVisibilityTests`, `StartupWriteMaskingTests`,
`EcosystemVisibilityTests`, `ChangeRequestScopeTests`, `ChangeRequestDiffTests`,
`ChangeRequestJsonTests`, `SubmitChangeRequestValidatorTests`,
`AchievementWriteModelTests`, `DocumentUploadRulesTests`,
`LocalDocumentStorageTests`, `ProgramAccessRulesTests`, `UserAdminRulesTests`,
`PasswordChangeRulesTests`, `PasswordResetSecretsTests`,
`Pbkdf2PasswordHasherTests`, `MaskedEmailTests`, `KisiselVeriMaskesiTests`,
`SearchTextTests`, `PagedRequestTests`, `CsvBuilderTests`,
`AssistantToolboxTests`, `AiRedactionTests`, `ChatHistoryTests`,
`ConversationOwnershipTests`, `OpenRouterPayloadTests`,
`StartupRegistrationTests`, `EpostaGonderimTests`.

**Yok olan:** entegrasyon testi (`WebApplicationFactory`) ve **arayüz testi**
(hiç yok).

### 9.2. Uçtan uca betikler (Python, çalışan API'ye karşı)

`scripts/e2e_faz3.py` (81 kontrol), `e2e_faz4.py` (125), `e2e_faz5.py` (104)
— toplam **310 kontrol**. Gerçek rollerle giriş yapıp aynı ucu farklı
jetonlarla çağırıyor; MCP ile REST'in aynı sayıyı verdiğini doğruluyor.

### 9.3. Headless Chrome render betikleri

`render_faz3..8.py` + `render_vps.py` — **352 render kontrolü**.
`google-chrome-stable --headless=new --dump-dom` ile gerçek DOM üzerinden.

Bu adım defalarca iş gördü: `data-testid`'yi yutan `Card` bileşenini, Karar
Verici'ye tekil tutar sızdıran ilk maskeleme sürümünü, derlenmeyen bir
`AppShell`'i ve ekranda etkisiz kalan "Çıkış" düğmesini yalnızca render
yakaladı. **index.html'in 200 dönmesi uygulamanın açıldığını göstermez.**

Betikler veriyi değiştirdiği için (girişim pasife alma, kullanıcı oluşturma)
tam yeşil zincir **sıfırlanmış veritabanı** ister.

### 9.4. CI (`.github/workflows/ci.yml`)

| İş | Adımlar |
|---|---|
| `backend` | restore → build (Release) → `dotnet test` → **`dotnet list package --vulnerable`** (bulgu varsa CI kırmızı) |
| `frontend` | `npm ci` → `npm run build` (`tsc -b` gerçek tip kontrolü buradan geçiyor) |

E2E ve render betikleri CI'da **koşmuyor** — çalışan bir yığın ve Chrome
gerektiriyorlar.

### 9.5. Yerel altyapı

`docker-compose.yml`: Postgres 16 (**5433**), pgAdmin (`--profile tools`,
5050), Loki + Grafana (`--profile observability`, 3100/3300).
API **5080**, Vite **5173**.

`docker-compose.prod.yml`: Postgres portu yayımlanmıyor, API yalnızca
loopback'e bağlanıyor, şemayı kendisi kuruyor (`Database:MigrateOnStartup`),
tohum verisi kapalı, kök olmayan kullanıcı.

---

## 10. Sorunlar ve teknik borç

Hepsi bu oturumda kodda **doğrulandı**; her madde dosya/satır kanıtı taşıyor.

### 10.1. MVP riski taşıyan boşluklar

**B-01 — Kilometre taşı yazma yolu hiç yok. (Yüksek)**

`Milestone` varlığı, EF eşlemesi, Türkçe etiketleri ve zaman çizelgesindeki
gösterimi var; **yazan tek kod `DevDataSeeder`.**

- Endpoint yok (14 endpoint dosyasında `Milestone` geçmiyor)
- `ChangeTargetType` içinde yok → girişim kullanıcısı öneremiyor
- Arayüzde form yok (`frontend/src`'de yalnızca etiket ve tip olarak geçiyor)

Sonuç: MVP #2'nin "**önemli gelişim adımları** kronolojik olarak izlenir"
maddesi çalışan üründe **karşılanmıyor**. Demoda görünüyor çünkü tohum verisi
onları yazmış. Boş bir veritabanında kimse kilometre taşı ekleyemez.

**B-02 — Girişim profili onaysız yayına giriyor. (Yüksek — jüri sorusu)**

`SubmitChangeRequestHandler`: `TargetType == ChangeTargetType.Startup` ise
öneri **gönderimle aynı anda** uygulanıyor ve kayıt `Approved` olarak
kapanıyor. Doküman, ekip üyesi ve başarı/yatırım hâlâ onay bekliyor.

Brief MVP #3 net: *"değişiklikler admin onayıyla yayınlanır (doğrudan yayın
yok — onay akışı zorunlu)"*. Karar `docs/Gelistirme_Kararlari.md §3p` içinde
gerekçelendirilmiş (profil alanları kanıt gerektirmiyor, yönetici kuyruğunu
doldurmaya değmiyor) ama **brief'in sözünden ayrılıyor.** Portal ekranı bunu
yazıyor ("profil bilgileriniz kaydettiğiniz anda yayına girer") — yani
gizlenmiyor, ama savunulması gereken bir sapma.

**B-03 — Program katılımı öneri akışında yok. (Düşük)**

Girişim kullanıcısı "şu programa katıldım" önerisi gönderemiyor;
`ProgramParticipation` `ChangeTargetType` içinde değil. Bu muhtemelen doğru
karar (katılımı program yöneticisi doğrular) ama hiçbir yerde kayıtlı değil.

### 10.2. Acele borcu: koda yazılı geçici çözümler

**B-04 — Tüm hız sınırları 10 kat gevşek. (Yüksek)**

`AuthRateLimit.cs:29` → `private const int TestMultiplier = 10;`

Kendi doküman yorumu şunu diyor: *"Demo/canlıya çıkmadan önce bu **1**'e
döndürülmeli."* Döndürülmemiş. Etkin değerler: giriş **100/dk**, IP kovası
**300/dk**, kütlesel aktarma **100/saat**, AI **200/dk**, MCP **1000/dk**,
global **3000/dk**.

Aynı çarpan ikinci bir yerde daha kopyalanmış:
`AssistantToolbox.cs:71` → `ExcelExportHourlyLimit = 10 * 10`.

Yani kaba kuvvet koruması, kütlesel veri çekme koruması ve AI kotası
**tasarlandığı sıkılıkta çalışmıyor.** Kod doğru, sabit yanlış.

**B-05 — En pahalı ucun hız sınırı kaldırılmış. (Yüksek)**

`StartupEndpoints.cs:81-85`:

> *"Hız sınırı (AuthRateLimit.AiReportPolicy) ŞİMDİLİK KALDIRILDI —
> geliştirme/deneme sırasında saatte 6 kovası engelliyordu. Demo/canlı öncesi
> geri eklenmeli."*

`GET /api/startups/{id}/ai-report` tek tıklamada **6 standart bölüm + varsa
özel istek** için ayrı ayrı model çağrısı yapıyor. Şu anda yalnızca global
kova (3000/dk) onu sınırlıyor. `AiReportPolicy` tanımlı ama **hiçbir uca bağlı
değil** — yani ölü politika.

Bu ikisi (B-04, B-05) aynı hikâyenin iki yarısı: geliştirme hızını açan
gevşetmeler geri alınmamış.

### 10.3. Ölü ve yarı bağlı kod

| Kod | Durum |
|---|---|
| `POST /api/ai/ask` + `AskAssistantHandler` + `AskAssistantRequest` + doğrulayıcı | **Arayüzden hiç çağrılmıyor.** Arayüz `/api/ai/chat` kullanıyor. Uç yalnızca betikler/Swagger için ayakta |
| `AuthRateLimit.AiReportPolicy` | Tanımlı, kayıtlı, **kullanılmıyor** (B-05) |
| `Milestone` yazma tarafı | Varlık + eşleme + etiket var, yazan yok (B-01) |
| `IAssistantExportStore` (bellek içi) | Yeniden başlatmada tüm indirme jetonları kaybolur. Tek örnekli kurulumda sorun değil, yatay ölçekte **sessizce** bozulur |
| `IOperationRateLimiter` (bellek içi) | Aynı sorun: ikinci örnek kotayı ikiye katlar |

### 10.4. Mimari sürtünme — değişimi pahalı yapan yerler

**B-06 — `api/types.ts` elle tutulan 819 satırlık ayna.**
Backend DTO'su değişince TypeScript tarafı **elle** güncelleniyor. Ayrışma
yalnızca çalışma zamanında görülür. OpenAPI şeması zaten üretiliyor (Swagger
açık) ama istemci tipi ondan üretilmiyor.

**B-07 — Arayüz `strict` modda derlenmiyor.**
`tsconfig.app.json` içinde `"strict": true` **yok**. `noUnusedLocals`,
`noUnusedParameters`, `noFallthroughCasesInSwitch` var ama `strictNullChecks`,
`noImplicitAny` yok. 11.480 satır arayüz kodu bu ağ olmadan yazılmış.

**B-08 — Hiç entegrasyon testi, hiç arayüz testi yok.**
266 birim testi saf mantığı kapsıyor (kapsam filtresi, maskeleme, doğrulama).
Ama **67 ucun politika bağlantısı** — hangi ucun hangi politikayla korunduğu —
yalnızca çalışan sisteme karşı koşan Python betikleriyle doğrulanıyor. Bir uç
üzerindeki `.RequireAuthorization(...)` satırının silinmesi hiçbir testi
kırmaz. Arayüz tarafında test dosyası **sıfır**.

**B-09 — CI şu anda kırmızı (ya da olmalı).**
`dotnet list package --vulnerable --include-transitive` MailKit 4.8.0 ve
MimeKit 4.8.0 için orta seviye zafiyet bildiriyor (doğrulandı). CI adımı bu
metni görürse iş **başarısız** oluyor. Yani `main` dalı kendi kalite kapısını
geçmiyor.

**B-10 — Bellekte yapılan agregasyon ve sabit tavanlar.**
Pano istatistikleri kapsam filtresinden sonra **bellekte** hesaplanıyor
(`EcosystemStatsHandler`, 304 satır). CSV aktarma **2000 satır** tavanıyla ve
tek seferde üretiliyor; akış ya da arka plan işi yok. Ekosistem ölçeğinde
sorun değil, on binlerce kayıtta duvar.

**B-11 — Soft delete zinciri elle yürütülüyor.**
Global sorgu filtresi yalnızca okumayı daraltıyor. `DeleteStartupHandler` altı
tabloyu tek tek işaretliyor. Yeni bir alt tablo eklenince buraya satır eklemek
**hatırlanmak** zorunda; unutulursa yetim kayıt sessizce kalır.

**B-12 — Sağlayıcıya özel EF yasağı bedelini Türkçe aramada ödüyor.**
Application katmanında `EF.Functions.ILike`, `AsSplitQuery`,
`ExecuteUpdate/Delete` kullanılamıyor. Türkçe **İ** küçültmede bozulduğu için
karşılaştırmalar `SearchText.Normalize` üzerinden yapılıyor ve sorgular
`ToLower()` çevirisine düşüyor — indeks kullanımı için ideal değil.

**B-13 — 11.851 satır migration, son beşi tek haftada.**
Dokuz migration'ın beşi 4–6 Eylül arasında yazılmış (güvenlik damgası, kaba
kuvvet, kendi kendine kayıt, sohbet geçmişi, bildirim, bildirim silme, Excel
aktarma). Veri modeli son dalgada hâlâ hareket ediyordu — sıfırdan yazımda bu
yedi kararın **baştan** modele girmesi 11 bin satır göç kodunu ortadan
kaldırır.

**B-14 — Belge kütlesi.**
`docs/Gelistirme_Kararlari.md` tek başına **1.478 satır / 102 KB**.
`Guvenlik_Denetimi_ve_Iyilestirme_Plani.md` 314 satır,
`Denetim_Duzeltme_Plani.md` 719 satır. Toplam 5.033 satır belge, 34.700 satır
koda karşılık. Değerli ama artık **bakım maliyeti** üretiyor: kodla ayrışan her
satır yanlış bilgi.

### 10.5. Belge-kod ayrışması

**B-15 — README ürünü olduğundan eksik gösteriyor.**

| README diyor | Kod ne diyor |
|---|---|
| "191 birim testi" | **266** |
| MCP "altı araç" / "yedi araç" (iki yerde farklı) | **8 araç tanımlı, 7'si MCP'de** |
| Bilinçli sınırlar: *"AI sohbeti tek soruluk; oturum geçmişi tutulmuyor, önceki soruya atıf yapılamıyor"* | **Çok turlu sohbet var** (`AiConversation`, `/api/ai/chat`, 05.09 commit'i). Bu satır artık yanlış |
| Bilinçli sınırlar: yenileme jetonu yok | Doğru |

Bunlar küçük hatalar değil: README projenin jüriye/dış okuyucuya dönük yüzü ve
**ürünü olduğundan eksik gösteriyor.**

### 10.6. Ne sorun DEĞİL

Yeniden yazımda bozulmaması gereken, sağlam duran şeyler:

- **RBAC'ın dört tek noktası.** 266 testin en büyük kısmı bunları kapsıyor ve
  hiçbir dilim kendi filtresini yazmıyor. Bu disiplin nadir ve çalışıyor.
- **Maskelemenin yazma yolunda da tutulması.** `ApplyTo(startup, visibility)`
  gerçek bir hatayı kapattı (vergi numarasını sessizce silen `PUT`).
- **"Veri yok" ile "yetkiniz yok" ayrımı.** `null` dönen alan + `Sensitive`
  bileşeni. Jüriye gösterilecek KVKK anlatısının görünen yüzü.
- **Hata şeklinin tek olması.** Üç ayrı kaynak aynı `ApiErrorBody`'yi üretiyor;
  arayüz tek biçim ayrıştırıyor.
- **Kimliğin iki taşıma yolu.** Çerez tarayıcı için, Bearer betik/MCP için.
  CSRF'in HMAC türevi olması (rastgele ikinci çerez değil) gerçek bir saldırı
  yolunu kapattı.
- **Derinlemesine savunma gerekçesi.** MCP handler'ları doğrudan çağırdığı için
  handler içi tekrar kontrol zorunlu — bu bir fazlalık değil.
- **Üç katmanlı doğrulama alışkanlığı.** Özellikle headless render adımı:
  dört ayrı gerçek hatayı yalnızca o yakaladı.
- **Kod yorumlarının "neden" anlatması.** Bu raporun yazılabilmesinin sebebi.
  Yeniden yazımda korunması gereken en değerli alışkanlık.

---

## 11. Sıfırdan yazım rehberi

### 11.1. Kapsamın değişmeyen çekirdeği

Yeniden yazımda **pazarlık dışı** olan, brief'ten gelen liste:

1. Merkezi girişim kartı (künye + teknoloji + ekip + ürün + program geçmişi +
   göstergeler, **tek kartta**)
2. Kronolojik gelişim yolculuğu — program katılımları **ve** elle girilen
   gelişim adımları
3. Girişim portalı + **onay akışı**; doğrudan yayın yok
4. Alan bazlı satış/yatırım/başarı/doküman modeli (serbest metin değil)
5. Dört rol, satır **ve** alan düzeyinde yetki
6. KVKK: hassas alan maskeleme + denetim izi + saklama süreleri

Bu altı maddenin **hepsi** çalışan üründe, boş bir veritabanında,
kullanıcı eliyle uçtan uca yapılabilir olmalı. Tohum verisinin yazdığı ama
kullanıcının yazamadığı hiçbir şey "var" sayılmaz — B-01'in dersi bu.

### 11.2. Korunması gereken kararlar

| Karar | Neden korunmalı |
|---|---|
| `Api → Application → Domain`, `Infrastructure → Application` | Domain'in hiçbir şeye bağımlı olmaması eşleme/maskeleme testlerini veritabanısız yazdırıyor |
| Dikey dilim klasörlemesi | Bir use-case'in her parçası tek klasörde; 67 uçta gezinmeyi mümkün kılan tek şey |
| MediatR yok, düz handler | MCP handler'ları doğrudan çağırıyor; ardışık düzen soyutlaması burada yalnızca engel |
| Mapper kütüphanesi yok | Maskeleme kararının okunabilir kalması bir gereksinim, tercih değil |
| Doğrulama endpoint filtresinde | Handler'ın doğrulayıcıyı çağırmayı unutması mümkün olmuyor |
| `FallbackPolicy` = kimlik zorunlu | Yeni uç yazan kişinin yetkilendirmeyi **unutması** mümkün olmuyor |
| RBAC'ın tek noktaları | Kural sayısı 4'te kaldı; her yeni ekran yeni kural doğurmuyor |
| `Result<T>` → tek HTTP eşlemesi | Aynı hata türü her uçta aynı kodu üretiyor |
| Yetkisiz alan `null`, boş string değil | "Veri yok"/"yetkiniz yok" ayrımı |
| Denetim izinde e-posta **maskeli** | İz, denenen adreslerin ham listesine dönüşmüyor |
| Enum'lar JSON'da **ad** olarak | Arayüz tipleri okunabilir, yeni enum değeri anlam kaydırmıyor |
| Tohum verisinde kasıtlı boşluklar | Boş durum ekranları demoda gerçek veriyle görünüyor |
| Sunucu tarafı biçimlendirmede kültür **açıkça** `tr-TR` | Yerel ayara bırakılan biçimlendirme makineye bağlı diff üretiyor |
| Yorumlar "neden"i anlatıyor | Altı ay sonra kararı geri almadan önce gerekçeyi okumak |

### 11.3. Yeniden yazımda değişmesi gerekenler

Öncelik sırasıyla:

**1. İstemci tiplerini üret, elle yazma.** (B-06)
Swagger/OpenAPI şeması zaten var. `api/types.ts`'in 819 satırı üretilmeli.
Ayrışma derleme zamanında yakalanmalı, çalışma zamanında değil.

**2. Arayüzü `strict` modda yaz.** (B-07)
`"strict": true` ilk günden. 11 bin satır kod sonradan strict'e taşınmıyor;
baştan yazılıyor.

**3. Politika bağlantısını test edilebilir yap.** (B-08)
İki seçenek: (a) `WebApplicationFactory` ile entegrasyon testi — her uç için
"yetkisiz rol 403 alır" testi; (b) uç tanımlarını bir tablodan üret ve
tablonun kendisini test et. Şu anda bir `.RequireAuthorization()` satırının
silinmesi hiçbir testi kırmıyor — bu kabul edilemez.

**4. Geçici sabitleri kodda bırakma.** (B-04, B-05)
Hız sınırı değerleri **yapılandırmadan** okunmalı (`RateLimits:Auth:PerMinute`),
koda gömülü çarpan olmamalı. O zaman "test için gevşet" bir `.env` satırı olur,
geri alınması unutulan bir `const` olmaz. Ayrıca: her isimli politikanın en az
bir uca bağlı olduğunu doğrulayan bir test.

**5. Kilometre taşı ve program katılımını öneri akışına al.** (B-01, B-03)
`ChangeTargetType` beş/altı değer olmalı: `Startup`, `TeamMember`,
`Achievement`, `Document`, `Milestone`, (isteğe bağlı) `Participation`.
Kilometre taşı için CRUD ucu + arayüz formu + portal öneri yolu.

**6. Onay akışındaki sapmayı karara bağla.** (B-02)
İki tutarlı seçenekten biri:
- **(a) Brief'e dön:** profil değişikliği de onay bekler. Kuyruk kalabalıklaşır
  ama "doğrudan yayın yok" cümlesi koşulsuz doğru olur.
- **(b) Sapmayı modele yaz:** alan bazında "onay gerektirir mi" bayrağı
  (`RequiresApproval`) — telefon onaysız, vergi numarası onaylı. O zaman karar
  bir `if TargetType == Startup` özel durumu değil, modelin bir parçası olur ve
  jüriye "hangi alan neden" gösterilebilir.

Öneri: **(b)**. Brief'in ruhuna daha yakın ve şu andaki blanket istisnadan daha
savunulabilir.

**7. Bellek içi durumu dışarı çıkar.** (B-04'ün altyapı yarısı)
`IAssistantExportStore` ve `IOperationRateLimiter` bellekte. Tek örnekte
çalışıyor, ikinci örnekte **sessizce** bozuluyor. Ya arayüzün arkasına kalıcı
bir uygulama koy (Postgres tablosu yeter), ya da tek örnek varsayımını
yapılandırmada açıkça belgele.

**8. Agregasyonu veritabanına taşı.** (B-10)
Pano istatistikleri kapsam filtresinden sonra bellekte hesaplanıyor. Sağlayıcı
bağımsızlığı kuralı bunu zorlaştırıyor — kural gözden geçirilmeli: tek
sağlayıcı (Postgres) hedeflenip `ILike`/`ExecuteUpdate` serbest bırakılırsa
hem arama hem agregasyon düzelir. **Sağlayıcı bağımsızlığı bu projede hiçbir
şey kazandırmıyor** (SQLite'a geçme planı yok), maliyeti ise Türkçe aramada ve
pano performansında ödeniyor.

**9. Soft delete zincirini modele yaz.** (B-11)
`DELETE ON CASCADE` benzeri bir zincir tanımı ya da `Startup` kökünden
gezilebilir bir "sahip olunan tablolar" listesi. Elle yürüyen altı satır,
yedinci tablo eklenince unutulacak.

**10. Belge kütlesini üçe indir.** (B-14, B-15)
- `CLAUDE.md` — her görevde geçerli kurallar (bugünkü hâli iyi)
- Tek **mimari kararlar** belgesi (bugünkü 1.478 satırlık dosyanın hâlâ geçerli
  olan kararları; tarihsel "şu dalgada şunu yaptık" anlatısı ayıklanmış)
- Tek **çalıştırma** belgesi (kurulum, komutlar, doğrulama sırası)

README **ürünü** anlatmalı, faz geçmişini değil. Bugünkü README'nin 39 KB'ının
büyük kısmı "Faz 2 tamamlandı… Dalga 1 tamamlandı…" — bu bilgi git
geçmişinde zaten var ve README'yi kodla ayrışmaya en açık dosya yapıyor
(B-15'teki üç yanlış satırın kaynağı bu).

**11. MailKit'i güncelle, CI'ı yeşile al.** (B-09)
`main` kendi kalite kapısını geçmiyor. Yeniden yazımın ilk commit'i yeşil CI
ile başlamalı.

**12. Veri modelini baştan tam kur.** (B-13)
Sıfırdan yazımın en büyük tek kazancı: güvenlik damgası, kaba kuvvet sayacı,
kendi kendine kayıt, sohbet geçmişi, bildirim, bildirim silme ve Excel aktarma
— yedisi de **baştan** modele girerse 11.851 satır göç kodu tek `InitialCreate`
olur.

### 11.4. Önerilen yazım sırası

Her adım kendinden öncekinin üstüne biniyor; her adımın sonunda **çalışan ve
doğrulanmış** bir dilim olmalı.

| Adım | İçerik | Bitiş kanıtı |
|---|---|---|
| 0 | Çözüm iskeleti, `strict` arayüz, yeşil CI, üretilmiş istemci tipleri, Postgres, tek `InitialCreate` (17 tablo, yedi geç karar dâhil) | `dotnet build && dotnet test && npm run build` yeşil, CI yeşil |
| 1 | Kimlik + RBAC'ın dört tek noktası + `FallbackPolicy` + denetim izi yazıcısı | Kapsam ve maskeleme birim testleri; her rol için 403 entegrasyon testi |
| 2 | MVP #1: girişim kartı (arama/filtre/sıralama/sayfalama, tam kart, ekip) | Rol × alan maskeleme matrisi testte; headless render |
| 3 | MVP #2: program zinciri + katılım + **kilometre taşı CRUD** + zaman çizelgesi | Boş veritabanında kullanıcı eliyle uçtan uca bir yolculuk kurulabiliyor |
| 4 | MVP #3: onay akışı + portal + alan bazlı onay gerekliliği (§11.3/6b) | "Doğrudan yayın yok" iddiası koşulsuz doğrulanabiliyor |
| 5 | MVP #4: TPH başarı modeli + doküman yükleme/indirme (imza kontrolü dâhil) | Tür değişimi 409, maskeli tutar `null`, indirme izde |
| 6 | Karar destek: ekosistem panosu (**veritabanı tarafı agregasyon**) + CSV/Excel + agregat maskeleme | Karar Verici toplamı görüyor, tekil tutarı görmüyor |
| 7 | AI + MCP: araç kutusu aynı handler'ları sarıyor, anahtar yoksa yerel plan | MCP ile REST aynı sayıyı veriyor |
| 8 | Cilalama: bildirim, kayıt başvurusu, şifre kurtarma, KVKK metinleri | Tam doğrulama turu |

Adım 0–5 zorunlu kapsam. 6–8 zorunlu kapsamın **dışında** ama ürünün ayırt
edici tarafı; 5'ten önce başlanmamalı.

### 11.5. Yeniden yazım için kontrol listesi

Her adımın sonunda sorulacaklar:

- [ ] Bu dilimde yazılan yetki kuralı RBAC'ın dört tek noktasından birinin
      içinde mi, yoksa beşinci bir yer mi açtım?
- [ ] Maskelenen alan yazma yolunda korunuyor mu (tam değiştirmeli `PUT`
      onu silmiyor mu)?
- [ ] Yeni uç `FallbackPolicy` dışında mı — öyleyse `.AllowAnonymous()`
      bilinçli mi?
- [ ] Yeni uçta politika **ve** handler içi kontrol var mı (MCP hattı)?
- [ ] Yeni alt tablo eklediysem soft delete zincirine girdi mi?
- [ ] Yeni istek tipi için **tek** doğrulayıcı var mı, endpoint filtresine
      bağlı mı?
- [ ] Yazma işlemi denetim izine düşüyor mu; kişisel veri maskeli mi?
- [ ] Kullanıcıya dönen metin Türkçe mi; sunucu tarafı biçimlendirmede kültür
      açıkça verildi mi?
- [ ] Bu özelliği **tohum verisi olmadan**, boş veritabanında kullanıcı eliyle
      yapabiliyor muyum?
- [ ] Geçici bir gevşetme eklediysem yapılandırmada mı, kodda sabit olarak mı?
- [ ] `npm run build` koştu mu (`npx tsc --noEmit` bu repoda hiçbir dosyayı
      kontrol etmiyor)?
- [ ] Headless render kontrolü yapıldı mı (200 dönmesi yeterli değil)?

---

## 12. Ortam ve tuzaklar (yeniden yazımda da geçerli)

| Konu | Değer / kural |
|---|---|
| Portlar | Postgres **5433**, API **5080**, Vite **5173**, pgAdmin 5050, Loki 3100, Grafana 3300 |
| `.env` | Değerler **çift tırnaklı** kalmalı (bağlantı dizesinde `;` var) |
| Sırlar | Tümü `T3_` önekli ortam değişkeni; `__` iç içe bölüm (`T3_Jwt__Secret` → `Jwt:Secret`). JWT anahtarı **hiçbir koşulda** `appsettings.json`'a yazılmaz |
| Arayüz tip kontrolü | **Yalnızca** `npm run build` (`tsc -b`). Kök `tsconfig.json` sadece referans dosyası; `npx tsc --noEmit` hiçbir dosyayı kontrol etmez |
| EF | Application katmanında sağlayıcıya özel API yasak (bugünkü kural — §11.3/8'de gözden geçirilmesi öneriliyor) |
| Türkçe **İ** | Küçültmede bozulur → karşılaştırmalar `SearchText.Normalize` üzerinden; gösterim etiketlerinde küçültme yapılmaz |
| Kabuk | Bash aracı **zsh** çalıştırır (fish değil). `pkill -f 'T3[.]Api'` yaz — köşeli parantez olmadan kendi kabuğunu öldürür |
| `dotnet ef` | Proje-yerel manifest (8.0.10) + **mutlak** `--project` yolları |
| Doğrulama betikleri | Temiz tohum verisi ister (veriyi değiştiriyorlar) |

---

## 13. Özet

**Çalışan ürün:** 67 uç, 17 tablo, 4 rol, 10 politika, 22 ekran, 8 AI aracı,
34.700 satır elle yazılmış kod. Backend derleniyor, 266 test geçiyor, arayüz
derleniyor.

**Zorunlu kapsamın durumu:** altı maddenin dördü tam, ikisinde sapma —
kilometre taşı yazma yolu hiç yok (B-01) ve girişim profili onaysız yayına
giriyor (B-02).

**En kritik üç borç:**
1. Hız sınırları 10 kat gevşek ve en pahalı ucun sınırı hiç yok (B-04, B-05)
2. 67 ucun yetki bağlantısını koruyan hiçbir test yok (B-08)
3. `main` kendi CI kapısını geçmiyor (B-09)

**Sıfırdan yazımın en büyük kazancı:** yedi geç kararın baştan modele girmesi
(11.851 satır göç kodu → tek `InitialCreate`), `strict` arayüz, üretilmiş
istemci tipleri ve politika bağlantısını koruyan entegrasyon testleri.

**Korunması gereken:** RBAC'ın dört tek noktası, maskelemenin yazma yolunda da
tutulması, "veri yok"/"yetkiniz yok" ayrımı, üç katmanlı doğrulama alışkanlığı
ve yorumların "neden"i anlatması.

---

*Bu rapor 9 Eylül 2026'da `a4b2d2d` commit'ine karşı üretildi. Kod değiştikçe
eskiyecek; yeniden yazım başladığında kaynak değil **karşılaştırma noktası**
olarak kullanılmalı.*
