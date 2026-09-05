# Siber Güvenlik ve Veri Güvenliği Denetimi — Bulgular ve İyileştirme Planı

**Denetim tarihi:** 4 Eylül 2026 · **Kapsam:** `backend/src` (231 C# dosyası), `frontend/src`, `Dockerfile`, `docker-compose*.yml`, `.env*`, `docs/`, git geçmişi · **Yöntem:** kod okuma + yapılandırma karşılaştırması (canlı sisteme sızma testi yapılmadı)

---

## 0. Yönetici özeti

Bu depo, güvenliği sonradan eklenmiş bir proje değil. Yetkilendirme varsayılan kapalı (`FallbackPolicy`), RBAC dört tek noktada toplanmış (`IStartupScope`, `StartupVisibility`, `IChangeRequestScope`, `ProgramAccessGuard`), maskeleme **yazma yolunda da** korunuyor (`StartupWriteModel.ApplyTo`), jeton `HttpOnly` çerezde, CSRF çift-gönderim var, CSP sıkı, log için ayrı kişisel veri maskesi (`KisiselVeriMaskesi`) yazılmış, denetim izinde e-posta maskeleniyor, dosya adı yola girmiyor, CSV formül enjeksiyonu kaçırılıyor, tohumlayıcı ortam sınırıyla korunuyor. RBAC ve maskeleme için **20 birim testi** var. Bu, jüriye anlatılabilir bir güvenlik duruşu.

Bulunan 21 sorunun neredeyse tamamı bu yüzden "unutulmuş kontrol" değil, **iki farklı sınıfa** düşüyor:

1. **Sınırın yarısı uygulanmış.** Kontrol var, ama tek yolda: `IsActive` yalnızca girişte bakılıyor, `MustChangePassword` yalnızca arayüzde, hız sınırı yalnızca iki uçta, CSRF ara katmanı doğru ama yanındaki yorum artık yanlış.
2. **Kod ile dağıtım arasındaki kayma.** Canlı sunucu elle sertleştirilmiş (`RequireHttps=true`, güvenilen vekil = docker ağ geçidi), fakat **depodaki üretim yapılandırması bunun tersini söylüyor**. Sunucu bir kez yeniden kurulursa sertleştirme kaybolur.

Yüksek önemli dört bulgu: **G-01** (pasife alınan hesap 8 saat daha tam yetkili), **G-02** (üretimde şifre sıfırlama jetonu diskte düz metin), **G-03** (depodaki üretim yapılandırması TLS'i ve gerçek istemci IP'sini kapatıyor), **G-04** (AI açıkken kişisel veri yurt dışına gidiyor, aydınlatma metni bunun tersini yazıyor).

| Önem | Adet | Bulgular |
|---|---|---|
| Yüksek | 4 | G-01 … G-04 |
| Orta | 6 | G-05 … G-10 |
| Düşük / sertleştirme | 11 | G-11 … G-21 |

> **5–6 Eylül Demo Day notu.** Aşağıdaki Faz 0'daki dört iş bu akşam kapatılabilir (hepsi yapılandırma ve metin düzeltmesi, kod riski yok). G-01 ve G-05 kod işi — demoya yetişmezse **saklamak yerine anlatmak** daha güçlü bir duruş: "jeton iptal listesi bilinçli olarak Faz 1'e bırakıldı, gerekçesi şu, planı şu" cümlesi jüri karşısında bir eksiklikten çok mühendislik olgunluğu okur. §5'te jüri soru-cevabı için hazırlık var.

---

## 1. Yüksek önemli bulgular

### G-01 · Jeton iptali yok — pasife alınan hesap 8 saat daha tam yetkili

**Nerede:** `T3.Api/Identity/HttpCurrentUser.cs`, `T3.Infrastructure/Identity/JwtOptions.cs:22`, `T3.Application/Features/Users/DeactivateUser/DeactivateUserHandler.cs`

`HttpCurrentUser` kimliği **yalnızca jetondaki claim'lerden** okuyor: `t3:uid`, `t3:role`, `t3:startup`, `t3:programs`. `IsActive` alanına tüm kod tabanında sadece beş yerde bakılıyor ve beşi de kimlik akışının içinde: `LoginHandler`, `GetSessionHandler`, `RequestPasswordReset`, `ResetPassword`, `ChangePassword`. Yani veriye dokunan **hiçbir** handler hesabın hâlâ aktif olup olmadığını sormuyor.

Jeton ömrü 480 dakika (8 saat, bilinçli bir takas olarak `JwtOptions`'ta gerekçelendirilmiş). Sonuç:

- **Pasife alınan hesap erişimini kaybetmiyor.** `DeactivateUserHandler`'ın yorumu — *"Giriş akışı `IsActive` kontrol ettiği için erişim anında kesilir"* — yanlış. Kesilen şey **yeni giriş**. Elinde çerezi/Bearer jetonu olan eski kullanıcı 8 saate kadar: `GET /api/reports/export` ile 2000 girişimin vergi numarası + iletişim + finansallarını CSV indirir, `GET /api/documents/{id}/download` ile sözleşmeleri çeker, SuperAdmin ise `/api/audit-logs`'tan maskelenmemiş tüm izi okur.
- **Rol düşürme de işlemiyor.** `UpdateUserHandler` `user.Role`'ü değiştiriyor ama eski jeton hâlâ `t3:role=SuperAdmin` taşıyor. Yetkisi alınan kişi 8 saat boyunca eski rolüyle yazmaya devam eder.
- **Program kapsamı donmuş.** `t3:programs` claim'i jetonda; `SyncProgramsAsync` bir programı kaldırsa da `StartupScope` eski listeye göre satır açar.
- **Çıkış jetonu geçersiz kılmıyor.** `/api/auth/logout` yalnızca çerezi siliyor. Çalınmış bir jeton çıkıştan sonra da geçerli — ve giriş yanıtının gövdesinde jetonun bir kopyası zaten dönüyor.

`GetSessionHandler`'ın veritabanından tazelemesi yalnızca **arayüzü** düzeltiyor: ekran kilitlenir, API açık kalır. Betik, `curl` ya da MCP istemcisi `/api/me`'yi hiç çağırmaz.

**Neden bu kadar önemli:** RBAC'ın tamamı jetonun içindeki üç claim'e dayanıyor. "Yetkiyi geri alma" bir RBAC sisteminin kurucu işlemidir; şu an sistemde bu işlem yok, yalnızca gecikmeli olarak var.

---

### G-02 · Üretimde şifre sıfırlama jetonu diskte düz metin

**Nerede:** `T3.Infrastructure/DependencyInjection.cs:44`, `T3.Infrastructure/Notifications/FileOutboxEmailSender.cs`

```csharp
services.AddScoped<IEmailSender, FileOutboxEmailSender>();
```

Kayıt **koşulsuz** — ortam kontrolü yok. Tohumlayıcı için doğru kurulan sınır (`IsDevelopment()`) burada kurulmamış. Sonuç: `Production` ortamında da her `POST /api/auth/forgot-password` isteği sunucu diskine bir dosya yazıyor:

```
storage/outbox/20260904-134501-ahmet_at_firma_test.txt
  → https://t3girisimportali.com/sifre-sifirla/<HAM_JETON>
```

`PasswordResetToken` tablosunda jetonun yalnızca SHA-256 özeti tutuluyor — bu doğru karar, ve gerekçesi (`"veritabanı okuyan biri hiçbir hesabın şifresini sıfırlayamamalı"`) sınıfın kendi yorumunda yazıyor. Ama **ham jetonun ikinci kopyası** aynı sunucunun diskinde, `api-storage` hacminde, düz metin olarak duruyor. Dosyalar için ne süre sınırı, ne sayı sınırı, ne temizlik işi var; dosya adında da e-posta adresinin tamamı geçiyor.

Bu dosyalara erişebilen herkes — hacmi okuyabilen bir yedekleme işi, aynı makinedeki başka bir konteynerin yanlış mount'u, sunucuya giren biri, `docker cp` yetkisi olan bir operatör — **iki dakikada SuperAdmin hesabını devralır**. Jeton 2 saat geçerli ve tek kullanımlık olması burada işe yaramıyor: dosya jeton tazeyken de orada.

**İkinci sonuç:** gerçek SMTP olmadığı için üretimde şifre kurtarma **fiilen çalışmıyor**. Kullanıcı sıfırlama isteyip e-posta beklerken, jeton sunucuda kalıyor. Pratikte tek kurtarma yolu yöneticinin şifre atamasına dönüyor — yani projenin `MustChangePassword` ile kapatmaya çalıştığı "hesabın şifresini bir başkası da biliyor" durumu geri geliyor.

---

### G-03 · Depodaki üretim yapılandırması TLS'i ve gerçek istemci IP'sini kapatıyor

**Nerede:** `docker-compose.prod.yml:38,45`, `.env.prod.example:19`, karşılaştırma: `README.md:343–363`

Canlı sunucu **doğru** kurulmuş. README'nin anlattığına göre `Hosting:RequireHttps=true` açık, güvenilen vekil docker ağ geçidine ayarlı, denetim izine gerçek istemci IP'si düşüyor, çerez `secure` bayraklı. Bunların hiçbiri depoda yok:

```yaml
# docker-compose.prod.yml — depodaki hâli
T3_Hosting__RequireHttps: "false"
T3_Hosting__TrustedProxies__0: ${TRUSTED_PROXY_IP:-127.0.0.1}
```

`TRUSTED_PROXY_IP` varsayılanı `127.0.0.1`, ama konteyner içinden bakıldığında host'taki nginx'ten gelen bağlantı **loopback değil docker köprü ağ geçidi** (tipik olarak `172.x.0.1`) görünür. Liste eşleşmediği için `UseForwardedHeaders` başlıkları reddeder ve üç şey birden bozulur:

1. `Request.IsHttps == false` → `SessionCookie.Issue` oturum çerezini **`Secure` bayrağı olmadan** yazar. Çerez, TLS'i olmayan herhangi bir istekte düz metin gider. `SessionCookie`'nin kendi yorumu bu bağı doğru anlatıyor (*"çerezin bayrağı isteğin şemasına bağlı"*) — sorun kodda değil, kodun beslendiği yapılandırmada.
2. `RequireHttps=false` olduğu için `UseHsts()` hiç çağrılmaz. HSTS tamamen vekile kalır; uygulama bunu doğrulamaz, yalnızca bir uyarı log'u düşer.
3. `HttpClientContext.IpAddress` her kullanıcı için **aynı** adresi (ağ geçidini) yazar. Denetim izindeki "nereden" alanı — KVKK'da işlem güvenliği kaydının parçası olarak beyan edilen alan — anlamsızlaşır; `AuthRateLimit`'in `ip|email` bölümlemesi de IP boyutunu kaybeder.

**Asıl sorun bu üçü değil, kayma.** Sertleştirmenin tek kopyası bir sunucuda, elle, kod dışında duruyor. Sunucu yeniden kurulursa, ikinci bir ortam açılırsa ya da devir sırasında vakıf kendi altyapısına taşırsa depodan çıkan yapılandırma **güvensiz olanı** kurar ve kimse fark etmez — çünkü uygulama açılır, çalışır, yalnızca çerez `Secure` değildir.

---

### G-04 · AI açıkken kişisel veri yurt dışına aktarılıyor; aydınlatma metni bunun tersini söylüyor

**Nerede:** `T3.Infrastructure/Ai/AnthropicChatModel.cs`, `T3.Application/Features/Assistant/AssistantToolbox.cs`, `frontend/src/features/legal/PrivacyNoticePage.tsx`

`T3_Ai__ApiKey` tanımlıyken asistan turu şunu yapıyor: araç sonuçlarını (`AssistantToolbox` → `get_startup_card`, `list_achievements`, …) JSON'a çevirip `https://api.anthropic.com/v1/messages` adresine POST ediyor. `AnthropicChatModel`'in kendi yorumu sınırı *"araçların döndürdüğü, zaten maskelenmiş veri"* diye tanımlıyor ve bu teknik olarak doğru — ama **maskelenmiş ≠ kişisel veri değil**. SuperAdmin ya da Program Yöneticisi sorduğunda giden yükün içinde:

- `ContactEmail`, `ContactPhone` (`StartupVisibility.ShowContactDetails` açık),
- ekip üyelerinin **ad soyadı, görevi, LinkedIn adresi** (`ShowTeamPersonalData` açık),
- SuperAdmin için `TaxNumber`.

Bunlar aydınlatma metninde `"Kimlik ve iletişim"` başlığı altında zaten kişisel veri olarak sayılıyor. Metnin **Aktarım** bölümü ise şunu diyor:

> "Kişisel veriler üçüncü taraflarla paylaşılmaz; **yurt dışına aktarılmaz.** Yapay zekâ destekli soru-cevap özelliği kullanıldığında yalnızca soruyu soran kullanıcının görme yetkisi olan kayıtlar model sağlayıcısına gönderilir…"

İki cümle birbirini çürütüyor: ilki aktarımı reddediyor, ikincisi tarif ediyor. Anahtar tanımlıyken **ilk cümle yanlış** ve KVKK m.9 kapsamında yurt dışına aktarım gerçekleşiyor:

- veri işleyen sözleşmesi yok,
- aktarım mekanizması yok (açık rıza / taahhütname / KVKK'nın onayladığı bir yol),
- veri işleme envanterinde aktarım kaydı yok,
- ilgili kişiye (ekip üyesi) aktarım hiç bildirilmemiş — kendisi sistemin kullanıcısı bile değil, verisi girişim tarafından girilmiş.

`docs/Loglama_Plani.md` içinde Sentry değerlendirilirken *"yurt dışı aktarım"* açık iş O-06 olarak not edilmiş — yani ekip bu kavramı biliyor, ama aynı testi zaten devrede olan AI aktarımına uygulamamış.

**Not:** Anahtar boş bırakıldığında (`DisabledChatModel` + `OfflineAssistant`) hiçbir veri dışarı çıkmıyor ve arayüz hangi modun yanıtladığını söylüyor. Yani mimari doğru kurulmuş; eksik olan hukuki katman ve metnin doğruluğu.

---

## 2. Orta önemli bulgular

### G-05 · Kaba kuvvet: hesap kilidi yok, IP başına toplam sınır yok, giriş zamanlamayla numaralandırılabiliyor

**Nerede:** `T3.Api/RateLimiting/AuthRateLimit.cs:63`, `T3.Application/Features/Auth/Login/LoginHandler.cs`

Bölüm anahtarı `$"{Ip}|{email}"`, pencere 1 dakika, izin 10. Bölümleme kararının gerekçesi doğru (bir hesaba yapılan saldırı herkesin girişini kilitlemesin), fakat karşı taraf boş kaldı: **e-posta başına** sınır var, **IP başına toplam** sınır yok. Tek bir IP'den 1.000 farklı e-posta ile dakikada 10.000 deneme yapılabilir — parola serpiştirme (password spraying) saldırısı pratikte sınırsız. `User` üzerinde başarısız deneme sayacı, hesap kilidi ya da artan gecikme yok; CAPTCHA yok.

`Auth.RateLimited` izi pencere başına **tek satır** yazıyor. Bu karar kendi başına doğru (saldırgan izi şişirip kendi izini boğmasın) ama yan etkisi şu: denetim izi saldırının **hacmini** hiç göstermiyor, yalnızca "bir kez kilit devreye girdi" diyor.

**Zamanlama yan kanalı.** `LoginHandler` üç durumda da aynı mesajı dönüyor — kullanıcı adı numaralandırmaya karşı doğru tasarım. Ama `user is null` dalında PBKDF2 **hiç çalıştırılmıyor**: kayıtlı olmayan adres onlarca milisaniye daha hızlı yanıt alır. Aynı asimetri `RequestPasswordResetHandler`'da daha da belirgin — gerçek kullanıcı için jeton üretimi + veritabanı yazımı + dosya yazımı yapılıyor, olmayan kullanıcı için hiçbiri. Özdeş mesaj, saatle yeniliyor.

---

### G-06 · `MustChangePassword` yalnızca arayüz kuralı

**Nerede:** `T3.Domain/Identity/User.cs:28` ve kullanım listesi

Bayrak beş yerde geçiyor: iki yerde `true` yapılıyor (`CreateUser`, `SetUserPassword`), iki yerde `false` (`ChangePassword`, `ResetPassword`), bir yerde yanıta konuyor (`SessionUserResponse`). **Hiçbir yerde bir işlemi engellemiyor.** Zorlama tamamen arayüzdeki rota koruyucusunda.

Yöneticinin attığı geçici şifreyle giriş yapan kullanıcı, şifresini hiç değiştirmeden `Authorization: Bearer` ile tüm REST yüzeyini, CSV aktarımını ve `/mcp` ucunu kullanabilir. `User.cs`'nin yorumundaki hedef — *"yöneticinin bildiği şifrenin kalıcı olmaması"* — sunucu tarafında sağlanmıyor. Bu bir yetki sınırı olarak tasarlanmış ama arayüz süsü olarak uygulanmış; G-02 ile birleşince (üretimde kurtarmanın tek yolu yönetici ataması) etkisi büyüyor.

---

### G-07 · Hız sınırı yalnızca iki uçta; kütlesel veri çekme yüzeyleri açık

**Nerede:** `Program.cs:213` (`AddRateLimiter`), `RequireRateLimiting` kullanımı yalnızca `AuthEndpoints` ve `AssistantEndpoints`'te

Sınırsız kalan uçlar, sızma değeri sırasına göre:

| Uç | Tek istekte ne çıkar |
|---|---|
| `GET /api/reports/export` | 2000 girişim × vergi no + iletişim + tüm finansallar (rolün gördüğü kadarı) |
| `GET /api/startups` | Sayfa başına 100 kayıt, sayfa sınırı yok |
| `GET /api/documents/{id}/download` | Sözleşme, mali tablo, patent dosyası — dosya başına bir istek |
| `POST /mcp` | Yukarıdakilerin hepsi, program döngüsüyle |
| `GET /api/reports/ecosystem` | Ağır agregasyon sorgusu (DoS yüzeyi) |

`options.GlobalLimiter` hiç kullanılmamış. Ele geçirilmiş tek bir Program Yöneticisi jetonu, kapsamındaki her şeyi bir `for` döngüsüyle dakikalar içinde dışarı taşır. Her indirme denetim izine bir satır yazıyor — bu iyi — ama **hiçbir şey yavaşlatmıyor ve hiçbir şey uyarmıyor**. `Report.Export` izi süzgeci kaydediyor, hangi girişim satırlarının çıktığını kaydetmiyor; sızıntı sonrası "ne gitti" sorusu cevaplanamaz.

`ExportStartupsHandler`'daki `MaxRows = 2000` üst sınırı bilinçli konmuş ve doğru, ama istek **sayısını** sınırlamıyor.

---

### G-08 · Yükleme boyutu uygulama sınırından önce sunucuda sınırlanmıyor; içerik doğrulaması uzantıya bakıyor

**Nerede:** `T3.Api/Endpoints/DocumentEndpoints.cs:29`, `T3.Application/Features/Documents/DocumentUploadRules.cs:16`

Kod tabanında `RequestSizeLimit`, `MultipartBodyLengthLimit` ya da `MaxRequestBodySize` **hiç geçmiyor**. Sıra şöyle işliyor: ASP.NET multipart gövdeyi tamamen ayrıştırıp diske/belleğe alıyor → `file.Length` okunuyor → **sonra** `DocumentUploadRules.Check` 20 MB kuralını uyguluyor. Yani 20 MB kuralı, dosya zaten sunucuya yazıldıktan sonra devreye giren bir kural. Gerçek kapı nginx'teki `client_max_body_size 25m` ve o değer **depoda değil, sunucuda** (yine G-03'teki kayma).

İçerik doğrulaması yalnızca uzantı tablosuna dayanıyor. Sınıfın yorumu istemcinin `Content-Type` başlığına güvenmemeyi doğru gerekçelendiriyor ve tipi uzantıdan türetiyor — ama **dosyanın içeriğine hiç bakılmıyor**: sihirli bayt (magic byte) kontrolü yok, kötücül yazılım taraması yok. `.pdf` uzantılı bir dosya herhangi bir şey olabilir. İndirmede `nosniff` + `Content-Disposition: attachment` tarayıcıda çalışmasını engelliyor (bu doğru kurulmuş), ama sistem yine de "keyfi ikili dosyaları saklayıp yetkili kullanıcılara dağıtan" bir kanal.

---

### G-09 · Denetim izinin bütünlüğü ve iş değişikliğiyle atomikliği garanti değil

**Nerede:** `T3.Infrastructure/Audit/AuditWriter.cs:45`, `T3.Application/Common/Interfaces/IAppDbContext.cs`

**Atomiklik.** `AuditWriter.WriteForActorAsync` kendi içinde `db.SaveChangesAsync(ct)` çağırıyor ve her handler'da bu çağrı, handler'ın kendi `SaveChangesAsync`'inden **sonra** geliyor. İkisini kapsayan bir işlem (transaction) yok. Denetim satırının yazımı başarısız olursa — veya süreç arada düşerse — veri değişikliği **zaten kalıcı** ve izi yok. `ApproveChangeRequestHandler` "onay ile uygulama tek `SaveChanges`'ta kalıcı olur" garantisini doğru kuruyor ama denetim satırlarını o işlemin dışında bırakıyor. Projenin en güçlü iddiası ("her değişiklik izlenebilir") en zayıf halkasında garantisiz.

**Bütünlük.** `AuditLogs` sıradan, yazılabilir bir tablo ve `IAppDbContext` üzerinden tüm Application katmanına açık. Ekleme-dışı yetki kısıtı (grant), hash zinciri, ayrı WORM/yalnızca-ekleme kopyası yok. SuperAdmin ya da veritabanına erişen biri geçmişi sessizce yeniden yazabilir. KVKK/ISO tarzı bir "işlem güvenliği kaydı"nın taşıması gereken özellik kurcalamaya karşı **kanıtlanabilirlik**; şu an iz yalnızca *var*, korunmuyor.

**Saklama.** `AuditLogs` ve `PasswordResetTokens` için silme/anonimleştirme işi yok. Her ikisi de IP adresi, tarayıcı bilgisi ve maskelenmemiş varlık anlık görüntüleri (`StartupAuditSnapshot` — vergi no ve iletişim dahil, ve bu bilinçli) tutuyor. Süresiz büyüyen bir kişisel veri deposu.

---

### G-10 · Onaylayan, göremediği alanı onaylıyor

**Nerede:** `T3.Application/Features/Approvals/GetChangeRequest/GetChangeRequestHandler.cs:36` ile `ChangeRequestApplier.cs:104` karşılaştırması

Karar ekranı diff'i maskeliyor — doğru, ve gerekçesi handler'ın yorumunda açıkça yazıyor (*"kartta vergi numarasını göremeyen Program Yöneticisi, aynı alanı diff satırında okuyamamalı"*). Uygulama tarafı ise maskesiz yazıyor:

```csharp
// ChangeRequestApplier.ApplyStartupAsync
model.ApplyTo(startup);          // görünürlük almayan aşırı yükleme
```

Veri bütünlüğü açısından bu doğru: öneriyi gönderen girişim kullanıcısı kendi verisinin tamamını görüyor, gövdesi eksiksiz. Ama hesap verebilirlik açısından bir boşluk: `TaxNumber`'ı **göremeyen** bir Program Yöneticisi, o alanı değiştiren bir öneriyi onaylayabiliyor ve denetim izi "inceledi ve onayladı" diyor. İnceleyicinin hiç görmediği bir değişikliğe imza attığı bir onay akışı, onay akışı değil.

---

## 3. Düşük önemli bulgular ve sertleştirme

| # | Bulgu | Yer | Not |
|---|---|---|---|
| **G-11** | CSRF çift-gönderim jetonu oturuma bağlı değil (HMAC yok), `__Host-` öneki kullanılmıyor | `Security/SessionCookie.cs:64` | Alt alan adından çerez yazabilen saldırgan hem çerezi hem başlığı kendisi koyar. `SameSite=Strict` bugün kapatıyor — ama sınıfın kendi yorumu bunu tek dayanak yapmama gerekçesini zaten yazmış |
| **G-12** | `/health/db` anonim ve **bekleyen migration adlarını** dönüyor; `AllowedHosts: "*"` | `Endpoints/HealthEndpoints.cs:29`, `appsettings.json` | Şema/sürüm bilgisi keşif değeri taşır. Sağlık ucu "ok/degraded" ile yetinmeli |
| **G-13** | `ResolveInsideRoot` ayırıcısız `StartsWith(_root)` kullanıyor | `Storage/LocalDocumentStorage.cs:63` | `…/documents-yedek` gibi kardeş klasör kontrolü geçer. Yol sunucu üretimli olduğu için bugün sömürülemez; **derinlemesine savunma kırık** — S3 sürümüne geçilirken ya da yol bir gün girdiye bağlanırsa doğrudan açık olur |
| **G-14** | `SeedOptions.Enabled` varsayılanı `true`, `Password` sabit `"T3.Creathon!2026"` | `Persistence/Seed/SeedOptions.cs` | Ortam kontrolü tutuyor ve `Program.cs` yanlış yapılandırmada `LogError` düşürüyor — bu iyi. Ama **varsayılanın yönü ters**: güvenlik varsayılanları kapalı başlar |
| **G-15** | PBKDF2 (ASP.NET varsayılanı) iterasyonu açıkça verilmemiş; `SuccessRehashNeeded` yutuluyor | `Identity/Pbkdf2PasswordHasher.cs:18` | 2026 için alt sınırda. Ayrıca rehash sinyali `Verify` içinde `or` ile eziliyor → parametreler yükseltilse bile eski hash'ler hiç güncellenmez |
| **G-16** | Paketler `8.0.10`'da sabit (Ekim 2024); **.NET 8 desteği Kasım 2026'da bitiyor**; CI yok | `*.csproj`, `.github` yok | `dotnet list package --vulnerable` ve `npm audit` hiçbir yerde koşmuyor. Devir tarihi ile destek bitişi çakışıyor — .NET 10 LTS'e geçiş planı bugünden gerekli |
| **G-17** | Dev yığını sertleştirilmemiş: postgres `0.0.0.0:5433`'e yayımlı, pgAdmin `SERVER_MODE=False` (giriş ekranı **kapalı**) + `.env`'de `PGADMIN_PASSWORD="admin"`, imaj `latest` | `docker-compose.yml` | Bu dosya bir sunucuda çalıştırılırsa kimlik doğrulaması olmayan bir veritabanı yöneticisi ağa açılır. Konteynerlerde `cap_drop`, `read_only`, kaynak limiti ve api healthcheck'i yok; DB bağlantı dizesinde `SSL Mode` verilmemiş |
| **G-18** | Yedekleme/geri yükleme prosedürü yok; dokümanlar ve DB diskte şifresiz; `TaxNumber` kolon şifrelemesi yok | dağıtım | KVKK "uygun güvenlik düzeyi" hem gizlilik hem **erişilebilirlik** ister. Şu an tek bir disk kaybı tüm kurumsal hafızayı silir |
| **G-19** | Aydınlatma metni üç yerde gerçeği yansıtmıyor | `features/legal/PrivacyNoticePage.tsx` | (a) *"erişim jetonu tarayıcınızın yerel deposunda tutulur"* — artık `HttpOnly` çerez, Dalga 2'de değişti, metin güncellenmedi; (b) zorunlu çerezler (`t3.session`, `t3.csrf`) hiç beyan edilmemiş; (c) denetim izi için saklama süresi verilmemiş (yalnızca "yasal süreler") — G-09'daki süresiz birikimle örtüşüyor. Ayrıca G-04'teki aktarım cümlesi |
| **G-20** | Prompt injection: girişim kullanıcısının `ProductDescription`'ına yazdığı metin araç sonucu olarak modele gidiyor | `AssistantToolbox` → `AnthropicChatModel` | 4000 karakter serbest metin, girişimin kendi kontrolünde. Karar Verici'nin sorusuna verilen yanıtı yönlendirebilir. Araçlar salt-okunur olduğu için etki **veri sızması değil karar bütünlüğü**; yanıtta kaynak gösterimi kısmi azaltma, sistem yönergesinde araç verisini "veri, talimat değil" olarak çerçeveleyen bir cümle yok |
| **G-21** | `DisableAntiforgery()` yanındaki yorum artık yanlış | `Endpoints/DocumentEndpoints.cs:47` | *"API çerezle değil Bearer jetonuyla kimlik doğruluyor, dolayısıyla CSRF yüzeyi yok"* — Dalga 2'den beri tarayıcı **çerezle** kimlik doğruluyor. Uç bugün korunuyor, ama koruyan şey `CsrfProtection` ara katmanı, yorumun söylediği şey değil. Yanıltıcı yorum, bir sonraki geliştiricinin ara katmanı gereksiz sanıp kaldırmasına davet |

---

## 4. İyileştirme planı

Fazlar **risk × efor** sırasına göre, her iş için kabul kriteri var. Kabul kriterleri projenin mevcut doğrulama alışkanlığına bağlanıyor: birim testi (`T3.Application.Tests`), gerçek API'ye tüm rollerle vuran Python E2E betiği (`scripts/e2e_faz*.py`), headless render.

### Faz 0 — Demo öncesi, bu akşam (yapılandırma ve metin; kod riski yok)

> **Durum: tamamlandı (4 Eylül 2026).** Dördü de kapandı. Ayrıntı ve
> reddedilen alternatifler için bkz.
> [Gelistirme_Kararlari.md §3l](Gelistirme_Kararlari.md). Doğrulama:
> `dotnet build && dotnet test` (210 birim testi) yeşil; E2E/render betikleri
> bu turda koşulmadı (bkz. not, dosyanın sonu).

| İş | Ne yapılacak | Kabul kriteri |
|---|---|---|
| **G-03** | `docker-compose.prod.yml`'de `T3_Hosting__RequireHttps: "true"`, `TRUSTED_PROXY_IP` varsayılanını kaldır (`:?` ile **zorunlu** yap). `.env.prod.example`'a docker ağ geçidini nasıl bulacağını yaz (`docker network inspect`). Canlı sunucudaki elle yapılmış ayarları depoya geri yaz | `docker compose -f docker-compose.prod.yml config` çıktısı canlı `.env` ile birebir; yeni kurulan bir konteynerde `Set-Cookie` başlığında `secure` ve `Strict-Transport-Security` görünüyor; denetim izinde iki farklı istemciden iki farklı IP |
| **G-02** | `IEmailSender` kaydını ortama bağla: `Production`'da `FileOutboxEmailSender` **kaydedilmesin**. Gerçek SMTP yoksa `ThrowingEmailSender` (açılışta `LogError`, çağrıda hata) kaydet — sessiz düz metin yazmaktan iyi. Mevcut `storage/outbox/*` dosyalarını sunucudan **sil** | `Production` ortamında `POST /api/auth/forgot-password` sonrası `storage/outbox` boş; açılış log'unda "SMTP yapılandırılmadı" uyarısı; `ls /app/storage/outbox` sıfır dosya |
| **G-19 + G-04 (metin)** | Aydınlatma metnini düzelt: jeton cümlesini `HttpOnly` çereze güncelle, `t3.session`/`t3.csrf` zorunlu çerezlerini beyan et, denetim izi saklama süresini yaz, **Aktarım** bölümünü AI aktarımını dürüstçe anlatacak şekilde yeniden yaz. `KVKK_ONAY_SURUMU`'nu artır (metin değişti → onay yenilenmeli) | `PrivacyNoticePage` metni kodla çelişmiyor (satır satır kontrol); `KVKK_ONAY_SURUMU` yeni; giriş ekranı onayı yeniden soruyor; `Auth.KvkkConsent` izinde yeni sürüm |
| **G-14** | `SeedOptions.Enabled` varsayılanını `false` yap, `Password` sabitini kaldır (yapılandırma yoksa tohumlama hata versin) | `appsettings.Development.json`'daki `Seed:Enabled=true` ile geliştirmede hâlâ tohumlanıyor; değer kaldırıldığında tohumlanmıyor |

### Faz 1 — İlk iki hafta (kimlik ve erişim sınırları)

> **Durum: tamamlandı (4 Eylül 2026), bir madde kısmi.** G-01 yenileme jetonu
> hariç tam uygulandı (bilinçli kısayol: erişim jetonu 60 dakika + istek
> başına `IsActive`/`Role`/`SecurityStamp` kontrolü — bkz. Gelistirme_Kararlari.md
> §3l). G-21'in E2E maddesi (CSRF multipart vakası) eklenmedi, kod düzeltmesi
> yapıldı. Diğer beşi (G-05, G-06, G-08, G-12, G-13) tam. Doğrulama: 210 birim
> testi (yeni: `Pbkdf2PasswordHasherTests`, `LocalDocumentStorageTests`,
> `DocumentUploadRulesTests` imza vakaları, `AiRedactionTests`).

| İş | Ne yapılacak | Kabul kriteri |
|---|---|---|
| **G-01** | Jeton ömrünü 480 → 60 dakikaya indir + `HttpOnly` çerezde **yenileme jetonu** getir (`JwtOptions` yorumunun zaten işaret ettiği doğru çözüm). Buna ek olarak istek başına doğrulama: bir `IUserStateProvider` (kullanıcı başına 30–60 sn önbellekli) `IsActive` + `Role` + `SecurityStamp` denetimi yapsın; jetondaki damga veritabanındakiyle uyuşmazsa 401. `Deactivate`/`UpdateUser`/`SetUserPassword`/`ChangePassword` damgayı döndürsün | E2E betiğine yeni senaryo: giriş → jetonu sakla → hesabı pasife al → **aynı jetonla** `/api/reports/export` 401 dönüyor. Aynı senaryo rol düşürme ve program kaldırma için de. `logout` sonrası eski jeton 401 |
| **G-05** | `AuthRateLimit`'e ikinci, **yalnızca IP'ye** bağlı kova ekle (ör. 30/dk) — mevcut `ip\|email` kovası kalsın (zincirlenmiş sınırlayıcı). `User`'a `FailedLoginCount` + `LockedUntil`: 10 başarısızlıkta 15 dk kilit, başarılı girişte sıfırla. `LoginHandler`'da kullanıcı bulunamadığında da **sahte PBKDF2 doğrulaması** çalıştır (sabit süre); `RequestPasswordReset`'te aynı dengeleme | E2E: 40 farklı e-posta ile tek IP'den deneme → 30'uncudan sonra 429. Aynı hesaba 10 yanlış şifre → 11'incide kilit mesajı. Kayıtlı/kayıtsız e-posta yanıt süreleri 100 örnekte istatistiksel olarak ayırt edilemez |
| **G-06** | `MustChangePassword` bir **sunucu sınırı** olsun: bayrak açıkken izin verilen uçlar yalnızca `/api/me`, `/api/auth/change-password`, `/api/auth/logout`; geri kalanı 403 (`"Devam etmek için şifrenizi değiştirmelisiniz."`). Uç nokta filtresi olarak yaz — politika hattı MCP'de atlanabiliyor, filtre handler'a girmeden çalışır | E2E: yönetici şifre atar → kullanıcı Bearer ile `/api/startups` çağırır → 403; `change-password` sonrası aynı çağrı 200 |
| **G-08** | Yükleme ucuna `.WithMetadata(new RequestSizeLimitAttribute(21_000_000))` ve `MultipartBodyLengthLimit` ekle; `client_max_body_size` değerini depoya (ops/nginx örneği) taşı. `DocumentUploadRules`'a sihirli bayt kontrolü ekle (PDF `%PDF`, ZIP tabanlı Office `PK`, PNG/JPEG imzaları) — uzantı ile içerik uyuşmazsa reddet | E2E: 30 MB dosya → uygulamaya varmadan 413; `.pdf` adlı HTML dosyası → 400 "dosya içeriği uzantıyla uyuşmuyor"; `DocumentUploadRulesTests`'e imza vakaları |
| **G-12** | `/health/db`'yi bekleyen migration adlarını dönmeyecek şekilde daralt (`pendingMigrationCount`); `AllowedHosts`'u üretimde `PUBLIC_BASE_URL` alan adına bağla | Anonim `/health/db` yanıtında migration adı yok; bilinmeyen `Host` başlığıyla gelen istek 400 |
| **G-13** | `ResolveInsideRoot`'ta kök yolun sonuna `Path.DirectorySeparatorChar` ekleyerek karşılaştır; `Path.GetRelativePath` ile `..` içermediğini de doğrula. Birim testi ekle | `LocalDocumentStorageTests`: `"../gizli.pdf"`, `"/etc/passwd"` ve kardeş klasör vakaları `UnauthorizedAccessException` atıyor |
| **G-21** | Yanıltıcı yorumu düzelt; `CsrfProtection`'ın çok-parçalı (multipart) yükleme yolunu da kapsadığını gösteren bir E2E vakası ekle | Kod incelemesi + E2E: CSRF başlığı olmadan çerezle dosya yükleme 403 |

### Faz 2 — İlk ay (veri koruma, KVKK, gözlemleme)

> **Durum: 4 Eylül 2026'da büyük ölçüde tamamlandı.** G-04 ((a) redaksiyon),
> G-07, G-10, G-16 (ilk adım) tam. G-09 ve G-11 kısmi — atomiklik bu turda
> dokunulan kimlik/şifre yollarına uygulandı, geri kalan ~30 handler ve DB
> grant/hash zinciri ertelendi; CSRF oturuma bağlandı ama `__Host-` öneki
> eklenmedi. G-15 tam. Gerekçeler için bkz.
> [Gelistirme_Kararlari.md §3l](Gelistirme_Kararlari.md).

| İş | Ne yapılacak | Kabul kriteri |
|---|---|---|
| **G-04** | Karar ver ve **belgele**: (a) modele giden araç sonuçlarından kişisel veriyi çıkar (ad/e-posta/telefon/vergi no yerine yer tutucu; model zaten sayı ve isim uydurmaması için yönlendiriliyor), **ya da** (b) yurt dışı aktarımı hukuki olarak kur (veri işleyen sözleşmesi, aktarım mekanizması, envanter kaydı, aydınlatma metni, ilgili kişiye bildirim) ve AI'ı kurum kararına bağlı bir anahtarla aç. (a) tercih edilirse `AssistantToolbox`'a `AiRedaction` katmanı — REST maskelemesinden **ayrı** ve daha sıkı | Seçilen yol `docs/Gelistirme_Kararlari.md`'ye reddedilen alternatifle birlikte yazılmış. (a) için: `AssistantToolboxTests`'te giden yükün ad/e-posta/telefon/vergi no içermediği doğrulanıyor. (b) için: sözleşme + envanter kaydı dosyada |
| **G-07** | `options.GlobalLimiter` ile kullanıcı başına genel kova (ör. 300/dk); `export`, `download` ve `/mcp` için ayrı ve daha sıkı kovalar (ör. 10 aktarım/saat). `Report.Export` izine dönen satır kimliklerinin özetini ekle. Eşik aşımında `Security.MassExport` izi + uyarı | E2E: 11'inci CSV aktarımı 429; `/mcp` döngüsü sınıra takılıyor; `Report.Export` izinde satır kimlikleri |
| **G-09** | `AuditWriter`'a `SaveChanges` çağırmayan bir kip ekle; handler'lar iş değişikliği + iz satırını **tek** `SaveChanges`'ta yazsın (`ApproveChangeRequestHandler`'ın zaten uyguladığı kalıp). Veritabanı düzeyinde `AuditLogs` için `INSERT`-dışı yetkiyi kaldır (uygulama kullanıcısına `UPDATE`/`DELETE` verilmez). Satır başına önceki satırın hash'ini taşıyan zincir alanı. `AuditLogs` ve `PasswordResetTokens` için saklama süresi + temizlik işi | Süreç iz yazımı sırasında öldürüldüğünde veri değişikliği de geri alınıyor (entegrasyon testi). `UPDATE audit_logs` uygulama kullanıcısıyla hata veriyor. Zincir doğrulama betiği `scripts/` altında. Süresi geçen `PasswordResetTokens` satırları siliniyor |
| **G-10** | Öneri maskeli bir alanı değiştiriyorsa onay ekranı bunu **açıkça** göstersin (`"Bu öneri görme yetkiniz olmayan 1 alanı değiştiriyor"`) ve bu tür öneriler SuperAdmin onayına yükselsin. Alternatif: uygulamada da `ApplyTo(startup, visibility)` kullanıp gizli alan değişikliklerini reddet | `ChangeRequestDiffTests`: vergi no değişikliği içeren öneri, Program Yöneticisi için `RequiresElevation=true`; E2E: Program Yöneticisi onay denemesi 403, SuperAdmin 200 |
| **G-11** | CSRF jetonunu oturuma bağla: değer `HMAC(sunucu anahtarı, oturum kimliği)` olsun; çerezleri `__Host-` önekiyle yaz (Path=/, Secure, alan adı yok) | Başka bir oturumun CSRF jetonuyla yapılan yazma isteği 403; çerez adları `__Host-` önekli; `SameSite` kapalıyken bile koruma çalışıyor (test) |
| **G-15** | PBKDF2 iterasyonunu açıkça yükselt (`PasswordHasherOptions`) ya da Argon2id'ye geç; `SuccessRehashNeeded` durumunda hash'i yeniden yaz | `Pbkdf2PasswordHasherTests`: eski parametrelerle üretilmiş hash başarılı doğrulamada güncelleniyor |
| **G-20** | Sistem yönergesine araç verisinin **veri** olduğunu, içindeki talimatların yok sayılacağını söyleyen bir kural ekle; araç sonuçlarında serbest metin alanlarını sınırla | `AssistantToolboxTests`: `ProductDescription`'a gömülü talimat modeli araç çağrısı yapmaya/rol dışı davranmaya yönlendirmiyor (sabit yanıt testi) |
| **G-16 (ilk adım)** | GitHub Actions: `dotnet build` + `dotnet test` + `dotnet list package --vulnerable --include-transitive` + `npm audit --audit-level=high`. Kırılan derleme birleştirmeyi engellesin | Boş bir PR'da işlem hattı yeşil; kasıtlı zafiyetli paket eklendiğinde kırmızı |

### Faz 3 — Kuruma devir öncesi

> **Durum: G-17 tamamlandı (4 Eylül 2026), gerisi başlanmadı.** G-16 (.NET 10
> geçişi), G-18 (yedekleme/şifreleme) ve bağımsız sızma testi/VERBİS
> incelemesi bilinçli olarak bu turda ele alınmadı — bunlar altyapı/organizasyon
> kararları, bir kod değişikliği turunda güvenle kapatılamaz.

| İş | Ne yapılacak | Kabul kriteri |
|---|---|---|
| **G-16** | .NET 10 LTS'e geçiş (destek Kasım 2026'da bitiyor); paketleri güncelle; `Directory.Packages.props` ile merkezî sürüm yönetimi | `net10.0` hedefiyle tüm testler ve E2E geçiyor |
| **G-18** | `pg_dump` + doküman hacmi için zamanlanmış yedek, **şifreli** ve makine dışı; belgelenmiş geri yükleme tatbikatı; `TaxNumber` ve doküman deposu için şifreleme (kolon şifrelemesi ya da disk şifrelemesi) | Yedekten sıfırdan geri yükleme tatbikatı belgelenmiş ve bir kez yapılmış; hacim şifreli |
| **G-17** | ✅ Dev yığını sertleştirildi: postgres portu `127.0.0.1`'e bağlı, pgAdmin `SERVER_MODE=True` + zorunlu parola + `127.0.0.1`, imaj etiketleri sabit, `cap_drop: [ALL]` + `mem_limit` tüm servislerde. `read_only` ve `SSL Mode=Require` bilinçli olarak eklenmedi (bkz. Gelistirme_Kararlari.md §3l) | `docker compose --profile tools --profile observability config` temiz |
| — | Bağımsız sızma testi + KVKK VERBİS/envanter gözden geçirmesi; olay müdahale planı (ihlal bildirimi 72 saat); `.claude/agents/rbac-kvkk-denetcisi.md` kontrol listesine bu dokümandaki G-numaralarını ekle | Rapor + kapatılan bulgu listesi |

