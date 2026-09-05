# Mail Servisi Kurulumu — t3girisimportali.com

**Durum:** Kod tarafı yazıldı (bkz. "Kod tarafı"); **hesaplar ve DNS kayıtları
henüz kurulmadı**. SMTP değişkenleri boş olduğu sürece üretimde
`ThrowingEmailSender` kayıtlı kalır, yani canlıda şifre sıfırlama isteği açıkça
hata verir — sessizce kaybolmaz.

## Neden iki servis?

İki ayrı ihtiyaç var ve **tek ücretsiz servis ikisini birden karşılamıyor**:

| İhtiyaç | Kim yapacak | Neden |
|---|---|---|
| Uygulamanın mail *atması* (şifre sıfırlama, bildirim) | **Brevo** | Günde 300 mail, süresiz ücretsiz, kredi kartı istemiyor, SMTP relay veriyor |
| `info@`, `iletisim@` gibi kutulara mail *gelmesi* ve oradan yazışmak | **Zoho Mail (Forever Free)** | 5 kullanıcı, kullanıcı başına 5 GB, tek alan adı, ücretsiz |

Kritik ayrım: **Zoho'nun ücretsiz planı IMAP/POP/ActiveSync vermiyor** — kutulara
yalnızca web arayüzü ve Zoho mobil uygulamasından erişiliyor. Bu yüzden uygulama
Zoho üzerinden SMTP ile mail atamaz; gönderim ayrı servisten (Brevo) gitmek
zorunda. İkisi aynı alan adı altında çakışmadan çalışır: MX (gelen) Zoho'yu
gösterir, SPF/DKIM (giden) her ikisini birden yetkilendirir.

## Adım 1 — Brevo hesabı ve alan adı doğrulama

1. brevo.com'dan ücretsiz hesap aç. Kayıt için **kurumsal bir adres** kullan;
   gmail/hotmail ile açılan hesaplar doğrulamada takılabiliyor.
2. **Senders, Domains & Dedicated IPs → Domains → Add a domain** →
   `t3girisimportali.com`.
3. Sihirbaz üç kayıt verir (değerler hesaba özeldir, aşağıdaki tabloya işle):
   - `brevo-code` doğrulama TXT'i
   - DKIM: `mail._domainkey` TXT
   - SPF include: `spf.brevo.com`
4. **SMTP & API → SMTP** sekmesinden bir **SMTP key** üret. Kod fazında lazım:
   - Sunucu: `smtp-relay.brevo.com`
   - Port: `587` (STARTTLS)
   - Kullanıcı: hesabın giriş e-postası (panelde "Login" olarak yazar)
   - Şifre: SMTP key (hesap şifresi **değil**)
5. Gönderen adresini (`no-reply@t3girisimportali.com`) Senders listesine ekle.

## Adım 2 — Zoho Mail (Forever Free)

1. zoho.com/mail → **Forever Free Plan** ile kaydol, "kendi alan adım var" yolunu
   seç. Ücretsiz plan bazen fiyat sayfasında aşağıda gizli kalıyor; ücretli
   plana geçmeden önce Free Plan sekmesini kontrol et.
2. Alan adı doğrulaması: Zoho bir TXT (`zoho-verification=...`) veya CNAME
   verir → tabloya işle → panelde "Verify".
3. `info@t3girisimportali.com` gibi ilk kutuyu (admin) oluştur.
4. Zoho'nun **MX kayıtları veri merkezine göre değişir** — panelin sana
   gösterdiği değerleri kullan, aşağıdaki liste yalnızca ne bekleyeceğini
   bilmen için:
   - ABD (zoho.com) DC: `mx.zoho.com` (10), `mx2.zoho.com` (20), `mx3.zoho.com` (50)
   - AB (zoho.eu) DC: `mx.zoho.eu` (10), `mx2.zoho.eu` (20), `mx3.zoho.eu` (50)
5. Zoho'nun DKIM'ini de aç (**Domains → DKIM → Add selector**, tipik seçici
   `zmail`): `zmail._domainkey` TXT.

## Adım 3 — DNS kayıtları (turkticaret.net paneli)

Önce **yetkili DNS sunucusu kim** onu doğrula: alan adı turkticaret.net'ten
alındıysa büyük ihtimalle nameserver'lar da onlarda ve kayıtlar oradaki
"Alan Adı / DNS Yönetimi" ekranından giriliyor. A kaydı VPS'i gösteriyor
(siteyi oraya yönlendirmiştik), yani panel doğru panel.

Girilecek kayıtlar:

| Tip | Host / Ad | Değer | Not |
|---|---|---|---|
| MX | `@` | `mx.zoho.com` — öncelik 10 | Zoho panelindeki değeri kullan |
| MX | `@` | `mx2.zoho.com` — öncelik 20 | |
| MX | `@` | `mx3.zoho.com` — öncelik 50 | |
| TXT | `@` | `zoho-verification=zb…` | Zoho'nun verdiği doğrulama |
| TXT | `@` | `brevo-code:…` | Brevo'nun verdiği doğrulama |
| TXT | `@` | `v=spf1 include:zoho.com include:spf.brevo.com ~all` | **Tek satır, aşağıya bak** |
| TXT | `zmail._domainkey` | `v=DKIM1; k=rsa; p=…` | Zoho DKIM |
| TXT | `mail._domainkey` | `k=rsa; p=…` | Brevo DKIM |
| TXT | `_dmarc` | `v=DMARC1; p=none; rua=mailto:dmarc@t3girisimportali.com` | Önce `p=none` |

Panel `@` yerine boş host ya da alan adının kendisini isteyebilir; üçü de kök
alan adı demektir. TTL'i 3600 (1 saat) bırak, yayılma tipik olarak 15 dk–2 saat.

