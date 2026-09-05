# Loglama Planı — uygulama günlüğü, korelasyon ve KVKK

> **Durum: uygulandı** (4 Eylül 2026, planla aynı gün — Demo Day beklenmeden).
> Yerleşik kararlar [Gelistirme_Kararlari.md §3k](Gelistirme_Kararlari.md)
> içine taşındı; bu dosya artık yalnızca gerekçe arşivi. Aşağıdaki paket
> sürümleri ve yapılandırma blokları planlama sırasında doğrulanmamıştı — canlı
> koşuda birkaç fark çıktı, hepsi düzeltildi:
>
> - `Serilog.Sinks.Grafana.Loki` 9.0.2, `Serilog` 4.3.1'i (planın öngördüğü gibi)
>   otomatik çekti.
> - `Serilog:WriteTo` dizisi Development'ta **tam** yeniden yazıldı — .NET
>   yapılandırma sağlayıcıları diziyi indekse göre birleştiriyor, tek eleman
>   eklemek base'teki Console/File'ı silerdi.
> - `ExceptionHandlingMiddleware`'deki `Response.Clear()` `X-Request-Id`
>   başlığını da siliyordu; canlı 500 testiyle yakalandı, başlık `Clear()`
>   sonrası yeniden yazılarak düzeltildi.
>
> Ayrıntı: [Gelistirme_Kararlari.md §3k ve "Loglama" tuzakları](Gelistirme_Kararlari.md).

## 1. Neden

Bugün backend'de yalnızca `Microsoft.Extensions.Logging` var ve tek hedefi
konsol. Sonuç:

- Kullanıcı "hata aldım" diyor; elde `docker logs` içinde zaman damgasına göre
  arama yapmaktan başka yol yok. İsteği ekrana bağlayan bir kimlik yok.
- `ExceptionHandlingMiddleware` hatayı doğru yakalıyor ama istemciye dönen
  gövdede (`ApiErrorBody`) hiçbir referans yok.
- Log yapısal değil: `LogWarning(ex, "Doğrulama hatası: {Path}", …)` satırları
  konsolda düz metin. Rol, kullanıcı, süre, sonuç kodu gibi alanlarda arama yok.
- Konteyner yeniden başlatıldığında log gider; saklama ve döndürme politikası
  yok.

## 2. Üç ayrı kavram — sınırları korunur

Bu projede kolayca karıştırılabilecek üç şey var. Aynı depoya konmaları
**mimari hata** olur:

| | Bugün | Neyi cevaplar | Nerede durur |
|---|---|---|---|
| **Denetim izi** (`AuditLog`) | ✅ var | "Kim neyi değiştirdi" | Postgres, rol kapılı (`ViewAuditLogs`), maskeli, yalnızca eklenir |
| **Uygulama günlüğü** | ❌ yok | "Bu istekte ne oldu" | Bu planın konusu |
| **Hata izleme / uyarı** | ❌ yok | "Hangi hata yeni, kaç kullanıcıyı vurdu" | Açık iş O-06, bkz. §9 |

**`AuditLog` hata logu olarak kullanılmaz.** Üç gerekçe, her biri tek başına
yeterli:

1. Hukuki kapsamı belli bir KVKK kaydı; teşhis gürültüsü oraya karışırsa kayıt
   "değiştirilemez iz" olma niteliğini kaybeder.
2. `ViewAuditLogs` yalnızca Süper Yönetici'ye açık. Hata ayıklama ihtiyacı olan
   kişi ile denetim izini görmesi gereken kişi aynı değil.
3. Teşhis etmek istediğin veritabanının **içinde** durur. Postgres düşerse
   düşme sebebini yazan kayıt da düşer.

Aynı gerekçenin tersi de doğru: uygulama günlüğü denetim izinin yerine geçmez —
log döner ve silinir, iz dönmez.

## 3. Karar: Serilog + Grafana Loki

**Serilog** kurulur; sink **değiştirilebilir son adım** olarak kalır. Bu, depoda
zaten uygulanan kalıbın aynısı: `IEmailSender` arkasında sağlayıcı değişir,
`IDocumentStorage` arkasında S3 bekler.