---

## 5. Jüri soru-cevabı için hazırlık (5–6 Eylül)

Bu bulguların bir kısmı demoya kadar kapanmayacak. Kapanmayanı saklamak yerine **çerçeveleyin** — soru gelirse hazır cevap, gelmezse sunumda bir cümle:

- *"Yetkiyi geri alma neden anlık değil?"* → "Jeton ömrü bir iş günü, bu bilinçli bir takas: yenileme jetonu tek origin kararına bağlıydı ve o karar Dalga 2'de verildi. Şimdi sırada güvenlik damgası var; pasife alma anlık kesecek. Şu an gerçek: yeni giriş anında kesiliyor, mevcut oturum en fazla 8 saat sürüyor." (Faz 1'de kapanıyor.)
- *"AI'a veri gidiyor mu?"* → "Anahtar tanımlıysa evet, ve **yalnızca soranın kendi yetkisiyle görebildiği** kayıtlar gidiyor — araçlar REST'in aynı handler'larını sarıyor, paralel veri yolu yok. Ama bu bir yurt dışı aktarımdır ve aydınlatma metnimizi bu akşam bunu doğru anlatacak şekilde düzelttik. Anahtar boşken sistem tamamen yerel çalışıyor ve arayüz hangi modun yanıtladığını söylüyor."
- *"Sızıntı olsa ne gitti diye cevap verebilir misiniz?"* → "İz her indirmeyi ve her aktarımı kaydediyor; aktarımda hangi satırların çıktığını kaydetmek Faz 2'de. Bunu bir eksik olarak biliyoruz ve planda numarası var."
- *"Log KVKK deliği açmıyor mu?"* → burada güçlüsünüz: `KisiselVeriMaskesi` + `MaskedEmail` + istek log'unda e-posta yerine `KullaniciId` + 14 gün saklama. Bunu anlatın.

Sunumda gösterilecek en güçlü üç şey: **maskeleme yazma yolunda da tutuluyor** (`StartupWriteModel.ApplyTo` — Program Yöneticisi'nin kaydettiği düzenleme vergi numarasını silmiyor), **MCP ayrı veri yolu değil** (`AssistantToolbox` aynı handler'ları sarıyor, ajan kendi yetkisi kadar görüyor), **kapsam dışı kayıt 403 değil 404 dönüyor** (403 kaydın varlığını sızdırırdı).

---

## 6. Denetim kapsamı ve sınırları

**İncelenen:** tüm Application/Api/Infrastructure/Domain kaynak dosyaları; kimlik, oturum, CSRF, CSP, hız sınırı, RBAC ve maskeleme yolları; doküman yükleme/indirme ve depolama; CSV aktarımı; MCP ve AI sınırı; denetim izi; EF yapılandırması ve göçler; Docker/compose/`.env` yapılandırması; arayüzde jeton taşıma ve XSS yüzeyi; git geçmişinde sır taraması (**temiz** — `.env` hiçbir commit'te yok, izlenen tek eşleşme `PasswordResetSecrets.cs` kaynak dosyası); dokümanlar.

**Yapılmayan:** canlı sisteme sızma testi, bağımlılık ağacında CVE taraması (`dotnet list package --vulnerable` koşulmadı), veritabanı yetki denetimi, sunucu üzerinde işletim sistemi/nginx sertleştirme denetimi, kaynak kodu dinamik analizi. Bunların ilk üçü Faz 2–3'te planda; sızma testi Faz 3'te.

**Kod tabanına dair genel not:** yorumlar "ne yaptığını" değil "neden böyle" olduğunu anlatıyor ve bu denetimi hızlandırdı — birkaç bulgu doğrudan bir yorumun kendi gerekçesiyle çelişmesinden çıktı (G-01'de `DeactivateUserHandler`, G-21'de `DisableAntiforgery`). Yorum disiplini bir güvenlik özelliği; korunmalı. Buna karşılık aynı disiplin bir risk de taşıyor: **yorum kodla birlikte güncellenmezse yanlış güvence verir.** G-21 tam olarak bu.
