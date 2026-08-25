# Demo senaryosu, video çekim planı ve jüri soruları

> **Taslak — ekip onayı bekliyor.** 26 Ağustos teslimindeki iki çıktı (prototip
> videosu + sunum) ve 5–6 Eylül Demo Day'deki 5 dk pitch + 5 dk soru-cevap için.
> Buradaki her adım **çalışan** ürün üzerinde denendi; ekranda olmayan bir şey
> senaryoya yazılmadı.
>
> Demo hesapları ve şifre: [README — Demo hesapları](../README.md#demo-hesapları).

---

## 0. Demo öncesi kontrol listesi (5 dakika, atlanmaz)

1. **Veritabanını sıfırla ve tohum verisini yükle.** Doğrulama betikleri veriyi
   değiştiriyor (girişim pasife alıyor, öneri onaylıyor); temiz tohum verisi
   olmadan sayılar demoda tutarsız görünür.
   ```bash
   docker compose down -v && docker compose up -d postgres
   ```
2. API ve arayüzü başlat, `/health` 200 dönsün.
3. **Onay kuyruğunda bekleyen öneri olduğunu doğrula** (rozette sayı görünmeli) —
   demonun kalbi bu ekran.
4. Tarayıcıyı **temiz profille** aç (eski oturum çerezi kalmasın), yakınlaştırmayı
   %100'e al, ekran genişliğini 1440 px civarına getir.
5. Mobil görüntü çekilecekse cihaz emülasyonunu 390 px'e al — üç genişlikte yatay
   taşma kontrolü var, güvenle gösterilebilir.

---

## 1. Prototip videosu — çekim planı (hedef: 3 dakika)

Anlatının kuralı: **her sahne bir MVP maddesini kapatır** ve söylenen cümle
ekranda görünenle birebir aynı olur.

| # | Süre | Rol | Ekran | Ne yapılır | Ne söylenir (özet) |
|---|---|---|---|---|---|
| 1 | 0:00–0:20 | — | Giriş ekranı | Sadece giriş ekranı, sonra Süper Yönetici ile giriş | "Dağınık girişim verisi tek platformda; erişim rol bazlı. Dört rol var, hepsini sırayla göstereceğiz." |
| 2 | 0:20–0:50 | Süper Yönetici | `/pano` | Sektör/program filtresini değiştir, toplamların değiştiğini göster | "Ekosistemin karnesi: kaç girişim, hangi programda, ne kadar yatırım. Rapor siparişi gerekmiyor." |
| 3 | 0:50–1:20 | Süper Yönetici | `/girisimler` → girişim kartı | Ara → karta gir → **Genel/Ekip/Program/Başarı/Doküman** sekmelerini gez | "**MVP #1:** tek girişim kartı — profil, ekip, ürün, program geçmişi, finans ve doküman aynı yerde." |
| 4 | 1:20–1:40 | Süper Yönetici | Kartta **Gelişim yolculuğu** | Kronolojiyi yukarıdan aşağı göster | "**MVP #2:** bu girişim bizimle nereden nereye geldi — program katılımları ve dönüm noktaları kronolojik." |
| 5 | 1:40–2:10 | **Girişim kullanıcısı** | `/portal` | Bir yatırım turu ya da ekip üyesi ekle → "öneri kuyruğa alındı" mesajını göster | "**MVP #3:** girişim kendi verisini günceller ama **hiçbir tabloya doğrudan yazmaz.** Çıkan şey bir öneri." |
| 6 | 2:10–2:35 | Program Yöneticisi | `/onaylar` → öneri detayı | Diff ekranını göster, **onayla** | "Aynı öneri, karar veren tarafta: ne değişiyor, eski ve yeni değer yan yana. Onaydan sonra kayıt yayında." |
| 7 | 2:35–2:50 | **Karar Verici** | Aynı girişim kartı | Tutar alanında **kilit/maskeleme** göster | "**KVKK:** aynı kart, farklı rol. Karar Verici ekosistem toplamını görüyor, tek girişimin tutarını görmüyor. Kural kodda tek yerde." |
| 8 | 2:50–3:00 | Süper Yönetici | `/denetim` | Onay + indirme + giriş satırlarını göster | "Her işlem izde: kim, ne zaman, hangi IP. **MVP #4** yapılandırılmış veri + hesap verebilirlik." |

### Çekim notları

- **Şifre yazarken ekranı gizleme derdine girmeyin** — demo hesapları kurgusal,
  şifre README'de zaten yazılı. Ama gerçek bir e-posta/telefon **hiç**
  görünmemeli: tohum verisi `.test` alan adı ve tahsis edilmemiş telefon öneki
  kullanıyor, o yüzden güvenli.
- Sahne 5→6 geçişinde **rol değiştiğini ekranda belli edin** (sağ üstteki rol
  rozeti kadraja girsin). Videonun en kolay kaybettiği şey "şimdi kim
  bakıyor" bilgisi.
- Sahne 7 videonun **en değerli 15 saniyesi**: maskeleme jüriye KVKK
  gereksinimini kapattığınızı gösteren tek somut kanıt. Kesmeyin.
- Kayıt sırasında ağı kesmeyin; ürün ağ hatasında Türkçe uyarı gösteriyor ama
  video için gereksiz gürültü.

---

## 2. Demo Day — 5 dakika pitch iskeleti

| Süre | Bölüm | İçerik |
|---|---|---|
| 0:00–0:40 | **Problem** | "T3 ekosisteminde girişim verisi program program dağınık: Ön Kuluçka'nın Excel'i, TEKNOFEST'in formu, kurucunun sunumu. Aynı girişimin üç farklı hikâyesi var ve hiçbiri güncel değil." |
| 0:40–1:10 | **Kime ne kaybettiriyor** | Program yöneticisi mükerrer veri girişi; karar verici haftalar süren rapor turu; girişim aynı bilgiyi her yere yeniden yazıyor; program sorumlusu değişince hafıza kişiyle gidiyor. |
| 1:10–3:30 | **Çözüm — canlı demo** | Videodaki 3, 4, 5, 6, 7 numaralı sahneler (kart → yolculuk → portal önerisi → onay → maskeleme). **Panoyu en sona bırakın**: en gösterişli ekran ama en az ayırt edici olan. |
| 3:30–4:10 | **Nasıl kurumsal** | Varsayılan kapalı yetkilendirme, satır + alan düzeyinde erişim, onay akışı, denetim izi, KVKK maskelemesi tek noktada, tek origin dağıtım + CSP/çerez güvenliği. "Doğrulama: 191 birim testi, 310 uçtan uca kontrol, 352 render kontrolü." |
| 4:10–4:40 | **AI** | "AI'ı uygulamaya anahtar gömerek değil **MCP sunucusu** olarak verdik: analist Claude'dan bağlanıyor ve **kendi yetkisi kadar** görüyor. Yani AI yetki sisteminin yanından dolaşan bir kapı değil." |
| 4:40–5:00 | **Kapanış** | "Altı zorunlu MVP maddesi çalışıyor, kalanı ölçek ve içerik onayı. Bugün kurulabilir hâlde." |

### Pitch'te söylenmeyecekler

- Uydurma kullanıcı/ciro projeksiyonu.
- "Yapay zekâ destekli" ifadesini panelin yerel planlayıcısı için kullanmak —
  panel model bağlı değilken bunu **kendisi** söylüyor; jüri ekranda görür.
- "Tamamen KVKK uyumlu" — metinlerin hukuki onayı yok. Doğru cümle: "KVKK
  gereksinimleri teknik olarak karşılandı, aydınlatma metninin hukuki onayı
  sürüyor ve ürün bunu ekranda taslak olarak işaretliyor."

---

## 3. Jüri soruları — hazır cevaplar

**"Girişimi sisteme kim ekliyor?"**
Program Yöneticisi ya da Süper Yönetici arayüzden ekliyor; girişim kullanıcısı
kendi profilini portaldan güncelliyor ama yayına onayla giriyor. Program
Yöneticisi eklediği kaydı **kendi programının bir dönemine bağlamak zorunda** —
kapsamı "programlarımdan geçmiş girişimler" olarak tanımlı, ekran bu kuralı
kaydettikten sonra söylüyor.

**"Girişim yanlış/şişirilmiş finansal veri girerse?"**
Girişim hiçbir tabloya doğrudan yazamıyor. Her kayıt önce öneri; karar veren
diff ekranında eski/yeni değeri görüp onaylıyor ya da gerekçeyle reddediyor.
Finansal kayıtlarda kaynak/doğrulama alanı var ve her karar denetim izine
düşüyor.

**"Program yöneticisi başka programın girişimini görebiliyor mu?"**
Hayır. Satır düzeyi kapsam tek noktada (`IStartupScope`) ve kapsam dışı kayıt
403 değil **404** dönüyor — 403 "böyle bir kayıt var" bilgisini sızdırırdı.
Onay kuyruğu da aynı şekilde ayrık: iki yöneticinin kuyruğu kesişmiyor,
birleşimi tüm istekleri veriyor (uçtan uca kontrol bunu ölçüyor).

**"KVKK'yı nasıl karşıladınız?"**
Üç katman: (1) alan düzeyi maskeleme — finansal tutar ve kişisel iletişim
bilgisi role göre; kural tek dosyada ve okunabilir. (2) Maskelenen alan yazma
yolunda da korunuyor: göremediğiniz vergi numarasını kaydettiğinizde
silinmiyor. (3) Aydınlatma metni + kullanım şartları girişten önce erişilebilir,
başvuru adresi ekranda. Eksik: metinlerin hukuki onayı — ürün bunu "Taslak"
olarak işaretliyor.

**"Yapay zekâyı nerede kullanıyorsunuz?"**
Karar destekte. Ama modeli uygulamaya gömmedik: sistem `POST /mcp` ile MCP
sunucusu; harici ajan kendi jetonuyla bağlanıyor, araçlar REST ile **aynı**
handler'ları sarıyor, dolayısıyla AI da satır kapsamı ve maskelemeye tabi.
Anahtar girilirse panel içi yanıtlar da modele geçiyor; girilmezse panel yerel
planlayıcıyla çalıştığını **söylüyor**.

**"Veriler nerede duruyor, dışarı çıkıyor mu?"**
PostgreSQL, vakıf kontrolündeki sunucuda. Uygulama arayüzü ile API'yi aynı
kökten sunuyor; harici servise istek yok. AI kullanımı MCP ile kullanıcının
kendi istemcisinden yapılıyor ve o kullanım denetim izine düşüyor.

**"Oturum/kimlik güvenliği?"**
Jeton `HttpOnly` + `SameSite=Strict` çerezde (script okuyamıyor), CSP `'self'`
ile sınırlı, çerezle gelen yazma isteği CSRF jetonu istiyor. Giriş denemeleri
IP + e-posta başına sınırlı ve başarısız girişler maskeli e-postayla ize
düşüyor. Yönetici şifre atadığında kullanıcı ilk girişte değiştirmeden başka
ekrana geçemiyor — şifrenin ikinci sahibi kalmıyor.

**"Kaç kişi kullanabilir / ölçek?"**
Hedef ölçek yüzlerce girişim, onlarca kurum içi kullanıcı. Sorgular sayfalı,
liste ve rapor uçları aynı filtreyi paylaşıyor. Bu ölçekte tek uygulama
konteyneri + tek PostgreSQL yeterli; darboğaz beklenen yerde değil.

**"Yapmadığınız ne var?"** (bu soru gelirse dürüst cevap en iyi cevap)
Dört şey: KVKK metinlerinin hukuki onayı; hata izleme servisi bağlantısı
(yeri hazır, hesap/DSN ve yurt dışı aktarım kararı gerekiyor); gerçek SMTP
(şifre sıfırlama e-postası şimdilik sunucudaki geliştirme kutusuna yazılıyor);
üretim konteyner imajının gerçek bir sunucuda çalıştırılması (Dockerfile ve
compose yazıldı, tek origin sunum yerelde uçtan uca doğrulandı).

---

## 4. Yedek plan

- **İnternet/ağ giderse:** demo tamamen yerel (Postgres + API + arayüz aynı
  makinede). Videoyu da yanınızda bulundurun.
- **Bir ekran açılmazsa:** ürün beyaz ekran yerine Türkçe hata veriyor;
  panikleyip yenilemek yerine "hata sınırı devrede" deyip video kaydına geçin.
- **Süre daralırsa:** sahne 2 (pano) ve sahne 8 (denetim izi) kesilebilir.
  **Sahne 5-6-7 kesilemez** — MVP #3 ve KVKK oradan gösteriliyor.