Sink olarak **Grafana Loki** seçildi; sorgu arayüzü **Grafana**.

| | Loki + Grafana | Seq |
|---|---|---|
| Lisans | AGPLv3 — ücretsiz, **kullanıcı sınırı yok** | Ücretsiz Individual **tek kişilik**; paylaşılan kurulum Team, 790 $/yıl |
| Kuruma devir | Sorunsuz — vakıf istediği kadar kişiye açar | Devirde ilk gün lisans satın alma kararı çıkar |
| Konteyner | 2 (loki + grafana) | 1 |
| Sorgu dili | LogQL (öğrenilecek) | Seq sorgu dili (ekip biliyor) |

Lisans farkı belirleyici oldu: bu sistem T3 Vakfı'na devredilecek ve devredilen
bir sistemin gözlemleme katmanının "tek kişi bakabilir" olması kabul edilebilir
değil. Öğrenme maliyeti tek seferlik, lisans maliyeti yıllık.

### Topoloji — ajan yok

```
T3.Api ──Serilog.Sinks.Grafana.Loki──▶ Loki ◀──sorgu── Grafana ──▶ tarayıcı
         (uygulamadan doğrudan HTTP push)
```

**Promtail ya da Grafana Alloy kurulmuyor.** Bu ajanlar dosyadan/konteynerden log
*toplamak* için var; Serilog Loki'ye doğrudan HTTP ile yazdığı için aradaki
adım gereksiz. Ayrıca Promtail Loki 3.4 ile Alloy'a birleştirildi ve
kullanımdan kaldırıldı — kurulmayan bileşen, sonradan göç ettirilmesi
gerekmeyen bileşendir.

Konsol ve dönen JSON dosya sink'leri **kalır**: Loki düştüğünde log kaybolmasın
ve `docker logs` ilk teşhis aracı olarak çalışmaya devam etsin diye.

### Reddedilen alternatifler

- **Seq.** Teknik olarak en iyi .NET deneyimi ve ekip zaten biliyor; ücretsiz
  Individual lisansın **tek kullanıcıyla sınırlı** olması (Datalust'ın ifadesi:
  "birden fazla kişi erişemez, giriş bilgileri paylaşılsa bile") kuruma devirle
  bağdaşmadı. Team lisansı 790 $/yıl (4 Eylül 2026 fiyatı).
- **Promtail / Grafana Alloy.** Yukarıda; toplayıcıya ihtiyaç yok.
- **OTLP ile Loki'ye yazmak.** Loki OTLP'yi `/otlp` ucundan doğal olarak kabul
  ediyor ve uzun vadede satıcı bağımsız yol bu. Bugün reddedildi: Serilog sink'i
  tek satır yapılandırma, OTLP yolu OpenTelemetry paket setini ve yeni bir
  kavram katmanını getirir. Geçilecekse sink satırı değişir, uygulama kodu değil.
- **Serilog → PostgreSQL sink.** Logu teşhis edilecek veritabanının içine
  koyar; §2'deki 3. gerekçe.
- **Sentry (barındırılan).** Açık iş O-06 olarak duruyor; yurt dışı aktarım
  kararı verilmedi. Bkz. §9.

## 4. Faz A — Serilog çekirdeği

**Paketler.** Sürümler birbirine bağlı, sırayla doğrula:

```bash
cd backend/src/T3.Api
dotnet add package Serilog.AspNetCore          # 10.0.0 (28 Kasım 2025) → Serilog 4.3+
dotnet add package Serilog.Sinks.Grafana.Loki  # 9.0.2 (8 Ağustos 2026) → Serilog 4.3.1+ ister
```

> **Sürüm tuzağı:** Loki sink'inin 9.x sürümü Serilog **4.3.1 veya sonrasını**
> zorunlu kılıyor (v9 yeniden yazımı Serilog 4.x'in `IBatchedLogEventSink`
> altyapısına dayanıyor). `Serilog.AspNetCore` 10.x bu şartı karşılıyor; daha
> eski bir `Serilog.AspNetCore` seçilirse sink derlenmez. `Serilog.AspNetCore`
> konsol, dosya ve `CompactJsonFormatter`'ı zaten getiriyor — ayrıca eklenmez.
>
> `net8.0` hedefinin desteklendiğini `dotnet restore` ile doğrula; bu plan
> yazılırken NuGet'e erişim yoktu.