### Bu tabloda insanların en çok yanıldığı üç yer

1. **SPF tek kayıt olmalı.** İki ayrı `v=spf1 …` TXT'i yan yana koyarsan SPF
   *geçersiz* olur (RFC 7208) ve iki servisin de maili spam'e düşer. Zoho ve
   Brevo'nun include'ları **aynı satırda** birleştirilir. Zoho AB veri
   merkezindeyse `include:zoho.com` yerine `include:zoho.eu` yazılır.
2. **Hostingin varsayılan MX'i silinmeli.** turkticaret.net alan adıyla birlikte
   kendi mail sunucusuna işaret eden bir MX bırakmış olabilir; kalırsa gelen
   mailin bir kısmı oraya düşer ve Zoho'da hiç görünmez. Zoho MX'lerini eklemeden
   önce eski MX satırlarını temizle.
3. **`~all` yerine `-all` yazmak için acele etme.** Kurulum oturana kadar soft
   fail kalsın; DMARC raporlarında iki hafta temiz görüntü aldıktan sonra
   `-all` ve `p=quarantine`'e geç.

## Adım 4 — Doğrulama

```bash
dig +short MX  t3girisimportali.com
dig +short TXT t3girisimportali.com          # tek bir v=spf1 satırı görmelisin
dig +short TXT zmail._domainkey.t3girisimportali.com
dig +short TXT mail._domainkey.t3girisimportali.com
dig +short TXT _dmarc.t3girisimportali.com
```

Uçtan uca test: Gmail'e bir mail at, gelen mailde **Show original** →
`SPF: PASS`, `DKIM: PASS`, `DMARC: PASS` üçünü de gör. mail-tester.com'a
gönderip 10/10'a yakın skor almak da hızlı bir kontrol.

## Kod tarafı (yazıldı)

Kod hazır; devreye girmesi için yalnızca yukarıdaki hesapların açılması ve dört
ortam değişkeninin doldurulması gerekiyor.

| Dosya | Ne yapar |
|---|---|
| `T3.Infrastructure/Notifications/SmtpEmailSender.cs` | MailKit ile gerçek gönderim; başlık kurulumu test edilebilsin diye `BuildMessage` ayrı |
| `T3.Infrastructure/Notifications/EmailOptions.cs` | `FromName`, `ReplyTo` ve `Smtp` (host/port/kullanıcı/parola/StartTLS/zaman aşımı) |
| `T3.Infrastructure/DependencyInjection.cs` | `AddEmailSender`: SMTP tamsa gerçek gönderici, değilse üretimde `Throwing` / geliştirmede `FileOutbox` |
| `Features/Auth/RequestPasswordReset/RequestPasswordResetHandler.cs` | Gönderim hatası yanıtı değiştirmiyor; ize `gönderim başarısız` düşüyor |
| `tests/T3.Application.Tests/EpostaGonderimTests.cs` | 6 test: yapılandırma tespiti, gönderen/yanıt başlıkları, düz metin gövde |

Kararların gerekçesi: [Gelistirme_Kararlari.md §3m](Gelistirme_Kararlari.md).

### Devreye alma

`.env.prod` içine (değerler Brevo panelinden):

```bash
MAIL_FROM="no-reply@t3girisimportali.com"
MAIL_REPLY_TO="info@t3girisimportali.com"     # Zoho'daki gerçek kutu
SMTP_HOST="smtp-relay.brevo.com"
SMTP_PORT="587"
SMTP_USER="brevo-hesabinin-giris-adresi"
SMTP_PASSWORD="brevo-smtp-key"                # hesap şifresi DEĞİL
```

`docker-compose.prod.yml` bunları `T3_Email__Smtp__*` olarak konteynere
geçiriyor. Dördü birden dolu değilse uygulama üretimde `ThrowingEmailSender`
ile kalır — yani "sessizce mail gitmiyor" durumu oluşmaz, sıfırlama isteği
açıkça hata verir ve log'a düşer.

### Doğrulama sırası

```bash
# 1. Derleme + birim testleri (yeni MailKit paketi ilk derlemede indirilir)
cd backend && dotnet build && dotnet test

# 2. Yerelde gerçek SMTP denemesi: .env içindeki dört SMTP satırının yorumunu
#    kaldır, API'yi başlat, kendi adresine sıfırlama iste
curl -X POST http://localhost:5080/api/auth/forgot-password \
  -H 'Content-Type: application/json' \
  -d '{"email":"kendi-adresin@ornek.com"}'
```

Beklenen: log'da `E-posta gönderildi: k***@ornek.com`, kutuda mail, mailde
`SPF: PASS` / `DKIM: PASS`. Log'da **bağlantının kendisi görünmemeli** — gövde
bilerek loglanmıyor, çünkü ham jeton taşıyor.

Sık görülen iki hata:

- `535 Authentication failed` → `SMTP_USER` hesabın giriş adresi olmalı,
  `SMTP_PASSWORD` panelde üretilen SMTP key olmalı.
- `Sender not verified` / `550` → `MAIL_FROM` adresi Brevo'da doğrulanmış
  gönderenler arasında değil.

## Limitler (ücretsiz katmanlar, Eylül 2026)

- **Brevo:** günde 300 mail, süresiz ücretsiz. Şifre sıfırlama hacmi için
  fazlasıyla yeterli; toplu duyuru yapılacaksa limit burada biter.
- **Zoho Forever Free:** 5 kullanıcı, kullanıcı başına 5 GB, tek alan adı,
  30 MB ek dosya sınırı, **IMAP/POP/ActiveSync yok** (webmail + Zoho mobil).
  6. kullanıcıda tüm hesap ücretliye geçer.