**`Program.cs`** — `builder` kurulduktan hemen sonra, `AddApplication()`'dan
önce:

```csharp
// Log yapılandırması appsettings'ten okunur: sink'i değiştirmek kod değil
// yapılandırma değişikliği olsun diye. Sırlar yine T3_ önekiyle geliyor.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Uygulama", "T3.Api")
    .Destructure.With<KisiselVeriMaskesi>());   // bkz. Faz C
```

Ardışık düzende, `UseForwardedHeaders()` **sonrasında** (gerçek IP oturmuş
olsun) ve `ExceptionHandlingMiddleware` **öncesinde**:

```csharp
// Her isteğe tek özet satır: yöntem, yol, durum, süre. Gövde loglanmaz.
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "{RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0} ms)";

    options.EnrichDiagnosticContext = (diagnostic, http) =>
    {
        diagnostic.Set("IstekId", http.TraceIdentifier);
        diagnostic.Set("Rol", http.User.FindFirst(AppClaims.Role)?.Value ?? "(anonim)");
        // Kullanıcı kimliği evet, e-posta hayır: Guid kişisel veri değil,
        // e-posta kişisel veridir ve logun rol kapısı yok.
        diagnostic.Set("KullaniciId", http.User.FindFirst(AppClaims.UserId)?.Value);
    };
});
```

**`appsettings.json`** — mevcut `Logging` bloğu Serilog'a çevrilir:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "WriteTo": [
    { "Name": "Console" },
    {
      "Name": "File",
      "Args": {
        "path": "storage/logs/t3-.jsonl",
        "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact",
        "rollingInterval": "Day",
        "retainedFileCountLimit": 14,
        "fileSizeLimitBytes": 52428800,
        "rollOnFileSizeLimit": true
      }
    }
  ]
}
```

> `Microsoft.EntityFrameworkCore.Database.Command` **`Warning`'de kalır.**
> `Information`'a çekilmesi parametreleriyle birlikte SQL'i loga yazar — vergi
> numarası ve tutarlar dâhil. Bu bir kolaylık ayarı değil, KVKK sınırı.

**Ne loglanmaz — kod yorumuyla birlikte sabitlenir:**

- `/api/auth/*` istek gövdesi (şifre, sıfırlama jetonu).
- Doküman yükleme gövdesi.
- Ham e-posta, telefon, vergi no, tutar (bkz. Faz C).
- JWT'nin kendisi ve `Authorization` / `Cookie` başlıkları.

## 5. Faz B — Korelasyon kimliği ve hata referansı

Amaç: kullanıcı ekranda gördüğü kısa kodu söylesin, log tek sorguda bulunsun.

1. **`RequestIdMiddleware`** (`T3.Api/Middleware/`): gelen `X-Request-Id`
   başlığı varsa ve biçimi güvenliyse onu kullanır, yoksa üretir; yanıta aynı
   başlıkla geri koyar ve `LogContext`'e iter.
   *Gelen değere doğrudan güvenilmez* — uzunluk ve karakter sınırı uygulanır,
   yoksa istemci loga istediğini yazdırır (log injection).
2. **`ApiErrorBody`** bir alan kazanır:
   ```csharp
   public sealed record ApiErrorBody(
       int Status, string Title, string[]? Errors = null, string? Referans = null);
   ```
   Şekli `ExceptionHandlingMiddleware`, `ValidationFilter` ve `ApiResults` ile
   birebir aynı kalmalı — üçü de aynı anda güncellenir.
3. **`ExceptionHandlingMiddleware`** 500 gövdesine `Referans = http.TraceIdentifier`
   koyar. 4xx'e koymaz: doğrulama hatası kullanıcının düzeltebileceği bir şey,
   referans numarası gürültü olur.
4. **Arayüz**: `lib/apiClient.ts` `Referans`'ı okur, hata bileşeni "Hata
   referansı: `a3f9c1`" satırını gösterir ve kopyalanabilir yapar.

## 6. Faz C — KVKK redaksiyonu (pazarlıksız)

Log **yeni bir kişisel veri deposudur**. Ürünün her alanı role göre maskelenirken
`LogInformation("{Email} giriş yaptı")` satırı ham adresi rol kapısı olmayan bir
yere yazar. Bu, KVKK uyumlu bir ürünün yanına KVKK deliği açmaktır.

1. **`KisiselVeriMaskesi`** (`T3.Infrastructure/Logging/`) — Serilog
   `IDestructuringPolicy`. Nesne loglandığında `Email`, `Phone`, `TaxNumber`,
   `Amount`, `ContactEmail` gibi ad taşıyan alanları maskeler. E-posta için
   mevcut `MaskedEmail.Of()` kullanılır — ikinci bir maskeleme kuralı yazılmaz,
   `AuditWriter` ile aynı davranış korunur.
2. **Alan adı listesi tek yerde** durur ve yorumda "neden" yazar. Yeni hassas
   alan eklendiğinde buraya da eklenir; `rbac-kvkk-denetcisi` ajanının kontrol
   listesine bu dosya girer.
3. **Saklama süresi:** dosyada 14 gün (`retainedFileCountLimit`), Loki'de 14 gün
   (§7 — Loki'de bu **ayrıca açılmak zorunda**, varsayılanı sonsuz).
4. **Aydınlatma metni güncellenir** (`features/legal/PrivacyNoticePage.tsx`):
   işlenen veri kategorilerine "teknik kayıt (IP, istek kimliği, tarayıcı)" ve
   saklama süresi eklenir. Metinler zaten "Taslak" işaretli; hukuki onay
   alınırken bu madde de kapsama girer.
5. **Birim testi:** `KisiselVeriMaskesiTests` — e-posta, telefon ve tutar
   içeren bir nesnenin log çıktısında ham değerin **bulunmadığı** doğrulanır.
   Bu test, kuralın sessizce gevşemesini engelleyen tek mekanizmadır.

## 7. Faz D — Loki + Grafana

### 7.1. Etiket disiplini — Loki'yi öldüren hata

Loki'de **etiket (label) ile log satırı içeriği farklı şeylerdir.** Her benzersiz
etiket kombinasyonu ayrı bir akış (stream) yaratır; yüksek kardinaliteli bir
alan etiket yapılırsa Loki milyonlarca akış üretip çöker.

- **Etiket olacaklar** (sabit, küçük küme): `app`, `env`, `level`.
- **Etiket olmayacaklar:** `IstekId`, `KullaniciId`, `RequestPath`, `Rol`.
  Bunlar log satırının **içinde** durur ve LogQL ile aranır
  (`{app="t3-api"} | json | IstekId="a3f9c1"`).

Bu ayrım sink yapılandırmasında `propertiesAsLabels` listesiyle kurulur; listeye
alan eklemek ucuz görünen ama geri dönüşü pahalı bir karardır.

### 7.2. `loki-config.yaml` (tek ikili, dosya sistemi)

```yaml
auth_enabled: false          # tek kiracı; port yalnızca loopback'e bağlı (aşağıya bak)

server:
  http_listen_port: 3100

common:
  path_prefix: /loki
  storage:
    filesystem:
      chunks_directory: /loki/chunks
      rules_directory: /loki/rules
  replication_factor: 1
  ring:
    kvstore: { store: inmemory }

schema_config:
  configs:
    - from: 2026-01-01
      store: tsdb
      object_store: filesystem
      schema: v13
      index: { prefix: index_, period: 24h }

limits_config:
  allow_structured_metadata: true
  retention_period: 336h      # 14 gün — KVKK saklama süresi
  ingestion_rate_mb: 4
  ingestion_burst_size_mb: 8

# Loki VARSAYILAN OLARAK HİÇBİR ŞEYİ SİLMEZ. retention_period tek başına
# yetmez; silmeyi yapan compactor'dır ve retention_enabled açıkça verilmelidir.
compactor:
  working_directory: /loki/compactor
  retention_enabled: true
  delete_request_store: filesystem
  retention_delete_delay: 2h
```

> **Bu bloğun en önemli satırı `retention_enabled: true`.** Grafana'nın kendi
> belgesi: "varsayılan olarak `compactor.retention-enabled` bayrağı ayarlı
> değildir, dolayısıyla Loki'ye gönderilen loglar sonsuza kadar yaşar."
> VPS'te ~5 GB boş alan ve 5 başka compose projesi var; bu satır unutulursa log
> deposu diski doldurur ve **yalnızca T3'ü değil, o makinedeki herkesi** düşürür.

### 7.3. `docker-compose.yml` (geliştirme)

```yaml
  loki:
    image: grafana/loki:3.4
    container_name: t3-loki
    restart: unless-stopped
    command: -config.file=/etc/loki/loki-config.yaml
    volumes:
      - ./ops/loki-config.yaml:/etc/loki/loki-config.yaml:ro
      - loki-data:/loki
    ports:
      - "127.0.0.1:3100:3100"   # yalnızca loopback: log deposu ağa açılmaz

  grafana:
    image: grafana/grafana:11.6.0
    container_name: t3-grafana
    restart: unless-stopped
    depends_on: [loki]
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${T3_GRAFANA_PASSWORD:?T3_GRAFANA_PASSWORD gerekli}
      GF_AUTH_ANONYMOUS_ENABLED: "false"
      GF_USERS_ALLOW_SIGN_UP: "false"
    volumes:
      - ./ops/grafana-datasource.yaml:/etc/grafana/provisioning/datasources/loki.yaml:ro
      - grafana-data:/var/lib/grafana
    ports:
      - "127.0.0.1:3300:3000"   # Grafana'nın varsayılanı 3000; VPS'te 3001 dolu
```

`admin/admin` varsayılanı bırakılmaz — parola `.env`'den `T3_GRAFANA_PASSWORD`
ile gelir, `.env.example`'a yalnızca yer tutucu yazılır.

### 7.4. Serilog sink yapılandırması

`appsettings.Development.json` (üretimde adres ortam değişkeninden gelir):

```json
{
  "Name": "GrafanaLoki",
  "Args": {
    "uri": "http://loki:3100",
    "labels": [
      { "key": "app", "value": "t3-api" },
      { "key": "env", "value": "development" }
    ],
    "propertiesAsLabels": [ "level" ]
  }
}
```

`propertiesAsLabels` listesi bilinçli olarak tek elemanlı — gerekçesi §7.1.

## 8. Faz E — Üretim ve devir

- **VPS'e Loki + Grafana koymadan önce disk hesabı yapılır.** 14 günlük
  retention ve günlük log hacmi ölçülür (`storage/logs` boyutundan tahmin
  edilebilir); ~5 GB boş alanın en fazla yarısı hedeflenir. Ölçüm çıkmazsa
  VPS'te yalnızca dosya sink'i kalır, Loki geliştirme ortamında çalışır.
- `docker-compose.prod.yml` içindeki `api` servisine docker log sürücü sınırı
  eklenir:
  ```yaml
    logging:
      driver: json-file
      options: { max-size: "20m", max-file: "5" }
  ```
  Bu satır olmadan `docker logs` sınırsız büyür.
- **Port seçimi:** sunucuda 6540/8080/8081/3001/5246/5341/12000/12001 dolu.
  Loki ve Grafana kullanılmayan portlara, **yalnızca loopback'e** bağlanır;
  erişim SSH tüneliyle olur. Grafana dışarı açılacaksa nginx + TLS + kimlik
  doğrulama şart — API için kurulan kalıbın aynısı.
- **Devir notu:** Loki'nin kullanıcı sınırı olmadığı için vakıf kendi ekibine
  istediği kadar Grafana hesabı açabilir; ek lisans maliyeti yok.

## 9. Sentry / hata izleme (açık iş O-06)

Bu plan **kapsamıyor**. Log "ne oldu"yu, hata izleme "hangi hata yeni ve kaç
kişiyi vurdu"yu cevaplar; ikisi birbirinin yerine geçmez. Log sistemi
kurulduktan sonra yeniden değerlendirilir. Yurt dışı aktarım kararı hâlâ
blokaj; kendi sunucunda çalışan Sentry API uyumlu bir seçenek (GlitchTip) o
blokajı kaldırır ama ek konteyner + Postgres/Redis bağımlılığı getirir.
Grafana tarafında alarm (`Grafana Alerting`) LogQL sorgusu üzerinden kurulabilir
ve basit eşik uyarıları için yeterlidir — yığın izi gruplandırma ve sürüm takibi
vermez.

## 10. Doğrulama

Her fazdan sonra, `scripts/README.md` sırasına uyarak:

```bash
cd backend && dotnet build && dotnet test
```

Yeni kontroller eklenir:

- **Birim:** `KisiselVeriMaskesiTests` — hassas alan log çıktısında ham geçmiyor.
- **Uçtan uca:** 500 üreten bir uçta yanıt gövdesinde `Referans` alanı var ve
  `X-Request-Id` yanıt başlığıyla aynı.
- **Render:** hata ekranında referans numarası **görünüyor** ve kopyalanabilir.
  (`index.html` 200 döndü demek ekranda göründü demek değil — bu adım atlanmaz.)
- **Loki:** API'ye bir istek at, `logcli` ya da Grafana'dan
  `{app="t3-api"} | json | IstekId="…"` sorgusu satırı bulsun.
- **Retention:** `compactor` günlüğünde silme turunun koştuğu görülsün; yalnızca
  yapılandırmaya bakıp "açık" demek yetmez.
- **Elle:** `/api/auth/login`'e yanlış şifreyle vur, hem log dosyasında hem
  Loki'de şifrenin ve ham e-postanın **geçmediğini** doğrula.

## 11. Riskler

| Risk | Önlem |
|---|---|
| Log kişisel veri sızdırır | Faz C zorunlu; birim testi kuralı kilitler; `rbac-kvkk-denetcisi` kontrol listesine girer |
| **Loki sonsuza kadar saklar, disk dolar, diğer projeler düşer** | `compactor.retention_enabled: true` + `retention_period`; doğrulama adımında silme turu gözlenir; VPS'e koymadan önce disk hesabı |
| Yüksek kardinaliteli etiket Loki'yi çökertir | `propertiesAsLabels` küçük ve sabit; §7.1 gerekçesiyle birlikte kodda durur |
| Grafana varsayılan parolayla açık kalır | `GF_SECURITY_ADMIN_PASSWORD` zorunlu, anonim erişim kapalı, port loopback |
| Sürüm uyuşmazlığı (sink Serilog 4.3.1+ ister) | Paketler §4'teki sırayla eklenir; `dotnet restore` ilk adım |
| `ApiErrorBody` şekli üç yerde ayrışır | `ExceptionHandlingMiddleware`, `ValidationFilter`, `ApiResults` aynı commit'te güncellenir |
| Loki düşerse log kaybolur | Konsol + dosya sink'i kaldırılmaz; Loki üçüncü hedeftir, tek hedef değil |

## 12. Uygulama sırası

1. Faz A (Serilog + istek logu, konsol + dosya) → `dotnet build && dotnet test`
2. Faz C (redaksiyon + test) → **A'dan hemen sonra**, arada log yazılmadan
3. Faz B (korelasyon + hata referansı + arayüz) → `npm run build`
4. Faz D (Loki + Grafana, geliştirme) → etiket disiplini ve retention doğrulanır
5. Faz E (üretim sınırları, disk hesabı) → `render_vps.py`
6. Tam doğrulama turu (`scripts/README.md`, 4 tur)

> Faz C'nin A'dan hemen sonra gelmesi bilinçli: arada geçen her koşuda maskesiz
> log yazılır ve o dosyalar elle temizlenmek zorunda kalır. Faz D'nin Faz C'den
> sonra gelmesi de aynı gerekçeyle — maskesiz satır Loki'ye bir kez girerse
> retention süresi dolana kadar orada durur.
